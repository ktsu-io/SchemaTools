
using System.Text.Json.Serialization;

namespace ktsu.io.SchemaTools;
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "We are implementing an enum type for a schema, so I think this is a valid use case")]
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
