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
        private readonly List<string> _variables = new List<string>();

        public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.Name.ToString().Equals("NSubstitute"))
                return node.WithName(SyntaxFactory.IdentifierName("Moq"));

            if (node.Name.ToString().Contains("NSubstitute"))
                return null;

            return base.VisitUsingDirective(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var returnObject = base.VisitClassDeclaration(node);
            if (returnObject is ClassDeclarationSyntax newClass)
            {
                var members = newClass.Members;
                var fields = newClass.Members.OfType<FieldDeclarationSyntax>();
                foreach (var field in fields)
                {
                    if (field.Declaration.Variables.Any(v => _variables.Contains(v.ToString())))
                    {
                        var newField = SyntaxFactory.FieldDeclaration(field.AttributeLists,
                            field.Modifiers,
                            SyntaxFactory.VariableDeclaration(
                                SyntaxFactory.GenericName(SyntaxFactory.Identifier("Mock"),
                                    SyntaxFactory.TypeArgumentList(
                                        SyntaxFactory.SeparatedList(new[] { field.Declaration.Type }))),
                                SyntaxFactory.SeparatedList(new[]
                                {
                                    SyntaxFactory.VariableDeclarator(
                                        SyntaxFactory.Identifier(
                                            field.Declaration.Variables[0].Identifier.ValueText + "Mock"))
                                })));

                        var currentMember = members.FirstOrDefault(m => m is FieldDeclarationSyntax f &&
                            f.Declaration.Variables[0].Identifier.ValueText
                                .Equals(field.Declaration.Variables[0].Identifier.ValueText));

                        members = members.ReplaceRange(currentMember, new[] { field, newField });
                    }
                }

                returnObject = newClass.WithMembers(members);
            }

            return returnObject;
        }

        private static bool IsSame(StatementSyntax statement, string otherVariable)
        {
            return statement is ExpressionStatementSyntax expressionStatement &&
                   expressionStatement.Expression is AssignmentExpressionSyntax assignment &&
                   assignment.Left is IdentifierNameSyntax variableName
                   && otherVariable.Equals(variableName.ToString());
        }

        private static bool IsSameLocal(StatementSyntax statement, string otherVariable)
        {
            return statement is LocalDeclarationStatementSyntax expressionStatement &&
                   expressionStatement.Declaration.Variables[0].Identifier.ValueText.Equals(otherVariable);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            //if (node.AttributeLists.Any(f => f.Attributes.Any(a => a.Name.ToString().Equals("Ignore"))))
            //{
            //    return null;
            //}

            var statements = node.Body.Statements;
            foreach (var statement in node.Body.Statements)
            {
                if (statement is ExpressionStatementSyntax expressionStatement &&
                    expressionStatement.Expression is AssignmentExpressionSyntax assignment &&
                    assignment.Left is IdentifierNameSyntax variableName &&
                    assignment.Right is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax member &&
                    member.Expression is IdentifierNameSyntax identifier
                    && identifier.ToString().Equals("Substitute")
                    && member.Name is GenericNameSyntax typeArgument)
                {
                    _variables.Add(variableName.ToString());
                    var objectCreation =
                        SyntaxFactory.ObjectCreationExpression(
                            typeArgument.WithIdentifier(SyntaxFactory.Identifier("Mock")),
                            invocation.ArgumentList,
                            null);

                    member = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(variableName + "Mock"),
                        SyntaxFactory.IdentifierName("Object"));

                    assignment = assignment.WithRight(member);

                    var newAssignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(variableName + "Mock"), objectCreation);

                    var objectCreationStatement = SyntaxFactory.ExpressionStatement(assignment);
                    var x = statements.FirstOrDefault(f => IsSame(f, variableName.ToString()));
                    statements = statements.ReplaceRange(x,
                        new[] { SyntaxFactory.ExpressionStatement(newAssignment), objectCreationStatement });
                }

                else if (statement is LocalDeclarationStatementSyntax localDeclaration &&
                    localDeclaration.Declaration.Variables[0] is VariableDeclaratorSyntax variable &&
                    variable.Initializer.Value is InvocationExpressionSyntax invocation1 &&
                    invocation1.Expression is MemberAccessExpressionSyntax member1 &&
                    member1.Expression is IdentifierNameSyntax identifier1
                    && identifier1.ToString().Equals("Substitute")
                    && member1.Name is GenericNameSyntax typeArgument1)
                {

                    var objectCreation =
                        SyntaxFactory.ObjectCreationExpression(
                            typeArgument1.WithIdentifier(SyntaxFactory.Identifier("Mock")),
                            invocation1.ArgumentList,
                            null);

                    member = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(variable.Identifier + "Mock"),
                        SyntaxFactory.IdentifierName("Object"));

                    variable = variable.WithInitializer(variable.Initializer.WithValue(member));
                    localDeclaration = localDeclaration.WithDeclaration(
                        localDeclaration.Declaration.WithVariables(SyntaxFactory.SeparatedList(new[] { variable })));

                    var newDeclaration = localDeclaration.WithDeclaration(
                        localDeclaration.Declaration.WithVariables(SyntaxFactory.SeparatedList(new[]
                        {
                                SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variable.Identifier + "Mock"),
                                    variable.ArgumentList,
                                    variable.Initializer.WithValue(objectCreation))
                        })).WithType(SyntaxFactory.ParseTypeName("var")));

                    var x = statements.FirstOrDefault(f => IsSameLocal(f, variable.Identifier.ToString()));
                    statements = statements.ReplaceRange(x,
                        new[] { newDeclaration, localDeclaration });
                }
            }

            var body = node.Body.WithStatements(statements);
            node = node.WithBody(body);
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var member = node.Expression as MemberAccessExpressionSyntax;
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

            else if (isMember && member.Name.ToString().Equals("Returns"))
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

                    var setup = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression, mockGet, SyntaxFactory.IdentifierName("Setup")),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(lambda) })));

                    member = member.WithExpression(setup);

                    node = node.WithExpression(member);
                }
            }

            else if (isMember && member.Name.ToString().Equals("ReturnsForAnyArgs"))
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

                    var setup = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression, mockGet,
                            SyntaxFactory.IdentifierName("SetupIgnoreArgs")),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(lambda) })));

                    member = member.WithExpression(setup).WithName(SyntaxFactory.IdentifierName("Returns"));

                    node = node.WithExpression(member);
                }
            }

            else if (isMember && member.Expression is InvocationExpressionSyntax receivedExpression &&
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
