namespace AlchemyFX.Data

open System
open System.Windows
open LiteDB
open LiteDB.FSharp
open LiteDB.FSharp.Extensions
open Unity
open Microsoft.Win32
open AlchemyFX
open AlchemyFX.UI
open AlchemyFX.Data

type AppStyles =
| ResizableWindow = 0
| ToolWindow = 1

module AppConfig =
    module Defaults =
        let [<Literal>] LocalhostRoot = "http://localhost:8887"
        let [<Literal>] BuildPath = "~/develop/repos/mis/sw/ims/ims4/ims4/Web/src/main/webapp/build"
        let [<Literal>] PackageManager = "npm"

        let [<Literal>] AppStyle = "1"
        let [<Literal>] StayOnTop = "1"

[<CLIMutable>]
type AppConfigProps = 
    {
        mutable LocalhostRoot : string
        mutable BuildPath : string
        mutable WSLDistro : string
        mutable PackageManager : string

        mutable AppStyle : AppStyles
        mutable StayOnTop : bool

        mutable IsFirstStart : bool
    }

type GetDefaultDistro = delegate of unit -> string

module Keys =
    let [<Literal>] MainUrl = "MainUrl"
    let [<Literal>] LocahlostRoot = "LocahlostRoot"
    let [<Literal>] BuildPath = "BuildPath"
    let [<Literal>] WSLDistro = "WSLDistro"

    let [<Literal>] ProfileName = "ProfileName"
    let [<Literal>] TaskUrl = "TaskUrl"

    let [<Literal>] PackageManager = "PackageManager"

    let [<Literal>] AppStyle = "AppStyle"
    let [<Literal>] StayOnTop = "StayOnTop"

    let [<Literal>] IsFirstStart = "IsFirstStart"

[<AbstractClass>]
type ApplicationConfig internal (source : LocationsStorage, props : AppConfigProps) =

    member c.LocalhostRoot
        with get () = props.LocalhostRoot
        and set (value) =
            source.SetValue(Keys.LocahlostRoot, value)
            props.LocalhostRoot <- value

    member c.BuildPath
        with get () = props.BuildPath
        and set (value) =
            source.SetValue(Keys.BuildPath, value)
            props.BuildPath <- value

    member c.WSLDistro
        with get () = props.WSLDistro
        and set (value) =
            source.SetValue(Keys.WSLDistro, value)
            props.WSLDistro <- value

    member c.PackageManager
        with get () = props.PackageManager
        and set (value) =
            source.SetValue(Keys.PackageManager, value)
            props.PackageManager <- value


    member c.AppStyle
        with get () = props.AppStyle
        and set (value) =
            source.SetValue(Keys.AppStyle, if value = AppStyles.ResizableWindow then "0" else "1")
            props.AppStyle <- value

    member c.StayOnTop
        with get () = props.StayOnTop
        and set (value) =
            source.SetValue(Keys.StayOnTop, if value then "1" else "0")
            props.StayOnTop <- value

    member c.IsFirstStart
        with get () = props.IsFirstStart
        and set (value : bool) =
            source.SetValue(Keys.IsFirstStart, if value then "1" else "0")
            props.IsFirstStart <- value

       

and AppConfig(source : LocationsStorage, getDefaultDistro : GetDefaultDistro) =
    inherit ApplicationConfig(
        source = source,
        props = {
            LocalhostRoot = source.GetValue(Keys.LocahlostRoot) |> emptyToDefault AppConfig.Defaults.LocalhostRoot
            BuildPath = source.GetValue(Keys.BuildPath) |> emptyToDefault AppConfig.Defaults.BuildPath
            WSLDistro = source.GetValue(Keys.WSLDistro) |> emptyToDefault (getDefaultDistro.Invoke())
            PackageManager = source.GetValue(Keys.PackageManager) |> emptyToDefault AppConfig.Defaults.PackageManager

            AppStyle = source.GetValue(Keys.AppStyle) |> emptyToDefault "0" |> Int32.Parse |> enum<AppStyles>
            StayOnTop = source.GetValue(Keys.StayOnTop) |> emptyToDefault "1" |> (=) "1"

            IsFirstStart = source.GetValue(Keys.IsFirstStart) |> emptyToDefault "1" |> (=) "1"
        }
    )

and [<CLIMutable>] TaskProfileProps = 
    {
        mutable Id : ObjectId
        mutable Name : string
        mutable MainUrl : string
        mutable TaskUrl : string
    }

and TaskProfile private (source : LocationsStorage, props : TaskProfileProps) =

    new (source : LocationsStorage) =
        TaskProfile(
            source = source,
            props = {
                Id = ObjectId.Empty
                Name = source.GetValue(Keys.ProfileName) |> emptyToDefault ""
                MainUrl = source.GetValue(Keys.MainUrl) |> emptyToDefault ""
                TaskUrl = source.GetValue(Keys.TaskUrl) |> emptyToDefault ""
            }
        )

    member c.Id
        with get () = props.Id

    member c.Name
        with get () = props.Name
        and set (value) =
            source.SetValue(Keys.ProfileName, value)
            props.Name <- value

    member c.MainUrl
        with get () = props.MainUrl
        and set (value) =
            source.SetValue(Keys.MainUrl, value)
            props.MainUrl <- value

    member c.TaskUrl
        with get () = props.TaskUrl
        and set (value) =
            source.SetValue(Keys.TaskUrl, value)
            props.TaskUrl <- value

and IBasicStorage =
    abstract member GetValue : key:string -> string
    abstract member GetValue : key:string * defaultValue:string -> string
    abstract member SetValue : key:string * value:string -> unit
    abstract member With : defaultStore:string -> IBasicStorage

and RegistryStorage(appName: string, defaultStore: string) =
    interface IBasicStorage with
        member s.GetValue(key: string) = s.ReadFromRegistry(defaultStore, key)
        member s.GetValue(key: string, defaultValue : string) =
            match s.GetValue(key) with
            | null -> defaultValue
            | value -> value
        member s.SetValue(key: string, value: string) = s.WriteToRegistry(defaultStore, key, value)
        member s.With(defaultStore: string) = RegistryStorage(appName, defaultStore)

    new(context: App.Context, defaultStore: string) = RegistryStorage(context.AppName, defaultStore)

    member s.GetValue(key: string) = (s :> IBasicStorage).GetValue(key)
    member s.GetValue(key: string, defaultValue:string) = (s :> IBasicStorage).GetValue(key, defaultValue)
    member s.SetValue(key: string, value: string) = (s :> IBasicStorage).SetValue(key, value)
    member s.With(defaultStore: string) = (s :> IBasicStorage).With(defaultStore)

    member private s.ReadFromRegistry(store: string, key: string) =
        let mutable value = null
        let appKey = Registry.CurrentUser.OpenSubKey(sprintf "SOFTWARE\\%s" appName)
        if appKey <> null then
            let valueKey = appKey.OpenSubKey(store)
            if valueKey <> null then
                value <- valueKey.GetValue(key)
                valueKey.Close()
            appKey.Close()
        value :?> string

    member private s.WriteToRegistry(store: string, key: string, value: string) =
        let appKey = Registry.CurrentUser.CreateSubKey(sprintf "SOFTWARE\\%s" appName)
        let valueKey = appKey.CreateSubKey(store)
        valueKey.SetValue(key, value)
        valueKey.Close()
        appKey.Close()
        
        

and WindowStorage(appContext : App.Context) =
    inherit RegistryStorage(appContext.AppName, "Window")

and LocationsStorage(appContext : App.Context) =
    inherit RegistryStorage(appContext.AppName, "Locations")