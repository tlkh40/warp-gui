namespace warp

open Avalonia.Controls
open Fabulous.Avalonia

open Avalonia.Media
open Fabulous
open type Fabulous.Avalonia.View

// Taken from:
// https://isthisit.nz/posts/2021/execute-a-shell-process-in-fsharp/
module WarpUtil =
    open System

    type ProcessResult = { 
        ExitCode : int; 
        StdOut : string; 
        StdErr : string 
    }

    let executeProcess (processName: string) (processArgs: string) =
        let psi = new Diagnostics.ProcessStartInfo(processName, processArgs) 
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.CreateNoWindow <- true        
        let proc = Diagnostics.Process.Start(psi) 
        let output = new Text.StringBuilder()
        let error = new Text.StringBuilder()
        proc.OutputDataReceived.Add(fun args -> output.Append(args.Data) |> ignore)
        proc.ErrorDataReceived.Add(fun args -> error.Append(args.Data) |> ignore)
        proc.BeginErrorReadLine()
        proc.BeginOutputReadLine()
        proc.WaitForExit()
        { ExitCode = proc.ExitCode; StdOut = output.ToString(); StdErr = error.ToString() }
    
    let getConnection _ =
        let proc = executeProcess "warp-cli" "status"
        not (proc.StdOut.Contains("Disconnected"))

    let toggleConnection _ =
        if getConnection () then
            executeProcess "warp-cli" "disconnect" |> ignore
        else
            executeProcess "warp-cli" "connect" |> ignore

module App =
    open Avalonia.Themes.Fluent
    open Avalonia.Controls.ApplicationLifetimes

    type Model =
        { Connected: bool }

    type Msg =
        | ToggleConnection of bool
        // True from timer, false from normal
        | GetState of bool
        | Exit
        | Show

    let initModel = { Connected = false }

    let timerCmd () =
        async {
            do! Async.Sleep 15000
            printfn "Hi"
            return GetState(true)
        }
        |> Cmd.OfAsync.msg

    let init () = initModel, Cmd.ofMsg (GetState true)

    let content model =
        (VStack() {
            TextBlock("Warp UI").fontSize(30)
                .fontWeight(FontWeight.Bold).centerText()
            ToggleSwitch(model.Connected, ToggleConnection)
                .offContent("").onContent("").content("")
                .padding(0).margin(0).width(40)
                .renderTransform(ScaleTransform(1.7, 1.7))
                .renderTransformOrigin(Avalonia.RelativePoint.Center)
                .center()
        })
            .center()

    let trayUrl model =
        if model.Connected then "avares://warp/Assets/connected.ico" else "avares://warp/Assets/disconnected.ico"

    let trayIcon model = 
        TrayIcon(
            (trayUrl model), "Warp ui"
        ).onClicked(Show).menu(
            NativeMenu() {
                NativeMenuItem("Toggle connection", ToggleConnection false)
                NativeMenuItem("exit", Exit)
            }
        )

    let window model = 
        Window(content model)
            .height(450)
            .width(300)
            .canResize(false)

    let update msg model =
        match msg with
        | Exit ->
            match FabApplication.Current.ApplicationLifetime with
            | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
                desktopLifetime.Shutdown()
            | _ -> ()
            model, Cmd.none
        | ToggleConnection(_) ->
            WarpUtil.toggleConnection ()
            model, Cmd.ofMsg (GetState false)
        | GetState(s) ->
            let connection = WarpUtil.getConnection ()
            { model with Connected = connection }, (if s then timerCmd() else Cmd.none)
        | Show ->
            if not FabApplication.Current.MainWindow.IsVisible then
                try
                    let node = ViewNode.get(FabApplication.Current)
                    let struct (_, view) = Helpers.createViewForWidget node ((window model).Compile())
                    FabApplication.Current.MainWindow <- view :?> Window 
                    FabApplication.Current.MainWindow.Show()
                    FabApplication.Current.MainWindow.BringIntoView()
                    FabApplication.Current.MainWindow.Focus() |> ignore
                with
                    | b -> printfn $"{b}"
            model, Cmd.none


    let view model = 
        DesktopApplication(
            (window model)
        )
         |> _.trayIcon((trayIcon model))
    
    let create () =
        let program = Program.statefulWithCmd init update 
                                                            |> Program.withView view

        Avalonia.AppBuilder.Configure(fun () ->
            let app =
                FabApplication(
                    OnFrameworkInitialized =
                        fun app ->
                            let widget = 
                                (View.Component(program.State) {
                                    let! model = Mvu.State
                                    program.View model
                                }).Compile()

                            let treeContext: ViewTreeContext =
                                { CanReuseView = program.CanReuseView
                                  GetViewNode = ViewNode.get
                                  GetComponent = Component.get
                                  SetComponent = Component.set
                                  SyncAction = program.SyncAction
                                  Logger = program.State.Logger
                                  Dispatch = ignore }

                            let def = WidgetDefinitionStore.get widget.Key
                            def.AttachView(widget, treeContext, ValueNone, app) |> ignore
                            match app.ApplicationLifetime with
                            | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
                                desktopLifetime.ShutdownMode <- ShutdownMode.OnExplicitShutdown
                            | _ -> ()

                )
            app.RequestedThemeVariant <- Avalonia.Styling.ThemeVariant.Dark
            app.Resources["ToggleSwitchPostContentMargin"] <- 0
            app.Resources["ToggleSwitchPreContentMargin"] <- 0
            app.Styles.Add(FluentTheme())
            app)
