using Newtonsoft.Json;
using System.Numerics;

namespace ktsu.io.SchemaTools
{
	internal class DataEditorOptions
	{
		[JsonIgnore]
		public static FileName FileName => (FileName)$"{nameof(DataEditorOptions)}.json";
		[JsonIgnore]
		public static FilePath FilePath => (FilePath)Path.Combine(Pathfinder.AppData, Pathfinder.ProjectName, FileName);
		public FilePath CurrentDataSourcePath { get; set; } = new();
		public string WindowState { get; set; } = "Normal";
		public Vector2 WindowPos { get; set; } = new Vector2(50, 50);
		public Vector2 WindowSize { get; set; } = new Vector2(1600, 1000);

		private static JsonSerializer JsonSerializer { get; } = JsonSerializer.Create(new JsonSerializerSettings()
		{
			Formatting = Formatting.Indented,
		});

		private static void EnsureDirectoryExists()
		{
			string? dirName = Path.GetDirectoryName(FilePath);
			if (!string.IsNullOrEmpty(dirName))
			{
				Directory.CreateDirectory(dirName);
			}
		}

		public void Save(DataEditor editor)
		{
			EnsureDirectoryExists();

			CurrentDataSourcePath = editor.DataSource?.FilePath ?? new();

			if (ImGuiApp.Window != null)
			{
				WindowState = ImGuiApp.Window.WindowState.ToString();
				WindowPos = new Vector2(ImGuiApp.Window.X, ImGuiApp.Window.Y);
				WindowSize = new Vector2(ImGuiApp.Window.Width, ImGuiApp.Window.Height);
			}

			using var fileStream = File.OpenWrite(FilePath);
			using var streamWriter = new StreamWriter(fileStream);
			JsonSerializer.Serialize(streamWriter, this);
		}

		public void Load(DataEditor editor)
		{
			EnsureDirectoryExists();

			try
			{
				using var streamReader = File.OpenText(FilePath);
				JsonSerializer.Populate(streamReader, this);
			}
			catch (FileNotFoundException) { }

			if (Enum.TryParse(WindowState, out Veldrid.WindowState windowState))
			{
				ImGuiApp.InitialWindowState = windowState;
			}

			ImGuiApp.InitialWindowSize = WindowSize;
			ImGuiApp.InitialWindowPos = WindowPos;

			if (DataSource.TryLoad(CurrentDataSourcePath, out var dataSource))
			{
				editor.DataSource = dataSource;
			}
		}
	}
}
