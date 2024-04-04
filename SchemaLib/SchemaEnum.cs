#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

public class SchemaEnum : SchemaChild<EnumName>
{
	[JsonPropertyName("Values")]
	private Collection<EnumValueName> ValueCollection { get; set; } = new();
	[JsonIgnore]
	public IReadOnlyCollection<EnumValueName> Values => ValueCollection;

	public bool TryAddValue(EnumValueName enumValueName)
	{
		ArgumentException.ThrowIfNullOrEmpty(enumValueName, nameof(enumValueName));
		if (!ValueCollection.Any(v => v == enumValueName))
		{
			ValueCollection.Add(enumValueName);
			return true;
		}

		return false;
	}

	public bool TryRemoveValue(EnumValueName enumValueName) => ValueCollection.Remove(enumValueName);

	public bool TryRemove() => ParentSchema?.TryRemoveEnum(this) ?? false;
}
