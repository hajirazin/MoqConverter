using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class BackToRecord : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            return expressionStatement.Expression is InvocationExpressionSyntax i
                   && i.Expression is MemberAccessExpressionSyntax m
                   && m.Name.ToString() == "BackToRecord";
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax i)
                || !(i.Expression is MemberAccessExpressionSyntax m)
                || m.Name.ToString() != "BackToRecord")
                return expressionStatement;

            return expressionStatement.WithExpression(i.WithExpression(m.WithName(SyntaxFactory.IdentifierName("Reset"))
                .WithExpression(SyntaxFactory.IdentifierName(m + "Mock"))));
        }
    }
}
