using CoreGateway.Dispatcher.DbModel;

namespace CoreGateway.Dispatcher.DataAccess
{
    internal interface IDispatcherDataAccess
    {
        Task<FileToProcess> InsertFileToProcess(string fileName, CancellationToken token);
        void PrepareDatabase();
        Task<FileToProcess?> DeferFileToProcess(Guid id, string error);
        Task<DateTime> GetServerTime();
        Task<FileToProcess?> FindFileToProcess(string fileName, CancellationToken token);
        Task CompleteFileToProcess(Guid id);
        Task<FileToProcess> ResendFileToProcess(Guid id);
    }
}