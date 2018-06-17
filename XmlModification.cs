using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff
{
    public class XmlModification
    {
        private static readonly string ModificationNodeName = "Modification";

        public XmlModificationType Type { get; set; }
        public int NodeId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public int? AfterNodeId { get; set; }
        public XmlElement Element { get; set; }

        public XmlModification() { }

        public XmlElement Serialize(XmlDocument doc)
        {
            var element = doc.CreateElement(ModificationNodeName);

            var idAttribute = doc.CreateAttribute(nameof(NodeId));
            idAttribute.Value = NodeId.ToString();
            element.Attributes.Append(idAttribute);

            var typeAttribute = doc.CreateAttribute(nameof(Type));
            typeAttribute.Value = Type.ToString();
            element.Attributes.Append(typeAttribute);

            if (!string.IsNullOrWhiteSpace(Name))
            {
                var nameAttribute = doc.CreateAttribute(nameof(Name));
                nameAttribute.Value = Name.ToString();
                element.Attributes.Append(nameAttribute);
            }
            if (!string.IsNullOrWhiteSpace(Value))
            {
                var valueAttribute = doc.CreateAttribute(nameof(Value));
                valueAttribute.Value = Value.ToString();
                element.Attributes.Append(valueAttribute);
            }
            if (AfterNodeId.HasValue)
            {
                var afterNodeIdAttribute = doc.CreateAttribute(nameof(AfterNodeId));
                afterNodeIdAttribute.Value = AfterNodeId.ToString();
                element.Attributes.Append(afterNodeIdAttribute);
            }
            if (Element != null)
            {
                element.AppendChild(doc.ImportNode(Element, true));
            }

            return element;
        }

        public static XmlModification RenameElement(int id, string newName)
        {
            return new XmlModification
            {
                Type = XmlModificationType.RenameElement,
                NodeId = id,
                Name = newName
            };
        }

        public static XmlModification AddAttribute(int id, string name, string value)
        {
            return new XmlModification
            {
                Type = XmlModificationType.AddAttribute,
                NodeId = id,
                Name = name,
                Value = value
            };
        }

        public static XmlModification ModifyAttribute(int id, string name, string value)
        {
            return new XmlModification
            {
                Type = XmlModificationType.ModifyAttribute,
                NodeId = id,
                Name = name,
                Value = value
            };
        }

        public static XmlModification RemoveAttribute(int id, string name)
        {
            return new XmlModification
            {
                Type = XmlModificationType.RemoveAttribute,
                NodeId = id,
                Name = name
            };
        }

        public static XmlModification InsertElement(int id, XmlElement element, int afterNodeId)
        {
            return new XmlModification
            {
                Type = XmlModificationType.InsertElement,
                NodeId = id,
                Element = element,
                AfterNodeId = afterNodeId
            };
        }

        public static XmlModification RemoveElement(int id)
        {
            return new XmlModification
            {
                Type = XmlModificationType.RemoveElement,
                NodeId = id
            };
        }

        public static XmlModification ModifyElementValue(int id, string newValue)
        {
            return new XmlModification
            {
                Type = XmlModificationType.ModifyElementValue,
                NodeId = id,
                Value = newValue
            };
        }
    }

    public enum XmlModificationType
    {
        // order determines precedence (earlier values will be processed first)
        ModifyElementValue,
        InsertElement,

        ModifyAttribute,
        AddAttribute,

        RemoveElement,
        RemoveAttribute,
        RenameElement
    }
}