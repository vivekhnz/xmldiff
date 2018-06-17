using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace xmldiff
{
    public class XmlDiff
    {
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

            var diff = Diff(null, taggedOriginal.Document.DocumentElement, modified.DocumentElement);

            var selectors = doc.CreateElement("Selectors");
            selectors.AppendChild(diff.Selector.Serialize(doc));
            patch.AppendChild(selectors);

            // sort modifications by node ID first, then by modification type
            var mods = doc.CreateElement("Modifications");
            foreach (var mod in diff.Modifications.OrderByDescending(m => m.NodeId).ThenBy(m => (int)m.Type))
            {
                mods.AppendChild(mod.Serialize(doc));
            }
            patch.AppendChild(mods);

            return doc;
        }

        private XmlNodeDiff Diff(string xpath, XmlElement original, XmlElement modified)
        {
            if (string.IsNullOrEmpty(xpath))
            {
                xpath = original.Name;
            }
            var id = taggedOriginal.GetId(original);
            var mods = new List<XmlModification>();

            var originalChildElements = original.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement)
                .Select(n => n as XmlElement).ToList();
            var modifiedChildElements = new List<XmlElement>();

            if (modified != null)
            {
                // determine whether element has been renamed
                if (original.Name != modified.Name)
                {
                    // element has been renamed
                    mods.Add(XmlModification.RenameElement(id, modified.Name));
                }

                // compare attributes
                mods.AddRange(CompareAttributes(id, original, modified));

                // compare element value
                modifiedChildElements = modified.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement)
                    .Select(n => n as XmlElement).ToList();
                if (modifiedChildElements.Count == 0 && original.InnerText != modified.InnerText)
                {
                    mods.Add(XmlModification.ModifyElementValue(id, modified.InnerText));
                }
                else if (modifiedChildElements.Count > 0 && originalChildElements.Count == 0)
                {
                    mods.AddRange(modifiedChildElements.Select(e => XmlModification.InsertElement(id, e, -1)));
                }
            }

            if (originalChildElements.Count == 0 || modified == null)
            {
                return new XmlNodeDiff(new XmlSelector(xpath, taggedOriginal.GetId(original)), mods);
            }

            // if all element names are unique, just specify the element name
            var groupedOriginalElements = originalChildElements.GroupBy(e => e.Name);
            if (groupedOriginalElements.All(g => g.Count() == 1) &&
                TryMapModifiedElements(original, modified, e => e.Name, out var mapped, out var childMods))
            {
                mods.AddRange(childMods);
                return new XmlNodeDiff(new XmlSelector(xpath, taggedOriginal.GetId(original), mapped), mods);
            }

            // if all elements have a common 'primary key' attribute, specify the element name + attribute
            if (groupedOriginalElements.Count() == 1 &&
                TryGetKeyAttribute(groupedOriginalElements.First().ToList(), out var pkAttribute) &&
                TryMapModifiedElements(original, modified,
                    e => $"{e.Name}[@{pkAttribute}='{e.GetAttribute(pkAttribute)}']", out mapped, out childMods))
            {
                mods.AddRange(childMods);
                return new XmlNodeDiff(new XmlSelector(xpath, taggedOriginal.GetId(original), mapped), mods);
            }

            // last resort - just specify the index
            if (modifiedChildElements.Count > originalChildElements.Count)
            {
                // if there are more children in the modified version, we need to add items to match
                var lastNodeId = taggedOriginal.GetId(originalChildElements.Last());
                mods.AddRange(modifiedChildElements.Skip(originalChildElements.Count).Select(
                    e => XmlModification.InsertElement(id, e, lastNodeId)));
            }
            else
            {
                // if there are less children in the modified version, we need to remove items to match
                mods.AddRange(originalChildElements.Skip(modifiedChildElements.Count).Select(
                    e => XmlModification.RemoveElement(taggedOriginal.GetId(e))));
                originalChildElements = originalChildElements.Take(modifiedChildElements.Count).ToList();
            }

            var children =
                (from i in Enumerable.Range(0, originalChildElements.Count)
                 let child = originalChildElements[i]
                 let mappedChild = modifiedChildElements[i]
                 select Diff($"*[{i}]", child, mappedChild)).ToList();
            mods.AddRange(children.SelectMany(c => c.Modifications));

            var selector = new XmlSelector(xpath, taggedOriginal.GetId(original), children.Select(c => c.Selector));
            return new XmlNodeDiff(selector, mods);
        }

        private bool TryMapModifiedElements(XmlElement original, XmlElement modified,
            Func<XmlElement, string> getChildXPath, out List<XmlSelector> mappedSelectors,
            out List<XmlModification> modifications)
        {
            modifications = new List<XmlModification>();
            var originalChildElements = original.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement)
                .Select(n => n as XmlElement).ToList();

            var mappedArray = new XmlSelector[originalChildElements.Count];
            var mappedToOriginalId = new Dictionary<XmlElement, int>();

            for (int i = 0; i < originalChildElements.Count; i++)
            {
                var element = originalChildElements[i];

                // get the XPath for this element and try to locate it in the modified doc
                var xpath = getChildXPath(element);
                var mapped = modified.SelectNodes(xpath).Cast<XmlNode>()
                    .Select(n => n as XmlElement).Where(n => n != null).ToList();

                if (mapped.Count == 1)
                {
                    // if we found exactly one match, this XPath is a valid mapping
                    var mappedNode = mapped.First();
                    var diff = Diff(xpath, element, mappedNode);
                    modifications.AddRange(diff.Modifications);
                    mappedArray[i] = diff.Selector;
                    mappedToOriginalId.Add(mappedNode, taggedOriginal.GetId(element));
                }
                else if (mapped.Count > 2)
                {
                    // we found more than one match - this XPath is ambiguous
                    mappedSelectors = null;
                    modifications = null;
                    return false;
                }

                // if we didn't find a match, the element was either removed or renamed
            }

            // identify additions, removals and renames
            var modifiedChildElements = modified.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement)
                .Select(n => n as XmlElement).ToList();
            int lastNodeId = -1;
            for (int a = 0; a < modifiedChildElements.Count; a++)
            {
                var addition = modifiedChildElements[a];
                if (mappedToOriginalId.TryGetValue(addition, out var mappedNodeId))
                {
                    // store the node ID of the original node so we know where to insert additions
                    lastNodeId = mappedNodeId;
                }
                else
                {
                    // identify whether this element was added or renamed
                    bool wasRenamed = false;
                    for (int r = 0; r < mappedArray.Length; r++)
                    {
                        if (mappedArray[r] == null)
                        {
                            var removal = originalChildElements[r];
                            var removalId = taggedOriginal.GetId(removal);

                            var diff = Diff(getChildXPath(removal), removal, addition);
                            if (diff.Modifications.All(
                                m => m.Type == XmlModificationType.RenameElement && m.NodeId == removalId))
                            {
                                // the 'removed' element was actually renamed to the 'addition'
                                modifications.AddRange(diff.Modifications);
                                mappedArray[r] = diff.Selector;
                                mappedToOriginalId.Add(addition, removalId);
                                wasRenamed = true;
                                break;
                            }
                        }
                    }

                    if (!wasRenamed)
                    {
                        // insert a new element
                        modifications.Add(XmlModification.InsertElement(taggedOriginal.GetId(original), addition, lastNodeId));
                    }
                }
            }

            // add removal modifications for any elements that were removed
            for (int i = 0; i < mappedArray.Length; i++)
            {
                if (mappedArray[i] == null)
                {
                    // remove the element
                    var removal = originalChildElements[i];
                    var diff = Diff(getChildXPath(removal), removal, null);
                    modifications.AddRange(diff.Modifications);
                    mappedArray[i] = diff.Selector;
                    modifications.Add(XmlModification.RemoveElement(taggedOriginal.GetId(removal)));
                }
            }

            mappedSelectors = mappedArray.ToList();
            return true;
        }

        private bool TryGetKeyAttribute(IEnumerable<XmlElement> elements, out string attributeName)
        {
            attributeName = null;

            // we assume that the first attribute of an element is the key
            var firstAttributeForEachElement = elements.Select(e => e.Attributes.Cast<XmlAttribute>().FirstOrDefault());

            // get the name of the attribute
            var keyAttribute = firstAttributeForEachElement.FirstOrDefault()?.Name;
            if (string.IsNullOrWhiteSpace(keyAttribute))
            {
                return false;
            }

            // verify that all elements have the same key attribute
            if (firstAttributeForEachElement.Any(a => a.Name != keyAttribute))
            {
                return false;
            }

            // verify that all keys have unique values
            if (firstAttributeForEachElement.GroupBy(a => a.Value).Any(g => g.Count() > 1))
            {
                return false;
            }

            attributeName = keyAttribute;
            return true;
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

        public void Save(string path)
        {
            patch.Save(path);
        }
    }
}