﻿namespace SharpXml.Tests

module SerializationTests =

    open System
    open System.Collections.Generic
    open NUnit.Framework

    open SharpXml
    open SharpXml.ValueTypeSerializer
    open SharpXml.Tests.TestHelpers
    open SharpXml.Tests.Types

    let serialize<'a> (element : 'a) =
        XmlSerializer.SerializeToString<'a>(element)

    [<Test>]
    let ``Can serialize DateTime values``() =
        let curr = DateTime.Now
        let date = curr.Date
        serialize date |> should equal (sprintf "<dateTime>%s</dateTime>" (date.ToString("yyyy-MM-dd")))

    [<Test>]
    let ``Can serialize floats``() =
        let value = 2.528
        serialize value |> should equal (sprintf "<double>%.3f</double>" value)

    [<Test>]
    let ``Can serialize simple classes without default constructors``() =
        let cls = TestClass(800, "foo bar")
        serialize cls |> should equal "<testClass><v1>800</v1><v2>foo bar</v2></testClass>"

    [<Test>]
    let ``Can serialize simple classes``() =
        let cls = SimpleClass(V1 = "foo bar", V2 = 800)
        serialize cls |> should equal "<simpleClass><v1>foo bar</v1><v2>800</v2></simpleClass>"

    [<Test>]
    let ``Can serialize dictionaries with string keys``() =
        let dict = Dictionary<string, int>()
        dict.Add("foo", 42)
        dict.Add("bar", 200)
        serialize dict |> should equal "<dictionary><item><key>foo</key><value>42</value></item><item><key>bar</key><value>200</value></item></dictionary>"

    [<Test>]
    let ``Can serialize dictionaries with integer keys``() =
        let dict = Dictionary<int, string>()
        dict.Add(42, "foo")
        dict.Add(200, "bar")
        serialize dict |> should equal "<dictionary><item><key>42</key><value>foo</value></item><item><key>200</key><value>bar</value></item></dictionary>"

    [<Test>]
    let ``Can serialize arrays``() =
        let array = [| 35; 200; 42 |]
        serialize array |> should equal "<array><item>35</item><item>200</item><item>42</item></array>"

    [<Test>]
    let ``Can serialize classes with nested classes as properties``() =
        let cls = NestedClass(V1 = "foobar", V2 = SimpleClass(V1 = "bar foo", V2 = 200))
        serialize cls |> should equal "<nestedClass><v1>foobar</v1><v2><v1>bar foo</v1><v2>200</v2></v2></nestedClass>"

    [<Test>]
    let ``Can serialize classes with nested classes and properties with null values``() =
        let cls = NestedClass2(V1 = "foobar", V2 = NestedClass2(V1 = "barfoo"))
        serialize cls |> should equal "<nestedClass2><v1>foobar</v1><v2><v1>barfoo</v1></v2></nestedClass2>"

    [<Test>]
    let ``Can serialize double nested classes``() =
        let cls = NestedClass2(V1 = "foobar", V2 = NestedClass2(V1 = "barfoo", V2 = NestedClass2(V1 = "ham eggs")))
        serialize cls |> should equal "<nestedClass2><v1>foobar</v1><v2><v1>barfoo</v1><v2><v1>ham eggs</v1></v2></v2></nestedClass2>"

    [<Test>]
    let ``Can serialize double nested classes containing properties with null values``() =
        let cls = NestedClass2(V1 = "foobar", V2 = NestedClass2(V1 = "barfoo", V2 = NestedClass2()))
        serialize cls |> should equal "<nestedClass2><v1>foobar</v1><v2><v1>barfoo</v1><v2></v2></v2></nestedClass2>"

    [<Test>]
    let ``Can serialize F# record types``() =
        let record = { Value = 99; Name = "ham & eggs" }
        serialize record |> should equal "<testRecord><value>99</value><name>ham & eggs</name></testRecord>"

    [<Test>]
    let ``Can serialize F# tuples``() =
        let tuple = 406, "foo bar test"
        serialize tuple |> should equal "<tuple><item1>406</item1><item2>foo bar test</item2></tuple>"

    [<Test>]
    let ``Can serialize newline characters``() =
        let special = "foo\r\nbar"
        let cls = TestClass(305, special)
        serialize cls |> should equal "<testClass><v1>305</v1><v2>foo\r\nbar</v2></testClass>"

    [<Test>]
    let ``Can serialize XML encoded characters``() =
        let special = "</v2>"
        let cls = TestClass(210, special)
        serialize cls |> should equal "<testClass><v1>210</v1><v2>&lt;/v2&gt;</v2></testClass>"

    [<Test>]
    let ``Can serialize non-printable characters``() =
        let chars = string [ for i in 10 .. 30 -> char i ]
        let cls = TestClass(999, chars)
        serialize cls |> should equal (sprintf "<testClass><v1>999</v1><v2>%s</v2></testClass>" chars)

    [<Test>]
    let ``Can serialize dictionaries``() =
        let dict = Dictionary<string, int>()
        dict.Add("foo", 1)
        dict.Add("bar", 2)
        let cls = DictClass(V1 = dict, V2 = 200)
        serialize cls |> should equal "<dictClass><v1><item><key>foo</key><value>1</value></item><item><key>bar</key><value>2</value></item></v1><v2>200</v2></dictClass>"

    [<Test>]
    let ``Can serialize IEnumerables``() =
        let cls = IEnumerableClass(V1 = "foo bar", V2 = List<int>(seq { 1 .. 2 }))
        serialize cls |> should equal "<iEnumerableClass><v1>foo bar</v1><v2><item>1</item><item>2</item></v2></iEnumerableClass>"

    [<Test>]
    let ``Can serialize class with custom ToString()``() =
        let cls = CustomParserClass(X = 200, Y = 400)
        serialize cls |> should equal "<customParserClass>200x400</customParserClass>"
