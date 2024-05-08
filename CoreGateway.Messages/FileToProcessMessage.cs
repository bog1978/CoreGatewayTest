namespace CoreGateway.Messages
{
    public record FileToProcessMessage(Guid MessageId, string FilePath);
}
