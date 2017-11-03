using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class ReturnWithIgnore : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return false;
            var nodeString = node.ToString();
            if (!(node.Expression is MemberAccessExpressionSyntax smes))
                return false;

            var nodeNameString = smes.Name.ToString();
            return (nodeString.Contains(".Expect") || nodeString.Contains("Stub")) &&
                   nodeNameString.Contains("Return") &&
                   nodeString.Contains("Ignore");
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            var str = expressionStatement.ToString();
            str = str.Replace(".IgnoreArguments()", string.Empty);
            return SyntaxFactory.ParseStatement(str) as ExpressionStatementSyntax;
            //if (!(expressionStatement.Expression is InvocationExpressionSyntax node)) return expressionStatement;
            //var maes = (MemberAccessExpressionSyntax)node.Expression;
            //var smes =
            //    (InvocationExpressionSyntax)((MemberAccessExpressionSyntax)
            //        ((InvocationExpressionSyntax)maes.Expression).Expression)
            //    .Expression;
            //var iex = ((MemberAccessExpressionSyntax)smes.Expression).Expression;
            //var a =
            //    (InvocationExpressionSyntax)((SimpleLambdaExpressionSyntax)smes.ArgumentList.Arguments[0]
            //        .Expression).Body;
            //var x = a.WithExpression(((MemberAccessExpressionSyntax)a.Expression).WithExpression(iex));
            //maes = maes.WithExpression(x).WithName(SyntaxFactory.IdentifierName("ReturnsForAnyArgs"));
            //var invocationExpressionSyntax = node.WithExpression(maes);
            //return expressionStatement.WithExpression(invocationExpressionSyntax);
        }
    }
}
