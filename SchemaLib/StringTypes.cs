using ktsu.io.StrongStrings;
using ktsu.io.StrongPaths;
using System.Text.Json.Serialization;

namespace ktsu.io.SchemaTools
{
	public record class ClassName : StrongStringAbstract<ClassName> { }
	public record class MemberName : StrongStringAbstract<MemberName> { }
	public record class EnumName : StrongStringAbstract<EnumName> { }
	public record class EnumValueName : StrongStringAbstract<EnumValueName> { }
	public record class ContainerName : StrongStringAbstract<ContainerName>
	{
		public static ContainerName Vector { get; } = (ContainerName)"vector";
	}

	public record class DataSourceName : StrongStringAbstract<DataSourceName> { }

	/// <summary>
	/// A FilePath that is relative to the schema file
	/// </summary>
	public record class SchemaRelativeFilePath : RelativePathAbstract<SchemaRelativeFilePath>
	{
		public static SchemaRelativeFilePath Make(Schema schema, AnyFilePath to) => Make<SchemaRelativeFilePath>(schema.FilePath, to);
	}
}
