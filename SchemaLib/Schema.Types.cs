using Newtonsoft.Json;

namespace ktsu.io
{
	public partial class Schema
	{
		public class ClassName : StronglyTypedString<ClassName> { }
		public class MemberName : StronglyTypedString<MemberName> { }
		public class EnumName : StronglyTypedString<EnumName> { }
		public class EnumValueName : StronglyTypedString<EnumValueName> { }
		public static class Types
		{
			public const string TypeQualifier = $"{nameof(ktsu.io)}.{nameof(Schema)}+{nameof(Types)}+";
			public class Null : BaseType { }
			public class Int : BaseType { }
			public class Long : BaseType { }
			public class Float : BaseType { }
			public class Double : BaseType { }
			public class String : BaseType { }
			public class DateTime : BaseType { }
			public class TimeSpan : BaseType { }
			public class Bool : BaseType { }

			public class Enum : BaseType
			{
				[JsonProperty]
				public EnumName Name { get; set; }
				public Enum(EnumName name) => Name = name;
			}

			public class Array : BaseType
			{
				[JsonProperty]
				public BaseType ElementType { get; set; }
				[JsonProperty]
				public string Container { get; set; }
				[JsonProperty]
				public MemberName Key { get; set; }
				public bool IsKeyed => ElementType.IsObject && !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Container);

				public Array(BaseType elementType)
				{
					ElementType = elementType;
					Key = (MemberName)string.Empty;
					Container = string.Empty;
				}

				/// <summary>
				/// Parameterless construction is only allowed to be used  by the FromString() method as it cant know what the element type will be yet
				/// Everyone else should use the constructor that takes an element type
				/// </summary>
				public Array()
				{
					ElementType = new Null();
					Key = (MemberName)string.Empty;
					Container = string.Empty;
				}

				public SchemaMember? GetKeyMember()
				{
					if (ElementType is Object objectElement && objectElement.Class.TryGetMember(Key, out var keyMember))
					{
						return keyMember;
					}

					return null;
				}
			}

			public class Object : BaseType
			{
				public SchemaClass Class { get; }
				[JsonProperty]
				public ClassName ClassName => Class.Name;
				public Object(SchemaClass schemaClass) => Class = schemaClass;
				public override string ToString() => ClassName;
			}

			public static readonly HashSet<Type> BuiltIn = new()
			{
				typeof(Int),
				typeof(Long),
				typeof(Float),
				typeof(Double),
				typeof(String),
				typeof(DateTime),
				typeof(TimeSpan),
				typeof(Bool),
				typeof(Enum),
				typeof(Array),
			};

			public static readonly HashSet<Type> Primitives = new()
			{
				typeof(Int),
				typeof(Long),
				typeof(Float),
				typeof(Double),
				typeof(String),
				typeof(DateTime),
				typeof(TimeSpan),
				typeof(Bool),
			};

			[JsonConverter(typeof(JsonConverters.AsSubclass), TypeQualifier)]
			[JsonObject(MemberSerialization.OptIn)]
			public abstract class BaseType : IEquatable<BaseType?>
			{
				public bool Equals(BaseType? other)
				{
					if (ReferenceEquals(this, other))
					{
						return true;
					}

					if (other?.GetType() != GetType())
					{
						return false;
					}

					return other.ToString() != ToString();
				}

				public override bool Equals(object? obj) => Equals(obj as BaseType);
				public override int GetHashCode() => HashCode.Combine(ToString());
				public override string ToString() => GetType().Name ?? string.Empty;

				public static object? FromString(string? str)
				{
					if (string.IsNullOrEmpty(str))
					{
						return null;
					}

					var type = typeof(Types).GetNestedTypes().FirstOrDefault(t => t.Name == str);
					if (type is null)
					{
						return null;
					}

					return Activator.CreateInstance(type);
				}

				public bool IsBuiltIn => BuiltIn.Contains(GetType());
				public bool IsPrimitive => Primitives.Contains(GetType());
				public bool IsIntegral => this switch
				{
					Int => true,
					Long => true,
					_ => false,
				};
				public bool IsDecimal => this switch
				{
					Float => true,
					Double => true,
					_ => false,
				};
				public bool IsNumeric => IsIntegral || IsDecimal;
				public bool IsContainer => this switch
				{
					Array => true,
					_ => false,
				};
				public bool IsObject => this is Object;
				public bool IsArray => this is Array;
				public bool IsComplexArray => this is Array array && array.ElementType.IsObject;
				public bool IsPrimitiveArray => this is Array array && array.ElementType.IsPrimitive;
			}
		}
	}
}