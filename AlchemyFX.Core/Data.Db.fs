[<RequireQualifiedAccess>]
module AlchemyFX.Data.Db

open System
open LiteDB
open LiteDB.FSharp
open LiteDB.FSharp.Extensions

let collection<'e> (db : LiteDatabase) collectionName : LiteCollection<'e> =
    collectionName |> db.GetCollection<'e>