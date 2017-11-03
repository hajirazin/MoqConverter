using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.Converters
{
    public abstract class FileRewritter : CSharpSyntaxRewriter
    {
        public abstract bool IsValidFile(CompilationUnitSyntax root);
    }
}
