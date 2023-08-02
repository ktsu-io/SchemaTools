using System.CodeDom.Compiler;

namespace ktsu.io
{
	public class CodeGenerator : IndentedTextWriter
	{
		public CodeGenerator() : base(new StringWriter(), "\t")
		{
		}

		override public string ToString() => InnerWriter.ToString() ?? string.Empty;

		public new void NewLine() => WriteLineNoTabs(string.Empty);
	}

	public class Scope : IDisposable
	{
		private bool disposedValue;

		private CodeGenerator? CodeGenerator { get; set; }

		public Scope(CodeGenerator codeGenerator)
		{
			CodeGenerator = codeGenerator;
			CodeGenerator.WriteLine("{");
			CodeGenerator.Indent++;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing && CodeGenerator != null)
				{
					CodeGenerator.Indent--;
					CodeGenerator.WriteLine("};");
				}

				CodeGenerator = null;
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}