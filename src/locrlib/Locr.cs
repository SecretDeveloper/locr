using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace locrlib
{
    /// <summary>
    /// public API for locr functionality.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class locr
    {
        private const string _template = @"locr
{dirsummary}
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
                    //if (result.IsText && current < 32) result.IsText = false; // check for binary files.
                    if (current == lineFeed || current == carriageReturn)
                    {
                        if ((lastlastByte==-1 || lastlastByte == carriageReturn) && (lastByte == -1 || lastByte == lineFeed))
                        {
                            result.Blanks++;
                        }

                        result.Lines++;

                        if (current == lineFeed && lastByte == carriageReturn)
                        {
                            result.Lines--; // windows uses 2 characters for newlines (CRLF), we already counted the CR so decrement now.
                        }
                    }
                    
                    lastlastByte = lastByte;
                    lastByte = current;
                    current = stream.ReadByte();
                }
            }
            
            // cleanup
            if (!(lastByte == lineFeed || lastByte == carriageReturn) && result.Lines > 0) result.Lines++;

            return new List<AnalysisResult>(){result};
        }
        
        private static string ListResultsToString(List<AnalysisResult> list, AnalysisOptions options, Stopwatch sw)
        {
            SortedDictionary<string, AnalysisResult> dict = MergeResults(list, options);

            var message = _template;

            if (list.Count > 1) message = message.Replace("{dirsummary}", "");
            message = message.Replace("{dirsummary}", "");

            message = message.Replace("{tableheader}", PadToLength("Extension", 30)+PadToLength("Lines", 30)+PadToLength("Blanks", 30));

            decimal filesPerSecond = (Convert.ToDecimal(list.Count) / Convert.ToDecimal(sw.ElapsedMilliseconds)) * 1000M;
            filesPerSecond = Math.Floor(filesPerSecond);

            message = message.Replace("{perf}",
                string.Format("{0} files scanned in {1}ms, ({2} files/sec)", list.Count.ToString(), sw.ElapsedMilliseconds, filesPerSecond));

            var lines = 0;
            var blanks = 0;

            var sb = new StringBuilder();
            foreach (var analysisResult in dict.Values)
            {
                lines += analysisResult.Lines;
                blanks += analysisResult.Blanks;
                sb.AppendLine(analysisResult.ToString());
            }
            message = message.Replace("{table}", sb.ToString());

            message = message.Replace("{summary}", string.Format("{0}{1}{2}", PadToLength("Summary:", 30),PadToLength(lines.ToString(), 30), PadToLength(blanks.ToString(), 30)));

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
            return string.Format("{0}{1}{2}", locr.PadToLength(this.Extension,30), locr.PadToLength(this.Lines.ToString(), 30), locr.PadToLength(this.Blanks.ToString(), 30), this.IsText);
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
