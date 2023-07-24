using medmondson;
using Newtonsoft.Json;
using System.Diagnostics;

namespace SchemaLib
{
	[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
	[JsonConverter(typeof(JsonConverters.AsString))]
	public abstract class StronglyTypedString : IEquatable<StronglyTypedString?>
	{
		protected string Value { get; set; } = string.Empty;
		public static implicit operator string(StronglyTypedString value) => value.Value;
		protected string GetDebuggerDisplay() => ToString();
		public override int GetHashCode() => HashCode.Combine(Value);
		public override string ToString() => Value;
		public StronglyTypedString FromString(string? value)
		{
			Value = value ?? string.Empty;
			return this;
		}
		public bool Equals(StronglyTypedString? other) => string.Equals(Value, other?.Value);
		public static bool operator ==(StronglyTypedString? a, StronglyTypedString? b) => a?.Equals(b) ?? false;
		public static bool operator !=(StronglyTypedString? a, StronglyTypedString? b) => (!a?.Equals(b)) ?? false;
		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj?.GetType() != GetType())
			{
				return false;
			}

			return Equals(obj as StronglyTypedString);
		}
	}

	public abstract class StronglyTypedString<T> : StronglyTypedString where T : StronglyTypedString<T>, new()
	{
		public static explicit operator StronglyTypedString<T>(string? value)
		{
			var t = new T();
			t.FromString(value);
			return t;
		}
	}
}
