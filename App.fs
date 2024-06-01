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

    type Model =
        { Connected: bool }

    type Msg =
        | ToggleConnection of bool
        // True from timer, false from normal
        | Exit
        | GetState of bool

    let initModel = { Connected = false }

    let timerCmd () =
        async {
            do! Async.Sleep 15000
            return GetState(true)
        }
        |> Cmd.ofAsyncMsg

    let init () = initModel, Cmd.ofMsg (GetState true)

    let update msg model =
        match msg with
        | Exit -> model, Cmd.none
        | ToggleConnection(_) ->
            WarpUtil.toggleConnection ()
            model, Cmd.ofMsg (GetState false)
        | GetState(s) ->
            let connection = WarpUtil.getConnection ()
            { model with Connected = connection }, (if s then timerCmd() else Cmd.none)

    let view model =
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
        ).menu(
            NativeMenu() {
                NativeMenuItem("Toggle connection", ToggleConnection false)
                NativeMenuItem("exit", Exit)
            }
        )


    let app model = 
        DesktopApplication(
        Window(view model)
            .height(450)
            .width(300)
            .canResize(false)
        )
         |> _.trayIcon((trayIcon model))
    
    let theme = FluentTheme()

    let program = Program.statefulWithCmd init update app