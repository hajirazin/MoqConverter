using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class ExpectWithCall : IStatementStrategy
    {
        public virtual bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return false;
            if (!(node.Expression is MemberAccessExpressionSyntax smes))
                return false;

            if (!(smes.Expression is InvocationExpressionSyntax nodeInner))
                return false;

            if (!(nodeInner.Expression is MemberAccessExpressionSyntax member))
                return false;

            if (!(member.Expression is IdentifierNameSyntax) && !(member.Expression is MemberAccessExpressionSyntax))
                return false;

            var nodeString = node.ToString();
            var nodeNameString = smes.Name.ToString();

            return (nodeString.Contains("Expect") || nodeString.Contains("Stub")) &&
                   nodeNameString.Contains("Return") && member.Name.ToString().Equals("Call");
        }

        public virtual ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            var returnInvocation = (InvocationExpressionSyntax) expressionStatement.Expression;
            var returnMember = (MemberAccessExpressionSyntax) returnInvocation.Expression;
            returnMember = returnMember.WithName(SyntaxFactory.IdentifierName("Returns"));

            var callInvocation = (InvocationExpressionSyntax)returnMember.Expression;
            var argument = callInvocation.ArgumentList.Arguments[0].Expression;
            if (argument is MemberAccessExpressionSyntax argumentMember)
            {
                var identifier = (IdentifierNameSyntax) argumentMember.Expression;
                identifier = SyntaxFactory.IdentifierName(identifier + "Mock");
                var setupMember = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    identifier,
                    SyntaxFactory.IdentifierName("Setup"));

                var lambdaParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("s"));
                var setupLambda = SyntaxFactory.SimpleLambdaExpression(lambdaParameter,
                    argumentMember.WithExpression(SyntaxFactory.IdentifierName("s")));

                var setupInvocation = SyntaxFactory.InvocationExpression(setupMember,
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(setupLambda)
                    })));

                returnMember = returnMember.WithExpression(setupInvocation);
                returnInvocation = returnInvocation.WithExpression(returnMember);
                expressionStatement = expressionStatement.WithExpression(returnInvocation);
            }else if (argument is InvocationExpressionSyntax argumentInvo)
            {
                var argumentMember1 = (MemberAccessExpressionSyntax)argumentInvo.Expression;
                var identifier = (IdentifierNameSyntax)argumentMember1.Expression;
                identifier = SyntaxFactory.IdentifierName(identifier + "Mock");
                var setupMember = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    identifier,
                    SyntaxFactory.IdentifierName("Setup"));

                var lambdaParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("s"));
                argumentInvo = argumentInvo.WithExpression(argumentMember1.WithExpression(SyntaxFactory.IdentifierName("s")));    
                var setupLambda = SyntaxFactory.SimpleLambdaExpression(lambdaParameter,
                    argumentInvo);

                var setupInvocation = SyntaxFactory.InvocationExpression(setupMember,
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(setupLambda)
                    })));


                returnMember = returnMember.WithExpression(setupInvocation);
                returnInvocation = returnInvocation.WithExpression(returnMember);
                expressionStatement = expressionStatement.WithExpression(returnInvocation);
            }

            return expressionStatement;

        }
    }
}


