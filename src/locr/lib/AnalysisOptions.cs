using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CliParse;

namespace locr.lib
{
    [CliParse.ParsableClass("locr", "Utility for counting the number of lines of text in files and folders.")]
    public class AnalysisOptions:CliParse.Parsable
    {
        [CliParse.ParsableArgument("path", ShortName = 'p', ImpliedPosition = 1)]
        public string Path { get; set; }

        [CliParse.ParsableArgument("match", ShortName = 'm', DefaultValue = "", Description = "Only files matched by the supplied regular expression will be scanned")]
        public string FileMatch { get; set; }

        private string _fileInclude;
        [CliParse.ParsableArgument("include", ShortName = 'i', DefaultValue = "", Description = "Only files include files that pass wildcard search e.g. *.cs")]
        public string FileInclude
        {
            get { return _fileInclude; }
            set { _fileInclude = WildcardToRegex(value); }
        }

        [CliParse.ParsableArgument("matchdir", ShortName = 'd', Description = "Only directories matched by the supplied regular expression will be scanned")]
        public string DirectoryMatch { get; set; }

        [CliParse.ParsableArgument("verbosity", ShortName = 'v', DefaultValue = 0, Description = "Output verbosity level. 0=None, 1=Chatty Neighbor, 2=Gossip Columnist.  Default is 0.")]
        public int Verbosity { get; set; }

        [CliParse.ParsableArgument("recurse", ShortName = 'r', DefaultValue = false, Description = "Recurse subfolders contained in provided path.")]
        public bool Recurse { get; set; }

        public override void PreParse(IEnumerable<string> args, CliParseResult result)
        {
            base.PreParse(args, result);
            Path = Environment.CurrentDirectory;
        }


        public Dictionary<string, AnalysisExtensionSummary> Sort(Dictionary<string, AnalysisExtensionSummary> sortable)
        {
            return sortable.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        public static string WildcardToRegex(string pattern)
        {
            return pattern.Replace("*",".*");
        }

        public bool ShouldScanDirectory(string directoryPath)
        {
            if (string.IsNullOrEmpty(DirectoryMatch)) return true;

            var directoryName = System.IO.Path.GetDirectoryName(directoryPath);
            return string.IsNullOrEmpty(directoryName) || Regex.IsMatch(directoryName, DirectoryMatch);
        }

        public bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(FileMatch) && string.IsNullOrEmpty(FileInclude)) return true;

            var filename = System.IO.Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(filename)) return true;

            if (!string.IsNullOrEmpty(FileMatch) && !Regex.IsMatch(filename, FileMatch))
                return false;

            return string.IsNullOrEmpty(FileInclude) || Regex.IsMatch(filename, FileInclude);
        }
    }
}