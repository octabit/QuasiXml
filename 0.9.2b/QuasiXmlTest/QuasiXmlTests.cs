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
            string markup = @"<root>
                <element>
                    <subelement attribute=""attributedata"" />
                </element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            Assert.AreEqual(root["element"]["subelement"].Attributes["attribute"], "attributedata");
            Assert.AreEqual(root["element"]["subelement"]["nonexisting"], null);
        }

        [TestMethod]
        public void TestCanSetParentPropertyCorrectlyWhenModifyingTree()
        {
            string markup = @"<root>
                <element attribute=""attributedata"" attribute2=""attributedata2"">
                    <subelement attribute=""attributedata"" />
                </element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            QuasiXmlNode newNode = new QuasiXmlNode();
            newNode.OuterMarkup = @"<newnode >text</newnode>";
            root.Children.Add(newNode);

            Assert.AreEqual(root["element"], root["element"]["subelement"].Parent);
            Assert.AreEqual(root["element"].Parent, root["newnode"].Parent);
        }

        [TestMethod]
        public void TestCanSetInnerTextProperty()
        {
            string markup = @"<root>
                <element>This is a test
                    <subelement>text.</subelement>
                </element>
            </root>";

            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            root["element"].InnerText = "Hakkuna \t\nmatata";

            Assert.AreEqual("Hakkuna \t\nmatata", root["element"].InnerText);
            Assert.AreEqual(1, root["element"].Children.Count);
        }

        [TestMethod]
        public void TestSystemXmlCharacterEntities()
        {
            string markup = @"<root>
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

            var testNodes = root.Descendants.Where(node => node.Attributes.ContainsKey("test") && node.Attributes["test"] == "true").ToList();

            Assert.IsTrue(root.Descendants.Count(node => node.Name == "element") == 1);
            Assert.IsTrue(root.Descendants.Count(node => node.Name == "subelement") == 1);
            Assert.IsTrue(root.Descendants.Count(node => node.Name == "subsubelement") == 1);
            Assert.AreEqual("element", testNodes[0].Name);
            Assert.AreEqual("subsubelement", testNodes[1].Name);
        }
    }
}
