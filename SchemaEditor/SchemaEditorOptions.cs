namespace ktsu.io.SchemaTools;
using ktsu.io.AppDataStorage;
using ktsu.io.StrongPaths;
using ktsu.io.ImGuiApp;
using System.Collections.ObjectModel;
using ktsu.io.SchemaEditor;

internal class SchemaEditorOptions : AppData<SchemaEditorOptions>
{
	public AbsoluteFilePath CurrentSchemaPath { get; set; } = new();
	public ClassName CurrentClassName { get; set; } = new();
	public ImGuiAppWindowState WindowState { get; set; } = new();
	public HashSet<string> HiddenItems { get; set; } = new();
	public Dictionary<string, Collection<float>> DividerStates { get; set; } = new();
	public Popups Popups { get; set; } = new();
}
