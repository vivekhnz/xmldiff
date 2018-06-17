using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff.Modifications
{
    public class RenameElementModification : XmlModification
    {
        public string NewName { get; private set; }

        public RenameElementModification(int id, string newName)
            : base(XmlModificationType.RenameElement, id)
        {
            NewName = newName;
        }

        public RenameElementModification(int id, XmlNode node)
            : this(id, node.SelectSingleNode($"@{nameof(NewName)}")?.Value)
        {
        }

        public override void Serialize(XmlElement element)
        {
            AppendAttribute(element, nameof(NewName), NewName);
        }

        public override void Apply(XmlElement element)
        {
            // create an element with the new name
            var newElement = element.OwnerDocument.CreateElement(NewName);

            // move attributes
            while (element.HasAttributes)
            {
                newElement.SetAttributeNode(element.RemoveAttributeNode(element.Attributes[0]));
            }

            // move child elements
            while (element.HasChildNodes)
            {
                newElement.AppendChild(element.FirstChild);
            }

            // replace existing element
            if (element.ParentNode != null)
            {
                element.ParentNode.ReplaceChild(newElement, element);
            }
        }
    }
}