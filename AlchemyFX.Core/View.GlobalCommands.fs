namespace AlchemyFX.View

open System
open AlchemyFX
open System.ComponentModel
open System.Collections.Generic

type GlobalCommands = 
| ShowConfigDialog
| ChangeProfileUpdateStatus

| LoadTaskMarkdownEditorContent
| SaveTaskMarkdownEditorContent

| OpenTaskPage

type GlobalCommandMediator() =
    inherit CommandMediator<GlobalCommands>()

and IGlobalCommandProvider =
    abstract Commands : GlobalCommandMediator with get

