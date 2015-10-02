using System;
using System.Diagnostics;

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
                var path = args[0];
                var result = lib.locr.Analyse(path);
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
