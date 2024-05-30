module UI.TextRange

open System
open System.IO
open System.Text
open System.Windows
open System.Windows.Media
open System.Windows.Controls
open System.Windows.Documents
open System.Windows.Input
open AlchemyFX.UI

let createHyperlink  (url: string) (textRange: TextRange) =
    let hyperlink = new Hyperlink(NavigateUri = Uri url)
    hyperlink.Inlines.Add (Run textRange.Text)
    textRange.Text <- ""
    textRange.Start.Paragraph.Inlines.Add(hyperlink)