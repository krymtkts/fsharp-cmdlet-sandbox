namespace avalonia_funcui

open System.Management.Automation
open System
open System.Runtime.InteropServices

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts
open Avalonia.Logging
open Avalonia.Themes.Fluent
open Elmish

module AssemblyHelper =

    let resolver =
        DllImportResolver(fun libraryName assembly searchPath ->
            let moduleDir =
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)

            let skiaDll =
                System.IO.Path.Combine(
                    moduleDir,
                    $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/{libraryName}.dll"
                )


            if skiaDll |> IO.File.Exists then
                printfn "Loading SkiaSharp library from: %s" skiaDll

                NativeLibrary.TryLoad(skiaDll)
                |> function
                    | true, out ->
                        printfn "Successfully loaded library. Handle: %A" out
                        out
                    | _ ->
                        printfn "SkiaSharp library loaded successfully."
                        IntPtr.Zero
            else
                printfn "SkiaSharp library not found: %s" skiaDll
                IntPtr.Zero)


    do
        printfn "Preparing Avalonia assemblies..."
        NativeLibrary.SetDllImportResolver(typeof<SkiaSharp.SKImageInfo>.Assembly, resolver)
        NativeLibrary.SetDllImportResolver(typeof<HarfBuzzSharp.Blob>.Assembly, resolver)
        NativeLibrary.SetDllImportResolver(typeof<Avalonia.AppBuilder>.Assembly, resolver)
        printfn "assemblies prepared."

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
            // __.mainWindow <- new MainWindow()
            // desktopLifetime.MainWindow <- __.mainWindow
            // desktopLifetime.ShutdownMode <- ShutdownMode.OnMainWindowClose
            printfn "MainWindow set as the main window."
        | _ -> ()

[<Cmdlet(VerbsDiagnostic.Test, "AvaloniaFuncUI")>]
[<OutputType(typeof<PSObject>)>]
type SelectPocofCommand() =
    inherit PSCmdlet()

    static let app =
        printfn "\n\n\n\n\nConfiguring Avalonia FuncUI application...\n\n\n\n\n"
        // AssemblyHelper.prepare ()
        // printfn "prepared.\n\n\n\n\n"

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

        printfn "\n\n\n\n\nAvalonia FuncUI application configured.\n\n\n\n\n"

        app

    override __.BeginProcessing() = printfn "BeginProcessing called."

    override __.ProcessRecord() = printfn "Hello from AvaloniaFuncUI"

    override __.EndProcessing() =
        printfn "\n\n\n\n\nEndProcessing called"

        printfn "Starting Avalonia FuncUI application..."

        let app = (app.Instance :?> App)
        app.mainWindow <- new MainWindow()
        app.mainWindow.WindowStartupLocation <- WindowStartupLocation.CenterScreen
        app.desktopLifetime.MainWindow <- app.mainWindow
        app.desktopLifetime.ShutdownMode <- ShutdownMode.OnMainWindowClose

        let cts = new Threading.CancellationTokenSource()

        app.mainWindow.Closed.Add(fun _ ->
            printfn "MainWindow closed, shutting down application."
            cts.Cancel())

        app.mainWindow.Show()
        let ret = app.Run(cts.Token)
        printfn $"Avalonia FuncUI application started successfully. {ret}"

        app.mainWindow.Close()
        cts.Cancel()

        Console.WriteLine("\n\n\n\n\n\n\n\n\n\n")
