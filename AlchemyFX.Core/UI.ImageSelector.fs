namespace AlchemyFX.UI.ImageSelector

open System

type Choice() =
    member val Id = Guid.NewGuid() with get, set
    member val Name = "" with get, set
    member val Image = "" with get, set