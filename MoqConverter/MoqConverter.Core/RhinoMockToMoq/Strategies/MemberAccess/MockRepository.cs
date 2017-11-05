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
            if (expressionStatement.Expression is InvocationExpressionSyntax node &&
                node.Expression is MemberAccessExpressionSyntax member &&
                member.Expression is IdentifierNameSyntax identifier
                && identifier.ToString().Equals("MockRepository")
                && member.Name is GenericNameSyntax typeArgument)
            {
                typeArgument = typeArgument.WithIdentifier(SyntaxFactory.Identifier("Of"));
                node = node.WithExpression(member.WithExpression(SyntaxFactory.IdentifierName("Mock"))
                    .WithName(typeArgument));
                expressionStatement = expressionStatement.WithExpression(node);
            }

            return expressionStatement;
        }
    }
}
