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
        public string Name { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        public string Value { get; set; }
        public QuasiXmlNode Parent { get; set; }
        public QuasiXmlNodeCollection Children { get; set; }
        public QuasiXmlNodeType NodeType { get; set; }
        public bool IsSelfClosing { get; set; }
        public QuasiXmlParseSettings ParseSettings { get; set; }
        public QuasiXmlRenderSettings RenderSettings { get; set; }

        public string InnerMarkup
        {
            // Returns a rendered string of child nodes
            get
            {
                return render(this, false);
            }
            // Parse incoming markup and replace child nodes with the results children
            set
            {
                QuasiXmlNode root = new QuasiXmlNode();
                root.parse("<root>" + value + "</root>");
                this.Children = root.Children;
            }
        }

        public string OuterMarkup
        {
            // Returns a rendered string of this node and its children
            get
            {
                return render(this, true);
            }
            // Parse incoming markup and replace this node with the result
            set
            {
                this.parse(value);
            }
        }

        public string InnerText
        {
            get
            {
                StringBuilder innerTextBuilder = new StringBuilder();

                foreach (QuasiXmlNode node in Children)
                {
                    if (node.NodeType == QuasiXmlNodeType.Text)
                        innerTextBuilder.Append(node.Value);
                    if (node.NodeType == QuasiXmlNodeType.Element)
                        innerTextBuilder.Append(node.InnerText);
                }

                return innerTextBuilder.ToString();
            }

            // Replaces all child nodes with an text node containing set value
            set
            {
                this.Children.Clear();
                this.Children.Add(new QuasiXmlNode() { NodeType = QuasiXmlNodeType.Text, Value = value });
            }
        }

        // Overloads the square brackets to provide array-like access
        public QuasiXmlNode this[string name]
        {
            get
            {
                if (this.Children.FirstOrDefault(n => n.Name == name) != null)
                    return this.Children.Single(c => c.Name == name);

                return null;
            }
            set
            {
                QuasiXmlNode node = this.Children.Single(c => c.Name == name);
                node = value;
            }
        }

        public QuasiXmlNode() 
        {
            Attributes = new Dictionary<string, string>();
            Children = new QuasiXmlNodeCollection();
            Children.CollectionChanged += this.OnChildrenChanged;
            ParseSettings = new QuasiXmlParseSettings() { NormalizeAttributeValueWhitespaces = false, AbortOnError = false, AutoCloseOpenTags = false };
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

        /// <exception cref="QuasiXmlException">A ProtoXmlParseException is thrown when the markup cannot be parsed.</exception>
        private void parse(string markup)
        {
            bool isRoot = true;
            QuasiXmlNode currentTag = null;
            List<Tuple<QuasiXmlNode, int>> openNodes = new List<Tuple<QuasiXmlNode, int>>(); //Node, End position of the nodes tag

            int lastSearchTagStartPosition = 0;
            int searchTagStartPosition = 0;
            int tagEndPosition = -1;
            int commentEndPosition = -1;
            int cdataEndPosition = -1;

            while (true)
            {
                int tagBeginPosition = markup.IndexOf("<", searchTagStartPosition);
                int commentBeginPosition = markup.IndexOf("<!--", searchTagStartPosition);
                int cdataBeginPosition = markup.IndexOf("<![CDATA[", searchTagStartPosition);

                if (cdataBeginPosition != -1)
                    cdataEndPosition = markup.IndexOf("]]>", cdataBeginPosition);

                if (commentBeginPosition != -1)
                    commentEndPosition = markup.IndexOf("-->", commentBeginPosition);

                bool tagIsInsideCommentBlock = tagBeginPosition >= commentBeginPosition && tagBeginPosition <= commentEndPosition;
                bool tagIsInsideCdataBlock = tagBeginPosition >= cdataBeginPosition && tagBeginPosition <= cdataEndPosition;

                try
                {
                    if (tagBeginPosition != -1)
                    {
                        //Retrieve last iterations nodes text by looking in the space between the last tags end and current tags start
                        string value = string.Empty;

                        if (isRoot == false)
                            value = markup.Substring(tagEndPosition + 1, (tagBeginPosition - 1) - tagEndPosition); //Do not include start and end tag in value
                        if (value.Trim() != string.Empty)
                            openNodes[openNodes.Count - 1].Item1.Children.Add(new QuasiXmlNode() { NodeType = QuasiXmlNodeType.Text, Name = null, Value = value });
                    }

                    if (tagBeginPosition != -1 && tagIsInsideCommentBlock == false && tagIsInsideCdataBlock == false)
                    {
                        tagEndPosition = markup.ToLower().IndexOf(">", tagBeginPosition);
                        bool isEndTag = markup.Substring(tagBeginPosition + 1, 1) == "/";
                        bool isSelfClosingTag = markup.Substring(tagBeginPosition, (tagEndPosition - tagBeginPosition) + 1).Replace(" ", string.Empty).Contains("/>");
                        searchTagStartPosition = tagEndPosition + 1;

                        if (isEndTag)
                        {
                            int initialNumberOfOpenTags = openNodes.Count;

                            //Remove the last occrance of the current node type:
                            for (int i = openNodes.Count; i > 0; i--)
                                if (openNodes[i - 1].Item1.Name == extractName(markup, tagBeginPosition + 1))
                                    openNodes.RemoveAt(i - 1);

                            if (initialNumberOfOpenTags == openNodes.Count)
                            {
                                int errorLine = getLineNumber(markup, tagBeginPosition);
                                throw new QuasiXmlException("Missing open '" + extractName(markup, tagBeginPosition + 1) + "' tag to close." + Environment.NewLine
                                    + "Source line number: " + errorLine, errorLine);
                            }

                            if (openNodes.Count > 0)
                                currentTag = openNodes[openNodes.Count - 1].Item1; //Current tag is the last open tag
                            else
                                currentTag = null;
                            continue;
                        }

                        if (isRoot == false)
                        {
                            currentTag.Children.Add(new QuasiXmlNode());
                            currentTag = currentTag.Children[currentTag.Children.Count - 1];
                        }
                        else
                        {
                            currentTag = this;
                            isRoot = false;
                        }

                        if (openNodes.Count > 0)
                            currentTag.Parent = openNodes.Last().Item1;
                        currentTag.NodeType = QuasiXmlNodeType.Element;
                        currentTag.Name = extractName(markup, tagBeginPosition);
                        currentTag.Attributes = extractAttributes(markup, tagBeginPosition, tagEndPosition);
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
                    else if (tagIsInsideCommentBlock)
                    {
                        string comment = markup.Substring(commentBeginPosition + "<!--".Length, commentEndPosition - (commentBeginPosition + "<!--".Length));

                        openNodes[openNodes.Count - 1].Item1.Children.Add(new QuasiXmlNode() { NodeType = QuasiXmlNodeType.Comment, Name = null, Value = comment });

                        searchTagStartPosition = commentEndPosition + "-->".Length - 1;
                        tagEndPosition = searchTagStartPosition;
                    }
                    else if (tagIsInsideCdataBlock)
                    {
                        string cdata = markup.Substring(cdataBeginPosition + "<![CDATA[".Length, cdataEndPosition - (cdataBeginPosition + "<![CDATA[".Length));

                        openNodes[openNodes.Count - 1].Item1.Children.Add(new QuasiXmlNode() { NodeType = QuasiXmlNodeType.CDATA, Name = null, Value = cdata });

                        searchTagStartPosition = cdataEndPosition + "]]>".Length - 1;
                        tagEndPosition = searchTagStartPosition;
                    }

                    if (lastSearchTagStartPosition == searchTagStartPosition)
                    {

                        int errorLine = getLineNumber(markup, tagBeginPosition);
                        throw new QuasiXmlException("Missing end token." + Environment.NewLine
                            + "Source line number: " + errorLine, errorLine);
                    }

                    lastSearchTagStartPosition = searchTagStartPosition;
                }
                catch (Exception)
                {
                    int errorLine = getLineNumber(markup, tagBeginPosition);
                    throw new QuasiXmlException("Parser error." + Environment.NewLine
                        + "Source line number: " + errorLine, errorLine);
                }
            }

            if (openNodes.Count > 0)
            {
                if (ParseSettings.AutoCloseOpenTags == false)
                    throw new QuasiXmlException("Missing end tag.");

                //TODO: auto close

            }
        }

        private string render(QuasiXmlNode node, bool includeRoot)
        {
            return render(node, includeRoot, 0);
        }

        private string render(QuasiXmlNode node, bool includeRoot, int level)
        {
            string currentLevelIndent = string.Empty;
            string lineEnd = string.Empty;

            if (RenderSettings.AutoIndentMarkup)
            {
                currentLevelIndent = currentLevelIndent.PadRight(RenderSettings.IndentNumberOfChars * level, RenderSettings.IndentCharacter);
                lineEnd = Environment.NewLine;
            }
            
            StringBuilder markupBuilder = new StringBuilder();

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

                        if (node.IsSelfClosing)
                        {
                            markupBuilder.Append(" />");
                            markupBuilder.Append(lineEnd);
                            _isLineIndented = false;
                        }
                        else
                        {
                            markupBuilder.Append('>');
                            markupBuilder.Append(lineEnd);
                            _isLineIndented = false;

                            foreach (QuasiXmlNode child in node.Children)
                                markupBuilder.Append(render(child, true, level + 1));

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
                            markupBuilder.Append(currentLevelIndent);
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
                        markupBuilder.Append("<![CDATA[");
                        markupBuilder.Append(node.Value);
                        markupBuilder.Append("]]>");
                        break;

                    case QuasiXmlNodeType.Comment:
                        if (!_isLineIndented)
                        {
          
                            markupBuilder.Append(currentLevelIndent);
                            _isLineIndented = true;
                        }
                        markupBuilder.Append("<!--");
                        markupBuilder.Append(node.Value);
                        markupBuilder.Append("-->");
                        break;
                }
            }
            else
            {
                foreach (QuasiXmlNode child in node.Children)
                    markupBuilder.Append(render(child, true, level + 1));
            }

            return markupBuilder.ToString();
        }

        public override string ToString()
        {
            return this.OuterMarkup;
        }

        private void OnChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (QuasiXmlNode newNode in e.NewItems)
                    newNode.Parent = this;
            }
        }

        private string extractName(string markup, int startTagBeginPosition)
        {
            int tagNameStartPosition = markup.IndexOf<char>((c => !char.IsWhiteSpace(c)), startTagBeginPosition + 1); //Find first non-whitespace char after startTagBeginPosition + 1's index
            int tagNameEndPosition = markup.IndexOf(" ", tagNameStartPosition) > 0 && markup.IndexOf(" ", tagNameStartPosition) < markup.IndexOf(">", startTagBeginPosition) 
                ? markup.IndexOf(" ", tagNameStartPosition) 
                : markup.IndexOf(">", startTagBeginPosition);

            return markup.Substring(tagNameStartPosition, tagNameEndPosition - tagNameStartPosition);
        }

        private Dictionary<string, string> extractAttributes(string markup, int startTagBeginPosition, int startTagEndPosition)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string startTagContent = markup.Substring(startTagBeginPosition, (startTagEndPosition - startTagBeginPosition) + 1);
            startTagContent = startTagContent.Trim().TrimStart('<').TrimEnd('>').TrimEnd('/').Trim();

            if (!startTagContent.Contains(' '))
                return result;

            startTagContent = startTagContent.Substring(startTagContent.Split(new char[] { ' ' })[0].Length).Trim(); //Get rid of the tag name

            List<string> attributeComponents = new List<string>();

            while (true)
            {
                int equalsCharPosition = startTagContent.IndexOf('=');
                if (equalsCharPosition == -1)
                    break;

                attributeComponents.Add(startTagContent.Substring(0, equalsCharPosition).TrimEnd()); //Add key

                startTagContent = startTagContent.Substring(equalsCharPosition + 1, (startTagContent.Length - equalsCharPosition) - 1).TrimStart(); //Cut 

                string valueWrapper = startTagContent[0].ToString();
                if (valueWrapper != '"'.ToString() && valueWrapper != "'")
                    break;

                int valueEndPostition = startTagContent.IndexOf(valueWrapper[0], 1);
                if (valueEndPostition == -1)
                    break;

                attributeComponents.Add(startTagContent.Substring(1, valueEndPostition - 1)); //Add value

                startTagContent = startTagContent.Substring(valueEndPostition + 1, startTagContent.Length - (valueEndPostition + 1)).TrimStart(); //Cut
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
                    string currentAttributeValue = attributeComponents[i + 1].Trim();
                    currentAttributeValue = Regex.Replace(currentAttributeValue, @"\s+", " ");
                    result.Add(attributeComponents[i].Trim().Trim('=').Trim(), currentAttributeValue);
                }
                else
                    result.Add(attributeComponents[i].Trim().Trim('=').Trim(), attributeComponents[i + 1].Trim());
            }

            return result;
        }

        private static int getLineNumber(string markup, int index)
        {
            int rows = 1;
            int currentIndex = 0;

            while (currentIndex < index)
            {
                int i = markup.IndexOf(Environment.NewLine, currentIndex, (index - currentIndex) + 1);

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
