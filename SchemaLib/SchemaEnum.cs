using System.Text.Json.Serialization;

namespace ktsu.io.SchemaTools
{
	public class SchemaEnum : SchemaChild
	{
		[JsonInclude]
		public EnumName EnumName { get; private set; } = new();

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
		public void Rename(EnumName enumName) => EnumName = enumName;

		public IReadOnlyList<EnumValueName> Values => ValueList;
	}
}