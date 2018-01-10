using System;
using MoqConverter.Core;
using MoqConverter.Core.Converters;
using MoqConverter.Core.RhinoMockToMoq;

namespace MoqConverter.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Init();
            //var converter = new SolutionConverter();
            //converter.Convert(@"C:\TTL\web\source\WebComponents\");
            //converter.Convert(@"C:\TTL\web\source\WebComponents\DeliveryOptions\test\UnitTests");

            //File convert
            FileRewritter fileRewritter = new Rewritter();
            var converter = new FileConverter(fileRewritter);
            converter.Convert(@"C:\TTL\web\source\WebComponents\JourneyPlanning\test\UnitTests\Command");

            Logger.Log("End ...");
            System.Console.ReadLine();
        }
    }
}
