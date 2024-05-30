namespace AlchemyFX.UI

open System.Windows.Media

module HexColor =

    let toBrush (colorCode : string) =
        BrushConverter().ConvertFrom(colorCode) :?> SolidColorBrush