using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoqConverter.Core.Converters;
using MoqConverter.Core.RhinoMockToMoq.Strategies.Statement;

namespace MoqConverter.Core.RhinoMockToMoq
{
    public partial class Rewritter : FileRewritter
    {
        public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.Name.ToString().Equals("Rhino.Mocks"))
                return node.WithName(SyntaxFactory.IdentifierName("Moq"));

            if (node.Name.ToString().Contains("Rhino.Mocks"))
                return null;

            return base.VisitUsingDirective(node);
        }

        public override bool IsValidFile(CompilationUnitSyntax root)
        {
            return root != null && root.Usings.ToList().Any(u => u.Name.ToString().Contains("Rhino.Mocks"));
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            return VisitMethodDeclaration(node, "MockRepository");
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            try
            {
                if (node.Declaration.Variables[0].Initializer.Value is ObjectCreationExpressionSyntax objectCreation &&
                    objectCreation.Type.ToString() == "MockRepository")
                {
                    return null;
                }
            }
            catch
            {
                Logger.Log("Exception in VisitLocalDeclarationStatement", ConsoleColor.Yellow);
            }

            return base.VisitLocalDeclarationStatement(node);
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax member &&
                member.Expression is IdentifierNameSyntax identifier
                && identifier.ToString().Equals("MockRepository")
                && member.Name is GenericNameSyntax typeArgument)
            {
                if (node.ArgumentList?.Arguments.Any() ?? false)
                {
                    var objectCreation =
                        SyntaxFactory.ObjectCreationExpression(
                            typeArgument.WithIdentifier(SyntaxFactory.Identifier("Mock")),
                            node.ArgumentList,
                            null);
                    var expression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        objectCreation,
                        SyntaxFactory.IdentifierName("Object"));

                    return expression;
                }

                typeArgument = typeArgument.WithIdentifier(SyntaxFactory.Identifier("Of"));
                node = node.WithExpression(member.WithExpression(SyntaxFactory.IdentifierName("Mock"))
                    .WithName(typeArgument));
            }

            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode VisitEmptyStatement(EmptyStatementSyntax node)
        {
            //Simply remove all Empty Statements
            return null;
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            try
            {
                if (node.Left is MemberAccessExpressionSyntax leftMember
                    && leftMember.Expression is IdentifierNameSyntax identifier
                    && Variables.Contains(identifier.ToString()))
                {
                    var lambda = SyntaxFactory.SimpleLambdaExpression(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("setup")),
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("setup"), leftMember.Name));
                    ExpressionSyntax setup = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(identifier + "Mock"),
                            SyntaxFactory.IdentifierName("Setup")),
                        SyntaxFactory.ArgumentList().WithArguments(
                            SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(lambda) })
                        ));

                    var right = node.Right;
                    if (right is InvocationExpressionSyntax invocation)
                    {
                        right = this.VisitInvocationExpression(invocation) as ExpressionSyntax;
                    }

                    right = right ?? node.Right;
                    var rightArgument = SyntaxFactory.Argument(right ?? node.Right);
                    if (right is LiteralExpressionSyntax literal &&
                        literal.Kind() == SyntaxKind.NullLiteralExpression)
                    {
                        rightArgument = rightArgument.WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("value")));
                    }

                    var exp = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, setup,
                            SyntaxFactory.IdentifierName("Returns")),
                        SyntaxFactory.ArgumentList().WithArguments(
                            SyntaxFactory.SeparatedList(new[]
                            {
                                rightArgument
                            })
                        ));

                    return exp;
                }
            }
            catch (Exception exception)
            {
                Logger.Log("Exception in VisitAssignmentExpression", ConsoleColor.Yellow);
            }

            return base.VisitAssignmentExpression(node);
        }
    }
}
