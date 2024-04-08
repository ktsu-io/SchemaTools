#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaLib;

using ktsu.io.StrongPaths;
using ktsu.io.StrongStrings;

public sealed record class BaseTypeName : StrongStringAbstract<BaseTypeName> { }
public sealed record class ClassName : StrongStringAbstract<ClassName> { }
public sealed record class MemberName : StrongStringAbstract<MemberName> { }
public sealed record class EnumName : StrongStringAbstract<EnumName> { }
public sealed record class EnumValueName : StrongStringAbstract<EnumValueName> { }
public sealed record class ContainerName : StrongStringAbstract<ContainerName>
{
	public static ContainerName Vector { get; } = (ContainerName)"vector";
}

public sealed record class DataSourceName : StrongStringAbstract<DataSourceName> { }

/// <summary>
/// A FilePath that is relative to the schema file
/// </summary>
public sealed record class SchemaRelativeFilePath : RelativePathAbstract<SchemaRelativeFilePath>
{
	public static SchemaRelativeFilePath Make(Schema schema, AnyFilePath to)
	{
		ArgumentNullException.ThrowIfNull(schema);
		ArgumentNullException.ThrowIfNull(to);

		return Make<SchemaRelativeFilePath>(schema.FilePath, to);
	}
}
