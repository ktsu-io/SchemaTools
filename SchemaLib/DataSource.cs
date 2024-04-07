#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;

using ktsu.io.StrongPaths;


public class DataSource : SchemaChild<DataSourceName>
{
	public const string FileSuffix = ".data.json";
	public RootSchemaMember RootSchemaMember { get; set; } = new();
	public FilePath FilePath { get; private set; } = new();

	public override bool TryRemove() => throw new NotImplementedException();
}
