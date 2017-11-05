using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class Assert : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            var str = expressionStatement.ToString();
            return str.Contains("AssertWasCalled") || str.Contains("AssertWasNotCalled");
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node)) return expressionStatement;

            if (!(node.Expression is MemberAccessExpressionSyntax member))
                return expressionStatement;

            if (!(member.Expression is IdentifierNameSyntax identifier))
                return expressionStatement;

            var verify = "Verify";
            var lambdaBody = ((LambdaExpressionSyntax)node.ArgumentList.Arguments[0].Expression).Body;
            if (lambdaBody is AssignmentExpressionSyntax)
            {
                verify = "VerifySet";
            }

            var str = node.ToString().Contains("AssertWasNotCalled") ? "Times.Never" : "Times.AtLeastOnce";
            var mockGet = SyntaxFactory.IdentifierName(identifier.Identifier.ValueText + "Mock");

            member = member.WithExpression(mockGet).WithName(SyntaxFactory.IdentifierName(verify));
            node = node.WithExpression(member);
            node = node.WithArgumentList(node.ArgumentList.WithArguments(
                node.ArgumentList.Arguments.Add(SyntaxFactory.Argument(SyntaxFactory.ParseExpression(str)))));
            return expressionStatement.WithExpression(node);
            //if (!(expressionStatement.Expression is InvocationExpressionSyntax node)) return expressionStatement;
            //var body = ((SimpleLambdaExpressionSyntax)node.ArgumentList.Arguments[0].Expression).Body;
            //if (body is InvocationExpressionSyntax call)
            //{
            //    var sme = (MemberAccessExpressionSyntax)call.Expression;
            //    var nodeExpression = ((MemberAccessExpressionSyntax)node.Expression).Expression;
            //    var x = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(sme.Kind(),
            //        nodeExpression, SyntaxFactory.Token(SyntaxKind.DotToken),
            //        SyntaxFactory.IdentifierName(_toString)));
            //    var d = call.WithExpression(sme.WithExpression(x));
            //    return expressionStatement.WithExpression(d);
            //}
            //else if (body is AssignmentExpressionSyntax assignment)
            //{
            //    var left = (MemberAccessExpressionSyntax)assignment.Left;
            //    var nodeExpression = ((MemberAccessExpressionSyntax)node.Expression).Expression;
            //    var x = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(left.Kind(),
            //        nodeExpression, SyntaxFactory.Token(SyntaxKind.DotToken),
            //        SyntaxFactory.IdentifierName(_toString)));
            //    var d = assignment.WithLeft(left.WithExpression(x));
            //    return expressionStatement.WithExpression(d);
            //}
            //else if (body is BlockSyntax)
            //{
            //    var nodeString = node.ToString();
            //    if (nodeString.Contains("Ignore"))
            //    {
            //        nodeString = nodeString.Replace(_fromString, _toString + "().WhenForAnyArgs");
            //        nodeString = nodeString.Replace(".IgnoreArguments()", string.Empty);
            //    }
            //    else
            //    {
            //        nodeString = nodeString.Replace(_fromString, _toString + "().When");
            //    }
            //    return expressionStatement.WithExpression(SyntaxFactory.ParseExpression(nodeString));
            //}

            //return expressionStatement.WithExpression(node);
        }
    }
}
