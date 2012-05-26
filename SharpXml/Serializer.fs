﻿namespace SharpXml

/// Writer function delegate
type internal WriterFunc = System.IO.TextWriter -> obj -> unit

/// Record type containing the type specific information
/// for the first element to serialize
type internal TypeInfo = {
    Type : System.Type
    OriginalName : string
    ClsName : string }

type internal DictNames = {
    Item : string
    Key : string
    Value : string }

/// Record type containing the serialization information
/// for a specific property member
type internal PropertyWriterInfo = {
    Info : System.Reflection.PropertyInfo
    OriginalName : string
    ClsName : string
    GetFunc : Reflection.GetterFunc
    WriteFunc : Lazy<WriterFunc>
    Default : obj }

module internal SerializerBase =

    open System
    open System.IO

    /// General purpose XML tags writer function
    let writeTag (w : TextWriter) (name : string) writeFunc (value : obj) =
        w.Write("<{0}>", name)
        writeFunc w value
        w.Write("</{0}>", name)

/// Module containing the serialization logic
/// for value types
module internal ValueTypeSerializer =

    open System
    open System.Globalization
    open System.IO
    open System.Xml

    open SharpXml.Extensions

    let shortDateTimeFormat = "yyyy-MM-dd"
    let defaultFormat = "dd/MM/yyyy HH:mm:ss"
    let defaultFormatWithFraction = "dd/MM/yyyy HH:mm:ss.fff"
    let xsdFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ"
    let xsdFormat3F = "yyyy-MM-ddTHH:mm:ss.fffZ"
    let xsdFormatSeconds = "yyyy-MM-ddTHH:mm:ssZ"

    /// Convert the given DateTime into XSD format
    let toXsdFormat (date : DateTime) =
        // TODO: replace this with own logic
        XmlConvert.ToString(date.ToUniversal(), XmlDateTimeSerializationMode.Utc)

    /// Convert the given DateTime into the shortest possible XSD format
    let toShortestXsdFormat (date : DateTime) =
        let day = date.TimeOfDay
        if day.Ticks = 0L then date.ToString(shortDateTimeFormat)
        elif day.Milliseconds = 0 then date.ToUniversal().ToString(xsdFormatSeconds)
        else toXsdFormat date

    let writeString (writer : TextWriter) (content : string) =
        let inline sanitize c =
            match c with
            | '<' -> writer.Write("&lt;")
            | '>' -> writer.Write("&gt;")
            | _ -> writer.Write(c)
        Seq.iter sanitize content

    let inline nullableWriter writer value func =
        if value <> null then func writer value

    let writeStringObject writer (value : obj) =
        let v : string = unbox value
        writeString writer v

    let writeObject (writer : TextWriter) (value : obj) =
        writer.Write(value)

    let writeFloat2f (writer : TextWriter) (value : obj) =
        let v = sprintf "%.2f" (unbox value)
        writeString writer v

    let writeDateTime writer (value : obj) =
        let v = toShortestXsdFormat (unbox value)
        writeString writer v

    let writeNullableDateTime writer (value : obj) =
        nullableWriter writer value writeDateTime

    let writeDateTimeOffset writer (value : obj) =
        let v : DateTimeOffset = unbox value
        writeString writer (v.ToString("o"))

    let writeNullableDateTimeOffset writer (value : obj) =
        nullableWriter writer value writeDateTimeOffset

    let writeGuid writer (value : obj) =
        let v : Guid = unbox value
        writeString writer (v.ToString("N"))

    let writeNullableGuid writer (value : obj) =
        nullableWriter writer value writeGuid

    let writeChar (writer : TextWriter) (value : obj) =
        let v : char = unbox value
        writer.Write(v)

    let writeChars (writer : TextWriter) (value : obj) =
        let v : char[] = unbox value
        writer.Write(v)

    let writeByte (writer : TextWriter) (value : obj) =
        let v : byte = unbox value
        writer.Write(v)

    let writeSByte (writer : TextWriter) (value : obj) =
        let v : sbyte = unbox value
        writer.Write(v)

    let writeBytes (writer : TextWriter) (value : obj) =
        let v = Convert.ToBase64String(unbox value)
        writer.Write(v)

    let writeUInt16 (writer : TextWriter) (value : obj) =
        let v : uint16 = unbox value
        writer.Write(v)

    let writeInt16 (writer : TextWriter) (value : obj) =
        let v : int16 = unbox value
        writer.Write(v)

    let writeInt32 (writer : TextWriter) (value : obj) =
        let v : int = unbox value
        writer.Write(v)

    let writeUInt32 (writer : TextWriter) (value : obj) =
        let v : uint32 = unbox value
        writer.Write(v)

    let writeInt64 (writer : TextWriter) (value : obj) =
        let v : int64 = unbox value
        writer.Write(v)

    let writeUInt64 (writer : TextWriter) (value : obj) =
        let v : uint64 = unbox value
        writer.Write(v)

    let writeFloat (writer : TextWriter) (value : obj) =
        let v : float = unbox value
        writer.Write(v.ToString(CultureInfo.InvariantCulture))

    let writeFloat32 (writer : TextWriter) (value : obj) =
        let v : float32 = unbox value
        writer.Write(v.ToString(CultureInfo.InvariantCulture))

    let writeBool (writer : TextWriter) (value : obj) =
        let v : bool = unbox value
        match v with
        | true -> writer.Write("true")
        | false -> writer.Write("false")

    let writeDecimal writer (value : obj) =
        let v : decimal = unbox value
        writeString writer (v.ToString(CultureInfo.InvariantCulture))

    let writeEnum writer (value : obj) =
        writeObject writer value

    let writeEnumNames (writer : TextWriter) (value : obj) =
        let v : int = unbox value
        writer.Write(v)

    let writeType writer (value : obj) =
        let v : Type = unbox value
        writeString writer v.AssemblyQualifiedName

    let writeException writer (value : obj) =
        let v : Exception = unbox value
        writeString writer v.Message

    /// Get the appropriate writer function for the
    /// specified value type
    let getValueTypeWriter (t : Type) =
        if t = typeof<Nullable<DateTime>> then
            Some writeNullableDateTime
        elif t = typeof<Guid> then
            Some writeGuid
        elif t = typeof<Nullable<Guid>> then
            Some writeNullableGuid
        elif t = typeof<DateTimeOffset> then
            Some writeDateTimeOffset
        elif t = typeof<Nullable<DateTimeOffset>> then
            Some writeNullableDateTimeOffset
        elif t.IsEnum || t.UnderlyingSystemType.IsEnum then
            if t.HasAttribute("FlagsAttribute") then Some writeEnumNames else Some writeEnum
        else
            match Type.GetTypeCode(t.NullableUnderlying()) with
            | TypeCode.Boolean -> Some writeBool
            | TypeCode.Byte -> Some writeByte
            | TypeCode.Char -> Some writeChar
            | TypeCode.DateTime -> Some writeDateTime
            | TypeCode.Decimal -> Some writeDecimal
            | TypeCode.Double -> Some writeFloat
            | TypeCode.Int16 -> Some writeInt16
            | TypeCode.Int32 -> Some writeInt32
            | TypeCode.Int64 -> Some writeInt64
            | TypeCode.SByte -> Some writeSByte
            | TypeCode.Single -> Some writeFloat32
            | TypeCode.UInt16 -> Some writeUInt16
            | TypeCode.UInt32 -> Some writeUInt32
            | TypeCode.UInt64 -> Some writeUInt64
            | _ -> None

