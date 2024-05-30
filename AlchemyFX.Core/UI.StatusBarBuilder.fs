namespace AlchemyFX.UI

open System
open System.Collections.Generic
open System.Collections.Specialized
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Media
open System.Windows.Documents
open System.Windows.Media.Imaging
open AlchemyFX
open AlchemyFX.View

type StatusBarBuilder(
    statusBar : StatusBar,
    dictionary : dict,
    brushes : Brush dict,
    additionalComponent : obj option
 ) =

    let clearStatuses () =
        statusBar.Items.Clear()

    let renderStatuses () =
        dictionary
        |> Seq.iteri
            (fun index pair -> 
                let key, value = pair.Key, pair.Value
                if value <> null then
                    if index <> 0 then statusBar.Items.Add(Separator(Height = 15)) |> ignore
                    let textBlock = TextBlock(ToolTip = value)
                    textBlock.Inlines.Add($"{key}: ")
                    (match brushes.TryGet(key) with
                    | Some brush -> brush
                    | _ -> Color.FromArgb(255uy, 43uy, 91uy, 128uy) |> SolidColorBrush :> Brush)
                    |> fun brush ->
                        textBlock.Inlines.Add(Run(value.Turncate(25, "…"), Foreground = brush))
                        statusBar.Items.Add(StatusBarItem(Content = textBlock)) |> ignore
                else ()
            )

    let renderAdditionalComponent () =
        match additionalComponent with
        | Some component' ->
            StatusBarItem(
                Content = component',
                HorizontalAlignment = HorizontalAlignment.Right
            )
            |> statusBar.Items.Add
            |> ignore
        | None -> ()

    new(statusBar : StatusBar) =
        StatusBarBuilder(
            statusBar,
            dict(),
            dict<Brush>(),
            None
        )

    member sb.Dictionary = dictionary

    member sb.StatusBar = statusBar

    member sb.Item
        with get (key : string) = dictionary.[key]
        and set (key : string) (value : string) =
            dictionary.AddOrUpdate(key, value) |> ignore
            sb.Update()

    member sb.WithComponent(additionalComponent : obj) =
        StatusBarBuilder(
            statusBar,
            dictionary,
            brushes,
            Some additionalComponent
        )

    member sb.WithBrush(key : string, brush : Brush) =
        brushes.AddOrUpdate(key, brush) |> ignore
        sb

    member sb.WithColor(key : string, hexcolor : String) =
        sb.WithBrush(key, hexcolor |> HexColor.toBrush)

    member sb.AdditionalComponent = additionalComponent

    member sb.Clear() = clearStatuses()

    member sb.Update() =
        clearStatuses()
        renderStatuses()
        renderAdditionalComponent()

    static member CreateOpenSettingsButton() =
        Button(
            Width = 67.0,
            Height = 20.0,
            Name = "OpenSettingsButton",
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Content = 
                ([
                    Image(
                        Width = 16.0,
                        Height = 16.0,
                        Source = (Application.Current.Resources["SettingsIcon"] :?> ImageSource),
                        Margin = Thickness(0.0, 0.0, 5.0, 0.0)
                    ) :> UIElement
                    TextBlock(Text = "Config") :> UIElement
                ] |> stack'h)
        )

module Status =

    module Location =
        let toHostName location =
            location
            |> Location.tryToHostName
            |> Option.defaultValue "Unknown"

    module ProfileViewModel =
        let toProfileStatus (profileViewModel : ProfileViewModel) =
            match profileViewModel.Mode with
            | ProccessingMode.Nothing -> "Not selected"
            | _ -> profileViewModel.CurrentProfile.Name