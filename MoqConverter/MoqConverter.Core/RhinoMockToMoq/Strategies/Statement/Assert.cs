using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class Assert : IStatementStrategy
    {
        private readonly string _fromString;
        private readonly string _toString;

        public Assert(string fromString, string toString)
        {
            _fromString = fromString;
            _toString = toString;
        }

        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            return expressionStatement.ToString().Contains(_fromString);
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node)) return expressionStatement;

            if (!(node.Expression is MemberAccessExpressionSyntax member))
                return expressionStatement;

            var memberExpression = member.Expression;
            var mockGet = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Mock"), SyntaxFactory.IdentifierName("Get")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(memberExpression) })));

            member = member.WithExpression(mockGet).WithName(SyntaxFactory.IdentifierName("Verify"));
            node = node.WithExpression(member);

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
