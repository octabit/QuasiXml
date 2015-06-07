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
    public class QuasiXmlTests
    {
        [TestMethod]
        public void TestCanAccessNodesWithIndexingAccessor()
        {
            //Arrange
            string markup = 
            @"<root>
                <element>
                    <subelement attribute=""attributedata"" />
                </element>
            </root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            //Assert
            Assert.AreEqual(root["element"]["subelement"].Attributes["attribute"], "attributedata");
            Assert.AreEqual(root["element"]["subelement"]["nonexisting"], null);
        }

        [TestMethod]
        public void TestCanSetParentPropertyCorrectlyWhenModifyingTree()
        {
            //Arrange
            string markup = 
            @"<root>
                <element attribute=""attributedata"" attribute2=""attributedata2"">
                    <subelement attribute=""attributedata"" />
                </element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            //Act
            QuasiXmlNode newNode = new QuasiXmlNode();
            newNode.OuterMarkup = @"<newnode >text</newnode>";
            root.Children.Add(newNode);

            //Assert
            Assert.AreEqual(root["element"], root["element"]["subelement"].Parent);
            Assert.AreEqual(root["element"].Parent, root["newnode"].Parent);
        }

        [TestMethod]
        public void TestCanSetInnerTextProperty()
        {
            //Arrange
            string markup = 
            @"<root>
                <element>This is a test
                    <subelement>text.</subelement>
                </element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            //Act
            root["element"].InnerText = "Hakkuna \t\nmatata";

            //Assert
            Assert.AreEqual("Hakkuna \t\nmatata", root["element"].InnerText);
            Assert.AreEqual(1, root["element"].Children.Count);
        }

        [TestMethod]
        public void TestSystemXmlCharacterEntities()
        {
            string markup = 
            @"<root>
                <element attribute=""data"">This is a text ... and I want to replace the string ""..."" with the XML character entity &amp;ellipsis; post parsing.</element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;
            root["element"].Children[0].Value = root["element"].Children[0].Value.Replace("...", "&hellip;");
            
            XmlDocument document = new XmlDocument();
            document.LoadXml(markup);
            document["root"]["element"].ChildNodes[0].Value = document["root"]["element"].ChildNodes[0].Value.Replace("...", "&hellip;");

            Assert.AreNotEqual(document.InnerXml, root.OuterMarkup);
        }

        [TestMethod]
        public void TestCanGetDescendants()
        {
            //Arrange
            string markup =
            @"<root>
                <element test=""true"">
                    <subelement>
                        <subsubelement test=""true"">text</subsubelement>
                    </subelement>
                </element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            //Act
            var testNodes = root.Descendants.Where(node => node.Attributes.ContainsKey("test") && node.Attributes["test"] == "true").ToList();

            //Assert
            Assert.IsTrue(root.Descendants.Count(node => node.Name == "element") == 1);
            Assert.IsTrue(root.Descendants.Count(node => node.Name == "subelement") == 1);
            Assert.IsTrue(root.Descendants.Count(node => node.Name == "subsubelement") == 1);
            Assert.AreEqual("element", testNodes[0].Name);
            Assert.AreEqual("subsubelement", testNodes[1].Name);
        }

        [TestMethod]
        public void TestCanGetAscendants()
        {
            //Arrange
            string markup =
            @"<root>
                <element test=""true"">
                    <subelement>
                        <subsubelement test=""true"">text</subsubelement>
                    </subelement>
                </element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            //Act
            var subsubelement = root.Descendants.Single(node => node.Name == "subsubelement");

            //Assert
            Assert.IsTrue(subsubelement.Ascendants.Count(node => node.Name == "subelement") == 1);
            Assert.IsTrue(subsubelement.Ascendants.Count(node => node.Name == "element") == 1);
            Assert.IsTrue(subsubelement.Ascendants.Count(node => node.Name == "root") == 1);
            Assert.AreEqual(3, subsubelement.Ascendants.Count);
        }

        [TestMethod]
        public void TestCanGetAllLinksInXHTML()
        {
            //Arrange
            string markup =
            @"<html>
                <body>
                    <article>
                        <p>
                           This is a test text, and <a href=""http://foo.bar"">this is a link</a>.
                        </p>
                    </article>
                    <footer>
                        <a href=""http://foo.bar/2"">Another link</a>
                    </footer>
                </body>
            </html>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            //Act
            var links = root.Descendants.Where(node => node.Name == "a" &&
                node.Attributes.ContainsKey("href")).ToList();

            //Assert
            Assert.AreEqual(2, links.Count);
            Assert.AreEqual("http://foo.bar", links[0].Attributes["href"]);
            Assert.AreEqual("http://foo.bar/2", links[1].Attributes["href"]);
        }
    }
}
