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
    let props: obj Generic.HashSet = Generic.HashSet()

    let add (io: PSObject) =
        match io.BaseObject with
        | :? IDictionary as dct ->
            for d in Seq.cast<DictionaryEntry> dct do
                d |> Entry.Dict |> input.Add
        | _ -> io |> Entry.Obj |> input.Add

    let addProps (io: PSObject) =
        match io.BaseObject with
        | :? IDictionary as dct ->
            for d in Seq.cast<DictionaryEntry> dct do
                d |> PSObject.AsPSObject |> _.Properties |> Seq.iter (fun p -> p |> _.Name |> props.Add |> ignore)
        | _ ->
            for p in io.Properties do
                p |> _.Name |> props.Add |> ignore

    [<Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)>]
    member val InputObject: PSObject [] = [||] with get, set

    [<Parameter(Mandatory = true)>]
    [<ValidateSet("Raw", "DUs", "Properties", "RawAndProperties", "DUsAndProperties")>]
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
            | _ -> ()

    override __.EndProcessing() =
        // print memory usage.
        printfn "%20s Memory usage: %s" __.Mode <| System.GC.GetTotalMemory(true).ToString("#,##0")

// Raw          259,237,176
// DUs          283,093,752
// Properties    56,019,104
// Both       1,922,577,960