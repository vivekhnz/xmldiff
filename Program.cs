using System;
using CommandDotNet;

namespace xmldiff
{
    class Program
    {
        static int Main(string[] args)
        {
            var appRunner = new AppRunner<XmlDiffApp>();
            return appRunner.Run(args);
        }
    }
}
