namespace AlchemyFX.Data

open System
open LiteDB
open LiteDB.FSharp
open LiteDB.FSharp.Extensions
open System.Runtime.CompilerServices
open Unity

[<Extension>]
type UnityDataExtensions () =
    
    [<Extension>]
    static member inline RegisterLiteDatabase(serviceContainer : IUnityContainer, source : string) =
        new LiteDatabase(
            source,
            new FSharpBsonMapper()
        )
        |> serviceContainer.RegisterInstance<LiteDatabase>