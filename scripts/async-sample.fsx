open System
open System.Threading
open System.Threading.Tasks

let printTimeCancellationTokenSource = new CancellationTokenSource()

let printTime () =
    let token = printTimeCancellationTokenSource.Token

    async {
        while not token.IsCancellationRequested do
            Console.WriteLine(DateTime.Now)
            do! Async.Sleep(1000)

        Console.WriteLine("Print time task cancelled")
    }

let readInputAsync () =
    async {
        let mutable input = ""

        while input <> "END" do
            input <- Console.ReadLine()
            printTimeCancellationTokenSource.Cancel()
            Console.WriteLine(input)
    }

let printTimeTask = printTime ()
let readInputTask = readInputAsync ()

[ printTimeTask; readInputTask ]
|> Async.Parallel
|> Async.RunSynchronously


0 // return an integer exit code
