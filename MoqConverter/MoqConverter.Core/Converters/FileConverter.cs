﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public void Convert(string files)
        {
            foreach (var file in DirSearch(files))
            {
                var text = File.ReadAllText(file);
                var syntaxTree = CSharpSyntaxTree.ParseText(text);
                var root = syntaxTree.GetRoot();

                if (!_syntaxRewriter.IsValidFile(root as CompilationUnitSyntax))
                    continue;

                Logger.Log($"Starting Convertion of file {Path.GetFileNameWithoutExtension(file)}", ConsoleColor.Red);
                try
                {
                    root = _syntaxRewriter.Visit(root);
                }
                catch (Exception exception)
                {
                    Logger.Log($"Failed Convertion of file {Path.GetFileNameWithoutExtension(file)}",
                        ConsoleColor.Yellow);
                }

                var code = Prettify(root);
                File.WriteAllText(file, code);
            }
        }

        private List<string> DirSearch(string sDir)
        {
            return Directory.GetFiles(sDir, "*.*", SearchOption.AllDirectories).ToList();
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
