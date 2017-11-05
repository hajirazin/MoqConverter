using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class PropertyBehavior : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return false;
            if (!(node.Expression is MemberAccessExpressionSyntax member))
                return false;

            var nodeNameString = member.Name.ToString();
            return nodeNameString.Equals("PropertyBehavior");
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return expressionStatement;
            if (!(node.Expression is MemberAccessExpressionSyntax member))
                return expressionStatement;
            if (!(member.Expression is InvocationExpressionSyntax node1))
                return expressionStatement;
            if (!(node1.Expression is MemberAccessExpressionSyntax member1))
                return expressionStatement;
            if (!(member1.Expression is IdentifierNameSyntax identifier))
                return expressionStatement;
            
            member1 = member1.WithName(SyntaxFactory.IdentifierName("SetupProperty"))
                .WithExpression(SyntaxFactory.IdentifierName(identifier + "Mock"));
            node1 = node1.WithExpression(member1);
            expressionStatement = expressionStatement.WithExpression(node1);
            return expressionStatement;
        }
    }
}
