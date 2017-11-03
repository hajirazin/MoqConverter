using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.Helpers
{
    public static class Mollifier
    {
        private static readonly ConcurrentDictionary<string, int> VariableCount = new ConcurrentDictionary<string, int>();

        public static ExpressionSyntax MakeCompilerHappy(MemberAccessExpressionSyntax member, string methodName)
        {
            var str = member.ToString();
            VariableCount.GetOrAdd(methodName, -1);
            VariableCount[methodName]++;
            var count = VariableCount[methodName];
            str = $"var variableToMakeCompilerHappy{(count == 0 ? "" : count.ToString())} = " + str;
            var statement = SyntaxFactory.ParseExpression(str);
            return statement;
        }

        public static ExpressionSyntax MakeCompilerHappy(MemberAccessExpressionSyntax member)
        {
            return MakeCompilerHappy(member, GetMethodName(member));
        }

        private static string GetMethodName(SyntaxNode member)
        {
            var nn = member.Parent;
            while (nn != null)
            {
                if (nn is MethodDeclarationSyntax method)
                    return method.Identifier.ValueText;

                nn = nn.Parent;
            }

            return string.Empty;
        }
    }
}
