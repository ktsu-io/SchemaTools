using System.Text.Json.Serialization;

namespace ktsu.io.SchemaTools
{
	public class SchemaClass : SchemaChild
	{
		[JsonInclude]
		public ClassName ClassName { get; private set; } = new();

		[JsonPropertyName("Members")]
		private List<SchemaMember> MemberList { get; set; } = new();

		public IReadOnlyList<SchemaMember> Members => MemberList;

		public bool TryAddMember(MemberName memberName)
		{

			if (!string.IsNullOrEmpty(memberName) && !MemberList.Any(m => m.MemberName == memberName))
			{
				SchemaMember schemaMember = new();
				schemaMember.Rename(memberName);
				schemaMember.AssosciateWith(this);
				MemberList.Add(schemaMember);
				return true;
			}

			return false;
		}

		public void Rename(ClassName className) => ClassName = className;

		public bool TryGetMember(MemberName? memberName, out SchemaMember? schemaMember)
		{
			schemaMember = MemberList.FirstOrDefault(c => c.MemberName == memberName);
			return schemaMember != null;
		}

		public void RemoveMember(SchemaMember schemaMember) => MemberList.Remove(schemaMember);
	}
}