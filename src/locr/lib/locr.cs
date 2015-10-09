using System;
using System.Diagnostics;
using System.IO;
using CliParse;

namespace locr.lib
{
    /// <summary>
    /// public API for locr functionality.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class locr
    {

        public event EventHandler OnStatusUpdate;

        public string Analyse(AnalysisOptions options)
        {
            var sw = new Stopwatch();
            sw.Start();
            
            var analysisResult = new AnalysisResult();
            analysisResult.Path = options.Path;

            if (File.Exists(analysisResult.Path))
            {
                analysisResult.Add(AnalyseFile(analysisResult.Path, options));
            }
            else
            {
                analysisResult.Merge(AnalyseDirectory(analysisResult.Path, options));
            }
            sw.Stop();
            analysisResult.ElapsedMilliseconds = sw.ElapsedMilliseconds;

            return analysisResult.ToString();
        }
        
        private AnalysisResult AnalyseDirectory(string path, AnalysisOptions options)
        {
            var analysisResult = new AnalysisResult();
            UpdateStatus("Analysing directory:", path);
            
            analysisResult.Path = path;
            foreach (var item in Directory.EnumerateFiles(path))
            {
                analysisResult.TotalFileCount++;
                var analysis = AnalyseFile(item, options);
                if (!analysis.Scanned)
                {
                    analysisResult.IgnoredFileCount++;
                    continue;
                }
                analysisResult.Add(analysis);
            }

            foreach (var item in Directory.EnumerateDirectories(path))
            {
                if (!options.ShouldScanDirectory(item))
                {
                    UpdateStatus("Skipping directory:", item);
                    continue;
                }
                var directoryAnalysis = AnalyseDirectory(item, options);
                analysisResult.Merge(directoryAnalysis);
            }

            return analysisResult;
        }

        private AnalysisFileResult AnalyseFile(string filePath, AnalysisOptions options)
        {
            var result = new AnalysisFileResult();
            if (!options.ShouldScanFile(filePath))
            {
                UpdateStatus("Skipping file:", filePath);
                result.Scanned = false; // marked as skipped file
                return result;
            }

            UpdateStatus("Analysing file:", filePath);
            
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

            return result;
        }

        private void UpdateStatus(string prefix, string message)
        {
            if (OnStatusUpdate == null) return;

            OnStatusUpdate(this, new AnalysisEventArgs(){Message = message, Prefix = prefix});
        }
    }
}
