using ImGuiNET;
using ktsu.io;
using ktsu.io.ReusableWinForms;
using System.Diagnostics;
using System.Numerics;
using System.Windows.Forms;

namespace medmondson
{
	public class SchemaEditor
	{
		internal Schema? CurrentSchema { get; set; }
		internal SchemaClass? CurrentClass { get; set; }
		internal SchemaEditorOptions Options { get; } = new();
		private static float FieldWidth => ImGui.GetIO().DisplaySize.X * 0.15f;

		[STAThread]
		private static void Main(string[] _)
		{
			SchemaEditor schemaEditor = new();
			schemaEditor.Options.Load(schemaEditor);
			ImGuiApp.Start(nameof(SchemaEditor), schemaEditor.Options.WindowState, schemaEditor.Tick, schemaEditor.Menu, schemaEditor.Resized);
		}

		private void Resized() => Options.Save(this);

		private void Tick(float dt)
		{
			ShowReferences();
			ShowEnums();
			ShowClasses();
			ShowMembers();
		}

		private void Reset()
		{
			CurrentSchema = null;
			CurrentClass = null;
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

		public static string GetSchemaPath()
		{
			var paths = Pathfinder.Paths;
			const string SchemaPathKey = "Schema";
			if (!paths.TryGetValue(SchemaPathKey, out string? schemaPath))
			{
				throw new ArgumentException("Could not retrieve the path", SchemaPathKey);
			}

			return schemaPath;
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
			fileDialog.InitialDirectory = GetSchemaPath();
			fileDialog.Filter = "schema files (*.schema.json)|*.schema.json|All files (*.*)|*.*";
			fileDialog.RestoreDirectory = true;

			if (fileDialog.ShowDialog() == DialogResult.OK)
			{
				Reset();
				if (Schema.TryLoad(fileDialog.FileName, out var schema) && schema != null)
				{
					CurrentSchema = schema;
					CurrentClass = CurrentSchema.LocalClasses.FirstOrDefault();
					Options.Save(this);
				}
			}
		}

		private void Save()
		{
			if (string.IsNullOrEmpty(CurrentSchema?.FilePath))
			{
				SaveAs();
				return;
			}

			CurrentSchema.Save();
		}

		private void SaveAs()
		{
			using var fileDialog = new SaveFileDialog();
			fileDialog.InitialDirectory = GetSchemaPath();
			fileDialog.Filter = "schema files (*.schema.json)|*.schema.json|All files (*.*)|*.*";
			fileDialog.RestoreDirectory = true;

			if (fileDialog.ShowDialog() == DialogResult.OK)
			{
				if (CurrentSchema != null)
				{
					CurrentSchema.FilePath = fileDialog.FileName;
				}

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
					string result = TextPrompt.Show("New Enum Name?");
					var newEnumName = (Schema.EnumName)result;
					if (CurrentSchema.Enums.ContainsKey(newEnumName))
					{
						MessageBox.Show("An Enum with that name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
					}
					else if (!string.IsNullOrEmpty(newEnumName))
					{

						CurrentSchema.LocalEnums.Add(newEnumName, new());
						Options.Save(this);
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
					string newClassName = TextPrompt.Show("New Class Name?");
					if (string.IsNullOrEmpty(newClassName) || CurrentSchema.LocalClasses.Any(c => c.Name == newClassName))
					{
						//TODO: throw an error popup
						throw new NotImplementedException();
					}
					else
					{
						var schemaClass = new SchemaClass()
						{
							Name = (Schema.ClassName)newClassName,
						};
						CurrentSchema.LocalClasses.Add(schemaClass);
						CurrentClass = schemaClass;
						Options.Save(this);
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
					string newMemberName = TextPrompt.Show("New Member Name?");
					if (string.IsNullOrEmpty(newMemberName) || CurrentClass.Members.Any(m => m.Name == newMemberName))
					{
						//TODO: throw an error popup
					}
					else
					{
						var schemaMember = new SchemaMember()
						{
							Name = (Schema.MemberName)newMemberName,
						};
						CurrentClass.Members.Add(schemaMember);
						Options.Save(this);
					}
				}
			}
		}

		private void ShowNewReference()
		{
			if (CurrentSchema != null)
			{
				if (ImGui.Button("Add Reference", new Vector2(FieldWidth, 0)))
				{
					if (!string.IsNullOrEmpty(CurrentSchema.FilePath))
					{
						using var fileDialog = new OpenFileDialog();
						fileDialog.InitialDirectory = GetSchemaPath();
						fileDialog.Filter = "schema files (*.schema.json)|*.schema.json|All files (*.*)|*.*";
						fileDialog.RestoreDirectory = true;

						if (fileDialog.ShowDialog() == DialogResult.OK)
						{
							string relativePath = Pathfinder.GetRelativePath(CurrentSchema.FilePath, fileDialog.FileName);
							CurrentSchema.References.Add(relativePath);
							CurrentSchema.LoadReferences();
						}
					}
					else
					{
						MessageBox.Show("Save the schema before adding references.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
					}
				}
			}
		}

		private void ShowReferences()
		{
			if (CurrentSchema != null)
			{
				if (ImGui.CollapsingHeader($"{Path.GetFileName(CurrentSchema.FilePath)} References", ImGuiTreeNodeFlags.DefaultOpen))
				{
					ImGui.Indent();
					ShowNewReference();
					ImGui.NewLine();
					foreach (string? reference in CurrentSchema.References.OrderBy(r => r).ToList())
					{
						string refFilename = Path.GetFileName(reference);
						if (ImGui.Button($"X##deleteReference{reference}", new Vector2(ImGui.GetFrameHeight(), 0)))
						{
							CurrentSchema.References.Remove(reference);
							CurrentSchema.LoadReferences();
						}

						ImGui.SameLine();
						ImGui.SetNextItemWidth(FieldWidth);
						ImGui.InputText($"##Reference{reference}", ref refFilename, 64, ImGuiInputTextFlags.ReadOnly);
					}

					ImGui.Unindent();
					ImGui.NewLine();
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
					foreach (var kvp in CurrentSchema.LocalEnums.OrderBy(e => e.Key).ToList())
					{
						string enumName = kvp.Key.ToString();
						if (ImGui.Button($"X##deleteEnum{enumName}", new Vector2(ImGui.GetFrameHeight(), 0)))
						{
							CurrentSchema.LocalEnums.Remove(kvp.Key);
						}

						ImGui.SameLine();
						ImGui.SetNextItemWidth(FieldWidth);
						ImGui.InputText($"##Enum{enumName}", ref enumName, 64, ImGuiInputTextFlags.ReadOnly);
						ImGui.SameLine();
						if (ImGui.Button($"+##addEnumValue{enumName}", new Vector2(ImGui.GetFrameHeight(), 0)))
						{
							string result = TextPrompt.Show("New Enum Value?");
							var newValue = (Schema.EnumValueName)result;
							if (string.IsNullOrEmpty(newValue))
							{
								//TODO: throw an error popup
								throw new NotImplementedException();
							}
							else
							{
								kvp.Value.Add(newValue);
							}
						}

						ImGui.Indent();
						foreach (var val in kvp.Value.ToList())
						{
							string enumValue = val.ToString();
							if (ImGui.Button($"X##deleteEnumValue{enumName}{enumValue}", new Vector2(ImGui.GetFrameHeight(), 0)))
							{
								kvp.Value.Remove(val);
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

				if (ImGui.CollapsingHeader($"External Enums", ImGuiTreeNodeFlags.DefaultOpen))
				{
					ImGui.Indent();
					foreach (var kvp in CurrentSchema.ReferencedEnums.OrderBy(e => e.Key))
					{
						string enumName = kvp.Key.ToString();
						ImGui.SetNextItemWidth(FieldWidth);
						ImGui.InputText($"##Enum{enumName}", ref enumName, 64, ImGuiInputTextFlags.ReadOnly);
					}

					ImGui.Unindent();
					ImGui.NewLine();
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
					foreach (var schemaClass in CurrentSchema.LocalClasses.OrderBy(c => c.Name).ToList())
					{
						if (ImGui.Button($"X##deleteClass{schemaClass.Name}", new Vector2(ImGui.GetFrameHeight(), 0)))
						{
							CurrentSchema.LocalClasses.Remove(schemaClass);
						}

						ImGui.SameLine();
						ImGui.SetNextItemWidth(FieldWidth);
						if (ImGui.Button($"{schemaClass.Name}", new Vector2(FieldWidth, 0)))
						{
							CurrentClass = schemaClass;
							Options.Save(this);
						}
					}

					ImGui.Unindent();
					ImGui.NewLine();
				}

				if (ImGui.CollapsingHeader($"External Classes", ImGuiTreeNodeFlags.DefaultOpen))
				{
					ImGui.Indent();
					foreach (var schemaClass in CurrentSchema.ReferencedClasses.OrderBy(c => c.Name))
					{
						string name = schemaClass.Name;
						ImGui.SetNextItemWidth(FieldWidth);
						ImGui.SetNextItemWidth(FieldWidth);
						ImGui.InputText($"##Class{name}", ref name, 64, ImGuiInputTextFlags.ReadOnly);
					}

					ImGui.Unindent();
					ImGui.NewLine();
				}
			}
		}

		public static void ShowMemberConfig(Schema schema, SchemaMember schemaMember)
		{
			ImGui.Button(MakeTypenameDisplay(schemaMember), new Vector2(FieldWidth, 0));
			if (ImGui.BeginPopupContextItem($"##{schemaMember.Name}Typename", ImGuiPopupFlags.MouseButtonLeft))
			{
				foreach (var type in Schema.Types.Primitives)
				{
					if (ImGui.Selectable(type.Name))
					{
						if (Activator.CreateInstance(type) is Schema.Types.BaseType newType)
						{
							schemaMember.Type = newType;
						}
					}
				}

				ImGui.Separator();
				foreach (var schemaClass in schema.Classes.OrderBy(c => c.Name))
				{
					if (ImGui.Selectable(schemaClass.Name))
					{
						schemaMember.Type = new Schema.Types.Object(schemaClass);
					}
				}

				ImGui.Separator();
				if (ImGui.BeginMenu(nameof(Schema.Types.Enum), schema.Enums.Any()))
				{
					foreach (var kvp in schema.Enums.OrderBy(kvp => kvp.Key))
					{
						if (ImGui.Selectable(kvp.Key))
						{
							schemaMember.Type = new Schema.Types.Enum(kvp.Key);
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
								schemaMember.Type = new Schema.Types.Array(newType);
							}
						}
					}

					ImGui.Separator();
					foreach (var schemaClass in schema.Classes)
					{
						if (ImGui.Selectable(schemaClass.Name))
						{
							schemaMember.Type = new Schema.Types.Array(new Schema.Types.Object(schemaClass));
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
				array.Container = container;

				if (array.ElementType is Schema.Types.Object obj)
				{
					ImGui.SameLine();
					ImGui.Button(array.Key, new Vector2(FieldWidth, 0));
					if (ImGui.BeginPopupContextItem($"##{schemaMember.Name}Key", ImGuiPopupFlags.MouseButtonLeft))
					{
						if (ImGui.Selectable("<none>"))
						{
							array.Key = (Schema.MemberName)string.Empty;
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
						if (ImGui.Button($"X##deleteMember{schemaMember.Name}", new Vector2(frameHeight, 0)))
						{
							CurrentClass.Members.Remove(schemaMember);
						}

						ImGui.SameLine();
						ImGui.SetNextItemWidth(FieldWidth);
						ImGui.InputText($"##{schemaMember.Name}", ref name, 64, ImGuiInputTextFlags.ReadOnly);
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
				return $"{nameof(Schema.Types.Enum)}({enumType.Name})";
			}

			return schemaMember.Type.ToString();
		}
	}
}
