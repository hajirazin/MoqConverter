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

            if (!(member.Expression is IdentifierNameSyntax identifier))
                return expressionStatement;

            var memberExpression = SyntaxFactory.IdentifierName(identifier.Identifier.ValueText + "Mock");

            return expressionStatement.WithExpression(node.WithExpression(member.WithExpression(memberExpression)
                .WithName(SyntaxFactory.IdentifierName("Verify"))));
        }
    }
}
