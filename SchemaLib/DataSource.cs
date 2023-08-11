namespace ktsu.io.SchemaTools;
using ktsu.io.StrongPaths;

public class DataSource : SchemaChild
{
	public const string FileSuffix = ".data.json";
	public RootSchemaMember RootSchemaMember { get; set; } = new();
	public DataSourceName Name { get; private set; } = new();
	public FilePath FilePath { get; private set; } = new();
}
