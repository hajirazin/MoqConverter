using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace MoqConverter.Core.Converters
{
    public abstract class FileRewritter : CSharpSyntaxRewriter
    {
        protected readonly HashSet<string> Variables = new HashSet<string>();

        public abstract bool IsValidFile(CompilationUnitSyntax root);

        public SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node, string mocker)
        {
            var statements = node.Body.Statements;
            foreach (var statement in node.Body.Statements)
            {
                try
                {
                    if (statement is ExpressionStatementSyntax expressionStatement &&
                        expressionStatement.Expression is AssignmentExpressionSyntax assignment &&
                        assignment.Left is IdentifierNameSyntax variableName &&
                        assignment.Right is InvocationExpressionSyntax invocation &&
                        invocation.Expression is MemberAccessExpressionSyntax member &&
                        member.Expression is IdentifierNameSyntax identifier
                        ///  && identifier.ToString().Equals(mocker, StringComparison.OrdinalIgnoreCase)
                        && member.Name is GenericNameSyntax typeArgument)
                    {
                        Variables.Add(variableName.ToString());
                        var objectCreation =
                            SyntaxFactory.ObjectCreationExpression(
                                typeArgument.WithIdentifier(SyntaxFactory.Identifier("Mock")),
                                invocation.ArgumentList,
                                null);

                        if (typeArgument.Identifier.ToString() == "GeneratePartialMock")
                        {
                            objectCreation = objectCreation.WithInitializer(SyntaxFactory.InitializerExpression(
                                SyntaxKind.ObjectInitializerExpression,
                                SyntaxFactory.SeparatedList(new ExpressionSyntax[]
                                {
                                    SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        SyntaxFactory.IdentifierName("CallBase"), SyntaxFactory.IdentifierName("true"))
                                })));
                        }
                        else if (typeArgument.Identifier.ToString() == "StrictMock")
                        {
                            objectCreation = objectCreation.WithArgumentList(objectCreation.ArgumentList.WithArguments(
                                SyntaxFactory.SeparatedList(new[]
                                {
                                    SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("MockBehavior"),
                                        SyntaxFactory.IdentifierName("Strict")))
                                })));
                        }

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
                             variable.Initializer != null &&
                             variable.Initializer.Value is InvocationExpressionSyntax invocation1 &&
                             invocation1.Expression is MemberAccessExpressionSyntax member1)
                    {
                        if (member1.Expression is IdentifierNameSyntax identifier1 &&
                            // identifier1.ToString().Equals(mocker, StringComparison.OrdinalIgnoreCase) &&
                            member1.Name is GenericNameSyntax typeArgument1)
                        {
                            Variables.Add(variable.Identifier.ToString());
                            var objectCreation =
                                SyntaxFactory.ObjectCreationExpression(
                                    typeArgument1.WithIdentifier(SyntaxFactory.Identifier("Mock")),
                                    invocation1.ArgumentList,
                                    null);

                            if (typeArgument1.Identifier.ToString() == "GeneratePartialMock")
                            {
                                objectCreation = objectCreation.WithInitializer(SyntaxFactory.InitializerExpression(
                                    SyntaxKind.ObjectInitializerExpression,
                                    SyntaxFactory.SeparatedList(new ExpressionSyntax[]
                                    {
                                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                            SyntaxFactory.IdentifierName("CallBase"), SyntaxFactory.IdentifierName("true"))
                                    })));
                            }
                            else if (typeArgument1.Identifier.ToString() == "StrictMock")
                            {
                                objectCreation = objectCreation.WithArgumentList(objectCreation.ArgumentList.WithArguments(
                                    SyntaxFactory.SeparatedList(new[]
                                    {
                                        SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("MockBehavior"),
                                            SyntaxFactory.IdentifierName("Strict")))
                                    })));
                            }

                            member = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(variable.Identifier + "Mock"),
                                SyntaxFactory.IdentifierName("Object"));

                            variable = variable.WithInitializer(variable.Initializer.WithValue(member));
                            localDeclaration = localDeclaration.WithDeclaration(
                                localDeclaration.Declaration.WithVariables(
                                    SyntaxFactory.SeparatedList(new[] { variable })));

                            var newDeclaration = localDeclaration.WithDeclaration(
                                localDeclaration.Declaration.WithVariables(SyntaxFactory.SeparatedList(new[]
                                {
                                    SyntaxFactory.VariableDeclarator(
                                        SyntaxFactory.Identifier(variable.Identifier + "Mock"),
                                        variable.ArgumentList,
                                        variable.Initializer.WithValue(objectCreation))
                                })).WithType(SyntaxFactory.ParseTypeName("var")));

                            var x = statements.FirstOrDefault(f => IsSameLocal(f, variable.Identifier.ToString()));
                            statements = statements.ReplaceRange(x,
                                new[] { newDeclaration, localDeclaration });
                        }
                        else if (member1.Expression is ObjectCreationExpressionSyntax objectCreationExpression
                            && objectCreationExpression.Type.ToString() == "MockRepository" &&
                            member1.Name is GenericNameSyntax genericName &&
                            genericName.Identifier.ToString() == "PartialMock")
                        {
                            Variables.Add(variable.Identifier.ToString());
                            var objectCreation =
                                SyntaxFactory.ObjectCreationExpression(
                                    genericName.WithIdentifier(SyntaxFactory.Identifier("Mock")),
                                    invocation1.ArgumentList,
                                    null);

                            objectCreation = objectCreation.WithInitializer(SyntaxFactory.InitializerExpression(
                                SyntaxKind.ObjectInitializerExpression,
                                SyntaxFactory.SeparatedList(new ExpressionSyntax[]
                                {
                                    SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        SyntaxFactory.IdentifierName("CallBase"),
                                        SyntaxFactory.IdentifierName("true"))
                                })));

                            member = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(variable.Identifier + "Mock"),
                                SyntaxFactory.IdentifierName("Object"));

                            variable = variable.WithInitializer(variable.Initializer.WithValue(member));
                            localDeclaration = localDeclaration.WithDeclaration(
                                localDeclaration.Declaration.WithVariables(
                                    SyntaxFactory.SeparatedList(new[] { variable })));

                            var newDeclaration = localDeclaration.WithDeclaration(
                                localDeclaration.Declaration.WithVariables(SyntaxFactory.SeparatedList(new[]
                                {
                                    SyntaxFactory.VariableDeclarator(
                                        SyntaxFactory.Identifier(variable.Identifier + "Mock"),
                                        variable.ArgumentList,
                                        variable.Initializer.WithValue(objectCreation))
                                })).WithType(SyntaxFactory.ParseTypeName("var")));

                            var x = statements.FirstOrDefault(f => IsSameLocal(f, variable.Identifier.ToString()));
                            statements = statements.ReplaceRange(x,
                                new[] { newDeclaration, localDeclaration });
                        }
                    }
                }
                catch (Exception exception)
                {
                    Logger.Log("Exception in VisitMethodDeclaration", ConsoleColor.Yellow);
                }
            }

            try
            {
                var body = node.Body.WithStatements(statements);
                node = node.WithBody(body);
            }
            catch
            {
                Logger.Log("Exception in VisitMethodDeclaration", ConsoleColor.Yellow);
            }

            return base.VisitMethodDeclaration(node);
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
                    if (field.Declaration.Variables[0].Identifier.ValueText == "MockRepository")
                    {
                        members = members.Remove(field);
                    }

                    else if (field.Declaration.Variables.Any(v => Variables.Contains(v.ToString())))
                    {
                        var newField = SyntaxFactory.FieldDeclaration(field.AttributeLists,
                            SyntaxFactory.TokenList(field.Modifiers.Select(s => s.WithoutTrivia())),
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

                        var currentMember = members
                            .FirstOrDefault(m => m is FieldDeclarationSyntax f &&
                                                 f.Declaration.Variables[0].Identifier.ValueText
                                                     .Equals(field.Declaration.Variables[0]
                                                         .Identifier.ValueText));

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
    }
}
