using ktsu.io.StrongStrings;
using System.Text.Json.Serialization;

namespace ktsu.io
{
	public partial class Schema
	{
		[JsonConverter(typeof(JsonConverters.AsString))]
		public record class ClassName : AnyStrongString<ClassName> { }

		[JsonConverter(typeof(JsonConverters.AsString))]
		public record class MemberName : AnyStrongString<MemberName> { }

		[JsonConverter(typeof(JsonConverters.AsString))]
		public record class EnumName : AnyStrongString<EnumName> { }

		[JsonConverter(typeof(JsonConverters.AsString))]
		public record class EnumValueName : AnyStrongString<EnumValueName> { }

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
				public EnumName Name { get; set; }
				public Enum(EnumName name) => Name = name;
			}

			public class Array : BaseType
			{
				public BaseType ElementType { get; set; }
				public string Container { get; set; }
				public MemberName Key { get; set; }
				[JsonIgnore]
				public bool IsKeyed => ElementType.IsObject && !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Container);

				public Array(BaseType elementType)
				{
					ElementType = elementType;
					Key = (MemberName)string.Empty;
					Container = string.Empty;
				}

				/// <summary>
				/// Parameterless construction is only allowed to be used by the FromString() method as it cant know what the element type will be yet
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
				[JsonIgnore]
				public SchemaClass Class { get; }
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

				[JsonIgnore]
				public bool IsBuiltIn => BuiltIn.Contains(GetType());
				[JsonIgnore]
				public bool IsPrimitive => Primitives.Contains(GetType());
				[JsonIgnore]
				public bool IsIntegral => this switch
				{
					Int => true,
					Long => true,
					_ => false,
				};
				[JsonIgnore]
				public bool IsDecimal => this switch
				{
					Float => true,
					Double => true,
					_ => false,
				};
				[JsonIgnore]
				public bool IsNumeric => IsIntegral || IsDecimal;
				[JsonIgnore]
				public bool IsContainer => this switch
				{
					Array => true,
					_ => false,
				};
				[JsonIgnore]
				public bool IsObject => this is Object;
				[JsonIgnore]
				public bool IsArray => this is Array;
				[JsonIgnore]
				public bool IsComplexArray => this is Array array && array.ElementType.IsObject;
				[JsonIgnore]
				public bool IsPrimitiveArray => this is Array array && array.ElementType.IsPrimitive;
			}
		}
	}
}