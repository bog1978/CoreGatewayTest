using System.Data.Common;
using System.Text;
using LinqToDB.Interceptors;

namespace CoreGateway.Dispatcher.DataAccess
{
    internal class LogCommandsInterceptor : CommandInterceptor
    {
        private readonly ILogger _logger;

        public LogCommandsInterceptor(ILogger logger) => _logger = logger;

        public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Выполняется команда типа: {command.CommandType}");
            foreach (DbParameter arg in command.Parameters)
                sb.AppendLine($"   {arg.ParameterName}: {arg.DbType}: {arg.Value}");
            sb.AppendLine(command.CommandText);
            _logger.LogInformation(sb.ToString());

            return base.CommandInitialized(eventData, command);
        }
    }
}
