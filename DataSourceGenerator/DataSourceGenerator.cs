﻿namespace ktsu.io.SchemaTools
{
	using ktsu.io.StrongPaths;
	using ktsu.io.CodeBlocker;

	internal class DataSourceGenerator
	{
		private const string InputPathKey = "Data";
		private const string OutputPathKey = "DataSourceGeneratorOutput";
		private const string ProjectPathKey = "DataSourceGeneratorProject";
		//private const string PublishedDataKey = "PublishedData";

		private const string GeneratedSuffixHeader = ".gen.h";
		private const string GeneratedSuffixCode = ".gen.cpp";

		private const string FileHeader = "// Auto-generated by DataSourceGenerator - Do not manually edit!";

		private static string? InputPath;
		private static string? OutputPath;
		private static string? ProjectPath;
		//private static string? PublishedDataPath;

		private static void Main(string[] args)
		{
			var paths = Pathfinder.Paths;
			if (!paths.TryGetValue(InputPathKey, out InputPath))
			{
				throw new KeyNotFoundException($"Could not retrieve the path: {InputPathKey}");
			}

			if (!paths.TryGetValue(OutputPathKey, out OutputPath))
			{
				throw new KeyNotFoundException($"Could not retrieve the path: {OutputPathKey}");
			}

			if (!paths.TryGetValue(ProjectPathKey, out ProjectPath))
			{
				throw new KeyNotFoundException($"Could not retrieve the path: {ProjectPathKey}");
			}

			//if (!paths.TryGetValue(PublishedDataKey, out PublishedDataPath))
			//{
			//	throw new Exception($"Could not retrieve the path: {PublishedDataKey}");
			//}

			if (args.Length >= 1)
			{
				InputPath = Path.Combine(Pathfinder.ProjectRoot, args.First());
			}

			if (args.Length >= 2)
			{
				OutputPath = Path.Combine(Pathfinder.ProjectRoot, args.Skip(1).First());
			}

			if (args.Length >= 3)
			{
				ProjectPath = Path.Combine(Pathfinder.ProjectRoot, args.Skip(2).First());
			}

			Console.WriteLine($"Loading project: {ProjectPath}");
			var project = ProjectLib.Load(ProjectPath);
			var filters = ProjectLib.Load($"{ProjectPath}.filters");
			project.PurgeCPPFiles(f => f.EndsWith(GeneratedSuffixHeader) || f.EndsWith(GeneratedSuffixCode));
			filters.PurgeCPPFiles(f => f.EndsWith(GeneratedSuffixHeader) || f.EndsWith(GeneratedSuffixCode));

			var files = new List<string>();
			Pathfinder.GatherFilesRecursively(InputPath, f => f.EndsWith(DataSource.FileSuffix), files);

			var headerList = new List<string>();
			var classList = new List<string>();
			foreach (string p in files)
			{
				var dataFilePath = (FilePath)p;
				Console.WriteLine($"Reading: {dataFilePath.FileName}");
				if (DataSource.TryLoad(dataFilePath, out var dataSource) && dataSource is not null)
				{
					using (var code = new CodeGenerator())
					{
						string filename = $"{dataSource.Name}.gen.h";
						headerList.Add(filename);
						string filePath = Path.Combine(OutputPath, filename);
						var relativePath = AnyRelativePath.Make<RelativePath>((FilePath)ProjectPath, (FilePath)filePath);
						Console.WriteLine($"Generating: {filename}");
						GenerateDataSourceHeader(dataSource, code);
						File.WriteAllText(filePath, code.ToString());
						project.AddCPPFile(relativePath);
						filters.AddCPPFile(relativePath);
					}

					using (var code = new CodeGenerator())
					{
						classList.Add(dataSource.Name);
						string filename = $"{dataSource.Name}.gen.cpp";
						string filePath = Path.Combine(OutputPath, filename);
						string relativePath = Pathfinder.GetRelativePath(ProjectPath, filePath);
						Console.WriteLine($"Generating: {filename}");
						GenerateDataSourceCode(dataSource, code);
						File.WriteAllText(filePath, code.ToString());
						project.AddCPPFile(relativePath);
						filters.AddCPPFile(relativePath);
					}
				}
			}

			using (var code = new CodeGenerator())
			{
				string filename = $"DataSources.gen.h";
				string filePath = Path.Combine(OutputPath, filename);
				string relativePath = Pathfinder.GetRelativePath(ProjectPath, filePath);
				Console.WriteLine($"Generating: {filename}");
				code.WriteLine("#pragma once");
				code.WriteLine(FileHeader);
				headerList.ForEach(h => code.WriteLine($"#include \"{h}\""));
				code.NewLine();
				code.WriteLine("#include <string>");
				code.WriteLine("#include <functional>");
				code.NewLine();
				code.WriteLine("namespace Json { class Value; }");
				code.NewLine();
				code.WriteLine("namespace DataSources");
				using (new Scope(code))
				{
					code.WriteLine("using string = std::string;");
					code.NewLine();
					code.WriteLine("struct Data");
					using (new Scope(code))
					{
						code.WriteLine($"Data(std::function<Json::Value(const string&)> reader, std::function<void(const string&, Json::Value&)> writer) : ");
						var initializerList = new List<string>();
						classList.ForEach(c => initializerList.Add($"{Schema.LowerCaseFirst(c)}(reader, writer)"));

						code.Indent++;
						string indentString = "";
						for (int i = 0; i < code.Indent; ++i)
						{
							indentString += "\t";
						}

						string initializerString = string.Join($",\r\n{indentString}", initializerList);
						code.WriteLine(initializerString);
						code.Indent--;
						code.WriteLine("{}");
						code.NewLine();
						classList.ForEach(c => code.WriteLine($"static bool ShowDataSource{c};"));
						code.NewLine();
						code.WriteLine($"void ImGuiMenu();");
						code.WriteLine($"void ImGui();");
						code.WriteLine($"void Load();");

						code.NewLine();
						classList.ForEach(c => code.WriteLine($"{c} {Schema.LowerCaseFirst(c)};"));
					}
				}

				File.WriteAllText(filePath, code.ToString());
				project.AddCPPFile(relativePath);
				filters.AddCPPFile(relativePath);
			}

			using (var code = new CodeGenerator())
			{
				string filename = $"DataSources.gen.cpp";
				string filePath = Path.Combine(OutputPath, filename);
				string relativePath = Pathfinder.GetRelativePath(ProjectPath, filePath);
				Console.WriteLine($"Generating: {filename}");

				code.WriteLine($"#include \"DataSources.gen.h\"");
				code.NewLine();
				code.WriteLine("#include <imgui.h>");
				code.NewLine();
				code.WriteLine("namespace DataSources");
				using (new Scope(code))
				{
					classList.ForEach(c => code.WriteLine($"/*static*/ bool Data::ShowDataSource{c} = false;"));
					code.NewLine();
					code.WriteLine($"void Data::ImGuiMenu()");
					using (new Scope(code))
					{
						code.WriteLine($"if(ImGui::BeginMenu(\"DataSources\"))");
						using (new Scope(code))
						{
							classList.ForEach(c => code.WriteLine($"ImGui::MenuItem(\"{c}\", \"\", &ShowDataSource{c});"));

							code.WriteLine($"ImGui::EndMenu();");
						}
					}

					code.NewLine();
					code.WriteLine($"void Data::ImGui()");
					using (new Scope(code))
					{
						classList.ForEach(c => code.WriteLine($"{Schema.LowerCaseFirst(c)}.ImGui(ShowDataSource{c});"));
					}

					code.NewLine();
					code.WriteLine($"void Data::Load()");
					using (new Scope(code))
					{
						classList.ForEach(c => code.WriteLine($"{Schema.LowerCaseFirst(c)}.Deserialize();"));
					}
				}

				File.WriteAllText(filePath, code.ToString());
				project.AddCPPFile(relativePath);
				filters.AddCPPFile(relativePath);
			}

			project.Save();
			filters.Save();
		}

		private static void GenerateDataSourceHeader(DataSource dataSource, CodeBlocker code)
		{
			var array = dataSource.RootSchemaMember.Type as Schema.Types.Array;

			code.WriteLine("#pragma once");
			code.WriteLine(FileHeader);
			var container = array?.Container ?? new();
			if (!string.IsNullOrEmpty(container))
			{
				code.WriteLine($"#include \"{container}.h\"");
			}

			code.WriteLine("#include <string>");
			code.WriteLine("#include <vector>");
			code.WriteLine("#include <functional>");
			code.NewLine();
			code.WriteLine("namespace Json { class Value; }");
			var rootTypename = dataSource.RootSchemaMember.Type;
			if (rootTypename.IsObject)
			{
				code.WriteLine($"namespace SchemaClasses {{ class {rootTypename}; }}");
			}
			else if (array != null && array.IsComplexArray)
			{
				code.WriteLine($"namespace SchemaClasses {{ class {array.ElementType}; }}");
			}

			code.NewLine();
			code.WriteLine("namespace DataSources");
			using (new Scope(code))
			{
				code.WriteLine("using string = std::string;");
				code.WriteLine("template<class T>");
				code.WriteLine("using vector = std::vector<T>;");
				code.NewLine();
				code.WriteLine($"class {dataSource.Name}");
				using (new Scope(code))
				{
					code.Indent--;
					code.WriteLine("private:");
					code.Indent++;
					code.WriteLine($"std::function<Json::Value(const string&)> ReadDelegate{{nullptr}};");
					code.WriteLine($"std::function<void(const string&, Json::Value&)> WriteDelegate{{nullptr}};");
					code.Indent--;
					code.WriteLine("public:");
					code.Indent++;
					code.WriteLine($"static const string FilePath;");
					code.WriteLine($"{dataSource.Name}(std::function<Json::Value(const string&)> reader, std::function<void(const string&, Json::Value&)> writer) : ReadDelegate(reader), WriteDelegate(writer){{}}");
					code.WriteLine($"void Deserialize();");
					code.WriteLine($"void Serialize();");
					code.WriteLine($"void Destroy();");
					code.WriteLine($"void ImGui(bool& open);");
					GenerateRoot(dataSource.RootSchemaMember, code);
				}
			}
		}

		private static void GenerateRoot(SchemaMember schemaMember, CodeBlocker code)
		{
			if (schemaMember.Type is Schema.Types.Array array)
			{
				string elementTypeString = $"SchemaClasses::{array.ElementType}";
				var container = array.Container;
				if (string.IsNullOrEmpty(container))
				{
					container = ContainerName.Vector;
				}

				if (array.TryGetKeyMember(out var keyMember) && keyMember != null)
				{
					elementTypeString = $"{keyMember.Type}, {elementTypeString}";
				}

				string pointer = array.ElementType.IsObject ? "*" : string.Empty;
				code.WriteLine($"{container}<{elementTypeString}{pointer}> {schemaMember.MemberName};");
			}
			else if (schemaMember.Type.IsObject)
			{
				code.WriteLine($"SchemaClasses::{schemaMember.Type}* Root = nullptr;");
			}
		}

		private static void GenerateDataSourceCode(DataSource dataSource, CodeBlocker code)
		{
			if (InputPath != null)
			{
				string publishedFilePathRelative = Pathfinder.GetRelativePath(InputPath, dataSource.FilePath).Replace("\\", "\\\\");
				code.WriteLine(FileHeader);
				code.WriteLine($"#include \"{dataSource.Name}.gen.h\"");
				code.WriteLine($"#include \"ColumnStack.gen.h\"");
				GenerateRootInclude(dataSource.RootSchemaMember, code);
				code.NewLine();
				code.WriteLine("#include <json/json.h>");
				code.WriteLine("#include <imgui.h>");
				code.WriteLine("#include <imgui/misc/cpp/imgui_stdlib.h>");
				code.NewLine();
				code.WriteLine("using namespace SchemaClasses;");
				code.WriteLine("using namespace DataSources;");
				code.WriteLine("using namespace std;");
				code.NewLine();
				code.WriteLine($"/*static*/ const string {dataSource.Name}::FilePath = \"{publishedFilePathRelative}\";");
				code.NewLine();
				code.WriteLine($"void {dataSource.Name}::Deserialize()");
				using (new Scope(code))
				{
					code.WriteLine("Destroy();");
					code.WriteLine("if(ReadDelegate != nullptr)");
					using (new Scope(code))
					{
						code.WriteLine("auto jsonObj = ReadDelegate(FilePath);");
						SchemaClassGenerator.GenerateDeserializeMember(dataSource.RootSchemaMember, code);
					}
				}

				code.NewLine();
				code.WriteLine($"void {dataSource.Name}::Serialize()");
				using (new Scope(code))
				{
					code.WriteLine("if(WriteDelegate != nullptr)");
					using (new Scope(code))
					{
						code.WriteLine("auto jsonObj = Json::Value(Json::objectValue);");
						SchemaClassGenerator.GenerateSerializeMember(dataSource.RootSchemaMember, code);
						code.WriteLine("WriteDelegate(FilePath, jsonObj);");
					}
				}

				code.NewLine();
				code.WriteLine($"void {dataSource.Name}::Destroy()");
				using (new Scope(code))
				{
					SchemaClassGenerator.GenerateDestroyMember(dataSource.RootSchemaMember, code);
				}

				code.NewLine();
				code.WriteLine($"void {dataSource.Name}::ImGui(bool& open)");
				using (new Scope(code))
				{
					code.WriteLine($"if(open && ImGui::Begin(\"DataSources::{dataSource.Name}\", &open))");
					using (new Scope(code))
					{
						code.WriteLine($"if(ImGui::Button(\"Load\"))");
						using (new Scope(code))
						{
							code.WriteLine($"Deserialize();");
						}

						code.WriteLine($"ImGui::SameLine();");
						code.WriteLine($"if(ImGui::Button(\"Save\"))");
						using (new Scope(code))
						{
							code.WriteLine($"Serialize();");
						}

						SchemaClassGenerator.GenerateImGuiEditField(dataSource.RootSchemaMember, code);
						code.WriteLine($"ImGui::End();");
					}
				}
			}
		}

		private static void GenerateRootInclude(SchemaMember schemaMember, CodeBlocker code)
		{
			if (schemaMember.Type is Schema.Types.Array array && array.IsComplexArray)
			{
				code.WriteLine($"#include \"{array.ElementType}.gen.h\"");
			}
			else if (schemaMember.Type.IsObject)
			{
				code.WriteLine($"#include \"{schemaMember.Type}.gen.h\"");
			}
		}
	}
}