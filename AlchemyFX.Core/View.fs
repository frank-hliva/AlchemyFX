namespace AlchemyFX.View

open System
open AlchemyFX
open System.ComponentModel
open System.Collections.Generic

type CommandMediator<'cmdKey when 'cmdKey : equality>() =
    let subscribers = new Dictionary<'cmdKey, Action<obj> seq>()

    let getEntriesByKey key =
        subscribers.TryGet(key)
        |> function
        | Some entries -> entries
        | None -> Seq.empty

    member this.Subscribers = subscribers

    member this.SubscribeCommand(key: 'cmdKey, action : Action) =
        getEntriesByKey key
        |> fun entries ->
            subscribers.AddOrUpdate(
                key,
                seq {
                    yield! entries
                    let newAction (eventArg : obj) = action.Invoke()
                    yield newAction
                }
            ) |> ignore

    member this.SubscribeCommand(key: 'cmdKey, action : Action<'cmdArg>) =
        getEntriesByKey key
        |> fun entries ->
            subscribers.AddOrUpdate(
                key,
                seq {
                    yield! entries
                    let newAction (eventArg : obj) = 
                        eventArg |> unbox<'cmdArg> |> action.Invoke
                    yield newAction
                }
            ) |> ignore

    member this.TriggerCommand(key : 'cmdKey, arg : 'cmdArg) =
        if this.ContainsCommand key  then
            subscribers[key] |> Seq.iter(_.Invoke(arg))

    member this.TriggerCommand(key : 'cmdKey) =
        if this.ContainsCommand key then
            subscribers[key] |> Seq.iter(_.Invoke())

    member this.ContainsCommand(key : 'cmdKey) =
        subscribers.ContainsKey(key)

module Cmds =
    let create<'cmdKey when 'cmdKey : equality>() =
        CommandMediator<'cmdKey>()

    let trigger<'cmdKey when 'cmdKey : equality> key (mediator : 'cmdKey CommandMediator) =
        mediator.TriggerCommand(key)

    let triggerWithArg<'cmdKey, 'cmdArg when 'cmdKey : equality> key (arg : 'cmdArg) (mediator : 'cmdKey CommandMediator) =
        mediator.TriggerCommand(key, arg)

    let subscribe<'cmdKey when 'cmdKey : equality> key action (mediator : 'cmdKey CommandMediator) =
        mediator.SubscribeCommand(key, action)

    let subscribeWithArg<'cmdKey, 'cmdArg when 'cmdKey : equality> key (action : 'cmdArg Action) (mediator : 'cmdKey CommandMediator) =
        mediator.SubscribeCommand(key, action)

    let contains<'cmdKey when 'cmdKey : equality> key (mediator : 'cmdKey CommandMediator) =
        mediator.ContainsCommand(key)

[<AbstractClass>]
type BasicViewModel() as vm =
    let propertyChangedEvent = new Event<_, _>()

    let iNotifyPropertyChanged = vm :> INotifyPropertyChanged

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member vm.PropertyChanged = propertyChangedEvent.Publish

    member vm.INotifyPropertyChanged = iNotifyPropertyChanged

    member vm.PropertyChanged = iNotifyPropertyChanged.PropertyChanged

    abstract OnPropertyChanged : string -> unit
    default vm.OnPropertyChanged propertyName = 
        propertyChangedEvent.Trigger(
            vm,
            PropertyChangedEventArgs(propertyName)
        )

    abstract OnPropertiesChanged : string seq -> unit
    default vm.OnPropertiesChanged propertyNames = 
        propertyNames |> Seq.iter vm.OnPropertyChanged