using ImGuiNET;
using System.Diagnostics;
using System.Numerics;
using System.Windows.Forms;
using ktsu.io.ReusableWinForms;
using ktsu.io.StrongPaths;

namespace ktsu.io.SchemaTools
{
	public class SchemaEditor
	{
		internal Schema? CurrentSchema { get; set; }
		internal SchemaClass? CurrentClass { get; set; }
		internal SchemaEditorOptions Options { get; } = new();
		private static float FieldWidth => ImGui.GetIO().DisplaySize.X * 0.15f;

		private DividerContainer DividerContainerCols { get; } = new("RootDivider");

		[STAThread]
		private static void Main(string[] _)
		{
			SchemaEditor schemaEditor = new();
			ImGuiApp.Start(nameof(SchemaEditor), schemaEditor.Options.WindowState, schemaEditor.DividerContainerCols.Tick, schemaEditor.Menu, schemaEditor.Resized);
		}

		public SchemaEditor()
		{
			Options = SchemaEditorOptions.LoadOrCreate();
			RestoreOpenSchema();
			DividerContainerCols.Add("Left", 0.25f, ShowLeftPanel);
			DividerContainerCols.Add("Right", 0.75f, ShowRightPanel);
		}

		private void Resized() => Options.Save(this);

		private void ShowLeftPanel(float dt)
		{
			ShowEnums();
			ShowClasses();
		}

		private void ShowRightPanel(float dt)
		{
			ShowMembers();
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
			Options.Save(this);
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
					CurrentClass = CurrentSchema.GetFirstClass();
					Options.Save(this);
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
				Options.Save(this);
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
						Options.Save(this);
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
						Options.Save(this);
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
						Options.Save(this);
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
				if (ImGui.CollapsingHeader($"{Path.GetFileName(CurrentSchema.FilePath)} Enums", ImGuiTreeNodeFlags.DefaultOpen))
				{
					ImGui.Indent();
					ShowNewEnum();
					ImGui.NewLine();
					foreach (var schemaEnum in CurrentSchema.Enums.OrderBy(e => e.EnumName).ToList())
					{
						string enumName = schemaEnum.EnumName;
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
		}

		private void ShowClasses()
		{
			if (CurrentSchema != null)
			{
				if (ImGui.CollapsingHeader($"{Path.GetFileName(CurrentSchema.FilePath)} Classes", ImGuiTreeNodeFlags.DefaultOpen))
				{
					ImGui.Indent();
					ShowNewClass();
					ImGui.NewLine();
					foreach (var schemaClass in CurrentSchema.Classes.OrderBy(c => c.ClassName).ToList())
					{
						if (ImGui.Button($"X##deleteClass{schemaClass.ClassName}", new Vector2(ImGui.GetFrameHeight(), 0)))
						{
							CurrentSchema.Classes.Remove(schemaClass);
						}

						ImGui.SameLine();
						ImGui.SetNextItemWidth(FieldWidth);
						if (ImGui.Button($"{schemaClass.ClassName}", new Vector2(FieldWidth, 0)))
						{
							CurrentClass = schemaClass;
							Options.Save(this);
						}
					}

					ImGui.Unindent();
					ImGui.NewLine();
				}
			}
		}

		public static void ShowMemberConfig(Schema schema, SchemaMember schemaMember)
		{
			ImGui.Button(MakeTypenameDisplay(schemaMember), new Vector2(FieldWidth, 0));
			if (ImGui.BeginPopupContextItem($"##{schemaMember.MemberName}Typename", ImGuiPopupFlags.MouseButtonLeft))
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
				foreach (var schemaClass in schema.Classes.OrderBy(c => c.ClassName))
				{
					if (ImGui.Selectable(schemaClass.ClassName))
					{
						schemaMember.SetType(new Schema.Types.Object()
						{
							ClassName = schemaClass.ClassName,
						});
					}
				}

				ImGui.Separator();
				if (ImGui.BeginMenu(nameof(Schema.Types.Enum), schema.Enums.Any()))
				{
					foreach (var schemaEnum in schema.Enums.OrderBy(e => e.EnumName))
					{
						if (ImGui.Selectable(schemaEnum.EnumName))
						{
							schemaMember.SetType(new Schema.Types.Enum()
							{
								EnumName = schemaEnum.EnumName,
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
						if (ImGui.Selectable(schemaClass.ClassName))
						{
							schemaMember.SetType(new Schema.Types.Array()
							{
								ElementType = new Schema.Types.Object()
								{
									ClassName = schemaClass.ClassName,
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
				ImGui.InputText($"##Container{schemaMember.MemberName}", ref container, 64);
				array.Container = (ContainerName)container;

				if (array.ElementType is Schema.Types.Object obj && obj.Class is not null)
				{
					ImGui.SameLine();
					ImGui.Button(array.Key, new Vector2(FieldWidth, 0));
					if (ImGui.BeginPopupContextItem($"##{schemaMember.MemberName}Key", ImGuiPopupFlags.MouseButtonLeft))
					{
						if (ImGui.Selectable("<none>"))
						{
							array.Key = new();
						}

						foreach (var primitiveMember in obj.Class.Members.Where(m => m.Type.IsPrimitive).OrderBy(m => m.MemberName))
						{
							if (ImGui.Selectable(primitiveMember.MemberName))
							{
								array.Key = primitiveMember.MemberName;
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
				if (ImGui.CollapsingHeader($"{CurrentClass.ClassName} Members", ImGuiTreeNodeFlags.DefaultOpen))
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
						string name = schemaMember.MemberName;
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
	}
}
