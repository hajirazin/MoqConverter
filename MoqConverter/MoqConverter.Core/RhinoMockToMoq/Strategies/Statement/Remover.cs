using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public class Remover : IStatementStrategy
    {
        private static readonly List<string> StatementsToRemove = new List<string>
        {
            ".VerifyAllExpectations",
            ".VerifyAll()",
            ".Replay()",
            ".ReplayAll()",
            "MockRepository()"
        };

        public bool IsEligible(ExpressionStatementSyntax expressionStatement)
        {
            var nodeString = expressionStatement.ToString();
            return StatementsToRemove.Any(s => nodeString.Contains(s));
        }

        public ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement)
        {
            return null;
        }
    }
}
