namespace locr.lib
{
    /// <summary>
    /// Single file analysis results.
    /// </summary>
    public class AnalysisExtensionSummary
    {
        public string Extension { get; set; }
        public long Bytes { get; set; }
        public int Lines { get; set; }
        public int Blanks { get; set; }
        public bool IsText { get; set; }
        public bool Scanned { get; set; }

        public int FileCount { get; set; }

        public AnalysisExtensionSummary()
        {
            Extension = "";
            Lines = 0;
            Blanks = 0;
            IsText = true;
            Scanned = true;

            FileCount = 1;
        }

        public override string ToString()
        {
            var padding = 20;
            return string.Format("{0}{1}{2}{3}{4}"
                , AnalysisResult.PadToLength(this.Extension, 20)
                , AnalysisResult.PadToLength(this.FileCount.ToString("N0"), padding)
                , AnalysisResult.PadToLength(this.Lines.ToString("N0"), padding) 
                , AnalysisResult.PadToLength(this.Blanks.ToString("N0"), padding)
                , AnalysisResult.PadToLength(this.Bytes.ToString("N0"), padding));
        }

        public void Merge(AnalysisExtensionSummary right)
        {
            if (right.Scanned)
            {
                this.FileCount += right.FileCount;
                this.Bytes += right.Bytes;
                this.Lines += right.Lines;
                this.Blanks += right.Blanks;
            }
        }
    }
}