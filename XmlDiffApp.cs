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
            [Argument(Name = "Output patch file", Description = "Path to save the patch file to")] string patchOutputPath)
        {
            // verify source files exist
            if (!File.Exists(originalXmlPath))
            {
                Console.WriteLine($"Could not find original XML file at '{originalXmlPath}'");
                return;
            }
            if (!File.Exists(modifiedXmlPath))
            {
                Console.WriteLine($"Could not find modified XML file at '{modifiedXmlPath}'");
                return;
            }

            // assign each element a unique ID
            var original = new XmlDoc(originalXmlPath);
            var taggedDoc = original.Tag();

            // diff files
            var modified = new XmlDoc(modifiedXmlPath);
            var diff = modified.Diff(taggedDoc);

            // save patch file
            diff.Save(patchOutputPath);
        }

        [ApplicationMetadata(Description = "Patches an XML file with a specified diff file.")]
        public void Patch(
            [Argument(Name = "Source XML file", Description = "Path to the source XML file to patch")] string sourceXmlPath,
            [Argument(Name = "Patch file", Description = "Path to the patch file")] string patchPath,
            [Argument(Name = "Destination XML file", Description = "Path to save the patched XML file to")] string destinationXmlPath)
        {
            // verify source files exist
            if (!File.Exists(sourceXmlPath))
            {
                Console.WriteLine($"Could not find source XML file at '{sourceXmlPath}'");
                return;
            }
            if (!File.Exists(patchPath))
            {
                Console.WriteLine($"Could not find patch file at '{patchPath}'");
                return;
            }

            // load source doc and patch
            var source = new XmlDoc(sourceXmlPath);
            var patch = XmlDiff.Load(patchPath);

            // apply patch
            var patched = source.Patch(patch);

            // save patched file
            patched.Save(destinationXmlPath);
        }
    }
}