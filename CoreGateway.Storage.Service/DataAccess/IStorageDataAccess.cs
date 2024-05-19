using CoreGateway.Storage.Service.DbModel;

namespace CoreGateway.Dispatcher.DataAccess
{
    public interface IStorageDataAccess
    {
        Task<FileDatum> InsertFile(Guid id, string fileName, byte[] data, CancellationToken token);
        Task<FileDatum?> GetFile(Guid id, CancellationToken token);
        void PrepareDatabase();
    }
}