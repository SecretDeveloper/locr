using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                var result = locrlib.locr.Analyse(path);
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
