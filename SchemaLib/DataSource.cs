using System.Text.Json;
using System.Text.Json.Nodes;
using ktsu.io.StrongPaths;

namespace ktsu.io.SchemaTools
{
	public class DataSource
	{
		public const string FileSuffix = ".json";
		public FilePath SchemaPath { get; private set; } = new();
		public RootSchemaMember RootSchemaMember { get; set; } = new();
		public JsonNode? Root { get; set; }
		public Schema Schema { get; private set; } = new();
		public DataSourceName Name { get; private set; } = new();
		public FilePath FilePath { get; private set; } = new();
		public FileName FileName { get; private set; } = new();

		public static bool TryLoad(FilePath filePath, out DataSource? dataSource)
		{
			dataSource = null;

			if (!string.IsNullOrEmpty(filePath))
			{
				try
				{
					Utf8JsonReader reader = new(File.ReadAllBytes(filePath));
					dataSource = JsonSerializer.Deserialize<DataSource>(ref reader, Schema.JsonSerializerOptions);
					if (dataSource != null)
					{
						var fileName = (FileName)Path.GetFileName(filePath);
						dataSource.FilePath = filePath;
						dataSource.FileName = fileName;
						dataSource.Name = (DataSourceName)Schema.MakePascalCase(fileName.RemoveSuffix(FileSuffix));
						dataSource.LoadSchema(dataSource.SchemaPath);

						//TODO: walk every class and tell each member which class they belong to
					}
				}
				catch (FileNotFoundException)
				{
					//TODO: throw an error popup because the file has dissappeared
				}
				catch (JsonException)
				{
					//TODO: throw an error popup because the file is not well formed
				}
			}

			return dataSource != null;
		}

		public void Save()
		{
			byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(this, Schema.JsonSerializerOptions);
			File.WriteAllBytes(FilePath, bytes);
		}

		public void LoadSchema(FilePath path)
		{
			SchemaPath = path;
			string? dirName = Path.GetDirectoryName(FilePath);
			if (!string.IsNullOrEmpty(dirName))
			{
				var absoluteSchemaPath = (FilePath)Path.GetFullPath(Path.Combine(dirName, SchemaPath));
				if (Schema.TryLoad(absoluteSchemaPath, out var schema))
				{
					Schema = schema!;
				}
			}
		}

		public void ChangeFilePath(FilePath newFilePath) => FilePath = newFilePath;

	}
}
