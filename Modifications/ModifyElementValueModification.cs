using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff.Modifications
{
    public class ModifyElementValueModification : XmlModification
    {
        public string NewValue { get; private set; }

        public ModifyElementValueModification(int id, string innerText)
            : base(XmlModificationType.ModifyElementValue, id)
        {
            NewValue = innerText;
        }

        public ModifyElementValueModification(int id, XmlNode node)
            : this(id, node.SelectSingleNode($"@{nameof(NewValue)}")?.Value)
        {
        }

        public override void Serialize(XmlElement element)
        {
            AppendAttribute(element, nameof(NewValue), NewValue);
        }

        public override void Apply(XmlElement element)
        {
            element.InnerText = NewValue;
        }
    }
}