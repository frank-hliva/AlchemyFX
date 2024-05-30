[<RequireQualifiedAccess>]
module rec AlchemyFX.WSL

open System
open System.Diagnostics
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Text
open AlchemyFX
open AlchemyFX.Data
open AlchemyFX.UI

type IEnvironment =
    abstract Distro : string option with get
    abstract CommandPath : string option with get
    abstract DefaultShell : string option with get

type EnvironmentProps =
    {
        Distro : string option
        CommandPath : string option
        DefaultShell : string option
    }

and Environment(props : EnvironmentProps) =
    interface IEnvironment with
        override this.Distro with get() = props.Distro
        override this.CommandPath with get() = props.CommandPath
        override this.DefaultShell with get() = props.DefaultShell
    new(appConfig : AppConfig) =
        Environment({
            Distro = Some appConfig.WSLDistro
            CommandPath = Some appConfig.BuildPath
            DefaultShell = Some <| Distro.getDefaultShellProcess appConfig.WSLDistro
        })
    static member empty() =
        Environment({ Distro = None; CommandPath = None; DefaultShell = None  })

type EnvironmentConfigAdapter(appConfig : AppConfig) =
    let defaultShell = Distro.getDefaultShellProcess appConfig.WSLDistro
    interface IEnvironment with
        member this.Distro with get() = Some appConfig.WSLDistro
        member this.CommandPath with get() = Some appConfig.BuildPath
        member this.DefaultShell = Some defaultShell

module private IEnvironment =
    let internal toArgs (command : string) (props : IEnvironment) =
        match props.CommandPath with
        | Some commandPath -> $"cd {commandPath};{command}"
        | _ -> command
        |> fun command ->
            match props.Distro with
            | Some distro -> $"-d {distro} "
            | _ -> ""
            |> fun args -> 
                match props.DefaultShell with
                | Some defaultShell -> $"{args} -- {defaultShell} -l -c \"{command}\""
                | _ -> args + command

type DistroInfo =
    {
        Name : string
        Id : string
        IsDefault : bool
    }

module Command =
    let toProcess' (encoding : Text.Encoding) wslCommand =
        new Process(StartInfo = ProcessStartInfo(
            FileName = "wsl",
            Arguments = wslCommand,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        ))
        |> fun process' ->
            process'.StartInfo.StandardOutputEncoding <- encoding
            process'

    let toProcess wslCommand =
        toProcess' Encoding.Unicode wslCommand

module Process =
    let fetchTextOutput (proc : Process) =
        proc.Start() |> ignore
        let output = proc.StandardOutput.ReadToEnd()
        proc.WaitForExit()
        output

    let fetchTextOutputAsync (proc : Process) = async {
        proc.Start() |> ignore
        let! output = proc.StandardOutput.ReadToEndAsync() |> Async.AwaitTask
        do! proc.WaitForExitAsync() |> Async.AwaitTask
        return output
    }

    let fetchTextOutputNoWait (proc : Process) =
        proc.Start() |> ignore
        proc.StandardOutput.ReadToEnd()

module TextOutput =
    let parseLines : string -> _ = 
        _.ToLines()
        >> Seq.map(fun distro -> distro.Trim())
        >> Seq.filter(fun distro -> distro <> "")

let getDistros () =
    Command.toProcess "--list --all"
    |> Process.fetchTextOutput
    |> TextOutput.parseLines
    |> Seq.skip(1)
    |> Seq.map(fun distro ->
        let defaultTag = "(Default)"
        if distro.EndsWith(defaultTag) then
            let distro = distro.[0..distro.Length - (defaultTag.Length + 1)].Trim()
            { Name = distro; Id = distro; IsDefault = true }
        else { Name = distro; Id = distro; IsDefault = false }
    )

module Distro =

    let getDefaultShellProcess (distro : string) =
        $"-d {distro} echo $SHELL"
        |> Command.toProcess' Encoding.ASCII
        |> Process.fetchTextOutput
        |> TextOutput.parseLines
        |> Seq.last

    let getDefault () =
        getDistros ()
        |> Seq.pick(fun distro ->
            if distro.IsDefault
            then Some distro
            else None
        )

type Command(env : IEnvironment) =

    let outputDataReceivedEvent = new Event<_>()

    let outputDataReceived (type' : OutputType) (e : DataReceivedEventArgs) =
        outputDataReceivedEvent.Trigger
        <| CommandDataReceivedEventArgs(e.Data, type')

    member this.OutputDataReceived = outputDataReceivedEvent.Publish

    member this.Start(command : string) =
        let proc = new Process()
        let arguments = env |> IEnvironment.toArgs command
        proc.StartInfo <- ProcessStartInfo(
            FileName = "wsl",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        )
        
        outputDataReceived OutputType.OutputData
        |> proc.OutputDataReceived.Add
        outputDataReceived OutputType.ErrorData
        |> proc.ErrorDataReceived.Add

        proc.Start() |> ignore
        proc.BeginOutputReadLine() |> ignore
        proc.BeginErrorReadLine() |> ignore
        RunningCommand(command, arguments, proc)

    new() = Command(Environment.empty())

and Path (env : IEnvironment) =

    member p.SetDistro(distro : string option) =
        Path({
            new IEnvironment with
                member this.Distro with get() = distro
                member this.CommandPath with get() = env.CommandPath
                member this.DefaultShell with get() = env.DefaultShell
        })

    member p.SetDistro(distro : string) = p.SetDistro(Some distro)

    member p.SetCommandPath(commandPath : string option) =
        Path({
            new IEnvironment with
                member this.Distro with get() = env.Distro
                member this.CommandPath with get() = commandPath
                member this.DefaultShell with get() = env.DefaultShell
        })

    member p.SetCommandPath(commandPath : string) =
        p.SetCommandPath(Some commandPath)

    member p.SetDefaultShell(defaultShell : string option) =
        Path({
            new IEnvironment with
                member this.Distro with get() = env.Distro
                member this.CommandPath with get() = env.CommandPath
                member this.DefaultShell with get() = defaultShell
        })

    member p.SetDefaultShell(defaultShell : string) =
        p.SetCommandPath(Some defaultShell)

    member p.Exists(path : string) =
        env
        |> IEnvironment.toArgs $"ls {path}"
        |> Command.toProcess 
        |> fun process' ->
            process'.Start() |> ignore
            process'.WaitForExit()
            process'.ExitCode = 0

and [<AllowNullLiteral>] RunningCommand(
    command : string,
    fullCommand : string,
    proc : Process
) = 
    member this.Command = command
    member this.FullCommand = fullCommand
    member this.Process = proc
    member this.Kill() =
        proc.Kill()

and CommandDataReceivedEventArgs(data : string, receivedDataType : OutputType) =
    inherit EventArgs()
    member ea.Data : string = data
    member ea.OutputType : OutputType = receivedDataType

and OutputType =
| OutputData = 0
| ErrorData = 1