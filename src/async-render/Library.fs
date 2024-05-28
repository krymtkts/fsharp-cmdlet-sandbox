namespace async_render

open System
open System.Management.Automation
open System.Reflection
open System.Threading
open System.Threading.Tasks
open System.Management.Automation.Runspaces

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

    // let input = ListWithEvent<PSObject>()
    let input = Collections.ObjectModel.ObservableCollection<PSObject>()

    let printTimeCancellationTokenSource = new CancellationTokenSource()
    let token = printTimeCancellationTokenSource.Token

    let printTime (silent: bool, render: PSObject -> unit) =
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

        input.CollectionChanged.Add (fun item ->
            match item.Action with
            | Collections.Specialized.NotifyCollectionChangedAction.Add ->
                if not silent then
                    let item = input.[i]
                    item |> render
                    printfn "==========printed via render"
            | _ -> ()

            i <- i + 1)

    let readInputAsync () =
        async {
            while not token.IsCancellationRequested do
                try
                    printfn "==========While END"
                    let item = Console.ReadLine()
                    item |> input.Add

                    if item = "END" then
                        printTimeCancellationTokenSource.Cancel()
                with
                | e -> Console.WriteLine($"BOOM! {e.Message}")

            Console.WriteLine("While END done")
        }

    let mutable printTimeTask: Task<unit> option = None
    let mutable readInputTask: Task<unit> option = None

    let originalRunspace = Runspace.DefaultRunspace

    [<Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)>]
    member val InputObject: PSObject [] = [||] with get, set

    [<Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)>]
    member val Silent: SwitchParameter = SwitchParameter false with get, set

    member __.Render(item: PSObject) =
        Runspace.DefaultRunspace <- originalRunspace

        __.InvokeCommand.InvokeScript(
            @"""input->$input""| Write-Host",
            true,
            PipelineResultTypes.None,
            [| item |],
            null
        )
        |> ignore

    override __.BeginProcessing() =
        __.InvokeCommand.InvokeScript(
            "'==========Begin processing' | Write-Host",
            true,
            PipelineResultTypes.Output,
            null,
            null
        )
        |> ignore

        // printTimeTask <- printTime () |> Async.StartAsTask |> Some
        printTime (__.Silent.IsPresent, __.Render)
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

            o |> input.Add

    override __.EndProcessing() =
        printfn "==========End processing"

        [ printTimeTask; readInputTask ]
        |> Seq.choose id
        |> Seq.cast<Task>
        |> Array.ofSeq
        |> Task.WaitAll

        printfn "==========End processing done"
