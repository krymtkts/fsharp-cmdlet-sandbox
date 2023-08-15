namespace prerelease_test_module

open System.Management.Automation

[<Cmdlet(VerbsDiagnostic.Test, "PreReleaseModule")>]
[<OutputType(typeof<PSObject>)>]
type SelectPocofCommand() =
    inherit PSCmdlet()

    override __.BeginProcessing() = base.BeginProcessing()

    override __.ProcessRecord() =
        printfn "Hello from prerelease-test-module"

    override __.EndProcessing() = base.EndProcessing()
