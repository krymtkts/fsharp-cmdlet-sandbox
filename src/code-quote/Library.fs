namespace code_quote

open System.Collections
open System.Management.Automation
open System.Text.RegularExpressions

open FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations
open System.Diagnostics

[<RequireQualifiedAccess>]
[<NoComparison>]
type Entry =
    | Obj of PSObject
    | Dict of DictionaryEntry

[<RequireQualifiedAccess>]
[<NoComparison>]
type Mode =
    | CodeQuotation
    | Lambda
    static member fromString(x: string) : Mode =
        match x with
        | "CodeQuotation" -> CodeQuotation
        | "Lambda" -> Lambda
        | _ -> failwith "Invalid mode"

[<RequireQualifiedAccess>]
[<NoComparison>]
type Operator =
    | And
    | Or
    static member fromString(x: string) : Operator =
        match x with
        | "And" -> And
        | "Or" -> Or
        | _ -> failwith "Invalid operator"

[<Cmdlet(VerbsCommon.Select, "ObjectTest")>]
[<OutputType(typeof<PSObject>)>]
type SelectObjectTestCommand() =
    inherit PSCmdlet()

    let matches (pattern: string) : string -> bool =
        try
            // NOTE: expect using cache.
            Regex(pattern, RegexOptions.None).IsMatch
        with
        | _ -> fun (_: string) -> true

    let conditions =
        [ matches "1"
          matches "b"
          matches "3"
          matches "d"
          matches "5" ]

    let buildExpr conditions op =
        conditions
        |> List.rev
        |> List.reduce (fun acc x -> fun e -> op (acc e) (x e))


    (*
        or condition
        if expr1 x then true else xxx
        if expr2 x then true else xxx
        if expr3 x then true else xxx
        if expr4 x then true else xxx
        if expr5 x then true else false

        and condition
        if expr1 x then xxx else false
        if expr2 x then xxx else false
        if expr3 x then xxx else false
        if expr4 x then xxx else false
        if expr5 x then true else false
    *)
    let generateExpr conditions op =
        let xVar = Var("x", typeof<string>)
        let x = xVar |> Expr.Var |> Expr.Cast<string>

        let combination =
            match op with
            | Operator.And -> fun c acc -> Expr.IfThenElse(<@ c %x @>, acc, <@ false @>)
            | Operator.Or -> fun c acc -> Expr.IfThenElse(<@ c %x @>, <@ true @>, acc)

        let rec recBody acc conditions =
            match conditions with
            | [] -> acc
            | condition :: conditions ->
                let acc = combination condition acc
                recBody acc conditions

        let body =
            match conditions |> List.rev with
            | [] -> <@@ true @@>
            | condition :: conditions ->
                let term = Expr.IfThenElse(<@ condition %x @>, <@ true @>, <@ false @>)
                recBody term conditions

        let lambda = Expr.Lambda(xVar, body)

        lambda
        |> LeafExpressionConverter.EvaluateQuotation
        :?> string -> bool

    let mutable test = fun _ -> false

    let getOperatorForLambda op =
        match op with
        | Operator.And -> (&&)
        | Operator.Or -> (||)

    [<Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)>]
    member val InputObject: PSObject [] = [||] with get, set

    [<Parameter>]
    [<ValidateSet("CodeQuotation", "Lambda")>]
    member val Mode = string Mode.Lambda with get, set

    [<Parameter>]
    [<ValidateSet("And", "Or")>]
    member val Operator = string Operator.And with get, set

    override __.BeginProcessing() =

        let w = Stopwatch()
        w.Start()

        match __.Mode |> Mode.fromString, __.Operator |> Operator.fromString with
        | Mode.CodeQuotation, o -> test <- generateExpr conditions o // NOTE: it takes over 100ms at first time.
        | Mode.Lambda, o -> test <- buildExpr conditions <| getOperatorForLambda o // NOTE: extremely fast.

        w.Stop()

        printfn $"expression generation: %d{w.ElapsedMilliseconds}ms"

    override __.ProcessRecord() =
        for io in __.InputObject do
            if io.ToString() |> test then
                io |> __.WriteObject

    override __.EndProcessing() = ()
