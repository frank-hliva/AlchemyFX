namespace AlchemyFX.Markdown

open System

type Text = char list

type Token =
    | Text of string
    | Bold of string
    | Bold2 of string
    | Italic of string
    | BoldItalic of string
    | LineBreak of string
    | Link of string * string
    member token.Value with get() =
        match token with
        | Bold b -> sprintf $"{{*{b}*}}"
        | Italic i -> $"{{/i/}}"
        | BoldItalic i -> $"***{i}***"
        | Link (t, l) -> t
        | LineBreak l -> l
        | Text t -> t

module Text =
    let toString : char list -> _ = String.Concat
    let toStrRev : char list -> _ = List.rev >> toString

    let toLink (input : char list) =
        let text = input |> toStrRev
        if text.StartsWith("www.") then $"https://{text}" else text
        |> fun location -> Link (text, location)

module rec Markdown =

    let isValidUrlChar (ch: char) =
        match ch with
        | ch when Char.IsLetterOrDigit ch -> true
        | '-' | '_' | '.' | '!' | '~' | '*' | '\'' | '(' | ')'
        | ';' | '/' | '?' | ':' | '@' | '&' | '=' | '+' | '$' | ',' | '#' | '%' -> true
        | _ -> false

    let startsWithPrefixes (prefixes : string seq) (input : Text) =
        prefixes
        |> Seq.exists
            (fun prefix -> 
                let prefixLen = prefix.Length
                input.Length > prefixLen &&
                input |> List.take prefixLen |> Text.toString = prefix
            )

    let (|StartsWithPrefixes|) (prefixes : string seq) (input : Text) =
        input |> startsWithPrefixes prefixes

    let isValidUrl = startsWithPrefixes ["http://"; "https://"; "www."]

    let boldItalic acc = function
    | [] -> acc, []
    | '*' :: '*' :: '*' :: t -> acc, t
    | c :: t -> bold (c :: acc) t

    let bold acc = function
    | [] -> acc, []
    | '*' :: '*' :: t -> acc, t
    | c :: t -> bold (c :: acc) t

    let bold2 acc = function
    | [] -> acc, []
    | '_' :: '_' :: t -> acc, t
    | c :: t -> bold (c :: acc) t

    let italic acc = function
    | [] -> acc, []
    | '*' :: t -> acc, t
    | c :: t -> italic (c :: acc) t

    let strike acc = function
    | [] -> acc, []
    | '~' :: t -> acc, t
    | c :: t -> italic (c :: acc) t

    let (|IsWhitespace|_|) input = 
        match input with
        | c when Char.IsWhiteSpace c -> Some c
        | _ -> None

    let link acc = function
    | [] -> acc, []
    | IsWhitespace c :: t
    | c :: t when (c |> isValidUrlChar |> not) -> acc, (c :: t)
    | '*' :: _ as t -> acc, t
    | c :: t -> link (c :: acc) t

    let toTextToken (tokensAcc : Token list) (charsAcc : char list) =
        match charsAcc.Length with
        | 0 -> tokensAcc
        | _ -> (charsAcc |> Text.toStrRev |> Text) :: tokensAcc

    let (|IsLineBreak|_|) input = 
        match input with
        | '\r' :: '\n' :: tail -> Some (LineBreak "\r\n", tail)
        | '\r' :: tail -> Some (LineBreak "\r", tail)
        | '\n' :: tail -> Some (LineBreak "\n", tail)
        | _ -> None

    let tokenize (tokensAcc : Token list) (acc : char list) (chars : char list) : Token list * char list =
        match chars with
        | [] -> acc |> (toTextToken tokensAcc), []
        | IsLineBreak (lineBreak, tail) -> 
            tail |> tokenize (lineBreak :: (acc |> toTextToken tokensAcc)) []
        | ('w' :: 'w' :: 'w' :: _)
        | ('h' :: 't' :: 't' :: 'p' :: _) as t when isValidUrl t ->
            let (url, t) = t |> link []
            t |> tokenize ((url |> Text.toLink) :: (acc |> toTextToken tokensAcc)) []
        | '*' :: '*' :: '*' :: t ->
            let (bold, t) = t |> boldItalic []
            t |> tokenize ((bold |> Text.toStrRev |> BoldItalic) :: (acc |> toTextToken tokensAcc)) []
        | '_' :: '_' :: t ->
            let (bold, t) = t |> bold2 []
            t |> tokenize ((bold |> Text.toStrRev |> Bold2) :: (acc |> toTextToken tokensAcc)) []
        | '*' :: '*' :: t ->
            let (bold, t) = t |> bold []
            t |> tokenize ((bold |> Text.toStrRev |> Bold) :: (acc |> toTextToken tokensAcc)) []
        | '*' :: t ->
            let (italic, t) = t |> italic []
            t |> tokenize ((italic |> Text.toStrRev |> Italic) :: (acc |> toTextToken tokensAcc)) []
        | '~' :: t ->
            let (italic, t) = t |> strike []
            t |> tokenize ((italic |> Text.toStrRev |> Italic) :: (acc |> toTextToken tokensAcc)) []
        | h :: t ->
            t |> tokenize tokensAcc (h :: acc)

    let tokenizeMarkdown input =
        input
        |> Seq.toList
        |> tokenize [] []
        |> fun (tokens, _) -> List.rev tokens

    type Element =
        { Token: Token; Start: int; End: int; Output : string }

    let toElements =
        let rec loop (elements : Element list) (start : int) = function
        | [] -> elements
        | (token : Token) :: t ->
            let output = token.Value
            let tokenLen = output.Length
            t |> loop ({ Token = token; Start = start; End = start + tokenLen; Output = output } :: elements) (start + tokenLen + 1)
        loop [] 0 >> Seq.rev