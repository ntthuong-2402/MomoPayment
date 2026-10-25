using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MomoPayment.EventBus.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MomoPayment.EventBus;

public class EventBusRabbitMQ : IEventBus, IDisposable
{
    private readonly IRabbitMqConnection _persistentConnection;
    private readonly ILogger<EventBusRabbitMQ> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly Dictionary<string, List<Type>> _handlers = new();
    private readonly List<Type> _eventTypes = new();
    private IModel? _consumerChannel;
    private string? _queueName;

    public EventBusRabbitMQ(
        IRabbitMqConnection persistentConnection,
        ILogger<EventBusRabbitMQ> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options)
    {
        _persistentConnection = persistentConnection;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _queueName = _options.SubscriptionClientName;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IntegrationEvent
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        if (!_persistentConnection.IsConnected)
        {
            _logger.LogWarning("RabbitMQ is not connected. Event {EventId} not published", @event.Id);
            return Task.CompletedTask;
        }

        using var channel = _persistentConnection.CreateModel();
        var eventName = @event.GetType().Name;

        channel.ExchangeDeclare(exchange: _options.ExchangeName, type: "direct", durable: true);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions
        {
            WriteIndented = false
        });

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // Persistent
        properties.MessageId = @event.Id.ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: eventName,
            mandatory: true,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published event {EventName} with ID {EventId} to exchange {Exchange}", eventName, @event.Id, _options.ExchangeName);
        return Task.CompletedTask;
    }

    public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = typeof(T).Name;
        var handlerType = typeof(TH);

        if (!_eventTypes.Contains(typeof(T)))
        {
            _eventTypes.Add(typeof(T));
        }

        if (!_handlers.ContainsKey(eventName))
        {
            _handlers[eventName] = new List<Type>();
        }

        if (_handlers[eventName].Contains(handlerType))
        {
            return;
        }

        _handlers[eventName].Add(handlerType);

        StartBasicConsume(eventName);
    }

    private void StartBasicConsume(string eventName)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        if (!_persistentConnection.IsConnected)
        {
            _logger.LogWarning("RabbitMQ not connected. Could not start consuming event {EventName}", eventName);
            return;
        }

        _consumerChannel ??= CreateConsumerChannel();

        _consumerChannel.QueueBind(
            queue: _queueName,
            exchange: _options.ExchangeName,
            routingKey: eventName);
    }

    private IModel CreateConsumerChannel()
    {
        var channel = _persistentConnection.CreateModel();

        channel.ExchangeDeclare(exchange: _options.ExchangeName, type: "direct", durable: true);

        channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += Consumer_Received;

        channel.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);

        return channel;
    }

    private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

        try
        {
            await ProcessEvent(eventName, message);
            _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RabbitMQ message for event {EventName}", eventName);
            // Requeue or dead-letter based on policies
            _consumerChannel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        if (!_handlers.TryGetValue(eventName, out var handlerTypes))
        {
            _logger.LogWarning("No handler registered for event {EventName}", eventName);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var eventType = _eventTypes.FirstOrDefault(t => t.Name == eventName);
        if (eventType == null) return;

        var integrationEvent = JsonSerializer.Deserialize(message, eventType);
        if (integrationEvent == null) return;

        foreach (var handlerType in handlerTypes)
        {
            var handler = scope.ServiceProvider.GetService(handlerType);
            if (handler == null) continue;

            var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
            var handleMethod = concreteType.GetMethod("Handle");
            if (handleMethod != null)
            {
                var task = (Task?)handleMethod.Invoke(handler, new[] { integrationEvent });
                if (task != null) await task;
            }
        }
    }

    public void Dispose()
    {
        _consumerChannel?.Dispose();
    }
}
