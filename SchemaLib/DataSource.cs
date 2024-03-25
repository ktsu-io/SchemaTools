namespace ktsu.io.SchemaTools;

using ktsu.io.StrongPaths;

public class DataSource : SchemaChild<DataSourceName>
{
	public const string FileSuffix = ".data.json";
	public RootSchemaMember RootSchemaMember { get; set; } = new();
	public FilePath FilePath { get; private set; } = new();
}
