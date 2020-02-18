using McMaster.Extensions.CommandLineUtils;
using System;
using System.Diagnostics;

namespace Doser
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                return CommandLineApplication.Execute<RunnerArgs>(args);
            }
            finally
            {
                if (Debugger.IsAttached)
                {
                    Console.WriteLine("");
                    Console.WriteLine("-----------------------------------------------------");
                    Console.WriteLine("Press Enter To Close Debugger");
                    Console.ReadLine();
                }
            }
        }
    }
}
