namespace AlchemyFX.View

open System
open AlchemyFX
open AlchemyFX.Data
open AlchemyFX.Data.Model
open System.ComponentModel

type ApplicationViewModel(
    globalCommands : GlobalCommandMediator,
    appContext : App.Context,
    profileViewModel : ProfileViewModel
) as vm =
    inherit ViewModel(globalCommands)

    let mutable subTitle = ""

    let changed () = 
        [
            nameof vm.AppName
            nameof vm.SubTitle
            nameof vm.Title
        ] |> vm.OnPropertiesChanged

    do (profileViewModel :> INotifyPropertyChanged).PropertyChanged.Add
        (fun event ->
            match event.PropertyName with
            | nameof profileViewModel.CurrentProfile -> changed()
            | _ -> ()
        )

    interface IProfileViewModelProvider with
        override this.ProfileViewModel = profileViewModel

    member vm.AppName = appContext.AppName

    member vm.SubTitle
        with get() = subTitle
        and set value =
            subTitle <- value
            changed()

    member vm.Title with get() =
        [vm.AppName; profileViewModel.CurrentProfile.Name; vm.SubTitle]
        |> List.filter (String.IsNullOrWhiteSpace >> not)
        |> String.concat " - "

type IApplicationViewModelProvider =
    abstract ApplicationViewModel : ApplicationViewModel with get