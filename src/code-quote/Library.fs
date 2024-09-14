namespace code_quote

open System.Collections
open System.Management.Automation

[<RequireQualifiedAccess>]
[<NoComparison>]
type Entry =
    | Obj of PSObject
    | Dict of DictionaryEntry

[<Cmdlet(VerbsCommon.Select, "ObjectTest")>]
[<OutputType(typeof<PSObject>)>]
type SelectObjectTestCommand() =
    inherit PSCmdlet()

    [<Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)>]
    member val InputObject: PSObject [] = [||] with get, set

    override __.BeginProcessing() = ()

    override __.ProcessRecord() =
        for io in __.InputObject do
            io |> __.WriteObject

    override __.EndProcessing() = ()
