using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoqConverter.Core.Converters;

namespace MoqConverter.Core.NSubstituteToMoq
{
    public class Rewritter : FileRewritter
    {

        public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.Name.ToString().Equals("NSubstitute"))
                return node.WithName(SyntaxFactory.IdentifierName("Moq"));

            if (node.Name.ToString().Contains("NSubstitute"))
                return null;

            return base.VisitUsingDirective(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            return VisitMethodDeclaration(node, "Substitute");
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var member = node.Expression as MemberAccessExpressionSyntax;
            var isReturnForAnyArgs = false;
            bool isMember = member != null;
            if (isMember &&
                member.Expression is IdentifierNameSyntax identifier
                && identifier.ToString().Equals("Substitute")
                && member.Name is GenericNameSyntax typeArgument)
            {
                typeArgument = typeArgument.WithIdentifier(SyntaxFactory.Identifier("Of"));
                node = node.WithExpression(member.WithExpression(SyntaxFactory.IdentifierName("Mock"))
                    .WithName(typeArgument));
            }

            else if (isMember && (member.Name.ToString().Equals("Returns") ||
                                  (isReturnForAnyArgs = member.Name.ToString().Equals("ReturnsForAnyArgs"))))
            {
                IdentifierNameSyntax mockObject = null;
                SimpleLambdaExpressionSyntax lambda = null;

                ExpressionSyntax GetSetup(ExpressionSyntax expression)
                {
                    switch (expression)
                    {
                        case InvocationExpressionSyntax invocation:
                            return invocation.WithExpression(GetSetup(invocation.Expression));
                        case MemberAccessExpressionSyntax m:
                            return m.WithExpression(GetSetup(m.Expression));
                        case IdentifierNameSyntax i:
                            mockObject = i;
                            return SyntaxFactory.IdentifierName("setup");
                        default:
                            return SyntaxFactory.IdentifierName("setup");
                    }
                }

                lambda = SyntaxFactory.SimpleLambdaExpression(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("setup")),
                    GetSetup(member.Expression));

                if (mockObject != null)
                {
                    var mockGet = SyntaxFactory.IdentifierName(mockObject.Identifier.ValueText + "Mock");

                    var name = isReturnForAnyArgs && member.Expression is InvocationExpressionSyntax
                        ? "SetupIgnoreArgs"
                        : "Setup";
                    var setup = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression, mockGet, SyntaxFactory.IdentifierName(name)),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[] {SyntaxFactory.Argument(lambda)})));

                    member = member.WithExpression(setup).WithName(SyntaxFactory.IdentifierName("Returns"));

                    node = node.WithExpression(member);
                }
            }

            else if (isMember && member.Expression is InvocationExpressionSyntax receivedExpression &&
                     receivedExpression.Expression is MemberAccessExpressionSyntax received &&
                     (received.Name.ToString().Equals("Received") ||
                      received.Name.ToString().Contains("DidNotReceive")))
            {
                var timesText = received.Name.ToString().Contains("DidNotReceive")
                    ? "Times.Never"
                    : "Times.AtLeastOnce";
                if (receivedExpression.ArgumentList.Arguments.Count != 0)
                {
                    if (receivedExpression.ArgumentList.Arguments[0]
                            .Expression is LiteralExpressionSyntax numberString &&
                        int.TryParse(numberString.ToString(), out var number) && number == 1)
                    {
                        timesText = "Times.Once";
                    }
                }

                var mockGet = SyntaxFactory.IdentifierName(received.Expression + "Mock");
                
                var lambda = SyntaxFactory.SimpleLambdaExpression(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("verify")),
                    node.WithExpression(member.WithExpression(SyntaxFactory.IdentifierName("verify"))));

                var verify = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression, mockGet, SyntaxFactory.IdentifierName("Verify")),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Argument(lambda),
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression(timesText))
                        })));

                node = verify;

            }
            else if (node.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
            {
                if (memberAccessExpressionSyntax.Expression is IdentifierNameSyntax identifierSyntax &&
                    identifierSyntax.ToString().Equals("Arg"))
                {
                    memberAccessExpressionSyntax =
                        memberAccessExpressionSyntax.WithExpression(SyntaxFactory.IdentifierName("It"));
                    if (node.ArgumentList.Arguments.Count == 1 && !node.ArgumentList.Arguments[0].Expression
                            .IsKind(SyntaxKind.SimpleLambdaExpression))
                    {
                        memberAccessExpressionSyntax =
                            memberAccessExpressionSyntax.WithName(SyntaxFactory.IdentifierName("IsIn"));
                    }

                    if (memberAccessExpressionSyntax.Name is GenericNameSyntax genericName &&
                        genericName.Identifier.ValueText.Equals("Any"))
                    {
                        genericName = genericName.WithIdentifier(SyntaxFactory.Identifier("IsAny"));
                        memberAccessExpressionSyntax = memberAccessExpressionSyntax.WithName(genericName);
                    }
                    node = node.WithExpression(memberAccessExpressionSyntax);
                }
            }

            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (node.Left is MemberAccessExpressionSyntax member)
            {
                if (member.Expression is InvocationExpressionSyntax receivedExpression &&
                    receivedExpression.Expression is MemberAccessExpressionSyntax received &&
                    (received.Name.ToString().Equals("Received") || received.Name.ToString().Equals("DidNotReceive")))
                {
                    var timesText = received.Name.ToString().Equals("DidNotReceive") ? "Times.Never" : "Times.AtLeastOnce";
                    if (receivedExpression.ArgumentList.Arguments.Count != 0)
                    {
                        if (receivedExpression.ArgumentList.Arguments[0]
                                .Expression is LiteralExpressionSyntax numberString &&
                            int.TryParse(numberString.ToString(), out var number) && number == 1)
                        {
                            timesText = "Times.Once";
                        }
                    }

                    var mockGet = SyntaxFactory.IdentifierName(received.Expression + "Mock"); ;

                    var lambda = SyntaxFactory.SimpleLambdaExpression(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("verify")),
                        node.WithLeft(member.WithExpression(SyntaxFactory.IdentifierName("verify"))));

                    var verify = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression, mockGet, SyntaxFactory.IdentifierName("VerifySet")),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxFactory.Argument(lambda),
                                SyntaxFactory.Argument(SyntaxFactory.ParseExpression(timesText))
                            })));

                    return verify;

                }
            }
            return base.VisitAssignmentExpression(node);
        }

        public override bool IsValidFile(CompilationUnitSyntax root)
        {
            return root != null && root.Usings.ToList().Any(u => u.Name.ToString().Contains("NSubstitute"));
        }
    }
}
