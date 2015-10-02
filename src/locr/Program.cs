using System;
using System.Diagnostics;
using System.Reflection;
using CliParse;
using locr.lib;

namespace locr
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
#if DEBUG
                Debugger.Launch();
#endif
                var options = new AnalysisOptions();
                var result = options.CliParse(args);
                if (result.Successful == false || result.ShowHelp)
                {
                    foreach (var message in result.CliParseMessages)
                    {
                        Console.WriteLine(message);
                    }
                    
                    
                    Console.WriteLine(options.GetHelpInfo());
                    return;
                }

                var analyis = lib.locr.Analyse(options);
                Console.WriteLine(analyis);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
