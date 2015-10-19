using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace locr.lib
{
    public class AnalysisResult
    {
        public string Path { get; set; }
        public long ElapsedMilliseconds { get; set; }
        
        public int TotalFileCount { get; set; }
        public int TotalDirectoryCount { get; set; }

        public int IgnoredFileCount { get; set; }
        public int BinaryFileCount { get; set; }

        public long TotalBytes { get; set; }
        public int TotalLines { get; set; }
        public int TotalBlanks { get; set; }

        public AnalysisOptions AnalysisOptions { get; set; }

        public Dictionary<string, AnalysisExtensionSummary> FileResults = new Dictionary<string, AnalysisExtensionSummary>();

        public AnalysisResult()
        {
            this.AnalysisOptions = new AnalysisOptions();
            this.Path = "";
            this.ElapsedMilliseconds = 0L;
            this.TotalFileCount = 0;
            this.TotalDirectoryCount = 0;
            this.IgnoredFileCount = 0;
            this.BinaryFileCount = 0;
        }

        public void Merge(AnalysisResult analyseDirectory)
        {
            this.TotalDirectoryCount++;
            this.TotalDirectoryCount += analyseDirectory.TotalDirectoryCount;
            this.IgnoredFileCount += analyseDirectory.IgnoredFileCount;
            this.TotalFileCount += analyseDirectory.TotalFileCount;
            this.BinaryFileCount += analyseDirectory.BinaryFileCount;
            
            foreach (var fileResult in analyseDirectory.FileResults.Values)
            {
                UpdateStats(fileResult);
            }
        }


        public void Add(AnalysisExtensionSummary extensionSummary)
        {
            if (extensionSummary == null) throw new ArgumentNullException("extensionSummary");

            this.TotalFileCount++;

            if (!extensionSummary.Scanned)
            {
                this.IgnoredFileCount++;
                return;
            }

            UpdateStats(extensionSummary);
        }

        public void UpdateStats(AnalysisExtensionSummary extensionSummary)
        {
            if (extensionSummary == null) throw new ArgumentNullException("extensionSummary");
            
            if (FileResults.ContainsKey(extensionSummary.Extension))
            {
                var result = FileResults[extensionSummary.Extension];
                result.Merge(extensionSummary);
            }
            else
                FileResults[extensionSummary.Extension] = (extensionSummary);

            if (!extensionSummary.IsText) this.BinaryFileCount++;

            this.TotalLines += extensionSummary.Lines;
            this.TotalBytes += extensionSummary.Bytes;
            this.TotalBlanks += extensionSummary.Blanks;
        }

        public static string PadToLength(string value, int totalLength, char padChar = ' ', bool padLeft = false)
        {
            if (totalLength <= 1) return value;
            if (value.Length >= totalLength) return value;

            var padLength = totalLength - value.Length;
            var pad = "";
            for (int i = 0; i < padLength; i++)
                pad += padChar;

            if (padLeft) return pad + value;
            return value + pad;

        }

        public override string ToString()
        {
            const string template = @"locr {version}
---------------------------------------------------------------------------------------------------
{dirdetails}
{perf}
---------------------------------------------------------------------------------------------------
{tableheader}
{table}
---------------------------------------------------------------------------------------------------
{summary}
---------------------------------------------------------------------------------------------------
";

            const int padTotalWidth = 20;

            var message = template;

            message = message.Replace("{version}", Assembly.GetExecutingAssembly().GetName().Version.ToString());

            var dirdetails = "Scanning: " + this.Path;

            message = message.Replace("{dirdetails}",
                string.Format(
                    "Scanning: " + this.Path + "\nDirectories:{0}\nFiles:{1} (Scanned:{2}, Ignored:{3})",
                    this.TotalDirectoryCount,
                    this.TotalFileCount,
                    (this.TotalFileCount - this.IgnoredFileCount),
                    this.IgnoredFileCount));

            message = message.Replace("{tableheader}"
                , PadToLength("Extension", padTotalWidth)
                  + PadToLength("Files", padTotalWidth) 
                  + PadToLength("Lines", padTotalWidth) 
                  + PadToLength("Blanks", padTotalWidth) 
                  + PadToLength("Bytes", padTotalWidth));

            decimal filesPerSecond = (Convert.ToDecimal(TotalFileCount-IgnoredFileCount) / Convert.ToDecimal(ElapsedMilliseconds)) * 1000M;
            filesPerSecond = Math.Floor(filesPerSecond);

            message = message.Replace("{perf}",
                String.Format("{0} files scanned in {1}ms, ({2} files/sec)", TotalFileCount-IgnoredFileCount, ElapsedMilliseconds, filesPerSecond));
            
            var sb = new StringBuilder();
            Dictionary<string, AnalysisExtensionSummary> sortedList = this.AnalysisOptions.Sort(FileResults);
            foreach (var key in sortedList.Keys)
            {
                sb.AppendLine(FileResults[key].ToString());
            }
            var table = "";
            if (sb.Length > 0) table = sb.ToString().Substring(0, sb.Length - 1);
            message = message.Replace("{table}", table);
            
            message = message.Replace("{summary}", String.Format("{0}{1}{2}{3}{4}"
                , PadToLength("Total:", padTotalWidth)
                , PadToLength(TotalFileCount.ToString("N0"), padTotalWidth)
                , PadToLength(TotalLines.ToString("N0"), padTotalWidth)
                , PadToLength(TotalBlanks.ToString("N0"), padTotalWidth)
                , PadToLength(TotalBytes.ToString("N0"), padTotalWidth)));

            return message;
        }
    }
}