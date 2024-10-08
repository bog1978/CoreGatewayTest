// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB;
using LinqToDB.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1573, 1591
#nullable enable

namespace CoreGateway.Dispatcher.DbModel
{
	public partial class CoreGatewayDb : DataConnection
	{
		public CoreGatewayDb()
		{
			InitDataContext();
		}

		public CoreGatewayDb(string configuration)
			: base(configuration)
		{
			InitDataContext();
		}

		public CoreGatewayDb(DataOptions<CoreGatewayDb> options)
			: base(options.Options)
		{
			InitDataContext();
		}

		partial void InitDataContext();

		public ITable<FileToProcess>        FileToProcesses        => this.GetTable<FileToProcess>();
		public ITable<FileToProcessHistory> FileToProcessHistories => this.GetTable<FileToProcessHistory>();
	}

	public static partial class ExtensionMethods
	{
		#region Table Extensions
		public static FileToProcess? Find(this ITable<FileToProcess> table, Guid id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<FileToProcess?> FindAsync(this ITable<FileToProcess> table, Guid id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}
		#endregion
	}
}
