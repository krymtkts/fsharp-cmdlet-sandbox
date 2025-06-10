namespace avalonia_funcui

open System.Management.Automation

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
        // TODO: PowerShell cmdlet cannot resolve libSkiaSharp.dll automatically,
        // TODO: dirty workaround 1 is add the path for libSkiaSharp.dll to PATH environment variable
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(Array.empty)
        |> ignore
