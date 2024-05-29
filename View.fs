namespace Cat.Counter

module Amogus =
    open Elmish
    open Avalonia.Layout
    open Avalonia.Controls
    open Avalonia.FuncUI
    open Avalonia.FuncUI.DSL
    open Cat.Util.Boom
    open Avalonia.Media
    open Avalonia.FuncUI.Elmish.ElmishHook
    open Avalonia.Threading
    open System

    type Model = {
        Connected: bool
    }

    type Msg = 
        | CommandToggle
        | GetState

    let init() =
        {
            Connected = false
        }, Cmd.ofMsg GetState

    let update msg model =
        match msg with
        | CommandToggle ->
            // Do magic
            let proc = executeProcess "warp-cli" "status"
            let status = not (proc.StdOut.Contains("Disconnected"))
            if status then 
                executeProcess "warp-cli" "disconnect" |> ignore
                printfn "Ran command for disconnect"
            else
                executeProcess "warp-cli" "connect" |> ignore
                printfn "Ran command for connect"
            model, Cmd.ofMsg GetState
        | GetState ->
            let proc = executeProcess "warp-cli" "status"
            let status = not (proc.StdOut.Contains("Disconnected"))
            printfn "Ran status check"
            { model with Connected = status }, Cmd.none

    let private subscriptions (model: Model) : Sub<Msg> =
        let timerSub (dispatch: Msg -> unit) =
            let invoke() = dispatch Msg.GetState; true
            DispatcherTimer.Run(invoke, TimeSpan.FromMilliseconds 10000.0)

        [ 
            [nameof timerSub], timerSub
        ]
    let transform = ScaleTransform(1.7, 1.7)

    let view () = Component (fun ctx->
            let model, dispatch = ctx.useElmish(init, update, Program.withSubscription subscriptions)
            Grid.create [
                Grid.height 450
                Grid.width 300
                Grid.children [
                    StackPanel.create [
                        Grid.row 0
                        StackPanel.spacing 10
                        StackPanel.horizontalAlignment HorizontalAlignment.Center
                        StackPanel.verticalAlignment VerticalAlignment.Center
                        StackPanel.children [
                            TextBlock.create [
                                TextBlock.padding 10
                                TextBlock.margin 2
                                TextBlock.textAlignment TextAlignment.Center
                                TextBlock.fontSize 32
                                TextBlock.text "CF Warp GUI"
                            ]
                            ToggleSwitch.create [
                                    ToggleSwitch.width 40
                                    ToggleSwitch.padding 0
                                    ToggleSwitch.padding 0
                                    ToggleSwitch.horizontalAlignment HorizontalAlignment.Center
                                    ToggleSwitch.init (fun v -> 
                                        v.Resources["ToggleSwitchPreContentMargin"] <- 0
                                        v.Resources["ToggleSwitchPostContentMargin"] <- 0
                                        ();
                                    )
                                    ToggleSwitch.renderTransform transform
                                    ToggleSwitch.renderTransformOrigin Avalonia.RelativePoint.Center
                                    ToggleSwitch.isChecked model.Connected
                                    ToggleSwitch.onClick (fun _ -> CommandToggle |> dispatch)
                            ]
                         ]
                    ]
                ]
            ]
    )