namespace CoreGateway.Messages
{
    public record FileProcessedMessage(Guid MessageId, Guid? FileGuid, string? Error);
}
