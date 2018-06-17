using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff.Modifications
{
    public class AddAttributeModification : XmlModification
    {
        public string Name { get; private set; }
        public string Value { get; private set; }

        public AddAttributeModification(int id, string name, string value)
            : base(XmlModificationType.AddAttribute, id)
        {
            Name = name;
            Value = value;
        }

        public AddAttributeModification(int id, XmlNode node)
            : this(id, node.SelectSingleNode($"@{nameof(Name)}")?.Value,
                node.SelectSingleNode($"@{nameof(Value)}")?.Value)
        {
        }

        public override void Serialize(XmlElement element)
        {
            AppendAttribute(element, nameof(Name), Name);
            AppendAttribute(element, nameof(Value), Value);
        }

        public override void Apply(XmlElement element)
        {
            AppendAttribute(element, Name, Value);
        }
    }
}