/// Serialization logic for list and collection types
module internal ListSerializer =

    open System
    open System.Collections
    open System.Collections.Generic
    open System.IO

    open SerializerBase

    /// Writer function for integer arrays
    let writeIntArray (writer : TextWriter) (value : obj) =
        let array : int [] = unbox value
        array
        |> Array.iter (fun elem -> writeTag writer "item" ValueTypeSerializer.writeInt32 elem)

    /// Writer function for string arrays
    let writeStrArray (writer : TextWriter) (value : obj) =
        let array : string [] = unbox value
        array
        |> Array.iter (fun elem -> writeTag writer "item" ValueTypeSerializer.writeStringObject elem)

    /// Writer function for untyped IEnumerables
    let writeEnumerable itemName determineFunc (writer : TextWriter) (value : obj) =
        let collection : IEnumerable = unbox value
        collection
        |> Seq.cast
        |> Seq.iter (fun elem ->
            let writeFunc = determineFunc (elem.GetType())
            writeTag writer itemName writeFunc elem)

    /// Writer function for generic IEnumerables
    let writeGenericEnumerable<'a> itemName (writeFunc : WriterFunc) (writer : TextWriter) (value : obj) =
        let collection : IEnumerable<'a> = unbox value
        collection
        |> Seq.iter (writeTag writer itemName writeFunc)

    /// Wrapper function to get the generic IEnumerable writer
    let getGenericEnumerableWriter itemName elemWriter t =
        // TODO: this does not look sane at all
        let writer = Type.GetType("SharpXml.ListSerializer").GetMethod("writeGenericEnumerable")
        let mtd = writer.MakeGenericMethod([| t |])
        fun (w : TextWriter) x -> mtd.Invoke(null, [| itemName; elemWriter; w; x |]) |> ignore

