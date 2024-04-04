#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ktsu.io.StrongPaths;
using ktsu.io.StrongStrings;
using ktsu.io.StringifyJsonConvertorFactory;
using System.Reflection;

public partial class Schema
{
	#region FilePaths
	public static FileExtension FileExtension { get; } = (FileExtension)".schema.json";
	public AbsoluteFilePath FilePath { get; private set; } = new();
	[JsonInclude]
	public SchemaPaths RelativePaths { get; init; } = new();
	[JsonIgnore]
	public AbsoluteDirectoryPath ProjectRootPath => FilePath.DirectoryPath / RelativePaths.RelativeProjectRootPath;
	[JsonIgnore]
	public AbsoluteDirectoryPath DataSourcePath => FilePath.DirectoryPath / RelativePaths.RelativeProjectRootPath;
	#endregion

	#region SchemaChildren
	[JsonPropertyName("Classes")]
	private Collection<SchemaClass> ClassCollection { get; set; } = [];
	[JsonIgnore]
	public IReadOnlyCollection<SchemaClass> Classes => ClassCollection;

	[JsonPropertyName("Enums")]
	private Collection<SchemaEnum> EnumCollection { get; set; } = [];
	[JsonIgnore]
	public IReadOnlyCollection<SchemaEnum> Enums => EnumCollection;

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
			new StringifyJsonConvertorFactory(),
		}
	};

	public static bool TryLoad(AbsoluteFilePath filePath, out Schema? schema)
	{
		schema = null;

		if (!string.IsNullOrEmpty(filePath))
		{
			try
			{
				schema = JsonSerializer.Deserialize<Schema>(File.ReadAllBytes(filePath), JsonSerializerOptions);
				if (schema is not null)
				{
					schema.FilePath = filePath;

					// Walk every class and tell each member which class they belong to
					schema.Reassosciate();
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

		return schema is not null;
	}

	private void Reassosciate()
	{
		foreach (var schemaClass in Classes)
		{
			schemaClass.AssosciateWith(this);
			foreach (var member in schemaClass.Members)
			{
				member.AssosciateWith(schemaClass);
				member.Type.AssosciateWith(member);
			}
		}

		foreach (var schemaEnum in Enums)
		{
			schemaEnum.AssosciateWith(this);
		}
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

	public void ChangeFilePath(AbsoluteFilePath newFilePath) => FilePath = newFilePath;

	public static bool TryRemoveChild<TChild>(TChild child, Collection<TChild> collection)
	{
		ArgumentNullException.ThrowIfNull(child);
		ArgumentNullException.ThrowIfNull(collection);

		return collection.Remove(child);
	}

	public bool TryRemoveEnum(SchemaEnum schemaEnum) => schemaEnum?.ParentSchema?.EnumCollection.Remove(schemaEnum) ?? false;
	public bool TryRemoveClass(SchemaClass schemaClass) => schemaClass?.ParentSchema?.ClassCollection.Remove(schemaClass) ?? false;

	public static TChild? GetChild<TName, TChild>(TName name, Collection<TChild> collection) where TChild : SchemaChild<TName>, new() where TName : AnyStrongString, new()
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(collection);

		return collection.FirstOrDefault(c => c.Name == name);
	}

	public static bool TryGetChild<TName, TChild>(TName name, Collection<TChild> collection, out TChild? child) where TChild : SchemaChild<TName>, new() where TName : AnyStrongString, new()
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(collection);

		child = null;
		if (name.IsEmpty())
		{
			return false;
		}

		child = collection.FirstOrDefault(c => c.Name == name);
		return child is not null;
	}

	public bool TryGetEnum(EnumName name, out SchemaEnum? schemaEnum) => TryGetChild(name, EnumCollection, out schemaEnum);
	public bool TryGetClass(ClassName name, out SchemaClass? schemaClass) => TryGetChild(name, ClassCollection, out schemaClass);

	public SchemaEnum? GetEnum(EnumName name) => GetChild(name, EnumCollection);
	public SchemaClass? GetClass(ClassName name) => GetChild(name, ClassCollection);


	public TChild? AddChild<TChild, TName>(TName name, Collection<TChild> collection) where TChild : SchemaChild<TName>, new() where TName : AnyStrongString, new()
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(collection);

		if (!string.IsNullOrEmpty(name) && !collection.Any(c => c.Name == name))
		{
			TChild newChild = new();
			newChild.Rename(name);
			newChild.AssosciateWith(this);
			collection.Add(newChild);

			return newChild;
		}

		return null;
	}

	public bool TryAddChild<TChild, TName>(TName name, Collection<TChild> collection) where TChild : SchemaChild<TName>, new() where TName : AnyStrongString, new() =>
		AddChild(name, collection) is not null;

	public bool TryAddEnum(EnumName name) => TryAddChild(name, EnumCollection);
	public bool TryAddClass(ClassName name) => TryAddChild(name, ClassCollection);
	public SchemaEnum? AddEnum(EnumName name) => AddChild(name, EnumCollection);
	public SchemaClass? AddClass(ClassName name) => AddChild(name, ClassCollection);

	public bool TryAddClass(Type type) => AddClass(type) is not null;

	public SchemaClass? AddClass(Type type)
	{
		if (type is not null)
		{
			SchemaClass newClass = new();
			newClass.Rename((ClassName)type.Name);
			newClass.AssosciateWith(this);
			foreach (var member in type.GetMembers())
			{
				var memberType = member switch
				{
					PropertyInfo propertyInfo => propertyInfo.PropertyType,
					FieldInfo fieldInfo => fieldInfo.FieldType,
					_ => null
				};

				if (memberType is not null)
				{
					var schemaType = GetOrCreateSchemaType(memberType);
					if (schemaType is not null)
					{
						var newMember = newClass.AddMember((MemberName)member.Name);
						newMember?.SetType(schemaType);
					}
				}
			}

			ClassCollection.Add(newClass);

			return newClass;
		}

		return null;
	}

	private Types.BaseType? GetOrCreateSchemaType(Type type)
	{
		bool isEnumerable = type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
		if (type.IsArray || isEnumerable)
		{
			var elementType = type.HasElementType ? type.GetElementType() : type.GetGenericArguments().LastOrDefault();
			if (elementType is not null)
			{
				var schemaType = GetOrCreateSchemaType(elementType);
				if (schemaType is not null)
				{
					return new Types.Array { ElementType = schemaType };
				}
			}
		}
		else if (type.IsEnum)
		{
			var enumName = (EnumName)type.Name;
			if (!TryGetEnum(enumName, out var schemaEnum))
			{
				schemaEnum = AddEnum(enumName);
				foreach (string name in Enum.GetNames(type))
				{
					schemaEnum?.TryAddValue((EnumValueName)name);
				}
			}

			if (schemaEnum is not null)
			{
				return new Types.Enum { EnumName = schemaEnum.Name };
			}
		}
		else if (type.IsPrimitive || type.FullName == "System.String")
		{
			string typeName = type.Name switch
			{
				"Int32" => "Int",
				"Int64" => "Long",
				"Single" => "Float",
				"Double" => "Double",
				"String" => "String",
				"DateTime" => "DateTime",// ?
				"TimeSpan" => "TimeSpan",// ?
				"Boolean" => "Bool",
				_ => "",
			};

			return Types.BaseType.CreateFromString(typeName) as Types.BaseType;
		}
		else if (type.IsClass)
		{
			if (!TryGetClass((ClassName)type.Name, out var memberClass))
			{
				memberClass = AddClass(type);
			}

			if (memberClass is not null)
			{
				return new Types.Object { ClassName = memberClass.Name };
			}
		}
		return null;
	}

	public SchemaClass? GetFirstClass() => Classes.FirstOrDefault();
	public SchemaClass? GetLastClass() => Classes.LastOrDefault();
}
