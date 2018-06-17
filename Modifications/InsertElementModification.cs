using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff.Modifications
{
    public class InsertElementModification : XmlModification
    {
        public XmlElement Element { get; private set; }
        public int AfterNodeId { get; private set; }

        public InsertElementModification(int id, XmlElement element, int afterNodeId)
            : base(XmlModificationType.InsertElement, id)
        {
            Element = element;
            AfterNodeId = afterNodeId;
        }

        public InsertElementModification(int id, XmlNode node)
            : this(id, node.FirstChild as XmlElement,
                int.Parse(node.SelectSingleNode($"@{nameof(AfterNodeId)}")?.Value))
        {
        }

        public override void Serialize(XmlElement element)
        {
            element.AppendChild(element.OwnerDocument.ImportNode(Element, true));
            AppendAttribute(element, nameof(AfterNodeId), AfterNodeId.ToString());
        }

        public override void Apply(XmlElement element)
        {
            var toInsertAfter = element.SelectSingleNode($"*[@{TaggedXmlDoc.IdAttribute}={AfterNodeId}]");
            var imported = element.OwnerDocument.ImportNode(Element, true);
            element.InsertAfter(imported, toInsertAfter);
        }
    }
}