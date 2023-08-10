namespace ktsu.io.SchemaTools
{
	public abstract class SchemaChild
	{
		public Schema? ParentSchema { get; private set; }
		public void AssosciateWith(Schema schema) => ParentSchema = schema;
	}

	public abstract class SchemaClassChild : SchemaChild
	{
		public SchemaClass? ParentClass { get; private set; }
		public void AssosciateWith(SchemaClass schemaClass)
		{
			ParentClass = schemaClass;
			if (schemaClass.ParentSchema is not null)
			{
				AssosciateWith(schemaClass.ParentSchema);
			}
		}
	}

	public abstract class SchemaMemberChild : SchemaClassChild
	{
		public SchemaMember? ParentMember { get; private set; }
		public void AssosciateWith(SchemaMember schemaMember) => ParentMember = schemaMember;
	}
}
