namespace CoreGateway.Messages
{
    public record FileToProcessMessage(Guid Id, string FilePath);
}
