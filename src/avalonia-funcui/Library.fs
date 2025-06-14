namespace avalonia_funcui

open System.Management.Automation
open System.Runtime.InteropServices

open Avalonia
open Avalonia.Controls
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI.DSL
open Elmish

module Main =
    type State = { message: string }

    let init () = { message = "Hello, World!" }, Cmd.none

    type Msg =
        | Send of string
        | NoOp

    let update (msg: Msg) (state: State) : State * Cmd<_> =
        match msg with
        | Send newMessage -> { state with message = newMessage }, Cmd.none
        | _ -> state, Cmd.none

    let view (state: State) (dispatch: Msg -> unit) =
        DockPanel.create [

            DockPanel.children [

                TextBlock.create [

                    TextBlock.text state.message
                    DockPanel.dock Dock.Top

                ]

                Button.create [

                    Button.content "Click Me"
                    Button.onClick (fun _ -> dispatch (Send "Button Clicked!"))
                    DockPanel.dock Dock.Bottom

                ]

            ]

        ]

type MainWindow() as this =
    inherit HostWindow()

    do
        base.Title <- "Example"
        base.Height <- 300.0
        base.Width <- 300.0

        Elmish.Program.mkProgram Main.init Main.update Main.view
        |> Program.withHost this
        |> Program.run

    override this.OnClosed(e: System.EventArgs) : unit = base.OnClosed(e: System.EventArgs)


type App() =
    inherit Application()

    member val mainWindow: MainWindow | null = null with get, set
    member val desktopLifetime: IClassicDesktopStyleApplicationLifetime | null = null with get, set

    override __.Initialize() =
        __.Styles.Add(FluentTheme())
        __.RequestedThemeVariant <- Styling.ThemeVariant.Dark
        printfn "Application initialized with FluentTheme and Dark variant."

    override __.OnFrameworkInitializationCompleted() =
        match __.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as (desktopLifetime: IClassicDesktopStyleApplicationLifetime) ->
            __.desktopLifetime <- desktopLifetime
            __.mainWindow <- new MainWindow()
            desktopLifetime.MainWindow <- __.mainWindow
            desktopLifetime.ShutdownMode <- ShutdownMode.OnMainWindowClose
            printfn "MainWindow set as the main window."
        | _ -> ()

open System
open System.Diagnostics
open Avalonia.Logging

[<Cmdlet(VerbsDiagnostic.Test, "AvaloniaFuncUI")>]
[<OutputType(typeof<PSObject>)>]
type SelectPocofCommand() =
    inherit PSCmdlet()

    static let app =
        printfn
            $"OSArchitecture: {RuntimeInformation.OSArchitecture} OSDescription: {RuntimeInformation.OSDescription} FrameworkDescription: {RuntimeInformation.FrameworkDescription} ProcessArchitecture: {RuntimeInformation.ProcessArchitecture} RuntimeIdentifier: {RuntimeInformation.RuntimeIdentifier}"

        printfn "EndProcessing called"

        let moduleDir =
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)

        printfn "Module directory: %s" moduleDir

        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            [ $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/av_libglesv2.dll"
              $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libHarfBuzzSharp.dll"
              $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libSkiaSharp.dll" ]
        elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            [ $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libAvaloniaNative.dylib"
              $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libHarfBuzzSharp.dylib"
              $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libSkiaSharp.dylib" ]
        elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            [ $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libHarfBuzzSharp.so"
              $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libSkiaSharp.so" ]
        else
            List.empty
        |> List.iter (fun skiaDll ->
            let skiaPath = System.IO.Path.Combine(moduleDir, skiaDll)

            try
                printfn "Loading SkiaSharp library from: %s" skiaPath

                if System.IO.File.Exists(skiaPath) then
                    printfn "SkiaSharp library found."
                    NativeLibrary.Load(skiaPath) |> ignore
            with e ->
                printfn "Failed to load SkiaSharp library: %s" e.Message
                ())

        let app =
            let lt =
                new Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime()

            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .LogToTextWriter(Console.Out, LogEventLevel.Verbose)
                .SetupWithLifetime(lt)
        // .SetupWithoutStarting()

        printfn "Avalonia FuncUI application configured."

        app

    override __.BeginProcessing() = printfn "BeginProcessing called."

    override __.ProcessRecord() = printfn "Hello from AvaloniaFuncUI"

    override __.EndProcessing() =

        printfn "Starting Avalonia FuncUI application..."

        let app = (app.Instance :?> App)
        app.mainWindow <- new MainWindow()
        app.desktopLifetime.MainWindow <- app.mainWindow
        app.desktopLifetime.ShutdownMode <- ShutdownMode.OnMainWindowClose

        let cts = new Threading.CancellationTokenSource()

        app.mainWindow.Closed.Add(fun _ ->
            printfn "MainWindow closed, shutting down application."
            cts.Cancel())

        app.mainWindow.Show()
        let ret = app.Run(cts.Token)
        printfn $"Avalonia FuncUI application started successfully. {ret}"

        // app.mainWindow.Close()
        cts.Cancel()

        Console.WriteLine("\n\n\n\n\n\n\n\n\n\n")
