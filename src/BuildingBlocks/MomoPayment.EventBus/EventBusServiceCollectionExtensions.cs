using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MomoPayment.EventBus;

public static class EventBusServiceCollectionExtensions
{
    public static IServiceCollection AddMomoEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitOptions = new RabbitMqOptions();
        configuration.GetSection(RabbitMqOptions.SectionName).Bind(rabbitOptions);

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddSingleton<IRabbitMqConnection>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<DefaultRabbitMqConnection>>();
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>();

            var factory = new ConnectionFactory
            {
                HostName = options.Value.HostName,
                Port = options.Value.Port,
                UserName = options.Value.UserName,
                Password = options.Value.Password,
                VirtualHost = options.Value.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true
            };

            return new DefaultRabbitMqConnection(factory, options, logger);
        });

        services.AddSingleton<IEventBus, EventBusRabbitMQ>();

        return services;
    }
}
