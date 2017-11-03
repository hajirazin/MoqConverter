using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoqConverter.Core.RhinoMockToMoq.Strategies.MemberAccess
{
    public interface IMemberAccessStrategy
    {
        bool IsEligible(MemberAccessExpressionSyntax node);
        SyntaxNode Visit(MemberAccessExpressionSyntax node);
    }
}
