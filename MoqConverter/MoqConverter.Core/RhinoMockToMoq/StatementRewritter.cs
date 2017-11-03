using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoqConverter.Core.RhinoMockToMoq.Strategies.Statement;

namespace MoqConverter.Core.RhinoMockToMoq
{
    public partial class Rewritter
    {
        private static readonly List<IStatementStrategy> StatementStrategies = new List<IStatementStrategy>
        {
            //new Remover(),
            new VerifyAllExpectations(),
            //new Throw(),
            //new WhenCalled(),
            //new PropertyBehavior(),
            new Assert("AssertWasCalled", "Verify"),
            //new Assert("AssertWasNotCalled", "DidNotReceive"),
            new Repeat(),
            new ReturnWithIgnore(),
            new Return(),
            new MockRepository(),
            //new ExpectWithoutReturn(),
            //new Ignore()
        };

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            try
            {
                foreach (var strategy in StatementStrategies)
                {
                    try
                    {
                        if (strategy.IsEligible(node))
                        {
                            var convertedObject = strategy.Visit(node);
                            if (convertedObject == null)
                                return null;

                            node = convertedObject;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Exception in VisitInvocationExpression", ConsoleColor.Yellow);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Exception in VisitInvocationExpression", ConsoleColor.Yellow);
            }

            return base.VisitExpressionStatement(node);
        }
    }
}
