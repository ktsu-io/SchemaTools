#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;

using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using ktsu.io.StrongPaths;
using ktsu.io.ImGuiApp;
using ktsu.io.ImGuiWidgets;

public class SchemaEditor
{
	internal Schema? CurrentSchema { get; set; }
	internal SchemaClass? CurrentClass { get; set; }
	internal SchemaEditorOptions Options { get; } = new();
	private static float FieldWidth => ImGui.GetIO().DisplaySize.X * 0.15f;
	private DateTime LastSaveOptionsTime { get; set; } = DateTime.MinValue;
	private DateTime SaveOptionsQueuedTime { get; set; } = DateTime.MinValue;
	private TimeSpan SaveOptionsDebounceTime { get; } = TimeSpan.FromSeconds(3);
	private DividerContainer DividerContainerCols { get; }

	[STAThread]
	private static void Main(string[] _)
	{
		SchemaEditor schemaEditor = new();
		string title = nameof(SchemaEditor);
		if (schemaEditor.CurrentSchema != null)
		{
			title += $" - {schemaEditor.CurrentSchema.FilePath.FileName}";
		}

		ImGuiApp.Start(title, schemaEditor.Options.WindowState, schemaEditor.Tick, schemaEditor.ShowMenu, schemaEditor.WindowResized);
	}

	public SchemaEditor()
	{
		DividerContainerCols = new("RootDivider", DividerResized);
		Options = SchemaEditorOptions.LoadOrCreate();
		RestoreOptions();
		DividerContainerCols.Add("Left", 0.25f, ShowLeftPanel);
		DividerContainerCols.Add("Right", 0.75f, ShowRightPanel);
	}

	private void RestoreOptions()
	{
		RestoreOpenSchema();
		RestoreDividerStates();
	}

	private void WindowResized() => QueueSaveOptions();

	private void DividerResized(DividerContainer container)
	{
		Options.DividerStates[container.Id] = container.GetSizes();
		QueueSaveOptions();
	}

	private void RestoreDividerStates()
	{
		if (Options.DividerStates.TryGetValue(DividerContainerCols.Id, out var sizes))
		{
			DividerContainerCols.SetSizesFromList(sizes);
		}
	}

	//Dont call this directly, call QueueSaveOptions instead so that we can debounce the saves and avoid saving multiple times per frame or multiple frames in a row
	private void SaveOptionsInternal() => Options.Save();

	private void QueueSaveOptions() => SaveOptionsQueuedTime = DateTime.Now;

	private void SaveOptionsIfRequired()
	{
		//debounce the save requests and avoid saving multiple times per frame or multiple frames in a row
		if ((SaveOptionsQueuedTime > LastSaveOptionsTime) && ((DateTime.Now - SaveOptionsQueuedTime) > SaveOptionsDebounceTime))
		{
			SaveOptionsInternal();
			LastSaveOptionsTime = DateTime.Now;
		}
	}

	private void Tick(float dt)
	{
		DividerContainerCols.Tick(dt);

		SaveOptionsIfRequired();
	}

	private void ShowLeftPanel(float dt)
	{
		ShowCollapsiblePanel($"Enums ({CurrentSchema?.Enums.Count ?? 0})", ShowEnums);
		ShowCollapsiblePanel($"Classes ({CurrentSchema?.Classes.Count ?? 0})", ShowClasses);
	}

	private void ShowRightPanel(float dt)
	{
		ShowSchemaConfig();
		ShowMembers();
	}

	private static void ShowCollapsiblePanel(string name, Action contentDelegate)
	{
		if (ImGui.CollapsingHeader(name))
		{
			contentDelegate?.Invoke();
		}
	}
	private void Reset()
	{
		CurrentSchema = null;
		CurrentClass = null;
	}

	private void RestoreOpenSchema()
	{
		if (string.IsNullOrEmpty(Options.CurrentSchemaPath))
		{
			return;
		}

		if (Schema.TryLoad(Options.CurrentSchemaPath, out var previouslyOpenSchema) && previouslyOpenSchema != null)
		{
			CurrentSchema = previouslyOpenSchema;
			CurrentClass = null;

			if (string.IsNullOrEmpty(Options.CurrentClassName))
			{
				return;
			}

			if (CurrentSchema.TryGetClass(Options.CurrentClassName, out var previouslyOpenClass))
			{
				CurrentClass = previouslyOpenClass;
			}
		}
	}

