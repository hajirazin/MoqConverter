using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class Repeat : Return, IStatementStrategy
    {
        public override bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node) ||
                !(node.Expression is MemberAccessExpressionSyntax memberAccessExpression) ||
                !(memberAccessExpression.Expression is MemberAccessExpressionSyntax repeatExpression))
                return false;

            return repeatExpression.Name.ToString().Equals("Repeat");
        }

        public override ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            var nodeString = expressionStatement.ToString();
            var invocationExpressionSyntax = (InvocationExpressionSyntax)expressionStatement.Expression;
            var memberAccessExpression = (MemberAccessExpressionSyntax)invocationExpressionSyntax.Expression;
            var name = memberAccessExpression.Name.ToString();
            var isNever = name.Equals("Never");
            var isRepeatAny = name.Equals("Any");
            var isSingleRepeat = isRepeatAny || name.Equals("Once") || name.Equals("AtLeastOnce") || isNever;
            var setup = isSingleRepeat ? "Setup" : "SetupSequence";
            var isExpect = nodeString.Contains(".Expect");

            var node = (InvocationExpressionSyntax)((MemberAccessExpressionSyntax)memberAccessExpression.Expression).Expression;

            var memberOuter = (MemberAccessExpressionSyntax)node.Expression;

            if (memberOuter.Name.ToString() == "IgnoreArguments")
            {
                node = (InvocationExpressionSyntax)memberOuter.Expression;
                memberOuter = (MemberAccessExpressionSyntax)node.Expression;
                setup = "SetupIgnoreArgs";
            }

            var isWithReturn = false;
            if (memberOuter.Expression is InvocationExpressionSyntax nodeInner)
            {
                if (IsSetupLambdaProperty(nodeInner))
                    setup = "SetupSet";
                isWithReturn = true;
                var member = (MemberAccessExpressionSyntax)nodeInner.Expression;
                ExpressionSyntax expression;
                if (member.Expression is MemberAccessExpressionSyntax property)
                {
                    expression = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("Mock"),
                            SyntaxFactory.IdentifierName("Get")),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                            {SyntaxFactory.Argument(property)})));
                }
                else
                {
                    if (member.Expression is InvocationExpressionSyntax ignorExpression
                        && ignorExpression.Expression is MemberAccessExpressionSyntax ignoreMember
                        && member.Name.ToString() == "IgnoreArguments")
                    {
                        nodeInner = ignorExpression;
                        member = ignoreMember;
                        setup = "SetupIgnoreArgs";
                    }

                    var identifier = (IdentifierNameSyntax)member.Expression;
                    var mockGet = SyntaxFactory.IdentifierName(identifier + "Mock");
                    expression = mockGet;
                }

                member = member.WithExpression(expression).WithName(SyntaxFactory.IdentifierName(setup));
                nodeInner = nodeInner.WithExpression(member);
                memberOuter = memberOuter.WithExpression(nodeInner).WithName(SyntaxFactory.IdentifierName("Returns"));
            }
            else if (memberOuter.Expression is IdentifierNameSyntax identifier)
            {
                if (IsSetupLambdaProperty(node))
                    setup = "SetupSet";
                var mockGet = SyntaxFactory.IdentifierName(identifier + "Mock");
                memberOuter = memberOuter.WithExpression(mockGet).WithName(SyntaxFactory.IdentifierName(setup));
            }

            node = node.WithExpression(memberOuter);
            if (name.Equals("Twice"))
            {
                node = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, node, memberOuter.Name),
                    node.ArgumentList);
            }
            else if (name.Equals("Times"))
            {
                var numberExpression = invocationExpressionSyntax.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
                var number = int.Parse(numberExpression.Token.ValueText);
                while (number == 0)
                {
                    node = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, node, memberOuter.Name),
                        node.ArgumentList);
                    number--;
                }
            }

            if (isWithReturn && node.ArgumentList?.Arguments != null && node.ArgumentList.Arguments.Count <= 0)
            {
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
            }

            if (isExpect && isSingleRepeat && !isRepeatAny && !isNever)
            {
                node = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, node, SyntaxFactory.IdentifierName("Verifiable")));
            }
            if (isNever)
            {
                if (!isWithReturn)
                {
                    node = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression, node,
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("Throws"),
                            SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(new[]
                                {(TypeSyntax) SyntaxFactory.IdentifierName("Exception")})))));
                }
            }

            expressionStatement = expressionStatement.WithExpression(node);
            return expressionStatement;
        }

        private static bool IsSetupLambdaProperty(InvocationExpressionSyntax setupCall)
        {
            var arguments = setupCall?.ArgumentList?.Arguments;
            return arguments?.Count > 0 && arguments.Value[0]?.Expression is SimpleLambdaExpressionSyntax s &&
                   s.Body is AssignmentExpressionSyntax;
        }
    }
}
