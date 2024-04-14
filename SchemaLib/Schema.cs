#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaLib;

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
	public static FileExtension FileExtension => (FileExtension)".schema.json";
	public AbsoluteFilePath FilePath { get; private set; } = new();
	[JsonInclude]
	public SchemaPaths RelativePaths { get; private set; } = new();
	[JsonIgnore]
	public AbsoluteDirectoryPath ProjectRootPath => FilePath.DirectoryPath / RelativePaths.RelativeProjectRootPath;
	[JsonIgnore]
	public AbsoluteDirectoryPath DataSourcePath => FilePath.DirectoryPath / RelativePaths.RelativeDataSourcePath;
	#endregion

	#region SchemaChildren
	[JsonInclude]
	private Collection<SchemaClass> Classes { get; set; } = [];
	public IReadOnlyCollection<SchemaClass> GetClasses() => Classes;

	[JsonInclude]
	private Collection<SchemaEnum> Enums { get; set; } = [];
	public IReadOnlyCollection<SchemaEnum> GetEnums() => Enums;

	[JsonInclude]
	private Collection<DataSource> DataSources { get; set; } = [];
	public IReadOnlyCollection<DataSource> GetDataSources() => DataSources;

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
		foreach (var schemaClass in GetClasses())
		{
			schemaClass.AssosciateWith(this);
			foreach (var member in schemaClass.GetMembers())
			{
				member.AssosciateWith(schemaClass);
				member.Type.AssosciateWith(member);
			}
		}

		foreach (var schemaEnum in GetEnums())
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

	public bool TryRemoveEnum(SchemaEnum schemaEnum) => schemaEnum?.ParentSchema?.Enums.Remove(schemaEnum) ?? false;
	public bool TryRemoveClass(SchemaClass schemaClass) => schemaClass?.ParentSchema?.Classes.Remove(schemaClass) ?? false;
	public bool TryRemoveDataSource(DataSource dataSource) => dataSource?.ParentSchema?.DataSources.Remove(dataSource) ?? false;

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

	public bool TryGetEnum(EnumName name, out SchemaEnum? schemaEnum) => TryGetChild(name, Enums, out schemaEnum);
	public bool TryGetClass(ClassName name, out SchemaClass? schemaClass) => TryGetChild(name, Classes, out schemaClass);

	public SchemaEnum? GetEnum(EnumName name) => GetChild(name, Enums);
	public SchemaClass? GetClass(ClassName name) => GetChild(name, Classes);
	public DataSource? GetDataSource(DataSourceName name) => GetChild(name, DataSources);

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

	public bool TryAddEnum(EnumName name) => TryAddChild(name, Enums);
	public bool TryAddClass(ClassName name) => TryAddChild(name, Classes);
	public bool TryAddDataSource(DataSourceName name) => TryAddChild(name, DataSources);
	public SchemaEnum? AddEnum(EnumName name) => AddChild(name, Enums);
	public SchemaClass? AddClass(ClassName name) => AddChild(name, Classes);
	public DataSource? AddDataSource(DataSourceName name) => AddChild(name, DataSources);

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

			Classes.Add(newClass);

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

	public SchemaClass? GetFirstClass() => GetClasses().FirstOrDefault();
	public SchemaClass? GetLastClass() => GetClasses().LastOrDefault();

	private IEnumerable<Types.BaseType> GetDiscreteTypes()
	{
		yield return new Types.Int();
		yield return new Types.Long();
		yield return new Types.Float();
		yield return new Types.Double();
		yield return new Types.String();
		yield return new Types.DateTime();
		yield return new Types.TimeSpan();
		yield return new Types.Bool();

		foreach (var schemaEnum in GetEnums())
		{
			yield return new Types.Enum { EnumName = schemaEnum.Name };
		}

		foreach (var schemaClass in GetClasses())
		{
			yield return new Types.Object { ClassName = schemaClass.Name };
		}
	}

	public IEnumerable<Types.BaseType> GetTypes() =>
		GetDiscreteTypes().Concat(GetDiscreteTypes().Select(t => new Types.Array { ElementType = t }));
}
