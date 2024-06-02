namespace warp.Desktop

open System
open Avalonia
open warp


module Program =
    [<CompiledName "BuildAvaloniaApp">]
    let buildAvaloniaApp () =
        App
            .create()
            .UsePlatformDetect()
            .LogToTrace()

    [<EntryPoint; STAThread>]
    let main argv =
        buildAvaloniaApp()
            .StartWithClassicDesktopLifetime(argv)
