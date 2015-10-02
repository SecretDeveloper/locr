using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace locr.lib
{
    /// <summary>
    /// public API for locr functionality.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class locr
    {
        private const string _template = @"locr {version}
---------------------------------------------------------------------------------------------------
{perf}
---------------------------------------------------------------------------------------------------
{tableheader}
{table}
---------------------------------------------------------------------------------------------------
{summary}
---------------------------------------------------------------------------------------------------
";

        public static string Analyse(string path, AnalysisOptions options = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            List<AnalysisResult> list = null;

            if (File.Exists(path))
            {
                list = AnalyseFile(path, options);
            }
            else
            {
                list = AnalyseDirectory(path, options);
            }
            sw.Stop();

            return ListResultsToString(list, options, sw);
        }
        
        private static List<AnalysisResult> AnalyseDirectory(string path, AnalysisOptions options)
        {
            var list = new List<AnalysisResult>();
            foreach (var item in Directory.EnumerateFiles(path))
            {
                list.AddRange(AnalyseFile(item, options));
            }

            foreach (var item in Directory.EnumerateDirectories(path))
            {
                list.AddRange(AnalyseDirectory(item, options));
            }

            return list;
        }

        private static List<AnalysisResult> AnalyseFile(string filePath, AnalysisOptions options)
        {
            var result = new AnalysisResult();
            result.Path = filePath;
            result.Extension = Path.GetExtension(filePath) ?? "";
            result.Extension = result.Extension.ToLowerInvariant();
            if (result.Extension.Length == 0) result.Extension = "(blank)";
            
            var carriageReturn = (int)Convert.ToByte('\r'); // 13
            var lineFeed= (int)Convert.ToByte('\n'); // 10

            var lastByte = -1;
            var lastlastByte = -1;
            
            using (var stream = File.OpenRead(filePath))
            {
                int current = stream.ReadByte();
                
                while(current != -1)
                {
                    // mac
                    if (lastByte == carriageReturn && current== carriageReturn) result.Blanks++;
                    // unix
                    if (lastByte == lineFeed && current == lineFeed) result.Blanks++;
                    // windows
                    if (lastlastByte == carriageReturn && lastByte == lineFeed && current == carriageReturn) result.Blanks++;
                    
                    if ((lastByte!=carriageReturn && current == lineFeed) || current == carriageReturn)
                    {
                        result.Lines++;
                    }
                    
                    lastlastByte = lastByte;
                    lastByte = current;
                    result.Bytes ++;
                    current = stream.ReadByte();
                }
            }
            
            // cleanup
            if (!(lastByte == lineFeed || lastByte == carriageReturn) && result.Lines > 0) result.Lines++;

            return new List<AnalysisResult>(){result};
        }
        
        private static string ListResultsToString(List<AnalysisResult> list, AnalysisOptions options, Stopwatch sw)
        {
            var padTotalWidth = 25;
            SortedDictionary<string, AnalysisResult> dict = MergeResults(list, options);

            var message = _template;

            message = message.Replace("{version}", Assembly.GetExecutingAssembly().GetName().Version.ToString());

            message = message.Replace("{tableheader}", PadToLength("Extension", padTotalWidth) + PadToLength("Lines", padTotalWidth) + PadToLength("Blanks", padTotalWidth) + PadToLength("Bytes", padTotalWidth));

            decimal filesPerSecond = (Convert.ToDecimal(list.Count) / Convert.ToDecimal(sw.ElapsedMilliseconds)) * 1000M;
            filesPerSecond = Math.Floor(filesPerSecond);

            message = message.Replace("{perf}",
                string.Format("{0} files scanned in {1}ms, ({2} files/sec)", list.Count, sw.ElapsedMilliseconds, filesPerSecond));

            var lines = 0;
            var blanks = 0;
            var bytes = 0L;

            var sb = new StringBuilder();
            foreach (var analysisResult in dict.Values)
            {
                lines += analysisResult.Lines;
                blanks += analysisResult.Blanks;
                bytes += Convert.ToInt64(analysisResult.Bytes);
                sb.AppendLine(analysisResult.ToString());
            }
            message = message.Replace("{table}", sb.ToString());

            message = message.Replace("{summary}", string.Format("{0}{1}{2}{3}", PadToLength("Total:", padTotalWidth), PadToLength(lines.ToString(), padTotalWidth), PadToLength(blanks.ToString(), padTotalWidth), PadToLength(bytes.ToString(), padTotalWidth)));

            return message;
        }
        
        private static SortedDictionary<string, AnalysisResult> MergeResults(IEnumerable<AnalysisResult> list, AnalysisOptions options)
        {
            var dict = new SortedDictionary<string, AnalysisResult>();

            foreach (var analysisResult in list)
            {
                if (!dict.ContainsKey(analysisResult.Extension))
                {
                    dict[analysisResult.Extension] = analysisResult;
                    continue;
                }
                dict[analysisResult.Extension].Merge(analysisResult);
            }
            return dict;
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
    }

    internal class AnalysisResult
    {
        public string Path { get; set; }
        public string Extension { get; set; }
        public long Bytes { get; set; }
        public int Lines { get; set; }
        public int Blanks { get; set; }
        public bool IsText { get; set; }

        public AnalysisResult()
        {
            Path = "";
            Extension = "";
            Lines = 0;
            Blanks = 0;
            IsText = true;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}{2}{3}", locr.PadToLength(this.Extension, 25), locr.PadToLength(this.Lines.ToString(), 25), locr.PadToLength(this.Blanks.ToString(), 25), locr.PadToLength(this.Bytes.ToString(), 25));
        }

        public void Merge(AnalysisResult right)
        {
            this.Blanks += right.Blanks;
            this.Lines += right.Lines;
        }
    }

    public class AnalysisOptions
    {
    }
}
