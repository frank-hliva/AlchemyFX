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
open System.Windows.Controls

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

    let mutable richTextSource = FlowDocument()
    let mutable isModified = false
    let mutable textEditor = None

    member vm.IsModified
        with get() = isModified
        and set value =
            if isModified <> value then
                isModified <- value
                nameof vm.IsModified |> vm.OnPropertyChanged

    member vm.TextEditor
        with get() = textEditor
        and set value =
            if textEditor <> value then
                textEditor <- value
                nameof vm.TextEditor |> vm.OnPropertyChanged

    abstract member LoadDocumentFromString : string -> bool
    default vm.LoadDocumentFromString (documentContent : string) =
        vm.IsModified <- false
        try
            if documentContent |> Text.isXaml then
                vm.RichTextSource <- FlowDocument.ofXaml documentContent
                true
            else


        with
        | _ ->
            textEditor.DocumentSession
            |> FlowDocumentSession.clear
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