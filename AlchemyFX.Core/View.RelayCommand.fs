namespace AlchemyFX.View

open System
open System.Diagnostics
open System.Windows.Input

open System
open System.Diagnostics
open System.Windows.Input

[<AllowNullLiteral>]
type RelayCommand<'T>(execute: Action<'T>, canExecute: Predicate<'T> option) as rc =
    let event = new DelegateEvent<EventHandler>()
    do
        if execute = null then
            raise (new ArgumentNullException("execute"))

    member rc.CanExecuteChanged = event.Publish

    member rc.CanExecute arg =
        match canExecute with
        | Some canExecuteFunc ->
            arg
            |> unbox<'T>
            |> canExecuteFunc.Invoke
        | None -> true

    member rc.Execute arg =
        arg 
        |> unbox<'T>
        |> execute.Invoke

    interface ICommand with
        [<CLIEvent>]
        member i.CanExecuteChanged = rc.CanExecuteChanged
        member i.CanExecute arg = rc.CanExecute arg
        member i.Execute arg = rc.Execute arg

    new (execute: Action<'T>) = RelayCommand<'T>(execute, None)

type RelayCommand(execute: Action, canExecute: Func<bool> option) as rc =
    let event = new DelegateEvent<EventHandler>()
    do
        if execute = null then
            raise (new ArgumentNullException("execute"))


    member private rc.execute = execute
    member private rc.canExecute = canExecute

    member rc.CanExecuteChanged = event.Publish
    member rc.CanExecute arg =
        match canExecute with
        | Some canExecuteFunc -> canExecuteFunc.Invoke()
        | None -> true
    member rc.Execute arg = execute.Invoke()

    interface ICommand with
        [<CLIEvent>]
        member i.CanExecuteChanged = rc.CanExecuteChanged
        member i.CanExecute arg = rc.CanExecute arg
        member i.Execute arg = rc.Execute arg

    new (execute: Action) = RelayCommand(execute, None)

    new (inputRelayCommand: RelayCommand) =
        new RelayCommand(
            inputRelayCommand.execute,
            inputRelayCommand.canExecute
        )
