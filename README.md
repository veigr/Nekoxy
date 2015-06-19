Nekoxy
================

Nekoxy は、[TrotiNet](http://trotinet.sourceforge.net/) を使用した簡易HTTPローカルプロキシライブラリです。  
アプリケーションに組み込み、HTTP通信データを読み取る用途を想定しています。

### 機能

* 指定ポートでローカルプロキシを1つ起動
* 起動時にプロセス内プロキシ設定を適用
    * HTTPプロトコルのみに適用する
    * システムのプロキシ設定(インターネットオプションの設定)がある場合、それをアップストリームプロキシに適用 (設定されている全てのプロトコルに適用される)
* レスポンスデータをクライアントに送信後、AfterSessionComplete イベントを発行
* AfterSessionComplete イベントにてリクエスト/レスポンスデータを読み取り可能
* Transfer-Encoding: chunked なレスポンスデータは、TrotiNet を用いて予めデコードされる
* Content-Encoding 指定のレスポンスデータは、TrotiNet を用いて予めデコードされる
* アップストリームプロキシを設定可能
    * システムのプロキシ設定より優先して適用される

### 制限事項

* HTTPにのみ対応し、HTTPS等には対応しない
* 複数起動不可
* Transfer-Encoding: chunked なリクエストの RequestBody の読み取りは未対応
    * ResponseBody には対応
* Transfer-Encoding: chunked なレスポンスデータは、Content-Length 指定のデータに変更され、クライアントに送信される
* アップストリームプロキシの設定と環境によっては動作が遅くなる場合がある
    * TrotiNet は Dns.GetHostAddresses で取得されたアドレスを順番に接続試行するため、接続先によっては動作が遅くなる可能性があります。  
      例えば 127.0.0.1 で待ち受けている別のローカルプロキシに対して接続したい場合、localhost を指定するとまず ::1 へ接続試行し、その後 127.0.0.1 へアクセスするという挙動となり、動作が遅くなってしまうことが有ります。  
      これを回避するには、UpstreamProxyHostにホスト名ではなくIPアドレスで指定するといった手段が考えられます。

### 取得

* [NuGet Gallery](https://www.nuget.org/packages/Nekoxy/)から取得可能です。

### 依存ライブラリ

* [TrotiNet](http://trotinet.sourceforge.net/)  
TrotiNetはGNU Lesser General Public License v3.0で保護されています。
* [log4net](https://logging.apache.org/log4net/)  
log4netはApache License, Version 2.0([https://www.apache.org/licenses/LICENSE-2.0.txt](https://www.apache.org/licenses/LICENSE-2.0.txt))で保護されています。  
※Nekoxyは利用していないが、TrotiNetが依存している(参照のみ・再頒布なし)

### TrotiNet について

* Nekoxyは、TrotiNetを同梱し頒布しています。
* TrotiNetに改変は加えていません。
* TrotiNetはGNU Lesser General Public License v3.0で保護されています。
* 利用しているTrotiNetのソースは、TrotiNet-Srcフォルダに添付されています。
* GNU GENERAL PUBLIC LICENSE Version 3 および GNU LESSER GENERAL PUBLIC LICENSE Version 3 のライセンス文書のコピーは、TrotiNet-Srcフォルダに添付されています。

### Nekoxy のライセンス

* MIT License  
参照 : LICENSE ファイル

### 更新履歴

#### 1.1.0

* Start() メソッドで isSetIEProxySettings が true 時、アップストリームプロキシに WinHTTPGetIEProxyConfigForCurrentUser() で取得したシステム設定を用いるよう変更。  
UpstreamProxyHost プロパティはこの設定よりも優先される。
* システム設定にプロキシバイパス設定がある場合、そちらを利用するよう変更。

#### 1.0.3

* Start() 時に Listening 開始を待つよう修正
* IsInListening プロパティ公開
* WinInetUtil 公開


#### 1.0.1

* 初回リリース
