namespace AlchemyFX.UI

open System
open System.Windows
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Input

type private Window' = Window

[<RequireQualifiedAccess>]
module LaunchWindow =
    let image = 
        "pack://application:,,,/Images/Launch.png"
        |> Uri
        |> BitmapImage
        |> ImageBrush

    let create() =
        Window'(
            Width = 720.0, 
            Height = 659.0, 
            WindowStyle = WindowStyle.None, 
            ResizeMode = ResizeMode.NoResize, 
            ShowInTaskbar = false, 
            //Topmost = true, 
            Background = image, 
            Cursor = Cursors.Wait,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        )

    let show (launchWindow : Window') =
        launchWindow.Show()

    let close (launchWindow : Window') =
        launchWindow.Close()