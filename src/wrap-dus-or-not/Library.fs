namespace wrap_dus_or_not

open System.Collections
open System.Management.Automation

[<Cmdlet(VerbsData.Out, "ObjectWrappedDUs")>]
[<OutputType(typeof<PSObject>)>]
type OutObjectWrappedDUsCommand() =
    inherit PSCmdlet()

    let mutable input: obj Generic.List = Generic.List()

    [<Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)>]
    member val InputObject: PSObject [] = [||] with get, set

    override __.BeginProcessing() = base.BeginProcessing()

    override __.ProcessRecord() =
        for io in __.InputObject do
            input.Add(io)

    override __.EndProcessing() =
        // print memory usage.
        printfn "Memory usage: %d" (System.GC.GetTotalMemory(true))
