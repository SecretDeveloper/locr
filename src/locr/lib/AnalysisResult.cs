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

        private int TotalFileCount { get; set; }
        private int TotalDirectoryCount { get; set; }

        private int IgnoredFileCount { get; set; }
        private int BinaryFileCount { get; set; }

        private long TotalBytes { get; set; }
        private int TotalLines { get; set; }
        private int TotalLinesOfCode { get; set; }

        private AnalysisOptions AnalysisOptions { get; set; }

        private readonly Dictionary<string, AnalysisExtensionSummary> FileResults = new Dictionary<string, AnalysisExtensionSummary>();

        public AnalysisResult()
        {
            AnalysisOptions = new AnalysisOptions();
            Path = "";
            ElapsedMilliseconds = 0L;
            TotalFileCount = 0;
            TotalDirectoryCount = 0;
            IgnoredFileCount = 0;
            BinaryFileCount = 0;
        }

        public void Merge(AnalysisResult analyseDirectory)
        {
            TotalDirectoryCount++;
            TotalDirectoryCount += analyseDirectory.TotalDirectoryCount;
            IgnoredFileCount += analyseDirectory.IgnoredFileCount;
            TotalFileCount += analyseDirectory.TotalFileCount;
            BinaryFileCount += analyseDirectory.BinaryFileCount;
            
            foreach (var fileResult in analyseDirectory.FileResults.Values)
            {
                UpdateStats(fileResult);
            }
        }


        public void Add(AnalysisExtensionSummary extensionSummary)
        {
            if (extensionSummary == null) throw new ArgumentNullException("extensionSummary");

            TotalFileCount++;

            if (!extensionSummary.Scanned)
            {
                IgnoredFileCount++;
                return;
            }

            UpdateStats(extensionSummary);
        }

        private void UpdateStats(AnalysisExtensionSummary extensionSummary)
        {
            if (extensionSummary == null) throw new ArgumentNullException("extensionSummary");
            
            if (FileResults.ContainsKey(extensionSummary.Extension))
            {
                var result = FileResults[extensionSummary.Extension];
                result.Merge(extensionSummary);
            }
            else
                FileResults[extensionSummary.Extension] = (extensionSummary);

            if (!extensionSummary.IsText) BinaryFileCount++;

            TotalLines += extensionSummary.Lines;
            TotalBytes += extensionSummary.Bytes;
            TotalLinesOfCode += extensionSummary.LinesOfCode;
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
----------------------------------------------------------------------------------------------------
{dirdetails}
{perf}
----------------------------------------------------------------------------------------------------
{tableheader}
{table}
----------------------------------------------------------------------------------------------------
{summary}
----------------------------------------------------------------------------------------------------
";

            const int padTotalWidth = 20;

            var message = template;

            message = message.Replace("{version}", Assembly.GetExecutingAssembly().GetName().Version.ToString());

            message = message.Replace("{dirdetails}",
                string.Format(
                    "Scanning: " + Path + "\nDirectories:{0}\nFiles:{1} (Scanned:{2}, Ignored:{3})",
                    TotalDirectoryCount,
                    TotalFileCount,
                    (TotalFileCount - IgnoredFileCount),
                    IgnoredFileCount));

            message = message.Replace("{tableheader}"
                , PadToLength("Extension", padTotalWidth)
                  + PadToLength("Files", padTotalWidth) 
                  + PadToLength("Lines", padTotalWidth) 
                  + PadToLength("LOC", padTotalWidth)
                  + PadToLength("Bytes", padTotalWidth));

            var filesPerSecond = (Convert.ToDecimal(TotalFileCount-IgnoredFileCount) / Convert.ToDecimal(ElapsedMilliseconds)) * 1000M;
            filesPerSecond = Math.Floor(filesPerSecond);

            message = message.Replace("{perf}",
                string.Format("{0} files scanned in {1}ms, ({2} files/sec)", TotalFileCount-IgnoredFileCount, ElapsedMilliseconds, filesPerSecond));
            
            var sb = new StringBuilder();
            var sortedList = AnalysisOptions.Sort(FileResults);
            foreach (var key in sortedList.Keys)
            {
                sb.AppendLine(FileResults[key].ToString());
            }
            var table = "";
            if (sb.Length > 0) table = sb.ToString().Substring(0, sb.Length - 1);
            message = message.Replace("{table}", table);

            message = message.Replace("{summary}", string.Format("{0}{1}{2}{3}{4}"
                , PadToLength("Total:", padTotalWidth)
                , PadToLength(TotalFileCount.ToString("N0"), padTotalWidth)
                , PadToLength(TotalLines.ToString("N0"), padTotalWidth)
                , PadToLength(TotalLinesOfCode.ToString("N0"), padTotalWidth)
                , PadToLength(TotalBytes.ToString("N0"), padTotalWidth)));

            return message;
        }
    }
}