using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff.Modifications
{
    public class RemoveElementModification : XmlModification
    {
        public RemoveElementModification(int id)
            : base(XmlModificationType.RemoveElement, id)
        {
        }

        public RemoveElementModification(int id, XmlNode node)
            : this(id)
        {
        }

        public override void Serialize(XmlElement element)
        {
        }

        public override void Apply(XmlElement element)
        {
            element.ParentNode.RemoveChild(element);
        }
    }
}