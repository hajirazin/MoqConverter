using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class WhenCalled : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            var nodeString = expressionStatement.ToString();
            return nodeString.Contains("WhenCalled") && (nodeString.Contains("Expect") || nodeString.Contains("Stub"));
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node)) return expressionStatement;
            var nodeString = node.ToString();
            var when = "When";
            if (nodeString.Contains("Ignore"))
            {
                when = "WhenForAnyArgs";
                nodeString = nodeString.Replace(".IgnoreArguments()", string.Empty);
            }

            nodeString = nodeString
                .Replace("Expect", when)
                .Replace("Stub", when)
                .Replace("WhenCalled", "Do");
            return expressionStatement.WithExpression(SyntaxFactory.ParseExpression(nodeString));
        }
    }
}
