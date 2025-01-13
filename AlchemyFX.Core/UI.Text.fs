namespace AlchemyFX.UI

open System
open System.IO
open System.Text
open System.Windows
open System.Windows.Controls
open System.Windows.Documents
open System.Windows.Input
open AlchemyFX
open AlchemyFX.UI

module Text =

    let isXaml (text : string) =
        match text.ToLines() |> Seq.tryHead with
        | Some firstLine -> firstLine.Trim().Contains "<?xml"
        | None -> false

    let isPlainText = isXaml >> not

    //let toFlowDocument (plainText : string) =
    //    let flowDocument = new FlowDocument()
    //    flowDocument.Blocks.Add(plainText |> Run |> Paragraph)
    //    flowDocument