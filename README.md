Nekoxy
================

Nekoxy は、[TrotiNet](http://trotinet.sourceforge.net/) を使用した簡易HTTPローカルプロキシライブラリです。  
アプリケーションに組み込み、HTTP通信データを読み取る用途を想定しています。

### 機能

* 指定ポートでローカルプロキシを1つ起動
* プロセス内プロキシ設定を適用可能
* レスポンスデータをクライアントに送信後、AfterSessionComplete イベントを発行
* AfterSessionComplete イベントにてリクエスト/レスポンスデータを読み取り可能
* Transfer-Encoding: chunked なレスポンスデータは、TrotiNet を用いて予めデコードされる
* Content-Encoding 指定のレスポンスデータは、TrotiNet を用いて予めデコードされる
* アップストリームプロキシを設定可能


### 制限事項

* HTTP にのみ対応し、HTTPS 等には未対応
    * HTTPS 読み書きは TrotiNet が非対応
    * CONNECT による単なる中継もやや動作が怪しいため
* 複数起動不可
* Transfer-Encoding: chunked なリクエストの RequestBody の読み取りは未対応
    * ResponseBody には対応
* Transfer-Encoding: chunked なレスポンスデータは、Content-Length 指定のデータに変更され、クライアントに送信される
    * 一旦 Nekoxy 内でデータをすべて受け取ってから下流に流すという動作になってしまうため、巨大なデータの受信には適さない
    * デコードせず下流に流しデータだけ読み取るのが理想だが、TrotiNet でそれをやるのは少々面倒そうなので絶賛放置中
    * 経路上での Transfer-Encoding のデコード自体は　RFC 7230 の 3.3.1　で認められている
* Transfer-Encoding は chunked 以外には対応していない
    * TrotiNet が非対応
    * gzip とかはそもそも Opera くらいしか対応してないっぽい？
* アップストリームプロキシの設定と環境によっては動作が遅くなる場合がある
    * TrotiNet は Dns.GetHostAddresses で取得されたアドレスを順番に接続試行するため、接続先によっては動作が遅くなる可能性がある。  
      例えば 127.0.0.1 で待ち受けている別のローカルプロキシに対して接続したい場合、localhost を指定するとまず ::1 へ接続試行し、その後 127.0.0.1 へアクセスするという挙動となり、動作が遅くなってしまうことがある。  
      これを回避するには、UpstreamProxyHost プロパティにホスト名ではなくIPアドレスで指定するといった手段が考えられる。


### プロセス内プロキシ設定

プロセス内のプロキシ設定を変更することで、システムのプロキシ設定を変更せずにプロセス内の通信を Nekoxy に向けることが出来るようになります。

既定では、HttpProxy.StartUp() 時に HTTP プロトコルのみ Nekoxy に向け、他プロトコルはその時に設定されているシステムのプロキシ設定を用いるよう適用します。  
isSetProxyInProcess 引数に false を指定した場合は適用されません。

HTTPプロトコル : client -> Nekoxy  
他プロトコル : client -> IE Settings Proxy


##### Nekoxy.WinInetUtil.SetProxyInProcessForNekoxy()

HttpProxy.StartUp() 時に用いているメソッドです。  
Nekoxy のポート番号を指定し、HTTP プロトコルのみ Nekoxy に向けるようプロセス内のプロキシ設定を変更します。

HTTP プロトコル以外はシステムのプロキシ設定を参照しますが、メソッド実行時の値が設定されるだけであり、リアルタイムには反映されない点に注意が必要です。


#### Nekoxy.WinInetUtil.SetProxyInProcess()

プロキシ設定とバイパスリストを指定して、プロセス内プロキシ設定を変更します。

e.g. `WinInetUtil.SetProxyInProcess("http=127.0.0.1:8888;https=127.0.0.1:8888", "local");`


### アップストリームプロキシ設定

HttpProxy.UpstreamProxyConfig を用いることで、Nekoxy を通った通信の上流プロキシを設定できます。
既定では、システムのプロキシ設定が適用されます。


##### Type プロパティの設定

* SystemProxy : システムのプロキシ設定を使用
    * WebRequest.GetSystemWebProxy().GetProxy() でリクエストURLに対応する接続先の解決を試みる
        * 自動構成も適用される
        * プロセス内プロキシ設定と異なり、リアルタイム反映
    * 何らかの原因でHTTPリクエストからリクエストURLが取得できなかった場合、自動構成を諦め WinHttpGetIEProxyConfigForCurrentUser() のプロキシ設定を適用する
    * システムのプロキシ設定に Nekoxy で待ち受けているプロキシが設定されている場合はアップストリームプロキシに適用せず、ダイレクトアクセスとなる
* SpecificProxy : 指定の上流プロキシを使用
    * SpecificProxyHost プロパティと SpecificProxyPort プロパティで指定された上流プロキシを用いる
    * SpecificProxyHost プロパティが空の場合はダイレクトアクセスとなる
* DirectAccess : ダイレクトアクセスを使用
    * 上流プロキシを用いず、直接サーバーと通信


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
* TrotiNet に若干の修正を施しています。
    * 修正元 commit : 4e3dd17ecebcf6cd94dbd335b6a9df6103143e2a
    * keep-alive 時、HTTP リクエストを中継する際にサーバー側コネクションがタイムアウトにより CLOSE_WAIT になっていると、クライアント側コネクションも破棄され HTTP リクエストが失敗する問題を修正
    * 一部の無視されている例外を WARN レベルログに出力するよう変更
* TrotiNet は GNU Lesser General Public License v3.0 で保護されています。
* 利用している TrotiNet のソースは、TrotiNet-Src フォルダに添付されています。
* GNU GENERAL PUBLIC LICENSE Version 3 および GNU LESSER GENERAL PUBLIC LICENSE Version 3 のライセンス文書のコピーは、TrotiNet-Src フォルダに添付されています。


### Nekoxy のライセンス

* MIT License  
参照 : LICENSE ファイル


### 更新履歴

#### 1.5.1

* TrotiNet を修正
    * keep-alive 時に HTTP リクエスト送信が失敗する場合がある問題を修正
    * 一部の無視されている例外を WARN レベルログに出力するよう変更

#### 1.5.0

* [破壊的変更] アップストリームプロキシの指定方法を変更

#### 1.4.0

* システムのプロキシ設定を適用する場合、自動構成も反映されるよう変更
* PathAndQuery プロパティの導出方法をちょっと変更

#### 1.3.1

* Session, HttpRequest, HttpResponse クラスの ToString() メソッドに BodyAsString も付加するよう変更
* HttpResponse の Body が null の場合、BodyAsString を参照すると例外が発生していた問題を修正

#### 1.3.0

* HttpProxy クラスに AfterReadRequestHeaders イベント、AfterReadResponseHeaders イベントを追加
    * Body 受信前に発生する
    * 主にデバッグ用途を想定
* Session, HttpRequest, HttpResponse クラスの ToString() メソッドを override し、リクエスト/ステータス ラインとヘッダを文字列化して取得できるよう変更

#### 1.2.1

* Content-Length も Transfer-Encoding も無い Connection: close なレスポンスを正常に受信できるよう修正

#### 1.2.0

* HttpProxy クラスに IsEnableUpstreamProxy プロパティを追加
    * アップストリームプロキシの有効/無効は、UpstreamProxyHost プロパティではなく IsEnableUpstreamProxy で行うよう変更が必要。

#### 1.1.2

* Startup() を呼ばずに Shutdown() を呼んだ場合、NullReferenceException が発生する問題を修正

#### 1.1.1

* システムのプロキシ設定に Nekoxy で待ち受けているプロキシが設定されている場合、アップストリームプロキシに適用しないよう修正  
(アップストリームプロキシ設定に明示的に指定した場合は適用される)

#### 1.1.0

* Start() メソッドで isSetIEProxySettings が true 時、アップストリームプロキシに WinHTTPGetIEProxyConfigForCurrentUser() で取得したシステム設定を用いるよう変更。  
UpstreamProxyHost プロパティはこの設定よりも優先される。 (その後 1.2.0 で動作変更)
* システム設定にプロキシバイパス設定がある場合、そちらを利用するよう変更。

#### 1.0.3

* Start() 時に Listening 開始を待つよう修正
* IsInListening プロパティ公開
* WinInetUtil 公開


#### 1.0.1

* 初回リリース
