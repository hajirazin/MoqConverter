using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.MemberAccess
{
    public class ArgAnything : IMemberAccessStrategy
    {
        public bool IsEligible(MemberAccessExpressionSyntax node)
        {
            var nodeString = node.ToString();
            var nodeNameString = node.Name.ToString();
            return nodeString.Contains("Arg") &&
                   (nodeNameString.Contains("Anything") || nodeNameString.Contains("TypeOf"));
        }

        public SyntaxNode Visit(MemberAccessExpressionSyntax node)
        {
            var nodeString = node.ToString();
            nodeString = Regex.Replace(nodeString, "Arg(<[a-zA-Z0-9,_. <>\\[\\]]+>).Is.Anything", "It.IsAny$1()");
            nodeString = Regex.Replace(nodeString, "Arg(<[a-zA-Z0-9,_. <>\\[\\]]+>).Is.TypeOf", "It.IsAny$1()");
            return SyntaxFactory.ParseExpression(nodeString);
        }
    }
}
