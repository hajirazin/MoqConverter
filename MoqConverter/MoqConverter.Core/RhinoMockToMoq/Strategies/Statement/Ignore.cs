using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class Ignore : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return false;
            if (!(node.Expression is MemberAccessExpressionSyntax member))
                return false;

            var nodeNameString = member.Name.ToString();
            return nodeNameString.Equals("IgnoreArguments");
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node)) return expressionStatement;
            var nodeString = node.ToString();
            if (nodeString.Contains("ReturnsForAnyArgs") || nodeString.Contains("ReceivedWithAnyArgs"))
            {
                nodeString = nodeString.Replace("IgnoreArguments()", "");
            }

            nodeString = nodeString.Contains("Returns")
                ? nodeString.Replace("IgnoreArguments()", "").Replace("Returns", "ReturnsForAnyArgs")
                : nodeString.Replace("IgnoreArguments", "ReceivedWithAnyArgs");
            return expressionStatement.WithExpression(SyntaxFactory.ParseExpression(nodeString));
        }
    }
}