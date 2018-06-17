using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff
{
    public class XmlSelector
    {
        private static readonly string SelectorNodeName = "Selector";

        public string XPath { get; set; }
        public int NodeId { get; set; }
        public IEnumerable<XmlSelector> Children { get; set; }

        public XmlSelector() { }

        public XmlSelector(string xpath, int id, IEnumerable<XmlSelector> children)
        {
            XPath = xpath;
            NodeId = id;
            Children = children;
        }

        public XmlSelector(string xpath, int id)
            : this(xpath, id, new List<XmlSelector>()) { }

        public XmlElement Serialize(XmlDocument doc)
        {
            var element = doc.CreateElement(SelectorNodeName);

            var idAttribute = doc.CreateAttribute(nameof(NodeId));
            idAttribute.Value = NodeId.ToString();
            element.Attributes.Append(idAttribute);

            var pathAttribute = doc.CreateAttribute(nameof(XPath));
            pathAttribute.Value = XPath;
            element.Attributes.Append(pathAttribute);

            if (Children != null)
            {
                foreach (var child in Children)
                {
                    element.AppendChild(child.Serialize(doc));
                }
            }

            return element;
        }
    }
}