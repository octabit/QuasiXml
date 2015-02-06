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

namespace QuasiXml
{
    public class QuasiXmlException : Exception
    {
        public int LineNumber { get; private set; }

        public QuasiXmlException()
            : base() { }

        public QuasiXmlException(string message)
            : base(message) { }

        public QuasiXmlException(string message, Exception innerException)
            : base(message, innerException)  { }

        public QuasiXmlException(string message, int lineNumber)
            : base(message)
        {
            LineNumber = lineNumber;
        }

        public QuasiXmlException(string message, int lineNumber, Exception innerException)
            : base(message, innerException)
        {
            LineNumber = lineNumber;
        }
    }
}
