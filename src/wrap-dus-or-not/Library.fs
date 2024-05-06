namespace wrap_dus_or_not

open System.Collections
open System.Management.Automation

[<RequireQualifiedAccess>]
[<NoComparison>]
type Entry =
    | Obj of PSObject
    | Dict of DictionaryEntry

[<Cmdlet(VerbsData.Out, "ObjectWrappedDUs")>]
[<OutputType(typeof<PSObject>)>]
type OutObjectWrappedDUsCommand() =
    inherit PSCmdlet()

    let input: obj Generic.List = Generic.List()
    let props: string Generic.HashSet = Generic.HashSet()
    let types: System.Type Generic.HashSet = Generic.HashSet()
    let typeAndProps: Generic.Dictionary<System.Type,string seq> = Generic.Dictionary()

    let add (io: PSObject) =
        match io.BaseObject with
        | :? IDictionary as dct ->
            for d in Seq.cast<DictionaryEntry> dct do
                d |> Entry.Dict |> input.Add
        | _ -> io |> Entry.Obj |> input.Add

    let addProps (io: PSObject) =
        match io.BaseObject with
        | :? IDictionary as dct ->
            let d = Seq.cast<DictionaryEntry> dct |> Seq.head
            d |> PSObject.AsPSObject |> _.Properties |> Seq.iter (fun p -> p |> _.Name |> props.Add |> ignore)
        | _ ->
            for p in io.Properties do
                p |> _.Name |> props.Add |> ignore

    let addProps2 (io: PSObject) =
        match io.BaseObject with
        // | :? IDictionary as dct ->
        //     let d = Seq.cast<DictionaryEntry> dct |> Seq.head
        //     d |> PSObject.AsPSObject |> _.Properties |> Seq.iter (fun p -> p |> _.Name |> props.Add |> ignore)
        | _ ->
            for p in io.Properties do
                if not (props.Contains p.Name) then
                    p.Name |> props.Add |> ignore

    let throughProps (io: PSObject) =
        match io.BaseObject with
        | _ ->
            for p in io.Properties do
                p.Name |> ignore

    let addTypes (io: PSObject) =
        io.BaseObject.GetType() |> types.Add |> ignore

    let addTypeAndProps (io: PSObject) =
        let t = io.BaseObject.GetType()
        if t |> typeAndProps.ContainsKey  |> not then
            let props = io.Properties |> Seq.map (fun p -> p.Name)
            typeAndProps.Add (t, props) |> ignore

    [<Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)>]
    member val InputObject: PSObject [] = [||] with get, set

    [<Parameter(Mandatory = true)>]
    [<ValidateSet(
        "Raw",
        "DUs",
        "Properties",
        "RawAndProperties",
        "DUsAndProperties","Properties2",
        "RawAndProperties2",
        "DUsAndProperties2",
        "Types",
        "DUsAndThroughProperties",
        "RawAndProperties3",
        "DUsAndProperties3"
    )>]
    [<ValidateNotNullOrEmpty>]
    member val Mode: string = null with get, set

    override __.BeginProcessing() = base.BeginProcessing()

    override __.ProcessRecord() =
        for io in __.InputObject do
            match __.Mode with
            | "Raw" ->
                io |> input.Add
            | "DUs" ->
                io |> add
            | "Properties" ->
                io |> addProps
            | "RawAndProperties" ->
                io |> input.Add
                io |> addProps
            | "DUsAndProperties" ->
                io |> add
                io |> addProps
            | "Properties2" ->
                io |> addProps2
            | "RawAndProperties2" ->
                io |> input.Add
                io |> addProps2
            | "DUsAndProperties2" ->
                io |> add
                io |> addProps2
            | "Types" ->
                io |> addTypes
            | "DUsAndThroughProperties" ->
                io |> add
                io |> throughProps
            | "RawAndProperties3" ->
                io |> input.Add
                io |> addTypeAndProps
            | "DUsAndProperties3" ->
                io |> add
                io |> addTypeAndProps

            | _ -> ()

    override __.EndProcessing() =
        if typeAndProps.Count > 0 then
            typeAndProps.Values |> Seq.collect id |> Seq.iter (props.Add >> ignore)

        // print memory usage.
        let template = printfn  "%30s %10s %10s: %20s"
        template __.Mode "Memory" "usage" <| System.GC.GetTotalMemory(true).ToString("#,##0")
        template __.Mode "Input" "count" <| input.Count.ToString("#,##0")
        template __.Mode "Properties" "count"<| props.Count.ToString("#,##0")
        template __.Mode "Types" "count"<| types.Count.ToString("#,##0")
        types |> Seq.map _.Name |> Seq.iter (template __.Mode "Types" "type")
        props |> Seq.map string |> Seq.iter (template __.Mode "Properties" "properties")
