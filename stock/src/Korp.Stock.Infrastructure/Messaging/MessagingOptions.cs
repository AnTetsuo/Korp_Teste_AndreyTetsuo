namespace Infrastructure.Messaging;

internal sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string User { get; init; } = "guest";
    public string Password { get; init; } = "guest";
}

internal static class MessagingConstants
{
    public const string MessageStoreSchema = "stock_messaging";

    public const string OperationQueue = "stock-operation";
}
