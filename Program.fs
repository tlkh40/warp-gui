namespace warp.Desktop

open System
open Avalonia
open Fabulous.Avalonia
open warp

module Program =
    [<CompiledName "BuildAvaloniaApp">]
    let buildAvaloniaApp () =
        AppBuilder
            .Configure(fun () ->
                let app = Program.startApplication App.program
                app.Styles.Add(App.theme)
                app.RequestedThemeVariant <- Styling.ThemeVariant.Dark
                app.Resources["ToggleSwitchPostContentMargin"] <- 0
                app.Resources["ToggleSwitchPreContentMargin"] <- 0
                app)
            .LogToTrace(areas = Array.empty)
            .UsePlatformDetect()

    [<EntryPoint; STAThread>]
    let main argv =
        buildAvaloniaApp().StartWithClassicDesktopLifetime(argv)
