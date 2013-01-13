﻿//  Copyright 2012 Gregor Uhlenheuer
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

namespace SharpXml.Tests

module DeserializationTests =

    open System
    open System.Collections
    open System.Collections.Generic
    open System.Collections.ObjectModel
    open System.Collections.Specialized
    open System.Diagnostics
    open NUnit.Framework

    open SharpXml
    open SharpXml.Tests.TestHelpers
    open SharpXml.Tests.Types

    let deserialize<'a> input =
        XmlSerializer.DeserializeFromString<'a>(input)

    [<Test>]
    let ``Can deserialize a simple class``() =
        let out = deserialize<TestClass> "<testClass><v1>42</v1><v2>bar</v2></testClass>"
        out.V1 |> should equal 42
        out.V2 |> should equal "bar"

    [<Test>]
    let ``Can deserialize string arrays``() =
        let out = deserialize<TestClass3> "<testClass><v1><item>foo</item><item>bar</item></v1><v2>42</v2></testClass>"
        out.V1.Length |> should equal 2
        out.V1 |> should equal [| "foo"; "bar" |]
        out.V2 |> should equal 42

    [<Test>]
    let ``Can deserialize integer arrays``() =
        let out = deserialize<GenericClass<int[]>> "<testClass><v1>201</v1><v2><item>1</item><item>2</item></v2></testClass>"
        out.V1 |> should equal 201
        out.V2.Length |> should equal 2
        out.V2 |> should equal [| 1; 2 |]

    [<Test>]
    let ``Can deserialize char arrays from strings``() =
        let out = deserialize<char[]> "<array>char</array>"
        out |> should equal [| 'c'; 'h'; 'a'; 'r' |]

    [<Test>]
    let ``Can deserialize byte arrays``() =
        let array = [| 99uy; 100uy; 101uy |]
        let bytes = Convert.ToBase64String(array)
        let out = deserialize<byte[]> (sprintf "<array>%s</array>" bytes)
        out |> should equal array

    [<Test>]
    let ``Can deserialize class arrays``() =
        let out = deserialize<TestClass4> "<testClass4><v1><item><v1>42</v1><v2>foo</v2></item><item><v1>200</v1><v2>bar</v2></item></v1><v2>99</v2></testClass4>"
        out.V1.Length |> should equal 2
        out.V1.[0].V1 |> should equal 42
        out.V1.[1].V2 |> should equal "bar"
        out.V2 |> should equal 99

    [<Test>]
    let ``Can deserialize class lists``() =
        let out = deserialize<ListClass> "<listClass><v1><item><v1>42</v1><v2>foo</v2></item><item><v1>200</v1><v2>bar</v2></item></v1><v2>99</v2></listClass>"
        out.V1.Count |> should equal 2
        out.V1.[0].V1 |> should equal 42
        out.V1.[1].V2 |> should equal "bar"
        out.V2 |> should equal 99

    [<Test>]
    let ``Can deserialize string-keyed dictionaries``() =
        let out = deserialize<DictClass> "<dictClass><v1><item><key>foo</key><value>100</value></item><item><key>bar</key><value>200</value></item></v1><v2>99</v2></dictClass>"
        out.V1.Count |> should equal 2
        out.V1.["foo"] |> should equal 100
        out.V1.["bar"] |> should equal 200
        out.V2 |> should equal 99

    [<Test>]
    let ``Can deserialize enums``() =
        let out = deserialize<EnumClass> "<enumClass><v1>Foo</v1><v2>99</v2></enumClass>"
        out.V1 |> should equal TestEnum.Foo
        out.V2 |> should equal 99

    [<Test>]
    let ``Can deserialize untyped ArrayLists``() =
        let out = deserialize<ArrayListClass> "<arrayListClass><v1>937</v1><v2><item>ham</item><item>eggs</item></v2></arrayListClass>"
        out.V1 |> should equal 937
        out.V2.Count |> should equal 2
        out.V2.[0] |> should equal "ham"
        out.V2.[1] |> should equal "eggs"

    [<Test>]
    let ``Can deserialize generic custom list types``() =
        let out = deserialize<CustomListClass> "<customListClass><v1>100</v1><v2><item>foo</item><item>bar</item></v2></customListClass>"
        out.V1 |> should equal 100
        out.V2 |> shouldBe notNull
        out.V2.Count |> should equal 2
        out.V2.[0] |> should equal "foo"
        out.V2.[1] |> should equal "bar"

    [<Test>]
    let ``Can deserialize generic classes with generic lists``() =
        let out = deserialize<GenericListClass<string>> "<genericListClass><v1>100</v1><v2><item>foo</item><item>bar</item></v2></genericListClass>"
        out.V1 |> should equal 100
        out.V2 |> shouldBe notNull
        out.V2.Count |> should equal 2
        out.V2.[0] |> should equal "foo"
        out.V2.[1] |> should equal "bar"

    [<Test>]
    let ``Can deserialize classes with a static ParseXml function``() =
        let out = deserialize<CustomParserClass> "<customParserClass>200x400</CustomParserClass>"
        out.X |> should equal 200
        out.Y |> should equal 400

    [<Test>]
    let ``Can deserialize classes with a list of ParseXml-like classes``() =
        let out = deserialize<GenericListClass<CustomParserClass>> "<genericListClass><v1>99</v1><v2><item>100x200</item><item>200x400</item></v2></genericListClass>"
        out.V1 |> should equal 99
        out.V2.Count |> should equal 2
        out.V2.[0].Y |> should equal 200
        out.V2.[1].X |> should equal 200

    [<Test>]
    let ``Can deserialize classes with string constructors``() =
        let out = deserialize<StringCtorClass> "<stringCtorClass>300x50</stringCtorClass>"
        out.X |> should equal 300
        out.Y |> should equal 50

    [<Test>]
    let ``Can deserialize HashSets``() =
        let out = deserialize<GenericClass<HashSet<string>>> "<genericClass><v1>100</v1><v2><item>foo</item><item>bar</item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        out.V2 |> Seq.head  |> should equal "foo"
        out.V2 |> Seq.nth 1  |> should equal "bar"

    [<Test>]
    let ``Can deserialize Queues``() =
        let out = deserialize<GenericClass<Queue<string>>> "<genericClass><v1>100</v1><v2><item>foo</item><item>bar</item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        out.V2 |> Seq.head |> should equal "foo"
        out.V2 |> Seq.nth 1 |> should equal "bar"

    [<Test>]
    let ``Can deserialize Stacks``() =
        let out = deserialize<GenericClass<Stack<string>>> "<genericClass><v1>100</v1><v2><item>foo</item><item>bar</item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        out.V2 |> Seq.head |> should equal "bar"
        out.V2 |> Seq.nth 1 |> should equal "foo"

    [<Test>]
    let ``Can deserialize NameValueCollections``() =
        let out = deserialize<GenericClass<NameValueCollection>> "<genericClass><v1>100</v1><v2><item><key>one</key><value>foo</value></item><item><key>two</key><value>bar</value></item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        out.V2.["one"] |> should equal "foo"
        out.V2.["two"] |> should equal "bar"

    [<Test>]
    let ``Can deserialize custom NameValueCollections``() =
        let out = deserialize<GenericClass<CustomNameValueCollection>> "<genericClass><v1>100</v1><v2><item><key>one</key><value>foo</value></item><item><key>two</key><value>bar</value></item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        out.V2.["one"] |> should equal "foo"
        out.V2.["two"] |> should equal "bar"

    [<Test>]
    let ``Can deserialize untyped hash tables``() =
        let out = deserialize<GenericClass<Hashtable>> "<genericClass><v1>100</v1><v2><item><key>one</key><value>foo</value></item><item><key>two</key><value>bar</value></item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        out.V2.["one"] |> should equal "foo"
        out.V2.["two"] |> should equal "bar"

    [<Test>]
    let ``Can deserialize linked lists``() =
        let out = deserialize<GenericClass<LinkedList<string>>> "<genericClass><v1>100</v1><v2><item>one</item><item>two</item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        Seq.head out.V2 |> should equal "one"
        Seq.nth 1 out.V2 |> should equal "two"

    [<Test>]
    let ``Can deserialize readonly collections``() =
        let out = deserialize<GenericClass<ReadOnlyCollection<string>>> "<genericClass><v1>100</v1><v2><item>one</item><item>two</item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        Seq.head out.V2 |> should equal "one"
        Seq.nth 1 out.V2 |> should equal "two"

    [<Test>]
    let ``Can deserialize arrays of classes``() =
        let out = deserialize<GenericClass<Guest[]>> "<genericClass><v1>984</v1><v2><guest><firstName>ham</firstName></guest><guest><firstName>foo</firstName><lastName>bar</lastName><id>2</id></guest></v2></genericClass>"
        out.V1 |> should equal 984
        out.V2.Length |> should equal 2
        out.V2.[0].Id |> should equal 0
        out.V2.[0].FirstName |> should equal "ham"
        out.V2.[1].Id |> should equal 0
        out.V2.[1].FirstName |> should equal "foo"
        out.V2.[1].LastName |> should equal "bar"

    [<Test>]
    let ``Can deserialize sorted sets``() =
        let out = deserialize<GenericClass<SortedSet<string>>> "<genericClass><v1>100</v1><v2><item>one</item><item>two</item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        Seq.head out.V2 |> should equal "one"
        Seq.nth 1 out.V2 |> should equal "two"

    [<Test>]
    let ``Can deserialize string attributes with special chars``() =
        let out = deserialize<TestClass> "<testClass><v1 attr=\"http://url.com\">42</v1><v2>bar</v2></testClass>"
        out.V1 |> should equal 42
        out.V2 |> should equal "bar"

    [<Test>]
    let ``Can correctly skip single/empty fields``() =
        let out = deserialize<GenericClass<LinkedList<string>>> "<genericClass><v1 /><v2><item>one</item><item>two</item></v2></genericClass>"
        out.V1 |> should equal 0
        out.V2.Count |> should equal 2
        Seq.head out.V2 |> should equal "one"

    [<Test>]
    let ``Can deserialize immutable F# list``() =
        let out = deserialize<FSharpListClass> "<fSharpListClass><v1>4</v1><v2><item>one</item><item>two</item></v2></fSharpListClass>"
        out.V1 |> should equal 4
        out.V2.Length |> should equal 2
        out.V2 |> List.head |> should equal "one"

    [<Test>]
    let ``Can deserialize F# records``() =
        let out = deserialize<TestRecord> "<testRecord><value>842</value><name>foobar</name></testRecord>"
        out.Value |> should equal 842
        out.Name |> should equal "foobar"

    [<Test>]
    let ``Can deserialize F# records in random order``() =
        let out = deserialize<LargerRecord> "<largerRecord><value>842</value><id>foobar</id><bar>bar</bar><foo>foo</foo></largerRecord>"
        out.Value |> should equal 842
        out.Id |> should equal "foobar"
        out.Bar |> should equal "bar"
        out.Foo |> should equal "foo"

    [<Test>]
    let ``Can deserialize F# records with missing fields``() =
        let out = deserialize<LargerRecord> "<largerRecord><value>842</value><id>foobar</id></largerRecord>"
        out.Value |> should equal 842
        out.Id |> should equal "foobar"
        out.Bar |> shouldBe Null
        out.Foo |> shouldBe Null

    [<Test>]
    let ``Can deserialize classes with tuples``() =
        let out = deserialize<TupleClass> "<tupleClass><v1>53</v1><v2><item1>something</item1><item2>40</item2></v2></tupleClass>"
        out.V1 |> should equal 53
        out.V2.Item1 |> should equal "something"
        out.V2.Item2 |> should equal 40