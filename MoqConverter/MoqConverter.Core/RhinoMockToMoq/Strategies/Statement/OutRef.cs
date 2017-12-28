using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class OutRef : Return, IStatementStrategy
    {
        public override bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax invocation) ||
                !(invocation.Expression is MemberAccessExpressionSyntax member) ||
                member.Name.ToString() != "OutRef" ||
                !(member.Expression is InvocationExpressionSyntax inner))
                return false;

            var statement = SyntaxFactory.ExpressionStatement(inner);
            return base.IsEligible(statement);
        }

        public override ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax invocation) ||
                !(invocation.Expression is MemberAccessExpressionSyntax member) ||
                !(member.Expression is InvocationExpressionSyntax inner))
                return expressionStatement;

            var statement = SyntaxFactory.ExpressionStatement(inner);
            statement = base.Visit(statement);
            return expressionStatement.WithExpression(invocation.WithExpression(member.WithExpression(statement.Expression)));
        }
    }
}