/// Serialization logic
module internal Serializer =

    open System
    open System.Collections
    open System.Collections.Generic
    open System.IO
    open System.Reflection
    open System.Text.RegularExpressions

    open SharpXml.Attempt
    open SharpXml.Common
    open SharpXml.Extensions
    open SharpXml.Utils
    open SerializerBase

    let propertyCache = ref (Dictionary<Type, PropertyWriterInfo[]>())
    let serializerCache = ref (Dictionary<Type, WriterFunc>())
    let typeInfoCache = ref (Dictionary<Type, TypeInfo>())

    /// Try to determine one of a special serialization
    /// function, i.e. Exception, Uri
    let getSpecialWriters (t : Type) = fun () ->
        if t = typeof<Uri> then Some ValueTypeSerializer.writeStringObject
        elif t = typeof<Exception> then Some ValueTypeSerializer.writeException
        elif t = typeof<Type> then Some ValueTypeSerializer.writeType
        else None

    let writerFuncName = "ToXml"
    let instanceFlags = BindingFlags.Public ||| BindingFlags.Instance
    let staticFlags = BindingFlags.Public ||| BindingFlags.Static

    let writeEmpty _ _ = ()

    let writeAbstractProperties (writer : TextWriter) (value : obj) =
        ()

    /// Determine the name of the TypeInfo based on the given type
    let getTypeName (t : Type) =
        let baseName = if t.IsArray then "Array" else removeGenericSuffix t.Name
        if XmlConfig.Instance.EmitCamelCaseNames then
            baseName.ToCamelCase()
        else
            baseName

    /// Build a TypeInfo object based on the given Type
    let buildTypeInfo t = {
        Type = t
        OriginalName = t.Name
        ClsName =
            match getAttribute<SharpXml.Common.XmlElementAttribute> t with
            | Some attr when not (empty attr.Name) -> attr.Name
            | _ -> getTypeName t }

    /// Get the TypeInfo object associated with the given Type
    let getTypeInfo (t : Type) =
        match (!typeInfoCache).TryGetValue t with
        | true, ti -> ti
        | _ -> Atom.updateAtomDict typeInfoCache t (buildTypeInfo t)

    /// Try to determine the string writer function
    let getStringWriter (t : Type) = fun () ->
        if t = typeof<string> then Some ValueTypeSerializer.writeStringObject else None

    /// Try to determine a matching value type writer function
    let getValueTypeWriter (t : Type) = fun () ->
        if t.IsValueType then ValueTypeSerializer.getValueTypeWriter t else None

    /// Try to determine a member function 'ToXml'
    let getInstanceWriter (t : Type) = fun() ->
        match t.GetMethod(writerFuncName, instanceFlags, null, Type.EmptyTypes, null) |> Utils.toOption with
        | Some func ->
            (fun (w : TextWriter) x -> w.Write(func.Invoke(x, null))) |> Some
        | _ -> None

    /// Try to determine a static function 'ToXml'
    let getStaticWriter (t : Type) = fun () ->
        match t.GetMethod(writerFuncName, staticFlags, null, [| t |], null) |> Utils.toOption with
        | Some func ->
            (fun (w : TextWriter) x -> w.Write(func.Invoke(null, [| x |]))) |> Some
        | _ -> None

    let getItemName (attribute : XmlElementAttribute option) =
        match attribute with
        | Some attr when not (empty attr.ItemName) -> attr.ItemName
        | _ -> "item"

    let defaultDictNames = { Item = "item"; Key = "key"; Value = "value" }

    let getDictNames (attribute : XmlElementAttribute option) =
        let get str fb = if not (empty str) then str else fb
        match attribute with
        | Some attr ->
            let item = get attr.ItemName "item"
            let key = get attr.KeyName "key"
            let value = get attr.ValueName "value"
            { Item = item; Key = key; Value = value }
        | _ -> defaultDictNames

    /// Build a PropertyWriterInfo object based on the
    /// specified PropertyInfo
    let rec buildPropertyWriterInfo (propInfo : PropertyInfo) =
        let cls =
            match getAttribute<XmlElementAttribute> propInfo with
            | Some attr when not (empty attr.Name) -> attr.Name
            | _ -> propInfo.Name.ToCamelCase()
        { Info = propInfo
          OriginalName = propInfo.Name
          ClsName = cls
          GetFunc = Reflection.getObjGetter propInfo
          WriteFunc = lazy getWriterFunc propInfo.PropertyType
          Default = Reflection.getDefaultValue propInfo.PropertyType }

    /// Try to determine a enumerable serialization function
    and getEnumerableWriter attr (t : Type) = fun () ->
        let itemName = getItemName attr
        match TypeHelper.getTypeWithGenericType t typedefof<IEnumerable<_>> with
        | Some enumerableType ->
            let elemType = enumerableType.GetGenericArguments().[0]
            let elemWriter = getWriterFunc elemType
            Some <| ListSerializer.getGenericEnumerableWriter itemName elemWriter elemType
        | _ ->
            if t.HasInterface typeof<IEnumerable> ||
                t.IsAssignableFrom typeof<IEnumerable> then
                Some <| ListSerializer.writeEnumerable itemName getWriterFunc
            else None

    /// Get the PropertyWriterInfo array for the given type
    and getProperties (t : Type) =
        match (!propertyCache).TryGetValue t with
        | true, props -> props
        | _ ->
            let props =
                Reflection.getSerializableProperties t
                |> Seq.filter (fun p -> p.GetIndexParameters().Length = 0)
                |> Seq.map buildPropertyWriterInfo
                |> Array.ofSeq
            Atom.updateAtomDict propertyCache t props

    /// Writer for classes and other reference types
    and writeClass props (writer : TextWriter) (value : obj) =
        props
        |> Array.iter (fun p ->
            let v = p.GetFunc.Invoke(value)
            if v <> null then
                let writeFunc = p.WriteFunc.Value
                writeTag writer p.ClsName writeFunc v)

    /// Try to determine a class or interface serialization function
    and getClassWriter (t : Type) = fun () ->
        if t.IsClass || t.IsInterface then
            if t.IsAbstract && not t.IsInterface then
                Some writeAbstractProperties
            else
                let properties = getProperties t
                Some (writeClass properties)
        else None

    /// Try to determine a writer function for a dictionary
    and getDictionaryWriter attr (t : Type) = fun () ->
        let names = getDictNames attr
        if t.HasInterface(typeof<IDictionary>) ||
            t.IsAssignableFrom(typeof<IDictionary>) then
            Some (writeDictionary names)
        else None

    /// Try to determine a writer function for array types
    and getArrayWriter attr (t : Type) = fun () ->
        let itemName = getItemName attr
        if t.IsArray then
            if t = typeof<byte[]> then Some ValueTypeSerializer.writeBytes
            elif t = typeof<char[]> then Some ValueTypeSerializer.writeChars
            elif t = typeof<int[]> then Some ListSerializer.writeIntArray
            elif t = typeof<string[]> then Some ListSerializer.writeStrArray
            // other array types will be handled by the IEnumerable writer
            else None
        else None

    /// Writer function for a dictionary
    and writeDictionary names (writer : TextWriter) (value : obj) =
        let map : IDictionary = unbox value
        let keyWriter : WriterFunc option ref = ref None
        let valueWriter : WriterFunc option ref = ref None
        map.Keys
        |> Seq.cast
        |> Seq.iter (fun key ->
            let dictVal = map.[key]
            if (!keyWriter).IsNone then keyWriter := Some <| getWriterFunc (key.GetType())
            if (!valueWriter).IsNone then valueWriter := Some <| getWriterFunc (dictVal.GetType())
            match !keyWriter, !valueWriter with
            | Some kv, Some vw ->
                writer.Write(sprintf "<%s>" names.Item)
                writeTag writer names.Key kv key
                writeTag writer names.Value vw dictVal
                writer.Write(sprintf "</%s>" names.Item)
            | _ -> ())

    and getGenericWriter (t : Type) = fun () ->
        // TODO: generic writer
        None

    /// Determine the associated serialization writer
    /// function for the specified type
    and determineWriter (t : Type) =
        let attr = getAttribute<XmlElementAttribute> t
        let writer = attempt {
            let! strWriter = getStringWriter t
            let! valueWriter = getValueTypeWriter t
            let! specialWriter = getSpecialWriters t
            let! arrayWriter = getArrayWriter attr t
            let! dictWriter = getDictionaryWriter attr t
            let! genericWriter = getGenericWriter t
            let! enumerableWriter = getEnumerableWriter attr t
            let! instanceWriter = getInstanceWriter t
            let! staticWriter = getStaticWriter t
            let! classWriter = getClassWriter t
            classWriter }
        writer

    /// Get the writer function to serialize the
    /// specified type
    and getWriterFunc (t : Type) =
        match (!serializerCache).TryGetValue t with
        | true, serializer -> serializer
        | _ ->
            match determineWriter t with
            | Some s -> Atom.updateAtomDict serializerCache t s
            | _ ->
                let err = sprintf "could not determine serialization logic for type '%s'" t.FullName
                invalidOp err

    /// Write the given type using the appropriate serialization logic
    let writeType<'a> (writer : TextWriter) (element : 'a) =
        let tInfo = getTypeInfo typeof<'a>
        writeTag writer tInfo.ClsName (getWriterFunc typeof<'a>) element
