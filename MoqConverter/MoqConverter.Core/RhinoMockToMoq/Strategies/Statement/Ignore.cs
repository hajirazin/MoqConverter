using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class Ignore : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (expressionStatement.ToString().Contains("t.Error"))
            {
                
            }
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return false;
            if (!(node.Expression is MemberAccessExpressionSyntax member))
                return false;

            var nodeNameString = member.Name.ToString();
            return nodeNameString.Equals("IgnoreArguments");
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            var node = ((MemberAccessExpressionSyntax) ((InvocationExpressionSyntax) expressionStatement.Expression)
                .Expression).Expression;

            return expressionStatement.WithExpression(node);
        }
    }
}