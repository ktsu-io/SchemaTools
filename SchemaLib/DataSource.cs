using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ktsu.io
{
	public class DataSource
	{
		public const string FileSuffix = ".json";
		public string SchemaPath { get; set; } = string.Empty;
		public SchemaMember RootSchemaMember { get; set; } = new SchemaMember();
		public JToken? Root { get; set; }
		[JsonIgnore]
		public Schema Schema { get; private set; } = new();
		[JsonIgnore]
		public string Name { get; private set; } = string.Empty;
		[JsonIgnore]
		public string FilePath { get; set; } = string.Empty;
		[JsonIgnore]
		public string FileName { get; private set; } = string.Empty;

		private static JsonSerializer JsonSerializer { get; } = JsonSerializer.Create(new JsonSerializerSettings()
		{
			Formatting = Formatting.Indented,
		});

		public static DataSource Load(string filePath)
		{
			DataSource dataSource = new();

			try
			{
				using var streamReader = File.OpenText(filePath);
				JsonSerializer.Populate(streamReader, dataSource);
			}
			catch (FileNotFoundException) { }

			string dataFileName = Path.GetFileName(filePath);
			dataSource.Name = Schema.MakePascalCase(dataFileName.Replace(FileSuffix, string.Empty));
			dataSource.FilePath = filePath;
			dataSource.FileName = dataFileName;
			dataSource.RootSchemaMember.Name = (Schema.MemberName)"Root";
			dataSource.LoadSchema(dataSource.SchemaPath);
			return dataSource;
		}

		public static DataSource New()
		{
			DataSource dataSource = new();
			dataSource.RootSchemaMember.Name = (Schema.MemberName)"Root";
			return dataSource;
		}

		public void Save()
		{
			using var fileStream = File.OpenWrite(FilePath);
			using var streamWriter = new StreamWriter(fileStream);
			JsonSerializer.Serialize(streamWriter, this);
		}

		public void LoadSchema(string path)
		{
			SchemaPath = path;
			string? dirName = Path.GetDirectoryName(FilePath);
			if (!string.IsNullOrEmpty(dirName))
			{
				string absoluteSchemaPath = Path.GetFullPath(Path.Combine(dirName, SchemaPath));
				if (Schema.TryLoad(absoluteSchemaPath, out var schema) && schema != null)
				{
					Schema = schema;
				}
			}
		}
	}
}
