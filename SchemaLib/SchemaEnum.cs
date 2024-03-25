#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;

using System.Text.Json.Serialization;

public class SchemaEnum : SchemaChild<EnumName>
{
	[JsonPropertyName("Values")]
	private List<EnumValueName> ValueList { get; init; } = new();

	public bool TryAddValue(EnumValueName enumValueName)
	{
		if (!string.IsNullOrEmpty(enumValueName) && !ValueList.Any(v => v == enumValueName))
		{
			ValueList.Add(enumValueName);
			return true;
		}

		return false;
	}

	public void RemoveValue(EnumValueName enumValueName) => ValueList.Remove(enumValueName);

	public IReadOnlyList<EnumValueName> Values => ValueList;
}
