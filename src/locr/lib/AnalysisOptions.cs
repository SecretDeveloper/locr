﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace locr.lib
{
    [CliParse.ParsableClass("locr", "Utility for counting the number of lines conained in a file or directory of files.")]
    public class AnalysisOptions:CliParse.Parsable
    {
        [CliParse.ParsableArgument('p', "path", Required = true)]
        public string Path { get; set; }

        [CliParse.ParsableArgument('m', "match", DefaultValue = "", Description = "Only files matched by the supplied regular expression will be scanned")]
        public string FileMatch { get; set; }

        private string fileInclude;
        [CliParse.ParsableArgument('i', "include", DefaultValue = "", Description = "Only files include files that pass wildcard search e.g. *.cs")]
        public string FileInclude
        {
            get { return fileInclude; }
            set { fileInclude = WildcardToRegex(value); }
        }

        [CliParse.ParsableArgument('d', "matchdir", Description = "Only directories matched by the supplied regular expression will be scanned")]
        public string DirectoryMatch { get; set; }

        [CliParse.ParsableArgument('q', "quiet", DefaultValue = false, Description = "Less chatty output.")]
        public bool Quiet { get; set; }


        public Dictionary<string, AnalysisFileResult> Sort(Dictionary<string, AnalysisFileResult> sortable)
        {
            return sortable.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        public static string WildcardToRegex(string pattern)
        {
            return "^" + pattern.Replace("*",".*") + "$";
        }

        public bool ShouldScanDirectory(string directoryPath)
        {
            if (string.IsNullOrEmpty(DirectoryMatch)) return true;

            var directoryName = System.IO.Path.GetDirectoryName(directoryPath);
            if (string.IsNullOrEmpty(directoryName)) return true;

            return Regex.IsMatch(directoryName, DirectoryMatch);
        }

        public bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(FileMatch) && string.IsNullOrEmpty(FileInclude)) return true;

            var filename = System.IO.Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(filename)) return true;

            if (!string.IsNullOrEmpty(FileMatch) && Regex.IsMatch(filename, FileMatch))
                return true;

            if (!string.IsNullOrEmpty(FileInclude) && Regex.IsMatch(filename, FileInclude))
                return true;

            return false;
        }
    }
}