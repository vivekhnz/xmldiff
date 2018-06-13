using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff
{
    public class XmlModification
    {
        public XmlModificationType Type { get; set; }
        public int NodeId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public int? AfterNodeId { get; set; }
        public XmlElement Element { get; set; }

        public XmlModification() { }

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
        // order determines precedence (lower values will be processed first)
        RemoveAttribute,
        ModifyAttribute,
        AddAttribute,

        RemoveElement,
        RenameElement,
        ModifyElementValue,
        InsertElement
    }
}