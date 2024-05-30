namespace AlchemyFX.Data.Model

open System

open LiteDB
open LiteDB.FSharp
open LiteDB.FSharp.Extensions

open AlchemyFX
open AlchemyFX.Data

module ApplicationDatabase =
    let [<Literal>] Name = "app.litedb"

[<CLIMutable>]
type Profile =
    {
        mutable Id : EntityId
        mutable Name : string
        mutable MainUrl : string
        mutable TaskUrl : string
        mutable TaskBody : string
        mutable Created : DateTime
    }
    static member Empty() =
        {
            Id = EntityId.Empty
            Name = ""
            MainUrl = ""
            TaskUrl = ""
            TaskBody = ""
            Created = DateTime.Now
        }

type IProfileRepository = inherit IRepository<Profile, EntityId>

type ProfileRepository(db : LiteDatabase) =
    inherit BasicRepository<Profile, EntityId>(db, "profiles")
    interface IProfileRepository
