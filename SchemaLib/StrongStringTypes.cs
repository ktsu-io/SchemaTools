#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaLib;

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
