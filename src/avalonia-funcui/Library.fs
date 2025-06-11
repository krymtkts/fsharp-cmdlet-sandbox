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
                                                                Button.onClick (fun _ ->
                                                                    dispatch (Send "Button Clicked!"))
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

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add(FluentTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Dark

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            let mainWindow = MainWindow()
            desktopLifetime.MainWindow <- mainWindow
        | _ -> ()

[<Cmdlet(VerbsDiagnostic.Test, "AvaloniaFuncUI")>]
[<OutputType(typeof<PSObject>)>]
type SelectPocofCommand() =
    inherit PSCmdlet()

    override __.BeginProcessing() = base.BeginProcessing()

    override __.ProcessRecord() = printfn "Hello from AvaloniaFuncUI"

    override __.EndProcessing() =
        printfn
            $"OSArchitecture: {RuntimeInformation.OSArchitecture} OSDescription: {RuntimeInformation.OSDescription} FrameworkDescription: {RuntimeInformation.FrameworkDescription} ProcessArchitecture: {RuntimeInformation.ProcessArchitecture} RuntimeIdentifier: {RuntimeInformation.RuntimeIdentifier}"

        printfn "EndProcessing called"

        let moduleDir =
            System.IO.Path.GetDirectoryName(
                System
                    .Reflection
                    .Assembly
                    .GetExecutingAssembly()
                    .Location
            )

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
            with
            | e ->
                printfn "Failed to load SkiaSharp library: %s" e.Message
                ())

        printfn "Starting Avalonia FuncUI application..."

        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(Array.empty)
        |> ignore
