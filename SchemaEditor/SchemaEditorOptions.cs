using ktsu.io;
using Newtonsoft.Json;
using System.Numerics;

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

			using var fileStream = File.OpenWrite(FilePath);
			using var streamWriter = new StreamWriter(fileStream);
			Schema.JsonSerializer.Serialize(streamWriter, this);
		}

		public void Load(SchemaEditor editor)
		{
			Schema.EnsureDirectoryExists(FilePath);

			try
			{
				using var streamReader = File.OpenText(FilePath);
				Schema.JsonSerializer.Populate(streamReader, this);
			}
			catch (FileNotFoundException) { }

			Schema.TryLoad(CurrentSchema, out var schema);
			editor.CurrentSchema = schema;

			if (editor.CurrentSchema?.TryGetClass(CurrentClass, out var schemaClass) ?? false)
			{
				editor.CurrentClass = schemaClass;
			}
		}
	}
}
