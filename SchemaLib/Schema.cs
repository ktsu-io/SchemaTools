using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;

namespace ktsu.io
{
	public partial class Schema
	{
		public const string FileSuffix = ".schema.json";
		public string FilePath { get; set; } = string.Empty;
		[JsonProperty(nameof(Classes))]
		public List<SchemaClass> LocalClasses { get; set; } = new();

		[JsonProperty(nameof(Enums))]
		public Dictionary<EnumName, HashSet<EnumValueName>> LocalEnums { get; set; } = new();

		public List<string> References { get; set; } = new();
		[JsonIgnore]
		public List<SchemaClass> ReferencedClasses { get; set; } = new();
		[JsonIgnore]
		public Dictionary<EnumName, HashSet<EnumValueName>> ReferencedEnums { get; set; } = new();
		[JsonIgnore]
		public IEnumerable<SchemaClass> Classes => LocalClasses.Concat(ReferencedClasses);
		[JsonIgnore]
		public IDictionary<EnumName, HashSet<EnumValueName>> Enums => LocalEnums.Concat(ReferencedEnums).ToDictionary(e => e.Key, e => e.Value);
		public static JsonSerializerSettings JsonSerializerSettings { get; } = new()
		{
			Formatting = Formatting.Indented,
			Converters = new[] { new StringEnumConverter() },
		};
		public static JsonSerializer JsonSerializer { get; } = JsonSerializer.CreateDefault(JsonSerializerSettings);

		public static bool TryLoad(string filePath, out Schema? schema)
		{
			schema = null;

			if (!string.IsNullOrEmpty(filePath))
			{
				try
				{
					using var streamReader = File.OpenText(filePath);
					using var jsonReader = new JsonTextReader(streamReader);
					schema = JsonSerializer.Deserialize<Schema>(jsonReader);
					if (schema != null)
					{
						schema.FilePath = filePath;
						schema.LoadReferences();

						// TODO: walk every class and tell each member which class they belong to
					}
				}
				catch (FileNotFoundException)
				{
					//TODO: throw an error popup because the file has dissappeared
				}
				catch (JsonSerializationException)
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

			using var fileStream = File.OpenWrite(FilePath);
			using var streamWriter = new StreamWriter(fileStream);
			JsonSerializer.Serialize(streamWriter, this);
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

		public void LoadReferences()
		{
			ReferencedClasses.Clear();
			ReferencedEnums.Clear();
			var loadedFiles = new HashSet<string>
			{
				Path.GetFullPath(FilePath),
			};
			LoadReferencesInternal(this, loadedFiles);
		}

		private void LoadReferencesInternal(Schema schema, HashSet<string> loadedFiles)
		{
			foreach (string referencePath in schema.References)
			{
				string? dirName = Path.GetDirectoryName(schema.FilePath);
				if (dirName != null)
				{
					string absolutePath = Path.Combine(dirName, referencePath);
					if (loadedFiles.Add(Path.GetFullPath(absolutePath)))
					{
						if (TryLoad(absolutePath, out var otherSchema) && otherSchema != null)
						{
							foreach (var schemaClass in otherSchema.LocalClasses)
							{
								ReferencedClasses.Add(schemaClass);
							}

							foreach (var kvp in otherSchema.LocalEnums)
							{
								ReferencedEnums.Add(kvp.Key, kvp.Value);
							}

							LoadReferencesInternal(otherSchema, loadedFiles);
						}
					}
				}
			}
		}

		public bool TryGetClass(ClassName? className, out SchemaClass? schemaClass)
		{
			schemaClass = Classes.FirstOrDefault(c => c.Name == className);
			return schemaClass != null;
		}
	}
}