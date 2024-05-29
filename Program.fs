namespace Cat
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI.Hosts
open Avalonia.Themes.Fluent

type MainWindow() =
    inherit HostWindow()
    do
        base.Content <- Counter.Amogus.view()
        base.Width <- 300
        base.Height <- 450
        base.CanResize <- false

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add (FluentTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Dark

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow()
        | _ -> ()

module Program =
    [<EntryPoint>]
    let main(args: string[]) =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)