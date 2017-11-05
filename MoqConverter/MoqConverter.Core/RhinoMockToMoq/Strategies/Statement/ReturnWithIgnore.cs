using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class ReturnWithIgnore : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return false;
            var nodeString = node.ToString();
            if (!(node.Expression is MemberAccessExpressionSyntax smes))
                return false;

            var nodeNameString = smes.Name.ToString();
            return (nodeString.Contains(".Expect") || nodeString.Contains("Stub")) &&
                   nodeNameString.Contains("Return") &&
                   nodeString.Contains("Ignore");
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node)) return expressionStatement;

            if (!(node.Expression is MemberAccessExpressionSyntax memberOuter))
                return expressionStatement;

            if (!(memberOuter.Expression is InvocationExpressionSyntax ignorExpression)) return expressionStatement;

            if (!(ignorExpression.Expression is MemberAccessExpressionSyntax ignoreMember))
                return expressionStatement;

            if (!(ignoreMember.Expression is InvocationExpressionSyntax nodeInner))
                return expressionStatement;

            if (!(nodeInner.Expression is MemberAccessExpressionSyntax member))
                return expressionStatement;

            if (!(member.Expression is IdentifierNameSyntax identifier))
                return expressionStatement;
            var nodeString = expressionStatement.ToString();
            var isExpect = nodeString.Contains(".Expect");
            var mockGet = SyntaxFactory.IdentifierName(identifier.Identifier.ValueText + "Mock");

            member = member.WithExpression(mockGet).WithName(SyntaxFactory.IdentifierName("SetupIgnoreArgs"));
            nodeInner = nodeInner.WithExpression(member);
            memberOuter = memberOuter.WithExpression(nodeInner).WithName(SyntaxFactory.IdentifierName("Returns"));

            node = node.WithExpression(memberOuter);
            if (node.ArgumentList?.Arguments == null || node.ArgumentList.Arguments.Count <= 0)
                return expressionStatement.WithExpression(node);

            var argumentList = new SeparatedSyntaxList<ArgumentSyntax>();
            foreach (var argument in node.ArgumentList.Arguments)
            {
                if (argument.Expression is LiteralExpressionSyntax literal &&
                    literal.Kind() == SyntaxKind.NullLiteralExpression)
                {
                    argumentList = argumentList.Add(
                        argument.WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("value"))));
                }
                else
                {
                    argumentList = argumentList.Add(argument);
                }
            }

            node = node.WithArgumentList(node.ArgumentList.WithArguments(argumentList));
            if (isExpect)
            {
                node = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, node, SyntaxFactory.IdentifierName("Verifiable")));
            }

            return expressionStatement.WithExpression(node);
        }
    }
}
