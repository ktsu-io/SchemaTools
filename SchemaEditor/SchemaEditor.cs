#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;

using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using ktsu.io.Extensions;
using ktsu.io.ImGuiApp;
using ktsu.io.ImGuiStyler;
using ktsu.io.ImGuiWidgets;
using ktsu.io.SchemaEditor;
using ktsu.io.StrongPaths;

public class SchemaEditor
{
	public static SchemaEditor Instance { get; } = new();
	internal Schema? CurrentSchema { get; set; }
	internal SchemaClass? CurrentClass { get; set; }
	internal SchemaEditorOptions Options { get; } = new();
	private static float FieldWidth => ImGui.GetIO().DisplaySize.X * 0.15f;
	private DateTime LastSaveOptionsTime { get; set; } = DateTime.MinValue;
	private DateTime SaveOptionsQueuedTime { get; set; } = DateTime.MinValue;
	private TimeSpan SaveOptionsDebounceTime { get; } = TimeSpan.FromSeconds(3);
	private DividerContainer DividerContainerCols { get; init; }

	private ImGuiAppWindowState InitialWindowState { get; init; }
	private Popups Popups { get; } = new();

	private static void Main(string[] _)
	{
		string title = nameof(SchemaEditor);
		if (Instance.CurrentSchema is not null)
		{
			title += $" - {Instance.CurrentSchema.FilePath.FileName}";
		}

		ImGuiApp.Start(title, Instance.InitialWindowState, Instance.OnStart, Instance.OnTick, Instance.OnMenu, Instance.OnWindowResized);
	}

	public SchemaEditor()
	{
		DividerContainerCols =
			new(
				"RootDivider",
				DividerResized,
				DividerLayout.Columns,
				[
					new("Left", 0.25f, ShowLeftPanel),
					new("Right", 0.75f, ShowRightPanel),
				]
			);

		Options = SchemaEditorOptions.LoadOrCreate();
		Popups = Options.Popups;
		InitialWindowState = Options.WindowState;

		// restore open schema
		if (Schema.TryLoad(Options.CurrentSchemaPath, out var previouslyOpenSchema) && previouslyOpenSchema is not null)
		{
			CurrentSchema = previouslyOpenSchema;
			CurrentClass = null;
			CurrentClass = CurrentSchema.GetClass(Options.CurrentClassName);
		}

		// restore divider states
		if (Options.DividerStates.TryGetValue(DividerContainerCols.Id, out var sizes))
		{
			DividerContainerCols.SetSizesFromList(sizes);
		}
	}

	private void OnStart()
	{
	}

	private void OnWindowResized() => QueueSaveOptions();

	private void DividerResized(DividerContainer container)
	{
		Options.DividerStates[container.Id] = container.GetSizes();
		QueueSaveOptions();
	}


	//Dont call this directly, call QueueSaveOptions instead so that we can debounce the saves and avoid saving multiple times per frame or multiple frames in a row
	private void SaveOptionsInternal()
	{
		Options.CurrentSchemaPath = CurrentSchema?.FilePath ?? new();
		Options.CurrentClassName = CurrentClass?.Name ?? new();
		Options.DividerStates[DividerContainerCols.Id] = DividerContainerCols.GetSizes();
		Options.WindowState = ImGuiApp.WindowState;
		Options.Popups = Popups;
		Options.Save();
	}

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

	private void OnTick(float dt)
	{
		using (Theme.Color(Theme.Palette.Normal))
		{
			DividerContainerCols.Tick(dt);

			SaveOptionsIfRequired();

			Popups.Update();
		}
	}

	private void ShowLeftPanel(float dt)
	{
		using (Button.Alignment.Left())
		{
			if (ImGui.Button($"Enums ({CurrentSchema?.GetEnums().Count ?? 0})"))
			{
			}
		}
		ShowEnums();

		using (Button.Alignment.Left())
		{
			if (ImGui.Button($"Classes ({CurrentSchema?.GetClasses().Count ?? 0})"))
			{
			}
		}
		ShowClasses();
	}

	private void ShowRightPanel(float dt)
	{
		ShowSchemaConfig();
		ShowMembers();
	}

	private static void ShowCollapsiblePanel(string name, Action contentDelegate)
	{
		if (ImGui.CollapsingHeader(name, ImGuiTreeNodeFlags.DefaultOpen))
		{
			contentDelegate?.Invoke();
		}
	}
	private void Reset()
	{
		CurrentSchema = null;
		CurrentClass = null;
	}

