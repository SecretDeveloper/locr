using System;

namespace locr
{
    public class AnalysisEventArgs:EventArgs
    {
        public string Prefix { get; set; }
        public string Message { get; set; }

    }
}