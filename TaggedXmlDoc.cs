using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff
{
    public class TaggedXmlDoc
    {
        public static readonly string IdAttribute = "xmldiff.Id";

        private XmlDocument doc;

        public TaggedXmlElement Root { get; private set; }

        public TaggedXmlDoc(XmlDocument doc)
        {
            // tag each node incrementally
            this.doc = doc.Clone() as XmlDocument;
            Root = TagNodeIncrementally(this.doc.DocumentElement, 0);
        }

        public TaggedXmlDoc(XmlDocument doc, XmlSelector selector)
        {
            // tag each node using XPath selector
            this.doc = doc.Clone() as XmlDocument;
            Root = TagNodeWithXPath(this.doc, selector);
        }

        private TaggedXmlElement TagNodeIncrementally(XmlElement element, int id)
        {
            if (element == null)
                return null;

            int myId = id;
            id++;

            var children = new List<TaggedXmlElement>();
            if (element.HasChildNodes)
            {
                var childElements = element.ChildNodes.Cast<XmlNode>().Select(n => n as XmlElement)
                    .Where(e => e != null).ToList();
                for (int i = 0; i < childElements.Count; i++)
                {
                    var child = TagNodeIncrementally(childElements[i], id);
                    children.Add(child);

                    id = child.Id + 1;
                    if (child.Elements.Count > 0)
                    {
                        id = child.Elements.Last().Id + 1;
                    }
                }
            }

            return new TaggedXmlElement(myId, element, children);
        }

        private TaggedXmlElement TagNodeWithXPath(XmlNode parent, XmlSelector selector)
        {
            var node = parent.SelectSingleNode(selector.XPath) as XmlElement;
            if (node != null)
            {
                var attribute = doc.CreateAttribute(IdAttribute);
                attribute.Value = selector.NodeId.ToString();
                node.Attributes.Append(attribute);

                var childNodes = selector.Children.Select(s => TagNodeWithXPath(node, s))
                    .Where(n => n != null).ToList();

                return new TaggedXmlElement(selector.NodeId, node, childNodes);
            }

            return null;
        }

        public TaggedXmlElement FindById(int id)
        {
            return FindById(Root, id);
        }

        private TaggedXmlElement FindById(TaggedXmlElement parent, int id)
        {
            if (parent.Id == id)
            {
                return parent;
            }

            foreach (var child in parent.Elements)
            {
                var match = FindById(child, id);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        public void StripIds()
        {
            StripIds(doc.DocumentElement);
        }

        private void StripIds(XmlElement element)
        {
            element.RemoveAttribute(IdAttribute);
            foreach (var child in element.ChildNodes.Cast<XmlNode>().Select(n => n as XmlElement).Where(e => e != null))
            {
                StripIds(child);
            }
        }

        public void Save(string path)
        {
            doc.Save(path);
        }
    }
}