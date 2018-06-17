using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff.Modifications
{
    public class RemoveAttributeModification : XmlModification
    {
        public string Name { get; private set; }

        public RemoveAttributeModification(int id, string name)
            : base(XmlModificationType.RemoveAttribute, id)
        {
            Name = name;
        }

        public RemoveAttributeModification(int id, XmlNode node)
            : this(id, node.SelectSingleNode($"@{nameof(Name)}")?.Value)
        {
        }

        public override void Serialize(XmlElement element)
        {
            AppendAttribute(element, nameof(Name), Name);
        }

        public override void Apply(XmlElement element)
        {
            element.RemoveAttribute(Name);
        }
    }
}