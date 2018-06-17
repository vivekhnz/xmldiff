using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using xmldiff.Modifications;

namespace xmldiff
{
    public class XmlNodeDiff
    {
        public XmlSelector Selector { get; private set; }
        public List<XmlModification> Modifications { get; private set; }

        public XmlNodeDiff(XmlSelector selector, List<XmlModification> mods)
        {
            Selector = selector;
            Modifications = mods;
        }
    }
}