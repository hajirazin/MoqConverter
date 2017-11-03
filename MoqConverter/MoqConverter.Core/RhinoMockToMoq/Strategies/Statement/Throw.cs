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
            if (node.Expression is MemberAccessExpressionSyntax member && member.Name.ToString() == "Any")
            {
                var repeat = (MemberAccessExpressionSyntax) member.Expression;
                node = (InvocationExpressionSyntax) repeat.Expression;
            }

            var nodeString = node.ToString();
            var when = "When";
            if (nodeString.Contains("Ignore"))
            {
                when = "WhenForAnyArgs";
                nodeString = nodeString.Replace(".IgnoreArguments()", string.Empty);
            }

            var s = Regex.Replace(nodeString, "Stub", when, RegexOptions.Singleline);
            s = Regex.Replace(s, "Expect", when, RegexOptions.Singleline);
            s = Regex.Replace(s, "\\.Throw\\([\r\n ]*([A-Za-z0-9_ ]*)[\r\n ]*(\\((\".*\")*\\))?",
                ".Do(x => { throw $1$2; }", RegexOptions.Singleline);
            var expressionSyntax = SyntaxFactory.ParseExpression(s);
            var expressionStatementSyntax = expressionStatement.WithExpression(expressionSyntax);
            return expressionStatementSyntax;
        }
    }
}
