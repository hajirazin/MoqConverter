using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace MoqConverter.Core.Converters
{
    public interface IFileConverter
    {
        void Convert(string file);
    }
    public class FileConverter : IFileConverter
    {
        private readonly FileRewritter _syntaxRewriter;

        public FileConverter(FileRewritter syntaxRewriter)
        {
            _syntaxRewriter = syntaxRewriter;
        }
        public void Convert(string file)
        {
            var text = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(text);
            var root = syntaxTree.GetRoot();
            if (_syntaxRewriter.IsValidFile(root as CompilationUnitSyntax))
            {
                Logger.Log($"Starting Convertion of file {Path.GetFileNameWithoutExtension(file)}", ConsoleColor.Red);
                root = _syntaxRewriter.Visit(root);
                var code = Prettify(root);
                File.WriteAllText(file, code);
            }
        }

        private static string Prettify(SyntaxNode root)
        {
            var workspace = new AdhocWorkspace();
            var options = workspace.Options;
            options = options.WithChangedOption(CSharpFormattingOptions.IndentBlock, true);
            options = options.WithChangedOption(CSharpFormattingOptions.IndentBraces, false);

            var formattedNode = Formatter.Format(root, workspace, options);
            var formattedString = formattedNode.ToFullString();


            return Regex.Replace(formattedString, @"^\s+$[\r\n]*", "\r\n", RegexOptions.Multiline);
        }
    }
}
