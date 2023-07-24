namespace medmondson
{
	public class SchemaClass
	{
		public Schema.ClassName Name { get; set; } = (Schema.ClassName)string.Empty;
		public List<SchemaMember> Members { get; set; } = new List<SchemaMember>();

		public bool TryGetMember(Schema.MemberName? memberName, out SchemaMember? schemaMember)
		{
			schemaMember = Members.FirstOrDefault(c => c.Name == memberName);
			return schemaMember != null;
		}
	}
}