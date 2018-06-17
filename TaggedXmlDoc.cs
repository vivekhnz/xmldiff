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
        private int tagCount = 0;

        public TaggedXmlElement Root { get; private set; }

        public TaggedXmlDoc(XmlDocument doc)
        {
            this.doc = doc.Clone() as XmlDocument;
            Root = TagNode(this.doc.DocumentElement);
        }

        private TaggedXmlElement TagNode(XmlElement element)
        {
            if (element == null)
                return null;

            int id = tagCount;
            tagCount++;

            /*
            var attribute = doc.CreateAttribute(IdAttribute);
            attribute.Value = id.ToString();
            element.Attributes.Append(attribute);
            */

            var children = new List<TaggedXmlElement>();
            if (element.HasChildNodes)
            {
                children = element.ChildNodes.Cast<XmlNode>().Select(n => TagNode(n as XmlElement))
                    .Where(e => e != null).ToList();
            }

            return new TaggedXmlElement(id, element, children);
        }

        /*
        public int GetId(XmlElement element)
        {
            var attribute = element.GetAttribute(IdAttribute);
            if (string.IsNullOrEmpty(attribute))
                throw new Exception($"Specified node was not tagged with an '{IdAttribute}' attribute");

            return int.Parse(attribute);
        }
        */
    }
}