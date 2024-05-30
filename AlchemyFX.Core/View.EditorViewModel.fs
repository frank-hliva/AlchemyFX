namespace AlchemyFX.View

open System
open System.IO
open System.Xml
open System.Xml.Linq
open System.ComponentModel
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Text
open System.Windows
open System.Windows.Input
open System.Windows.Documents
open System.Windows.Media
open System.Windows.Markup

open AlchemyFX
open AlchemyFX.UI
open AlchemyFX.Data
open AlchemyFX.Data.Model
open AlchemyFX.UI.Editors
open MdXaml

type EditorViewModel(
    globalCommands,
    theme : ITheme
) =
    inherit ViewModel(globalCommands)

    let mutable plainTextSource = ""
    let mutable isModified = false

    let markdownEngine = Markdown()

    member vm.PlainTextSource
        with get() = plainTextSource
        and set value =
            if plainTextSource <> value then
                plainTextSource <- value
                vm.IsModified <- true
                nameof vm.PlainTextSource |> vm.OnPropertyChanged
                nameof vm.MarkdownOutputDocument |> vm.OnPropertyChanged

    member vm.MarkdownOutputDocument
        with get() =
            if
                plainTextSource = null ||
                String.IsNullOrWhiteSpace(plainTextSource)
            then ""
            else plainTextSource
            |> markdownEngine.Transform
            |> FlowDocument.withTheme theme

    member vm.IsModified
        with get() = isModified
        and set value =
            if isModified <> value then
                isModified <- value
                nameof vm.IsModified |> vm.OnPropertyChanged
                nameof vm.MarkdownOutputDocument |> vm.OnPropertyChanged

    abstract member LoadDocumentFromString : string -> bool
    default vm.LoadDocumentFromString(documentContent : string) =
        try
            vm.PlainTextSource <- documentContent
            vm.IsModified <- false
            true
        with
        | _ ->
            vm.PlainTextSource <- ""
            vm.IsModified <- false
            false

    abstract member SaveChanges : unit -> string
    default vm.SaveChanges() =
        if isModified then
            vm.IsModified <- false
        match plainTextSource with
        | null -> ""
        | _ -> plainTextSource
        
type TaskMarkdownEditorViewModel (
    commands : GlobalCommandMediator,
    theme : ITheme
) =
    inherit EditorViewModel (commands, theme)