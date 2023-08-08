using ktsu.io;
using System.Text.Json;
using System.Numerics;
using System.Text.Json.Serialization;

namespace ktsu.io
{
	internal class SchemaEditorOptions
	{
		[JsonIgnore]
		public static string FileName => $"{nameof(SchemaEditorOptions)}.json";
		[JsonIgnore]
		public static string FilePath => Path.Combine(Pathfinder.AppData, Pathfinder.ProjectName, FileName);
		public string CurrentSchema { get; set; } = string.Empty;
		public Schema.ClassName CurrentClass { get; set; } = (Schema.ClassName)string.Empty;
		public ImGuiAppWindowState WindowState { get; set; } = new();

		public void Save(SchemaEditor editor)
		{
			Schema.EnsureDirectoryExists(FilePath);

			CurrentClass = editor.CurrentClass?.Name ?? (Schema.ClassName)string.Empty;
			CurrentSchema = editor.CurrentSchema?.FilePath ?? string.Empty;

			if (ImGuiApp.Window != null)
			{
				WindowState.WindowSizeState = ImGuiApp.Window.WindowState;
				WindowState.Pos = new Vector2(ImGuiApp.Window.X, ImGuiApp.Window.Y);
				WindowState.Size = new Vector2(ImGuiApp.Window.Width, ImGuiApp.Window.Height);
			}

			string jsonString = JsonSerializer.Serialize(this, Schema.JsonSerializerOptions);

			//TODO: hoist this out to some static method called something like WriteTextSafely
			string tmpFilePath = $"{FilePath}.tmp";
			string bkFilePath = $"{FilePath}.bk";
			File.Delete(tmpFilePath);
			File.Delete(bkFilePath);
			File.WriteAllText(tmpFilePath, jsonString);
			File.Move(FilePath, bkFilePath);
			File.Move(tmpFilePath, FilePath);
			File.Delete(bkFilePath);
		}

		public static SchemaEditorOptions LoadOrCreate()
		{
			if (!string.IsNullOrEmpty(FilePath))
			{
				try
				{
					string jsonString = File.ReadAllText(FilePath);
					var options = JsonSerializer.Deserialize<SchemaEditorOptions>(jsonString);
					if (options != null)
					{
						return options;
					}
				}
				catch (FileNotFoundException)
				{
				}
				catch (JsonException)
				{
				}
			}

			return new();
		}
	}
}
