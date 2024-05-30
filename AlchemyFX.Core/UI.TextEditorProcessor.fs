namespace AlchemyFX.UI.Editors

open System
open System.IO
open System.Text
open System.Windows
open System.Windows.Media
open System.Windows.Controls
open System.Windows.Documents
open System.Windows.Input
open AlchemyFX.UI

type TextEditorProcessor() =

    let symbolSet = Set [
        "string"
        "char"
        "null"
    ]

    let specials = Set [
        '.'
        ')'
        '('
        '['
        ']'
        '>'
        '<'
        ':'
        ';'
        '\n'
        '\t'
        '\r'
    ]

    let isKnownSymbol (symbol: string) = symbolSet |> Set.contains(symbol.ToLower())

    let isSpecialSymbol (i: char) = specials |> Set.contains i

    let mutable foundSymbols = []

    let updateStyles =
        Seq.rev
        >> Seq.iter(
            fun (symbol : Symbol) ->
                try
                    symbol.ToRange()
                    |> fun range ->
                        range.ApplyPropertyValue(
                            TextElement.ForegroundProperty,
                            SolidColorBrush(Colors.LightBlue)
                        )
                with _ -> ()
        )

    let mapRunToSymbols (run: Run) = seq {
        let mutable startIndex = 0
        let mutable endIndex = 0
        let text = run.Text

        for i = 0 to text.Length - 1 do
            if Char.IsWhiteSpace(text[i]) || isSpecialSymbol(text[i]) then
                if i > 0 && not (Char.IsWhiteSpace(text[i - 1]) || isSpecialSymbol(text[i - 1])) then
                    endIndex <- i - 1
                    let word = text[startIndex..endIndex]
                    if isKnownSymbol(word) then
                        yield {
                            StartPosition = run.ContentStart.GetPositionAtOffset(startIndex, LogicalDirection.Forward)
                            EndPosition = run.ContentStart.GetPositionAtOffset(endIndex + 1, LogicalDirection.Backward)
                            Word = word
                        }
                startIndex <- i + 1

        let lastWord = text[startIndex..text.Length - 1]
        if isKnownSymbol(lastWord) then
            yield {
                StartPosition = run.ContentStart.GetPositionAtOffset(startIndex, LogicalDirection.Forward)
                EndPosition = run.ContentStart.GetPositionAtOffset(text.Length, LogicalDirection.Backward)
                Word = lastWord
            }
    }

    let mapRuns (transformRunToSymbols : Run -> Symbol seq) (document : FlowDocument) = seq {
        let mutable navigator = document.ContentStart
        while navigator.CompareTo(document.ContentEnd) < 0 do
            let context = navigator.GetPointerContext(LogicalDirection.Backward)
            if context = TextPointerContext.ElementStart && navigator.Parent :? Run then
                let run = navigator.Parent :?> Run
                if run.Text <> "" then
                    yield! transformRunToSymbols(run)
            navigator <- navigator.GetNextContextPosition(LogicalDirection.Forward)
    }

    interface ITextProcessor with

        member x.TextChanged (event : TextProcessEventArgs) =
            event.DocumentSession.FlowDocument
            |> FlowDocument.removeStyles
            |> mapRuns mapRunToSymbols
            |> updateStyles

and private Symbol =
    {
        Word : string
        StartPosition : TextPointer
        EndPosition : TextPointer
    }
    member s.ToRange() : TextRange =
        TextRange(s.StartPosition, s.EndPosition)