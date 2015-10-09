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

        public Dictionary<string, AnalysisFileResult> FileResults = new Dictionary<string, AnalysisFileResult>();

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

        public void Add(AnalysisFileResult fileResult)
        {
            if(fileResult == null) throw new ArgumentNullException("fileResult");
            if (fileResult.Scanned)
            {
                if (FileResults.ContainsKey(fileResult.Extension))
                {
                    var result = FileResults[fileResult.Extension];
                    result.Merge(fileResult);
                    result.FileCount++; // increment number of files for this extension.
                }
                else
                    FileResults[fileResult.Extension] = (fileResult);

                if (!fileResult.IsText) this.BinaryFileCount++;

                this.TotalLines += fileResult.Lines;
                this.TotalBytes += fileResult.Bytes;
                this.TotalBlanks += fileResult.Blanks;
            }
            else
            {
                this.IgnoredFileCount++;
            }
        }

        public void Merge(AnalysisResult analyseDirectory)
        {
            this.TotalDirectoryCount++;
            this.TotalDirectoryCount += analyseDirectory.TotalDirectoryCount;
            this.TotalFileCount += analyseDirectory.TotalFileCount;
            foreach (var fileResult in analyseDirectory.FileResults.Values)
            {
                Add(fileResult);
            }
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

            message = message.Replace("{dirdetails}", string.Format("Scanning: " + this.Path + "\nDirectories:{1}\nScanned Files:{0}\nIgnoredFiles:{2}", this.TotalFileCount, this.TotalDirectoryCount, this.IgnoredFileCount));

            message = message.Replace("{tableheader}"
                , PadToLength("Extension", padTotalWidth)
                  + PadToLength("Files", padTotalWidth) 
                  + PadToLength("Lines", padTotalWidth) 
                  + PadToLength("Blanks", padTotalWidth) 
                  + PadToLength("Bytes", padTotalWidth));

            decimal filesPerSecond = (Convert.ToDecimal(TotalFileCount) / Convert.ToDecimal(ElapsedMilliseconds)) * 1000M;
            filesPerSecond = Math.Floor(filesPerSecond);

            message = message.Replace("{perf}",
                String.Format("{0} files scanned in {1}ms, ({2} files/sec)", TotalFileCount, ElapsedMilliseconds, filesPerSecond));
            
            var sb = new StringBuilder();
            Dictionary<string, AnalysisFileResult> sortedList = this.AnalysisOptions.Sort(FileResults);
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