using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.Statement
{
    public interface IStatementStrategy
    {
        bool IsEligible(ExpressionStatementSyntax expressionStatement);
        ExpressionStatementSyntax Visit(ExpressionStatementSyntax expressionStatement);
    }
}
