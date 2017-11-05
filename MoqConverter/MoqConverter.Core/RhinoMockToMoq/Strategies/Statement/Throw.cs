using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class Throw : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            return expressionStatement.ToString().Contains(".Throw(");
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node)) return expressionStatement;

            if (!(node.Expression is MemberAccessExpressionSyntax memberOuter))
                return expressionStatement;

            if (!(memberOuter.Expression is InvocationExpressionSyntax nodeInner)) return expressionStatement;

            if (!(nodeInner.Expression is MemberAccessExpressionSyntax member))
                return expressionStatement;

            ExpressionSyntax identifierSyntax;
            if (member.Expression is InvocationExpressionSyntax ignore &&
                ignore.Expression is MemberAccessExpressionSyntax ignoreMember)
            {
                identifierSyntax = ignoreMember.Expression;
                if (identifierSyntax is IdentifierNameSyntax identifier)
                {
                    var mockGet = SyntaxFactory.IdentifierName(identifier.Identifier.ValueText + "Mock");

                    ignoreMember = ignoreMember.WithExpression(mockGet).WithName(SyntaxFactory.IdentifierName("SetupIgnoreArgs"));
                    ignore = ignore.WithExpression(ignoreMember);
                    memberOuter = memberOuter.WithExpression(ignore).WithName(SyntaxFactory.IdentifierName("Throws"));

                    node = node.WithExpression(memberOuter);
                    return expressionStatement.WithExpression(node);
                }
            }
            else
            {
                identifierSyntax = member.Expression;
                if (identifierSyntax is IdentifierNameSyntax identifier)
                {
                    var mockGet = SyntaxFactory.IdentifierName(identifier.Identifier.ValueText + "Mock");

                    member = member.WithExpression(mockGet).WithName(SyntaxFactory.IdentifierName("Setup"));
                    nodeInner = nodeInner.WithExpression(member);
                    memberOuter = memberOuter.WithExpression(nodeInner).WithName(SyntaxFactory.IdentifierName("Throws"));

                    node = node.WithExpression(memberOuter);
                    return expressionStatement.WithExpression(node);
                }
            }

            return expressionStatement;
            //if (!(expressionStatement.Expression is InvocationExpressionSyntax node)) return expressionStatement;
            //if (node.Expression is MemberAccessExpressionSyntax member && member.Name.ToString() == "Any")
            //{
            //    var repeat = (MemberAccessExpressionSyntax) member.Expression;
            //    node = (InvocationExpressionSyntax) repeat.Expression;
            //}

            //var nodeString = node.ToString();
            //var when = "When";
            //if (nodeString.Contains("Ignore"))
            //{
            //    when = "WhenForAnyArgs";
            //    nodeString = nodeString.Replace(".IgnoreArguments()", string.Empty);
            //}

            //var s = Regex.Replace(nodeString, "Stub", when, RegexOptions.Singleline);
            //s = Regex.Replace(s, "Expect", when, RegexOptions.Singleline);
            //s = Regex.Replace(s, "\\.Throw\\([\r\n ]*([A-Za-z0-9_ ]*)[\r\n ]*(\\((\".*\")*\\))?",
            //    ".Do(x => { throw $1$2; }", RegexOptions.Singleline);
            //var expressionSyntax = SyntaxFactory.ParseExpression(s);
            //var expressionStatementSyntax = expressionStatement.WithExpression(expressionSyntax);
            //return expressionStatementSyntax;
        }
    }
}
