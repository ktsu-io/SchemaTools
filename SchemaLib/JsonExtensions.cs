using Newtonsoft.Json.Linq;

namespace ktsu.io
{
	public static class JsonExtensions
	{
		public static void Populate<T>(this JToken token, T target) where T : class
		{
			using var jsonReader = token.CreateReader();
			Schema.JsonSerializer.Populate(jsonReader, target);
		}
	}
}
