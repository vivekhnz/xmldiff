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

            // compare attributes
            mods.AddRange(CompareAttributes(id, original, modified));

            // get original and modified child elements
            var originalChildElements = original.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement)
                .Select(n => n as XmlElement);
            var modifiedChildElements = modified.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement)
                .Select(n => n as XmlElement);

            var originalChildElementsGrouped = originalChildElements.GroupBy(e => e.Name)
                .ToDictionary(g => g.Key, g => g.ToList());
            var modifiedChildElementsGrouped = modifiedChildElements.GroupBy(e => e.Name)
                .ToDictionary(g => g.Key, g => g.ToList());

            // compare child elements
            if (originalChildElementsGrouped.Any(g => g.Value.Count > 1) ||
                modifiedChildElementsGrouped.Any(g => g.Value.Count > 1))
            {
                // if there are elements with duplicate names, the order of them probably matters
                mods.AddRange(CompareOrderedElements(id, originalChildElements, modifiedChildElements));
            }
            else
            {
                mods.AddRange(CompareUnorderedElements(id, originalChildElements, modifiedChildElements));
            }

            // determine whether element value has changed
            if (modifiedChildElements.Count() == 0 && original.InnerText != modified.InnerText)
            {
                mods.Add(XmlModification.ModifyElementValue(id, modified.InnerText));
            }

            // sort modifications by node ID first, then by modification type
            return mods.OrderByDescending(m => m.NodeId).ThenBy(m => (int)m.Type);
        }

        private IEnumerable<XmlModification> CompareAttributes(int id, XmlElement original, XmlElement modified)
        {
            var mods = new List<XmlModification>();

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

            return mods;
        }

        private IEnumerable<XmlModification> CompareOrderedElements(int id,
            IEnumerable<XmlElement> originalElements, IEnumerable<XmlElement> modifiedElements)
        {
            var mods = new List<XmlModification>();



            return mods;
        }

        private IEnumerable<XmlModification> CompareUnorderedElements(int id,
            IEnumerable<XmlElement> originalElements, IEnumerable<XmlElement> modifiedElements)
        {
            var mods = new List<XmlModification>();

            // compare child elements
            var possibleAdditions = new Dictionary<int, List<XmlElement>>();
            var presentInModified = new List<string>();

            int lastNodeId = -1;
            foreach (var modified in modifiedElements)
            {
                presentInModified.Add(modified.Name);
                var original = originalElements.FirstOrDefault(e => e.Name == modified.Name);
                if (original == null)
                {
                    // this child element was added or renamed
                    if (possibleAdditions.TryGetValue(lastNodeId, out var additionSet))
                    {
                        additionSet.Add(modified);
                    }
                    else
                    {
                        // record the ID of the last node so we know which node to insert the new element after
                        possibleAdditions.Add(lastNodeId, new List<XmlElement> { modified });
                    }
                }
                else
                {
                    lastNodeId = taggedOriginal.GetId(original);
                    mods.AddRange(Diff(original, modified));
                }
            }

            // determine whether items were added, renamed or removed
            foreach (var removal in originalElements.Where(e => !presentInModified.Contains(e.Name)))
            {
                var removalId = taggedOriginal.GetId(removal);
                bool wasRenamed = false;

                foreach (var additionSet in possibleAdditions)
                {
                    for (int a = 0; a < additionSet.Value.Count; a++)
                    {
                        var addition = additionSet.Value[a];
                        var comparison = Diff(removal, addition);
                        if (!comparison.Any(m => m.Type != XmlModificationType.RenameElement && m.NodeId == removalId))
                        {
                            // the 'removed' element was actually renamed to the 'addition'
                            mods.Add(XmlModification.RenameElement(removalId, addition.Name));
                            additionSet.Value.RemoveAt(a);
                            a--;
                            wasRenamed = true;
                            break;
                        }
                    }
                }

                if (!wasRenamed)
                {
                    // structure is different to any items being added
                    mods.Add(XmlModification.RemoveElement(removalId));
                }
            }

            // add any remaining additions
            foreach (var set in possibleAdditions)
            {
                foreach (var addition in set.Value)
                {
                    mods.Add(XmlModification.InsertElement(id, addition, set.Key));
                }
            }

            return mods;
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

            if (mod.AfterNodeId.HasValue)
            {
                var afterNodeIdAttribute = doc.CreateAttribute(nameof(mod.AfterNodeId));
                afterNodeIdAttribute.Value = mod.AfterNodeId.ToString();
                element.Attributes.Append(afterNodeIdAttribute);
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