open System
open System.IO

module Time =

    let parseTime (input : string) =
        let month, day, hours, mins = input[0..1], input[2..3], input[4..5], input[6..7]
        let now = DateTime.Now
        DateTime(
            now.Year,
            month |> Int32.Parse,
            day |> Int32.Parse,
            hours |> Int32.Parse,
            mins |> Int32.Parse,
            0,
            DateTimeKind.Utc
        )

    let diffTime (input : DateTime) =
        input.AddMinutes(-13)

    let addTime (input : DateTime) =
        input.AddMinutes(15)

    let toString (input : DateTime) =
        sprintf "%02d%02d%02d%02d"
            input.Month
            input.Day
            input.Hour
            input.Minute

let now = DateTime.UtcNow
let time =
    now
    |> Time.diffTime
    |> Time.toString

sprintf $"expiration: {now.AddMinutes(2)}"
