﻿namespace async_render

open System
open System.Management.Automation
open System.Reflection
open System.Threading
open System.Threading.Tasks

module Error =
    let stopUpstreamCommandsException (cmdlet: Cmdlet) =
        let stopUpstreamCommandsExceptionType =
            Assembly
                .GetAssembly(typeof<PSCmdlet>)
                .GetType("System.Management.Automation.StopUpstreamCommandsException")

        let stopUpstreamCommandsException =
            Activator.CreateInstance(
                stopUpstreamCommandsExceptionType,
                BindingFlags.Default
                ||| BindingFlags.CreateInstance
                ||| BindingFlags.Instance
                ||| BindingFlags.Public,
                null,
                [| cmdlet :> obj |],
                null
            )
            :?> Exception

        stopUpstreamCommandsException

type ListWithEvent<'T>() =
    inherit Collections.Generic.List<'T>()
    let event = new Event<'T>()

    member _.AddAndTrigger item =
        base.Add item
        event.Trigger item

    member _.ItemAdded = event.Publish

[<Cmdlet(VerbsData.Out, "GridAsync")>]
[<OutputType(typeof<PSObject>)>]
type OutGridAsyncCommand() =
    inherit PSCmdlet()

    let input = ListWithEvent<PSObject>()

    let printTimeCancellationTokenSource = new CancellationTokenSource()
    let token = printTimeCancellationTokenSource.Token

    let printTime (silent: bool) =
        let mutable i = 0

        // async {
        //     while not token.IsCancellationRequested do
        //         printfn "==========Print time task"

        //         for ip in input.GetRange(i, input.Count - i) do
        //             Console.WriteLine(ip)
        //             i <- i + 1

        //         do! Async.Sleep(1000)

        //     Console.WriteLine("Print time task cancelled")
        // }
        printfn "==========Set print time task"

        input.ItemAdded.Add (fun item ->
            if not silent then
                Console.WriteLine($"item => {item}")

            i <- i + 1)

    let readInputAsync () =
        async {
            let mutable input = ""

            while not token.IsCancellationRequested do
                printfn "==========While END"
                input <- Console.ReadLine()
                Console.WriteLine(input)

                if input = "END" then
                    printTimeCancellationTokenSource.Cancel()

            Console.WriteLine("While END done")
        }

    let mutable printTimeTask: Task<unit> option = None
    let mutable readInputTask: Task<unit> option = None

    [<Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)>]
    member val InputObject: PSObject [] = [||] with get, set

    [<Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)>]
    member val Silent: SwitchParameter = SwitchParameter false with get, set

    override __.BeginProcessing() =
        printfn "==========Begin processing"

        // printTimeTask <- printTime () |> Async.StartAsTask |> Some
        printTime (__.Silent.IsPresent)
        readInputTask <- readInputAsync () |> Async.StartAsTask |> Some

        printfn "==========Begin processing done"

    override __.ProcessRecord() =
        if not __.Silent.IsPresent then
            printfn "==========Processing record"

        for o in __.InputObject do
            if token.IsCancellationRequested then
                Console.WriteLine("Stop processing")

                __.EndProcessing()

                Error.stopUpstreamCommandsException (__) |> raise

            if not __.Silent.IsPresent then
                printfn "==========Add %A" o

            o |> input.AddAndTrigger

    override __.EndProcessing() =
        printfn "==========End processing"

        [ printTimeTask; readInputTask ]
        |> Seq.choose id
        |> Seq.cast<Task>
        |> Array.ofSeq
        |> Task.WaitAll

        printfn "==========End processing done"
