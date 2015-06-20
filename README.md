Nekoxy
================

Nekoxy は、[TrotiNet](http://trotinet.sourceforge.net/) を使用した簡易HTTPローカルプロキシライブラリです。  
アプリケーションに組み込み、HTTP通信データを読み取る用途を想定しています。

### 機能

* 指定ポートでローカルプロキシを1つ起動
* 起動時にプロセス内プロキシ設定を適用可能 (デフォルト有効)
    * HTTPプロトコルのみに適用する
    * システムのプロキシ設定(インターネットオプションの設定 / WinHTTPGetIEProxyConfigForCurrentUser() から取得)がある場合、それをアップストリームプロキシに適用 (設定されている全てのプロトコルに適用される)
    * ただし Nekoxy で待ち受けているプロキシが設定されている場合はアップストリームプロキシに適用しない
* レスポンスデータをクライアントに送信後、AfterSessionComplete イベントを発行
* AfterSessionComplete イベントにてリクエスト/レスポンスデータを読み取り可能
* Transfer-Encoding: chunked なレスポンスデータは、TrotiNet を用いて予めデコードされる
* Content-Encoding 指定のレスポンスデータは、TrotiNet を用いて予めデコードされる
* アップストリームプロキシを設定可能
    * システムのプロキシ設定より優先して適用される

### 制限事項

* HTTPにのみ対応し、HTTPS等には未対応
* 複数起動不可
* Transfer-Encoding: chunked なリクエストの RequestBody の読み取りは未対応
    * ResponseBody には対応
* Transfer-Encoding: chunked なレスポンスデータは、Content-Length 指定のデータに変更され、クライアントに送信される
* アップストリームプロキシの設定と環境によっては動作が遅くなる場合がある
    * TrotiNet は Dns.GetHostAddresses で取得されたアドレスを順番に接続試行するため、接続先によっては動作が遅くなる可能性があります。  
      例えば 127.0.0.1 で待ち受けている別のローカルプロキシに対して接続したい場合、localhost を指定するとまず ::1 へ接続試行し、その後 127.0.0.1 へアクセスするという挙動となり、動作が遅くなってしまうことが有ります。  
      これを回避するには、UpstreamProxyHost プロパティにホスト名ではなくIPアドレスで指定するといった手段が考えられます。
* システムのプロキシ設定の自動構成には未対応 (動作未確認)

### 取得

* [NuGet Gallery](https://www.nuget.org/packages/Nekoxy/) から取得可能です。

### 依存ライブラリ

* [TrotiNet](http://trotinet.sourceforge.net/)  
TrotiNet は GNU Lesser General Public License v3.0 で保護されています。
* [log4net](https://logging.apache.org/log4net/)  
log4net は Apache License, Version 2.0([https://www.apache.org/licenses/LICENSE-2.0.txt](https://www.apache.org/licenses/LICENSE-2.0.txt)) で保護されています。  
※Nekoxy は利用していないが、TrotiNetが依存している(参照のみ・再頒布なし)

### TrotiNet について

* Nekoxy は、TrotiNet を同梱し頒布しています。
* TrotiNetに改変は加えていません。
* TrotiNetはGNU Lesser General Public License v3.0で保護されています。
* 利用しているTrotiNetのソースは、TrotiNet-Srcフォルダに添付されています。
* GNU GENERAL PUBLIC LICENSE Version 3 および GNU LESSER GENERAL PUBLIC LICENSE Version 3 のライセンス文書のコピーは、TrotiNet-Src フォルダに添付されています。

### Nekoxy のライセンス

* MIT License  
参照 : LICENSE ファイル

### 更新履歴

#### 1.1.1

* システムのプロキシ設定に Nekoxy で待ち受けているプロキシが設定されている場合、アップストリームプロキシに適用しないよう修正  
(アップストリームプロキシ設定に明示的に指定した場合は適用される)

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
