﻿/*
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
namespace QuasiXml
{
    public class QuasiXmlParseSettings
    {
        public bool NormalizeAttributeValueWhitespaces { get; set; }
        public bool AutoCloseOpenTags { get; set; }
        public bool AbortOnError { get; set; }

        public QuasiXmlParseSettings()
        {
            NormalizeAttributeValueWhitespaces = false;
            AutoCloseOpenTags = false;
            AbortOnError = false;
        }

        public QuasiXmlParseSettings(bool normalizeAttributeValueWhitespaces, bool autoCloseOpenTags, bool abortOnError)
        {
            NormalizeAttributeValueWhitespaces = normalizeAttributeValueWhitespaces;
            AutoCloseOpenTags = autoCloseOpenTags;
            AbortOnError = abortOnError;
        }
    }
}
