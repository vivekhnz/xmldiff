using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using xmldiff.Modifications;

namespace xmldiff
{
    public class TaggedXmlElement
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public List<TaggedXmlElement> Elements { get; private set; }
        public Dictionary<string, string> Attributes { get; private set; }
        public string ElementValue { get; private set; }

        private XmlElement element;

        public TaggedXmlElement(int id, XmlElement original, List<TaggedXmlElement> children)
        {
            Id = id;
            element = original;

            Attributes = original.Attributes.Cast<XmlAttribute>().GroupBy(a => a.Name)
                .ToDictionary(g => g.Key, g => g.First().Value);
            Name = original.Name;
            Elements = children;
            ElementValue = children.Count > 0 ? string.Empty : original.InnerText;
        }

        public XmlNodeDiff Diff(XmlElement modified)
        {
            return Diff(Name, modified);
        }

        public XmlNodeDiff Diff(string xpath, XmlElement modified)
        {
            var mods = new List<XmlModification>();
            var modifiedChildElements = new List<XmlElement>();

            if (modified != null)
            {
                // determine whether element has been renamed
                if (Name != modified.Name)
                {
                    // element has been renamed
                    mods.Add(new RenameElementModification(Id, modified.Name));
                }

                // compare attributes
                mods.AddRange(CompareAttributes(modified));

                // compare element value
                modifiedChildElements = modified.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement)
                    .Select(n => n as XmlElement).ToList();
                if (modifiedChildElements.Count == 0 && ElementValue != modified.InnerText)
                {
                    mods.Add(new ModifyElementValueModification(Id, modified.InnerText));
                }
                else if (modifiedChildElements.Count > 0 && Elements.Count == 0)
                {
                    mods.AddRange(modifiedChildElements.Select(e => new InsertElementModification(Id, e, -1)));
                }
            }

            if (Elements.Count == 0 || modified == null)
            {
                return new XmlNodeDiff(new XmlSelector(xpath, Id), mods);
            }

            // if all element names are unique, just specify the element name
            var groupedOriginalElements = Elements.GroupBy(e => e.Name);
            if (groupedOriginalElements.All(g => g.Count() == 1) &&
                TryMapModifiedElements(modified, e => e.Name, out var mapped, out var childMods))
            {
                mods.AddRange(childMods);
                return new XmlNodeDiff(new XmlSelector(xpath, Id, mapped), mods);
            }

            // if all elements have a common 'primary key' attribute, specify the element name + attribute
            if (groupedOriginalElements.Count() == 1 &&
                TryGetKeyAttribute(groupedOriginalElements.First().ToList(), out var pkAttribute) &&
                TryMapModifiedElements(modified,
                    e => $"{e.Name}[@{pkAttribute}='{e.Attributes[pkAttribute]}']", out mapped, out childMods))
            {
                mods.AddRange(childMods);
                return new XmlNodeDiff(new XmlSelector(xpath, Id, mapped), mods);
            }

            // last resort - just specify the index

            // if we're selecting by index, the number of original and modified elements needs to be equal
            var elementsToSerialize = Elements;
            if (modifiedChildElements.Count > Elements.Count)
            {
                // if there are more children in the modified version, we need to add items to match
                var lastNodeId = Elements.Last().Id;
                mods.AddRange(modifiedChildElements.Skip(Elements.Count).Select(
                    e => new InsertElementModification(Id, e, lastNodeId)));
            }
            else
            {
                // if there are less children in the modified version, we need to remove items to match
                mods.AddRange(Elements.Skip(modifiedChildElements.Count).Select(
                    e => new RemoveElementModification(e.Id)));
                elementsToSerialize = Elements.Take(modifiedChildElements.Count).ToList();
            }

            var children =
                (from i in Enumerable.Range(0, Elements.Count)
                 let child = Elements[i]
                 let mappedChild = modifiedChildElements[i]
                 select child.Diff($"*[{i + 1}]", mappedChild)).ToList();
            mods.AddRange(children.SelectMany(c => c.Modifications));

