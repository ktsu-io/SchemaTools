#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaLib;

using System.Text.Json.Serialization;

public partial class Schema
{
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
			private SchemaClass? internalClass;
			public SchemaClass? Class
			{
				get
				{
					if (!string.IsNullOrEmpty(ClassName) && internalClass?.Name != ClassName)
					{
						ParentMember?.ParentSchema?.TryGetClass(ClassName, out internalClass);
					}

					return internalClass;
				}
			}

			public ClassName ClassName { get; init; } = new();
			public override string ToString() => ClassName;
		}

		public class SystemObject : Object { }
		public class Vector : SystemObject { }
		public class Vector2 : Vector { }
		public class Vector3 : Vector { }
		public class Vector4 : Vector { }
		public class ColorRGB : Vector3 { }
		public class ColorRGBA : Vector4 { }

		public static HashSet<Type> BuiltIn =>
		[
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
			typeof(Vector2),
			typeof(Vector3),
			typeof(Vector4),
			typeof(ColorRGB),
			typeof(ColorRGBA),
		];

		public static HashSet<Type> Primitives =>
		[
			typeof(Int),
			typeof(Long),
			typeof(Float),
			typeof(Double),
			typeof(String),
			typeof(DateTime),
			typeof(TimeSpan),
			typeof(Bool),
		];

		public static Dictionary<Type, Type> SystemTypes => new()
		{
			{ typeof(Vector2), typeof(System.Numerics.Vector2) },
			{ typeof(Vector3), typeof(System.Numerics.Vector3) },
			{ typeof(Vector4), typeof(System.Numerics.Vector4) },
			{ typeof(ColorRGB), typeof(System.Numerics.Vector3) },
			{ typeof(ColorRGBA), typeof(System.Numerics.Vector4) },
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
		[JsonDerivedType(typeof(Vector2), nameof(Vector2))]
		[JsonDerivedType(typeof(Vector3), nameof(Vector3))]
		[JsonDerivedType(typeof(Vector4), nameof(Vector4))]
		[JsonDerivedType(typeof(ColorRGB), nameof(ColorRGB))]
		[JsonDerivedType(typeof(ColorRGBA), nameof(ColorRGBA))]
		[JsonDerivedType(typeof(Object), nameof(Object))]
		[JsonPolymorphic(TypeDiscriminatorPropertyName = "TypeName")]
		public abstract class BaseType : SchemaMemberChild<BaseTypeName>, IEquatable<BaseType?>
		{
			public override bool TryRemove() => throw new InvalidOperationException("Cannot remove a type from a member");
			public bool Equals(BaseType? other) => ReferenceEquals(this, other) || ((other?.GetType()) == GetType() && other.ToString() != ToString());

			public override bool Equals(object? obj) => Equals(obj as BaseType);
			public override int GetHashCode() => HashCode.Combine(ToString());
			public override string ToString() => GetType().Name ?? string.Empty;

			public static object? CreateFromString(string? str)
			{
				if (string.IsNullOrEmpty(str))
				{
					return null;
				}

				var type = typeof(Types).GetNestedTypes().FirstOrDefault(t => t.Name == str);
				return type is null ? null : Activator.CreateInstance(type);
			}

			public string DisplayName
			{
				get
				{
					if (this is Array array)
					{
						return $"{nameof(Array)}({array.ElementType.DisplayName})";
					}
					else if (this is Enum enumType)
					{
						return $"{nameof(Enum)}({enumType.EnumName})";
					}

					return ToString();
				}
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
			public bool IsSystemObject => this is SystemObject;
			public bool IsArray => this is Array;
			public bool IsComplexArray => this is Array array && array.ElementType.IsObject;
			public bool IsPrimitiveArray => this is Array array && array.ElementType.IsPrimitive;
		}
	}
}
