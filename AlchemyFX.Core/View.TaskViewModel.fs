namespace AlchemyFX.View

open System
open AlchemyFX
open AlchemyFX.Data
open AlchemyFX.Data.Model
open System.ComponentModel

type TaskViewModel(
    globalCommands : GlobalCommandMediator,
    profileViewModel : ProfileViewModel
) as vm =
    inherit ViewModel(globalCommands)

    let triggerChangeProfileUpdateStatus () =
        globalCommands
        |> Cmds.trigger GlobalCommands.ChangeProfileUpdateStatus

    let changed () = 
        [
            nameof vm.TaskUrl
            nameof vm.TaskBody
            nameof vm.IsModified
        ] |> vm.OnPropertiesChanged

    let profileNotifPropertyChanged = profileViewModel :> INotifyPropertyChanged

    do profileNotifPropertyChanged.PropertyChanged.Add
        (fun event ->
            match event.PropertyName with
            | nameof profileViewModel.CurrentProfile
            | nameof profileViewModel.IsModified ->
                changed()
            | _ -> ()
        )        

    interface IProfileViewModelProvider with
        override vm.ProfileViewModel = profileViewModel

    member vm.IsModified
        with get() = profileViewModel.IsModified

    member vm.TaskUrl
        with get() = profileViewModel.CurrentProfile.TaskUrl
        and set value =
            profileViewModel.CurrentProfile.TaskUrl <- value
            changed ()
            triggerChangeProfileUpdateStatus()

    member vm.TaskBody
        with get() = profileViewModel.CurrentProfile.TaskBody
        and set value =
            profileViewModel.CurrentProfile.TaskBody <- value
            changed ()
            triggerChangeProfileUpdateStatus()