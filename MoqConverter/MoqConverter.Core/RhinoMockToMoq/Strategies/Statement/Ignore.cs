using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class Ignore : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return false;
            if (!(node.Expression is MemberAccessExpressionSyntax member))
                return false;

            var nodeNameString = member.Name.ToString();
            return nodeNameString.Equals("IgnoreArguments");
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            var n1 = ((MemberAccessExpressionSyntax) ((InvocationExpressionSyntax) expressionStatement.Expression)
                .Expression).Expression;

            if (!(n1 is InvocationExpressionSyntax node))
                return expressionStatement;

            if (!(node.Expression is MemberAccessExpressionSyntax member))
                return expressionStatement;

            if (!(member.Expression is IdentifierNameSyntax identifier))
                return expressionStatement;

            var nodeString = expressionStatement.ToString();
            var isExpect = nodeString.Contains(".Expect");

            var setup = "SetupIgnoreArgs";
            var lambdaBody = ((LambdaExpressionSyntax)node.ArgumentList.Arguments[0].Expression).Body;
            if (lambdaBody is AssignmentExpressionSyntax)
            {
                setup = "SetupSet";
            }

            member = member.WithName(SyntaxFactory.IdentifierName(setup))
                .WithExpression(SyntaxFactory.IdentifierName(identifier + "Mock"));

            node = node.WithExpression(member);

            if (isExpect)
            {
                node = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, node, SyntaxFactory.IdentifierName("Verifiable")));
            }

            expressionStatement = expressionStatement.WithExpression(node);
            return expressionStatement;
        }
    }
}