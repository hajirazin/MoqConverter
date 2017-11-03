using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class Return : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return false;
            var nodeString = node.ToString();
            if (!(node.Expression is MemberAccessExpressionSyntax smes))
                return false;

            var nodeNameString = smes.Name.ToString();

            return (nodeString.Contains("Expect") || nodeString.Contains("Stub")) &&
                   nodeNameString.Contains("Return");
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node)) return expressionStatement;

            if (!(node.Expression is MemberAccessExpressionSyntax memberOuter))
                return expressionStatement;

            if (!(memberOuter.Expression is InvocationExpressionSyntax nodeInner)) return expressionStatement;

            if (!(nodeInner.Expression is MemberAccessExpressionSyntax member))
                return expressionStatement;

            var memberExpression = member.Expression;
            var mockGet = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Mock"), SyntaxFactory.IdentifierName("Get")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(memberExpression) })));

            member = member.WithExpression(mockGet).WithName(SyntaxFactory.IdentifierName("Setup"));
            nodeInner = nodeInner.WithExpression(member);
            memberOuter = memberOuter.WithExpression(nodeInner).WithName(SyntaxFactory.IdentifierName("Returns"));

            node = node.WithExpression(memberOuter);

            return expressionStatement.WithExpression(node);
        }
    }
}