	private void OnMenu()
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
		Popups.OpenBrowserFileOpen("Open Schema", (filePath) =>
		{
			Reset();
			if (Schema.TryLoad(filePath, out var schema) && schema is not null)
			{
				CurrentSchema = schema;
				CurrentClass = CurrentSchema?.GetFirstClass();
				QueueSaveOptions();
			}
			else
			{
				Popups.OpenMessageOK("Error", "Failed to load schema.");
			}
		}, "*.schema.json");
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
		Popups.OpenBrowserFileSave("Save Schema", (filePath) =>
		{
			CurrentSchema?.ChangeFilePath(filePath);
			Save();
			QueueSaveOptions();
		}, "*.schema.json");
	}

	private void ShowNewEnum()
	{
		if (CurrentSchema is not null)
		{
			using (Button.Alignment.Left())
			{
				if (ImGui.Button("+ New Enum"))
				{
					Popups.OpenInputString("Input", "New Enum Name", string.Empty, (newName) =>
					{
						if (CurrentSchema.TryAddEnum((EnumName)newName))
						{
							QueueSaveOptions();
						}
						else
						{
							Popups.OpenMessageOK("Error", $"An Enum with that name ({newName}) already exists.");
						}
					});
				}
			}
		}
	}

	private void ShowNewClass()
	{
		if (CurrentSchema is not null)
		{
			using (Button.Alignment.Left())
			{
				if (ImGui.Button("+ New Class"))
				{
					Popups.OpenInputString("Input", "New Class Name", string.Empty, (newName) =>
					{
						if (CurrentSchema.TryAddClass((ClassName)newName))
						{
							CurrentClass = CurrentSchema.GetLastClass();
							QueueSaveOptions();
						}
						else
						{
							Popups.OpenMessageOK("Error", $"A Class with that name ({newName}) already exists.");
						}
					});
				}
			}
		}
	}

	private void ShowNewMember()
	{
		if (CurrentClass is not null)
		{
			if (ImGui.Button("+ New Member", new Vector2(FieldWidth, 0)))
			{
				Popups.OpenInputString("Input", "New Member Name", string.Empty, (newName) =>
				{
					if (CurrentClass.TryAddMember((MemberName)newName))
					{
						QueueSaveOptions();
					}
					else
					{
						Popups.OpenMessageOK("Error", $"A Member with that name ({newName}) already exists.");
					}
				});
			}
		}
	}

	private void ShowEnums()
	{
		if (CurrentSchema is not null)
		{
			using (var enumTree = new Tree())
			{
				using (enumTree.Child)
				{
					ShowNewEnum();
				}
				foreach (var schemaEnum in CurrentSchema.GetEnums().OrderBy(e => e.Name).ToCollection())
				{
					using (enumTree.Child)
					{
						using (Button.Alignment.Left())
						{
							ImGui.Button($"{schemaEnum.Name} ({schemaEnum.GetValues().Count})##EnumName{schemaEnum.Name}", new(FieldWidth, 0));
						}

						ImGui.SameLine();
						if (ImGui.Button($"X##deleteEnum{schemaEnum.Name}", new Vector2(ImGui.GetFrameHeight(), 0)))
						{
							schemaEnum.TryRemove();
						}
					}

					using (var valueTree = new Tree())
					{
						foreach (var enumValueName in schemaEnum.GetValues().ToCollection())
						{
							using (valueTree.Child)
							{
								using (Button.Alignment.Left())
								{
									ImGui.Button($"{enumValueName}##EnumValue{enumValueName}", new(FieldWidth, 0));
								}

								ImGui.SameLine();
								if (ImGui.Button($"X##deleteEnumValue{schemaEnum.Name}{enumValueName}", new Vector2(ImGui.GetFrameHeight(), 0)))
								{
									schemaEnum.TryRemoveValue(enumValueName);
								}
							}
						}

						using (valueTree.Child)
						{
							if (ImGui.Button($"+ New Value##addEnumValue{schemaEnum.Name}"))
							{
								Popups.OpenInputString("Input", "New Enum Value", string.Empty, (newValue) =>
								{
									if (!schemaEnum.TryAddValue((EnumValueName)newValue))
									{
										Popups.OpenMessageOK("Error", $"A Enum Value with that name ({newValue}) already exists.");
									}
								});
							}
						}
					}

					ImGui.NewLine();
				}
			}
		}
	}

	private void ShowClasses()
	{
		if (CurrentSchema is not null)
		{
			using (var classTree = new Tree())
			{
				using (classTree.Child)
				{
					ShowNewClass();
				}

				foreach (var schemaClass in CurrentSchema.GetClasses().OrderBy(c => c.Name).ToCollection())
				{
					using (classTree.Child)
					{
						using (Button.Alignment.Left())
						{
							if (ImGui.Button($"{schemaClass.Name}", new Vector2(FieldWidth, 0)))
							{
								CurrentClass = schemaClass;
								QueueSaveOptions();
							}
						}

						ImGui.SameLine();
						if (ImGui.Button($"X##deleteClass{schemaClass.Name}", new Vector2(ImGui.GetFrameHeight(), 0)))
						{
							schemaClass.TryRemove();
						}
					}
				}
			}

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
			foreach (var schemaClass in schema.GetClasses().OrderBy(c => c.Name))
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
			if (ImGui.BeginMenu(nameof(Schema.Types.Enum), schema.GetEnums().Count != 0))
			{
				foreach (var schemaEnum in schema.GetEnums().OrderBy(e => e.Name))
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
				foreach (var schemaClass in schema.GetClasses())
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

					foreach (var primitiveMember in obj.Class.GetMembers().Where(m => m.Type.IsPrimitive).OrderBy(m => m.Name))
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
		if (CurrentClass is not null)
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

				foreach (var schemaMember in CurrentClass.GetMembers().ToCollection())
				{
					string name = schemaMember.Name;
					if (ImGui.Button($"X##deleteMember{name}", new Vector2(frameHeight, 0)))
					{
						schemaMember.TryRemove();
					}

					ImGui.SameLine();
					ImGui.SetNextItemWidth(FieldWidth);
					ImGui.InputText($"##{name}", ref name, 64, ImGuiInputTextFlags.ReadOnly);
					ImGui.SameLine();
					if (CurrentSchema is not null)
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
				using (Theme.Color(Theme.Palette.Error))
				{
					Text.PrintWithTheme("Schema has not been saved. Save it before configuring relative paths.");

					if (ImGui.Button("Save Now"))
					{
						SaveAs();
					}
				}
				return;
			}

			Text.PrintWithTheme($"Schema Path: {CurrentSchema.FilePath}");

			bool projectRootIsSet = ValidateProjectRootIsSet();
			ShowSetProjectRoot();
			bool schemaLocationIsValid = ValidateSchemaLocation();

			if (projectRootIsSet && schemaLocationIsValid)
			{
				var colors = ImGui.GetStyle().Colors;
				var backgroundColor = ImGuiStyler.Color.FromVector(colors[(int)ImGuiCol.WindowBg]);
				var textColor = backgroundColor.CalculateOptimalTextColorForContrast();

				Text.PrintWithTheme($"Data Path: {CurrentSchema.RelativePaths.RelativeDataSourcePath}");
				ImGui.SameLine();
				if (ImGui.Button("Set Data Path"))
				{
					Popups.OpenBrowserDirectory("Select Data Path", (path) =>
					{
						CurrentSchema.RelativePaths.RelativeDataSourcePath = path.RelativeTo(CurrentSchema.ProjectRootPath);
					});
				}
			}
		}
	}

	private bool ValidateProjectRootIsSet()
	{
		if (string.IsNullOrEmpty(CurrentSchema?.RelativePaths.RelativeProjectRootPath))
		{
			using (Theme.Color(Theme.Palette.Warning))
			{
				Text.PrintWithTheme("Set the path of the project's root directory.");
			}
			return false;
		}

		return true;
	}

	private void ShowSetProjectRoot()
	{
		if (CurrentSchema is not null)
		{
			Text.PrintWithTheme($"Project Root Path: {CurrentSchema.RelativePaths.RelativeProjectRootPath}");
			ImGui.SameLine();
			if (ImGui.Button("Set Project Root"))
			{
				Popups.OpenBrowserDirectory("Select Project Root", (path) =>
				{
					CurrentSchema.RelativePaths.RelativeProjectRootPath = path.RelativeTo(CurrentSchema.FilePath);
				});
			}
		}
	}

	private bool ValidateSchemaLocation()
	{
		if (CurrentSchema is not null)
		{
			var absoluteSchemaPath = (AbsoluteFilePath)Path.GetFullPath(CurrentSchema.FilePath);
			var expectedProjectRoot = (AbsoluteDirectoryPath)Path.GetFullPath(CurrentSchema.FilePath.DirectoryPath / CurrentSchema.RelativePaths.RelativeProjectRootPath);
			var expectedRelativeSchemaPath = (RelativeFilePath)absoluteSchemaPath.WeakString.RemovePrefix(expectedProjectRoot);
			var expectedSchemaPath = expectedProjectRoot / expectedRelativeSchemaPath;
			if (Path.GetFullPath(expectedSchemaPath) != Path.GetFullPath(absoluteSchemaPath))
			{
				using (Theme.Color(Theme.Palette.Error))
				{
					Text.PrintWithTheme("Schema appears to have been moved.");
					Text.PrintWithTheme("Reset the path of the project's root directory.");
				}

				Text.PrintWithTheme($"Expected: {expectedSchemaPath}");
				Text.PrintWithTheme($"Actual: {absoluteSchemaPath}");

				return false;
			}
		}

		return true;
	}
}
