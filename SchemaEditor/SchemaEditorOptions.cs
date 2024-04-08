namespace ktsu.io.SchemaEditor;

using ktsu.io.AppDataStorage;
using ktsu.io.StrongPaths;
using ktsu.io.ImGuiApp;
using System.Collections.ObjectModel;
using ktsu.io.SchemaLib;

internal class SchemaEditorOptions : AppData<SchemaEditorOptions>
{
	public AbsoluteFilePath CurrentSchemaPath { get; set; } = new();
	public ClassName CurrentClassName { get; set; } = new();
	public ImGuiAppWindowState WindowState { get; set; } = new();
	public HashSet<string> HiddenItems { get; set; } = [];
	public Dictionary<string, Collection<float>> DividerStates { get; set; } = [];
	public Popups Popups { get; set; } = new();
}
