using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.MemberAccess
{
    public class ArgIsEqual : IMemberAccessStrategy
    {
        public bool IsEligible(MemberAccessExpressionSyntax node)
        {
            var nodeString = node.ToString();
            var nodeNameString = node.Name.ToString();
            return nodeString.Contains("Arg") &&
                   (nodeNameString.Contains("Equal") || nodeNameString.Contains("Same") ||
                    nodeNameString.Contains("Null"));
        }

        public SyntaxNode Visit(MemberAccessExpressionSyntax node)
        {
            var nodeString = node.ToString();
            nodeString = Regex.Replace(nodeString, "Arg(<[a-zA-Z0-9,_ <>\\[\\]]+>).Is.Equal", "It.IsIn");
            nodeString = Regex.Replace(nodeString, "Arg(<[a-zA-Z0-9,_ <>\\[\\]]+>).Is.Same", "It.IsIn");
            nodeString = Regex.Replace(nodeString, "Arg<([a-zA-Z0-9,_ <>\\[\\]]+)>.Is.Null", "It.IsIn(($1)null)");
            nodeString = Regex.Replace(nodeString, "Arg<([a-zA-Z0-9,_ <>\\[\\]]+)>.Is.NotNull", "It.Is<$1>(a => a != null)");
            return SyntaxFactory.ParseExpression(nodeString);
        }
    }
}
