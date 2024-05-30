namespace AlchemyFX

open System
open AlchemyFX.Data
open System.Net

type MyWork =
    {
        Locations : Locations
    }

and Locations =
    {
        Source : Uri
        Localhost : Uri
        LocalhostWithoutQuery : Uri
        Space : Uri
        RunLocalhostCommand : string
        LocalhostLegacyCommand : string
        HostName : string
        SshCommand : string
        SshCopyIdCommand : string
    }
    static member ViewerChoices = [
        { Name = "Host"; Id = ViewerChoices.HostName }
        { Name = "SSH"; Id = ViewerChoices.SshCommand }
        { Name = "ssh-copy-id"; Id = ViewerChoices.SshCopyIdCommand }
    ]
    member locs.ViewByChoice(choice : ViewerChoices) =
        match choice with
        | ViewerChoices.HostName -> locs.HostName
        | ViewerChoices.SshCommand -> locs.SshCommand
        | ViewerChoices.SshCopyIdCommand -> locs.SshCopyIdCommand
        | _ -> ""

and ViewerChoices =
| HostName = 0
| SshCommand = 2
| SshCopyIdCommand = 3

and NamedViewerChoice =
    {
        Name : string
        Id : ViewerChoices
    }

type LocationBuilder(config : AppConfig) =

    let localhostRoot = "http://localhost:8887"

    let toLocalhostUri (mainUrl : Uri) = 
        $"{localhostRoot}{mainUrl.PathAndQuery}" |> Uri

    let toLocalhostWithoutQueryUri (mainUrl : Uri) = 
        $"{localhostRoot}{mainUrl.LocalPath}" |> Uri

    let toSpaceUri localhostUrl = 
        $"{localhostRoot}/ims/html2/admin/space.html" |> Uri

    let toRunLocalhostCommand (mainUrl : Uri) =
        $"{config.PackageManager} run dev --customer=all --proxyUrl={mainUrl.Scheme}{Uri.SchemeDelimiter}{mainUrl.Host}:{mainUrl.Port}{mainUrl.LocalPath} --requestCaching=true"

    let toRunLocalhostLegacyCommand (mainUrl : Uri) = 
        $"{config.PackageManager} run local-dev -- --url {mainUrl.Scheme}{Uri.SchemeDelimiter}{mainUrl.Host}:{mainUrl.Port}{mainUrl.LocalPath} --reload"

    let processHostName action (mainUrl : Uri) = 
        mainUrl.ToString()
        |> Location.tryToHostName
        |> Option.map action
        |> Option.defaultValue ""

    let toHostName = processHostName id
    let toSshCommand = processHostName (fun host -> $"ssh root@{host}")
    let toSshCopyIdCommand = processHostName (fun host -> $"ssh-copy-id root@{host}")

    member locs.GetAllLocationsFor(sourceUrl: Uri) =
        {
            Source = sourceUrl
            Localhost = sourceUrl |> toLocalhostUri
            LocalhostWithoutQuery = sourceUrl |> toLocalhostWithoutQueryUri
            Space = sourceUrl |> toSpaceUri
            RunLocalhostCommand = sourceUrl |> toRunLocalhostCommand
            LocalhostLegacyCommand = sourceUrl |> toRunLocalhostLegacyCommand
            HostName = sourceUrl |> toHostName
            SshCommand = sourceUrl |> toSshCommand
            SshCopyIdCommand = sourceUrl |> toSshCopyIdCommand
        }

    member locs.GetAllLocationsFor(sourceUrl: string) =
        sourceUrl
        |> Uri
        |> locs.GetAllLocationsFor