#r "nuget: FSharp.Formatting"
#r "nuget: FSharp.Formatting.Literate"

#load "String.fs"

open System
open System.Text
open System.IO

open FSharp.Formatting.Markdown
open FSharp.Formatting.Common

open AlchemyFX

@"test.md"
|> File.ReadAllText

type LineInfo =
    {
        LineNumber:  int
        Length : int
        StartOffset : int64
    }

and MarkdownRangeWithOffset =
    {
        StartLine : int
        StartColumn : int
        EndLine : int
        EndColumn : int
        Offset : int64
        EndOffset : int64
    }
    static member ofMarkdownRange (lineInfoMap : LineInfoMap) (markdownRange : MarkdownRange) =
        {
            StartLine = markdownRange.StartLine
            StartColumn = markdownRange.StartColumn
            EndLine = markdownRange.EndLine
            EndColumn = markdownRange.EndColumn
            Offset = lineInfoMap[markdownRange.StartLine].StartOffset + int64 markdownRange.StartColumn
            EndOffset = (lineInfoMap[markdownRange.EndLine].StartOffset + int64 markdownRange.EndColumn - 1L)
        }
        
and LineInfoMap = Map<int, LineInfo>

and ElementInfo = {
    ElementName : string
    Range : MarkdownRangeWithOffset option
    //Element : Element
}

and Element =
| Block of MarkdownParagraph
| Inline of MarkdownSpan

module LineInfos =

    let parse (text: string) =
        let rec loop (lineNumber : int) (startLineOffset : int64) (acc : LineInfo list) = function
        | [] -> acc
        | (x : string) :: xs ->
            let lineLength = x.Length
            xs |> loop
                (lineNumber + 1)
                (startLineOffset + int64 lineLength)
                ({
                    LineNumber = lineNumber + 1
                    Length = lineLength
                    StartOffset = startLineOffset
                } :: acc)
        text
        |> _.ToLines()
        |> List.ofSeq
        |> loop 0 0 []
        |> Seq.rev

    let toMap =
        Seq.map(fun (lineInfo : LineInfo) -> lineInfo.LineNumber, lineInfo)
        >> Map

    let createMap = parse >> toMap


let rec extractElementInfos (lineInfoMap : LineInfoMap) (paragraphs: MarkdownParagraphs) : ElementInfo list =
    paragraphs
    |> List.collect (fun paragraph ->
        let elementName, range, element =
            match paragraph with
            | Heading(_, _, range) -> "Heading", range, Block paragraph
            | Paragraph(_, range) -> "Paragraph", range, Block paragraph
            | CodeBlock(_, _, _, _, _, range) -> "CodeBlock", range, Block paragraph
            | InlineHtmlBlock(_, _, range) -> "InlineHtmlBlock", range, Block paragraph
            | ListBlock(_, _, range) -> "ListBlock", range, Block paragraph
            | QuotedBlock(_, range) -> "QuotedBlock", range, Block paragraph
            | Span(body, range) -> "Span", range, Block paragraph
            | LatexBlock(_, _, range) -> "LatexBlock", range, Block paragraph
            | HorizontalRule(_, range) -> "HorizontalRule", range, Block paragraph
            | TableBlock(_, _, _, range) -> "TableBlock", range, Block paragraph
            | OtherBlock(_, range) -> "OtherBlock", range, Block paragraph
            | EmbedParagraphs(_, range) -> "EmbedParagraphs", range, Block paragraph
            | YamlFrontmatter(_, range) -> "YamlFrontmatter", range, Block paragraph
            | OutputBlock(_, _, _) -> "OutputBlock", None, Block paragraph
        let elementInfos =
            match paragraph with
            | Heading(_, body, _)
            | Paragraph(body, _)
            | Span(body, _) -> body |> extractSpanInfos lineInfoMap
            | ListBlock(_, items : MarkdownParagraphs list, _) ->
                items
                |> List.map (extractElementInfos lineInfoMap)
                |> List.concat
            | QuotedBlock(items : MarkdownParagraphs, _) ->
                items |> extractElementInfos lineInfoMap
            | _ -> []
        { ElementName = elementName; Range = range |> Option.map(MarkdownRangeWithOffset.ofMarkdownRange lineInfoMap)(*; Element = element*) } :: elementInfos
    )

and extractSpanInfos (lineInfoMap : LineInfoMap) (spans: MarkdownSpans) : ElementInfo list =
    spans
    |> List.collect (fun span ->
        let elementName, range, element =
            match span with
            | Literal(_, range) -> "Literal", range, Inline span
            | InlineCode(_, range) -> "InlineCode", range, Inline span
            | Strong(_, range) -> "Strong", range, Inline span
            | Emphasis(_, range) -> "Emphasis", range, Inline span
            | AnchorLink(_, range) -> "AnchorLink", range, Inline span
            | DirectLink(_, _, _, range) -> "DirectLink", range, Inline span
            | IndirectLink(_, _, _, range) -> "IndirectLink", range, Inline span
            | DirectImage(_, _, _, range) -> "DirectImage", range, Inline span
            | IndirectImage(_, _, _, range) -> "IndirectImage", range, Inline span
            | HardLineBreak(range) -> "HardLineBreak", range, Inline span
            | LatexInlineMath(_, range) -> "LatexInlineMath", range, Inline span
            | LatexDisplayMath(_, range) -> "LatexDisplayMath", range, Inline span
            | EmbedSpans(_, range) -> "EmbedSpans", range, Inline span
        { ElementName = elementName; Range = range |> Option.map(MarkdownRangeWithOffset.ofMarkdownRange lineInfoMap)(*; Element = element*) } :: []
    )

let md =
    "a **b** c" 

let lineMap = md |> LineInfos.createMap

let markdownDocument = md |> Markdown.Parse |> _.Paragraphs |> extractElementInfos lineMap |> List.rev
