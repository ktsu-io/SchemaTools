#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;
using System.Text.Json.Serialization;
using ktsu.io.StrongStrings;

public abstract class SchemaChild<TName> where TName : AnyStrongString, new()
{
	[JsonInclude]
	public TName Name { get; private set; } = new();

	public void Rename(TName name) => Name = name;

	public Schema? ParentSchema { get; private set; }
	public void AssosciateWith(Schema schema) => ParentSchema = schema;
}

public abstract class SchemaClassChild<TName> : SchemaChild<TName> where TName : AnyStrongString, new()
{
	public SchemaClass? ParentClass { get; private set; }
	public void AssosciateWith(SchemaClass schemaClass)
	{
		ArgumentNullException.ThrowIfNull(schemaClass);

		ParentClass = schemaClass;
		if (schemaClass.ParentSchema is not null)
		{
			AssosciateWith(schemaClass.ParentSchema);
		}
	}
}

public abstract class SchemaMemberChild<TName> : SchemaClassChild<TName> where TName : AnyStrongString, new()
{
	public SchemaMember? ParentMember { get; private set; }
	public void AssosciateWith(SchemaMember schemaMember) => ParentMember = schemaMember;
}
