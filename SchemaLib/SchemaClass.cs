#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

public class SchemaClass : SchemaChild<ClassName>
{
	[JsonPropertyName("Members")]
	private Collection<SchemaMember> MemberCollection { get; set; } = new();
	[JsonIgnore]
	public IReadOnlyCollection<SchemaMember> Members => MemberCollection;

	public SchemaMember? AddMember(MemberName memberName) => ParentSchema?.AddChild(memberName, MemberCollection) ?? throw new NotSupportedException("SchemaClass must be associated with a Schema before adding members");
	public bool TryAddMember(MemberName memberName) => AddMember(memberName) is not null;


	public bool TryGetMember(MemberName memberName, out SchemaMember? schemaMember) => Schema.TryGetChild(memberName, MemberCollection, out schemaMember);

	public bool TryRemoveMember(SchemaMember schemaMember) => Schema.TryRemoveChild(schemaMember, MemberCollection);

	public bool TryRemove() => ParentSchema?.TryRemoveClass(this) ?? false;
}
