namespace AlchemyFX.Data

open System
open System.Linq.Expressions
open LiteDB
open LiteDB.FSharp
open LiteDB.FSharp.Extensions
open AlchemyFX

type EntityId = ObjectId

type PredicateExpr<'entity> = Expression<Func<'entity, bool>>

type IRepository<'entity, 'id> =
    abstract member CollectionName : string with get
    abstract member TryGet : predicate : PredicateExpr<'entity> -> 'entity option
    abstract member GetMany : predicate : PredicateExpr<'entity> -> 'entity seq
    abstract member Add : entity : 'entity -> 'id
    abstract member Update : entity : 'entity -> bool
    abstract member AddOrUpdate : entity : 'entity -> bool
    abstract member Remove : predicate : PredicateExpr<'entity> -> unit

[<AbstractClass>]
type BasicRepository<'entity, 'id>(db : LiteDatabase, collectionName : string) as repo =

    let collection () = Db.collection<'entity> db collectionName
    let repository = repo :> IRepository<'entity, 'id>

    interface IRepository<'entity, 'id> with
        override repo.CollectionName = collectionName

        override repo.TryGet (predicate : PredicateExpr<'entity>) =
            collection().FindOne(predicate)
            |> Optional.ofNullObject

        override repo.GetMany (predicate : PredicateExpr<'entity>) =
            collection().Find predicate

        override repo.Add (entity : 'entity) =
            collection().Insert entity
            |> _.AsObjectId
            |> cast<'id>

        override repo.Update entity =
            collection().Update entity

        override repo.AddOrUpdate entity =
            collection().Upsert entity

        override repo.Remove (predicate : PredicateExpr<'entity>) =
            collection().Delete(predicate) |> ignore