	private void ShowMenu()
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

			ImGui.Separator();

			string schemaFilePath = CurrentSchema?.FilePath ?? "";
			if (ImGui.MenuItem("Open Externally", !string.IsNullOrEmpty(schemaFilePath)))
			{
				var p = new Process();
				p.StartInfo.FileName = $"explorer.exe";
				p.StartInfo.Arguments = schemaFilePath;
				p.Start();
			}

			ImGui.EndMenu();
		}
	}

	private void New()
	{
		Reset();
		CurrentSchema = new Schema();
		QueueSaveOptions();
	}

	private void Open()
	{
		using var fileDialog = new OpenFileDialog();
		fileDialog.Filter = "schema files (*.schema.json)|*.schema.json|All files (*.*)|*.*";
		fileDialog.RestoreDirectory = true;

		if (fileDialog.ShowDialog() == DialogResult.OK)
		{
			Reset();
			if (Schema.TryLoad((FilePath)fileDialog.FileName, out var schema) && schema != null)
			{
				CurrentSchema = schema;
				CurrentClass = CurrentSchema?.GetFirstClass();
				QueueSaveOptions();
			}
		}
	}

	private void Save()
	{
		if (string.IsNullOrEmpty(CurrentSchema?.FilePath ?? new()))
		{
			SaveAs();
			return;
		}

		CurrentSchema?.Save();
	}

	private void SaveAs()
	{
		using var fileDialog = new SaveFileDialog();
		fileDialog.Filter = "schema files (*.schema.json)|*.schema.json|All files (*.*)|*.*";
		fileDialog.RestoreDirectory = true;

		if (fileDialog.ShowDialog() == DialogResult.OK)
		{
			CurrentSchema?.ChangeFilePath((FilePath)fileDialog.FileName);
			Save();
			QueueSaveOptions();
		}
	}

	private void ShowNewEnum()
	{
		if (CurrentSchema != null)
		{
			if (ImGui.Button("Add Enum", new Vector2(FieldWidth, 0)))
			{
				var newName = (EnumName)TextPrompt.Show("New Enum Name?");
				if (CurrentSchema.TryAddEnum(newName))
				{
					QueueSaveOptions();
				}
				else
				{
					MessageBox.Show($"An Enum with that name ({newName}) already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				}
			}
		}
	}

	private void ShowNewClass()
	{
		if (CurrentSchema != null)
		{
			if (ImGui.Button("Add Class", new Vector2(FieldWidth, 0)))
			{
				var newName = (ClassName)TextPrompt.Show("New Class Name?");
				if (CurrentSchema.TryAddClass(newName))
				{
					CurrentClass = CurrentSchema.GetLastClass();
					QueueSaveOptions();
				}
				else
				{
					MessageBox.Show($"A Class with that name ({newName}) already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				}
			}
		}
	}

	private void ShowNewMember()
	{
		if (CurrentClass != null)
		{
			if (ImGui.Button("Add Member", new Vector2(FieldWidth, 0)))
			{
				var newName = (MemberName)TextPrompt.Show("New Member Name?");
				if (CurrentClass.TryAddMember(newName))
				{
					QueueSaveOptions();
				}
				else
				{
					MessageBox.Show($"A Member with that name ({newName}) already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				}
			}
		}
	}

	private void ShowEnums()
	{
		if (CurrentSchema != null)
		{
			ImGui.Indent();
			ShowNewEnum();
			ImGui.NewLine();
			foreach (var schemaEnum in CurrentSchema.Enums.OrderBy(e => e.Name).ToList())
			{
				string enumName = schemaEnum.Name;
				if (ImGui.Button($"X##deleteEnum{enumName}", new Vector2(ImGui.GetFrameHeight(), 0)))
				{
					CurrentSchema.Enums.Remove(schemaEnum);
				}

				ImGui.SameLine();
				ImGui.SetNextItemWidth(FieldWidth);
				ImGui.InputText($"##Enum{enumName}", ref enumName, 64, ImGuiInputTextFlags.ReadOnly);
				ImGui.SameLine();
				if (ImGui.Button($"+##addEnumValue{enumName}", new Vector2(ImGui.GetFrameHeight(), 0)))
				{
					var newValue = (EnumValueName)TextPrompt.Show("New Enum Value?");
					if (!schemaEnum.TryAddValue(newValue))
					{
						MessageBox.Show($"An Enum Value with that name ({newValue}) already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
					}
				}

				ImGui.Indent();
				foreach (var enumValueName in schemaEnum.Values.ToList())
				{
					string enumValue = enumValueName;
					if (ImGui.Button($"X##deleteEnumValue{enumName}{enumValue}", new Vector2(ImGui.GetFrameHeight(), 0)))
					{
						schemaEnum.RemoveValue(enumValueName);
					}

					ImGui.SameLine();
					ImGui.SetNextItemWidth(FieldWidth);
					ImGui.InputText($"##Enum{enumValue}", ref enumValue, 64, ImGuiInputTextFlags.ReadOnly);
				}

				ImGui.NewLine();
				ImGui.Unindent();
			}

			ImGui.Unindent();
		}
	}

	private void ShowClasses()
	{
		if (CurrentSchema != null)
		{
			ImGui.Indent();
			ShowNewClass();
			ImGui.NewLine();
			foreach (var schemaClass in CurrentSchema.Classes.OrderBy(c => c.Name).ToList())
			{
				if (ImGui.Button($"X##deleteClass{schemaClass.Name}", new Vector2(ImGui.GetFrameHeight(), 0)))
				{
					CurrentSchema.Classes.Remove(schemaClass);
				}

				ImGui.SameLine();
				ImGui.SetNextItemWidth(FieldWidth);
				if (ImGui.Button($"{schemaClass.Name}", new Vector2(FieldWidth, 0)))
				{
					CurrentClass = schemaClass;
					QueueSaveOptions();
				}
			}

			ImGui.Unindent();
			ImGui.NewLine();
		}
	}

	public static void ShowMemberConfig(Schema schema, SchemaMember schemaMember)
	{
		ArgumentNullException.ThrowIfNull(schema);
		ArgumentNullException.ThrowIfNull(schemaMember);

		ImGui.Button(MakeTypenameDisplay(schemaMember), new Vector2(FieldWidth, 0));
		if (ImGui.BeginPopupContextItem($"##{schemaMember.Name}Typename", ImGuiPopupFlags.MouseButtonLeft))
		{
			foreach (var type in Schema.Types.Primitives)
			{
				if (ImGui.Selectable(type.Name))
				{
					if (Activator.CreateInstance(type) is Schema.Types.BaseType newType)
					{
						schemaMember.SetType(newType);
					}
				}
			}

			ImGui.Separator();
			foreach (var schemaClass in schema.Classes.OrderBy(c => c.Name))
			{
				if (ImGui.Selectable(schemaClass.Name))
				{
					schemaMember.SetType(new Schema.Types.Object()
					{
						ClassName = schemaClass.Name,
					});
				}
			}

			ImGui.Separator();
			if (ImGui.BeginMenu(nameof(Schema.Types.Enum), schema.Enums.Count != 0))
			{
				foreach (var schemaEnum in schema.Enums.OrderBy(e => e.Name))
				{
					if (ImGui.Selectable(schemaEnum.Name))
					{
						schemaMember.SetType(new Schema.Types.Enum()
						{
							EnumName = schemaEnum.Name,
						});
					}
				}

				ImGui.EndMenu();
			}

			ImGui.Separator();
			if (ImGui.BeginMenu($"{nameof(Schema.Types.Array)}..."))
			{
				foreach (var type in Schema.Types.Primitives)
				{
					if (ImGui.Selectable(type.Name))
					{
						if (Activator.CreateInstance(type) is Schema.Types.BaseType newType)
						{
							schemaMember.SetType(new Schema.Types.Array()
							{
								ElementType = newType,
							});

						}
					}
				}

				ImGui.Separator();
				foreach (var schemaClass in schema.Classes)
				{
					if (ImGui.Selectable(schemaClass.Name))
					{
						schemaMember.SetType(new Schema.Types.Array()
						{
							ElementType = new Schema.Types.Object()
							{
								ClassName = schemaClass.Name,
							},
						});
					}
				}

				ImGui.EndMenu();
			}

			ImGui.EndPopup();
		}

		if (schemaMember.Type is Schema.Types.Array array)
		{
			ImGui.SameLine();
			ImGui.SetNextItemWidth(FieldWidth);
			string container = array.Container;
			ImGui.InputText($"##Container{schemaMember.Name}", ref container, 64);
			array.Container = (ContainerName)container;

			if (array.ElementType is Schema.Types.Object obj && obj.Class is not null)
			{
				ImGui.SameLine();
				ImGui.Button(array.Key, new Vector2(FieldWidth, 0));
				if (ImGui.BeginPopupContextItem($"##{schemaMember.Name}Key", ImGuiPopupFlags.MouseButtonLeft))
				{
					if (ImGui.Selectable("<none>"))
					{
						array.Key = new();
					}

					foreach (var primitiveMember in obj.Class.Members.Where(m => m.Type.IsPrimitive).OrderBy(m => m.Name))
					{
						if (ImGui.Selectable(primitiveMember.Name))
						{
							array.Key = primitiveMember.Name;
						}
					}

					ImGui.EndPopup();
				}
			}
		}
	}

	public static void ShowMemberHeadings()
	{
		ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
		ImGui.Button("Name", new Vector2(FieldWidth, 0));
		ImGui.SameLine();
		ImGui.Button("Type", new Vector2(FieldWidth, 0));
		ImGui.SameLine();
		ImGui.Button("Container", new Vector2(FieldWidth, 0));
		ImGui.SameLine();
		ImGui.Button("Key", new Vector2(FieldWidth, 0));
		ImGui.PopStyleColor();
	}

	private void ShowMembers()
	{
		if (CurrentClass != null)
		{
			if (ImGui.CollapsingHeader($"{CurrentClass.Name} Members", ImGuiTreeNodeFlags.DefaultOpen))
			{
				ImGui.Indent();
				ShowNewMember();
				ImGui.NewLine();
				float frameHeight = ImGui.GetFrameHeight();
				float spacing = ImGui.GetStyle().ItemSpacing.X;
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + frameHeight + spacing);

				ShowMemberHeadings();

				foreach (var schemaMember in CurrentClass.Members.ToList())
				{
					string name = schemaMember.Name;
					if (ImGui.Button($"X##deleteMember{name}", new Vector2(frameHeight, 0)))
					{
						CurrentClass.RemoveMember(schemaMember);
					}

					ImGui.SameLine();
					ImGui.SetNextItemWidth(FieldWidth);
					ImGui.InputText($"##{name}", ref name, 64, ImGuiInputTextFlags.ReadOnly);
					ImGui.SameLine();
					if (CurrentSchema != null)
					{
						ShowMemberConfig(CurrentSchema, schemaMember);
					}
				}

				ImGui.Unindent();
				ImGui.NewLine();
			}
		}
	}

	private static string MakeTypenameDisplay(SchemaMember schemaMember)
	{
		if (schemaMember.Type is Schema.Types.Array array)
		{
			return $"{nameof(Schema.Types.Array)}({array.ElementType})";
		}
		else if (schemaMember.Type is Schema.Types.Enum enumType)
		{
			return $"{nameof(Schema.Types.Enum)}({enumType.EnumName})";
		}

		return schemaMember.Type.ToString();
	}

	private void ShowSchemaConfig()
	{
		if (CurrentSchema is not null)
		{
			if (string.IsNullOrEmpty(CurrentSchema.FilePath))
			{
				ImGui.TextUnformatted("Schema has not been saved. Save it before configuring relative paths.");
				return;
			}

			ImGui.TextUnformatted($"Schema Path: {CurrentSchema.FilePath}");
			ImGui.TextUnformatted($"Data Source Path: {CurrentSchema.DataSourcePath}");
			ImGui.SameLine();
			if (ImGui.Button("Browse"))
			{
				var initialDir = CurrentSchema.DataSourcePath;
				if (string.IsNullOrEmpty(initialDir))
				{
					initialDir = CurrentSchema.FilePath.DirectoryPath;
				}

				using var dialog = new FolderBrowserDialog();
				dialog.InitialDirectory = initialDir;
				dialog.SelectedPath = initialDir;

				if (dialog.ShowDialog() == DialogResult.OK)
				{
					CurrentSchema.DataSourcePath = (DirectoryPath)dialog.SelectedPath;
				}
			}
		}
	}
}
