#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

public class SchemaClass : SchemaChild<ClassName>
{
	[JsonInclude]
	private Collection<SchemaMember> Members { get; set; } = [];

	public IReadOnlyCollection<SchemaMember> GetMembers() => Members;

	public SchemaMember? AddMember(MemberName memberName) => ParentSchema?.AddChild(memberName, Members) ?? throw new NotSupportedException("SchemaClass must be associated with a Schema before adding members");
	public bool TryAddMember(MemberName memberName) => AddMember(memberName) is not null;


	public bool TryGetMember(MemberName memberName, out SchemaMember? schemaMember) => Schema.TryGetChild(memberName, Members, out schemaMember);

	public bool TryRemoveMember(SchemaMember schemaMember) => Schema.TryRemoveChild(schemaMember, Members);

	public override bool TryRemove() => ParentSchema?.TryRemoveClass(this) ?? false;

	public override string Summary() => $"{Name} ({Members.Count})";
}
