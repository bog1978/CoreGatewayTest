namespace CoreGateway.Messages
{
    public record FileProcessedMessage(Guid Id, string? Error);
}
