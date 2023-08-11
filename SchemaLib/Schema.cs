using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ktsu.io.StrongPaths;
using ktsu.io.StrongStrings;

namespace ktsu.io.SchemaTools
{
	public partial class Schema
	{
		#region FilePaths
		public const string FileSuffix = ".schema.json";

		public FilePath FilePath { get; private set; } = new();
		public DirectoryPath DataSourcePath { get; set; } = new();
		#endregion

		#region Serializable Properties
		[JsonInclude]
		public List<SchemaClass> Classes { get; init; } = new();

		[JsonInclude]
		public List<SchemaEnum> Enums { get; init; } = new();

		#endregion

		public static JsonSerializerOptions JsonSerializerOptions { get; } = new(JsonSerializerDefaults.General)
		{
			WriteIndented = true,
			DefaultIgnoreCondition = JsonIgnoreCondition.Never,
			IgnoreReadOnlyFields = true,
			IgnoreReadOnlyProperties = true,
			IncludeFields = true,
			Converters = 
			{
				new JsonStringEnumConverter(),
				//new StrongPathJsonConvertorFactory(),
				new StrongStringJsonConvertorFactory(),
			}
		};

		public static bool TryLoad(FilePath filePath, out Schema? schema)
		{
			schema = null;

			if (!string.IsNullOrEmpty(filePath))
			{
				try
				{
					schema = JsonSerializer.Deserialize<Schema>(File.ReadAllBytes(filePath), JsonSerializerOptions);
					if (schema != null)
					{
						schema.FilePath = filePath;
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

		public bool TryAddClass(ClassName className)
		{
			if (!string.IsNullOrEmpty(className) && !Classes.Any(c => c.ClassName == className))
			{
				SchemaClass schemaClass = new();
				schemaClass.Rename(className);
				schemaClass.AssosciateWith(this);
				Classes.Add(schemaClass);
				return true;
			}

			return false;
		}

		public bool TryGetClass(ClassName? className, out SchemaClass? schemaClass)
		{
			schemaClass = Classes.FirstOrDefault(c => c.ClassName == className);
			return schemaClass != null;
		}

		public void RemoveClass(SchemaClass schemaClass) => Classes.Remove(schemaClass);

		public void ChangeFilePath(FilePath newFilePath) => FilePath = newFilePath;

		public bool TryAddEnum(EnumName enumName)
		{
			if (!string.IsNullOrEmpty(enumName) && !Enums.Any(e => e.EnumName == enumName))
			{
				SchemaEnum schemaEnum = new();
				schemaEnum.Rename(enumName);
				schemaEnum.AssosciateWith(this);
				Enums.Add(schemaEnum);

				return true;
			}

			return false;
		}

		public SchemaClass? GetFirstClass() => Classes.FirstOrDefault();
		public SchemaClass? GetLastClass() => Classes.LastOrDefault();
	}
}