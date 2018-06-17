using System;
using System.Xml;

namespace xmldiff
{
    public class XmlDoc
    {
        private XmlDocument doc;

        public XmlDoc(string path)
        {
            doc = new XmlDocument();
            doc.Load(path);
        }

        public TaggedXmlDoc Tag()
        {
            return new TaggedXmlDoc(doc);
        }

        public XmlDiff Diff(TaggedXmlDoc original)
        {
            return new XmlDiff(original.Root.Diff(doc.DocumentElement));
        }
    }
}