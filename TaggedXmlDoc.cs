using System;
using System.Xml;

public class TaggedXmlDoc
{
    private static readonly string IdAttribute = "xmldiff.Id";

    private XmlDocument doc;
    private int tagCount = 0;

    public TaggedXmlDoc(XmlDocument doc)
    {
        this.doc = doc.Clone() as XmlDocument;
        TagNode(this.doc.DocumentElement);
    }

    private void TagNode(XmlElement element)
    {
        if (element == null)
            return;

        var attribute = doc.CreateAttribute(IdAttribute);
        attribute.Value = tagCount.ToString();
        tagCount++;

        element.Attributes.Append(attribute);

        if (element.HasChildNodes)
        {
            foreach (var child in element.ChildNodes)
            {
                TagNode(child as XmlElement);
            }
        }
    }

    public void Save(string path)
    {
        doc.Save(path);
    }
}