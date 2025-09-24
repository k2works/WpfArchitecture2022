目次
Page
1
ソースコード
前提条件
想定読者
本稿の構成
認証アーキテクチャの注意点
Page 2
認証アーキテクチャの設計
認証ドメインからみた境界付けられたコンテキスト
認証ドメインからみたコンテキストマップ
認証の背景と要件
認証方式の選択
認証処理の概略
認証ドメインの論理ビュー設計
認証ドメインの実装ビュー設計
認証ドメインの配置ビュー
Page 3
例外処理アーキテクチャ
WPFの例外処理アーキテクチャ
gRPCの例外処理アーキテクチャ
Page 4
ロギングアーキテクチャ
ロギング要件
ログ出力レベル設計
ロギングアーキテクチャ概要
ロギングのドメインビュー
ロギングの論理ビュー
ロギング実装ビューの設計
ロギング配置ビューの設計
まとめ
ソースコード
　実際に動作するソースコードは、GitHub上に公開しているので、ぜひご覧ください。ビルドや実行方法については、リンク先のREADME.mdをご覧ください。また、実際に動作させるためには次の2つのライセンスが必要です。

ComponentOne for WPF
SPREAD for WPF 4.0J
　これらは試用ライセンスを発行することができます。

本稿だけで読み進められるように記載していますが、すべてのコードを詳細に解説しているわけではありません。本稿を読んだ後、あらためて動作させつつコードと本稿を読み比べていただければ、理解が深まるかと思います。

前提条件
　本稿はWPFアプリケーションのアーキテクチャ設計について記載したものです。すでに公開されている「見積編」および「設計編／前編」が前提となります。未読なものがあれば、そちらからまずはお読みください。

本稿にはサーバーサイドの設計も一部含まれていますが、見積編にも記載した通り、サーバーサイドについてはWPFアプリケーションを設計する上で、必要最低限の範囲に限定しています。サーバーサイドの実現方式は、オンプレ環境なのかクラウド環境なのか？ といった容易さなどで大きく変わってきます。そしてWPFアプリケーションから見た場合には本質的な問題ではありません。サーバーサイドまで厳密に記載すると話が発散し過ぎてしまい、WPFアプリケーションのアーキテクチャにフォーカスすることが難しくなるため、あくまで参考程度にご覧ください。

また本稿ではAdventureWorks社の業務のうち、発注業務システムのアーキテクチャとなります。特定業務は発注業務に限定されますが、認証などの複数の業務にまたがったアーキテクチャの実現についても言及しています。

本稿は以下の環境を前提に記載しています。

Visual Studio 2022 Version 17.4.0
Docker Desktop 4.14.0
Docker version 20.10.20
SQL Server 2022-latest(on Docker)
ComponentOne for WPF
SPREAD for WPF 4.0J
Test Assistant Pro 1.123
.NET 6.0.11
　本稿のサンプルは .NET 6で構築しますが、.NET Framework 4.6.2以上（.NET Standard 2.0水準以上）であれば同様のアーキテクチャで実現可能です。ただし一部利用しているパッケージのバージョンを当てなおす必要があるかもしれません。

想定読者
　次の技術要素の基本をある程度理解していることを想定しています。

C#
WPF
Docker
SQL Server
　これらの基本的な解説は、本稿では割愛しますが、知らないと理解できないという訳でもありません。

また下記の2つも概要が理解できていることが好ましいです。

Clean Architecture
ドメイン駆動設計（DDD）
　Clean Architectureについては、筆者のブログである「世界一わかりやすいClean Architecture」をあわせて読んでいただけると、本稿のアーキテクチャの設計意図が伝わりやすいかと思います。

ドメイン駆動設計の適用範囲については、本文内でも、つど解説いたします。

本稿の構成
　本章では非機能要件の中でも需要が高く、設計初期に共通で実施しておく必要のあるつぎの3つの非機能要件を例に、アーキテクチャを設計します。

認証アーキテクチャ
例外処理アーキテクチャ
ロギングアーキテクチャ
　ログ出力をする場合、一般的に利用者情報を付加することが多いと思います。そのため、先に認証処理を実現するとむだが少ないため、先に認証処理の設計を行うこととします。

認証アーキテクチャの注意点
　インターネットのように広く公開するサービスの場合は、適切な認証プロバイダーを選定してください。

本稿では、実際に皆さんにコードを動かしていただくことを想定して、外部サービスを利用しないで実現する方式を選定しています。企業内ネットワークのような限定された環境では必要十分な設計だと思いますが、オープンに公開するためには認証機能を独自開発するようなリスクは避けるべきでしょう。また仮に企業内ネットワークであっても、ほかに利用できる認証プロバイダーがあるのであれば、そちらの利用をご検討ください。

例えば、Azure Active Directoryのような製品を利用することを検討してください。

認証アーキテクチャの設計
　では認証アーキテクチャを設計していきましょう。それにあたり、いったん中心となるドメインを購買ドメインから認証ドメインに移します。さて、詳細の前に注意点があります。

以下の図が購買ドメインを設計してきた、現時点の境界付けられたコンテキストです。


これはあくまで、購買ドメインを中心に見たモデルです。そのため、購買ドメインにとって重要度が低い部分は、意図的に省略してきました。

ここからしばらくは、認証の設計をしていくため、認証ドメインを中心に設計します。認証ドメインは、購買・製造・販売それぞれから「汎用」ドメインとして共有されるドメインとなります。そのため、個別のドメインの中で設計するよりは、認証ドメインを独立した設計書として設計していくのが良いかと思います。

認証ドメインからみた境界付けられたコンテキスト
　さて、認証ドメインから境界付けられたコンテキストはつぎの通りです。


さすがに認証ドメイン視点とはいえ、認証ドメインがコアドメインになったりはせず、汎用ドメインのままでしょう。ただしAdventureWorks社は営利企業なので、販売が最優先だと判断し、販売ドメインをコアドメインとしました。

また、認証ドメインと他のドメインの関係に焦点を絞って設計しています。認証ドメインは、購買・製造・販売ドメインから共有される汎用ドメインで、それらから依存されています。

認証ドメインでは、例えばログインユーザーなどの文脈は、AdventureWorksコンテキストの文脈を利用します。そのためAdventureWorksドメインに依存します。認証処理では登録ユーザーを確認する必要があるでしょうから、汎用データベースドメインにも依存するでしょう。

このように視点が変わるとドメインの見え方は変わってきます。ドメインごとに境界付けられたコンテキストを定義することで、個々の境界付けられたコンテキストの複雑性を下げます。統合されたたった1つのモデルは、大きなドメインでは複雑すぎる場合があります。

また、すべての視点から正しい、たった1つのモデルを作ってメンテナンスし続けることは困難です。一定規模に分割して設計していくのが好ましいと考えています。

認証ドメインからみたコンテキストマップ
　さて、これらをコンテキストマップとして記載したものが次の通りです。


認証とデータベースがいずれも汎用コンテキストなので、それらとの関係は、カスタマー・サプライヤーとし、AdventureWorksとは共有カーネルの関係とします。

認証の背景と要件
　今回は、業務アプリケーションということで、つぎのような背景があるものとします。

利用者はAdventureWorks社の従業員である
利用者はWindowsドメイン参加しているWindows OSから利用する
利用者の勤務時間管理に、Windowsの起動・停止時間を利用している
十分な休憩を挟んで勤務しているか、停止から起動のインターバルを参照して管理している
　そのため、認証は非機能定義書において、つぎのような要件として定められているものとします。

Windows認証にてアカウントを特定する
特定されたアカウントが従業員として登録されていれば利用可能とする
APIの呼び出し時には、つど認証情報を検証する
認証の有効期限は24時間とする
期限を超えた場合、再認証する
再認証はアプリケーションの再起動で行う
　今回開発する対象は購買管理のシステムであり、日中の業務となり、日をまたいで継続した利用は通常運用では考えていません。また長時間認証された状態が維持されることはセキュリティ上好ましくありません。24時間は実用上長いですが、12時間では短く、その中間に適切な時間もないため24時間を有効期限とします。

認証方式の選択
　gRPCで利用可能な認証方式は、次のようなものがあります。

Azure Active Directory
クライアント証明書
Identityサーバー
JSON Web Token
OAuth 2.0
OpenID Connect
WS-Federation
　実はgRPCでは直接Windows認証は利用できません。これは、gRPCがプラットフォームに依存しないHTTP/2プロトコルとTLSに基づいているためです。これにより、Windows認証のような特定のプラットフォームに依存する認証メカニズムはサポートされません。

ただこれは、gRPCで直接Windows認証が行えないというだけで、Windows認証の併用ができない訳ではありません。

すこし「メタ」な話になりますが、本稿では認証基盤は本質的な課題ではありません。また誰もがすぐに動かして試せる範囲に収めたいため、Azure Active DirectoryやIdentityサーバーなどは避けたいです。そこで今回はJSON Web Token（JWT）を活用して、Windows認証の認証情報をgRPCで利用します。

JWTを利用した認証方式は、オンプレのような閉じた環境でも利用しやすく、gRPCと組み合わせやすい特徴があります。Windows認証を適用したREST APIでJWTを作成し、gRPCで利用することで、gRPCでもWindows認証で認証されたトークンを利用できます。

JSON Web Token（JWT）は、デジタル署名されたJSONデータ構造で、認証と認可情報を交換するために使用されます。JWTは3つのパートで構成されており、それぞれBase64Urlエンコードされた形式で、ドット（.）で区切られています。これらのパートは、ヘッダー、ペイロード、署名です。

txt
例：
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
パート	説明
ヘッダー（Header）	トークンのタイプと使用する署名アルゴリズムを指定します。
例: {"alg": "HS256", "typ": "JWT"}
ペイロード（Payload）	クレームと呼ばれる情報（ユーザーID、有効期限など）を含むJSONデータです。
例: {"sub": "1234567890", "name": "John Doe", "iat": 1516239022}
署名（Signature）	ヘッダーとペイロードを結合し、秘密鍵を使ってデジタル署名を生成します。これにより、トークンの内容が改ざんされていないことが検証できます。
　JWTを使用すると、クライアントとサーバー間で認証情報を安全かつ効率的に交換できます。ただし、機密データは含めないように注意してください。

