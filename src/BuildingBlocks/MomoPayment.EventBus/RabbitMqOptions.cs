namespace MomoPayment.EventBus;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "momo.events.exchange";
    public string SubscriptionClientName { get; set; } = "momo_subscriber";
    public int RetryCount { get; set; } = 5;
}
