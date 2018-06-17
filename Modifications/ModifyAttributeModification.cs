using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff.Modifications
{
    public class ModifyAttributeModification : XmlModification
    {
        public string Name { get; private set; }
        public string NewValue { get; private set; }

        public ModifyAttributeModification(int id, string name, string value)
            : base(XmlModificationType.ModifyAttribute, id)
        {
            Name = name;
            NewValue = value;
        }

        public ModifyAttributeModification(int id, XmlNode node)
            : this(id, node.SelectSingleNode($"@{nameof(Name)}")?.Value,
                node.SelectSingleNode($"@{nameof(NewValue)}")?.Value)
        {
        }

        public override void Serialize(XmlElement element)
        {
            AppendAttribute(element, nameof(Name), Name);
            AppendAttribute(element, nameof(NewValue), NewValue);
        }

        public override void Apply(XmlElement element)
        {
            element.SetAttribute(Name, NewValue);
        }
    }
}