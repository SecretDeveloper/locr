using System.Collections.Generic;
using System.Linq;

namespace locr.lib
{
    [CliParse.ParsableClass("locr", "Utility for counting the number of lines conained in a file or directory of files.")]
    public class AnalysisOptions:CliParse.Parsable
    {
        [CliParse.ParsableArgument('p', "path", Required = true)]
        public string Path { get; set; }

        [CliParse.ParsableArgument('m', "match", Description = "Only files matched by the supplied regular expression will be scanned")]
        public string FileMatch { get; set; }

        [CliParse.ParsableArgument('d', "matchdir", Description = "Only directories matched by the supplied regular expression will be scanned")]
        public string DirectoryMatch { get; set; }


        public Dictionary<string, AnalysisFileResult> Sort(Dictionary<string, AnalysisFileResult> sortable)
        {
            return sortable.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        public bool ShouldScanDirectory(string path)
        {
            return true;
        }

        public bool ShouldScanFile(string path)
        {
            return true;
        }
    }
}