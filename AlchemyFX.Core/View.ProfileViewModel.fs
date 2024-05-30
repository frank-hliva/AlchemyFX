namespace AlchemyFX.View

open System
open System.ComponentModel
open System.Collections.Generic
open AlchemyFX
open AlchemyFX.UI
open AlchemyFX.Data
open AlchemyFX.Data.Model
open AlchemyFX.View

type SortKind =
| Created = 0
| Name = 1

type ProccessingType<'entity> =
| Nothing
| New of 'entity
| Existing of 'entity

type ProccessingMode =
| Nothing = 0
| Create = 1
| Edit = 2

type ProfileViewModel(
    commands : GlobalCommandMediator,
    repository : Model.IProfileRepository,
    taskProfile : TaskProfile,
    taskMarkdownEditorViewModel : TaskMarkdownEditorViewModel
) as vm =
    inherit ViewModel(commands)

    let findProfileByName name = 
        if String.IsNullOrWhiteSpace name
        then Nothing
        else
            match repository.TryGet(fun profile -> profile.Name = name) with
            | Some profile -> Existing profile
            | _ -> Nothing

    let mutable currentProfile = taskProfile.Name |> findProfileByName

    let mapProfile mapper = 
        match currentProfile with
        | Existing profile -> profile |> mapper
        | New profile -> profile |> mapper
        | _ -> Profile.Empty() |> mapper

    let mutable profileName = mapProfile _.Name
    let mutable sortKind = SortKind.Created

    let mutable mainUrlBefore = mapProfile _.MainUrl
    let mutable taskUrlBefore = mapProfile _.TaskUrl
    let mutable taskBodyBefore = mapProfile _.TaskBody

    let resetFieldHistory (profile : Profile) =
        profileName <- profile.Name
        mainUrlBefore <- profile.MainUrl
        taskUrlBefore <- profile.TaskUrl
        taskBodyBefore <- profile.TaskBody

    let changeProfileUpdateStatus () =
        commands |> Cmds.trigger GlobalCommands.ChangeProfileUpdateStatus

    let profileChanged () =
        [
            nameof vm.CurrentProfile
            nameof vm.ProfileName
            nameof vm.Mode
            nameof vm.IsModified
            nameof vm.IsEditable
            nameof vm.MainUrl
        ] |> vm.OnPropertiesChanged

    let profilesChanged () =
        nameof vm.Profiles |> vm.OnPropertyChanged

    do (taskMarkdownEditorViewModel :> INotifyPropertyChanged).PropertyChanged.Add
        (fun event ->
            match event.PropertyName with
            | nameof vm.IsModified ->
                profileChanged()
            | _ -> ()
        )

    let setProfile (profile : Profile ProccessingType) =
        currentProfile <- profile
        let newProfile = mapProfile id
        profileName <- newProfile.Name
        taskProfile.Name <- profileName
        resetFieldHistory newProfile
        profileChanged ()

    let sortChanged () =
        [
            nameof vm.SortKind
            nameof vm.Profiles
        ] |> vm.OnPropertiesChanged

    let handleSaveTaskMarkdownEditorContent () =
        vm.SaveChanges()

    do commands.SubscribeCommand(
        GlobalCommands.SaveTaskMarkdownEditorContent,
        handleSaveTaskMarkdownEditorContent
    )

    let sort = function
    | SortKind.Name -> Seq.sortBy(fun p -> p.Name)
    | _ -> Seq.sortBy(fun p -> p.Created)

    member vm.Repository = repository

    member vm.Profiles with get() =
        repository.GetMany(fun x -> true)
        |> sort sortKind

    member vm.ProfileNameSet with get() : string Set =
        vm.Profiles
        |> Seq.map _.Name
        |> Set.ofSeq

    member vm.CurrentProfile with get() = mapProfile id

    member vm.ProfileName
        with get() = profileName
        and set value =
            profileName <- value
            profileChanged ()
            changeProfileUpdateStatus ()

    member vm.MainUrl
        with get() = vm.CurrentProfile.MainUrl
        and set value =
            vm.CurrentProfile.MainUrl <- value
            profileChanged ()
            changeProfileUpdateStatus ()

    member vm.Mode with get() =
        match currentProfile with
        | New _ -> ProccessingMode.Create
        | Existing _ -> ProccessingMode.Edit
        | _ -> ProccessingMode.Nothing

    member vm.IsEditable with get() =
        match currentProfile with
        | Nothing -> false
        | _ -> true

    member vm.SortKind 
        with get() = sortKind
        and set value =
            sortKind <- value
            sortChanged ()

    member vm.CreateNew() : Model.Profile =
        Profile.Empty()
        |> fun newProfile ->
            setProfile <| New newProfile
            profileChanged ()
            newProfile

    member vm.TryEdit(id : EntityId) =
        match repository.TryGet(fun p -> p.Id = id) with
        | Some profile ->
            setProfile <| Existing profile
            Ok profile
        | _ ->
            Error $"Profile with id: \"{id}\" not found"

    member vm.Edit(id : EntityId) =
        match vm.TryEdit id with
        | Ok profile -> profile
        | Error message ->
            message
            |> KeyNotFoundException
            |> raise

    member vm.Edit(profile : Model.Profile) = vm.Edit(profile.Id)

    member vm.Close() =
        setProfile Nothing

    member internal vm.Store(profile : Model.Profile) =
        profile.Name <- profileName
        resetFieldHistory profile
        repository.AddOrUpdate(profile) |> ignore
        profilesChanged ()
        profile

    member vm.IsValidProfileName(profileName) =
        (profileName |> vm.ProfileNameSet.Contains |> not) &&
        (profileName |> String.IsNullOrWhiteSpace |> not)

    member vm.IsModified with get() =
        let currentProfile = vm.CurrentProfile
        vm.IsEditable &&
        (
            (
                profileName <> currentProfile.Name &&
                vm.IsValidProfileName(profileName)
            ) ||
            taskMarkdownEditorViewModel.IsModified ||
            mainUrlBefore <> vm.MainUrl ||
            taskUrlBefore <> currentProfile.TaskUrl ||
            taskBodyBefore <> currentProfile.TaskBody
        )

    member vm.SaveChanges() =
        let currentProfile = vm.CurrentProfile;
        let id = currentProfile.Id;
        vm.Store(currentProfile) |> ignore
        vm.Close()
        if id = EntityId.Empty then
            repository.TryGet(fun p -> p.Name = currentProfile.Name)
            |> Option.iter(_.Id >> vm.Edit >> ignore)
        else vm.Edit(id) |> ignore

    member internal vm.Remove(id : EntityId) =
        repository.Remove(fun profile -> profile.Id = id) |> ignore
        profilesChanged ()
        if vm.IsEditable && vm.CurrentProfile.Id = id
        then vm.Close()

    member internal vm.Remove(profile : Model.Profile) = vm.Remove(profile.Id)

    member vm.Delete() =
        if vm.IsEditable
        then vm.Remove(vm.CurrentProfile)


type IProfileViewModelProvider =
    abstract ProfileViewModel : ProfileViewModel with get

module ProccessingMode =

    let toString = function
    | ProccessingMode.Create -> "Create"
    | ProccessingMode.Edit -> "Edit"
    | _ -> "Nothing"

    let toDisplayableString = function
    | ProccessingMode.Create -> "New"
    | ProccessingMode.Edit -> "Edit"
    | _ -> "None"