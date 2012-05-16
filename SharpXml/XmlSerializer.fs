﻿namespace SharpXml

open System
open System.Globalization
open System.IO
open System.Text

/// XML serializer
type XmlSerializer() =

    static let empty = String.IsNullOrWhiteSpace

    static member DeserializeFromString<'T> input : 'T =
        if empty input then Unchecked.defaultof<'T> else
            match Deserializer.determineReader typeof<'T> with
            | Some reader ->
                match XmlParser.parseAST input 0 with
                | [ xml ] -> reader xml :?> 'T
                | _ -> invalidArg "the input XML has no root element" "input"
            | _ -> Unchecked.defaultof<'T>

    static member SerializeToString<'T> (element : 'T) =
        let sb = StringBuilder()
        use writer = new StringWriter(sb, CultureInfo.InvariantCulture)
        if XmlConfig.Instance.WriteXmlHeader then writer.Write("<?xml version=\"1.0\" encoding=\"utf-8\"?>")
        Serializer.writeType writer element
        sb.ToString()

