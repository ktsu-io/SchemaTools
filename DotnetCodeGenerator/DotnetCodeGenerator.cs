#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.DotnetCodeGenerator;

using ktsu.io.SchemaCodeGenerator;
using ktsu.io.SchemaLib;


public class DotnetCodeGenerator : SchemaCodeGenerator
{


	protected override void GenerateCodeFor(Schema schema)
	{
		// ensure the output directory exists
		// make a project file if nescessary

		// generate a dotnet class for each schema class
		foreach (var schemaClass in schema.GetClasses())
		{
			GenerateCodeFor(schemaClass);
		}

		//generate a dotnet enum for each schema enum
		foreach (var schemaEnum in schema.GetEnums())
		{
			GenerateCodeFor(schemaEnum);
		}
	}

	protected override void GenerateCodeFor(SchemaClass schemaClass) => throw new NotImplementedException();
	protected override void GenerateCodeFor(SchemaEnum schemaEnum) => throw new NotImplementedException();
	protected override void GenerateCodeFor(SchemaMember schemaMember) => throw new NotImplementedException();
}
