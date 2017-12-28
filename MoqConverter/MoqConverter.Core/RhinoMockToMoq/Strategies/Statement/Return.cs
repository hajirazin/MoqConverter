using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class Return : IStatementStrategy
    {
        public virtual bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return false;
            if (!(node.Expression is MemberAccessExpressionSyntax smes))
                return false;

            if (!(smes.Expression is InvocationExpressionSyntax nodeInner))
                return false;

            if (!(nodeInner.Expression is MemberAccessExpressionSyntax member))
                return false;

            if (!(member.Expression is IdentifierNameSyntax) && !(member.Expression is MemberAccessExpressionSyntax))
                return false;

            var nodeString = node.ToString();
            var nodeNameString = smes.Name.ToString();

            return (nodeString.Contains("Expect") || nodeString.Contains("Stub")) &&
                   nodeNameString.Contains("Return");
        }

        public virtual ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            var nodeString = expressionStatement.ToString();
            var isRepeatAny = false;
            var isRepeatNever = false;
            var isExpect = nodeString.Contains(".Expect");
            var node = (InvocationExpressionSyntax)expressionStatement.Expression;
            var memberOuter = (MemberAccessExpressionSyntax)node.Expression;
            var nodeInner = (InvocationExpressionSyntax)memberOuter.Expression;
            var member = (MemberAccessExpressionSyntax)nodeInner.Expression;
            ExpressionSyntax expression;
            if (member.Expression is MemberAccessExpressionSyntax property)
            {
                if (property.Name.ToString() == "Repeat")
                {
                    isRepeatAny = member.Name.ToString() == "Any";
                    isRepeatNever = member.Name.ToString() == "Never";
                    
                    nodeInner = (InvocationExpressionSyntax)property.Expression;
                    member = (MemberAccessExpressionSyntax)nodeInner.Expression;
                    var identifier = (IdentifierNameSyntax)member.Expression;
                    var mockGet = SyntaxFactory.IdentifierName(identifier + "Mock");
                    expression = mockGet;
                }
                else
                    expression = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("Mock"),
                            SyntaxFactory.IdentifierName("Get")),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                            {SyntaxFactory.Argument(property)})));
            }
            else
            {
                var identifier = (IdentifierNameSyntax)member.Expression;
                var mockGet = SyntaxFactory.IdentifierName(identifier + "Mock");
                expression = mockGet;

            }

            member = member.WithExpression(expression).WithName(SyntaxFactory.IdentifierName("Setup"));
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
            if (isExpect && !isRepeatAny && !isRepeatNever)
            {
                node = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, node, SyntaxFactory.IdentifierName("Verifiable")));
            }

            if (isRepeatNever)
            {
                node = (InvocationExpressionSyntax) ((MemberAccessExpressionSyntax) node.Expression).Expression;
                node = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, node,
                    SyntaxFactory.GenericName(SyntaxFactory.Identifier("Throws"),
                        SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(new[]
                            {(TypeSyntax) SyntaxFactory.IdentifierName("Exception")})))));
            }

            return expressionStatement.WithExpression(node);
        }
    }
}
