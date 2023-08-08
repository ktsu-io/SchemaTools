using System.Text.Json.Serialization;
using System.Reflection;
using System.Text.Json;

namespace ktsu.io
{
	public class JsonConverters
	{
		public class AsString : JsonConverter<object>
		{
			private const object FromString = null; //placeholder for nameof()

			private static MethodInfo? GetFromStringMethod(Type type)
			{
				return type.GetMethod(nameof(FromString), BindingFlags.Public | BindingFlags.Static)
					?? type.GetMethod(nameof(FromString), BindingFlags.Public | BindingFlags.Instance);
			}
			public override bool CanConvert(Type objectType) => GetFromStringMethod(objectType) != null;
			public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				var valueString = reader.GetString();
				var methodInfo = GetFromStringMethod(typeToConvert);
				object? obj = null;
				if (methodInfo is not null)
				{
					if (!methodInfo?.IsStatic ?? false)
					{
						obj = Activator.CreateInstance(typeToConvert);
					}

					obj = methodInfo?.Invoke(obj, new[] { valueString });
				}

				return obj;
			}

			public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
			{
				writer.WriteStringValue(value?.ToString() ?? string.Empty);
			}
		}

		public class AsSubclass : JsonConverter<object>
		{
			private const object Properties = null; //placeholder for nameof()
			public AsSubclass() { }
			public AsSubclass(string typeQualifier) => TypeQualifier = typeQualifier;

			private string TypeQualifier { get; } = string.Empty;
			public override bool CanConvert(Type objectType) => true;

			private static bool ShouldSerialize(PropertyInfo propertyInfo)
			{
				return propertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() == null;
			}

			public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				string typename = reader.GetString() ?? string.Empty;
				string qualifiedTypename = string.IsNullOrEmpty(typename) ? typename : $"{TypeQualifier}{typename}";
				var type = Type.GetType(qualifiedTypename);
				if (type != null)
				{
					object? instance = Activator.CreateInstance(type);
					if (instance is not null)
					{
						var typeProperties = type.GetProperties().Where(ShouldSerialize);
						if (typeProperties.Any())
						{
							reader.Read();
							reader.get
							var jsonPropertiesToken = (JToken.Load(reader) as JProperty)?.Value;
							if (jsonPropertiesToken is JObject jsonPropertiesObj)
							{
								foreach (var typeProperty in typeProperties)
								{
									var propertyAttribute = typeProperty.GetCustomAttribute<JsonPropertyAttribute>();
									string jsonPropertyName = propertyAttribute?.PropertyName ?? typeProperty.Name;
									if (jsonPropertiesObj.TryGetValue(jsonPropertyName, out var jsonPropertyToken))
									{
										typeProperty.SetValue(instance, jsonPropertyToken.ToObject(typeProperty.PropertyType));
									}
								}
							}
						}
					}

					return instance;
				}

				return null;
			}

			public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
			{
				if (value is null)
				{
					JValue.CreateNull().WriteTo(writer);
				}
				else
				{
					var valueType = value.GetType();
					new JValue(valueType.Name).WriteTo(writer);

					JObject propertiesObj = new();

					var properties = valueType.GetProperties().Where(ShouldSerialize);
					foreach (var property in properties)
					{
						var propertyAttribute = property.GetCustomAttribute<JsonPropertyAttribute>();
						string name = propertyAttribute?.PropertyName ?? property.Name;
						object? propertyVal = property.GetValue(value);
						propertiesObj[name] = propertyVal is null ? JValue.CreateNull() : JToken.FromObject(propertyVal, serializer);
					}

					if (propertiesObj.HasValues)
					{
						new JProperty(nameof(Properties), propertiesObj).WriteTo(writer);
					}
				}
			}
		}
	}
}
