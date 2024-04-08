#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaLib;

using System.Diagnostics;
using ktsu.io.StrongPaths;


public class DataSource : SchemaChild<DataSourceName>
{
	public const string FileSuffix = ".data.json";
	public RootSchemaMember RootSchemaMember { get; set; } = new();
	public AbsoluteFilePath FilePath
	{
		get
		{
			Debug.Assert(ParentSchema is not null);
			return ParentSchema.DataSourcePath / (FileName)$"{Name}{FileSuffix}";
		}
	}

	public override bool TryRemove() => ParentSchema?.TryRemoveDataSource(this) ?? false;
}
