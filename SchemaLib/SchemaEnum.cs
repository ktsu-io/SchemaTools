#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

public class SchemaEnum : SchemaChild<EnumName>
{
	[JsonInclude]
	private Collection<EnumValueName> Values { get; set; } = [];
	public IReadOnlyCollection<EnumValueName> GetValues() => Values;

	public bool TryAddValue(EnumValueName enumValueName)
	{
		ArgumentException.ThrowIfNullOrEmpty(enumValueName, nameof(enumValueName));
		if (!Values.Any(v => v == enumValueName))
		{
			Values.Add(enumValueName);
			return true;
		}

		return false;
	}

	public bool TryRemoveValue(EnumValueName enumValueName) => Values.Remove(enumValueName);

	public bool TryRemove() => ParentSchema?.TryRemoveEnum(this) ?? false;
}
