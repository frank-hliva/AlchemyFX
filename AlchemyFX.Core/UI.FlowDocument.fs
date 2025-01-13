namespace rec AlchemyFX.UI.Editors

open System
open System.IO
open System.Xml
open System.Xml.Linq
open System.Windows
open System.Windows.Markup
open System.Windows.Controls
open System.Windows.Documents
open System.Windows.Input
open System.Runtime.CompilerServices

open AlchemyFX
open AlchemyFX.UI

module FlowDocument =

    let toText (flowDocument: FlowDocument) : string =
        TextRange(flowDocument.ContentStart, flowDocument.ContentEnd).Text

    let withTheme (theme : ITheme) (flowDocument : FlowDocument) =
        flowDocument.Background <- theme.DarkBackgroundBrush
        flowDocument.Foreground <- theme.DarkFontBrush
        flowDocument.FontFamily <- theme.CodeFontFamily
        flowDocument.FontSize <- theme.FontSizeNormal
        flowDocument.LineHeight <- flowDocument.FontSize * theme.LineSpacing
        flowDocument

    let clone (flowDocument: FlowDocument) =
        use stringReader = new StringReader(flowDocument |> XamlWriter.Save)
        use xmlTextReader = new XmlTextReader(stringReader)
        XamlReader.Load(xmlTextReader) :?> FlowDocument

    let clear (flowDocument : FlowDocument) =
        flowDocument.Blocks.Clear()
        flowDocument

    let inline toXaml (flowDocument : FlowDocument) =
        flowDocument
        |> XamlWriter.Save
        |> XDocument.Parse
        |> _.ToString(SaveOptions.None)

    let ofXaml (xamlText : string) =
        xamlText
        |> XamlReader.Parse
        :?> FlowDocument

    let removeStyles (document: FlowDocument) =
        TextRange(document.ContentStart, document.ContentEnd).ClearAllProperties()
        document

[<Extension>]
type FlowDocumentExtensions () =
    
    [<Extension>]
    static member inline ExportToXaml(flowDocument : FlowDocument) =
        flowDocument |> FlowDocument.toXaml

    [<Extension>]
    static member inline GetText(flowDocument : FlowDocument) =
        flowDocument |> FlowDocument.toText

type FlowDocumentSession(
    editor : RichTextBox,
    flowDocument : FlowDocument,
    Theme : ITheme option
) as session =
    let mutable theme : ITheme option = None
    let mutable fontMetrics : FontMetrics option = None

    let tryCalculateFontMetrics () =
        match theme with
        | Some theme -> Some <| FontMetrics.calculate session
        | _ -> None

    member session.Editor = editor
    member session.FlowDocument = flowDocument
    member session.Theme
        with get() = theme
        and set value =
            theme <- value
            fontMetrics <- tryCalculateFontMetrics()

    member session.FontMetrics
        with get() = fontMetrics

module FlowDocumentSession =

    let inline init (editor : RichTextBox) (flowDocument : FlowDocument) =
        FlowDocumentSession(editor, flowDocument, None)

    let updateMetrics (session : FlowDocumentSession) =
        match session.FontMetrics with
        | Some fontMetrics ->
            session.FlowDocument.Blocks
            |> Seq.iter
                (fun block -> 
                    block.LineHeight <- fontMetrics.LineHeight
                    block.Padding <- fontMetrics.BorderSpacing
                )
            session
        | _ -> session

    let appendText (textContent : string) (session : FlowDocumentSession) =
        (session.Editor : RichTextBox).AppendText(textContent)
        session |> updateMetrics

    let inline clear (session: FlowDocumentSession) =
        session.FlowDocument
        |> FlowDocument.clear
        |> ignore
        session

    let assignText (textContent : string) (session : FlowDocumentSession) =
        session
        |> clear
        |> appendText textContent

    let inline withTheme (theme : ITheme option) (session : FlowDocumentSession) =
        match theme with
        | None ->
            session.Theme <- None
        | Some theme -> 
            session.Theme <- Some theme
            session.FlowDocument
            |> FlowDocument.withTheme theme
            |> ignore
        session

type FontMetrics =
    {
        LineSpacing : double
        FontSize : double
        LineHeight : double
        BorderSpacing : Thickness
    }

module FontMetrics =

    let [<Literal>] defaultLineSpacing = 1.5

    let calculate (session : FlowDocumentSession) : FontMetrics =
        let lineSpacing = session.Theme |> function
            | Some theme -> theme.LineSpacing
            | None -> defaultLineSpacing
        let fontSize = session.FlowDocument.FontSize
        {
            LineSpacing = lineSpacing
            FontSize = fontSize
            LineHeight = fontSize * lineSpacing
            BorderSpacing = Thickness(fontSize * (lineSpacing - 1.0))
        }

