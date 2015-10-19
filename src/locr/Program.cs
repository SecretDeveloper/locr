using System;
using System.Diagnostics;
using System.Reflection;
using CliParse;
using locr.lib;

namespace locr
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
#if DEBUG
                Debugger.Launch();
#endif
                Execute(args);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void Execute(string[] args)
        {
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

            var analysis = new lib.locr();

            if (options.Verbosity > 0)
                analysis.OnStatusUpdate += analysis_OnStatusUpdate;

            var screen = analysis.Analyse(options);

            // reset screen update line
            Console.Write("\r                                                                                                       ");
            Console.Write("\r");

            Console.WriteLine(screen);
        }

        static void analysis_OnStatusUpdate(object sender, EventArgs e)
        {
            var args = e as AnalysisEventArgs; 
            if (args == null) return;
            
            const int maxLength = 100;
            var message = GetMessage(args.Prefix, args.Message, maxLength);

            var verbosity = args.Options.Verbosity;

            if(verbosity>1)
                Console.WriteLine(message);
            else if(verbosity>0)
                Console.Write("\r"+message);
                
        }

        private static string GetMessage(string prefix, string message, int length)
        {
            var p = prefix ?? "";
            var allowableMessageLenth = length - p.Length;

            var m = message ?? "";

            if (m.Length > allowableMessageLenth)
                m = m.Substring(m.Length - allowableMessageLenth);

            m = (p + m).PadRight(length);

            return m;
        }
    }
}
