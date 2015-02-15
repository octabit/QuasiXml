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
    public class QuasiXmlRenderTests
    {
        [TestMethod]
        public void TestCanRenderSameOutputAsSystemXml()
        {
            //Arrange
            string markup = @"<root>
                <element attribute=""attributedata"" attribute2=""attributedata2"">
                    <!--<subelement attribute=""attributedata"" />text -->text2
                    <![CDATA[(innehåll i cdata)]]>text3
                    <subelement2 attribute=""attributedata"" />
                </element>
            </root>";

            //Act
            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;
            
            XmlDocument document = new XmlDocument();
            document.LoadXml(markup);

            //Assert
            Assert.AreEqual(document.InnerXml, root.OuterMarkup);
        }

        [TestMethod]
        public void TestCanRenderMarkupWithIndentation()
        {
            //Arrange
            string markup = "<root><element><![CDATA[character data]]>this is a text<!-- this is a comment --><subelement>more text</subelement></element></root>";
            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;
            root.RenderSettings = new QuasiXmlRenderSettings() { AutoIndentMarkup = true, IndentCharacter = ' ', IndentNumberOfChars = 4 };

            //Act
            var result = root.OuterMarkup;

            //Assert
            string control = "<root>" + Environment.NewLine +
                             "    " + "<element>" + Environment.NewLine +
                             "    " + "    " + "<![CDATA[character data]]>this is a text<!-- this is a comment -->" + Environment.NewLine +
                             "    " + "    " + "<subelement>" + Environment.NewLine +
                             "    " + "    " + "    " + "more text" + Environment.NewLine +
                             "    " + "    " + "</subelement>" + Environment.NewLine +
                             "    " + "</element>" + Environment.NewLine +
                             "</root>" + Environment.NewLine;

            Assert.AreEqual(control, result);
        }

        [TestMethod]
        public void TestCanRenderMarkupWithoutIndentation()
        {
            //Arrange
            string markup = "<root><element><![CDATA[character data]]>this is a text<!-- this is a comment --><subelement>more text</subelement></element></root>";
            QuasiXmlNode root = new QuasiXmlNode();
            root.OuterMarkup = markup;

            //Act
            var result = root.OuterMarkup;

            //Assert
            Assert.AreEqual(markup, result);
        }
    }
}
