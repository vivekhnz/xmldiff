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

        public TaggedXmlDoc Patch(XmlDiff patch)
        {
            // tag source document using XPath selectors
            var tagged = new TaggedXmlDoc(doc, patch.Selector);

            // apply modifications
            foreach (var mod in patch.Modifications)
            {
                var element = tagged.FindById(mod.NodeId);
                if (element != null)
                {
                    element.ApplyModification(mod);
                }
            }

            // strip identifiers
            tagged.StripIds();

            return tagged;
        }
    }
}