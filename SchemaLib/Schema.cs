
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ktsu.io.StrongPaths;
using ktsu.io.StrongStrings;

namespace ktsu.io.SchemaTools;
#pragma warning disable CA1724 // "Type names should not match namespaces", you don't own the word "Schema" Microsoft
public partial class Schema
#pragma warning restore CA1724 // "Type names should not match namespaces"
{
	#region FilePaths
	public const string FileSuffix = ".schema.json";

	public FilePath FilePath { get; private set; } = new();
	public DirectoryPath? DataSourcePath { get; set; }
	#endregion

	#region Serializable Properties
	[JsonInclude]
	public Collection<SchemaClass> Classes { get; init; } = new();

	[JsonInclude]
	public Collection<SchemaEnum> Enums { get; init; } = new();

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

	public void ChangeFilePath(FilePath newFilePath) => FilePath = newFilePath;

	public static void RemoveChild<TChild>(TChild child, Collection<TChild> collection)
	{
		ArgumentNullException.ThrowIfNull(child);
		ArgumentNullException.ThrowIfNull(collection);

		collection.Remove(child);
	}

	public static void RemoveEnum(SchemaEnum schemaEnum) => RemoveChild(schemaEnum, schemaEnum?.ParentSchema?.Enums ?? throw new InvalidOperationException("Cannot remove an enum that is not assosciated with a schema"));
	public static void RemoveClass(SchemaClass schemaClass) => RemoveChild(schemaClass, schemaClass?.ParentSchema?.Classes ?? throw new InvalidOperationException("Cannot remove a class that is not assosciated with a schema"));
	
	public static bool TryGetChild<TName, TChild>(TName name, Collection<TChild> collection, out TChild? child) where TChild : SchemaChild<TName>, new() where TName : AnyStrongString, new()
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(collection);

		child = collection.FirstOrDefault(c => c.Name == name);
		return child != null;
	}

	public bool TryGetEnum(EnumName name, out SchemaEnum? schemaEnum) => TryGetChild(name, Enums, out schemaEnum);
	public bool TryGetClass(ClassName name, out SchemaClass? schemaClass) => TryGetChild(name, Classes, out schemaClass);


	public bool TryAddChild<TChild, TName>(TName name, Collection<TChild> collection) where TChild : SchemaChild<TName>, new() where TName : AnyStrongString, new()
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(collection);

		if (!string.IsNullOrEmpty(name) && !collection.Any(c => c.Name == name))
		{
			TChild newChild = new();
			newChild.Rename(name);
			newChild.AssosciateWith(this);
			collection.Add(newChild);

			return true;
		}

		return false;
	}

	public bool TryAddEnum(EnumName name) => TryAddChild(name, Enums);
	public bool TryAddClass(ClassName name) => TryAddChild(name, Classes);

	public SchemaClass? GetFirstClass() => Classes.FirstOrDefault();
	public SchemaClass? GetLastClass() => Classes.LastOrDefault();
}