認証処理の概略
　さてJWTはトークンの仕様であり、認証されたトークンが有効なものかどうかを検証する仕組みしか実はありません。どのようにWindows認証を行い、どうトークンを作成するかまでは含まれていません。

トークンはサーバーサイドで、秘密鍵を用いて何らかの方法で発行する必要があります。

ノード・コンポーネント・鍵の配置は、つぎのようになるでしょう。


そのうえで、つぎのように振る舞います。


購買アプリケーションは、起動時にRESTの認証APIを呼び出します。認証APIはRESTなので、Windows認証して呼び出し元の利用者のユーザーIDを取得できます。取得したユーザーIDを認証データベースと照らし合わせて、利用者情報を取得します。

データベースにユーザーが無事登録されていたら、利用者情報からトークンを作成して秘密鍵で署名して、トークンを購買アプリに返却します。購買アプリは、署名されたトークンをHTTPヘッダーに付与して購買APIを呼び出します。購買APIでは、トークンを公開鍵で複合して検証し、問題なければAPIの利用を許可します。

トークンには発行日時を含められるため、利用期限を設けることができますし、任意の情報を付与できるため、購買APIを利用するために必要なユーザー情報（例えば従業員IDなど）をトークンに含めておくこともできます。都度ユーザー情報を取得するようなオーバーヘッドを避けられます。

また購買API上だけでなく、購買アプリ上でも公開鍵を用いて復号することで、トークンに含まれた情報を利用できます。あたり前ですが、非常に良くできた仕組みですね。

認証ドメインの論理ビュー設計
　さて、これらの検討結果から、論理ビューを設計してみましょう。こんな感じでしょうか？


UserSerializerがレイアウトの都合上、右上の左下の2カ所に配置していますがご了承ください。

まず最外周にUIがありません。認証ドメインは、他のドメインに認証機能を提供するドメインのため、UIが存在しないからです。その代わりにWindows認証を行うためのWeb API（REST）があります。

また、ユースケースレイヤーもありません。認証ドメインにも「ユースケース（利用シーンの意味）」はあります。認証と検証ですね。ただ、ユースケース単位のオブジェクトは必要ないと考えたので、ユースケースレイヤーは利用しません。

こんな感じで、外周やレイヤーなど、必要に応じて取捨選択し、必要なものを追加します。クリーンアーキテクチャの「例の図」にあるレイヤー構成や構成要素に限定して考える必要はまったくありません。

レイヤー	オブジェクト	説明
AdventureWorksドメイン	User	システムの利用者
IUserRepository	Userのリポジトリー
認証ドメイン	IAuthenticationService	認証処理を実施し、IAuthenticationContextを初期化する
IAuthenticationContext	認証されたユーザーを扱う、認証コンテキスト
コントローラー・ゲートウェイ	UserSerializer	UserとJWTのシリアライズ・デシリアライズを行う
AuthenticationServiceClient	REST APIを呼び出して認証処理を行う
AuthenticationController	REST APIを提供し、Windows認証からユーザーを特定して利用者を認証する
ClientAuthenticationContext	IAuthenticationContextを実装した、gRPC用の認証コンテキスト。IAuthenticationContextと異なり、JWTトークンを保持して、gRPCを呼び出す際にサーバーサイドにトークンを渡す。
AuthenticationFilter	gRPCのクライアントを呼び出すと、必ず通過するフィルター。ClientAuthenticationContextからトークンを取得してヘッダーに付与することで認証情報をサーバーサイドに渡す。
ServerAuthenticationContext	IAuthenticationContextのgRPCサーバーサイド実装。クライアント側は1プロセス1ユーザーだが、サーバーサイドは1プロセスマルチユーザーのため、異なる実装が必要となる。
AuthenticationFilterAttribute	gRPCのサーバーサイドが呼び出された場合に必ず通過するフィルター。リクエストヘッダーからトークンを取得し、UserSerializerで複合することでユーザーの検証を行う。
　さきにも少し触れましたが、認証ドメインではざっくり言うと、認証と検証の2種類のユースケースがあります。認証はRESTで、検証はgRPCの利用時に行います。

そのため、上図のオブジェクトはつぎの2つのユースケースで考えると分かりやすいです。

REST APIによるユーザーの認証処理
gRPC利用時のユーザーの検証処理
　RESTで認証された際に作られたJWTを利用して、gRPCを呼び出し、正しく認証されたユーザーにだけgRPC APIの利用を許可します。順番に見ていきましょう。

REST APIによる認証処理
　RESTによる認証は、WPFアプリケーションの起動時に実施します。だいたいつぎのような流れになります。検証処理側は除外しています。


アプリケーションの起動時に、最初の画面のロードイベントで認証サービスを呼び出します。

認証は最初のスプラッシュ画面やローディング画面を表示した後に実施します。画面表示前に実施しておいて、認証情報をDIコンテナーに登録してしまうのがもっとも簡単です。ただその場合、初期画面の表示に時間が掛かってしまいます。そのため、初期画面を表示しておいて、初期画面で認証処理を行います。

ほかにはアプリケーション本体の画面とは別に、何らかの方法でスプラッシュを表示しておいて、認証し、認証情報をDIコンテナーに登録する方法もありだと思います。ただ今回は、初期画面で処理するようにしています。

では実際ながれを追っていきましょう。

①初期画面の遷移時にIAuthenticationServiceを呼び出します。

IAuthenticationServiceの実体はAuthenticationServiceで、HttpClientを利用して、②サーバーサイドの認証サービスを呼び出します。ここのAPIは前述のとおりRESTです。

サーバーサイドではAuthenticationControllerがリクエストを受け取り、③Windows認証を使ってアカウントを特定し、④IUserRepositoryを利用して、アカウントに対応する適切なUserか判定します。


適切なユーザーであれば、⑤UserSerializerを利用して認証されたUserをJSON Web Token（JWT）にシリアライズします。この時秘密鍵で署名することで、公開鍵でトークンが正しいものであることを検証できるようにします。JWTはレスポンスとして返却します。

AuthenticationServiceではレスポンスからトークンを受け取り、⑥トークンを復元してユーザー情報をClientAuthenticationContextへ設定します。

gRPC利用時のユーザーの検証処理
　さて、続いてはアプリケーション操作時にgRPCを呼び出した際の検証処理です。


①ユーザーが購買アプリケーションで何らかの操作をすると、ViewModelはgRPCのクライアント経由でサーバーサイドを呼び出します。

この時、gRPCクライアントにAuthenticationFilterを適用して②JWTトークンをHTTPヘッダーに付与します。

gRPCのサーバーサイドでは、AuthenticationFilterAttributeが受け取ったリクエストのヘッダーのauthorizationからJWTを取得したります。取得したトークンを③UserSerializer.Deserializeをつかって複合します。

ただしく複合できたら、④ServerAuthenticationContextに設定することで、以後必要に応じて利用します。

概ね悪くなさそうですね。では実際にコードを書きつつ、実装ビューを作って詳細に設計を落としていきましょう。

認証ドメインの実装ビュー設計
　では、先ほどのオブジェクトをコンポーネント単位に振り分けてみましょう。


こんな感じでしょうか？

認証ドメインのトップレベルのオブジェクトであるIAuthenticationServiceとIAuthenticationContextをAdventureWorks.Authenticationコンポーネント（つまりVisual Studioのプロジェクト）とします。

認証はJSON Web Token（JWT）で実現します。そのため署名・復元をおこなうUserSerializerをAdventureWorks.Authentication.Jwtコンポーネントに配置します。

JWTのクライアント側の実装となるAuthenticationServiceとClientAuthenticationContextをAdventureWorks.Authentication.Jwt.Clientに、サーバー側実装となるAuthenticationControllerをAdventureWorks.Authentication.Jwt.Serverに配置します。

WPFのケースでも説明しましたが、ホスティングに関する実装はそれだけに分離したいため、ASP.NET CoreのエントリーポイントとなるProgramクラスはAdventureWorks.Authentication.Jwt.Hosting.Restプロジェクトに置きました。


論理ビューとの対比はこんな感じ。特に抜け漏れはなさそうです。つづいて、リモートのビジネスロジックを呼び出して検証する側を見ていきましょう。


検証側はMagicOnionを利用したgRPC呼び出しになります。gRPCでJWTを利用するために、クライアント側でトークンをHTTPヘッダーに登録するAuthenticationFilterは、AdventureWorks.Authentication.MagicOnion.Clientに配置しました。同様にサーバーサイドで検証を行うAuthenticationFilterAttributeは、AdventureWorks.Authentication.MagicOnion.Serverに配置しました。検証時のシーケンスはこんな感じで、UserSerializerは1つだけ配置したので少しレイアウトが違いますが、だいたい同じようになりました。

もう一度全体を眺めてみましょう。


依存関係に循環もなく、コンポーネント間も基本的にインターフェイスベースの結合となっていて、悪くなさそうです。では本当に問題ないか、仮実装しながら設計を検証していきましょう。

認証処理の実装による検証
　まずはアプリケーション起動直後の認証処理です。MainWindowのViewModelに認証処理を組み込んで、認証が通ればメニューを表示するように実装します。

そのため、IAuthenticationServiceと画面遷移を提供するIPresentationServiceをDIコンテナーから注入します。

cs
private readonly IAuthenticationService _authenticationService;
private readonly IPresentationService _presentationService;

public MainViewModel(
[Inject] IAuthenticationService authenticationService,
[Inject] IPresentationService presentationService)
{
_authenticationService = authenticationService;
_presentationService = presentationService;
}
　KamishibaiではViewModelにDIコンテナーから注入したいオブジェクトにはInject属性を宣言する仕様となっています。

Kamishibaiでは型安全かつNullableを最大限活用して画面遷移パラメーターを渡せるように、コンストラクターで受け取れるようになっています。その際に、画面遷移パラメーターと、DIコンテナーから注入するオブジェクトを区別するために、注入する側にInject属性を付与する仕様になっています。

とはいえここでは、Kamishibaiのお作法は「だいたいこんなものかな」という理解で問題ありません。

その上で、MainWindowの画面遷移完了後に認証処理を呼び出します。

