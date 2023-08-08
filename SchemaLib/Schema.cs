using System.Text.Json;
using System.Globalization;

namespace ktsu.io
{
	public partial class Schema
	{
		public const string FileSuffix = ".schema.json";
		public string FilePath { get; set; } = string.Empty;
		public List<SchemaClass> Classes { get; set; } = new();
		public Dictionary<EnumName, HashSet<EnumValueName>> Enums { get; set; } = new();

		public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
		{
			WriteIndented = true,
		};

		public static bool TryLoad(string filePath, out Schema? schema)
		{
			schema = null;

			if (!string.IsNullOrEmpty(filePath))
			{
				try
				{
					string jsonString = File.ReadAllText(filePath);
					schema = JsonSerializer.Deserialize<Schema>(jsonString);
					if (schema != null)
					{
						schema.FilePath = filePath;

						// TODO: walk every class and tell each member which class they belong to
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

			return schema != null;
		}

		public static void EnsureDirectoryExists(string path)
		{
			string? dirName = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(dirName))
			{
				Directory.CreateDirectory(dirName);
			}
		}

		public void Save()
		{
			EnsureDirectoryExists(FilePath);

			string jsonString = JsonSerializer.Serialize(this, JsonSerializerOptions);

			//TODO: hoist this out to some static method called something like WriteTextSafely
			string tmpFilePath = $"{FilePath}.tmp";
			string bkFilePath = $"{FilePath}.bk";
			File.Delete(tmpFilePath);
			File.Delete(bkFilePath);
			File.WriteAllText(tmpFilePath, jsonString);
			try
			{
				File.Move(FilePath, bkFilePath);
			}
			catch (FileNotFoundException) { }
			File.Move(tmpFilePath, FilePath);
			File.Delete(bkFilePath);
		}

		public static string MakePascalCase(string input)
		{
			input = input.Replace("_", " ");
			var textInfo = new CultureInfo("en-US", false).TextInfo;
			input = textInfo.ToTitleCase(input);
			input = input.Replace(" ", string.Empty);
			return input;
		}

		public static string LowerCaseFirst(string str) => str[..1].ToLower() + str[1..];

		public bool TryGetClass(ClassName? className, out SchemaClass? schemaClass)
		{
			schemaClass = Classes.FirstOrDefault(c => c.Name == className);
			return schemaClass != null;
		}
	}
}