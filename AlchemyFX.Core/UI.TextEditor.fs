namespace rec AlchemyFX.UI.Editors

open System
open System.IO
open System.Text
open System.Windows
open System.Windows.Controls
open System.Windows.Documents
open System.Windows.Input
open AlchemyFX.UI

type TextEditor() as editor =
    inherit RichTextBox(
        AcceptsTab = true,
        AcceptsReturn = true
    )

    let textProcessEvent = new Event<TextProcessEventArgs>()

    let mutable skipUpdating = false

    let mutable documentSession =
        FlowDocumentSession.init editor editor.Document

    let mutable theme : ITheme option = None

    let contentChangedSubscription = 
        editor.TextChanged.Subscribe(editor.HandleContentChanged)

    let previewKeyDownSubscription =
        editor.PreviewKeyDown.Subscribe(editor.HandlePreviewKeyDown)

    let mutable textChangedSubscription =
        editor.TextChanged.Subscribe(editor.HandleTextChanged)

    let mutable textProcessorSubscriptions = ResizeArray<IDisposable>()

    static let TextSourceProperty =
        DependencyProperty.Register(
            "TextSource",
            typeof<string>,
            typeof<TextEditor>,
            PropertyMetadata(
                String.Empty,
                PropertyChangedCallback(TextEditor.OnTextChanged)
            )
        )

    static let IsModifiedProperty =
        DependencyProperty.Register(
            "IsModified",
            typeof<bool>,
            typeof<TextEditor>,
            PropertyMetadata(
                false,
                PropertyChangedCallback(TextEditor.OnIsModifiedChanged)
            )
        )

    member editor.DocumentSession with get() = documentSession

    member editor.TextProcess = textProcessEvent.Publish

    member editor.RegisterTextProcessor (textProcessor : ITextProcessor) =
        textProcessor.TextChanged
        |> editor.TextProcess.Subscribe 
        |> textProcessorSubscriptions.Add

    member private editor.HandleTextChanged (event : TextChangedEventArgs) =
        editor.IsModified <- true
        textChangedSubscription.Dispose()
        textProcessEvent.Trigger(TextProcessEventArgs(event, editor, documentSession))
        textChangedSubscription <- editor.TextChanged.Subscribe(editor.HandleTextChanged)

    member private editor.HandlePreviewKeyDown (event : KeyEventArgs) =
        match event.Key with
        | Key.Enter when Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ->
            event.Handled <- true
            ()
        | Key.Enter -> 
            event.Handled <- true
            let position = editor.CaretPosition
            let textRange = TextRange(position, position);
            textRange.Text <- "\r";
            editor.CaretPosition <- textRange.End
        | _ -> ()

    member editor.TextSource
        with get() = editor.GetValue(TextSourceProperty) :?> string
        and set(value) = editor.SetValue(TextSourceProperty, value)

    static member private OnTextChanged (dependencyObject : DependencyObject) (event : DependencyPropertyChangedEventArgs) =
        let richTextBox = unbox<TextEditor> dependencyObject
        event.NewValue
        :?> string
        |> richTextBox.UpdateDocument

    member private editor.HandleContentChanged (event : TextChangedEventArgs) =
        if editor.Document <> null then
            skipUpdating <- true
            editor.TextSource <- editor.Document |> FlowDocument.toText
            skipUpdating <- false

    member editor.Theme
        with get() = theme
        and set value =
            theme <- value
            documentSession <- documentSession |> FlowDocumentSession.withTheme theme

    member private editor.UpdateDocument (text : string) =
        if not skipUpdating && not (String.IsNullOrEmpty text) then
            FlowDocument.clear editor.Document |> ignore
            documentSession |> FlowDocumentSession.assignText text |> ignore

    member editor.IsModified
        with get() = editor.GetValue(IsModifiedProperty) :?> bool
        and set (value : bool) = editor.SetValue(IsModifiedProperty, value)

    static member private OnIsModifiedChanged (dependencyObject : DependencyObject) (event : DependencyPropertyChangedEventArgs) =
        let richTextBox = dependencyObject |> unbox<TextEditor>
        ()

    member editor.Dispose() =
        previewKeyDownSubscription.Dispose()
        contentChangedSubscription.Dispose()
        textChangedSubscription.Dispose()
        textProcessorSubscriptions |> Seq.iter(_.Dispose())
        editor.ClearValue(TextSourceProperty)


and TextProcessEventArgs(
    event : TextChangedEventArgs,
    textEditor : RichTextBox,
    documentSession : FlowDocumentSession
) =
    inherit EventArgs()
    member args.OriginalEvent = event 
    member args.TextEditor = textEditor
    member args.DocumentSession = documentSession

and ITextProcessor =
    abstract member TextChanged : TextProcessEventArgs -> unit