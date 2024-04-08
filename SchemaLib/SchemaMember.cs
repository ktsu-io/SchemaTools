#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaLib;

using System.Text.Json.Serialization;

public class SchemaMember : SchemaClassChild<MemberName>
{
	[JsonInclude]
	public Schema.Types.BaseType Type { get; private set; } = new Schema.Types.None();
	public string Description { get; set; } = string.Empty;

	public void SetType(Schema.Types.BaseType type)
	{
		Type = type;
		Type.AssosciateWith(this);
	}

	public override bool TryRemove() => ParentClass?.TryRemoveMember(this) ?? false;
}

public class RootSchemaMember : SchemaMember
{
	private MemberName Root { get; } = (MemberName)nameof(Root);

	[JsonInclude]
	public new MemberName Name => Root;

	[Obsolete("Not supported on the root schema member", true)]
	public new void Rename(MemberName _) => throw new NotSupportedException("Not supported on the root schema member");
}
