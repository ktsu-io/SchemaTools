using System.Text.Json;
using System.Numerics;
using ktsu.io.StrongPaths;

namespace ktsu.io.SchemaTools
{
	internal class SchemaEditorOptions
	{
		public static FileName FileName => (FileName)$"{nameof(SchemaEditorOptions)}.json";
		public static FilePath FilePath => (FilePath)Path.Combine(Pathfinder.AppData, Pathfinder.ProjectName, FileName);
		public FilePath CurrentSchemaPath { get; set; } = new();
		public ClassName CurrentClassName { get; set; } = new();
		public ImGuiAppWindowState WindowState { get; set; } = new();
		public Dictionary<string, bool> PanelStates { get; set; } = new();
		public Dictionary<string, List<float>> DividerStates { get; set; } = new();

		public void Save(SchemaEditor editor)
		{
			Schema.EnsureDirectoryExists(FilePath);

			CurrentClassName = editor.CurrentClass?.ClassName ?? new();
			CurrentSchemaPath = editor.CurrentSchema?.FilePath ?? new();

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
			try
			{
				File.Move(FilePath, bkFilePath);
			}
			catch (FileNotFoundException) { }

			File.Move(tmpFilePath, FilePath);
			File.Delete(bkFilePath);
		}

		public static SchemaEditorOptions LoadOrCreate()
		{
			Schema.EnsureDirectoryExists(FilePath);

			if (!string.IsNullOrEmpty(FilePath))
			{
				try
				{
					string jsonString = File.ReadAllText(FilePath);
					var options = JsonSerializer.Deserialize<SchemaEditorOptions>(jsonString, Schema.JsonSerializerOptions);
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
