namespace ktsu.io.SchemaTools;
using ktsu.io.AppDataStorage;
using ktsu.io.StrongPaths;
using ktsu.io.ImGuiApp;
using System.Collections.ObjectModel;

internal class SchemaEditorOptions : AppData<SchemaEditorOptions>
{
	public FilePath CurrentSchemaPath { get; set; } = new();
	public ClassName CurrentClassName { get; set; } = new();
	public ImGuiAppWindowState WindowState { get; set; } = new();
	public Dictionary<string, bool> PanelStates { get; set; } = new();
	public Dictionary<string, Collection<float>> DividerStates { get; set; } = new();
}
