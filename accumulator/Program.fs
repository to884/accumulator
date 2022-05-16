// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.Threading

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Logging
open CommandLine

/// コマンドラインオプション
type Options = {
    [<Option('p', "port", HelpText = "Port number")>] portNumber : int16 option
    [<Option('h', "host", HelpText = "Host name")>] hostName : string option
}

/// タイムスタンプを取得する
let timestamp =
    let zone = System.TimeZoneInfo.Local.GetUtcOffset(System.DateTime.Now)
    let prefix = if (zone<System.TimeSpan.Zero) then "-" else "+"
    System.DateTime.UtcNow.ToString("yyyyMMddHHmmssffff") + prefix + zone.ToString("hhss")

/// HTTP ヘッダをフォーマットする
let formatHeaders (headers : (string * string) list) =
    sprintf "Headers : [\n"
    // (string * string) list を 展開して、(string * string) を連結して string にする
    + List.fold (fun ac (l, r) -> ac + sprintf "    { %s : %s }\n" l r) "" headers
    + "]\n"

let bytesToString (bytes : byte[]) =
   Text.Encoding.UTF8.GetString(bytes)

/// HTTP POST のメッセージをフォーマットする    
let format (request : HttpRequest) =
    sprintf $"Timestamp : {timestamp}\n" 
    + $"Version : {request.httpVersion}\n"
    + $"Uri     : {request.url.ToString()}\n"
    + $"Path    : {request.rawHost}{request.rawPath}\n"
    + $"Method  : {request.rawMethod}\n"
    + formatHeaders request.headers
    + "Body : \"\"\"\n"
    + (bytesToString request.rawForm)
    + "\n\"\"\""

// let escape = string (char 0x18)

/// ルーティング
let router =
    // {hostname:port}/accumulater に対する POST を処理する
    POST >=> path "/accumulate"
         >=> request (fun r -> System.Console.WriteLine(format r); OK (format r))

/// main
[<EntryPoint>]
let main argv =
    // 引数をパースする
    let result = Parser.Default.ParseArguments<Options>(argv)
    match result with
    | :? Parsed<Options> as parsed -> Console.WriteLine(parsed.Value.ToString())
    |  _  -> failwith "Invalid Arguments"
 
    // コマンドラインオプションを設定する 
    let options = (result :?> Parsed<Options>).Value
    let portNumber = match options.portNumber with
                     | Some x -> (int)x
                     | None   -> (int)8080
    let hostName   = match options.hostName with
                     | Some x when x = "localhost" -> "127.0.0.1"
                     | Some x -> x
                     | None   -> "127.0.0.1"

    // Suave のコンフィグレーションを設定する                     
    let cts = new CancellationTokenSource()
    let config = {
        defaultConfig with
            cancellationToken = cts.Token
            bindings = [ HttpBinding.createSimple HTTP hostName portNumber ]
    }
   
    // Suave を起動する 
    let listening, server = startWebServerAsync config router
    Async.Start(server, cts.Token)
    
    // 任意のキー入力で終了する 
    printfn "Make requests now"
    Console.ReadKey true |> ignore
    
    cts.Cancel()
    
    0