using Newtonsoft.Json;

namespace medmondson
{
	public class SchemaMember
	{
		public Schema.MemberName Name { get; set; } = (Schema.MemberName)string.Empty;
		[JsonProperty(ReferenceLoopHandling = ReferenceLoopHandling.Serialize)]
		public Schema.Types.BaseType Type { get; set; } = new Schema.Types.Null();
		public string Description { get; set; } = string.Empty;
	}

	public class SchemaMemberProperties
	{
		protected SchemaMember SchemaMember { get; }
		public SchemaMemberProperties(SchemaMember schemaMember) => SchemaMember = schemaMember;
	}
}