#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;

using ktsu.io.StrongPaths;

public class SchemaPaths
{
	public RelativeDirectoryPath RelativeProjectRootPath { get; set; } = new();
	public RelativeDirectoryPath RelativeDataSourcePath { get; set; } = new();
}
