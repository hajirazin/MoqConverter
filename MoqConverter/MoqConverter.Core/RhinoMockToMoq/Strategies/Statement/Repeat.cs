using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class Repeat : Return, IStatementStrategy
    {
        public override bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            if (!(expressionStatement.Expression is InvocationExpressionSyntax node) ||
                !(node.Expression is MemberAccessExpressionSyntax memberAccessExpression) ||
                !(memberAccessExpression.Expression is MemberAccessExpressionSyntax repeatExpression))
                return false;

            return repeatExpression.Name.ToString().Equals("Repeat");
        }

        public override ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            var memberAccessExpression =
                (MemberAccessExpressionSyntax)((InvocationExpressionSyntax)expressionStatement.Expression).Expression;
            var name = memberAccessExpression.Name.ToString();
            var node = (InvocationExpressionSyntax)((MemberAccessExpressionSyntax)memberAccessExpression.Expression).Expression;

            var memberOuter = (MemberAccessExpressionSyntax)node.Expression;
            var nodeInner = (InvocationExpressionSyntax)memberOuter.Expression;
            var member = (MemberAccessExpressionSyntax)nodeInner.Expression;
            ExpressionSyntax expression;
            if (member.Expression is MemberAccessExpressionSyntax property)
            {
                expression = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("Mock"),
                        SyntaxFactory.IdentifierName("Get")),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                        {SyntaxFactory.Argument(property)})));
            }
            else
            {
                var identifier = (IdentifierNameSyntax)member.Expression;
                var mockGet = SyntaxFactory.IdentifierName(identifier + "Mock");
                expression = mockGet;

            }

            var setup = name.Equals("Any") ? "Setup" : "SetupSequence";
            member = member.WithExpression(expression).WithName(SyntaxFactory.IdentifierName(setup));
            nodeInner = nodeInner.WithExpression(member);
            memberOuter = memberOuter.WithExpression(nodeInner).WithName(SyntaxFactory.IdentifierName("Returns"));
            node = node.WithExpression(memberOuter);
            if (name.Equals("Twice"))
            {
                node = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, node, memberOuter.Name),
                    node.ArgumentList);
            }

            if (node.ArgumentList?.Arguments == null || node.ArgumentList.Arguments.Count <= 0)
                return expressionStatement.WithExpression(node);

            var argumentList = new SeparatedSyntaxList<ArgumentSyntax>();
            foreach (var argument in node.ArgumentList.Arguments)
            {
                if (argument.Expression is LiteralExpressionSyntax literal &&
                    literal.Kind() == SyntaxKind.NullLiteralExpression)
                {
                    argumentList = argumentList.Add(
                        argument.WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("value"))));
                }
                else
                {
                    argumentList = argumentList.Add(argument);
                }
            }

            node = node.WithArgumentList(node.ArgumentList.WithArguments(argumentList));
            expressionStatement = expressionStatement.WithExpression(node);
            return expressionStatement;
            //var nodeString = node.ToString();
            //var appendString = string.Empty;
            //if (nodeString.Contains("IgnoreArguments"))
            //{
            //    appendString = "WithAnyArgs";
            //}

            //var recieved = (name.Equals("Never") ? "DidNotReceive" : "Verifiable") + appendString;
            //int? number = null;
            //switch (name)
            //{
            //    case "Twice":
            //        number = 2;
            //        break;
            //    case "Once":
            //        number = 1;
            //        break;
            //    case "Times":
            //        var argument = node.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
            //        number = int.Parse(argument.Token.ValueText);
            //        break;
            //}


            //var s = n.ToString();
            //s +=  $".{recieved}({number}).";

            //return expressionStatement.WithExpression(SyntaxFactory.ParseExpression(s));
            //new Repeat("Any", ""),
            //new Repeat("Twice", ".Received(2)"),
            //new Repeat("AtLeastOnce", ".Received(1)"),
            //new Repeat("Once", ".Received(1)"),
            //new Repeat("Never", ".DidNotReceive()"),
            //_repeatRegex = new Regex($"\\.[\n\r ]*Repeat[\n\r ]*\\.[\n\r ]*{repeatKeyword}[\n\r ]*\\(\\)");
        }
    }
}
