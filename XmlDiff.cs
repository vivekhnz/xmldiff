using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using xmldiff.Modifications;

namespace xmldiff
{
    public class XmlDiff
    {
        public XmlSelector Selector { get; private set; }
        public List<XmlModification> Modifications { get; private set; }

        private XmlDiff() { }

        public XmlDiff(XmlNodeDiff rootDiff)
        {
            Selector = rootDiff.Selector;

            // sort modifications by node ID first, then by modification type
            Modifications = rootDiff.Modifications.OrderByDescending(m => m.NodeId).ThenBy(m => (int)m.Type).ToList();
        }

        public void Save(string path)
        {
            // serialize to doc
            var doc = new XmlDocument();

            var declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.InsertBefore(declaration, doc.DocumentElement);

            var patch = doc.CreateElement("Patch");
            doc.AppendChild(patch);

            var selectors = doc.CreateElement("Selectors");
            selectors.AppendChild(Selector.Serialize(doc));
            patch.AppendChild(selectors);

            var mods = doc.CreateElement("Modifications");
            foreach (var mod in Modifications)
            {
                mods.AppendChild(mod.Serialize(doc));
            }
            patch.AppendChild(mods);

            // save to file
            doc.Save(path);
        }

        public static XmlDiff Load(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);

            var patch = doc.DocumentElement;
            var selector = patch.SelectSingleNode("Selectors/Selector");
            var modifications = patch.SelectNodes("Modifications/*");

            return new XmlDiff
            {
                Selector = XmlSelector.Deserialize(selector),
                Modifications = modifications.Cast<XmlNode>().Select(n => XmlModification.Deserialize(n)).ToList()
            };
        }
    }
}