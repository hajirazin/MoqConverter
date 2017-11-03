using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class MockRepository : IStatementStrategy
    {
        private static readonly Regex Expression = new Regex("[A-Za-z0-9_]*\\.(Generate|Strict|GenerateStrict|Dynamic|GenerateDynamic|Partial|GeneratePartial)(Mock|Stub)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase); 

        public bool IsEligible(ExpressionStatementSyntax expressionStatementSyntax)
        {
            var nodeString = expressionStatementSyntax.ToString();
            return Expression.IsMatch(nodeString);
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is AssignmentExpressionSyntax assignment))
                return expressionStatement;

            if (!(assignment.Right is InvocationExpressionSyntax node))
                return expressionStatement;

            if (!(node.Expression is MemberAccessExpressionSyntax member))
                return expressionStatement;

            var typeArgument = member.Name as GenericNameSyntax;
            var objectCreation =
                SyntaxFactory.ObjectCreationExpression(typeArgument.WithIdentifier(SyntaxFactory.Identifier("Mock")),
                    node.ArgumentList,
                    SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression));

           member = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, objectCreation,
                SyntaxFactory.IdentifierName("Object"));
            var expressionStatementSyntax = expressionStatement.WithExpression(assignment.WithRight(member));
            return expressionStatementSyntax;
            //      nodeString = Regex.Replace(nodeString, "MockRepository\\.GenerateStub", "Substitute.For");
            //nodeString = Regex.Replace(nodeString, "MockRepository\\.GenerateMock<[a-zA-Z0-9,_ <>\\[\\]]+>\\(([a-zA-Z0-9_]*)\\)", "Mock.Of");
            //    nodeString = Regex.Replace(nodeString, "MockRepository\\.GenerateStrictMock", "Substitute.For");
            // nodeString = Expression.Replace(nodeString, "Substitute.For");
        }
    }
}
