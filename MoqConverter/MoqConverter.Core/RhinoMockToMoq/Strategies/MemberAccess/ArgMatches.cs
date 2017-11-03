using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.MemberAccess
{
    public class ArgMatches : IMemberAccessStrategy
    {
        public bool IsEligible(MemberAccessExpressionSyntax node)
        {
            var nodeString = node.ToString();
            var nodeNameString = node.Name.ToString();
            return nodeString.Contains("Arg") && nodeNameString.Contains("Matches");
        }

        public SyntaxNode Visit(MemberAccessExpressionSyntax node)
        {
            var nodeString = node.ToString();
            
            nodeString = Regex.Replace(nodeString, "Arg(<[a-zA-Z0-9,_ <>\\[\\]]+>)(.*)Matches", "It.Is$1", RegexOptions.Singleline);
            return SyntaxFactory.ParseExpression(nodeString);
        }
    }
}
