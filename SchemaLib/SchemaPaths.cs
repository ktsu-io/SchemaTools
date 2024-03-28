#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;

using ktsu.io.StrongPaths;

public class SchemaPaths
{
	public RelativeDirectoryPath ProjectRootPath { get; set; } = new();
	public RelativeDirectoryPath? DataSourcePath { get; set; }
}
