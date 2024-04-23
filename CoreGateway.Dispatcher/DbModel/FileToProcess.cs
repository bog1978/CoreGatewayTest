// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;
using System;

#pragma warning disable 1573, 1591
#nullable enable

namespace CoreGateway.Dispatcher.DbModel
{
	[Table("file_to_process")]
	public class FileToProcess
	{
		[Column("id"        , IsPrimaryKey = true )] public Guid     Id        { get; set; } // uuid
		[Column("name"      , CanBeNull    = false)] public string   Name      { get; set; } = null!; // text
		[Column("status"    , CanBeNull    = false)] public string   Status    { get; set; } = null!; // character varying(10)
		[Column("try_count"                       )] public int      TryCount  { get; set; } // integer
		[Column("created_at"                      )] public DateTime CreatedAt { get; set; } // timestamp (6) without time zone
		[Column("try_after"                       )] public DateTime TryAfter  { get; set; } // timestamp (6) without time zone
		[Column("errors"    , CanBeNull    = false)] public string[] Errors    { get; set; } = null!; // text[]
	}
}
