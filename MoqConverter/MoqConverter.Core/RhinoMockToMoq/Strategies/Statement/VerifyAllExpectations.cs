using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class VerifyAllExpectations : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return false;
            if (!(node.Expression is MemberAccessExpressionSyntax member))
                return false;

            var nodeNameString = member.Name.ToString();
            return nodeNameString.Equals("VerifyAllExpectations");
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return expressionStatement;
            if (!(node.Expression is MemberAccessExpressionSyntax member))
                return expressionStatement;

            var memberExpression = member.Expression;
            var mockGet = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Mock"), SyntaxFactory.IdentifierName("Get")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[] {SyntaxFactory.Argument(memberExpression)})));

            return expressionStatement.WithExpression(node.WithExpression(member.WithExpression(mockGet).WithName(SyntaxFactory.IdentifierName("VerifyAll"))));
        }
    }
}
