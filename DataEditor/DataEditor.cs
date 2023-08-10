using ImGuiNET;
using System.Numerics;
using System.Windows.Forms;

namespace ktsu.io.SchemaTools
{
	internal class DataEditor
	{
		internal DataSource? DataSource { get; set; }
		internal DataEditorOptions Options { get; set; } = new();
		private static float FieldWidth => ImGui.GetIO().DisplaySize.X * 0.15f;

		[STAThread]
		private static void Main()
		{
			DataEditor dataEditor = new();
			dataEditor.Options.Load(dataEditor);
			//ImGuiApp.Start(nameof(DataEditor), dataEditor.Tick, dataEditor.Menu, dataEditor.Resized);
		}

		private void Resized() => Options.Save(this);

#pragma warning disable IDE0060 // Remove unused parameter
		private void Tick(float dt) => ShowDetails();
#pragma warning restore IDE0060 // Remove unused parameter

		private void Reset() => DataSource = null;

		private void Menu()
		{
			if (ImGui.BeginMenu("File"))
			{
				if (ImGui.MenuItem("New"))
				{
					New();
				}

				if (ImGui.MenuItem("Open"))
				{
					Open();
				}

				if (ImGui.MenuItem("Save"))
				{
					Save();
				}

				ImGui.EndMenu();
			}
		}

		private static string GetDataPath()
		{
			var paths = Pathfinder.Paths;
			const string key = "Data";
			if (!paths.TryGetValue(key, out string? path))
			{
				throw new ArgumentException("Could not retrieve the path", key);
			}

			return path;
		}

		private void New()
		{
			Reset();
			DataSource = new();
			Options.Save(this);
		}

		private void Open()
		{
			using var fileDialog = new OpenFileDialog();
			fileDialog.InitialDirectory = GetDataPath();
			fileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
			fileDialog.RestoreDirectory = true;

			if (fileDialog.ShowDialog() == DialogResult.OK)
			{
				Reset();
				if (DataSource.TryLoad((FilePath)fileDialog.FileName, out var dataSource))
				{
					DataSource = dataSource;
				}

				Options.Save(this);
			}
		}

		private void Save()
		{
			if (DataSource is null)
			{
				return;
			}

			if (string.IsNullOrEmpty(DataSource.FilePath))
			{
				SaveAs();
				return;
			}

			DataSource?.Save();
		}

		private void SaveAs()
		{
			if (DataSource is null)
			{
				return;
			}

			using var fileDialog = new SaveFileDialog();
			fileDialog.InitialDirectory = GetDataPath();
			fileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
			fileDialog.RestoreDirectory = true;

			if (fileDialog.ShowDialog() == DialogResult.OK)
			{
				DataSource.ChangeFilePath((FilePath)fileDialog.FileName);
				Save();
				Options.Save(this);
			}
		}

		private void ShowDetails()
		{
			if (DataSource != null)
			{
				ImGui.TextUnformatted($"FilePath: {DataSource.FilePath}");
				ImGui.TextUnformatted($"Schema:");
				ImGui.SameLine();
				string schemaFileName = DataSource.Schema?.FilePath ?? string.Empty;
				schemaFileName = Path.GetFileName(schemaFileName);
				if (ImGui.Button(schemaFileName, new Vector2(FieldWidth, 0)))
				{
					if (string.IsNullOrEmpty(DataSource.FilePath))
					{
						MessageBox.Show("Save the Data Source before applying a schema.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
					}
					else
					{
						using var fileDialog = new OpenFileDialog();
						//fileDialog.InitialDirectory = SchemaEditor.GetSchemaPath();
						fileDialog.Filter = "schema files (*.schema.json)|*.schema.json|All files (*.*)|*.*";
						fileDialog.RestoreDirectory = true;

						if (fileDialog.ShowDialog() == DialogResult.OK)
						{
							string relativePath = Pathfinder.GetRelativePath(DataSource.FilePath, fileDialog.FileName);
							DataSource.LoadSchema((FilePath)relativePath);
						}
					}
				}

				if (DataSource.Schema != null)
				{
					SchemaEditor.ShowMemberHeadings();
					ImGui.SetNextItemWidth(FieldWidth);
					string root = "Root";
					ImGui.InputText($"##Root", ref root, 64, ImGuiInputTextFlags.ReadOnly);
					ImGui.SameLine();
					SchemaEditor.ShowMemberConfig(DataSource.Schema, DataSource.RootSchemaMember);
				}
			}
		}
	}
}
