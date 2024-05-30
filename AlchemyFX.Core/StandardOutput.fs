module AlchemyFX.StandardOutput

open System.Text.RegularExpressions

let removeAnsiColorCodes (input: string) =
    Regex.Replace(
        input = input,
        pattern = "\x1B\[([0-9]{1,2}(;[0-9]{1,2})?)?[m|K]?",
        replacement = ""
    )

let clean (input: string) =
    input
    |> nullToDefault ""
    |> removeAnsiColorCodes