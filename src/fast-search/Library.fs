namespace ``fast-search``

open System
open System.Management.Automation
open System.Management.Automation.Runspaces

[<Cmdlet(VerbsCommon.Find, "ObjectWithPattern")>]
[<OutputType(typeof<PSObject>)>]
type FindObjectWithPatternCommand() =
    inherit PSCmdlet()

    [<Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)>]
    member val InputObject: PSObject [] = [||] with get, set

    override __.BeginProcessing() =
        printfn "==========Begin processing"

        printfn "==========Begin processing done"

    override __.ProcessRecord() =
        printfn "==========Add %A" __.InputObject

    override __.EndProcessing() =
        printfn "==========End processing"

        printfn "==========End processing done"