cs
public class MainViewModel : INavigatedAsyncAware
{
・・・

    public async Task OnNavigatedAsync(PostForwardEventArgs args)
    {
        var result = await _authenticationService.TryAuthenticateAsync();
        if (result.IsAuthenticated)
        {
            await _presentationService.NavigateToMenuAsync();
        }
        else
        {
            _presentationService.ShowMessage(
                Purchasing.ViewModel.Properties.Resources.AuthenticationFailed,
                Purchasing.ViewModel.Properties.Resources.AuthenticationFailedCaption,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            // アプリケーションを終了する。
            Environment.Exit(1);
        }
    }
}
　Kamishibaiでは画面遷移後に処理を行いたい場合、INavigatedAsyncAwareを実装し、OnNavigatedAsyncで通知をうけます。

コンストラクターから注入したIAuthenticationServiceのTryAuthenticateAsyncを呼び出してユーザーを認証し、認証エラーとなった場合、アラートを表示してアプリケーションを終了します。

ViewModel上の処理は問題なさそうです。

ではTryAuthenticateAsyncの実装を確認しましょう。

cs
// Windows認証を有効化したHTTPクライアント
private static readonly HttpClient HttpClient = new(new HttpClientHandler { UseDefaultCredentials = true });
private readonly ClientAuthenticationContext _context;  // DIコンテナーから注入する
private readonly Audience _audience;                    // DIコンテナーから注入する

public async Task<AuthenticateResult> TryAuthenticateAsync()
{
try
{
// 環境変数からAPIのエンドポイントを取得する。
var baseAddress = Environments.GetEnvironmentVariable(
"AdventureWorks.Authentication.Jwt.Rest.BaseAddress",
"https://localhost:4001");
// 認証処理を呼び出す。
var token = await HttpClient.GetStringAsync($"{baseAddress}/Authentication/{_audience.Value}");
// トークンを受け取って複合し、結果をAuthenticationContextへ設定する。
_context.CurrentTokenString = token;
_context.CurrentUser = UserSerializer.Deserialize(token, _audience);
return new(true, Context);
}
catch
{
return new(false, Context);
}
}
　APIのベースアドレス（https://foo.co.jp など）は、実運用や各種テスト環境、実装環境すべてで異なります。その問題を解決する何らかの方法が必要で、個人的には環境変数を好んでいます。設定ファイルに記述した場合、ビルドしたモジュールに含まれる設定ファイルを環境別に書き換える必要があるため、トラブルになりがちだからです。

"AdventureWorks.Authentication.Jwt.Rest.BaseAddress"が環境変数の名称になります。環境変数名と一緒にデフォルト値を渡しています。開発時は、クローンしてビルドしただけで、そのまま実行できることが好ましいです。そのため、開発環境は環境変数がない前提でデフォルト値を渡しています。

認証APIには引数としてAudienceを渡しています。JWTのaudience（audクレーム）は、トークンの受信者を特定するために使用されます。購買ドメインでは、購買APIサービスを呼び出します。この購買APIサービスがトークンの受信者になります。そのため認証時に購買APIサービスのAudienceを渡します。

認証が正しくおこなわれたら、DIコンテナーから注入されたClientAuthenticationContextにユーザー情報を反映します。ClientAuthenticationContextはシングルトンにして、認証情報を必要とする箇所でシングルトンインスタンスを注入して利用します。

ではサーバー側のコードを確認してみましょう。

cs
private readonly IUserRepository _userRepository;

[HttpGet("{audience}")]
public async Task<string> AuthenticateAsync(string audience)
{
var account = User.Identity!.Name!;
var user = await _userRepository.GetUserAsync(new LoginId(account));
　ASP.NET Coreでは、Windows認証を有効にしておくと「User.Identity!.Name!」から、簡単に呼び出し元のWindowsアカウントを特定できます。アカウントを取得したら、IUserRepositoryインターフェイル経由でUserRepositoryを呼び出してUserオブジェクトを取得することでユーザーを認証します。

IUserRepositoryの実装クラス、UserRepositoryの実装を見てみましょう。

cs
public async Task<User?> GetUserAsync(LoginId loginId)
{
using var connection = _database.Open();

        const string query = @"
select
EmployeeId
from
AdventureWorks.vUser
where
LoginId = @LoginId
";
return await connection.QuerySingleOrDefaultAsync<User>(
query,
new
{
LoginId = loginId
});
}
　一般的なDapperの実装です。定数定義されたクエリーを実行し、実行結果をDapperを利用して自動的にUserオブジェクトに値を設定します。もう少し深堀して見てみましょう。Userクラスの中身を見てみましょう。

cs
public record User(EmployeeId EmployeeId);

[UnitOf(typeof(int))]
public partial struct EmployeeId{}
　Userはrecord型のオブジェクトで、ドメイン駆動型設計のエンティティに該当します。UserはメンバーにEmployeeIdを持っています。EmployeeIdは構造体で、ドメイン駆動設計のバリューオブジェクトに該当します。EmployeeIdはint型で扱うこともできるのですが、IDの取り違いはありがちな不具合を発生しがちです。

つぎのコードはEmployeeIdとProductIdをintであつかった時のサンプルコードです。

cs
public record ProductOrder(int ProductId, int EmployeeId);

public void Order(int employeeId, int productId)
{
var productOrder = new ProductOrder(employeeId, productId);
　ProductOrderにProductIdとEmployeeIdを渡していますが、順序が逆になってしまっています。そしてこのコードはコンパイルが通ってしまいます。

もちろん適切なテストがあれば、いずれかのタイミングで気が付きます。しかしデータベースから値を取得したときに、テストデータの初期値がどちらも1だと、気が付くのが遅くなってしまうこともあります。

ではIDをバリューオブジェクトとして扱った場合はどうなるでしょうか？

cs
public record ProductOrder(ProductId ProductId, EmployeeId EmployeeId);

public void Order(EmployeeId employeeId, ProductId productId)
{
var productOrder = new ProductOrder(employeeId, productId);
}
　このコードはコンパイルエラーになるので、実装時に即座にエラーに気が付きますし、そもそもIDEが適切なコードをアシストしてくれるかもしれません。

私は、開発上で最初のテストはコンパイルであると思っています。コンパイルはもっともはやく、必ず実行され、そしてテストを間違いません。そのため実装スタイルと一番重要な鉄則の1つに「不具合をコンパイラーが捕捉できるコードを優先する」があると思っていて、IDをバリューオブジェクトとして扱うことは、ベストプラクティスの1つだと思っています。

さてEmployeeIdをもう一度見てみましょう。

cs
[UnitOf(typeof(int))]
public partial struct EmployeeId{}
　UnitOf属性が付与されていることが見て取れますが、バリューオブジェクトの実装にはUnitGeneratorライブラリを利用します。

IDはもっとも単純なバリューオブジェクトですが、金額や重量のような計算をともなう場合は、実装が複雑になりがちです。UnitGeneratorは非常によく考えられたライブラリで、ドメイン駆動設計を強力にサポートしてくれるのでオススメです。

さて、実は下記のDapperを利用したコードはこのままでは動作しません。

cs
return await connection.QuerySingleOrDefaultAsync<User>(
query,
new
{
LoginId = loginId
});
　EmployeeIdをDapperが解釈できないからです。そのため、つぎのようなTypeHandlerを用意してあげる必要があります。

cs
public class EmployeeIdTypeHandler : SqlMapper.TypeHandler<EmployeeId>
{
public override void SetValue(IDbDataParameter parameter, EmployeeId value)
{
parameter.DbType = DbType.Int32;
parameter.Value = value.AsPrimitive();
}

    public override EmployeeId Parse(object value)
    {
        return new EmployeeId((System.Int32)value);
    }
}
　UnitGeneratorではこのTypeHandlerを次のように宣言するだけで実装できます。

cs
[UnitOf(typeof(int), UnitGenerateOptions.DapperTypeHandler)]
public partial struct EmployeeId
{
}
　よくできていますね。よくできているんですが、UnitGenerator側ではなくて、システム全体のアーキテクチャとしては少し問題があります。全体の構造を見てみましょう。


EmployeeIdはUserオブジェクトと同じようにAdventureWorksコンポーネントに配置されます。そのため、上記のように宣言的にTypeHandlerを実装しようとした場合、AdventureWorksがDapperに依存してしまいます。

もちろん、アーキテクチャ的な決断として、AdventureWorksがDapperに依存するのを受け入れるという判断もあります。

ただ個人的にはあまり好みではありません。というのは、AdventureWorksがDapperに依存してしまった場合、Dapperのバージョンを上げないといけないとなったときに、ほぼすべてのドメインが影響を受けてしまうからです。Dapperのバージョンを気軽に上げるということが、かなわなくなります。

ではUnitGeneratorは良いのか？というと、受け入れられる範囲だと思っています。UnitGeneratorは、Valueオブジェクトを生成するライブラリという側面ではすでに完成されていて、なんならバージョンはほぼ永久的に固定することができそうです。またUnitGeneratorはValueオブジェクトのコードを自動生成しているだけなので、問題があれば手動での実装に切り替えても支障がありません。

少しメタなことをいうと、その方がアーキテクチャ的に複雑なので、ここで解説するためという意図もあります。

そのため本稿ではそこは妥協せず、TypeHandlerを作成して、AdventureWorks.SqlServer側に配置することとしました。


AdventureWorksにEmployeeIdを、AdventureWorks.SqlServerにEmployeeIdTypeHandlerを配置しました。

EmployeeIdTypeHandlerの実装ですが、UnitGeneratorで宣言的に解決しないとなると、自ら実装しなくてはなりません。すべてのValueObjectに対して実装するのはそれなりに手間なので、コード生成形の手段で解決したいところです。

今回はT4 Templateを利用して、次のように解決することにしました。

cs
<#
var @namespace = "AdventureWorks.SqlServer";
var types = new []
{
(UnitName: "EmployeeId", UnitType: typeof(int)),
・・・
};
#>

<#@ include file="..\AdventureWorks.Database\DapperTypeHandlers.t4" once="true" #>
　T4の詳細は割愛します。少し古い仕組みですが、C#でもっとも簡単に利用できるコード生成手段です。生成されたコードがバージョン管理できるところが、個人的には結構好きです。

コード生成の実態は、includeディレクティブで指定しているDapperTypeHandlers.t4側にあります。これを共有することでTypeHandlerの品質と生産性を確保します。実体は直接GitHubでコードをみてみてください。

さて、忘れないうちに購買ドメインの配置ビューも更新しておきましょう。


UnitGeneratorはサーバーサイド、クライアントサイドのどちらにも配置されます。これでDapperを利用してエンティティやバリューオブジェクトを直接利用できるようになりました。ということで、認証処理側に戻りましょう。

cs
[HttpGet("{audience}")]
public async Task<string> AuthenticateAsync(string audience)
{
var account = User.Identity!.Name!;
var user = await _userRepository.GetUserAsync(new LoginId(account));
if (user is null)
{
throw new AuthenticationException();
}

    // ここで本来はuserとaudienceを照らし合わせて検証する

    // 認証が成功した場合、ユーザーからJWTトークンを生成する。
    return UserSerializer.Serialize(user, Properties.Resources.PrivateKey, new Audience(audience));
}
　IUnitRepositoryからUserを取得して、取得できなかった場合、ユーザーとして登録されていないため、認証エラーとします。その後、何らかの形でuserとaudienceを照らし合わせて、audienceを利用できるか検証（認可）します。

ユーザーとオーディエンスの情報がそろうことで、ユーザーの特定だけでなく、そのユーザーが対象のオーディエンスを利用できるかどうか、認可することが可能になります。秘密鍵で署名することで、認証情報を持ったJSON Web Token（JWT）を作成します。

JWTには任意の情報を詰めることができますが、あまり情報を詰めすぎると、gRPCの呼び出し時に通信量が増えてしまいます。今回はJWTには従業員IDだけ詰めることにしましたが、ロールのような権限情報を付与しても良いと思います。

さてこれで、サーバーサイドの処理が終わったのでクライアント側に戻ります。

cs
public async Task<AuthenticateResult> TryAuthenticateAsync()
{
try
{
var baseAddress = Environments.GetEnvironmentVariable(
"AdventureWorks.Authentication.Jwt.Rest.BaseAddress",
"https://localhost:4001");
var token = await HttpClient.GetStringAsync($"{baseAddress}/Authentication/{_audience.Value}");
_context.CurrentTokenString = token;
_context.CurrentUser = UserSerializer.Deserialize(token, _audience);
return new(true, _context);
}
catch
{
return new(false, _context);
}
}
　サーバーサイドでAuthenticationExceptionがスローされると、クライアント側でも例外が発生するので、キャッチして認証エラーとします。利用者が認証できないケースは、機能的なシナリオとして十分考えられるので、ここではランタイムエラーとはせずに、例外はキャッチして通常のロジック内で処理します。

正常に返却された場合、秘密鍵で証明されたトークンが返却されるので、トークンと、トークンから複合したUserオブジェクトを保持します。トークンはgRPCの通信時に利用し、Userオブジェクトは必要に応じてアプリケーションで利用します。

これで認証全体の流れが実装できることが確認できました。

記事内では結構すんなり進んでいますが、記事を書くために実装している間は、だいぶモデルとコードを行ったり来たりして、何度も細かい設計変更を行っています。10カ所やそこらじゃないです。「そういうもの」だと思ってください。

検証処理の実装による検証
　続いてはアプリケーション操作時にgRPCを呼び出した際の検証処理です。


ユーザーが購買アプリケーションで何らかの操作をすると、ViewModelはgRPCのクライアント経由でサーバーサイドを呼び出します。

ちょっとこのままだと、具体的な実装が見えにくいので、前回の「設計編／前編」で購買ドメインのVendorオブジェクトをIVendorRepository経由で取得するオブジェクトを配置してみましょう。また手狭になってしまうので、認証側のオブジェクトをいったん削除したものが次の図です。


ではViewModeから順番にコードを追って実装を確認していきましょう。ユーザーが何らかの操作をしたとき、ViewModeにDIされたIVendorRepositoryを呼び出してVendorオブジェクトを取得します。

cs
private readonly IVendorRepository _vendorRepository;

private async Task PurchaseAsync()
{
var vendor = await _vendorRepository.GetVendorByIdAsync(_selectedRequiringPurchaseProduct!.VendorId);
　このとき実際には、IVendorRepositoryを実装したVendorRepositoryClientが呼び出されます。

cs
private IAuthenticationContext _authenticationContext;
private Endpoint _endpoint;

public async Task<Vendor> GetVendorByIdAsync(VendorId vendorId)
{
var server = MagicOnionClient.Create<IVendorRepositoryService>(
GrpcChannel.ForAddress(_endpoint.Uri),
new IClientFilter[]
{
new AuthenticationFilter(_authenticationContext)
});
return await server.GetVendorByIdAsync(vendorId);
}
　MagicOnionClientからIVendorRepositoryServiceのインスタンスを動的に生成して、サーバーサイドを呼び出します。Endpointは初出ですが、これは後ほど説明します。

IVendorRepositoryServiceを生成するときにAuthenticationFilterを適用します。AuthenticationFilterではつぎのように、認証時に取得したトークンをHTTPヘッダーに付与します。

cs
public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
{
var header = context.CallOptions.Headers;
header.Add("authorization", $"Bearer {_authenticationContext.CurrentTokenString}");

    return await next(context);
}
　authorizationにBearer～の形式でトークンを設定するのは、OAuthの仕組みに則っています。トークンはHTTPヘッダーに格納されて、メッセージとともにリモートへ送信します。

サーバーサイドでgRPCが呼び出された場合、リクエストをいったんすべてAuthenticationFilterAttributeで受け取り、トークンを検証します。

cs
private readonly ServerAuthenticationContext _serverAuthenticationContext;

public override async ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
{
try
{
var entry = context.CallContext.RequestHeaders.Get("authorization");
var token = entry.Value.Substring("Bearer ".Length);
_serverAuthenticationContext.CurrentUser = UserSerializer.Deserialize(token, _audience);
_serverAuthenticationContext.CurrentTokenString = token;
}
catch (Exception e)
{
_logger.LogWarning(e, e.Message);
context.CallContext.GetHttpContext().Response.StatusCode = StatusCodes.Status401Unauthorized;
return;
}

    try
    {
        await next(context);
    }
    finally
    {
        _serverAuthenticationContext.ClearCurrentUser();
    }
}
　リクエストヘッダーのauthorizationからJWTを取得します。取得したトークンをUserSerializer.Deserializeをつかって署名を検証しつつ複合し、ServerAuthenticationContextに設定することで、以後必要に応じて利用します。トークンの複合に失敗した場合は、認証エラー（401エラー）を返します。

サーバーサイドではIAuthenticationContextをDIすることで、インスタンスを使いまわす想定です。単純にプロパティに設定してしまうと、他者の権限で実行されてしまう可能性があります。そのため、サーバー用のIAuthenticationContextはつぎのように実装しています。

cs
public class ServerAuthenticationContext : IAuthenticationContext
{
private readonly AsyncLocal<User> _currentUserAsyncLocal = new();

    public User CurrentUser
    {
        get
        {
            if (_currentUserAsyncLocal.Value is null)
                throw new InvalidOperationException("認証処理の完了時に利用してください。");

            return _currentUserAsyncLocal.Value;
        }

        internal set => _currentUserAsyncLocal.Value = value;
    }
}
　実体はAsyncLocal<T>に保持します。これによって同一スレッド上では必ず同じユーザーが取得できます。また設定はフィルターを通して行い、設定できた場合のみgRPCの実際の処理が実行されます。あとは必要な箇所でIAuthenticationContextをDIコンテナーから注入して利用します。

cs
public class VendorRepositoryService : ServiceBase<IVendorRepositoryService>, IVendorRepositoryService
{
private readonly IVendorRepository _repository;

    private readonly IAuthenticationContext _authenticationContext;

    public VendorRepositoryService(IVendorRepository repository, IAuthenticationContext authenticationContext)
    {
        _repository = repository;
        _authenticationContext = authenticationContext;
    }

    public async UnaryResult<Vendor> GetVendorByIdAsync(VendorId vendorId)
    {
        // 呼び出し元のユーザー情報を利用する。
        var user = _authenticationContext.CurrentUser;

        return await _repository.GetVendorByIdAsync(vendorId);
    }
}
　ところで、このコードは動きません。IUserRepositoryでDapperを利用するのにTypeHandlerを作成したようにValueオブジェクトのIMessagePackFormatterを作成する必要があります。

Vendorオブジェクトのコードを見てみましょう。

cs
public record Vendor(VendorId VendorId,
AccountNumber AccountNumber,
string Name,
CreditRating CreditRating,
bool IsPreferredVendor,
bool IsActive,
Uri? PurchasingWebServiceUrl,
TaxRate TaxRate,
ModifiedDateTime ModifiedDateTime,
IReadOnlyList<VendorProduct> VendorProducts);
　多数のValueオブジェクトが含まれています。TaxRateを見てみると値がdecimalの構造体であることが見て取れます。

cs
namespace AdventureWorks;

[UnitOf(typeof(decimal))]
public partial struct TaxRate
{
}
　TaxRateは全ドメインで共通して利用するため、AdventureWorksプロジェクトに含めます。

このような値をMagicOnionで送受信するためには、TaxRate用のIMessagePackFormatterを作成する必要があります。Dapperのときと同じように、UnitGeneratorでは属性指定することで生成ができます。

cs
[UnitOf(typeof(decimal), UnitGenerateOptions.MessagePackFormatter)]
public partial struct TaxRate
{
}
　ただ同様にこうしてしまうと、ドメインのコードがMagicOnion（正確にはそのシリアライザーであるMessagePack）に依存してしまい、ドメインのフレームワーク非依存が破壊されてしまいます。

Dapperのときと同様に、アーキテクチャ的に受け入れるという選択肢もあります。ただ、おなじくあまり好みではないため、プロジェクトは分けることにします。

AdventureWorks.TaxRateのMagicOnion用のIMessagePackFormatterなので、プロジェクトとしてはAdventureWorks.MagicOnionに含めましょう。

cs
namespace AdventureWorks.MagicOnion;

public class TaxRateFormatter : IMessagePackFormatter<TaxRate>
{
public void Serialize(ref MessagePackWriter writer, TaxRate value, MessagePackSerializerOptions options)
{
options.Resolver.GetFormatterWithVerify<System.Decimal>().Serialize(ref writer, value.AsPrimitive(), options);
}

    public TaxRate Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new TaxRate(options.Resolver.GetFormatterWithVerify<System.Decimal>().Deserialize(ref reader, options));
    }
}
　こんな感じのコードになります。やはり個別に実装するのは手間ですし、不具合も怖いのでT4なりなにかで自動生成するのがオススメです。

では忘れないうちに、TaxRateFormatterを実装ビューに反映しましょう。


VendorRepositoryの実装は、認証設計の際に説明したものと変わらないため、割愛します。これでひととおり見たのですが、ひとつ気になるところがありました。IVendorRepositoryのクライアント側実装であるVendorRepositoryClientです。

cs
private IAuthenticationContext _authenticationContext;
private Endpoint _endpoint;

public async Task<Vendor> GetVendorByIdAsync(VendorId vendorId)
{
var server = MagicOnionClient.Create<IVendorRepositoryService>(
GrpcChannel.ForAddress(_endpoint.Uri),
new IClientFilter[]
{
new AuthenticationFilter(_authenticationContext)
});
return await server.GetVendorByIdAsync(vendorId);
}
　問題はこの、MagicOnionClientからリモートのgRPCサーバーを呼び出し、gRPCクライアントの生成コードで毎回これを実装するには問題があります。

適用するフィルターが変わったときに、クライアント呼び出しコードをすべて修正しないといけない
エンドポイントも個別に指定したくない
単純にコードが多い
　というわけで、ファクトリーを作成して、これらを隠蔽しましょう。

cs
public interface IMagicOnionClientFactory
{
T Create<T>() where T : IService<T>;
}

public class MagicOnionClientFactory : IMagicOnionClientFactory
{
private readonly IAuthenticationContext _authenticationContext;
private readonly Endpoint _endpoint;

    public MagicOnionClientFactory(
        IAuthenticationContext authenticationContext,
        Endpoint endpoint)
    {
        _authenticationContext = authenticationContext;
        _endpoint = endpoint;
    }

    public T Create<T>() where T : IService<T>
    {
        return MagicOnionClient.Create<T>(
            GrpcChannel.ForAddress(_endpoint.Uri),
            new IClientFilter[]
            {
                new AuthenticationFilter(_authenticationContext)
            });
    }
}
　こんな感じでファクトリー側にコードを押し出して、利用する場所ではつぎのように使います。

cs
private readonly IMagicOnionClientFactory _clientFactory;
public async Task<Vendor> GetVendorByIdAsync(VendorId vendorId)
{
var server = _clientFactory.Create<IVendorRepositoryService>();
return await server.GetVendorByIdAsync(vendorId);
}
　ではこれを実装ビューに反映しましょう。


AdventureWorks全体で利用するMagicOnion用の、クライアント限定オブジェクトなので、AdventureWorks.MagicOnion.Clientプロジェクトに置くことにしました。これは認証やデータベースと同列のものなので、ドメインモデルにも反映する必要があるでしょう。ということで、反映したのが下図になります。


認証ドメイン視点がこちら。


そして購買ドメイン視点だとこちらになります。さて、気が付いた方はいらっしゃるでしょうか？ ここにきて重大な設計ミスが発覚しました。


ここの2つのネーミングがプロジェクトの命名規則に違反しています。

AdventureWorks.MagicOnion
AdventureWorks.MagicOnion.Client
　プロジェクトの親子関係は、子は親を具体化したもので、親は子の抽象となるように設計すると初期に宣言しました。ところが、AdventureWorks.MagicOnionはAdventureWorksのMagicOnion実装で、AdventureWorksドメイン固有のものです。

それに対してAdventureWorks.MagicOnion.Clientは全ドメインから汎用ドメインとして利用される、MagicOnionのクライアントサポートライブラリです。名前空間的に子であるAdventureWorks.MagicOnion.Clientのほうが、概念的にはスコープが広い状態になってしまっていて、ここだけ名前空間の設計が破綻しています。

ではどうするか？ 結局、ビジネス的なツリー構造と、技術的なツリー構造が混ざってしまったことが原因でしょう。ということで、そこを分離する必要があります。

ビジネス関連のドメインをAdventureWorks.Businessとして、ビジネスの実現をサポートする汎用ドメインは、「AdventureWorks. 汎用ドメイン名」という形に修正しましょう。

今回の最大のインパクトある設計ミスです。AdventureWorksドメインもそうですが、購買・販売・製造ドメインの名前空間をすべて変更しなくてはなりません。実際のところ、ゼロからアーキテクチャ設計していると、こういったどんでん返しはそれなりに発生します。

ではまず、境界付けられたコンテキストを修正します。


つぎのようなツリー構造になります。

AdventureWorks
ビジネス
購買
販売
製造
データベース
認証
MagicOnion
　ビジネスを挟んだことで、逆にすっきりしたように感じます。ここまで来れば明らかなんですが、ビジネスドメインのルートをAdventureWorksにしてしまうと、非ビジネスのドメインと命名がコンフリクトする可能性は排除できません。そのためトップをプロダクト名前空間にして、その下はビジネスのルートと、非ビジネスを並べる形が無難です。

コンテキストマップは変わらないので、つぎは実装ビューを見てみましょう。


ベージュ色の部分がビジネス関連のプロジェクトです。左下のAdventureWorks.Businessの領域をのぞくと、だいたい上半分に認証系のコンポーネント、下半分に検証系のコンポーネントが配置されています。右半分にクライアントサイド、左半分にサーバーサイドのコンポーネントが配置されています。

認証ドメインの配置ビュー
　先ほど、認証ドメインの実装ビューを作成しました。つづいて、そこで設計されたコンポーネントを論理ノードに配置しましょう。


これだけだと分かりにくいので、認証と検証にわけて流れを追いつつ見てみましょう。

認証処理の確認
　認証時の大まかな流れは次の通り。


アプリケーションの起動時にViewModelから、AdventureWorks.Authenticationコンポーネント（名前空間をすべて書くと長すぎるので、以後省略しつつ記載します。）のIAuthenticationServiceを経由して認証処理を呼び出します。IAuthenticationServiceの実装クラスであるAuthenticationServiceの含まれる～.Jwt.Rest.Clientコンポーネントをとおしてサーバーサイドを呼び出します。

サーバーサイドのリクエストは～.Jwt.Rest.ServerコンポーネントのAuthenticationControllerにRESTのリクエストが呼び出されます。AuthenticationControllerはWindows認証を利用してリモートのアカウントを取得し、AdventureWorks.BusinessコンポーネントのIUserRepositoryを利用して、データベースからUserを取得して認証処理を行います。このとき実体は～.Business.SqlServerコンポーネントのUserRepositoryが利用されます。

正しくUserが取得できたら、～.JwtコンポーネントのUserSerializerを利用してJWTトークンを作成して返却します。～.Hosting.RestはサーバーサイドのWeb APIをホスティングするための全体を統括するコンポーネントです。

おおむねこんな流れですが、問題はなさそうです。

検証処理の確認
　つづいて検証時の流れです。


ユーザー操作に応じてViewModelから～.PurchasingコンポーネントのIVendorRepositoryをとおしてVenderオブジェクトを取得します。IVendorRepositoryの実体はリモートのWeb API側にあります。そのため、IVendorRepositoryのgRPCクライアントである～Purchasing.MagicOnionコンポーネントのVendorRepositoryClientが呼び出されます。

その際、認証情報であるJWTを～.Authentication.MagicOnion.ClientコンポーネントのAuthenticationFilterで付与してからサーバーサイドが呼び出されます。

サーバーサイドでは～.Authentication.MagicOnion.ServerコンポーネントのAuthenticationFilterAttributeで、HTTPヘッダーからJWTを取得して検証します。

問題なければ～.Purchasing.MagicOnion.ServerコンポーネントのVendorRepositoryServiceが呼び出されます。VendorRepositoryServiceは～.Purchasing.SqlServerコンポーネントのVendorRepositoryクラスを利用してVendorオブジェクトを取得してクライアント側に返却します。

このときVendorオブジェクトに含まれるTaxRateのようなValueオブジェクトのgRPC上のシリアライズ・デシリアライズに～.Business.MagicOnionのFormatterが利用されます。

検証側もどうやら問題なさそうです。

さて、認証ドメインについてはいったんこのあたりにしましょう。まだ何かある可能性はありますが、認証ドメインだけみていても、コスパは悪そうです。他の要件を設計しながら、認証ドメインの設計に問題がないか検証していきましょう。

例外処理アーキテクチャ
　つづいて例外処理アーキテクチャについて設計します。例外処理は、WPFとgRPCでまったく異なります。そのため、それぞれ個別に設計していきましょう。

WPFの例外処理アーキテクチャ
　WPFの例外処理は、特別な意図がある場合を除いて、標準で提供されている各種の例外ハンドラーで一括して処理することにします。

実際問題、起こりうる例外をすべて正しく把握して、個別に設計・実装することはそもそも現実味がありません。特定の例外のみ発生箇所で個別に例外処理をしても、全体としての一貫性が失われることが多いです。また、例外の隠ぺいや必要なログ出力のもれにつながりやすいです。であれば、グローバルな例外ハンドラー系に基本的には任せて一貫した例外処理をまずは提供するべきかと思います。

ただもちろんすべてを否定するわけではありません。

例えば、何らかのファイルを操作するときに、別のプロセスによって例外がでることは普通に考えられます。このような場合にシステムエラーとするのではなくて、対象のリソースが処理できなかったことを明確に伝えるために、個別の例外処理をすることは、十分考えられます。

このように、正常なビジネス処理において起こりうる例外については、そもそもビジネス的にどのように対応するか仕様を明確にして、個別に対応してあげた方が好ましいものも多いでしょう。

逆に例えば、サーバーサイドのAPIを利用しようとした場合、通信状態が悪ければ例外が発生するでしょう。これらは個別に扱わず、必要であれば適当なリトライ処理の上で、特別な処理は行わずにシステムエラーとしてしまった方が良いでしょう。

業務シナリオとして起こりうるケースの判定に、例外を用いる必要がある場合は個別処理をする。
業務シナリオとは関係なく、システム的な要因による例外は、例外ハンドラーで共通処理をする。
　おおまかな方針としては、こんな感じが好ましいと考えています。ここでは共通の例外ハンドラーの扱いについて設計していきましょう。

例外ハンドリングの初期化
　今回は画面処理フレームワークにKamishibaiをもちいて、WPFアプリケーションはGeneric Host上で動作させます。そのため、例外ハンドリングの初期化はつぎのように行います。

cs
var builder = KamishibaiApplication<TApplication, TWindow>.CreateBuilder();

// 各種DIコンテナーの初期化処理

var app = builder.Build();
app.Startup += SetupExceptionHandler;
await app.RunAsync();
　ビルドしたappのStartupイベントをフックして、アプリケーションが起動した直後にSetupExceptionHandlerを呼び出して、例外ハンドリングを初期化します。

SetupExceptionHandlerの中では、次の3つのハンドラーを利用して例外処理を行います。

Application.Current.DispatcherUnhandledException
AppDomain.CurrentDomain.UnhandledException
TaskScheduler.UnobservedTaskException
Application.Current.DispatcherUnhandledException
　具体的な実装はつぎの通りです。

cs
Application.Current.DispatcherUnhandledException += (sender, args) =>
{
Log.Warning(args.Exception, "Dispatcher.UnhandledException sender:{Sender}", sender);
// 例外処理の中断
args.Handled = true;

    // システム終了確認
    var confirmResult = MessageBox.Show(
        AdventureWorks.Wpf.ViewModel.Properties.Resources.SystemErrorOccurredConfirm,
        AdventureWorks.Wpf.ViewModel.Properties.Resources.SystemErrorOccurredCaption,
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning,
        MessageBoxResult.Yes);
    if (confirmResult == MessageBoxResult.No)
    {
        Environment.Exit(1);
    }
};
　例外情報をログに出力したあと、例外処理を中断します。

その後に、システムの利用を継続するかどうか、ユーザーに確認を取り、継続が選ばれなかった場合はアプリケーションを終了します。

WPFの例外ハンドラーは、基本的にはApplication.DispatcherUnhandledExceptionで例外を処理します。Application.DispatcherUnhandledExceptionでは例外チェーンを中断できますが、それ以外では中断できないためです。

Environment.Exit(1)を呼び出さなくても、最終的にはアプリケーションは終了します。しかし、Environment.Exit(1)を呼び出さないと、つづいてAppDomain.CurrentDomain.UnhandledExceptionが呼び出されます。例外の2重処理になりやすいため、明示的に終了してしまうのが好ましいでしょう。

AppDomain.CurrentDomain.UnhandledException
　先のApplication.DispatcherUnhandledExceptionでは、つぎのように、明示的に作成したThreadで発生した例外は補足できません。

cs
var thread = new Thread(() =>
{
throw new NotImplementedException();
});
thread.Start();
　この場合は、AppDomain.UnhandledExceptionを利用して例外を補足します。

AppDomain.CurrentDomain.UnhandledExceptionでは、次のようにログ出力の後に、ユーザーにエラーを通知してアプリケーションを終了します。AppDomain.CurrentDomain.UnhandledExceptionでは例外チェーンを中断できず、この後アプリケーションは必ず終了されるため、確認はせずに通知だけします。

cs
AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
Log.Warning(args.ExceptionObject as Exception, "AppDomain.UnhandledException sender:{Sender}", sender);

    // システム終了通知
    MessageBox.Show(
        AdventureWorks.Wpf.ViewModel.Properties.Resources.SystemErrorOccurredAlert,
        AdventureWorks.Wpf.ViewModel.Properties.Resources.SystemErrorOccurredCaption,
        MessageBoxButton.OK,
        MessageBoxImage.Error,
        MessageBoxResult.OK);

    Environment.Exit(1);
};
　このとき、Environment.Exit(1)を呼び出すことで、Windowsのアプリケーションのクラッシュダイアログの表示を抑制します。

TaskScheduler.UnobservedTaskException
　つぎのようにTaskをasync/awaitせず、投げっぱなしでバックグラウンド処理した際に例外が発生した場合は、TaskScheduler.UnobservedTaskExceptionで補足します。

cs
private void OnClick(object sender, RoutedEventArgs e)
{
Task.Run(() =>
{
throw new NotImplementedException();
});
}
　ただTaskScheduler.UnobservedTaskExceptionは例外が発生しても即座にコールされないため注意が必要です。ユーザーの操作とは無関係に、「いつか」発行されるため、ユーザーに通知したり、アプリケーションを中断しても混乱を招くだけです。

未処理の例外は全般的に、あくまで最終手段とするべきものですが、特にTaskScheduler.UnobservedTaskExceptionは最後の最後の保険と考えて、つぎのようにログ出力程度に留めておくのが良いでしょう。

cs
TaskScheduler.UnobservedTaskException += (sender, args) =>
{
Log.Warning(args.Exception, "TaskScheduler.UnobservedTaskException sender:{Sender}", sender);
args.SetObserved();
};
　SetObservedは .NET Framework 4以前はアプリケーションが終了してしまうことがありましたが、現在は呼ばなくても挙動は変わらないはずです。一応念のため呼んでいます。

gRPCの例外処理アーキテクチャ
　Web APIで何らかの処理を実行中に例外が発生した場合、通常はリソースを解放してログを出力するくらいしかできません。ほかにできることといえば、外部リソース（例えばデータベース）を利用中に例外が発生したのであればリトライくらいでしょうか？

特殊なことをしていなければリソースの解放はC#のusingで担保するでしょうし、解放漏れがあったとしても例外時にフォローすることも難しいです。そのため実質的にはログ出力くらいです。

MagicOnionを利用してgRPCを実装する場合、通常はASP.NET Core上で開発します。ASP.NET Coreで開発している場合、一般的なロギングライブラリであれば、APIの例外時にはロガーの設定に則ってエラーログは出力されることが多いでしょう。

では何もする必要がないのでしょうか？ そんなことはありません。Web APIの実装側でも別途ログを出力しておくべきです。これはASP.NET Coreレベルでのログ出力では、接続元のアドレスは表示できても、例えば認証情報のようなデータはログに出力されないためです。誰が操作したときの例外なのか、障害の分析には最重要情報の1つです。ASP.NET Coreレベルのログも念のため残しておいた方が安全ですが、アプリケーション側ではアプリケーション側で例外を出力しましょう。

MagicOnionを利用してこのような共通処理を組み込みたい場合、認証のときにも利用したMagicOnionFilterAttributeを利用するのが良いでしょう。

このとき認証のときに利用したフィルターに組み込んでも良いのですが、つぎのように認証用のフィルターの後ろに例外処理用のフィルターを配置した方が良いと考えています。


これは認証とログ出力は別の関心ごとだからです。関心の分離ですね。ログ出力を修正したら認証が影響を受けてしまった、またはその逆のようなケースを防ぐためには、別々に実装しておいて組み合わせたほうが良いでしょう。

具体的な実装はつぎの通りです。

cs
public class ExceptionFilterAttribute : MagicOnionFilterAttribute
{
private readonly ILogger<ExceptionFilterAttribute> _logger;
private readonly IAuthenticationContext _authenticationContext;

    public ExceptionFilterAttribute(
        ILogger<ExceptionFilterAttribute> logger, 
        IAuthenticationContext authenticationContext)
    {
        _logger = logger;
        _authenticationContext = authenticationContext;
    }

    public override ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        try
        {
            return next(context);
        }
        catch (Exception e)
        {
            // 例外情報をログ出力した後に再スローする。
            _logger.LogError(・・・・
            throw;
        }
    }
}
　ILoggerとIAuthenticationContextをDIコンテナーから注入することで、認証情報を活用したログ出力が可能となります。具体的なログ出力については、つぎの章で詳細を設計しましょう。

ロギングアーキテクチャ
　ここからはロギングアーキテクチャについて設計していきましょう。

まずはロギングの要件を明確にします。

ロギング要件
　今回は仮に、つぎのような要件があるものとします。

ログは可能な限り一貫して保管するため、基本的にはサーバーサイドに保管すること
ただしロギングサーバーが停止している場合も考慮し、エラーレベルのログはローカルファイルシステムにもあわせて保管すること
ログは将来的な分析に利用するため、構造化ログを利用すること
ログの出力には、ログ出力を要求した端末・ユーザーの情報を含めること
ログの出力レベルは、モジュールの再配布なく、サーバーサイドから変更可能であること
　WPFアプリケーションの場合、要件5が実現できると、運用時に楽になることがあります。WPFアプリケーションの場合、Webアプリケーションとちがって再配布が容易ではないため、障害などの解析が必要になった際に、ログの出力レベルを再配布なく変更できることは、おおきなメリットとなります。

なお要件4に一部のログ属性について言及していますが、今回はこれ以外の属性については言及しません。特にユーザー情報に関しては、認証処理と密接にかかわってくるため入れましたが、それ以外は実際のプロジェクトにおいて必要に応じて検討してください。

ログ出力レベル設計
　さて詳細な設計に入る前に、ログの出力レベルについて決定しておきましょう。ここからは仮の実装を行いながら設計します。出力レベルの認識がずれていると、あとから直すのは負担が大きいためです。

レベル	通常時出力	クライアントサイド	サーバーサイド
Trace	×	Debugレベル以外のViewModelのメソッド呼び出し時	なし
Debug	×	ViewModelの画面遷移イベント発生時、またはCommand呼び出し時	なし
Information	〇	なし	HTTPのリクエスト、レスポンス時。認証後のAPI呼び出し前。
Warning	〇	アプリケーションの機能に影響を与える可能性のあると判断したとき。個別に相談の上決定し、つど本表を更新する。	同左＋認証エラー時
Error	〇	意図しない例外の発生時	同左
Critical	〇	システムの運用に致命的な問題が発生した場合	同左
　通常はInformationレベル以上は出力し、Debug以下は、障害の調査時などに必要に応じて一時的に利用します。

上記仕様の場合、サーバーサイドで例外が発生した場合、クライアントサイドでもWeb API呼び出しが例外となります。そのため、同一の例外が2重で出力されますが、取りこぼすよりは良いため上記定義とします。

個人的にはWarningの扱いが難しいといつも感じます。単純明快なルールを定められたことがありません。良い提案があればぜひ伺いたいです。

WPF上のTrace、Debugログ
　前述のとおり、基本的にViewModelのメソッドの呼び出し箇所でログを仕込みます。ただ、これを抜け漏れ誤りなく実装するのは大変です。

そこで今回はPostSharpというAOPライブラリを利用して、一括でログ出力コードを「編み込み」ます。AOPとはAspect Oriented Programming、アスペクト志向プログラミングのことで、横断的な関心ごとを「アスペクト」として、その関心を必要とする場所に「編み込む」方法論のことです。

詳細な説明はここでは割愛しますが、ここではログ出力処理をアスペクトとしてViewModelのメソッドに埋め込みます。

AOPライブラリは各種ありますが、おおきく2種類のタイプに分けられます。

プロキシーオブジェクトを作ってメソッド呼び出しをインターセプトするタイプ
コンパイル時にILを操作するなどして、特定のコードを任意の場所に埋め込むタイプ
　PostSharpは後者のタイプのライブラリです。

https://www.postsharp.net/
　10種類までのアスペクトであれば無償版でも利用でき、実績も多いため今回はPostSharpを利用します。具体的には、つぎのようなOnMethodBoundaryAspectを継承したアスペクトを作成します。

cs
public class LoggingAspect : OnMethodBoundaryAspect
{
public static ILogger Logger・・・

    public override void OnEntry(MethodExecutionArgs args)
    {
        var logLevel = GetLogLevel(args);
        Logger.Log(logLevel, "{Type}.{Method}({Args}) Entry", args.Method.ReflectedType!.FullName, args.Method.Name, args);
そしてViewModelプロジェクトのAssemblyInfo.csなどに、つぎのように記載します。

cs
using AdventureWorks.Wpf.ViewModel;

[assembly: LoggingAspect(AttributeTargetTypes = "*ViewModel")]
　これでビルド時にViewModelのメソッドの入り口でLoggingAspectクラスのOnEntryが呼び出されるコードが編み込まれます。非常に簡単ですし、抜け漏れ誤りなく実装が可能になります。

ロギングアーキテクチャ概要
　前述の要件を満たすために、ざっくり次のようなアーキテクチャを検討します。

ロギングのインターフェイスには、Microsoft.Extensions.Loggingを利用する
ロギングの実装には、Serilogを利用する（要件3）
ログの出力はSQL Server上に保管する（要件1）
WPFのログ出力は、SerilogのSink（出力先を拡張する仕組み）を利用してgRPCでサーバーへ送信して保管する（要件1）
またあわせてエラーレベルのログは、FileSinkを利用してローカルにも出よくする（要件2）
WPF・Web APIともに、起動時にログ設定ファイルをサーバーより取得して適用する（要件5）
ログ出力時は認証アーキテクチャで検討した仕組みを適用してユーザーを特定してログ出力する（要件4）
　上記を実現するため、ロギングWeb APIをgRPCで構築します。ざっくりした構成はつぎのとおりです。


ロギングAPIも認証を掛けたいため、認証APIを利用します。その上で、次のような相互作用をとります。


まず認証APIを呼び出して認証します。このときビジネスドメインのWeb APIとは別のaudienceを利用します。そのためWPFの起動時にビジネスドメインとロギングで最低2回の認証を呼び出します。

認証に成功したらロギング設定をロギングAPIから取得します。取得時にはビジネスドメインのWeb API同様にJWTで認証します。あとは取得した設定情報に基づいてログを出力します。

ビジネスドメインのWeb APIノードは、その機能の提供時に直接データベースサーバーを利用します。そのためログ出力も直接データベースへ出力します。ロギング時にログAPIを利用してもよいのですが、今回はノードを分散した耐障害性まで求めないものとして、直接データベースに出力することとします。

ロギングのドメインビュー
　さて、そろそろ慣れてきてもう気が付いていると思いますが、ロギングも汎用のライブラリ的に扱いますから、新しい汎用ドメインになります。

下図がロギングドメインからみた境界付けられたコンテキストです。


そして下図がコンテキストマップです。


ロギングコンテキストは、購買・製造・販売のコンテキストからカスタマー・サプライヤー関係で利用されます。その際に、認証されたユーザーにのみ利用を許可するため、認証コンテキストとAdventureWorks.Businessコンテキストを利用します。ロギングコンテキストは、WPFクライアントに対してgRPCでロギングAPIを提供するためMagicOnionコンテキストを利用します。そしてログの出力はSqlServerに行うため、データベースコンテキストも利用します。

ロギングの論理ビュー
　ではロギングの論理ビューを設計しましょう。


だいたいこんな感じでしょうか？ 今回は、中央からAdventureWorks.Businessドメインを省略しています。実際には認証処理のためにUserオブジェクトを参照するのですが、本質的には影響が小さいことと、ロギングドメインの外側にSerilogに直接依存している層を書きたかったので、省略しました。

最外周にMagicOnionが2カ所でてきます。これは別のサービスというわけではなく、片側にそろえると込み合ってしまうので、見やすくするために分けてあるだけです。

上側がログ出力側、下側が初期化処理側のオブジェクトが集まっています。

初期化処理
　まずは初期化処理についてみてみましょう。


認証と同様に、アプリケーション起動時に初期画面のViewModelから初期化を行います。

ILoggingInitializerインターフェイスを呼び出すことで初期化を行います。実際にはLoggingInitializerクラスを注入して、クラスを呼び出します。LoggingInitializerは、ISerilogConfigRepositoryインターフェイスを利用して設定をサーバーから取得して、Serilogを初期化します。

ISerilogConfigRepositoryインターフェイスの実体はSerilogConfigRepositoryClientクラスで、IMagicOnionFactoryを利用して、ISerilogConfigServiceインターフェイスの実体を取得して呼び出すことで、gRPCでサーバーサイドを呼び出します。

サーバーサイドではSerilogConfigServiceクラスが呼び出され、ISerilogRepositoryを使って、データベースから設定値を取得します。ISerilogRepositoryインターフェイスの実体はSerilogRepositoryクラスで、このクラスからデータベースを呼び出して実際の設定値を取得します。

実装ビューの設計時に、実際にコードを追いつつ詳細に見たいと思います。

ログ出力処理
　ログ出力処理の流れは次の通りです。


ユーザーがViewで何らかの操作をすると、ViewModelが呼び出されます。ViewModelのメソッドの入り口で、PoshSharpによって織り込まれたLoggingAspectが呼び出されて、ログを出力します。

ログ出力を指示されたSerilogは（ここは図には記載していません）、MagicOnionSinkを利用してMagicOnion経由でログを出力します。MagicOnionSinkはIMagicOnionFactoryからILoggingServiceのインスタンスを取得して、サーバーサイドにログを渡します。

サーバーサイドではLoggingServiceがログを受け取り、ILogRepositoryでログを書き込みます。ILogRepositoryインターフェイスの実体はLogRepositoryクラスで、ここで実際にデータベースにログが出力されます。

ロギング実装ビューの設計
　では先ほどの登場オブジェクトを、コンポーネントに割り振ってみましょう。


ひとまず問題なさそうです。コードと流れを追って見ていきましょう。

初期化処理
　つぎの流れで順番にみていきます。


アプリケーションの起動時、初期画面で認証を実施していた箇所に、ログ出力の初期化処理を追加します。

cs
public class MainViewModel : INavigatedAsyncAware
{
private readonly ILoggingInitializer _loggingInitializer;

    ・・・

    public async Task OnNavigatedAsync(PostForwardEventArgs args)
    {
        var authenticationResult = await _authenticationService.TryAuthenticateAsync();
        if (authenticationResult.IsAuthenticated
            && await _loggingInitializer.TryInitializeAsync())
        {
            await _presentationService.NavigateToMenuAsync();
        }
        else
        {
            _presentationService.ShowMessage(
                Purchasing.ViewModel.Properties.Resources.AuthenticationFailed,
                Purchasing.ViewModel.Properties.Resources.AuthenticationFailedCaption,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // アプリケーションを終了する。
            Environment.Exit(1);
        }
    }
}
　DIされたILoggingInitializerを呼び出してロガーを初期化します。初期化に失敗した場合は、ユーザー認証エラーでアプリケーションを終了します。

ILoggingInitializerの実装クラス、LoggingInitializerはつぎの通りです。

cs
public class LoggingInitializer : ILoggingInitializer
{

    private readonly ApplicationName _applicationName;
    private readonly ILoggerFactory _loggerFactory;

    ・・・

    public async Task<bool> TryInitializeAsync()
    {
        // ロギングドメインの認証処理を行う
        AuthenticationService authenticationService = 
            new(new ClientAuthenticationContext(), LoggingAudience.Instance);
        var result = await authenticationService.TryAuthenticateAsync();
        if (result.IsAuthenticated is false)
        {
            return false;
        }

        // ロギング設定を取得する
        MagicOnionClientFactory factory = new(result.Context, Endpoint);
        var repository = new SerilogConfigRepositoryClient(factory);
        var config = await repository.GetClientSerilogConfigAsync(_applicationName);
#if DEBUG
config = config with { MinimumLevel = LogEventLevel.Debug };
#endif

        // ロガーをビルドする
        var logger = config.Build();

        // ロギング設定を適用する
        MagicOnionSink.MagicOnionClientFactory = factory;
        LoggingAspect.Logger = 
            new SerilogLoggerProvider(logger)
                .CreateLogger(typeof(LoggingAspect).FullName!);

        return true;
    }
}
　ApplicationNameとILoggerFactoryをDIして初期化を行います。ログを出力する際、購買・製造・販売のどのアプリケーションのログが出力したか判別したいため、ApplicationNameを利用します。初期化処理ではまずAuthenticationServiceを利用して、ロギングドメインの認証を行います。

認証が成功したら、SerilogConfigRepositoryClientを利用してリモートから設定情報を取得します。このとき、アプリケーション名を指定して取得します。アプリケーション名を指定することで、個別のアプリケーションごとに設定が変更できるようにします。

設定情報を取得したらロガーをビルドして、MagicOnionSinkとLoggingAspectを初期化します。このとき、開発中はログを詳細にだしておきたいのでデバッグビルド時は、ログレベルを強制的にLogEventLevel.Debugに上書きしています。

LoggingAspectはSerilogには直接依存せず、Microsoft.Extensions.Hosting.Loggingのロガーを利用するため、SerilogLoggerProviderからロガーを生成しています。

全体としてはこのような流れになります。では詳細をもう少し掘り下げて見ていきましょう。

cs
public class SerilogConfigRepositoryClient : ISerilogConfigRepository
{
private readonly IMagicOnionClientFactory _clientFactory;

    public async Task<SerilogConfig> GetClientSerilogConfigAsync(ApplicationName applicationName)
    {
        var service = _clientFactory.Create<ISerilogConfigService>();
        return await service.GetServerSerilogConfigAsync(applicationName.Value);
SerilogConfigRepositoryClientはこれまでのMagicOnionのクライアント実装と相違ありません。IMagicOnionClientFactoryからサービスを生成して、リモートを呼び出します。

呼び出されたリモート側はつぎの通りです。

cs
public class SerilogConfigService : ServiceBase<ISerilogConfigService>, ISerilogConfigService
{
private readonly ISerilogConfigRepository _repository;

    public async UnaryResult<SerilogConfig> GetServerSerilogConfigAsync(string applicationName)
    {
        return await _repository.GetClientSerilogConfigAsync(new ApplicationName(applicationName));
    }
}
　ISerilogConfigRepositoryを呼び出してその結果を返却しています。実体はSQL Server向けのSerilogConfigRepositoryクラスです。

cs
public class SerilogConfigRepository : ISerilogConfigRepository
{
・・・
public async Task<SerilogConfig> GetClientSerilogConfigAsync(ApplicationName applicationName)
{
return await GetSerilogConfigAsync(applicationName, new ApplicationName("Client Default"));
}

    private async Task<SerilogConfig> GetSerilogConfigAsync(
        ApplicationName applicationName, 
        ApplicationName defaultName)
    {
        using var connection = _database.Open();

        const string query = @"
select
ApplicationName,
MinimumLevel,
Settings
from
Serilog.vLogSettings
where
ApplicationName = @Value";

        return await connection.QuerySingleOrDefaultAsync<SerilogConfig>(query, applicationName) 
               ?? await connection.QuerySingleAsync<SerilogConfig>(query, defaultName);
    }
}
　ログの設定はアプリケーションごとに定義できると記載しました。ただ、常に個別の設定がしたいわけではなく、通常時はデフォルトの設定を利用しておいて、障害時などに個別に設定が変更できるようにしたいです。

そのため、GetClientSerilogConfigAsyncを呼び出すと"Client Default"をデフォルト設定名として内部でGetSerilogConfigAsyncを呼び出しています。

まずは指定されたアプリケーション名で取得してみて、個別の定義がなければ"Client Default"の設定を取得して返却します。

SQLの中でSettings列が指定されていますが、ここに通常は設定ファイルに記載するフォーマットで設定を格納しておきます。これによってデータベース側の設定を更新することで、すべてのWPFアプリケーションに適用されるログ設定を変更可能にします。

取得した設定は、クライアント側でビルドされてロガーインスタンスを生成します。

cs
public record SerilogConfig(ApplicationName ApplicationName, LogEventLevel MinimumLevel, string Settings)
{
public ILogger Build()
{
var settingString = Settings
.Replace("%MinimumLevel%", MinimumLevel.ToString())
.Replace("%ApplicationName%", ApplicationName.Value);

        using var settings = new MemoryStream(Encoding.UTF8.GetBytes(settingString));
        var configurationRoot = new ConfigurationBuilder()
            .AddJsonStream(settings)
            .Build();

        global::Serilog.Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configurationRoot)
#if DEBUG
.WriteTo.Debug()
#endif
.CreateLogger();

        return global::Serilog.Log.Logger;
    }
}
　Settingsにはログレベルやアプリケーション名が変更できるように、置換文字列で記載しておいて、ビルド時に値を置き換えます。

その上で、置き換えられた設定からConfigurationBuilderを利用して設定オブジェクトをビルドします。そしてビルドされた設定からSerilogのロガーを生成します。

このとき、やはりデバッグ時はVisual StudioのDebugコンソールにも出したいので、WriteTo.Debug()を追加しています。

初期化処理はおおむねこの通りです。

ログ出力処理
　ではログ出力側のコードを見ながら流れを確認していきましょう。

先に説明した通り、ログの出力はViewModelのメソッド入り口で行われます。ただ個別に実装するわけではなく、LoggingAspectを織り込むことで行われます。

cs
[PSerializable]
public class LoggingAspect : OnMethodBoundaryAspect
{
public static ILogger Logger { get; set; } = new NullLogger<LoggingAspect>();

    public override void OnEntry(MethodExecutionArgs args)
    {
        var logLevel = GetLogLevel(args);
        Logger.Log(logLevel, "{Type}.{Method}({Args}) Entry", args.Method.ReflectedType!.FullName, args.Method.Name, args);
    }

    private static LogLevel GetLogLevel(MethodExecutionArgs args)
    {
        return args.Method.Name.StartsWith("On")
               || args.Method.GetCustomAttributes(true).Any(a => a is RelayCommandAttribute)
            ? LogLevel.Debug
            : LogLevel.Trace;
    }
}
　OnMethodBoundaryAspectのOnEntryをオーバーライドすることで、メソッドの呼び出しをフックします。呼び出されたメソッドをGetLogLevelに渡して、ログの出力レベルを判定します。画面遷移時とコマンドの呼び出し時はDebug、それ以外はTraceで出力する仕様なので、そのように判定します。

Kamishibaiを利用する場合、画面遷移イベントはOn～というメソッドを利用するためそれで判断します。画面遷移イベント以外にOn～を使うことは避けますが、Debugレベルのログは通常時は出力しないため、少々別のものが紛れ込んでも問題ないでしょう。

ViewModelのコマンドは、CommunityToolkit.Mvvmライブラリを利用して行います。具体的にはつぎのように記載します。

cs
[RelayCommand]
private Task GoBackAsync() => _presentationService.GoBackAsync();
　メソッドにRelayCommandAttributeを宣言することで、コード生成を利用して自動的にコマンドが作成されます。非常に便利なのでオススメです。このようにコマンドを自動生成するため、メソッドにRelayCommandAttributeが宣言されているかどうかで、コマンドの呼び出しかどうかを判定しています。

LoggingAspectからILoggerを呼び出すと、内部的にMagicOnionSinkが呼び出されます。

cs
public class MagicOnionSink : ILogEventSink
{
private readonly LogEventLevel _restrictedToMinimumLevel;

    public async void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < _restrictedToMinimumLevel)
        {
            return;
        }

        var service = MagicOnionClientFactory.Create<ILoggingService>();
        await service.RegisterAsync(
            new LogDto(
                ・・・
            ));
    }
}
　先頭で呼び出されたログの出力レベルが、出力条件を満たしているか判定し、見たいしていない場合は早期リターンします。条件を満たしたレベルであれば、MagicOnionClientFactoryを利用してILoggingServiceを生成し、リモートにログを送信します。

ここは細かな文字列編集などが多いため、一部のコードを省略していますので、詳細に興味があるかたはGitHubを直接ご覧ください。ログを送信されたサーバー側は次のとおりです。

cs
public class LoggingService : ServiceBase<ILoggingService>, ILoggingService
{
private readonly ILogRepository _eventRepository;
private readonly IAuthenticationContext _authenticationContext;

    public LoggingService(
        ILogRepository eventRepository, 
        IAuthenticationContext authenticationContext)
    {
        _eventRepository = eventRepository;
        _authenticationContext = authenticationContext;
    }

    public async UnaryResult RegisterAsync(LogDto logRecord)
    {
        await _eventRepository.RegisterAsync(
            new Log(
                ・・・
            ));
    }
}
　JWTによる認証情報と、ILogRepositoryを利用してSQL Serverにログを保管します。ILogRepositoryの実装は繰り返しになるため、省略します。こうしてログの出力設定をデータベース側に保管しておいて、任意のタイミングで出力レベルを調整できるようにしています。

その上でWPFアプリケーションの個別機能の実装側で特別な実装を行うことなく、抜け漏れ誤りなくログが出力できるようになりました。

ロギング配置ビューの設計
　それでは実装ビューで導出したコンポーネントをノード上に配置しましょう。特に難しい要素もなく、これまでの繰り返しになってしまうため、さらっと説明します。実装ビューのコンポーネントをノード上に配置したものが以下の図です。


初期化処理時のシーケンスをたどってみましょう。


問題ありませんね。ロギング処理時のパスも、ViewModelからのほぼ同様なので説明は割愛します。1点補足があるとすると、購買API側のログ出力は、SerilogのSqlServerSinkを直接使うことです。そのため、特別な仕組みは必要ありません。

まとめ
　今回は、非機能要件に関するアーキテクチャ設計を行いました。先に非機能がある程度設計されていた方が、ユースケースの設計を行うのが容易になる側面があります。

ユースケースの設計をするのに認証が必要になることは多いでしょうし、設計中に仮実装する上で、例外処理やロギングが提供されていた方がデバッグもやりやすくなります。そのため、私個人としてはこのあたりの非機能の設計はユースケースを掘り下げる前にある程度の品質で設計されていたほうがやりやすいと感じています。

もちろん、ユースケースを設計していく上で、これらの設計が変更になることはあります。しかしアーキテクチャを設計していく上で、ことなる要件を設計していくことで、ほかの設計の見直しが必要になることは、よくあることです。これは必要なコストと考えた方が良いでしょう。そのコストはWPFならWPFのアーキテクチャが一度ある程度に詰まると、次の開発では再利用可能な範囲が広いため、徐々に品質もコストも改善していくのが良いと考えています。

さて、次回はいよいよ最終回である「設計編／後編」になります。後編では、特定のユースケースを実現するアーキテクチャの設計と、開発環境を中心としたアーキテクチャについて解説したいと思います。それでは、また次回お会いしましょう！

