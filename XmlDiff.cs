using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff
{
    public class XmlDiff
    {
        public static readonly string ModificationNodeName = "Modification";

        private TaggedXmlDoc taggedOriginal;
        private XmlDocument modified;
        private XmlDocument patch;

        public XmlDiff(TaggedXmlDoc original, XmlDocument modified)
        {
            this.taggedOriginal = original;
            this.modified = modified.Clone() as XmlDocument;
            this.patch = Diff();
        }

        private XmlDocument Diff()
        {
            var doc = new XmlDocument();

            var declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.InsertBefore(declaration, doc.DocumentElement);

            var patch = doc.CreateElement("Patch");
            doc.AppendChild(patch);

            foreach (var mod in Diff(taggedOriginal.Document.DocumentElement, modified.DocumentElement))
            {
                patch.AppendChild(SerializeModification(doc, mod));
            }

            return doc;
        }

        public IEnumerable<XmlModification> Diff(XmlElement original, XmlElement modified)
        {
            var id = taggedOriginal.GetId(original);
            var mods = new List<XmlModification>();

            // determine whether element has been renamed
            if (original.Name != modified.Name)
            {
                // element has been renamed
                mods.Add(XmlModification.RenameElement(id, modified.Name));
            }

            // get original and modified attributes
            var originalAttributes = original.Attributes.Cast<XmlAttribute>().Where(a => a.Name != TaggedXmlDoc.IdAttribute)
                .GroupBy(a => a.Name).ToDictionary(a => a.Key, a => a.First().Value);
            var modifiedAttributes = modified.Attributes.Cast<XmlAttribute>().Where(a => a.Name != TaggedXmlDoc.IdAttribute)
                .GroupBy(a => a.Name).ToDictionary(a => a.Key, a => a.First().Value);

            // compare attributes
            foreach (var attribute in modifiedAttributes)
            {
                if (originalAttributes.TryGetValue(attribute.Key, out var oldValue))
                {
                    if (attribute.Value != oldValue)
                    {
                        // attribute value has changed
                        mods.Add(XmlModification.ModifyAttribute(id, attribute.Key, attribute.Value));
                    }

                    // remove from the original list so we can identify which attributes were removed
                    originalAttributes.Remove(attribute.Key);
                }
                else
                {
                    // attribute has been added
                    mods.Add(XmlModification.AddAttribute(id, attribute.Key, attribute.Value));
                }
            }

            // the remaining original attributes have been removed
            foreach (var attribute in originalAttributes)
            {
                // attribute was removed
                mods.Add(XmlModification.RemoveAttribute(id, attribute.Key));
            }

            // get original and modified child elements
            var originalChildElements = original.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement).GroupBy(e => e.Name)
                .ToDictionary(g => g.Key, g => g.ToList());
            var modifiedChildElements = modified.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement).GroupBy(e => e.Name)
                .ToDictionary(g => g.Key, g => g.ToList());

            // compare child elements
            var additions = new List<XmlElement>();
            var presentInModified = new List<string>();
            foreach (var modifiedGroup in modifiedChildElements)
            {
                presentInModified.Add(modifiedGroup.Key);
                if (originalChildElements.TryGetValue(modifiedGroup.Key, out var originalElements))
                {
                    if (modifiedGroup.Value.Count == 1 && originalElements.Count == 1)
                    {
                        mods.AddRange(Diff(originalElements.First() as XmlElement, modifiedGroup.Value.First() as XmlElement));
                    }
                    else
                    {
                        // todo: more than one element with this name exists
                        Console.WriteLine($"{id}: Multiple children elements with name '{modifiedGroup.Key}'");
                    }
                }
                else
                {
                    // this child element / these child elements were added or renamed
                    foreach (var element in modifiedGroup.Value)
                    {
                        additions.Add(element as XmlElement);
                    }
                }
            }

            // determine whether items were added, renamed or removed
            foreach (var originalGroup in originalChildElements.Where(g => !presentInModified.Contains(g.Key)))
            {
                for (int r = 0; r < originalGroup.Value.Count; r++)
                {
                    var removal = originalGroup.Value[r] as XmlElement;
                    var removalId = taggedOriginal.GetId(removal);
                    bool wasRenamed = false;
                    for (int a = 0; a < additions.Count; a++)
                    {
                        var addition = additions[a];
                        var comparison = Diff(removal, addition);
                        if (!comparison.Any(m => m.Type != XmlModificationType.RenameElement && m.NodeId == removalId))
                        {
                            // the 'removed' element was actually renamed to the 'addition'
                            mods.Add(XmlModification.RenameElement(removalId, addition.Name));
                            additions.RemoveAt(a);
                            a--;
                            wasRenamed = true;
                            break;
                        }
                    }

                    if (!wasRenamed)
                    {
                        // structure is different to any items being added
                        mods.Add(XmlModification.RemoveElement(removalId));
                    }
                }
            }

            // add any remaining additions
            foreach (var addition in additions)
            {
                mods.Add(XmlModification.AddElement(id, addition));
            }

            // determine whether element value has changed
            if (modifiedChildElements.Count == 0 && original.InnerText != modified.InnerText)
            {
                mods.Add(XmlModification.ModifyElementValue(id, modified.InnerText));
            }

            // sort modifications by node ID first, then by modification type
            return mods.OrderByDescending(m => m.NodeId).ThenBy(m => (int)m.Type);
        }

        private XmlElement SerializeModification(XmlDocument doc, XmlModification mod)
        {
            var element = doc.CreateElement(ModificationNodeName);

            var idAttribute = doc.CreateAttribute(nameof(mod.NodeId));
            idAttribute.Value = mod.NodeId.ToString();
            element.Attributes.Append(idAttribute);

            var typeAttribute = doc.CreateAttribute(nameof(mod.Type));
            typeAttribute.Value = mod.Type.ToString();
            element.Attributes.Append(typeAttribute);

            if (!string.IsNullOrWhiteSpace(mod.Name))
            {
                var nameAttribute = doc.CreateAttribute(nameof(mod.Name));
                nameAttribute.Value = mod.Name.ToString();
                element.Attributes.Append(nameAttribute);
            }

            if (!string.IsNullOrWhiteSpace(mod.Value))
            {
                var valueAttribute = doc.CreateAttribute(nameof(mod.Value));
                valueAttribute.Value = mod.Value.ToString();
                element.Attributes.Append(valueAttribute);
            }

            if (mod.Element != null)
            {
                element.AppendChild(doc.ImportNode(mod.Element, true));
            }

            return element;
        }

        public void Save(string path)
        {
            patch.Save(path);
        }
    }
}