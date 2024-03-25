namespace ktsu.io.SchemaTools;

using System.Text.Json.Serialization;

public partial class Schema
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "We're mimicing those types in our schema.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "We're mimicing those types in our schema.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "We're mimicing those types in our schema.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "I actually want the hierachy here")]

	public static class Types
	{
		public static string TypeQualifier => $"{typeof(Types).FullName}+";
		public class None : BaseType { }
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
			public EnumName EnumName { get; init; } = new();
		}

		public class Array : BaseType
		{
			public BaseType ElementType { get; init; } = new None();
			public ContainerName Container { get; set; } = new();
			public MemberName Key { get; set; } = new();
			public bool IsKeyed => ElementType.IsObject && !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Container);

			public bool TryGetKeyMember(out SchemaMember? keyMember)
			{
				keyMember = null;
				if (ElementType is Object objectElement)
				{
					objectElement.Class?.TryGetMember(Key, out keyMember);
				}

				return keyMember is not null;
			}
		}

		public class Object : BaseType
		{
			private SchemaClass? _class;
			public SchemaClass? Class
			{
				get
				{
					if (!string.IsNullOrEmpty(ClassName) && _class?.Name != ClassName)
					{
						ParentMember?.ParentSchema?.TryGetClass(ClassName, out _class);
					}

					return _class;
				}
			}

			public ClassName ClassName { get; init; } = new();
			public override string ToString() => ClassName;
		}

		public static HashSet<Type> BuiltIn => new()
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

		public static HashSet<Type> Primitives => new()
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

		[JsonDerivedType(typeof(None), nameof(None))]
		[JsonDerivedType(typeof(Int), nameof(Int))]
		[JsonDerivedType(typeof(Long), nameof(Long))]
		[JsonDerivedType(typeof(Float), nameof(Float))]
		[JsonDerivedType(typeof(Double), nameof(Double))]
		[JsonDerivedType(typeof(String), nameof(String))]
		[JsonDerivedType(typeof(DateTime), nameof(DateTime))]
		[JsonDerivedType(typeof(TimeSpan), nameof(TimeSpan))]
		[JsonDerivedType(typeof(Bool), nameof(Bool))]
		[JsonDerivedType(typeof(Enum), nameof(Enum))]
		[JsonDerivedType(typeof(Array), nameof(Array))]
		[JsonDerivedType(typeof(Object), nameof(Object))]
		[JsonPolymorphic(TypeDiscriminatorPropertyName = "TypeName")]
		public abstract class BaseType : SchemaMemberChild<BaseTypeName>, IEquatable<BaseType?>
		{
			public bool Equals(BaseType? other) => ReferenceEquals(this, other) || ((other?.GetType()) == GetType() && other.ToString() != ToString());

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
				return type is null ? null : Activator.CreateInstance(type);
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
