namespace AlchemyFX.UI

open System
open System.Globalization
open System.Collections.Generic
open System.Windows
open System.Windows.Data
open System.Windows.Controls
open System.Windows.Media
open System.Windows.Media.Imaging

[<AutoOpen>]
module Helpers =

    let mbox format = 
        Printf.ksprintf
            (fun text -> MessageBox.Show(text) |> ignore)
            format

type NotConverter() =
    interface IValueConverter with
        member _.Convert(value, targetType, parameter, culture) =
            match value with
            | :? bool as boolValue -> box <| not boolValue
            | _ -> value

        member _.ConvertBack(value, targetType, parameter, culture) =
            match value with
            | :? bool as boolValue -> box <| not boolValue
            | _ -> value

[<AutoOpen>]
module StackPanelExt =
    type StackPanel with
        member sp.add item = sp.Children.Add item |> ignore
        member sp.addRange items = items |> List.iter sp.add

    let stack orientation items = 
        StackPanel(
            Orientation = orientation,
            Margin = Thickness(0.0)
        )
        |> fun stackPanel ->
            stackPanel.addRange items
            stackPanel

    let stack'h items = stack Orientation.Horizontal items
    let stack'v items = stack Orientation.Vertical items

module Grid =
    let setRow (row: int) (element: UIElement) (grid: Grid) =
        Grid.SetRow(element, row)

    let setCol (row: int) (element: UIElement) (grid: Grid)  =
        Grid.SetColumn(element, row)