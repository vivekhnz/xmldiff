using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff.Modifications
{
    public abstract class XmlModification
    {
        private static readonly string ModificationNodeName = "Modification";

        public XmlModificationType Type { get; private set; }
        public int NodeId { get; private set; }

        protected XmlModification(XmlModificationType type, int id)
        {
            Type = type;
            NodeId = id;
        }

        public XmlElement Serialize(XmlDocument doc)
        {
            var element = doc.CreateElement(ModificationNodeName);

            AppendAttribute(element, nameof(NodeId), NodeId.ToString());
            AppendAttribute(element, nameof(Type), Type.ToString());

            Serialize(element);

            return element;
        }

        public static XmlModification Deserialize(XmlNode node)
        {
            if (!Enum.TryParse(typeof(XmlModificationType), node.SelectSingleNode("@Type")?.Value, out var type))
            {
                throw new Exception($"Couldn't identify modification type for node '{node.Name}'");
            }
            if (!int.TryParse(node.SelectSingleNode("@NodeId")?.Value, out var id))
            {
                throw new Exception($"Couldn't identify node ID for node '{node.Name}'");
            }

            switch (type)
            {
                case XmlModificationType.ModifyElementValue:
                    return new ModifyElementValueModification(id, node);
                case XmlModificationType.InsertElement:
                    return new InsertElementModification(id, node);
                case XmlModificationType.ModifyAttribute:
                    return new ModifyAttributeModification(id, node);
                case XmlModificationType.AddAttribute:
                    return new AddAttributeModification(id, node);
                case XmlModificationType.RemoveElement:
                    return new RemoveElementModification(id, node);
                case XmlModificationType.RemoveAttribute:
                    return new RemoveAttributeModification(id, node);
                case XmlModificationType.RenameElement:
                    return new RenameElementModification(id, node);
            }

            throw new NotImplementedException($"Modifications of type '{type}' are not supported.");
        }

        protected void AppendAttribute(XmlElement element, string name, string value)
        {
            var attribute = element.OwnerDocument.CreateAttribute(name);
            attribute.Value = value;
            element.Attributes.Append(attribute);
        }

        public abstract void Serialize(XmlElement element);
        public abstract void Apply(XmlElement element);
    }

    public enum XmlModificationType
    {
        Unknown,

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