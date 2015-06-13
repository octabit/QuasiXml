/*
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS HEADER.
 *
 * Copyright © 2015 Kevin Thomasson, thomassonkevin@gmail.com
 *
 * This file is part of QuasiXml.
 *
 * QuasiXml is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * QuasiXml is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with QuasiXml.  If not, see <http://www.gnu.org/licenses/>.
 *
 * License: GNU Lesser General Public License (LGPL)
 * Source code: https://quasixml.codeplex.com/
 */
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace QuasiXml
{
    public class QuasiXmlNode
    {
        private bool _isLineIndented = false;
        private const string XmlDeclaration = "?xml";
        private const string CdataStart = "<![CDATA[";
        private const string CdataEnd = "]]>";
        private const string CommentStart = "<!--";
        private const string CommentEnd = "-->";

        private class Tag
        {
            public string Name;
            public Dictionary<string, string> Attributes;
        }

        /// <summary>
        /// Gets or sets the name of this node.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets this nodes attributes.
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; }
        /// <summary>
        /// Gets or sets the value of this node.
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Gets or sets this nodes parent node.
        /// </summary>
        public QuasiXmlNode Parent { get; set; }
        /// <summary>
        /// Gets or sets this nodes child nodes.
        /// </summary>
        public QuasiXmlNodeCollection Children { get; set; }
        /// <summary>
        /// Gets or sets this nodes type.
        /// </summary>
        public QuasiXmlNodeType NodeType { get; set; }
        /// <summary>
        /// Determines whether or not this node renders as a self closing tag.
        /// </summary>
        public bool IsSelfClosing { get; set; }
        /// <summary>
        /// Gets or sets this nodes parser settings.
        /// </summary>
        public QuasiXmlParseSettings ParseSettings { get; set; }
        /// <summary>
        /// Gets or sets this nodes render settings.
        /// </summary>
        public QuasiXmlRenderSettings RenderSettings { get; set; }

        /// <summary>
        /// Gets a collection of all ascending nodes.
        /// </summary>
        public QuasiXmlNodeCollection Ascendants
        {
            get
            {
                if (Parent == null)
                    return null;

                var ascendants = new QuasiXmlNodeCollection();
                ascendants.Add(Parent);

                if(Parent.Ascendants != null)
                    ascendants.AddRange(Parent.Ascendants);

                return ascendants;
            }
        }

        /// <summary>
        /// Gets a collection of all decending nodes.
        /// </summary>
        public QuasiXmlNodeCollection Descendants
        {
            get
            {
                var descendants = new QuasiXmlNodeCollection();
                descendants.AddRange(Children);

                foreach (QuasiXmlNode node in Children)
                    descendants.AddRange(node.Descendants);

                return descendants;
            }
        }

        /// <summary>
        /// Gets a rendered string of child nodes. Set parses incoming markup and replaces this nodes children with the result of the parse operation.
        /// </summary>
        /// <exception cref="QuasiXmlException">A ProtoXmlParseException is thrown if the markup cannot be parsed.</exception>
        public string InnerMarkup
        {
            get
            {
                return Render(this, false);
            }
            set
            {
                var root = new QuasiXmlNode();
                root.Parse("<root>" + value + "</root>");
                this.Children = root.Children;
            }
        }

        /// <summary>
        /// Gets a rendered string of this node and its children. Set parses incoming markup and replaces this node with the result of the parse operation.
        /// </summary>
        /// <exception cref="QuasiXmlException">A ProtoXmlParseException is thrown if the markup cannot be parsed.</exception>
        public string OuterMarkup
        {
            get
            {
                return Render(this, true);
            }
            set
            {
                this.Parse(value);
            }
        }

        /// <summary>
        /// Gets the concatenated values of this node and all its children. Set replaces all child nodes with a text node containing the given value.
        /// </summary>
        public string InnerText
        {
            get
            {
                var innerTextBuilder = new StringBuilder();

                foreach (QuasiXmlNode node in Children)
                {
                    if (node.NodeType == QuasiXmlNodeType.Text)
                        innerTextBuilder.Append(node.Value);
                    if (node.NodeType == QuasiXmlNodeType.Element)
                        innerTextBuilder.Append(node.InnerText);
                }

                return innerTextBuilder.ToString();
            }
            set
            {
                this.Children.Clear();
                this.Children.Add(new QuasiXmlNode() { NodeType = QuasiXmlNodeType.Text, Value = value });
            }
        }

        //Overloads the square brackets to provide array-like access
        public QuasiXmlNode this[string name]
        {
            get
            {
                return this.Children.FirstOrDefault(c => c.Name.Equals(name, StringComparison.Ordinal));
            }
            set
            {
                QuasiXmlNode node = this.Children.Single(c => c.Name.Equals(name, StringComparison.Ordinal));
                node = value;
            }
        }

        public QuasiXmlNode() 
        {
            Attributes = new Dictionary<string, string>();
            Children = new QuasiXmlNodeCollection();
            Children.CollectionChanged += this.OnChildrenChanged;
            ParseSettings = new QuasiXmlParseSettings();
            RenderSettings = new QuasiXmlRenderSettings();
        }

        public QuasiXmlNode(QuasiXmlParseSettings parserSettings) : this()
        {
            ParseSettings = parserSettings;
        }

        public QuasiXmlNode(QuasiXmlRenderSettings renderSettings) : this()
        {
            RenderSettings = renderSettings;
        }

        public QuasiXmlNode(QuasiXmlParseSettings parserSettings, QuasiXmlRenderSettings renderSettings) : this()
        {
            ParseSettings = parserSettings;
            RenderSettings = renderSettings;
        }

        /// <exception cref="QuasiXmlException">A ProtoXmlParseException is thrown if the markup cannot be parsed.</exception>
        private void Parse(string markup)
        {
            bool isRoot = true;
            QuasiXmlNode currentTag = null;
            var openNodes = new List<Tuple<QuasiXmlNode, int>>(); //Node, End position of the nodes tag

            int lastSearchTagStartPosition = 0;
            int searchTagStartPosition = 0;
            int tagEndPosition = -1;
            int commentBeginPosition = -1;
            int cdataBeginPosition = -1;

            while (true)
            {
                int tagBeginPosition = markup.IndexOf('<', searchTagStartPosition);

                bool tagIsCommentStart = false;
                bool tagIsCdataStart = false;

                //Is the character following tag start equal to '!'?
                if (markup.Length - 1 > tagBeginPosition + 1 && markup[tagBeginPosition + 1] == '!')
                {
                    commentBeginPosition = markup.IndexOf(CommentStart, tagBeginPosition, StringComparison.Ordinal);
                    cdataBeginPosition = markup.IndexOf(CdataStart, tagBeginPosition, StringComparison.Ordinal);
                    tagIsCommentStart = tagBeginPosition == commentBeginPosition;
                    tagIsCdataStart = tagBeginPosition == cdataBeginPosition;
                }

                try
                {
                    if (tagBeginPosition != -1)
                    {
                        //Retrieve last iterations nodes text by looking in the space between the last tags end and current tags start
                        string value = string.Empty;

                        if ((tagBeginPosition - 1) - tagEndPosition >= 0)
                            value = markup.Substring(tagEndPosition + 1, (tagBeginPosition - 1) - tagEndPosition); //Do not include start and end tag in value
                        if (!string.IsNullOrWhiteSpace(value))
                            openNodes[openNodes.Count - 1].Item1.Children.Add(new QuasiXmlNode() { NodeType = QuasiXmlNodeType.Text, Name = null, Value = value, RenderSettings = openNodes.First().Item1.RenderSettings });
                    }

                    if (tagBeginPosition != -1 && tagIsCommentStart == false && tagIsCdataStart == false)
                    {
                        //Check if next tags start token is found before this tags end token
                        int nextTagStartTokenIndex = markup.IndexOf('<', tagBeginPosition + 1);
                        int nextEndTagTokenIndex = markup.IndexOf('>', tagBeginPosition);
                        if ((nextTagStartTokenIndex < nextEndTagTokenIndex) && tagEndPosition != -1 && nextTagStartTokenIndex != -1)
                        {
                            if (ParseSettings.AbortOnError)
                                throw new QuasiXmlException("Missing tag end token.", GetLineNumber(markup, tagBeginPosition));

                            searchTagStartPosition = nextTagStartTokenIndex - 1; //Ignore this tag by moving forward to right before the next tags start
                            continue;
                        }

                        tagEndPosition = nextEndTagTokenIndex; //TODO: Really seek within attribute values? Is '>' allowed in attribute values?

                        if (tagEndPosition == -1) //Should only occur if the last tag in the markup is missing an end token
                        {
                            if (ParseSettings.AbortOnError)
                                throw new QuasiXmlException("Missing tag end token.", GetLineNumber(markup, tagBeginPosition));

                            markup = markup + '>';
                            lastSearchTagStartPosition = tagBeginPosition - 1;
                        }

                        string tagString = markup.Substring(tagBeginPosition, (tagEndPosition - tagBeginPosition) + 1); //e.g. "<tag attribute='test'>"
                        bool isEndTag = markup[tagBeginPosition + 1] == '/'; //TODO: What if tag contains whitespace before  '/'
                        bool isSelfClosingTag = tagString.Replace(" ", string.Empty).EndsWith("/>", StringComparison.Ordinal);
                        searchTagStartPosition = tagEndPosition + 1;

                        Tag tag = ExtractTagParts(tagString);

                        if (isEndTag)
                        {
                            int initialNumberOfOpenTags = openNodes.Count;

                            //Remove the last occurance of the current node type:
                            for (int i = openNodes.Count; i > 0; i--)
                                if (openNodes[i - 1].Item1.Name.Equals(tag.Name, StringComparison.Ordinal))
                                    openNodes.RemoveAt(i - 1);

                            if (initialNumberOfOpenTags == openNodes.Count)
                                if (ParseSettings.AbortOnError)
                                    throw new QuasiXmlException("Missing open '" + tag.Name + "' tag to close.", GetLineNumber(markup, tagBeginPosition));

                            if (openNodes.Count > 0)
                                currentTag = openNodes[openNodes.Count - 1].Item1; //Current tag is the last open tag
                            else
                                currentTag = null;
                            continue;
                        }

                        if (isRoot == false)
                        {
                            currentTag.Children.Add(new QuasiXmlNode() { RenderSettings = openNodes.First().Item1.RenderSettings });
                            currentTag = currentTag.Children[currentTag.Children.Count - 1];
                        }
                        else
                        {
                            if (tag.Name.Equals(XmlDeclaration, StringComparison.OrdinalIgnoreCase))
                                continue;

                            currentTag = this;
                            isRoot = false;
                        }

                        if (openNodes.Count > 0)
                            currentTag.Parent = openNodes.Last().Item1;
                        currentTag.NodeType = QuasiXmlNodeType.Element;
                        currentTag.Name = tag.Name;
                        currentTag.Attributes = tag.Attributes;
                        currentTag.IsSelfClosing = isSelfClosingTag;

                        if (isSelfClosingTag == false)
                            openNodes.Add(new Tuple<QuasiXmlNode, int>(currentTag, tagEndPosition));
                        else
                        {
                            if (openNodes.Count > 0)
                                currentTag = openNodes[openNodes.Count - 1].Item1; //Current tag is the last open tag
                            else
                                currentTag = null;
                        }
                    }
                    else if (tagBeginPosition == -1)
                    {
                        break;
                    }
                    else if (tagIsCommentStart)
                    {
                        //Find comment end
                        int commentEndPosition = markup.IndexOf(CommentEnd, commentBeginPosition, StringComparison.Ordinal);
                        if (commentEndPosition == -1)
                        {
                            if (ParseSettings.AbortOnError)
                                throw new QuasiXmlException("Missing comment end token.", GetLineNumber(markup, commentBeginPosition));
                            else
                            {
                                searchTagStartPosition = commentBeginPosition + 1; //Recover by ignoring this comment start
                                tagEndPosition = searchTagStartPosition;
                                continue;
                            }
                        }

                        string comment = markup.Substring(commentBeginPosition + CommentStart.Length, commentEndPosition - (commentBeginPosition + CommentStart.Length));
                        openNodes[openNodes.Count - 1].Item1.Children.Add(new QuasiXmlNode() { NodeType = QuasiXmlNodeType.Comment, Name = null, Value = comment, RenderSettings = openNodes.First().Item1.RenderSettings });
                        searchTagStartPosition = commentEndPosition + CommentEnd.Length - 1;
                        tagEndPosition = searchTagStartPosition;
                    }
                    else if (tagIsCdataStart)
                    {
                        //Find CDATA end
                        int cdataEndPosition = markup.IndexOf(CdataEnd, cdataBeginPosition, StringComparison.Ordinal);
                        if (cdataEndPosition == -1)
                        {
                            if (ParseSettings.AbortOnError)
                                throw new QuasiXmlException("Missing CDATA end token.", GetLineNumber(markup, cdataBeginPosition));
                            else
                            {
                                searchTagStartPosition = cdataBeginPosition + 1; //Recover by ignoring this CDATA start
                                tagEndPosition = searchTagStartPosition;
                                continue;
                            }
                        }

                        string cdata = markup.Substring(cdataBeginPosition + CdataStart.Length, cdataEndPosition - (cdataBeginPosition + CdataStart.Length));
                        openNodes[openNodes.Count - 1].Item1.Children.Add(new QuasiXmlNode() { NodeType = QuasiXmlNodeType.CDATA, Name = null, Value = cdata, RenderSettings = openNodes.First().Item1.RenderSettings });
                        searchTagStartPosition = cdataEndPosition + CdataEnd.Length - 1;
                        tagEndPosition = searchTagStartPosition;
                    }

                    lastSearchTagStartPosition = searchTagStartPosition;
                }
                catch (QuasiXmlException e)
                {
                    throw e;
                }
                catch (Exception e)
                {
                    throw new QuasiXmlException("Parser error.", GetLineNumber(markup, tagBeginPosition), e);
                }
            }

            if (openNodes.Count > 0)
            {
               if (ParseSettings.AbortOnError == true)
                        throw new QuasiXmlException("Missing end tag.");

               if (ParseSettings.AutoCloseOpenTags == false)
                   foreach (Tuple<QuasiXmlNode, int> openNode in openNodes)
                       if (openNode.Item1.Parent != null)
                           openNode.Item1.Parent.Children.Remove(openNode.Item1);

               openNodes.Clear();
            }
        }

        private string Render(QuasiXmlNode node, bool includeRoot)
        {
            return Render(node, includeRoot, 0);
        }

        private string Render(QuasiXmlNode node, bool includeRoot, int level)
        {
            string currentLevelIndent = string.Empty;
            string lineEnd = string.Empty;

            if (RenderSettings.AutoIndentMarkup)
            {
                currentLevelIndent = currentLevelIndent.PadRight(RenderSettings.IndentNumberOfChars * level, RenderSettings.IndentCharacter);
                lineEnd = Environment.NewLine;
            }
            
            var markupBuilder = new StringBuilder();

            if (includeRoot)
            {
                switch (node.NodeType)
                {
                    case QuasiXmlNodeType.Element:
                        if (_isLineIndented)
                        {
                            markupBuilder.Append(lineEnd);
                            markupBuilder.Append(currentLevelIndent);
                        }
                        else
                        { 
                            markupBuilder.Append(currentLevelIndent);
                           _isLineIndented = true;
                        }
                        markupBuilder.Append('<');
                        markupBuilder.Append(node.Name);
                        foreach (KeyValuePair<string, string> attribute in node.Attributes)
                        {
                            markupBuilder.Append(' ');
                            markupBuilder.Append(attribute.Key);
                            markupBuilder.Append('=');
                            markupBuilder.Append('"');
                            markupBuilder.Append(attribute.Value);
                            markupBuilder.Append('"');
                        }

                        if (node.IsSelfClosing 
                            || (this.RenderSettings.RenderEmptyElementsAsSelfClosing 
                                && (!node.Children.Any() || (node.Children.Any() 
                                && node.Children.First().NodeType == QuasiXmlNodeType.Text 
                                && node.Children.First().Value == null))))
                        {
                            markupBuilder.Append(" />");
                            markupBuilder.Append(lineEnd);
                            _isLineIndented = false;
                        }
                        else
                        {
                            markupBuilder.Append('>');
                            if (!(node.Children.FirstOrDefault(n => n.NodeType == QuasiXmlNodeType.Text) != null
                                && (node.Children.First().Value == null
                                || node.Children.First().Value.StartsWith(lineEnd, StringComparison.Ordinal))))
                            {
                                markupBuilder.Append(lineEnd);
                            }
                            _isLineIndented = false;

                            foreach (QuasiXmlNode child in node.Children)
                                markupBuilder.Append(Render(child, true, level + 1));

                            if (_isLineIndented)
                            {
                                markupBuilder.Append(lineEnd);
                                markupBuilder.Append(currentLevelIndent);
                            }
                            else
                            {
                                markupBuilder.Append(currentLevelIndent);
                                _isLineIndented = true;
                            }
                            markupBuilder.Append("</");
                            markupBuilder.Append(node.Name);
                            markupBuilder.Append('>');
                            markupBuilder.Append(lineEnd);
                            _isLineIndented = false;
                        }
                        break;

                    case QuasiXmlNodeType.Text:
                        if(!_isLineIndented)
                        {
                            if (node.Value == null || !node.Value.StartsWith(lineEnd + currentLevelIndent, StringComparison.Ordinal))
                                markupBuilder.Append(currentLevelIndent);
                            else
                                if (node.RenderSettings.AutoIndentMarkup && node.Value != null)
                                    node.Value = node.Value.TrimEnd(lineEnd.ToCharArray().Concat(currentLevelIndent.ToCharArray()).ToArray());

                            _isLineIndented = true;
                        }
                        markupBuilder.Append(node.Value);
                        break;

                    case QuasiXmlNodeType.CDATA:
                        if (!_isLineIndented)
                        {
                            markupBuilder.Append(currentLevelIndent);
                            _isLineIndented = true;
                        }
                        markupBuilder.Append(CdataStart);
                        markupBuilder.Append(node.Value);
                        markupBuilder.Append(CdataEnd);
                        break;

                    case QuasiXmlNodeType.Comment:
                        if (!_isLineIndented)
                        {
                            markupBuilder.Append(currentLevelIndent);
                            _isLineIndented = true;
                        }
                        markupBuilder.Append(CommentStart);
                        markupBuilder.Append(node.Value);
                        markupBuilder.Append(CommentEnd);
                        break;
                }
            }
            else
            {
                foreach (QuasiXmlNode child in node.Children)
                    markupBuilder.Append(Render(child, true, level + 1));
            }

            if (level == 0)
                return markupBuilder.ToString().TrimEnd();

            return markupBuilder.ToString();
        }

        public override string ToString()
        {
            switch (this.NodeType)
            {
                case QuasiXmlNodeType.Text:
                    return "Text, Value=\"" + this.Value + "\"";
                case QuasiXmlNodeType.CDATA:
                    return "CDATA, Value=\"" + this.Value + "\"";
                case QuasiXmlNodeType.Comment:
                    return "Comment, Value=\"" + this.Value + "\"";
                default:
                    return "Element, Name=\"" + this.Name + "\"";
            }
        }

        private void OnChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (QuasiXmlNode newNode in e.NewItems)
                    newNode.Parent = this;
        }

        private Tag ExtractTagParts(string tag)
        {
            int tagNameStartPosition = -1;
            int tagNameEndPosition = -1;
            int attributesStartPosition = -1;
            int attributesEndPosition = -1;

            //Tag name start is first non-whitespace char after startTagBeginPosition + 1's index
            for (int i = 1; i < tag.Length; i++)
                if (!char.IsWhiteSpace(tag[i]) && tag[i] != '/')
                {
                    tagNameStartPosition = i;
                    break;
                }

            for (int i = tagNameStartPosition; i < tag.Length; i++)
            {
                if (char.IsWhiteSpace(tag[i]) && tagNameEndPosition == -1)
                {
                    tagNameEndPosition = i;
                    attributesStartPosition = i + 1;
                }
                if ((tag[i] == '>'))
                {
                    if(tagNameEndPosition == -1)
                        tagNameEndPosition = i;

                    attributesEndPosition = i;
                    break;
                }
            }

            return new Tag() 
                { 
                    Name = tag.Substring(tagNameStartPosition, tagNameEndPosition - tagNameStartPosition),
                    Attributes = attributesStartPosition != -1 ? ExtractAttributes(tag.Substring(attributesStartPosition, attributesEndPosition - attributesStartPosition)) : new Dictionary<string, string>()
                };
        }

        private Dictionary<string, string> ExtractAttributes(string attributes)
        {
            var result = new Dictionary<string, string>();
            var attributeComponents = new List<string>();

            while (true)
            {
                int equalsCharPosition = attributes.IndexOf('=');
                if (equalsCharPosition == -1)
                    break;

                attributeComponents.Add(attributes.Substring(0, equalsCharPosition).TrimEnd()); //Add key

                attributes = attributes.Substring(equalsCharPosition + 1, (attributes.Length - equalsCharPosition) - 1).TrimStart(); //Cut 

                char valueWrapper = attributes[0];
                if (valueWrapper != '"' && valueWrapper != '\'')
                    break;

                int valueEndPostition = attributes.IndexOf(valueWrapper, 1);
                if (valueEndPostition == -1)
                    break;

                attributeComponents.Add(attributes.Substring(1, valueEndPostition - 1)); //Add value

                attributes = attributes.Substring(valueEndPostition + 1, attributes.Length - (valueEndPostition + 1)).TrimStart(); //Cut
            }

            if (attributeComponents.Count % 2 != 0)
            {
                if (ParseSettings.AbortOnError)
                    return result;

                attributeComponents.RemoveAt(attributeComponents.Count - 1);
            }

            for (int i = 0; i < attributeComponents.Count; i = i + 2)
            {
                if (ParseSettings.NormalizeAttributeValueWhitespaces)
                {
                    string currentAttributeValue = attributeComponents[i + 1];
                    currentAttributeValue = Regex.Replace(currentAttributeValue, @"\s+", " ", RegexOptions.Compiled);
                    result.Add(attributeComponents[i].Trim(new char[] { ' ', '\t', '=' }), currentAttributeValue.Trim());
                }
                else
                    result.Add(attributeComponents[i].Trim(new char[]{' ','\t', '='}), attributeComponents[i + 1]);
            }

            return result;
        }

        private static int GetLineNumber(string markup, int index)
        {
            int rows = 1;
            int currentIndex = 0;

            while (currentIndex < index)
            {
                int i = markup.IndexOf(Environment.NewLine, currentIndex, (index - currentIndex) + 1, StringComparison.Ordinal);

                if (i != -1)
                {
                    rows++;
                    currentIndex = i + Environment.NewLine.Length;
                }
                else
                    break;
            }

            return rows;
        }
    }
}
