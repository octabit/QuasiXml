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
using System.Xml;
using QuasiXml;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QuasiXmlTest
{
    [TestClass]
    public class QuasiXmlParserTests
    {
        [TestMethod]
        public void TestCanParseElement()
        {
            string markup =
            @"<root>
                <element>content <subelement>text</subelement></element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            Assert.AreEqual(1, root.Children.Count);
            Assert.AreEqual(2, root["element"].Children.Count);
            Assert.AreEqual("element", root.Children[0].Name);
            Assert.AreEqual("content text", root["element"].InnerText);
        }

        [TestMethod]
        public void TestCanParseAttribute()
        {
            string markup =
            @"<root>
                <element attribute=""abc123='"" attribute2 ='abc123=""' attribute3= ""abc{0} 123=<"" />
            </root>";

            markup = string.Format(markup, "\t");

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            Assert.AreEqual("abc123='", root["element"].Attributes["attribute"]);
            Assert.AreEqual("abc123=\"", root["element"].Attributes["attribute2"]);
            Assert.AreEqual("abc\t 123=<", root["element"].Attributes["attribute3"]);

            root = new QuasiXmlNode( new QuasiXmlParseSettings() { NormalizeAttributeValueWhitespaces = true });
            root.OuterMarkup = markup;

            Assert.AreEqual("abc 123=<", root["element"].Attributes["attribute3"]);

            markup =
            @"<root>
                <element attribute=""attributedata"" attribute2="" attribute3=""attrubutedata3"" />
            </root>";

            root = new QuasiXmlNode(new QuasiXmlParseSettings() { AbortOnError = false });
            root.OuterMarkup = markup;

            Assert.AreEqual("attribute3=", root["element"].Attributes["attribute2"]);
        }

        [TestMethod]
        public void TestCanParseSelfClosingTag()
        {
            string markup =
            @"<root>
                <element attribute=""attributedata"" attribute2=""attributedata2"" />
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            Assert.IsInstanceOfType(root.Children.Single(e => e.Name == "element"), new QuasiXmlNode().GetType());


            Assert.AreEqual(1, root.Children.Count);
            Assert.AreEqual("element", root.Children[0].Name);
            Assert.AreEqual(QuasiXmlNodeType.Element, root["element"].NodeType);
            Assert.AreEqual(2, root["element"].Attributes.Count);
            Assert.AreEqual("attributedata", root["element"].Attributes["attribute"]);
            Assert.AreEqual("attributedata2", root["element"].Attributes["attribute2"]);
        }

        [TestMethod]
        public void TestCanParseSelfClosingTagWichIsAlsoRoot()
        {
            string markup =
            @"<root attribute=""attributedata"" attribute2=""attributedata2"" />";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            Assert.AreEqual(0, root.Children.Count);
            Assert.AreEqual(QuasiXmlNodeType.Element, root.NodeType);
            Assert.AreEqual(2, root.Attributes.Count);
            Assert.AreEqual("attributedata", root.Attributes["attribute"]);
            Assert.AreEqual("attributedata2", root.Attributes["attribute2"]);
        }

        [TestMethod]
        public void TestCanParseMarkupWithComment()
        {
            string markup =
            @"<root>
                <element attribute=""attributedata"" attribute2=""attributedata2"">
                    <!-- <commentelement attribute=""attributedata"" />commented out text -->live text
                    <subelement2 hej=""hå"" />more live text
                </element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            Assert.AreEqual(1, root.Children.Count);
            Assert.AreEqual(4, root["element"].Children.Count);
            Assert.AreEqual(QuasiXmlNodeType.Comment, root["element"].Children[0].NodeType);
            Assert.AreEqual(@" <commentelement attribute=""attributedata"" />commented out text ", root["element"].Children[0].Value);
        }



        [TestMethod]
        public void TestParseReturnsLeanTagWithOneSubTag()
        {
            string markup = 
            @"<root>
                < element attribute=""elementattribute"" attribute2=""elementattribute2"">
                    <subelement1 hej=""hå""  / >elementtext
                    <subelement2 hej=""hå""/>
                </element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

           Assert.IsInstanceOfType(root.Children.Single(e => e.Name == "element"), new QuasiXmlNode().GetType());
        }


        [TestMethod]
        public void TestParseMarkupWithCDATA()
        {
            string markup =
            @"<root>
                <element attribute=""elementattribute"" attribute2=""elementattribute2"">
                    <![CDATA[(innehåll i cdata)]]>t1
                    <subelement2 hej=""hå"" />
                </element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            Assert.IsInstanceOfType(root.Children.Single(e => e.Name == "element"), new QuasiXmlNode().GetType());
        }

        [TestMethod]
        public void TestParseMarkupWithCDATAContainingSpecialCharacters()
        {
            string markup =
            @"<root>
                <element attribute=""elementattribute"" attribute2=""elementattribute2"">
                    <![CDATA[(innehåll <i>da<ddds ffsd<"" -->cdata)]]>t1
                    <subelement2 hej=""hå"" />
                </element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            Assert.IsInstanceOfType(root.Children.Single(e => e.Name == "element"), new QuasiXmlNode().GetType());
        }


        [TestMethod]
        [ExpectedException(typeof(QuasiXmlException), "Missing end tag.")]
        public void TestParseShouldThrowExeptionMissingEndTag()
        {
            string markup =
            @"<root>
                <element attribute=""attributedata"" attribute2=""attributedata2"">
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;
        }
        
        [TestMethod]
        [ExpectedException(typeof(QuasiXmlException))]
        public void TestParseShouldThrowExeptionMissingOpenTagToClose()
        {
            string markup =
            @"<root><one><two></two></two></one></root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.ParseSettings.AbortOnError = true;
            root.OuterMarkup = markup;

            Assert.IsInstanceOfType(root, typeof(QuasiXmlNode));
        }

        [TestMethod]
        public void TestCanRecoverFromExeptionMissingOpenTagToClose()
        {
            string markup =
            @"<root><one><two></two></two></one></root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.ParseSettings.AbortOnError = false;
            root.OuterMarkup = markup;

            Assert.IsInstanceOfType(root, typeof(QuasiXmlNode));
        }
    }
}
