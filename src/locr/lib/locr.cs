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

            var analysisResult = new AnalysisResult {Path = options.Path};

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
            UpdateStatus("Analysing directory:", path, options);
            
            analysisResult.Path = path;
            foreach (var item in Directory.EnumerateFiles(path))
            {
                var analysis = AnalyseFile(item, options);
                analysisResult.Add(analysis);
            }

            if (!options.Recurse) return analysisResult;

            foreach (var item in Directory.EnumerateDirectories(path))
            {
                if (!options.ShouldScanDirectory(item))
                {
                    UpdateStatus("Skipping directory:", item, options);
                    continue;
                }
                var directoryAnalysis = AnalyseDirectory(item, options);
                analysisResult.Merge(directoryAnalysis);
            }

            return analysisResult;
        }

        private AnalysisExtensionSummary AnalyseFile(string filePath, AnalysisOptions options)
        {
            AnalysisExtensionSummary result = new AnalysisExtensionSummary();
            try
            {
                if (!options.ShouldScanFile(filePath))
                {
                    UpdateStatus("Skipping file:", filePath, options);
                    result.Scanned = false; // marked as skipped file
                    return result;
                }

                UpdateStatus("Analysing file:", filePath, options);
                var str = File.ReadAllText(filePath);

                // Scan file stream
                result = AnalyseString(str);
                result.Extension = Path.GetExtension(filePath) ?? "";
                result.Extension = result.Extension.ToLowerInvariant();
                if (result.Extension.Length == 0) result.Extension = "(blank)";

            }
            catch (Exception ex)
            {
                UpdateStatus("Skipping file - Error reading:", filePath, options);
                result.Scanned = false; // marked as skipped file
            }

            return result;
        }

        public AnalysisExtensionSummary AnalyseString(string str, string singleLineComment = "//", string multiLineStart="/*", string multiLineEnd="*/")
        {
            //SUPER SIMPLISTIC ATTEMPT AT COUNTING LINES, LOC, COMMENTS AND BLANKS.

            var result = new AnalysisExtensionSummary();
            
            var CARRAIGE_RETURN = '\r'; // 13
            var LINE_FEED = '\n'; // 10
            var FORWARD_SLASH = '/';
            var ASTERIX = '*';
            var TAB = '\t';
            var SPACE = ' ';

            var inSingleComment = false;
            var inMultiComment = false;
            var isCommentLine = true;
            var isBlankLine = true;
            var isTextFile = true;
            var consecutiveNullChars = 0;

            var lastByte = -1;
            var lastlastByte = -1;

            int index = 0;
            char current = str[index];

            while (index < str.Length -1)
            {
                //binary files
                if (current == '\0')
                {
                    consecutiveNullChars++;
                    if (consecutiveNullChars >= 4)
                        isTextFile = false;
                }
                else
                    consecutiveNullChars = 0;

                if (isTextFile)
                {
                    //Comments
                    if (lastByte == FORWARD_SLASH && (!inMultiComment && !inSingleComment))
                    {
                        inSingleComment = current == FORWARD_SLASH;
                        inMultiComment = current == ASTERIX;
                    }
                    // Closing comments
                    if (lastByte == ASTERIX && inMultiComment && current == FORWARD_SLASH)
                        inMultiComment = false;

                    // Any non whitespace char on a line not contained in a comment or starting with a comment
                    // means the line is not counted as a comment line.
                    if (current != TAB
                        && current != SPACE
                        && current != FORWARD_SLASH
                        && current != CARRAIGE_RETURN
                        && current != LINE_FEED
                    )
                    {
                        isBlankLine = false; // some non-whitespace char on this line so it is not blank.
                        if (!inMultiComment && !inSingleComment) isCommentLine = false;
                    }

                    // End of Line - counted after we pass the line break.
                    if ((current == LINE_FEED) || (lastByte == CARRAIGE_RETURN && current == CARRAIGE_RETURN))
                    {
                        result.Lines++;
                        //Comment reset
                        if (!isCommentLine && !isBlankLine) result.LinesOfCode++;
                        isCommentLine = true;
                        isBlankLine = true;
                        inSingleComment = false;
                    }
                }

                lastByte = current;
                result.Bytes++;
                current = str[++index];
            }
            
            if (lastByte == CARRAIGE_RETURN || lastByte == LINE_FEED)
                result.Lines+=2;

            // binary cleanup
            if (!isTextFile)
            {
                result.LinesOfCode = 0;
                result.Lines = 0;
                result.IsText = false;
            }

            return result;
        }

        private void UpdateStatus(string prefix, string message, AnalysisOptions options)
        {
            if (OnStatusUpdate == null) return;

            OnStatusUpdate(this, new AnalysisEventArgs(){Message = message, Prefix = prefix, Options = options});
        }
    }
}
