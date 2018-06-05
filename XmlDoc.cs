using System;
using System.Xml;

public class XmlDoc
{
    private XmlDocument doc;

    public XmlDoc(string path)
    {
        doc = new XmlDocument();
        doc.Load(path);
    }

    public TaggedXmlDoc Tag()
    {
        return new TaggedXmlDoc(doc);
    }
}