namespace avalonia_funcui

open System
open System.Collections
open System.Management.Automation
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
    let getModuleDir () =
        System.IO.Path.GetDirectoryName(Reflection.Assembly.GetExecutingAssembly().Location)

    let detectExtension () =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            "dll"
        elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            "so"
        elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            "dylib"
        else
            failwith "Unsupported OS"

    let resolver (ptrCache: Concurrent.ConcurrentDictionary<string, nativeint>) moduleDir extension =
        let tryLoadLibrary (moduleDir: string) (extension: string) (libraryName: string) =
            let libPath =
                let libPath =
                    if extension |> libraryName.EndsWith then
                        $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/{libraryName}"
                    else
                        $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/{libraryName}.{extension}"

                System.IO.Path.Combine(moduleDir, libPath)

            if libPath |> IO.File.Exists then
                printfn "Loading SkiaSharp library from: %s" libPath

                libPath
                |> NativeLibrary.TryLoad
                |> function
                    | true, ptr ->
                        printfn "Successfully loaded library. Handle: %A" ptr
                        ptr
                    | _ -> IntPtr.Zero
            else
                IntPtr.Zero

        DllImportResolver(fun libraryName assembly searchPath ->
            match libraryName |> ptrCache.TryGetValue with
            | true, ptr -> ptr
            | _ ->
                match tryLoadLibrary moduleDir extension libraryName with
                | ptr when ptr = IntPtr.Zero ->
                    // NOTE: fallback to the default behavior if the library is not found.
                    match NativeLibrary.TryLoad(libraryName, assembly, searchPath) with
                    | true, ptr ->
                        printfn "Successfully loaded library by fallback. Handle: %A" ptr
                        ptrCache.TryAdd(libraryName, ptr) |> ignore
                        ptr
                    | _ ->
                        // NOTE: Returning IntPtr.Zero means the library was not found. This will cause an error when P/Invoke is called.
                        printfn "Library not found: %s" libraryName
                        IntPtr.Zero
                | ptr ->
                    ptrCache.TryAdd(libraryName, ptr) |> ignore
                    ptr)

    do
        printfn "\n\n\n\n\nPreparing Avalonia assemblies...\n\n\n\n\n"
        // NOTE: those bindings cannot move out to static, it will cause a deadlock.
        let moduleDir = getModuleDir ()
        let extension = detectExtension ()
        let cache = new Concurrent.ConcurrentDictionary<string, nativeint>()
        let resolver = resolver cache moduleDir extension
        NativeLibrary.SetDllImportResolver(typeof<SkiaSharp.SKImageInfo>.Assembly, resolver)
        NativeLibrary.SetDllImportResolver(typeof<HarfBuzzSharp.Buffer>.Assembly, resolver)
        NativeLibrary.SetDllImportResolver(typeof<AppBuilder>.Assembly, resolver)
        NativeLibrary.SetDllImportResolver(typeof<Win32.AngleOptions>.Assembly, resolver)
        printfn "\n\n\n\n\nAvalonia assemblies prepared.\n\n\n\n\n"

module Main =
    open Avalonia.FuncUI

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
        let cellSize = 10.0
        let rows, cols = 30, 30
        let width = float cols * cellSize
        let height = float rows * cellSize
        let isAlive row col = (row + col) % 2 = 0

        let cells =
            Array2D.init (int height) (int width) (fun row col -> if isAlive row col then "white" else "black")

        Canvas.create [
            Canvas.width width
            Canvas.height height
            Canvas.children [
                for i in 0 .. (rows * cols - 1) do
                    let row = i / cols
                    let col = i % cols
                    let color = cells.[row, col]

                    yield
                        Rectangle.create [
                            Shapes.Rectangle.width cellSize
                            Shapes.Rectangle.height cellSize
                            Shapes.Rectangle.fill (Media.SolidColorBrush(Avalonia.Media.Color.Parse color))
                            Canvas.left (float col * cellSize)
                            Canvas.top (float row * cellSize)
                        ]
            ]

        ]
        |> generalize

type MainWindow() as this =
    inherit HostWindow()

    do
        base.Title <- "Example"
        base.Height <- 300.0
        base.Width <- 300.0
        base.CanResize <- false

        Program.mkProgram Main.init Main.update Main.view
        |> Program.withHost this
        |> Program.run

    override __.OnClosed(e: EventArgs) : unit = base.OnClosed(e: EventArgs)

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
            let lt = new ClassicDesktopStyleApplicationLifetime()

            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                // NOTE: Graphics operations can produce an extremely large amount of log output, which may be useful for debugging.
                // .LogToTextWriter(Console.Out, LogEventLevel.Verbose)
                .SetupWithLifetime(lt)

        printfn "\n\n\n\n\nAvalonia FuncUI application configured.\n\n\n\n\n"

        app

    override __.BeginProcessing() = printfn "BeginProcessing called."

    override __.ProcessRecord() = printfn "Hello from AvaloniaFuncUI"

    override __.EndProcessing() =
        printfn "\n\n\n\n\nEndProcessing called"

        printfn "Starting Avalonia FuncUI application..."

        let app = app.Instance :?> App
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
