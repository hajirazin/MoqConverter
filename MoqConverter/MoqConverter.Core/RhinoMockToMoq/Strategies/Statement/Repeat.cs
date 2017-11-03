using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class Repeat : IStatementStrategy
    {
        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return false;

            if (!(node.Expression is MemberAccessExpressionSyntax memberAccessExpression))
                return false;

            if (!(memberAccessExpression.Expression is MemberAccessExpressionSyntax repearAccessExpression))
                return false;

            return repearAccessExpression.Name.ToString().Equals("Repeat");
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node))
                return expressionStatement;

            if (!(node.Expression is MemberAccessExpressionSyntax memberAccessExpression))
                return expressionStatement;

            var n = (InvocationExpressionSyntax)((MemberAccessExpressionSyntax)memberAccessExpression.Expression)
                .Expression;
            var name = memberAccessExpression.Name.ToString();
            //if (name.Equals("Any"))
            {
                return expressionStatement.WithExpression(n);
            }

            var nodeString = node.ToString();
            var appendString = string.Empty;
            if (nodeString.Contains("IgnoreArguments"))
            {
                appendString = "WithAnyArgs";
            }

            var recieved = (name.Equals("Never") ? "DidNotReceive" : "Verifiable") + appendString;
            int? number = null;
            switch (name)
            {
                case "Twice":
                    number = 2;
                    break;
                case "Once":
                    number = 1;
                    break;
                case "Times":
                    var argument = node.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
                    number = int.Parse(argument.Token.ValueText);
                    break;
            }


            var s = n.ToString();
            s +=  $".{recieved}({number}).";

            return expressionStatement.WithExpression(SyntaxFactory.ParseExpression(s));
            //new Repeat("Any", ""),
            //new Repeat("Twice", ".Received(2)"),
            //new Repeat("AtLeastOnce", ".Received(1)"),
            //new Repeat("Once", ".Received(1)"),
            //new Repeat("Never", ".DidNotReceive()"),
            //_repeatRegex = new Regex($"\\.[\n\r ]*Repeat[\n\r ]*\\.[\n\r ]*{repeatKeyword}[\n\r ]*\\(\\)");
        }
    }
}
