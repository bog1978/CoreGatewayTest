using Refit;

namespace CoreGateway.Storage.Client
{
    public interface ICoreGatewayStorage
    {
        [Get("/File")]
        Task FileGET([Query] Guid fileGuid);

        [Multipart]
        [Headers("Accept: text/plain, application/json, text/json")]
        [Put("/File")]
        Task<Guid> FilePUT(StreamPart data);
    }
}