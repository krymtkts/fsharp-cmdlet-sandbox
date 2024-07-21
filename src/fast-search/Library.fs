namespace ``fast-search``

open System
open System.Collections
open System.Management.Automation

type TrieNode =
    {
        mutable IsEndOfWord: bool
        Children: Concurrent.ConcurrentDictionary<char, TrieNode>
    }

module Trie =
    let createNode () =
        {
            IsEndOfWord = false
            Children = new Concurrent.ConcurrentDictionary<char, TrieNode>()
        }

    let insert (root: TrieNode) (word: string) =
        let rec insertRec (node: TrieNode) (index: int) =
            if index = word.Length then
                node.IsEndOfWord <- true
            else
                let c = word.[index]
                let childNode =
                    match node.Children.TryGetValue(c) with
                    | true, child -> child
                    | false, _ ->
                        let newNode = createNode()
                        node.Children.TryAdd(c, newNode) |> ignore
                        newNode
                insertRec childNode (index + 1)
        insertRec root 0

    let searchPrefix (root: TrieNode) (prefix: string) =
        let rec searchRec (node: TrieNode) (index: int) =
            if index = prefix.Length then Some node
            else
                match node.Children.TryGetValue(prefix.[index]) with
                | true, child -> searchRec child (index + 1)
                | false, _ -> None
        searchRec root 0

    let collectWords (node: TrieNode) =
        let rec collectRec (node: TrieNode) (prefix: string) (results: Generic.List<string>) =
            if node.IsEndOfWord then results.Add(prefix)
            for kvp in node.Children do
                collectRec kvp.Value (prefix + kvp.Key.ToString()) results
        let results =  Generic.List<string>()
        collectRec node "" results
        results |> Seq.toList

    let search (root: TrieNode) (pattern: string) =
        match searchPrefix root pattern with
        | Some node -> collectWords node
        | None -> []


[<Cmdlet(VerbsCommon.Find, "ObjectWithPattern")>]
[<OutputType(typeof<PSObject>)>]
type FindObjectWithPatternCommand() =
    inherit PSCmdlet()

    let root = Trie.createNode()

    [<Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)>]
    member val InputObject: PSObject [] = [||] with get, set

    [<Parameter(Position = 0, Mandatory = true)>]
    [<ValidateNotNull>]
    member val Pattern: string = "" with get, set

    override __.BeginProcessing() =
        printfn "==========Begin processing"

        printfn "==========Begin processing done"

    override __.ProcessRecord() =
        __.InputObject |> Array.iter (_.ToString() >> Trie.insert root)

    override __.EndProcessing() =
        let stopWatch = Diagnostics.Stopwatch()
        stopWatch.Restart()
        printfn "==========End processing"

        let result = Trie.search root __.Pattern
        result |> Seq.iter __.WriteObject

        stopWatch.Stop()
        printfn "==========End processing done %dms %d" stopWatch.ElapsedMilliseconds <| Seq.length result
