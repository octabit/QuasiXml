# QuasiXml
What is QuasiXml?

QuasiXml is a .NET library providing a XML-ish parser that  produces an object model similar to System.Xml. The parser does not care if the markup being parsed is well formed or not, it gives it a shot - but only if you want it to. 

Examples of what you can do with QuasiXml:

* Parse non-well-formed XML/XHTML into an object model
* Control aspects of the parsing (e.g. white space normalization and error handling)
* Insert character entities into the object model
* Render markup with or without indentation

QuasiXml <i>does not</i> support: 

* DTD
* XPATH
* XSLT

Other Features:

* No dependencies to the System.XML namespace or any third party libraries
* Light weight

Read more about QuasiXml on my blog:

http://blog.kevinthomasson.se/programming/quasixml-an-xmlish-parser-for-dotnet

<b>There is no documentation at this point. Consult the unit tests for help on usage until further notice.</b>

/Kevin Thomasson
