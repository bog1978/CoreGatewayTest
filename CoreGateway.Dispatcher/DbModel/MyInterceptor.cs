using System.Data.Common;
using LinqToDB.Interceptors;

namespace CoreGateway.Dispatcher.DbModel
{
    internal class MyInterceptor : CommandInterceptor
    {
        private readonly ILogger _logger;

        public MyInterceptor(ILogger logger) => _logger = logger;

        public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
        {
            var text = command.CommandText;
            _logger.InterpolatedInformation($"{text}");
            return base.CommandInitialized(eventData, command);
        }
    }
}
