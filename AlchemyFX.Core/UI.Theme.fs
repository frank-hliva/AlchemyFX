namespace AlchemyFX.UI

open System
open System.Windows
open System.Windows.Media

module Resources =

    let resources = lazy(Application.Current.Resources)

    let read<'t> resourceName = resources.Value[resourceName] :?> 't

[<AllowNullLiteral>]
type ITheme =
    abstract member WindowBackgroundBrush: SolidColorBrush
    abstract member DarkFontBrush: SolidColorBrush
    abstract member DarkBackgroundBrush: SolidColorBrush
    abstract member DarkBlueBrush: SolidColorBrush
    abstract member LineNumbersForegroundBrush: SolidColorBrush
    abstract member ErrorFuchsiaBrush: SolidColorBrush
    abstract member ErrorLightRedBrush: SolidColorBrush
    abstract member LinkTextForegroundBrush: SolidColorBrush
    abstract member CaretBrush: SolidColorBrush

    abstract member CodeFontFamily: FontFamily

    abstract member FontSizeNormal: double
    abstract member FontSizeSmall: double
    abstract member InlineControlHeight: double

    abstract member LineSpacing: double

[<AllowNullLiteral>]
type ThemeDefinition() =
    let (!) = Resources.read<SolidColorBrush>
    interface ITheme with
        member this.WindowBackgroundBrush = !"WindowBackgroundBrush"
        member this.DarkFontBrush = !"DarkFontBrush"
        member this.DarkBackgroundBrush = !"DarkBackgroundBrush"
        member this.DarkBlueBrush = !"DarkBlueBrush"
        member this.LineNumbersForegroundBrush = !"LineNumbersForegroundBrush"
        member this.ErrorFuchsiaBrush = !"ErrorFuchsiaBrush"
        member this.ErrorLightRedBrush = !"ErrorLightRedBrush"
        member this.LinkTextForegroundBrush = !"LinkTextForegroundBrush"
        member this.CaretBrush = !"CaretBrush"

        member this.CodeFontFamily = "CodeFontFamily" |> Resources.read<FontFamily>
        member this.FontSizeNormal = "FontSizeNormal" |> Resources.read<double>
        member this.FontSizeSmall = "FontSizeSmall" |> Resources.read<double>
        member this.InlineControlHeight = "InlineControlHeight" |> Resources.read<double>

        member this.LineSpacing = "LineSpacing" |> Resources.read<double>