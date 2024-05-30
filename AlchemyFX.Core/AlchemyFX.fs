namespace AlchemyFX

open System
open System.Collections.Generic
open System.Text
open System.IO
open System.Runtime.CompilerServices
open System.Diagnostics
open System.Xml
open System.Xml.Linq
open Unity

module App =

    open System

    type Context =
        {
            AppName : string
        }

    let Context = { AppName = "AlchemyFX" }

[<AutoOpen>]
module AppHelpersImpl =
    let nil<'t> = null |> box |> unbox<'t>

    let cast<'target> (value : _) = value |> box |> unbox<'target>

    let print = printfn "%s"

    let nullToDefault default' value = if value = null then default' else value

    let emptyToDefault default' (value : string) = if String.IsNullOrWhiteSpace value then default' else value

module Optional =
    let ofNullObject (value : 't) =
        match value |> box with
        | null -> None
        | result -> result |> cast<'t> |> Some

    let toNullable (valueOpt: _ option) : Nullable<int> = 
        match valueOpt with
        | Some value -> Nullable value
        | None -> Nullable()

[<Extension>]
type DictionaryExtensions () =

    [<Extension>]
    static member inline TryGet(dictionary : Dictionary<'key, 'value>, key : 'key) =
        match dictionary.TryGetValue(key) with
        | true, value' -> Some value'
        | _ -> None

    [<Extension>]
    static member inline AddOrUpdate(dictionary : Dictionary<'key, 'value>, key : 'key, value : 'value) =
        if not <| dictionary.TryAdd(key, value) then
            dictionary[key] <- value
        dictionary

[<AutoOpen>]
module DictionaryExtensions =
    type dict<'t> = Dictionary<string, 't>
    type dict = Dictionary<string, string>

[<Extension>]
type UnityExtensions () =
    
    [<Extension>]
    static member inline RegisterApplicationContext(serviceContainer : IUnityContainer) =
        let context : App.Context = { AppName = "AlchemyFX" }
        serviceContainer.RegisterInstance<App.Context> context

module Location =

    let tryToHostName (location : string) =
        try Uri(location).Host |> Some with
        | _ -> None

    let toHostName = tryToHostName >> Option.defaultValue ""

    let openInBrowser (location : string) =
        ProcessStartInfo (location, UseShellExecute = true)
        |> Process.Start
        |> ignore

module IO =
    module Directory =
        let tryResolve (path : string) =
            try
                if path |> Directory.Exists |> not then
                    Directory.CreateDirectory path |> ignore
                    Result.Ok path
                else Result.Ok path
            with
            | ex -> Result.Error ex

    module File =

        let readAllText (filePath : string) =
            File.ReadAllText(filePath, Encoding.UTF8)

        let writeAllText (contents : string) (filePath : string) =
            File.WriteAllText(filePath, contents, Encoding.UTF8)

        let readOrCreate (defaultValue : string) (filePath : string) =
            if File.Exists filePath then
                filePath |> readAllText
            else
                filePath |> writeAllText defaultValue
                defaultValue

        let readOrCreateEmpty (filePath : string) =
            filePath |> readOrCreate ""

module HomeDirectory =
    let private homePath = Environment.GetFolderPath Environment.SpecialFolder.UserProfile
    let path = Path.Combine(homePath, App.Context.AppName)
    let private syntaxesPath = Path.Combine(path, "Syntaxes")
    let private scriptsPath = Path.Combine(path, "Scripts")

    let resolveStructure () =
        seq [homePath; syntaxesPath; scriptsPath]
        |> Seq.iter(IO.Directory.tryResolve >> ignore)

    module Syntaxes =
        let path = syntaxesPath

        open ICSharpCode.AvalonEdit.Highlighting

        let private resolveFile (fileName : string) =
            Path.Combine(syntaxesPath, fileName)
            |> IO.File.readOrCreateEmpty

        let readEditorSyntax (fileName : string) =
            Path.Combine(path, Path.ChangeExtension(fileName, ".xshd"))
            |> fun filePath ->
                use xshdFileStream = filePath |> File.OpenRead
                use xshdXmlReader = new XmlTextReader(xshdFileStream)
                Xshd.HighlightingLoader.Load(
                    xshdXmlReader,
                    ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance
                )

    module Scripts =
        let path = scriptsPath

        let private resolveFile (fileName : string) =
            Path.Combine(
                scriptsPath,
                Path.ChangeExtension(fileName, ".lua")
            )
            |> IO.File.readOrCreateEmpty

        module Init =
            let resolve () = $"init.lua" |> resolveFile