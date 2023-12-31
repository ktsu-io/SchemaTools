﻿namespace ktsu.io.SchemaTools
{
	using ktsu.io.StrongPaths;

	public static class SchemaClassGenerator
	{
		private const string InputPathKey = "Schema";
		private const string OutputPathKey = "SchemaClassGeneratorOutput";
		private const string ProjectPathKey = "SchemaClassGeneratorProject";

		private const string GeneratedSuffixHeader = ".gen.h";
		private const string GeneratedSuffixCode = ".gen.cpp";

		private const string FileHeader = "// Auto-generated by SchemaClassGenerator - Do not manually edit!";

		private static string? InputPath;
		private static string? OutputPath;
		private static string? ProjectPath;

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

			if (args.Length >= 1)
			{
				string arg = args.First();
				if (!arg.StartsWith('-'))
				{
					InputPath = Path.Combine(Pathfinder.ProjectRoot, arg);
				}
			}

			if (args.Length >= 2)
			{
				string arg = args.Skip(1).First();
				if (!arg.StartsWith('-'))
				{
					OutputPath = Path.Combine(Pathfinder.ProjectRoot, arg);
				}
			}

			if (args.Length >= 3)
			{
				string arg = args.Skip(2).First();
				if (!arg.StartsWith('-'))
				{
					ProjectPath = Path.Combine(Pathfinder.ProjectRoot, arg);
				}
			}

			Console.WriteLine($"Loading project: {ProjectPath}");
			var project = ProjectLib.Load(ProjectPath);
			var filters = ProjectLib.Load($"{ProjectPath}.filters");
			project.PurgeCPPFiles(f => f.EndsWith(GeneratedSuffixHeader) || f.EndsWith(GeneratedSuffixCode));
			filters.PurgeCPPFiles(f => f.EndsWith(GeneratedSuffixHeader) || f.EndsWith(GeneratedSuffixCode));

			var files = new List<string>();
			Pathfinder.GatherFilesRecursively(InputPath, f => f.EndsWith(Schema.FileSuffix), files);

			Console.WriteLine("Generating Schema Classes");

			foreach (string schemaFilePath in files)
			{
				Console.WriteLine($"Reading: {Path.GetFileName(schemaFilePath)}");
				if (Schema.TryLoad((FilePath)schemaFilePath, out var schema) && schema != null)
				{
					foreach (var schemaEnum in schema.Enums)
					{
						using (var code = new CodeGenerator())
						{
							string filename = $"Enum{schemaEnum.EnumName}.gen.h";
							string filePath = Path.Combine(OutputPath, filename);
							string relativePath = Pathfinder.GetRelativePath(ProjectPath, filePath);
							Console.WriteLine($"Generating: {filename}");
							GenerateEnumHeader(schemaEnum, code);
							File.WriteAllText(filePath, code.ToString());
							project.AddCPPFile(relativePath);
							filters.AddCPPFile(relativePath);
						}

						using (var code = new CodeGenerator())
						{
							string filename = $"Enum{schemaEnum.EnumName}.gen.cpp";
							string filePath = Path.Combine(OutputPath, filename);
							string relativePath = Pathfinder.GetRelativePath(ProjectPath, filePath);
							Console.WriteLine($"Generating: {filename}");
							GenerateEnumSource(schemaEnum, code);
							File.WriteAllText(filePath, code.ToString());
							project.AddCPPFile(relativePath);
							filters.AddCPPFile(relativePath);
						}
					}

					foreach (var schemaClass in schema.Classes)
					{
						using (var code = new CodeGenerator())
						{
							string filename = $"{schemaClass.ClassName}.gen.h";
							string filePath = Path.Combine(OutputPath, filename);
							string relativePath = Pathfinder.GetRelativePath(ProjectPath, filePath);
							Console.WriteLine($"Generating: {filename}");
							GenerateSchemaClassHeader(schemaClass, code);
							File.WriteAllText(filePath, code.ToString());
							project.AddCPPFile(relativePath);
							filters.AddCPPFile(relativePath);
						}

						using (var code = new CodeGenerator())
						{
							string filename = $"{schemaClass.ClassName}.gen.cpp";
							string filePath = Path.Combine(OutputPath, filename);
							string relativePath = Pathfinder.GetRelativePath(ProjectPath, filePath);
							Console.WriteLine($"Generating: {filename}");
							GenerateSchemaClassSource(schemaClass, code);
							File.WriteAllText(filePath, code.ToString());
							project.AddCPPFile(relativePath);
							filters.AddCPPFile(relativePath);
						}
					}
				}
			}

			if (!args.Contains("-NoColumnStack"))
			{
				GenerateColumnStackFiles(project, filters);
			}

			project.Save();
			filters.Save();
		}

		private static void GenerateColumnStackFiles(ProjectLib project, ProjectLib filters)
		{
			if (OutputPath == null || ProjectPath == null)
			{
				return;
			}

			using (var code = new CodeGenerator())
			{
				string filename = $"ColumnStack.gen.h";
				string filePath = Path.Combine(OutputPath, filename);
				string relativePath = Pathfinder.GetRelativePath(ProjectPath, filePath);
				Console.WriteLine($"Generating: {filename}");
				code.WriteLine("#pragma once");
				code.WriteLine(FileHeader);
				code.NewLine();
				code.WriteLine("#include <string>");
				code.WriteLine("#include <stack>");
				code.NewLine();
				code.WriteLine("namespace SchemaClasses");
				using (new Scope(code))
				{
					code.WriteLine("using string = std::string;");
					code.NewLine();
					code.WriteLine("class ColumnStack");
					using (new Scope(code))
					{
						code.WriteLine("public:");
						code.WriteLine("static void Push(int numColumns, string id);");
						code.WriteLine("static void PushSingle();");
						code.WriteLine("static void Pop();");
						code.NewLine();
						code.WriteLine("private:");
						code.WriteLine("static std::stack<std::pair<int, string>> Stack;");
						code.WriteLine("static int CurrentCols;");
						code.WriteLine("static string CurrentId;");
					}
				}

				File.WriteAllText(filePath, code.ToString());
				project.AddCPPFile(relativePath);
				filters.AddCPPFile(relativePath);
			}

			using (var code = new CodeGenerator())
			{
				string filename = $"ColumnStack.gen.cpp";
				string filePath = Path.Combine(OutputPath, filename);
				string relativePath = Pathfinder.GetRelativePath(ProjectPath, filePath);
				Console.WriteLine($"Generating: {filename}");

				code.WriteLine($"#include \"ColumnStack.gen.h\"");
				code.NewLine();
				code.WriteLine("#include <imgui.h>");
				code.NewLine();
				code.WriteLine("namespace SchemaClasses");
				using (new Scope(code))
				{
					code.WriteLine("/*static*/ std::stack<std::pair<int, string>> ColumnStack::Stack;");
					code.WriteLine("/*static*/ int ColumnStack::CurrentCols;");
					code.WriteLine("/*static*/ string ColumnStack::CurrentId;");
					code.NewLine();
					code.WriteLine("/*static*/ void ColumnStack::PushSingle() { Push(1, \"\"); }");
					code.NewLine();
					code.WriteLine("/*static*/ void ColumnStack::Push(int numColumns, string id)");
					using (new Scope(code))
					{
						code.WriteLine("Stack.push(std::make_pair(numColumns, id));");
						code.WriteLine("ImGui::Columns(1);");
						code.WriteLine("if(id.empty()) ImGui::Columns(numColumns);");
						code.WriteLine("else ImGui::Columns(numColumns, id.c_str());");
						code.WriteLine("CurrentCols = numColumns;");
						code.WriteLine("CurrentId = id;");
					}

					code.NewLine();
					code.WriteLine("/*static*/ void ColumnStack::Pop()");
					using (new Scope(code))
					{
						code.WriteLine("auto oldPair = Stack.top();");
						code.WriteLine("Stack.pop();");
						code.WriteLine("if(Stack.empty()) { ImGui::Columns(1); CurrentCols = 1; CurrentId = \"\"; }");
						code.WriteLine("else");
						using (new Scope(code))
						{
							code.WriteLine("auto pair = Stack.top();");
							code.WriteLine("ImGui::Columns(1);");
							code.WriteLine("if(pair.second.empty()) { ImGui::Columns(pair.first); CurrentId = \"\"; }");
							code.WriteLine("else { ImGui::Columns(pair.first, pair.second.c_str()); CurrentId = pair.second; }");
							code.WriteLine("CurrentCols = pair.first;");
						}
					}
				}

				File.WriteAllText(filePath, code.ToString());
				project.AddCPPFile(relativePath);
				filters.AddCPPFile(relativePath);
			}
		}

		private static void GenerateSchemaClassHeader(SchemaClass schemaClass, CodeGenerator code)
		{
			code.WriteLine("#pragma once");
			code.WriteLine(FileHeader);
			code.WriteLine($"#include \"ColumnStack.gen.h\"");
			var usedIncludes = new HashSet<string>();
			schemaClass.Members.ForEach(m => GenerateEnumIncludes(m, code, usedIncludes));
			schemaClass.Members.ForEach(m => GenerateContainerIncludes(m, code, usedIncludes));

			code.NewLine();
			code.WriteLine("#include <string>");
			code.WriteLine("#include <vector>");

			code.NewLine();
			code.WriteLine("namespace Json { class Value; }");

			code.NewLine();
			code.WriteLine("namespace SchemaClasses");
			using (new Scope(code))
			{
				code.WriteLine("using string = std::string;");
				code.WriteLine("template<class T>");
				code.WriteLine("using vector = std::vector<T>;");
				var usedDecls = new HashSet<string>();
				schemaClass.Members.ForEach(m => GenerateSchemaMemberFwdDecl(m, code, usedDecls));

				code.NewLine();
				code.WriteLine($"class {schemaClass.ClassName}");
				using (new Scope(code))
				{
					code.Indent--;
					code.WriteLine("public:");
					code.Indent++;
					code.WriteLine($"{schemaClass.ClassName}* DeepCopy() const;");
					code.WriteLine($"bool DeepEquals({schemaClass.ClassName}* other) const;");
					code.WriteLine("void Deserialize(Json::Value& jsonObj);");
					code.WriteLine("void Serialize(Json::Value& jsonObj) const;");
					code.WriteLine($"static {schemaClass.ClassName}* Make();");
					code.WriteLine("void Destroy();");
					code.WriteLine("void ImGui();");

					code.NewLine();
					schemaClass.Members.ForEach(m => GenerateSchemaMemberHeader(m, code));
				}
			}
		}

		private static void GenerateSchemaMemberHeader(SchemaMember schemaMember, CodeGenerator code)
		{
			if (schemaMember.Type is Schema.Types.Array array)
			{
				string elementTypeString = array.ElementType.ToString();
				string container = array.Container ?? "vector";
				string pointer = array.IsComplexArray ? "*" : string.Empty;
				if (array.TryGetKeyMember(out var keyMember) && keyMember is not null)
				{
					elementTypeString = $"{keyMember.Type}, {elementTypeString}";
				}

				code.WriteLine($"{container}<{elementTypeString}{pointer}> {schemaMember.MemberName};");
			}
			else if (schemaMember.Type is Schema.Types.Enum enumType)
			{
				code.WriteLine($"{enumType.EnumName} {schemaMember.MemberName};");
			}
			else if (schemaMember.Type.IsPrimitive)
			{
				code.WriteLine($"{schemaMember.Type} {schemaMember.MemberName};");
			}
			else if (schemaMember.Type.IsObject)
			{
				code.WriteLine($"{schemaMember.Type}* {schemaMember.MemberName} = nullptr;");
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		private static void GenerateSchemaMemberFwdDecl(SchemaMember schemaMember, CodeGenerator code, HashSet<string> usedDecls)
		{
			var obj = schemaMember.Type switch
			{
				Schema.Types.Object o => o,
				Schema.Types.Array a => a.ElementType,
				_ => null,
			};

			if (obj != null)
			{
				if (usedDecls.Add(obj.ToString()))
				{
					code.WriteLine($"class {obj};");
				}
			}
		}

		private static string GenerateConversionForType(Schema.Types.BaseType type)
		{
			return type switch
			{
				Schema.Types.String => "asString()",
				Schema.Types.Int => "asInt()",
				Schema.Types.Long => "asInt64()",
				Schema.Types.Float => "asFloat()",
				Schema.Types.Double => "asDouble()",
				Schema.Types.Bool => "asBool()",
				_ => throw new NotImplementedException(),
			};
		}

		private static string GenerateDeserializeCastForType(Schema.Types.BaseType type) => type is Schema.Types.Long ? "(long)" : string.Empty;
		private static string GenerateSerializeCastForType(Schema.Types.BaseType type) => type is Schema.Types.Long ? "(Json::Int64)" : string.Empty;

		private static void GenerateSchemaClassSource(SchemaClass schemaClass, CodeGenerator code)
		{
			code.WriteLine(FileHeader);
			code.WriteLine($"#include \"{schemaClass.ClassName}.gen.h\"");

			var usedIncludes = new HashSet<string>();
			schemaClass.Members.ForEach(m => GenerateIncludes(m, code, usedIncludes));

			code.NewLine();
			code.WriteLine("#include <json/json.h>");
			code.WriteLine("#include <imgui.h>");
			code.WriteLine("#include <imgui/misc/cpp/imgui_stdlib.h>");

			code.NewLine();
			code.WriteLine("using namespace SchemaClasses;");
			code.WriteLine("using string = std::string;");
			code.WriteLine("template<class T>");
			code.WriteLine("using vector = std::vector<T>;");

			code.NewLine();
			code.WriteLine($"bool {schemaClass.ClassName}::DeepEquals({schemaClass.ClassName}* other) const");
			using (new Scope(code))
			{
				code.WriteLine($"bool equal = true");
				schemaClass.Members.Where(m => !m.Type.IsComplexArray).ToList().ForEach(m => GenerateDeepEqualsMember(m, code));
				code.WriteLine($";");
				schemaClass.Members.Where(m => m.Type.IsComplexArray).ToList().ForEach(m => GenerateDeepEqualsMember(m, code));
				code.WriteLine($"return equal; ");
			}

			code.NewLine();
			code.WriteLine($"{schemaClass.ClassName}* {schemaClass.ClassName}::DeepCopy() const");
			using (new Scope(code))
			{
				code.WriteLine($"auto newObj = new {schemaClass.ClassName}();");
				schemaClass.Members.ForEach(m => GenerateDeepCopyMember(m, code));
				code.WriteLine($"return newObj;");
			}

			code.NewLine();
			code.WriteLine($"void {schemaClass.ClassName}::Deserialize(Json::Value& jsonObj)");
			using (new Scope(code))
			{
				schemaClass.Members.ForEach(m => GenerateDeserializeMember(m, code));
			}

			code.NewLine();
			code.WriteLine($"void {schemaClass.ClassName}::Serialize(Json::Value& jsonObj) const");
			using (new Scope(code))
			{
				schemaClass.Members.ForEach(m => GenerateSerializeMember(m, code));
			}

			code.NewLine();
			code.WriteLine($"{schemaClass.ClassName}* {schemaClass.ClassName}::Make()");
			using (new Scope(code))
			{
				code.WriteLine($"auto instance = new {schemaClass.ClassName}();");
				schemaClass.Members.ForEach(m => GenerateMakeMember(m, code));
				code.WriteLine($"return instance;");

			}

			code.NewLine();
			code.WriteLine($"void {schemaClass.ClassName}::Destroy()");
			using (new Scope(code))
			{
				schemaClass.Members.ForEach(m => GenerateDestroyMember(m, code));
			}

			code.NewLine();
			code.WriteLine($"void {schemaClass.ClassName}::ImGui()");
			using (new Scope(code))
			{
				code.WriteLine($"ColumnStack::Push(2, \"SchemaClasses:{schemaClass.ClassName}\");");
				schemaClass.Members.Where(m => !m.Type.IsContainer && !m.Type.IsObject).ToList().ForEach(m => GenerateImGuiEditField(m, code));
				code.WriteLine($"ColumnStack::Pop();");
				schemaClass.Members.Where(m => m.Type.IsContainer || m.Type.IsObject).ToList().ForEach(m => GenerateImGuiEditField(m, code));
			}
		}

		public static void GenerateDestroyMember(SchemaMember schemaMember, CodeGenerator code)
		{
			if (schemaMember.Type.IsObject)
			{
				code.WriteLine($"if({schemaMember.MemberName} != nullptr)");
				using (new Scope(code))
				{
					code.WriteLine($"{schemaMember.MemberName}->Destroy();");
					code.WriteLine($"delete {schemaMember.MemberName};");
				}
			}
			else if (schemaMember.Type is Schema.Types.Array array)
			{
				string elem = "elem";

				if (array.IsKeyed)
				{
					elem = "[key, elem]";
				}

				code.WriteLine($"for(auto& {elem} : {schemaMember.MemberName})");
				using (new Scope(code))
				{
					code.WriteLine($"elem->Destroy();");
					code.WriteLine($"delete elem;");
				}
			}

			if (schemaMember.Type.IsArray)
			{
				code.WriteLine($"{schemaMember.MemberName}.clear();");
			}
		}

		public static void GenerateMakeMember(SchemaMember schemaMember, CodeGenerator code)
		{
			if (schemaMember.Type.IsObject)
			{
				code.WriteLine($"instance->{schemaMember.MemberName} = {schemaMember.Type}::Make();");
			}
		}

		public static string GenerateNewForType(Schema.Types.BaseType type)
		{
			if (type is Schema.Types.String)
			{
				return $"string()";
			}
			else if (type.IsPrimitive || type is Schema.Types.Enum)
			{
				return $"{type}(0)";
			}
			else
			{
				return $"new {type}()";
			}
		}

		public static void GenerateDeepEqualsMember(SchemaMember schemaMember, CodeGenerator code)
		{
			if (schemaMember.Type is Schema.Types.Array array && array.IsComplexArray)
			{
				string elem = "elem";
				string otherElem = "otherElem";
				if (array.IsKeyed)
				{
					elem = "[key, elem]";
					otherElem = "[otherKey, otherElem]";
				}

				string key = string.Empty;
				if (!string.IsNullOrEmpty(array.Key))
				{
					key = "(key == otherKey) && ";
				}

				using (new Scope(code))
				{
					code.WriteLine($"if(!equal) return false;");
					code.WriteLine($"equal &= {schemaMember.MemberName}.size() == other->{schemaMember.MemberName}.size();");
					code.WriteLine($"int i=0;");
					code.WriteLine($"while(equal && i<{schemaMember.MemberName}.size())");
					using (new Scope(code))
					{
						code.WriteLine($"const auto& {elem} = {schemaMember.MemberName}.at(i);");
						code.WriteLine($"const auto& {otherElem} = other->{schemaMember.MemberName}.at(i);");
						code.WriteLine($"equal &= {key}elem->DeepEquals(otherElem);");
					}
				}
			}
			else if (schemaMember.Type.IsBuiltIn)
			{
				code.WriteLine($"&& ({schemaMember.MemberName} == other->{schemaMember.MemberName})");
			}
			else
			{
				code.WriteLine($"&& {schemaMember.MemberName}->DeepEquals(other->{schemaMember.MemberName})");
			}
		}

		public static void GenerateDeepCopyMember(SchemaMember schemaMember, CodeGenerator code)
		{
			if (schemaMember.Type is Schema.Types.Array array && array.IsComplexArray)
			{
				string elem = "elem";
				if (array.IsKeyed)
				{
					elem = "[key, elem]";
				}

				string addFunc = "push_back";
				if (!string.IsNullOrEmpty(array.Container))
				{
					addFunc = "add";
				}

				string key = string.Empty;
				if (!string.IsNullOrEmpty(array.Key))
				{
					key = "key, ";
				}

				code.WriteLine($"for(auto& {elem}: {schemaMember.MemberName})");
				using (new Scope(code))
				{
					code.WriteLine($"newObj->{schemaMember.MemberName}.{addFunc}({key}elem->DeepCopy());");
				}
			}
			else if (schemaMember.Type.IsBuiltIn)
			{
				code.WriteLine($"newObj->{schemaMember.MemberName} = {schemaMember.MemberName};");
			}
			else
			{
				code.WriteLine($"newObj->{schemaMember.MemberName} = {schemaMember.MemberName}->DeepCopy();");
			}
		}

		public static void GenerateDeserializeMember(SchemaMember schemaMember, CodeGenerator code, string? overrideName = null)
		{
			var name = schemaMember.MemberName;
			if (!string.IsNullOrEmpty(overrideName))
			{
				name = (MemberName)overrideName;
			}

			if (schemaMember.Type is Schema.Types.Array array)
			{
				using (new Scope(code))
				{
					code.WriteLine($"auto& arr = jsonObj[\"{name}\"];");
					code.WriteLine($"{name}.clear();");
					code.WriteLine($"{name}.reserve(arr.size());");
					code.WriteLine($"for(auto& elem : arr)");
					using (new Scope(code))
					{
						string addFunc = "push_back";
						if (!string.IsNullOrEmpty(array.Container))
						{
							addFunc = "add";
						}

						if (array.ElementType.IsBuiltIn)
						{
							string conversion = GenerateConversionForType(array.ElementType);
							string cast = GenerateDeserializeCastForType(array.ElementType);
							code.WriteLine($"{name}.{addFunc}({cast}elem.{conversion});");
						}
						else
						{
							string keyToken = string.Empty;
							if (!string.IsNullOrEmpty(array.Key))
							{
								keyToken = $"val->{array.Key}, ";
							}

							code.WriteLine($"auto val = {GenerateNewForType(array.ElementType)};");
							code.WriteLine($"val->Deserialize(elem);");
							code.WriteLine($"{name}.{addFunc}({keyToken}val);");
						}
					}
				}
			}
			else if (schemaMember.Type is Schema.Types.Enum enumType)
			{
				code.WriteLine($"{name} = {enumType.EnumName}::FromString(jsonObj[\"{name}\"].asString());");
			}
			else if (schemaMember.Type.IsPrimitive)
			{
				string conversion = GenerateConversionForType(schemaMember.Type);
				string cast = GenerateDeserializeCastForType(schemaMember.Type);
				code.WriteLine($"{name} = {cast}jsonObj[\"{name}\"].{conversion};");
			}
			else
			{
				using (new Scope(code))
				{
					code.WriteLine($"{name} = {GenerateNewForType(schemaMember.Type)};");
					code.WriteLine($"{name}->Deserialize(jsonObj[\"{name}\"]);");
				}
			}
		}

		public static void GenerateSerializeMember(SchemaMember schemaMember, CodeGenerator code)
		{
			if (schemaMember.Type is Schema.Types.Array array)
			{
				using (new Scope(code))
				{
					code.WriteLine($"auto arr = Json::Value(Json::arrayValue);");

					string elem = "elem";

					if (array.IsKeyed)
					{
						elem = "[key, elem]";
					}

					code.WriteLine($"for(auto& {elem} : {schemaMember.MemberName})");
					using (new Scope(code))
					{
						if (array.ElementType.IsPrimitive)
						{
							string cast = GenerateSerializeCastForType(array.ElementType);
							code.WriteLine($"arr.append(Json::Value({cast}elem));");
						}
						else
						{
							code.WriteLine($"auto obj = Json::Value(Json::objectValue);");
							code.WriteLine($"elem->Serialize(obj);");
							code.WriteLine($"arr.append(obj);");
						}
					}

					code.WriteLine($"jsonObj[\"{schemaMember.MemberName}\"] = arr;");
				}
			}
			else if (schemaMember.Type is Schema.Types.Enum enumType)
			{
				code.WriteLine($"jsonObj[\"{schemaMember.MemberName}\"] = {enumType.EnumName}::ToString({schemaMember.MemberName});");
			}
			else if (schemaMember.Type.IsPrimitive)
			{
				string cast = GenerateSerializeCastForType(schemaMember.Type);
				code.WriteLine($"jsonObj[\"{schemaMember.MemberName}\"] = Json::Value({cast}{schemaMember.MemberName});");
			}
			else
			{
				using (new Scope(code))
				{
					code.WriteLine($"auto obj = Json::Value(Json::objectValue);");
					code.WriteLine($"{schemaMember.MemberName}->Serialize(obj);");
					code.WriteLine($"jsonObj[\"{schemaMember.MemberName}\"] = obj;");
				}
			}
		}

		private static void GenerateIncludes(SchemaMember schemaMember, CodeGenerator code, HashSet<string> usedIncludes)
		{
			var obj = schemaMember.Type switch
			{
				Schema.Types.Object o => o,
				Schema.Types.Array a => a.ElementType,
				_ => null,
			};

			if (obj != null)
			{
				if (usedIncludes.Add(obj.ToString()))
				{
					code.WriteLine($"#include \"{obj}.gen.h\"");
				}
			}
		}

		private static void GenerateEnumIncludes(SchemaMember schemaMember, CodeGenerator code, HashSet<string> usedIncludes)
		{
			if (schemaMember.Type is Schema.Types.Enum enumType)
			{
				if (usedIncludes.Add(enumType.EnumName))
				{
					code.WriteLine($"#include \"Enum{enumType.EnumName}.gen.h\"");
				}
			}
		}

		private static void GenerateContainerIncludes(SchemaMember schemaMember, CodeGenerator code, HashSet<string> usedIncludes)
		{
			if (schemaMember.Type is Schema.Types.Array array)
			{
				if (!string.IsNullOrEmpty(array.Container) && usedIncludes.Add(array.Container))
				{
					code.WriteLine($"#include \"{array.Container}.h\"");
				}
			}
		}

		private static void GenerateEnumHeader(SchemaEnum schemaEnum, CodeGenerator code)
		{
			code.WriteLine("#pragma once");
			code.WriteLine(FileHeader);
			code.WriteLine("#include <string>");
			code.WriteLine("#include <vector>");
			code.NewLine();
			code.WriteLine("namespace SchemaClasses");
			using (new Scope(code))
			{
				code.WriteLine("using string = std::string;");
				code.WriteLine("template<class T>");
				code.WriteLine("using vector = std::vector<T>;");
				code.NewLine();

				code.WriteLine($"struct {schemaEnum.EnumName}");
				using (new Scope(code))
				{
					code.WriteLine($"enum class Type");
					using (new Scope(code))
					{
						foreach (var enumValue in schemaEnum.Values)
						{
							code.WriteLine($"{enumValue},");
						}
					}

					code.NewLine();
					code.WriteLine($"using enum Type;");
					code.WriteLine($"{schemaEnum.EnumName}() = default;");
					code.WriteLine($"{schemaEnum.EnumName}(const {schemaEnum.EnumName}& other) = default;");
					code.WriteLine($"{schemaEnum.EnumName}(Type&& other) {{ m_value = std::move(other); }}");
					code.WriteLine($"operator Type() const {{ return m_value; }}");
					code.WriteLine($"operator int() const {{ return (int)m_value; }}");
					code.WriteLine($"bool operator==(const {schemaEnum.EnumName}& other) const {{ return m_value == other.m_value; }}");
					code.WriteLine($"bool operator==(const Type& other) const {{ return m_value == other; }}");

					code.NewLine();
					code.WriteLine($"static string GetName() {{ return \"{schemaEnum.EnumName}\"; }}");
					code.WriteLine($"static const vector<string>& GetNames();");
					code.WriteLine($"static const vector<{schemaEnum.EnumName}>& GetValues();");

					code.NewLine();
					code.WriteLine($"static string ToString({schemaEnum.EnumName} value);");
					code.WriteLine($"static string ToString(Type value);");
					code.WriteLine($"static {schemaEnum.EnumName} FromString(const string& value);");

					code.NewLine();
					code.WriteLine($"template<typename T>");
					code.WriteLine($"static Type FromInt(T i) {{ return (Type)(int)i; }}");
					code.WriteLine($"static Type FromInt(int i) {{ return (Type)i; }}");
					code.WriteLine($"int AsInt() const {{ return (int)m_value; }}");

					code.NewLine();
					code.WriteLine($"private:");
					code.WriteLine($"Type m_value;");
				}
			}
		}

		private static void GenerateEnumSource(SchemaEnum schemaEnum, CodeGenerator code)
		{
			code.WriteLine(FileHeader);
			code.WriteLine($"#include \"Enum{schemaEnum.EnumName}.gen.h\"");

			code.NewLine();
			code.WriteLine("#include <stdexcept>");
			code.WriteLine("#include <imgui.h>");
			code.WriteLine("#include <imgui/misc/cpp/imgui_stdlib.h>");

			code.NewLine();
			code.WriteLine("namespace SchemaClasses");
			using (new Scope(code))
			{
				code.WriteLine("using string = std::string;");
				code.WriteLine("template<class T>");
				code.WriteLine("using vector = std::vector<T>;");

				code.NewLine();
				code.WriteLine($"string {schemaEnum.EnumName}::ToString({schemaEnum.EnumName} value)");
				using (new Scope(code))
				{
					foreach (var enumValue in schemaEnum.Values)
					{
						code.WriteLine($"if(value.m_value == {schemaEnum.EnumName}::{enumValue}) return \"{enumValue}\";");
					}

					code.WriteLine($"throw std::invalid_argument(\"Value was not a valid {schemaEnum.EnumName}\");");
				}

				code.NewLine();
				code.WriteLine($"string {schemaEnum.EnumName}::ToString(Type value)");
				using (new Scope(code))
				{
					foreach (var enumValue in schemaEnum.Values)
					{
						code.WriteLine($"if(value == {schemaEnum.EnumName}::{enumValue}) return \"{enumValue}\";");
					}

					code.WriteLine($"throw std::invalid_argument(\"Value was not a valid {schemaEnum.EnumName}\");");
				}

				code.NewLine();
				code.WriteLine($"{schemaEnum.EnumName} {schemaEnum.EnumName}::FromString(const string& value)");
				using (new Scope(code))
				{
					foreach (var enumValue in schemaEnum.Values)
					{
						code.WriteLine($"if(value == \"{enumValue}\") return {schemaEnum.EnumName}::{enumValue};");
					}

					code.WriteLine($"throw std::invalid_argument(value + \" was not a valid {schemaEnum.EnumName}\");");
				}

				code.NewLine();
				code.WriteLine($"const vector<{schemaEnum.EnumName}>& {schemaEnum.EnumName}::GetValues()");
				using (new Scope(code))
				{
					code.WriteLine($"static vector<{schemaEnum.EnumName}> list;");
					code.WriteLine("if(!list.empty()) { return list; }");
					code.WriteLine($"list.reserve({schemaEnum.Values.Count});");
					foreach (var enumValue in schemaEnum.Values)
					{
						code.WriteLine($"list.push_back({schemaEnum.EnumName}::{enumValue});");
					}

					code.WriteLine("return list;");
				}

				code.NewLine();
				code.WriteLine($"const vector<string>& {schemaEnum.EnumName}::GetNames()");
				using (new Scope(code))
				{
					code.WriteLine("static vector<string> list;");
					code.WriteLine("if(!list.empty()) { return list; }");
					code.WriteLine($"list.reserve({schemaEnum.Values.Count});");
					foreach (var enumValue in schemaEnum.Values)
					{
						code.WriteLine($"list.push_back(\"{enumValue}\");");
					}

					code.WriteLine("return list;");
				}
			}
		}

		public static void GenerateImGuiEditField(SchemaMember schemaMember, CodeGenerator code)
		{
			code.WriteLine($"ImGui::PushID(\"{schemaMember.MemberName}\");");
			using (new Scope(code))
			{
				if (schemaMember.Type is Schema.Types.Array array)
				{
					array.TryGetKeyMember(out var keyMember);
					code.WriteLine($"string title = string(\"{schemaMember.MemberName}\") + \" (array({array.ElementType}))\";");
					code.WriteLine($"ColumnStack::PushSingle();");
					code.WriteLine($"if(ImGui::CollapsingHeader(title.c_str()))");
					using (new Scope(code))
					{

						code.WriteLine($"ImGui::Indent();");

						code.WriteLine($"if(ImGui::Button(\"Add\"))");
						using (new Scope(code))
						{
							code.WriteLine($"auto val = {GenerateNewForType(array.ElementType)};");

							string addFunc = "push_back";
							if (!string.IsNullOrEmpty(array.Container))
							{
								addFunc = "add";
							}

							string key = string.Empty;
							if (keyMember != null)
							{
								key = $"{keyMember.Type}(), ";
							}

							code.WriteLine($"{schemaMember.MemberName}.{addFunc}({key}val);");
						}

						string filterCondition = "if(true)";
						if (array.IsPrimitiveArray || array.IsKeyed)
						{

							code.WriteLine($"ImGui::SameLine();");
							code.WriteLine($"static ImGuiTextFilter filter;");
							code.WriteLine($"filter.Draw();");
							filterCondition = $"if(filter.PassFilter(std::to_string(elem).c_str()))";
							if (array.ElementType is Schema.Types.String)
							{
								filterCondition = $"if(filter.PassFilter(elem.c_str()))";
							}
						}

						code.WriteLine($"int idx = 0;");
						code.WriteLine($"int indexToDelete = -1;");
						string elem = "elem";

						if (keyMember != null)
						{
							elem = "[key, elem]";

							filterCondition = $"if(filter.PassFilter(std::to_string(key).c_str()))";
							if (keyMember.Type is Schema.Types.String)
							{
								filterCondition = $"if(filter.PassFilter(key.c_str()))";
							}
						}

						if (schemaMember.Type.IsPrimitiveArray)
						{
							code.WriteLine($"ColumnStack::Push(2, \"primitiveArray\");");
						}

						code.WriteLine($"for(auto& {elem} : {schemaMember.MemberName})");
						using (new Scope(code))
						{
							code.WriteLine(filterCondition);
							using (new Scope(code))
							{
								code.WriteLine($"ImGui::PushID(idx);");
								if (schemaMember.Type.IsPrimitiveArray)
								{
									code.WriteLine($"if(ImGui::Button(\"X\"))");
									using (new Scope(code))
									{
										code.WriteLine($"indexToDelete = idx;");
									}

									code.WriteLine($"ImGui::SameLine();");
									code.WriteLine($"ImGui::TextUnformatted(std::to_string(idx).c_str());");
									code.WriteLine($"ImGui::NextColumn();");
									GenerateEditFieldForType(array.ElementType, schemaMember.MemberName, $"elem", code);
									code.WriteLine($"ImGui::NextColumn();");
									code.WriteLine($"ImGui::Separator();");
								}
								else if (schemaMember.Type.IsComplexArray)
								{
									code.WriteLine($"if(ImGui::Button(\"X\"))");
									using (new Scope(code))
									{
										code.WriteLine($"indexToDelete = idx;");
									}

									code.WriteLine($"ImGui::SameLine();");
									string key = $"\"{array.ElementType}\"";

									if (keyMember != null)
									{
										key = $"std::to_string(elem->{keyMember.MemberName})";
										if (keyMember.Type is Schema.Types.String)
										{
											key = $"elem->{keyMember.MemberName}";
										}
									}

									code.WriteLine($"string elementTitle = std::to_string(idx) + \": \" + {key};");
									code.WriteLine($"if(ImGui::CollapsingHeader(elementTitle.c_str()))");
									using (new Scope(code))
									{
										code.WriteLine($"ImGui::Indent();");
										GenerateEditFieldForType(array.ElementType, schemaMember.MemberName, $"elem", code);
										code.WriteLine($"ImGui::Unindent();");
									}
								}

								code.WriteLine($"ImGui::PopID();");
							}

							code.WriteLine($"idx++;");
						}

						if (schemaMember.Type.IsPrimitiveArray)
						{
							code.WriteLine($"ColumnStack::Pop();");
						}

						code.WriteLine($"if(indexToDelete >= 0)");
						using (new Scope(code))
						{
							if (!string.IsNullOrEmpty(array.Container))
							{
								code.WriteLine($"{schemaMember.MemberName}.removeAt(indexToDelete);");
							}
							else
							{
								code.WriteLine($"{schemaMember.MemberName}.erase({schemaMember.MemberName}.begin() + indexToDelete);");
							}
						}

						if (keyMember != null)
						{
							code.WriteLine();
							code.WriteLine($"auto keysToFix = vector<{keyMember.Type}>();");
							code.WriteLine($"for(auto& [key, elem] : {schemaMember.MemberName})");
							using (new Scope(code))
							{
								code.WriteLine($"auto& realKey = elem->{array.Key};");
								code.WriteLine($"if(key != realKey)");
								using (new Scope(code))
								{
									code.WriteLine($"keysToFix.push_back(key);");
								}
							}

							code.WriteLine();
							code.WriteLine($"for(auto& key : keysToFix)");
							using (new Scope(code))
							{
								code.WriteLine($"auto elem = {schemaMember.MemberName}[key];");
								code.WriteLine($"auto& realKey = elem->{array.Key};");
								code.WriteLine($"{schemaMember.MemberName}.add(realKey, elem);");
								code.WriteLine($"{schemaMember.MemberName}.remove(key);");
							}
						}

						code.WriteLine($"ImGui::Unindent();");
					}

					code.WriteLine($"ColumnStack::Pop();");
				}
				else if (schemaMember.Type.IsObject)
				{
					code.WriteLine($"ColumnStack::PushSingle();");
					code.WriteLine($"string elementTitle = string(\"{schemaMember.MemberName}\") + \" ({schemaMember.Type})\";");
					code.WriteLine($"if(ImGui::CollapsingHeader(elementTitle.c_str()))");
					using (new Scope(code))
					{
						code.WriteLine($"ImGui::Indent();");
						code.WriteLine($"{schemaMember.MemberName}->ImGui();");
						code.WriteLine($"ImGui::Unindent();");
					}

					code.WriteLine($"ColumnStack::Pop();");
				}
				else
				{
					code.WriteLine($"ImGui::TextUnformatted(\"{schemaMember.MemberName}\");");
					code.WriteLine($"ImGui::NextColumn();");
					code.WriteLine($"ImGui::SetNextItemWidth(-1);");
					GenerateEditFieldForType(schemaMember.Type, schemaMember.MemberName, schemaMember.MemberName, code);

					//TODO: enums
					code.WriteLine($"ImGui::NextColumn();");
					code.WriteLine($"ImGui::Separator();");
				}
			}

			code.WriteLine($"ImGui::PopID();");
		}

		private static void GenerateEditFieldForType(Schema.Types.BaseType type, string name, string variable, CodeGenerator code)
		{
			string line = type switch
			{
				Schema.Types.String => $"ImGui::InputText(\"##{name}EditField\", &{variable});",
				Schema.Types.Bool => $"ImGui::Checkbox(\"##{name}EditField\", &{variable});",
				Schema.Types.Object => $"{variable}->ImGui();",
				_ => $"ImGui::Input{type}(\"##{name}EditField\", &{variable}, 0.05f);",
			};

			code.WriteLine(line);
		}
	}
}