            var selector = new XmlSelector(xpath, Id, children.Select(c => c.Selector));
            return new XmlNodeDiff(selector, mods);
        }

        private bool TryMapModifiedElements(XmlElement modified, Func<TaggedXmlElement, string> getChildXPath,
            out List<XmlSelector> mappedSelectors, out List<XmlModification> modifications)
        {
            modifications = new List<XmlModification>();

            var mappedArray = new XmlSelector[Elements.Count];
            var mappedToOriginalId = new Dictionary<XmlElement, int>();

            for (int i = 0; i < Elements.Count; i++)
            {
                var element = Elements[i];

                // get the XPath for this element and try to locate it in the modified doc
                var xpath = getChildXPath(element);
                var mapped = modified.SelectNodes(xpath).Cast<XmlNode>()
                    .Select(n => n as XmlElement).Where(n => n != null).ToList();

                if (mapped.Count == 1)
                {
                    // if we found exactly one match, this XPath is a valid mapping
                    var mappedNode = mapped.First();
                    var diff = element.Diff(xpath, mappedNode);
                    modifications.AddRange(diff.Modifications);
                    mappedArray[i] = diff.Selector;
                    mappedToOriginalId.Add(mappedNode, element.Id);
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
                            var removal = Elements[r];
                            var diff = removal.Diff(getChildXPath(removal), addition);
                            if (diff.Modifications.All(
                                m => m.Type == XmlModificationType.RenameElement && m.NodeId == removal.Id))
                            {
                                // the 'removed' element was actually renamed to the 'addition'
                                modifications.AddRange(diff.Modifications);
                                mappedArray[r] = diff.Selector;
                                mappedToOriginalId.Add(addition, removal.Id);
                                wasRenamed = true;
                                break;
                            }
                        }
                    }

                    if (!wasRenamed)
                    {
                        // insert a new element
                        modifications.Add(new InsertElementModification(Id, addition, lastNodeId));
                    }
                }
            }

            // add removal modifications for any elements that were removed
            for (int i = 0; i < mappedArray.Length; i++)
            {
                if (mappedArray[i] == null)
                {
                    // remove the element
                    var removal = Elements[i];
                    var diff = removal.Diff(getChildXPath(removal), null);
                    modifications.AddRange(diff.Modifications);
                    mappedArray[i] = diff.Selector;
                    modifications.Add(new RemoveElementModification(removal.Id));
                }
            }

            mappedSelectors = mappedArray.ToList();
            return true;
        }

        private bool TryGetKeyAttribute(IEnumerable<TaggedXmlElement> elements, out string attributeName)
        {
            attributeName = null;

            // we assume that the first attribute of an element is the key
            var firstAttributeForEachElement = elements.Select(e => e.Attributes.FirstOrDefault());

            // get the name of the attribute
            var keyAttribute = firstAttributeForEachElement.FirstOrDefault().Key;
            if (string.IsNullOrWhiteSpace(keyAttribute))
            {
                return false;
            }

            // verify that all elements have the same key attribute
            if (firstAttributeForEachElement.Any(a => a.Key != keyAttribute))
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

        private IEnumerable<XmlModification> CompareAttributes(XmlElement modified)
        {
            var mods = new List<XmlModification>();

            // get original and modified attributes
            var originalAttributes = new Dictionary<string, string>(Attributes);
            var modifiedAttributes = modified.Attributes.Cast<XmlAttribute>().GroupBy(a => a.Name)
                .ToDictionary(a => a.Key, a => a.First().Value);

            // compare attributes
            foreach (var attribute in modifiedAttributes)
            {
                if (originalAttributes.TryGetValue(attribute.Key, out var oldValue))
                {
                    if (attribute.Value != oldValue)
                    {
                        // attribute value has changed
                        mods.Add(new ModifyAttributeModification(Id, attribute.Key, attribute.Value));
                    }

                    // remove from the original list so we can identify which attributes were removed
                    originalAttributes.Remove(attribute.Key);
                }
                else
                {
                    // attribute has been added
                    mods.Add(new AddAttributeModification(Id, attribute.Key, attribute.Value));
                }
            }

            // the remaining original attributes have been removed
            foreach (var attribute in originalAttributes)
            {
                // attribute was removed
                mods.Add(new RemoveAttributeModification(Id, attribute.Key));
            }

            return mods;
        }

        public void ApplyModification(XmlModification mod)
        {
            mod.Apply(element);
        }
    }
}