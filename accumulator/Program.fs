// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.Threading

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open CommandLine

/// コマンドラインオプション
type Options = {
    [<Option('p', "port", HelpText = "Port number")>] portNumber : uint16 option
    [<Option('h', "host", HelpText = "Host address or name")>] hostAddress : string option
}

/// タイムスタンプを取得する
let timestamp =
    // ロケールに対応するローカルのタイムゾーンを取得する。
    let zone = System.TimeZoneInfo.Local.GetUtcOffset(System.DateTime.Now)
    // UTC からの時差を計算し、+ または - のプレフィックスを取得する
    let prefix = if (zone < System.TimeSpan.Zero) then "-" else "+"
    // UTC および時差を計算し文字列で返す
    System.DateTime.UtcNow.ToString("yyyyMMddHHmmssffff") + prefix + zone.ToString("hhss")

/// HTTP ヘッダをフォーマットする
let formatHeaders (headers : (string * string) list) =
    sprintf "Headers : [\n"
    // (string * string) list （キー、値のタプル）のリストを 展開して、
    // (string * string) を連結し string （"Key : Value"）にする
    // ac = アキュームレーター
    // k  = ヘッダのキー
    // v  = ヘッダの値
    + List.fold (fun ac (k, v) -> ac + sprintf "    { %s : %s }\n" k v) "" headers
    + "]\n"

/// バイトストリームを UTF-8 文字列に変換する
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
/// Web サーバーのルートテーブル
/// ここパスと HTTP メソッドとどういう処理をさせるかのマッピングを記述する
let router =
    choose [
        // {hostname:port}/accumulate/ に対する POST を処理する
        // 標準出力に POST の内容を出力し、レスポンスに同様の内容を返す
        POST  >=> choose [
                    path "/accumulate" 
                    >=> request (fun r -> System.Console.WriteLine(format r); CREATED (format r))
                    ]
        // {hostname:port}/accumulate/ に対する PUT を処理する
        // 標準出力に PUT の内容を出力し、レスポンスに同様の内容を返す
        PUT   >=> choose [
                    path "/accumulate"
                    >=> request (fun r -> System.Console.WriteLine(format r); CREATED (format r))
                    ]
        // {hostname:port}/accumulate/ に対する PATCH を処理する
        // 標準出力に PATCH の内容を出力し、レスポンスに同様の内容を返す
        PATCH >=> choose [
                    path "/accumulate"
                    >=> request (fun r -> System.Console.WriteLine(format r); CREATED (format r))
                    ]
    ]

/// main
[<EntryPoint>]
let main argv =
    // 引数をパースする
    let result = Parser.Default.ParseArguments<Options>(argv)
    match result with
    | :? Parsed<Options> as parsed -> ()
    // パースに失敗した場合は、プログラムを終了する
    |  _  -> Environment.Exit(-1)
 
    // コマンドラインオプションを設定する 
    let options = (result :?> Parsed<Options>).Value
    let portNumber = match options.portNumber with
                     | Some x -> (int)x
                     | None   -> (int)8080
    let hostAddress = match options.hostAddress with
                      | Some x when x = "localhost" -> "127.0.0.1"
                      | Some x -> x
                      | None   -> "127.0.0.1"

    // Suave のコンフィグレーションを設定する                     
    let cts = new CancellationTokenSource()
    let config = {
        defaultConfig with
            // 非同期スレッドのキャンセレーショントークン
            cancellationToken = cts.Token
            // バインディングするアドレスとポート
            bindings = [ HttpBinding.createSimple HTTP hostAddress portNumber ]
    }
   
    // Suave を非同期で起動する 
    let listening, server = startWebServerAsync config router
    Async.Start(server, cts.Token)
    
    // 任意のキー入力で終了する 
    printfn "Press any key to exit"
    Console.ReadKey true |> ignore
   
    // キー入力された場合は、Async を中断する 
    cts.Cancel()

    // 正常終了でプログラムを終了する
    0