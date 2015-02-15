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
            //Arrange
            string markup =
            @"<root>
                <element>content <subelement>text</subelement></element>
            </root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            //Assert
            Assert.AreEqual(1, root.Children.Count);
            Assert.AreEqual(2, root["element"].Children.Count);
            Assert.AreEqual("element", root.Children[0].Name);
            Assert.AreEqual("content text", root["element"].InnerText);
        }

        [TestMethod]
        public void TestCanParseAttribute()
        {
            //Arrange
            string markup =
            @"<root>
                <element attribute=""abc123='"" attribute2 ='abc123=""' attribute3= ""abc{0} 123="" />
            </root>";

            markup = string.Format(markup, "\t");

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            Assert.AreEqual("abc123='", root["element"].Attributes["attribute"]);
            Assert.AreEqual("abc123=\"", root["element"].Attributes["attribute2"]);
            Assert.AreEqual("abc\t 123=", root["element"].Attributes["attribute3"]);

            root = new QuasiXmlNode( new QuasiXmlParseSettings() { NormalizeAttributeValueWhitespaces = true });
            root.OuterMarkup = markup;

            Assert.AreEqual("abc 123=", root["element"].Attributes["attribute3"]);

            markup =
            @"<root>
                <element attribute=""attributedata"" attribute2="" attribute3=""attrubutedata3"" />
            </root>";

            //Act
            root = new QuasiXmlNode(new QuasiXmlParseSettings() { AbortOnError = false });
            root.OuterMarkup = markup;

            //Assert
            Assert.AreEqual("attribute3=", root["element"].Attributes["attribute2"]);
        }

        [TestMethod]
        public void TestCanParseSelfClosingTag()
        {
            //Arrange
            string markup =
            @"<root>
                <element attribute=""attributedata"" attribute2=""attributedata2"" />
            </root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            //Assert
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
            //Arrange
            string markup =
            @"<root attribute=""attributedata"" attribute2=""attributedata2"" />";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            //Assert
            Assert.AreEqual(0, root.Children.Count);
            Assert.AreEqual(QuasiXmlNodeType.Element, root.NodeType);
            Assert.AreEqual(2, root.Attributes.Count);
            Assert.AreEqual("attributedata", root.Attributes["attribute"]);
            Assert.AreEqual("attributedata2", root.Attributes["attribute2"]);
        }

        [TestMethod]
        public void TestCanParseMarkupWithComment()
        {
            //Arrange
            string markup =
            @"<root>
                <element attribute=""attributedata"" attribute2=""attributedata2"">
                    <!-- <commentelement attribute=""attributedata"" />commented out text -->live text
                    <subelement attribute=""attributedata"" />more live text
                </element>
            </root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            //Assert
            Assert.AreEqual(1, root.Children.Count);
            Assert.AreEqual(4, root["element"].Children.Count);
            Assert.AreEqual(QuasiXmlNodeType.Comment, root["element"].Children[0].NodeType);
            Assert.AreEqual(@" <commentelement attribute=""attributedata"" />commented out text ", root["element"].Children[0].Value);
        }

        [TestMethod]
        public void TestCanParseMarkupWithCDATA()
        {
            //Arrange
            string markup =
            @"<root>
                <element attribute=""elementattribute"" attribute2=""elementattribute2"">
                    <![CDATA[cdata content </>""']]>text
                    <subelement attribute=""elementattribute"" />
                </element>
            </root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            //Assert
            Assert.IsInstanceOfType(root.Children.Single(e => e.Name == "element"), new QuasiXmlNode().GetType());
            Assert.AreEqual(3, root["element"].Children.Count);
            Assert.AreEqual(QuasiXmlNodeType.CDATA, root["element"].Children[0].NodeType);
            Assert.AreEqual(QuasiXmlNodeType.Text, root["element"].Children[1].NodeType);
            Assert.AreEqual(QuasiXmlNodeType.Element, root["element"].Children[2].NodeType);
            Assert.AreEqual(@"cdata content </>""'", root["element"].Children[0].Value);
        }

        [TestMethod]
        [ExpectedException(typeof(QuasiXmlException), "Missing end tag.")]
        public void TestShouldThrowExeptionMissingEndTag()
        {
            //Arrange
            string markup =
            @"<root>
                <element attribute=""attributedata"" attribute2=""attributedata2"">
            </root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.ParseSettings.AbortOnError = true;
            root.OuterMarkup = markup;
        }

        [TestMethod]
        public void TestCanRecoverFromExeptionMissingEndTag()
        {
            //Arrange
            string markup =
            @"<root>
                <element attribute=""attributedata"" attribute2=""attributedata2"">
            </root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.ParseSettings.AbortOnError = false;
            root.OuterMarkup = markup;

            //Assert
            Assert.AreEqual(QuasiXmlNodeType.Element, root.NodeType);
            Assert.AreEqual(0, root.Children.Count);
        }
        
        [TestMethod]
        [ExpectedException(typeof(QuasiXmlException))]
        public void TestShouldThrowExeptionMissingOpenTagToClose()
        {
            //Arrange
            string markup =
            @"<root><one><two></two></two></one></root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.ParseSettings.AbortOnError = true;
            root.OuterMarkup = markup;
        }

        [TestMethod]
        public void TestCanRecoverFromExeptionMissingOpenTagToClose()
        {
            //Arrange
            string markup =
            @"<root><one><two></two></two></one></root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.ParseSettings.AbortOnError = false;
            root.OuterMarkup = markup;

            //Assert
            Assert.IsInstanceOfType(root, typeof(QuasiXmlNode));
        }

        [TestMethod]
        [ExpectedException(typeof(QuasiXmlException))]
        public void TestShouldThrowExeptionMissingEndToken()
        {
            //Arrange
            string markup =
            @"<root><one></one</root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.ParseSettings.AbortOnError = true;
            root.OuterMarkup = markup;
        }

        [TestMethod]
        public void TestCanRecoverFromExeptionMissingEndToken()
        {
            //Arrange
            string markup =
            @"<root><one></one</root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.ParseSettings.AbortOnError = false;
            root.OuterMarkup = markup;

            //Assert
            Assert.IsInstanceOfType(root, typeof(QuasiXmlNode));
            Assert.AreEqual(0, root.Children.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(QuasiXmlException))]
        public void TestShouldThrowExeptionMissingCDATAEndToken()
        {
            //Arrange
            string markup =
            @"<root><one><![CDATA[cdata content]]</one></root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.ParseSettings.AbortOnError = true;
            root.OuterMarkup = markup;
        }

        [TestMethod]
        public void TestCanRecoverFromExeptionMissingCDATAEndToken()
        {
            //Arrange
            string markup =
            @"<root><one><![CDATA[cdata content]]</one></root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.ParseSettings.AbortOnError = false;
            root.OuterMarkup = markup;

            //Assert
            Assert.IsInstanceOfType(root, typeof(QuasiXmlNode));
            Assert.AreEqual(1, root.Children.Count);
            Assert.AreEqual(1, root.Children[0].Children.Count);
            Assert.AreEqual(QuasiXmlNodeType.Text, root.Children[0].Children[0].NodeType);
        }

        [TestMethod]
        [ExpectedException(typeof(QuasiXmlException))]
        public void TestShouldThrowExeptionMissingCommentEndToken()
        {
            //Arrange
            string markup =
            @"<root><one><!-- this is a comment --</one></root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.ParseSettings.AbortOnError = true;
            root.OuterMarkup = markup;
        }

        [TestMethod]
        public void TestCanRecoverFromExeptionMissingCommentEndToken()
        {
            //Arrange
            string markup =
            @"<root><one><!-- this is a comment --</one></root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.ParseSettings.AbortOnError = false;
            root.OuterMarkup = markup;

            //Assert
            Assert.IsInstanceOfType(root, typeof(QuasiXmlNode));
            Assert.AreEqual(1, root.Children.Count);
            Assert.AreEqual(1, root.Children[0].Children.Count);
            Assert.AreEqual(QuasiXmlNodeType.Text, root.Children[0].Children[0].NodeType);
        }
    }
}
