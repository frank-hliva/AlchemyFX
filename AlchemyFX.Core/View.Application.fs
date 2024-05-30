namespace AlchemyFX.View

[<AbstractClass>]
type ViewModel(
    commands : GlobalCommandMediator
) =
    inherit BasicViewModel()

    interface IGlobalCommandProvider with
        member this.Commands = commands

