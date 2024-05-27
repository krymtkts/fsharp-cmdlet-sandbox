namespace async_render

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

[<Cmdlet(VerbsData.Out, "GridAsync")>]
[<OutputType(typeof<PSObject>)>]
type OutGridAsyncCommand() =
    inherit PSCmdlet()

    let input = Collections.Generic.List<PSObject>()

    let printTimeCancellationTokenSource = new CancellationTokenSource()
    let token = printTimeCancellationTokenSource.Token

    let printTime () =
        let mutable i = 0

        async {
            while not token.IsCancellationRequested do
                printfn "==========Print time task"

                for ip in input.GetRange(i, input.Count - i) do
                    Console.WriteLine(ip)
                    i <- i + 1

                do! Async.Sleep(1000)

            Console.WriteLine("Print time task cancelled")
        }

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

    override __.BeginProcessing() =
        printfn "==========Begin processing"

        printTimeTask <- printTime () |> Async.StartAsTask |> Some
        readInputTask <- readInputAsync () |> Async.StartAsTask |> Some

        printfn "==========Begin processing done"

    override __.ProcessRecord() =
        printfn "==========Processing record"

        for o in __.InputObject do
            if token.IsCancellationRequested then
                Console.WriteLine("Stop processing")
                __.EndProcessing()

                Error.stopUpstreamCommandsException (__) |> raise

            printfn "==========Add %A" o
            o |> input.Add

    override __.EndProcessing() =
        printfn "==========End processing"

        [ printTimeTask; readInputTask ]
        |> Seq.choose id
        |> Seq.cast<Task>
        |> Array.ofSeq
        |> Task.WaitAll

        printfn "==========End processing done"
