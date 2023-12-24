
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace ktsu.io.SchemaTools;
public class SchemaClass : SchemaChild<ClassName>
{
	[JsonPropertyName("Members")]
	private Collection<SchemaMember> MemberList { get; set; } = new();

	public IReadOnlyList<SchemaMember> Members => MemberList;

	public bool TryAddMember(MemberName memberName) => ParentSchema?.TryAddChild(memberName, MemberList) ?? throw new NotSupportedException("SchemaClass must be associated with a Schema before adding members");


	public bool TryGetMember(MemberName memberName, out SchemaMember? schemaMember) => Schema.TryGetChild(memberName, MemberList, out schemaMember);

	public void RemoveMember(SchemaMember schemaMember) => Schema.RemoveChild(schemaMember, MemberList);
}
