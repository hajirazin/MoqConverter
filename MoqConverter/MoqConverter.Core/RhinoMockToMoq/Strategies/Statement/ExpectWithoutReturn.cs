using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class ExpectWithoutReturn : IStatementStrategy
    {
        private static readonly ConcurrentDictionary<string, int> VariableCount = new ConcurrentDictionary<string, int>();

        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            var nodeString = expressionStatement.ToString();
            return (nodeString.Contains(".Expect") || nodeString.Contains(".Stub")) && !nodeString.Contains("Return");
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return expressionStatement;

            if (!(node.Expression is MemberAccessExpressionSyntax member))
                return expressionStatement;

            if (!(member.Expression is IdentifierNameSyntax identifier))
                return expressionStatement;

            var nodeString = expressionStatement.ToString();
            var isExpect = nodeString.Contains(".Expect");

            var setup = "Setup";
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
            //if (!(expressionStatement.Expression is InvocationExpressionSyntax node)) return expressionStatement;

            //var nodeString = node.ToString();

            //var memberAccessExpressionSyntax = (MemberAccessExpressionSyntax)node.Expression;
            //if (memberAccessExpressionSyntax.Name.ToString().Contains("Ignore"))
            //{
            //    node = (InvocationExpressionSyntax)memberAccessExpressionSyntax.Expression;
            //}

            //memberAccessExpressionSyntax = (MemberAccessExpressionSyntax)node.Expression;
            //var expression = memberAccessExpressionSyntax.Expression;

            //if (!nodeString.Contains("Receive"))
            //{
            //    var syntaxArgument = nodeString.Contains("Ignore") ? "ReceivedWithAnyArgs" : "Received";
            //    var syntax = SyntaxFactory.InvocationExpression(
            //        SyntaxFactory.MemberAccessExpression(
            //            node.Expression.Kind(),
            //            expression,
            //            SyntaxFactory.Token(SyntaxKind.DotToken),
            //            SyntaxFactory.IdentifierName(syntaxArgument)));
            //    expression = syntax;
            //}

            //var body = ((SimpleLambdaExpressionSyntax)node.ArgumentList.Arguments[0].Expression).Body;
            //if (body is InvocationExpressionSyntax invocationExpressionSyntax)
            //{
            //    var a = ((MemberAccessExpressionSyntax)invocationExpressionSyntax.Expression).WithExpression(expression);

            //    return expressionStatement.WithExpression(invocationExpressionSyntax.WithExpression(a));
            //}
            //if (body is AssignmentExpressionSyntax assignment)
            //{
            //    var left = (MemberAccessExpressionSyntax)assignment.Left;
            //    return expressionStatement.WithExpression(assignment.WithLeft(left.WithExpression(expression)));
            //}
            //if (body is MemberAccessExpressionSyntax m)
            //{
            //    var a = m.WithExpression(expression);
            //    var x = "var variableToMakeCompilerHappy = " + a;
            //    var expressionStatementSyntax = SyntaxFactory.ParseStatement(x) as ExpressionStatementSyntax;
            //    return expressionStatementSyntax;
            //}

            //return expressionStatement.WithExpression(node);
        }
    }
}
