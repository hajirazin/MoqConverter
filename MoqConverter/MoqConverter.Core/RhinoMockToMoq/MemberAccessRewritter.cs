using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoqConverter.Core.RhinoMockToMoq.Strategies.MemberAccess;

namespace MoqConverter.Core.RhinoMockToMoq
{
    public partial class Rewritter
    {
        private static readonly List<IMemberAccessStrategy> MemberAccessStrategies = new List<IMemberAccessStrategy>
        {
            new ArgMatches(),
            new ArgAnything(),
            new ArgIsEqual()
        };

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            try
            {
                foreach (var strategy in MemberAccessStrategies)
                {
                    if (strategy.IsEligible(node))
                    {
                        var convertedObject = strategy.Visit(node);
                        if (convertedObject is MemberAccessExpressionSyntax m)
                            return base.VisitMemberAccessExpression(m);
                        return convertedObject;
                    }
                }
            }
            catch
            {
                Logger.Log("Exception in VisitMemberAccessExpression", ConsoleColor.Yellow);
            }

            return base.VisitMemberAccessExpression(node);
        }
    }
}
