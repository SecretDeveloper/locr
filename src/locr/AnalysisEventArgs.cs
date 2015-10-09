using System;
using locr.lib;

namespace locr
{
    public class AnalysisEventArgs:EventArgs
    {
        public string Prefix { get; set; }
        public string Message { get; set; }
        public AnalysisOptions Options { get; set; }

    }
}