using System;
using System.IO;
using CommandDotNet.Attributes;

namespace xmldiff
{
    public class XmlDiffApp
    {
        [ApplicationMetadata(Description = "Creates a diff between two XML files.")]
        public void Diff(
            [Argument(Name = "Original XML file", Description = "Path to the original XML file")] string originalXmlPath,
            [Argument(Name = "Modified XML file", Description = "Path to the modified XML file")] string modifiedXmlPath,
            [Argument(Name = "Output tagged XML file", Description = "Path to save the tagged XML file to")] string taggedXmlOutputPath,
            [Argument(Name = "Output patch file", Description = "Path to save the patch file to")] string patchOutputPath)
        {
            // verify source files exist
            if (!File.Exists(originalXmlPath))
                Console.WriteLine($"Could not find original XML file at '{originalXmlPath}'");
            if (!File.Exists(modifiedXmlPath))
                Console.WriteLine($"Could not find modified XML file at '{modifiedXmlPath}'");
        }
    }
}