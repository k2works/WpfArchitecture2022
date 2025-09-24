目次
Page
1
ソースコード
前提条件
想定読者
代表的なユースケースの実現
「ログインする」ユースケースの実現
「再発注する」ユースケースの実現
Page 2
開発者ビュー
概要を設計する
バージョン管理戦略
コーディング規約
テスト戦略
おわりに
ソースコード
　実際に動作するソースコードは、GitHub上に公開しているので、ぜひご覧ください。ビルドや実行方法については、リンク先のREADME.mdをご覧ください。また、実際に動作させるためには次の2つのライセンスが必要です。

ComponentOne for WPF
SPREAD for WPF 4.0J
　これらは試用ライセンスを発行することができます。

本稿だけで読み進められるように記載していますが、すべてのコードを詳細に解説しているわけではありません。本稿を読んだ後、あらためて動作させつつコードと本稿を読み比べていただければ、理解が深まるかと思います。

前提条件
本稿はWPFアプリケーションのアーキテクチャ設計について記載したものです。見積編および設計編／前編と設計編／中編を前提に記載しているので、未読であればそちらからお読みください。

本稿にはサーバーサイドの設計も一部含まれていますが、見積編にも記載した通り、サーバーサイドについてはWPFアプリケーションを設計する上で、必要最低限の範囲に限定しています。サーバーサイドの実現方式は、オンプレ環境なのかクラウド環境なのか？ といった容易さなどで大きく変わってきます。そしてWPFアプリケーションから見た場合には本質的な問題ではありません。サーバーサイドまで厳密に記載すると話が発散し過ぎてしまい、WPFアプリケーションのアーキテクチャにフォーカスすることが難しくなるため、あくまで参考程度にご覧ください。

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
これらの基本的な解説は、本稿では割愛しますが、知らないと理解できないという訳でもありません。また下記の2つも概要が理解できていることが好ましいです。

Clean Architecture
ドメイン駆動設計（DDD）
Clean Architectureについては、筆者のブログである「世界一わかりやすいClean Architecture」をあわせて読んでいただけると、本稿のアーキテクチャの設計意図が伝わりやすいかと思います。

ドメイン駆動設計の適用範囲については、本文内でも解説いたします。

代表的なユースケースの実現
　長くなりましたが、やっとユースケースを実装するための土台ができました。ここからは個別のユースケースの実現を設計することで、WPFアプリケーション全体のアーキテクチャを完成させていきます。

あらためてユースケースビューを見てみましょう。

No.	ユースケース	アーキテクチャパターン	代表	選定理由
1	ログインする	ログインパターン	✅	アプリケーション起動時の処理を実現するため。厳密にはログインはユースケースとは言えないかもしれないが、機能をユースケースで網羅するため含める。
2	発注する	基本パターン		
3	再発注する	基本パターン	✅	ユースケース固有の複雑な参照処理とそれを表示する高機能なグリッド、エンティティのCRUDが含まれるため。実際にはUとDは含まれないが、アーキテクチャ的にはCと差異がない。
4	・・・	・・・		
　今回は「ログインする」と「再発注する」の2つのユースケースを実現する過程で、アーキテクチャを煮詰めていきます。次のような流れでユースケースを実現します。

ユースケースの実現に必要なシナリオの一覧を洗い出す
洗い出したシナリオを記述する
シナリオの記述を実現しながら、各ビューを完成させていく
「ログインする」ユースケースの実現
シナリオ一覧
　まずは「ログインする」を実現する上で、必要なシナリオを洗い出します。

No.	シナリオ	説明
1	正常にログインする
2	認証に失敗する	
　アーキテクチャを設計する上で必要になりそうなシナリオはこの2つでしょう。説明欄は、シナリオ名で十分な場合は省略しても構いません。むしろシナリオ名に含まれる内容を重複して含めた場合、読み手にムダな時間を強いることになりますし、修正時のコストも高くなり、メンテナンス性が低下します。意味なく重複した記述は避けるのが私の好みです。

では個別のシナリオを記述していきましょう。

シナリオ「正常にログインする」
　シナリオでは、ユーザーとシステムとの対話を記述します。前提条件と、事後条件も明確にしておくことがポイントです。

【前提条件】

システムは起動していないこと

No.	ユーザーの振る舞い	システムの振る舞い
1	システムを起動する	トップ画面を表示する
2	認証処理を行う
3	認証に成功する
4	メニュー画面を表示する
　【事後条件】

システムはメニュー画面を表示していること

ここでは、仕様を定義しているわけではなく、あくまでアーキテクチャを設計しています。そのため、この時点で正確な仕様は必要なく、アーキテクチャを設計できるレベルで十分です。

また、今後このユースケース仕様を詰めていくうちに変更されたとしても構いません。その際に、アーキテクチャに影響する変更がなければ、この記述を修正する必要はありません。

あくまでも、アーキテクチャを設計する上で必要十分な仕様を定義することが目的です。

ではシナリオの「システムの振る舞い」にしたがって、実装を振り返りつつ、アーキテクチャを蒸留していきましょう。

トップ画面を表示する
　まずはエントリーポイントのProgram.csを見てみましょう。

cs
var builder = KamishibaiApplication<App, MainWindow>.CreateBuilder();

// MagicOnionのクライアントファクトリーをDIコンテナーに登録する。
builder.Services.AddSingleton(GetServiceEndpoint());
builder.Services.AddSingleton<IMagicOnionClientFactory, MagicOnionClientFactory>();

// 認証サービスを初期化する。
builder.Services.AddSingleton(PurchasingAudience.Instance);
var authenticationContext = new ClientAuthenticationContext();
builder.Services.AddSingleton(authenticationContext);
builder.Services.AddSingleton<IAuthenticationContext>(authenticationContext);
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

// ロギングサービスの初期化。
var applicationName = new ApplicationName("AdventureWorks.Business.Purchasing.Hosting.Wpf");
builder.Host.UseMagicOnionLogging(applicationName);

// MagicOnionのIFormatterResolverを準備する
FormatterResolverBuilder resolverBuilder = new();

// AdventureWorks.BusinessドメインのMagicOnionで利用するIFormatterResolverを登録する
resolverBuilder.Add(AdventureWorks.Business.MagicOnion.CustomResolver.Instance);

// View & ViewModelを初期化する。
// メニュー
builder.Services.AddPresentation<MainWindow, MainViewModel>();
builder.Services.AddPresentation<MenuPage, MenuViewModel>();

// MagicOnionのIFormatterResolverを初期化する
resolverBuilder.Build();

// アプリケーションのビルド
var app = builder.Build();

// 未処理の例外処理をセットアップする。
app.Startup += SetupExceptionHandler;
await app.RunAsync();
　一般的なGeneric Hostを起動するコードですね。ただ正直、好みのコードとは言えません。このコードにはいくつかの問題があります。

まず大前提としてWPFアプリケーションは、購買アプリケーションだけでなく、製造・販売ドメインにも存在します。それらのアプリケーションでも、つぎのコードは完全に同じ形で登場するでしょう。

MagicOnionのクライアントファクトリーのDIコンテナーへの登録
認証サービスのDIコンテナーへの登録
AdventureWorks.BusinessドメインのMagicOnionで利用するIFormatterResolverの登録
　DIコンテナーの初期化コードは、意図しないところでバグを生みがちです。

例えば購買アプリケーションを構築している途中に、認証サービスの修正が必要なことが判明して、DIコンテナーに登録するオブジェクトが増えたとしましょう。その場合、上述のProgram.csのコードを修正する必要があります。この時、本来は製造・販売ドメイン側の修正も必要になりますが、そういった影響範囲を常に正しく判断して管理することは難しいです。

またWPF間だけではなく、WPFとMagicOnionサーバー側でも同様の問題が発生します。MagicOnionで通信するためには、CustomResolverをクライアント側とサーバー側で同期をとってメンテナンスする必要がありますが、そういったところも抜けがちです。

そのため、各コンポーネントでDIコンテナーの初期化に必要な処理は、前述のロギングサービスの初期化のように、コンポーネント側に初期化用の拡張メソッドを用意しておくのが良いでしょう。

例えば認証サービスであればつぎのような拡張メソッドを用意してはどうでしょうか？

cs
public static class ServiceCollectionExtensions
{
public static void UseJwtRestClient(this IServiceCollection services, Audience audience)
{
services.AddSingleton(audience);
var authenticationContext = new ClientAuthenticationContext();
services.AddSingleton(authenticationContext);
services.AddSingleton<IAuthenticationContext>(authenticationContext);
services.AddSingleton<IAuthenticationService, AuthenticationService>();
}
}
　するとProgram.csはつぎのようになります。

cs
// 認証サービスを初期化する。

// Before
builder.Services.AddSingleton(PurchasingAudience.Instance);
var authenticationContext = new ClientAuthenticationContext();
builder.Services.AddSingleton(authenticationContext);
builder.Services.AddSingleton<IAuthenticationContext>(authenticationContext);
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

// After
builder.Services.UseJwtRestClient(PurchasingAudience.Instance);
　このようにすることで、各コンポーネントの初期化処理をコンポーネント側に閉じ込めることができます。ではこれで良いか？ というと実は不十分です。

認証処理の初期化コードのすぐ下に次のようなコードがあります。

cs
// ロギングサービスの初期化。
var applicationName = new ApplicationName(typeof(Program).Assembly.GetName().Name!);
builder.Host.UseMagicOnionLogging(applicationName);
　認証処理ではbuilder.Serviceが使われていますが、ロギングサービスではbuilder.Hostが使われています。両方つかうコンポーネントがでてきてもおかしくありません。そのためIServiceCollectionの拡張メソッドでは初期化が実現できないケースが出てくる可能性があります。

となるとbuilderを渡したら良いのではないか？ となりますが、builderはWPF側とASP.NET側で共通のインターフェイスが用意されていません。DIコンテナーの初期化は、各WPFアプリケーション間でも同期をとってメンテナンスする必要がありますが、WPFアプリケーションとASP.NETアプリケーション間でも同期をとってメンテナンスする必要があります。

DIコンテナーの初期化に誤りがあると、コンパイル時にはエラーにならなかったのに、起動してみたらDIコンテナーでエラーがでて、ぱっと見なにが悪いのか分からないなんてことは良くあります。このあたりがDIが難しいと感じさせている一因な気がします。

本来はbuilderが抽象化できて、WPFでもASP.NETでも同様に扱えると良いのですが、WebApplicationBuilderがプラットフォームに依存しないインターフェイスを持たないため、直接的に解決できません。

というわけで、WPFとASP.NETのbuilderを抽象化して共通で扱えるインターフェイスを作って、それぞれのアダプターを用意することで解決しましょう。

具体的には、プラットフォームに共通するつぎのようなインターフェイスを作成します。

cs
public interface IApplicationBuilder
{
IServiceCollection Services { get; }
IConfiguration Configuration { get; }
IHostBuilder Host { get; }
FormatterResolverBuilder FormatterResolver { get; }
}
　そしてWPF用の実装クラスを用意します。

cs
public class WpfApplicationBuilder<TApplication, TWindow> : IApplicationBuilder
where TApplication : Application
where TWindow : Window

{
private readonly IWpfApplicationBuilder<TApplication, TWindow> _applicationBuilder;

    public WpfApplicationBuilder(IWpfApplicationBuilder<TApplication, TWindow> applicationBuilder)
    {
        _applicationBuilder = applicationBuilder;
    }

    public IServiceCollection Services => _applicationBuilder.Services;
    public IConfiguration Configuration => _applicationBuilder.Configuration;
    public IHostBuilder Host => _applicationBuilder.Host;
    public FormatterResolverBuilder FormatterResolver { get; } = new();
}
　ではあらためて、認証サービスの初期化拡張メソッドを用意しましょう。

cs
public static class ApplicationBuilderExtensions
{
public static void UseJwtRestClient(this IApplicationBuilder builder, Audience audience)
{
builder.Services.AddSingleton(audience);
var authenticationContext = new ClientAuthenticationContext();
builder.Services.AddSingleton(authenticationContext);
builder.Services.AddSingleton<IAuthenticationContext>(authenticationContext);
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
}
}
　これを類似箇所にすべてあてはめた後のProgram.csはつぎのようになります。

cs
var kamishibaiBuilder = KamishibaiApplication<App, MainWindow>.CreateBuilder();
var builder = new AdventureWorks.Hosting.Wpf.WpfApplicationBuilder<App, MainWindow>(kamishibaiBuilder);

// MagicOnionのクライアントファクトリーをDIコンテナに登録する。
builder.UseMagicOnionClient(GetServiceEndpoint());

// 認証サービスを初期化する。
builder.UseJwtRestClient(PurchasingAudience.Instance);

// ロギングサービスの初期化。
var applicationName = new ApplicationName("AdventureWorks.Business.Purchasing.Hosting.Wpf");
builder.Host.UseMagicOnionLogging(applicationName);

// AdventureWorks.BusinessドメインのMagicOnionで利用するIFormatterResolverを登録する
builder.UseBusinessMagicOnion();

// View & ViewModelを初期化する。
builder.UsePurchasingView();

// MagicOnionのIFormatterResolverを初期化する
builder.FormatterResolver.Build();

// アプリケーションのビルド
var app = kamishibaiBuilder.Build();

// 未処理の例外処理をセットアップする。
app.Startup += SetupExceptionHandler;
await app.RunAsync();
　だいぶスッキリしました。

ただよく見ると、購買・製造・販売のWPFアプリケーションで共通化できそうな処理がまだ含まれています。

WpfApplicationBuilderの初期化
MagicOnionのクライアントファクトリーの初期化
認証サービスの初期化
ロギングサービスの初期化
FormatterResolverの初期化
未処理の例外処理のセットアップ
　ということでWpfApplicationBuilderを少し拡張して、これらの処理をまとめてしまいましょう。

cs
public class WpfApplicationBuilder<TApplication, TWindow> : IApplicationBuilder
where TApplication : Application
where TWindow : Window

{
・・・
public static WpfApplicationBuilder<TApplication, TWindow> CreateBuilder()
{
return new(KamishibaiApplication<TApplication, TWindow>.CreateBuilder());
}

    public IHost Build(ApplicationName applicationName, Audience audience)
    {
        // MagicOnionのクライアントファクトリーをDIコンテナに登録する。
        this.UseMagicOnionClient(GetServiceEndpoint());

        // 認証サービスを初期化する。
        this.UseJwtRestClient(audience);

        // ロギングサービスの初期化。
        this.Host.UseMagicOnionLogging(applicationName);
        
        // MagicOnionの初期化
        FormatterResolver.Build();
        
        // アプリケーションのビルド
        var app = _applicationBuilder.Build();

        // 未処理の例外処理をセットアップする。
        app.Startup += SetupExceptionHandler;
        return app;
    }
}
　WpfApplicationBuilderをインスタンス化するstaticメソッドと、Buildメソッドを追加しました。そしてBuildメソッドの中で、2～6の処理をまとめています。

これでProgram.csはつぎのようになりました。

cs
var builder = AdventureWorks.Hosting.Wpf.WpfApplicationBuilder<App, MainWindow>.CreateBuilder();

// View & ViewModelを初期化する。
builder.UsePurchasingView();

// アプリケーションのビルド
var applicationName = new ApplicationName("AdventureWorks.Business.Purchasing.Hosting.Wpf");
var app = builder.Build(applicationName, PurchasingAudience.Instance);

await app.RunAsync();
　WPFで横断的に共通な処理はWpfApplicationBuilderに含めることで、アプリケーション固有の部分だけが残り、よりシンプルになりましたし、メンテナンス性も向上しました。

さて、これらの処理もデータベースや認証と同様の粒度なので、境界付けられたコンテキストとコンテキストマップを更新しましょう。



ホスティングは、先ほどのIApplicationBuilderが含まれるため、ほぼすべてから依存されます。そのため関連は省略しています。

今回開発しているシステムは、クライアントサイドもサーバーサイドもすべてGeneric Hostが前提となっています。そのため、Microsoft.Extensions.Hostingに依存していますが、Generic Host上のロガーインターフェイスであるILogger<T>がそのライブラリから依存されています。そのためホスティングコンテキストについても、ほぼすべてから依存されていても問題ないと判断しました。

むしろWPFとサーバーサイドでコンポーネントを共通化するにあたって、コンポーネントの初期化処理含めて共通化するために、WPF側もGeneric Hostに載せている側面があります。そのためMicrosoft.Extensions.Hostingに依存することは問題ありません。

つづいて、ではIApplicationBuilderなどを含めるために実装ビューを更新しましょう。


コンポーネントをフルネームで記述すると混雑しすぎるし、文字が小さくなりすぎるので、親のパッケージ名を省略しています。例えば左上のBusinessコンポーネントはAdventureWorks.Businessが正式名称ですが、親のAdventureWorksを省略しています。

AdventureWorksドメインがトップにあり、その下にBusiness・Wpf・Logging・Authentication・MagicOnionの5つのサブドメインがあります。Businessサブドメインの下にはさらにPurchasingサブドメインがあります。

Purchasingサブドメインが、現在アーキテクチャ設計している購買システムになります。それ以外は、製造・販売システムからも共有される領域になります。

なお外部コンポーネントへの依存は、例えばPurchasing.ViewModelもPostSharpなどに依存しますが、全部書くと線で埋まってしまいます。そのため推移的に依存することにして一部省略しています。

Hostingドメインが右下に追加し、HostingコンポーネントとHosting.Wpfコンポーネントに、それぞれIApplicationBuilderとWpfApplicationBuilderを追加しました。

さて、起動処理について整理が済みましたので、「トップ画面を表示する」続きを見直していきましょう。先ほど、Program.csの中に、つぎのようなコードが含まれていました。

cs
var builder = AdventureWorks.Hosting.Wpf.WpfApplicationBuilder<App, MainWindow>.CreateBuilder();

・・・

var app = builder.Build(applicationName, PurchasingAudience.Instance);
await app.RunAsync();
　RunAsyncを呼び出すことで、MainWindowが起動されます。

MainWindowは、DIコンテナー初期化時に次のように登録されています。

cs
builder.Services.AddPresentation<MainWindow, MainViewModel>();
　そのため、MainWindow起動時にMainViewModelがDataContextにバインドされます。MainViewModelはINavigatedAsyncAwareを実装しているため、MainWindowが表示されたタイミングでOnNavigatedAsyncメソッドが呼び出されます。

cs
public class MainViewModel : INavigatedAsyncAware
{
・・・
public async Task OnNavigatedAsync(PostForwardEventArgs args)
{
・・・
認証処理を行う & 認証に成功する & メニュー画面を表示する
　OnNavigatedAsyncメソッドの中で、認証処理を行います。このときシステム的には、販売サービスのgRPCとロギングサービスのgRPCに対して、認証処理を行う必要があります。

認証が成功したら、メニュー画面を表示します。

cs
public class MainViewModel : INavigatedAsyncAware
{
・・・
public async Task OnNavigatedAsync(PostForwardEventArgs args)
{
var authenticationResult = await _authenticationService.TryAuthenticateAsync();
if (authenticationResult.IsAuthenticated
&& await _loggingInitializer.TryInitializeAsync())
{
await _presentationService.NavigateToMenuAsync();
}
　ここは特にアーキテクチャ的な修正はありませんね。ということで、シナリオ「正常にログインする」のアーキテクチャ設計は完了です。

シナリオ「認証に失敗する」
　では、次にシナリオ「認証に失敗する」を見ていきましょう。

【前提条件】

システムは起動していないこと

No.	ユーザーの振る舞い	システムの振る舞い
1	システムを起動する	トップ画面を表示する
2		認証処理を行う
3		認証に失敗する
4		認証失敗をアラートで通知する
5	アラートダイアログを閉じる	システムを終了する
　【事後条件】

システムは終了していること

2までは成功時のシナリオと同じですね。

シナリオの記述は、差分が小さいことはありがちです。そのため、差分だけ記述してもよいですし、全体が小さいならすべて書いてしまっても良いでしょう。そこは単一のルールを定めず、使い分けるのが良いと思います。

なお実装的には、アーキテクチャに影響するような内容は含まれないため、ここでは省略します。

「再発注する」ユースケースの実現
　さて、やっとここまできました。ユースケースを実現するためのアーキテクチャを設計していきましょう。

シナリオ一覧
　再発注するユースケースのシナリオ一覧は下記のとおりです。

No.	シナリオ	説明
1	再発注する
2	・・・	
　今回はユースケースの正常系となるシナリオ「再発注する」を取り上げることで、アーキテクチャ設計の手順を説明します。

シナリオ「再発注する」
　シナリオは下記のとおりです。

【前提条件】

システムはメニュー画面を表示していること

No.	ユーザーの振る舞い	システムの振る舞い
1	メニューから再発注を選択する	要再発注製品一覧画面に遷移する
2	再発注の必要な製品を表示する
3	再発注するベンダーを選択する	再発注画面に遷移する
4	選択されたベンダーと再発注が必要な製品の一覧を表示する
5	発注ボタンを押下する	発注する
6	発注が成功したことを、メッセージダイアログで表示する
7	要再発注製品一覧画面に遷移する
8	再発注の必要な製品を表示する
　【事後条件】

発注の明細がデータベースに登録されている
システムは要再発注製品一覧画面に遷移し、再発注が必要な製品を表示している
　シナリオにはいくつかの画面が登場します。この時点で正確な画面設計は不要ですが、まったくないのも進めづらいです。そこで、ざっくりしたスケッチを書いておくと良いでしょう。

この時点ではワイヤーフレームで十分です。紙芝居的にコミュニケーションにも使いたいので、私はPowerPointをよく利用しています。デザイン的な素養がない私でも使いこなせるので最高です。

「要再発注製品一覧」画面

再発注が必要な製品の一覧を表示する画面です。この画面から再発注するベンダーを選択します。ベンダー列とカテゴリー列は、同一の行は結合して表示し、ベンダー単位で中計金額を表示します。

業務的には非常によくありがちな画面です。よくありがちで、案外大変な画面です。業務アプリケーションではグリッドの操作性は命とも言えます。そのため、私の辞書には「グリッドを自作する」とういう言葉はありません。グリッドは必ずサードパーティ製のものを利用します。

この画面ではグレープシティのSPREAD for WPFに含まれるGcSpreadGridコントロールを利用します。


GcSpreadGridは、高度なデータグリッド機能を提供するコンポーネントです。このグリッドは、Excelのような機能を持ち、セルの書式設定やフィルタリング、ソート、グループ化、集計、およびデータの編集など多様な操作を可能にします。大量のデータに対しても高速なパフォーマンスを提供するため、大規模なデータセットにも対応しています。またグリッドのデザイナーも提供されており、開発も容易です。

あまりに多機能なので、すべての仕様を文書化して仕様書を作ることは困難です。そのため、仕様上機能を大きく制限して、文書化できる範囲で利用しているケースがよく見られます。しかし、それは余りにもったいないです。

そこで私はお客さまと次のような合意の上で、大きな制限を設けずに利用することを提案しています。

「サードパーティコントロールで標準的に有効化されている機能は、システムの仕様書としては明記せず、その動作は、サードパーティの仕様に準拠するものとする。ユーザーテストにおいて十分に評価し、取り決められたテスト期間中に、受け入れ可能な範囲で詳細の変更は行う。リリース後、不具合をのぞき、サードパーティー製コントロールの挙動について瑕疵（かし）対応は行わない。」

一見責任放棄のようですが、顧客としては最高の操作性をもつグリッドを利用できるため、大抵の場合は受け入れていただけます。そのために場合によって、事前にモックを活用してお客さまに評価していただくこともあります。

受託開発の場合、この手の融通が利かないことで、開発側・ユーザー側双方の足かせになることがおおいです。しかしお客さまと良い関係が築けていて、かつお客さまが開発に理解を示してくれる場合は、このような合意を取り付けることを考えても良いのではないでしょうか？

「再発注」画面

「要再発注製品一覧」画面で選択されたベンダーに対して再発注するための画面。配送方法を選択肢して再発注する。

ユースケースとコンポーネント
　さて、この時点で私が最初に考えるのは、ユースケースを実現する上でどうコンポーネントを分割するか？ ということです。

では現時点でWPFアプリケーションのコンポーネント構成を見てみましょう。Businessドメイン以外は省略します。


ここで考えたいのは、ユースケースを実現するオブジェクトをどこに実装していくかです。新たに実装するものはWPF上だけを考えるとつぎの5種類です。

View
ViewModel
再発注ユースケースでのみ利用するオブジェクト
3をサーバーから取得するクライアント実装
4で利用するクライアントとサーバー間のインターフェイス定義
　このうちViewだけはPurchasing.Viewの1つにまとめて、それ以外はユースケースごとに分割して実装することにします。

ユースケース間の依存関係は、丁寧に設計する必要があります。無作為に実装していると、ユースケース間で依存の循環が発生してしまいがちです。画面が双方向に移動するケースがあったり、ユースケース間で非常に似通ったオブジェクトが登場したりするからです。しかしここの依存関係が破綻すると、ユースケースの追加や変更が非常に困難になります。

これはドメインのどちらが上流・下流を設計するのと同じ意味があります。ユースケースの上流・下流をきちんと設計することで、仕様変更時の影響を適切に管理することができるようになります。依存関係が循環していると、がっちり密結合した状態になってしまって、どちらに手を入れても、どちらにも影響が発生する状況になりがちです。このため基本的にユースケースごとに独立したコンポーネントとすることで、依存関係を制御します。

ただしViewだけは例外とします。これはViewのスタイルやテーマを個別に分割していくと、メンテナンスが大変になったり、デザイナーでプレビューが正しく表示されなかったり、リソースディクショナリのロードが重くなったり・・・問題がおおくて私自身うまく解決できていないためです。何かいい方法があれば、ぜひ教えてください。

では具体的にどうするか？ ということで、いったん次のようにしてみました。


メニューのViewModelが含まれるPurchasing.ViewModelから、要再発注製品一覧画面のViewModelを呼び出して画面遷移します。そのコンポーネントがRePurchasing.ViewModelです。

要再発注製品一覧画面では、RePurchasingコンポーネント内のユースケースオブジェクトを利用して画面を表示します。

その際、サーバーサイドを呼び出して必要な情報を取得します。そのクライアントはRePurchasing.MagicOnion.Clientに含まれています。

クライアントはサーバーのMagicOnionで実装されたWeb APIを呼び出します。そのクライアントとサーバー間のインターフェイス定義がRePurchasing.MagicOnionに含まれています。

悪くなさそうですね。ではシナリオにしたがって設計・実装してみましょう。

要再発注製品一覧画面に遷移する
cs
[Navigate]
public partial class MenuViewModel
{
private readonly RePurchasing.ViewModel.IPresentationService _presentationService;

    public MenuViewModel(
        [Inject] RePurchasing.ViewModel.IPresentationService presentationService)
    {
        _presentationService = presentationService;
    }

    [RelayCommand]
    private Task NavigateRePurchasingAsync() => 
        _presentationService.NavigateToRequiringPurchaseProductsAsync();
}
　メニュー画面で再発注ボタンを押下したとき、ViewModelのCommandを呼び出します。Commandの実装には多様な方法がありますが、今回はCommunityToolkit.Mvvmというライブラリを利用します。

CommunityToolkit.Mvvmでは上述のようにRelayCommand属性を付与することで、自動的にCommandを生成してくれます。非常に便利なのでぜひ利用してみてください。

再発注の必要な製品を表示する
　再発注一覧画面は画面全体とは別にグリッド部分をユーザーコントロールとして実装します。このコントロールはSPREAD for WPFのデザイナーを利用して作成します。


SPREAD for WPF自体、Excelライクなグリッドを実装するためのコンポーネントですが、見てのとおりデザイナーもExcelライクな操作感になっています。比較的容易にグリッドをデザインできます。

WPFなのでプレゼンテーションはMVVMパターンを適用して実装します。View側からはViewModelをバインドしますが、このときXAML上でVisual StudioのIntelliSenseを利用してバインドするため、デザイン時ViewModelを適用することを強くオススメします。そうしておかないと、バインド時のプロパティ指定ミスが地味に多くなるからです。

xaml
<UserControl ・・・
d:DataContext="{d:DesignInstance {x:Type local:RequiringPurchaseProductsDesignViewModel}, IsDesignTimeCreatable=True}"
mc:Ignorable="d"
d:DesignHeight="450" d:DesignWidth="800">
　こんな感じでXAML側にRequiringPurchaseProductsDesignViewModelを指定します。デザイン時のViewModelは、私はxaml.csにあわせて定義しています。他で利用しないためです。デザイン時ViewModelはコード生成を検討したほうが良いかもしれません。

cs
using AdventureWorks.Business.Purchasing.RePurchasing.ViewModel;

namespace AdventureWorks.Business.Purchasing.View.RePurchasing;

public partial class RequiringPurchaseProductsPage
{
public RequiringPurchaseProductsPage()
{
InitializeComponent();
}
}

public class RequiringPurchaseProductsDesignViewModel : RequiringPurchaseProductsViewModel
{
public RequiringPurchaseProductsDesignViewModel() :
base(default!, default!, default!)
{
}
}
　実際のViewModelはDIを使う関係で、デフォルトコンストラクターは存在しないことが多いです。そのため、実際のViewModelを継承したデフォルトコンストラクターをもつデザイン時ViewModelを作成してバインドして利用します。

さて、肝心の再発注対象の一覧をどのように取得しましょうか？

ドメイン駆動設計の場合、ドメインの登場オブジェクトを抽象化したエンティティクラスが存在します。しかし、要再発注製品一覧画面のグリッドで表示する1行を表すオブジェクトは、多数のエンティティから取得した情報を組み合わせたものになります。


この場合、必要な情報をエンティティのまま取得してきて、プレゼンテーション層で組み立てるようにすると、一覧画面で必要となる性能がでないことも多いです。

そこでCQRSパターンを活用します。

CQRSは、「コマンドクエリ責務分離」の略で、データベースに対する読み取り操作（クエリー）と書き込み操作（コマンド）を別々に扱うアーキテクチャのことです。

書き込み側の操作はDDDのエンティティを利用しますが、ユースケース時の複雑な情報の取得は読み取り専用のクエリーオブジェクトを利用します。データベースの読み取り操作には最適化されたクエリを使用することで、パフォーマンスの改善を期待できますし、プレゼンテーション層の実装も容易になります。またクエリーオブジェクトは、エンティティをユースケース視点で抽象化したものなので、実際のデータの持ち方を隠蔽できます。

私個人の経験として実際にそれで救われたことがあります。

私は業務アプリケーションでRDBを利用する場合、まずは基本的に、十分に正規化された形で保持するように設計します。そのように設計すると、複雑な検索が存在する場合に、多数の結合を利用した複雑なクエリーが必要になることがあります。そして、システムの利用が拡大した結果、どうしても十分な性能が出ない状態になりました。利用料が当初想定より一桁増えてしまったので、さすがに厳しかったです。

そこでデータを保存する際に、正規化されたデータとは別に、検索に適した形で非正規化したテーブルを作っておき、検索時に利用するように改修しました。

WPF側では、検索サービスにクエリーオブジェクトを利用していた結果、C#のプログラム改修を一切なしに、検索性能を改善することができました。登録側もデータベーストリガーを利用しました。

というわけで要検索製品一覧画面のレコードは、エンティティではなく、RePurchasingコンポーネント内に、RequiringPurchaseProductクラスを作成します。

cs
public record RequiringPurchaseProduct
{
public RequiringPurchaseProduct(
VendorId VenderId,
string VendorName,
ProductCategoryId ProductCategoryId,
string ProductCategoryName,
・・・
　プロパティが多すぎるので、省略しますが、雰囲気は伝わるとおもいます。フラットな構造になっています。そしてエンティティに対するリポジトリーに該当する、クエリーオブジェクトに対するクエリーサービスをPurchasingコンポーネントに作成します。

cs
namespace AdventureWorks.Business.Purchasing.RePurchasing;

public interface IRequiringPurchaseProductQuery
{
Task<IList<RequiringPurchaseProduct>> GetRequiringPurchaseProductsAsync();
}
　そしてこのクエリーサービスのMagicOnion実装を、Purchasing.MagicOnion.Clientに定義します。

cs
public class RePurchasingQueryClient : IRePurchasingQuery
{
・・・
public async Task<IList<RequiringPurchaseProduct>> GetRequiringPurchaseProductsAsync()
{
var client = _clientFactory.Create<IRequiringPurchaseProductQueryService>();
return await client.GetRequiringPurchaseProductsAsync();
}
}
　MagicOnionを利用するには、それに適したインターフェイスを定義する必要があります。それが_clientFactoryから生成しているIRequiringPurchaseProductQueryServiceです。

IRequiringPurchaseProductQueryServiceはクライアントとサーバー間で共有するインターフェイスなので、Purchasing.MagicOnionに定義します。

では、ここまでの実装をモデルに反映します。


再発注画面に遷移する
　ベンダーを選択肢、発注ボタンを押下されたら、再発注画面に遷移します。

再発注画面ではベンダーの詳細情報が必要になります。しかし要再発注製品一覧画面のクエリーモデルには、ベンダーIDと名称くらいしかもっていません。

ベンダーは画面遷移前と遷移後と、どちらで取得することもできますが、ここでは画面遷移前に取得します。遷移後の画面ではベンダーの情報を表示するために、ViewModelのベンダー情報をViewでバインドします。遷移後で取得した場合、遷移後のViewModelのプロパティでnullを許可する必要があります。逆に、遷移前に取得してから渡す場合、コンストラクターで注入することができるため、nullは不許可に設計できます。そのため、遷移前に取得してから渡すことにします。

cs
[Navigate]
public partial class RequiringPurchaseProductsViewModel :
INavigatedAsyncAware
{
[RelayCommand]
private async Task PurchaseAsync()
{
var vendor = await _vendorRepository.GetVendorByIdAsync(_selectedRequiringPurchaseProduct!.VendorId);

        await _presentationService.NavigateToRePurchasingAsync(vendor);
    }
}
　遷移Commandは、メニューからの遷移と同様にRelayCommandを利用して生成します。やはり便利です。特に非同期メソッドも何も考えずに利用できるのは、非常に便利です。またKamishibaiを利用することで、画面遷移時の引数を、つぎの画面のコンストラクターに型安全に渡すことができます。

さて、Vendor関連を実装ビューに反映しましょう。


悪くないですね。

選択されたベンダーと再発注が必要な製品の一覧を表示する

この画面では、ベンダーの情報と、再発注が必要な製品の一覧を表示します。この画面でアーキテクチャ的に重要なポイントは、プルダウンコントロールくらいでしょうか。

プルダウンもなかなか奥深いコントロールです。業務アプリケーションのプルダウンとなると、個人的には検索可能なプルダウンはどうしても欲しいと思います。そうなるとやはり自作は大変です。ということで、やはりサードパーティのコンポーネントスイートを利用したほうが良いでしょう。

サードパーティのコンポーネントスイートを利用する場合、プルダウンのような標準に含まれるコンポーネントがある場合もありますが、基本的にはコンポーネントスイート側のものを利用するようにします。これはデザインのテイストなどを統一するためです。

発注する
　さて、発注するにあたって、要再発注製品一覧画面のクエリーオブジェクトから、発注オブジェクトを作成する必要があります。ここでドメイン駆動設計における集約の悩みが生まれてきます。

下図は、左がデータモデルで、右が今回設計したドメインモデルです。


どちらも5つのオブジェクトが登場します。細かい名前は、それぞれのお作法や視点がやや異なるためです。

データモデル	ドメインモデル	説明
Vendor	Vendor	製品の発注先ベンダー
ProductVendor	VendorProduct	製品の取り扱いベンダー。発注ドメインから見た場合、Vendorの子オブジェクトなので名前を入れ替えた。
Product	Product	製品
PurchaseOrderHeader	PurchaseOrder	発注。ドメインモデル側でHeaderがないのは、一般的に集約的なオブジェクトがあった場合、ルートオブジェクトは集合全体を扱う名称にすることが多いため。
PurchaseOrderDetail	PurchaseOrderDetail	発注明細
ShipMethod	ShipMethod	支払方法
　最大の違いは見ての通り、オブジェクト間の関係です。

データモデルは、RDBであるSQL Serverのサンプルデータベースですから、リレーションモデルで設計されています。十分に正規化されており、関連するオブジェクト間の関係はすべてリレーションで表現されています。

ドメインモデルは、集約の単位でリレーションが分割されています。ドメインモデルではリレーションで表現されていたけど、ドメインモデルではリレーションで表現されていない箇所は、IDで表現されています。ひとつながりのオブジェクトの範囲をドメイン駆動設計では集約と呼びます。

オブジェクト指向設計でRDBと同じリレーショナルモデルを表現した場合、巨大になりすぎてしまいます。

もちろん遅延リードのような仕組みを利用することもできます。しかし、遅延リードのような仕組みでは、いつ遅延リードが行われるのか、透明性が失われます。遅延リードの実行有無によってオブジェクトの一貫性が損なわれることもあります。また遅延リードを行うということは、エンティティ内にリポジトリーの仕組みを含めることになります。これはエンティティを複雑化し、エンティティがドメインロジックの本質に注力することが難しくなります。

このような理由から遅延リードのような仕組みは基本的に用いず、集約を設計することが推奨されています。

ではデータモデルのような連続的なリレーショナルモデルから、集約のような不連続のモデルを設計するとき場合、どのような指針に基づくべきでしょうか？

オブジェクト指向において、オブジェクト間の関連は大きく次の3種類に分けられます。

関係	説明
関連 (Association)	オブジェクト間の一般的な関係を示します。関連は、通常、一つのオブジェクトが別のオブジェクトを使用する場合に発生します。関連は双方向または単方向があり、一対一、一対多、多対多などの多重度を持つことができます。図上の実線で表す。
共有集約 (Aggregation)	オブジェクト間の"全体-部分"関係を表します。この関係では、あるオブジェクトが他のオブジェクトの集合を持ち、それらをグループ化しています。ただし、部分オブジェクトは独立して存在でき、複数の集約オブジェクトと共有されることがあります。共有集約では、全体オブジェクトが破棄されても部分オブジェクトは存続します。図上で親側に白抜きの菱形のついた実線で表す。未登場。
コンポジション (Composition)	より強い"全体-部分"関係を表します。これは、あるオブジェクトが他のオブジェクトの集合を持ち、それらをグループ化している点では共有集約に似ていますが、部分オブジェクトは全体オブジェクトに完全に依存しています。全体オブジェクトが破棄されると、部分オブジェクトも自動的に破棄されます。この関係は独占的で、部分オブジェクトは一つの全体オブジェクトにのみ属します。図上で親側に黒塗りの菱形のついた実線で表す。
　下図は、先ほどのオブジェクトの概念を純粋にモデル化したものです。


発注はベンダーに対して行われ、ベンダーが存在しなければ、その発注は発生しません。同様に発注明細も発注の一部です。発注と配送方法の間には関連があります。しかし、どちらがどちらの一部とは言えません。

ベンダーと製品の関係も同様ですが、それらの間にはベンダーがどのような製品を取り扱っているのか表す、ベンダー製品という概念が存在します。発注明細は製品が存在しなければ発生しませんが、製品からみると発注明細は、製品の一部ではないため、関連として表現しています。

これらを集約として分割していくにあたり、私はつぎのような指標を設けています。

関係	説明
関連 (Association)	集約に含めない
共有集約 (Aggregation)	集約に含めない
コンポジション (Composition)	基本的に含めるが、つぎの2つは含めない
1. マスター・トランザクションのようなライフサイクルが大幅に異なるオブジェクト
2. 画像や動画のような大きなバイナリー
　この指標にしたがって分割した結果が下図の通りです。


さて、ここで私が一番悩んだのは、発注オブジェクト（PurchaseOrder - PurchaseOrderDetail）をどのように組み立てるか？ ということです。PurchaseOrderオブジェクトを作って、そこにPurchaseOrderDetailを1つずつ追加していくのがもっとも直感的ではあります。しかし実はそれは困難です。

一番分かりやすいのは、配送料（Freight）です。配送料は、発注対象の総重量に配送方法のグラムあたりの単価である配送レートを掛けたものです。そのため配送料は、発注明細が追加されるたびに再計算する必要があります。しかし、発注オブジェクトには配送方法は含まれないため、配送料を計算することができません。

一番簡単な解決策は、配送ボタンを押下された際に呼ばれるViewModelで、配送明細をすべて作成して、配送料を計算してから配送オブジェクトを生成する方法です。しかしそうしてしまうと、ViewModel側にビジネスロジックが漏れ出してしまいます。何のためのドメイン層なのかということになってしまいます。

そこで今回はビルダークラスを作成して解決することとしました。コードを見ながら説明しましょう。まずはビルダークラスで使うコードです。

cs
// ビルダー生成に必要なオブジェクトを集めて、発注ビルダーを生成する
EmployeeId employeeId = ...
Vendor vendor = ...
ShipMethod shipMethod = ...
Date orderDate = ...
var builder = new PurchaseOrderBuilder(employeeId, vendor, shipMethod, orderDate);

// 要再発注製品の一覧
IEnumerable<RequiringPurchaseProduct> requiringPurchaseProducts = RequiringPurchaseProducts;
foreach (var requiringPurchaseProduct in requiringPurchaseProducts)
{
var product = await _productRepository.GetProductByIdAsync(requiringPurchaseProduct.ProductId);
// 製品と数量を指定して明細を追加する。
builder.AddDetail(product, requiringPurchaseProduct.PurchasingQuantity);
}

// 発注をビルドする
PurchaseOrder purchaseOrder = builder.Build();
　ビルダークラスは、発注オブジェクトを生成するために必要な情報を集めて、最後にBuildメソッドを呼び出すことで、発注オブジェクトを生成します。

エンティティがビルダーとしての役割を兼ねることもできますが、今回のようにできないことがあります。その場合、ビルダーとして機能は専用クラスをドメイン側に用意することを考えると良いでしょう。

その際にビルダークラスに配送方法を渡しておくことで、ビルド時に配送料を計算することが可能になります。なお下記のコードは問題にフォーカスするため、実際のコードとはやや異なります。

cs
private readonly List<(Product Product, Quantity Quantity)> _details = new();

public void AddDetail(Product product, Quantity quantity)
{
_details.Add((product, quantity));
}

public PurchaseOrder Build()
{
DollarPerGram shipRate = _shipMethod.ShipRate;
Gram totalWeight = _details
.Select(x => x.Product.Weight * x.Quantity)
.Sum();
Dollar freight = shipRate * totalWeight;
　やっとバリューオブジェクトの価値を説明することができるようになりました。

配送料は、発注対象の総重量に配送方法のグラムあたりの単価である配送レートを掛けたものです。このとき配送レートや重量、金額はintはdecimalといった .NETで提供されるプリミティブ型を利用して実装することもできます。しかしその場合、誤って別の数字を計算にもちいてしまってバグが発生する可能性があります。

そこで重量単価（DollarPerGram）、グラム、ドルといったバリューオブジェクトを作成して、それらを使って計算することで、誤って別の数字を計算にもちいてしまうことを防ぐことができます。

特に業務アプリケーションの場合、多様な数値を扱います。このときにプリミティブ型を利用してしまうと、どの数値がどのような意味を持っているのかが分かりにくくなってしまいます。そこでバリューオブジェクトを利用することで、数値の意味を明確にできます。

例えばグラムは足し引きはできますが、掛け算はできません。しかしグラムをintで扱ってしまうと、掛け算を防ぐことはできません。そういったドメインロジックを値ごとに明確に実装できることがバリューオブジェクトの価値です。

業務アプリケーションの場合、本当に多様なバリューオブジェクトが発生してきます。本稿ではバリューオブジェクトの実装にUnitGeneratorライブラリを利用しています。非常に便利なライブラリなのでオススメです。

こうしてビルドしたPurchaseOrderオブジェクトはリポジトリーを利用して登録します。リポジトリーの呼び出し移行は、ロガーの処理と変わりがないので、ここでは説明を省略します。

このあたりのドメイン駆動的な設計は、ユースケースの実現を通すことで見えてきます。

発注が成功したことを、メッセージダイアログで表示する
　アラートの表示はKamishibaiでViewModel側から利用する仕組みが用意されているので、これを利用します。

cs
_presentationService.ShowMessage(Purchasing.ViewModel.Properties.Resources.RegistrationCompleted);
要再発注製品一覧画面に遷移する
　要再発注製品一覧画面に遷移するには、KamishibaiのIPresentationServiceを利用して、前の画面に戻ります。

cs
await _presentationService.GoBackAsync();
再発注の必要な製品を表示する
　要再発注製品一覧画面を最初に遷移してきたときは、INavigatedAsyncAwareのOnNavigatedAsyncを実装して、再発注の必要な製品を表示しました。INavigatedAsyncAwareは画面が前に遷移したときに呼ばれるインターフェイスです。

戻ってきたときはIResumingAsyncAwareを利用します。

cs
[Navigate]
[INotifyPropertyChanged]
public partial class RequiringPurchaseProductsViewModel :
INavigatedAsyncAware,
IResumingAsyncAware
{
・・・
public async Task OnResumingAsync(PreBackwardEventArgs args)
{
RequiringPurchaseProducts.Replace(
await _requiringPurchaseProductQuery.GetRequiringPurchaseProductsAsync());
}
　実際の処理自体は変わりません。

これで「再発注する」ユースケースが実装できました。これでデータソーサのCRUDを実現する目途が立ちました。

開発者ビュー
　さて、ここまでは開発する対象のアーキテクチャを設計してきました。ここからは、開発対象の周囲の環境を中心とした開発者ビューを設計します。

バージョン管理戦略
コーディング規約
テスト戦略
ビルド戦略
デプロイ戦略
リリース戦略
　これらすべてがアーキテクチャかというと、コーディング規約など、あまり一般的ではないものも含まれます。ただ私は「アーキテクチャとは技術的に重要な決定のすべて」という論を推しています。そのため、開発者ビューもアーキテクチャの一部として設計することにしています。

開発の各フェーズを想定して、他の文書で規定されない技術的な決定は、少なくともいったんは開発者ビューに入れてしまってよいと考えています。必要になったときに、個別の文書などに切り出すことを検討します。

概要を設計する
　上記の要素を設計していくにあたって、多くの要素は独立しているわけではなくて、複雑に絡み合っています。そのため、ざっくり概要を設計した上で詳細に落とし込んでいきます。

またそのためには、開発の背景が必要になってきます。短いサイクルでリリースし続けるシステムなのか、短くても3カ月〜半年くらいのサイクルでリリースされるシステムなのか。どの程度クリティカルなシステムなのか。などなど、開発の背景を把握しておく必要があります。

そこで本稿では、つぎのような背景を想定して設計を進めていきます。

開発対象のシステムは、業務上重要なシステムであり、基幹システムとして認定されていて、それにふさわしい開発品質を保証する必要がある。

開発対象のシステムは、3か月～半年くらいのサイクルでリリースされる。重要度の低い不具合は、次期リリースのタイミングでリリースされるが、重要度の高い不具合は、緊急リリースとしてリリースされることもある。ただし緊急リリースの頻度は十分に低い。

システムの利用者と開発者は異なる組織であり、開発者は通常、システムが運用されるネットワークとは異なる環境で開発している。

開発サイドのテストが完了した後、運用環境に持ち込み、運用環境と同一ネットワーク上にある受入テスト環境でテストを行い、問題なければ本番環境にリリースする。

上記のような背景があるためCIは実施しますが、CDに関してはビルドしたモジュールをビルドモジュールの配置場所に配置するところまでを行うこととします。手動テストの実施は、配置場所からテスト環境につどデプロイして実施します。その際、可能な限りスクリプトによってデプロイする方針とします。これによって、受入テストや本番環境へのリリースに対するテストが、常時開発環境で行われることとなり、リリース品質の向上が見込めます。

では個別の要素について設計していきましょう。

バージョン管理戦略
　バージョン管理システムにはGitHubを使います（あくまで本稿での決定で、GitHubがもっともよいという意味合いではありません）。

近年のAIの急速な普及や、それらのエコシステムへの取り組み事情を考えたときに、GitHubを利用することで品質や生産性を最大化できると考えました。

さて、Gitを利用する場合、バージョン管理のブランチ戦略を考える必要があるでしょう。Gitのブランチ戦略として一般的によく知られているものとして、Gitflow Workflow、GitHub Flow、およびGitLab Flowの3つが挙げられます。

AdventureWorksではGitflow Workflowを採用することとします。

Gitflow Workflowは、大規模なプロジェクトや複数の開発者が関与するプロジェクトに適しています。この戦略では、以下のような役割ごとのブランチが利用されます。

ブランチ	説明
main	本番環境用で、リリース済みのコードが保管されるブランチです。
develop	開発用ブランチで、機能追加やバグ修正のコミットが行われる場所です。
feature	新機能開発用のブランチで、developブランチから派生し、開発が完了したらdevelopにマージされます。
release	リリース前の最終調整用のブランチで、developから派生し、準備が整ったらmasterとdevelopにマージされます。
hotfix	緊急のバグ修正用のブランチで、masterから派生し、修正が完了したらmasterとdevelopにマージされます。
　他の2つのブランチ戦略との違いの1つにdevelopブランチの有無があります。リリースサイクルが長めの開発では、mainブランチとdevelopブランチが分かれていると扱いやすいと考えています。

GitLab Flowのmainブランチとenvironmentブランチの関係も似ていますが、受入テストが完了したモジュールを、そのまま本番環境に昇格する運用としたいため、Gitflow Workflowを採用することとします。

なお私個人は、Gitflow Workflowのreleaseブランチを省略した形で運用しているケースが多いです。developブランチでリリースモジュールも作りこんでいく形で運用しています。

コーディング規約
　開発上利用する言語のコーディング規約は、早い段階で決めておくことが望ましいです。コーディング規約を0から書き上げるのは非常に大変な作業です。そこで、既存のコーディング規約を参照しつつ、必要に応じて変更を加えていくことをオススメします。

例えばC#であれば、私はMicrosoftの公式コーディング規約をベースにしています。

ただしprivateまたはinternalであるstaticフィールドを使用する場合の、「s_」プレフィックスと、スレッド静的なプレフィックスの「t_」は受け入れがたいため、これらのプレフィックスを使用しないようにしています。

そういった形で、一般的なコーディング規約をベースに、マッチしない部分だけをカスタマイズしています。

テスト戦略
　WPFのテスト戦略を考えたとき、アーキテクチャとして重要になるのは、どこまでを自動テストとして含めるか？ という点です。

WPFを操作した場合、おおむね次のようなフローとなります。

ユーザーがViewを操作する
ViewがViewModelのコマンドを呼び出す。またはバインドされたプロパティを更新する
ViewModelがユースケースもしくはドメイン層をよびだす
呼び出されたドメイン層の実体はgRPCのクライアントで、サーバーサイドを呼び出す
サーバーサイドでドメイン層の実体が実行される
　このとき、自動テストとしてどこまでを含めるべきでしょうか？ 選択肢としては現実的に次の2つが考えられます。

ViewModelの振る舞いまでを含める
サーバーサイドの振る舞いまで含める
　基本的に前者の方が低コストで実施できて、後者の方がテスト価値は高いです。

後者まで実現できれば、システムのリグレッションテストとして利用できるため、テストは非常に高い価値をもたらしてくれるでしょう。しかし、実際には非常に多くの課題を解決する必要がでてきます。

現実的には、サーバーサイドの実装までほぼ完成した段階にならないと、テストも完成しないことになります。結合テストまで完了した状態にならないと、テストも完成しませんし、多くのプロジェクトではそのタイミングでテストに十分に投資するコストも時間も残っていないことが経験上多いです。

これを解決するためには、プロジェクトの受託前から顧客とも、システムを結合した状態でのテストを維持するコストを支払い続けられるか、入念に話し合っておく必要があります。

また、後者はある程度テストに習熟した組織でないと運用は難しいでしょう。前者をまずは導入してみて、十分な経験を得てから後者に取り組んでいくというのも現実的な選択肢です。

さて、WPF単体のアーキテクチャの視点で考えると、実は前者のケースの方が考えるべきことが多いです。というのは、後者のケースはシステムとしてはほぼ完成していて、テストのためのアーキテクチャとして考えることは、テストケースごとにテストデータをどう入れ替えるか？ くらいだからです。

逆に前者の場合、ViewModelより下の層をスタブとして入れ替える仕組みが必要になります。しかもテストケースによってスタブの振る舞いは変える必要があるため、そのアーキテクチャを決定する必要があります。

今回、予算編でも説明した通り、WPFのテストはOSSのテストフレームワークであるFriendlyと、その有償サポートツールのTest Assistant Proを利用します。

UIのテストフレームワークは多数ありますが、一般的なキャプチャー＆リプレイ方式のツールは、テストを維持するのが非常に厳しいと感じてきました。何らかの修正が入ったとき、テストをキャプチャーしなおさないといけないのは、非常に手間です。それに対してFriendlyをつかったテストコードは非常にメンテナンス性が高いです。

下記のコードは、ユースケースの実現で記載したシナリオ「再発注する」をテストするコードです。

cs
[Test]
public void 再発注する()
{
var mainWindow = _app.AttachMainWindow();
var menuPage = _app.AttachMenuPage();
var rePurchasingPage = _app.AttachRePurchasingPage();
var requiringPurchaseProductsPage = _app.AttachRequiringPurchaseProductsPage();

    ////////////////////////////////////////////////////////////////////////////
    // MenuPage
    ////////////////////////////////////////////////////////////////////////////
    // メニューから再発注を選択し、RequiringPurchaseProductsPageへ移動する。
    menuPage.NavigateRePurchasing.EmulateClick();
    mainWindow.NavigationFrame.Should().BeOfPage<RequiringPurchaseProductsPage>();


    ////////////////////////////////////////////////////////////////////////////
    // RequiringPurchaseProductsPage
    ////////////////////////////////////////////////////////////////////////////
    // グリッドの表示行数の確認
    requiringPurchaseProductsPage.RequiringPurchaseProducts.RowCount.Should().Be(9);
    // 選択済みベンダーの確認
    requiringPurchaseProductsPage.SelectedRequiringPurchaseProductVendorName.Text.Should().Be("Vendor 1");

    // 発注ボタンを押下し、RePurchasingPage画面へ遷移する。
    requiringPurchaseProductsPage.PurchaseCommand.EmulateClick();
    mainWindow.NavigationFrame.Should().BeOfPage<RePurchasingPage>();

    ////////////////////////////////////////////////////////////////////////////
    // RePurchasingPage
    ////////////////////////////////////////////////////////////////////////////
    // 発注ボタンを押下し、登録完了ダイアログで、OKを押下する。
    var async = new Async();
    rePurchasingPage.PurchaseCommand.EmulateClick(async);
    var messageBox = _app.Attach_MessageBox(@"");
    messageBox.Button_OK.EmulateClick();
    async.WaitForCompletion();

    // RequiringPurchaseProductsPage画面へ戻る
    mainWindow.NavigationFrame.Should().BeOfPage<RequiringPurchaseProductsPage>();

    ////////////////////////////////////////////////////////////////////////////
    // RequiringPurchaseProductsPage
    ////////////////////////////////////////////////////////////////////////////
    // 発注した商品が減っていることを確認する。
    requiringPurchaseProductsPage.RequiringPurchaseProducts.RowCount.Should().Be(7);
}
　Friendlyを知らない方でも、何をやっているかだいたい理解できるコードになっているかと思います。Friendlyの詳細はここでは説明を省略します。公式のGitHubをご覧ください。

またFriendlyとTest Assistant Proを作成しているのは日本の会社なので、サポートや導入コンサルティングを日本語で受けられるのも大きなポイントです。Friendlyではテストを実施するときに、テスト対象のWPFアプリケーションを別プロセスで起動して、そこにアタッチする形でテストを実行します。

cs
var mainWindow = _app.AttachMainWindow();
　購買管理WPFアプリケーションのエントリーポイントはAdventureWorks.Business.Purchasing.Hosting.Wpfです。テストのときにこれを呼び出すと、バックエンドにつながってしまいます。そのためテスト用のスタブに切り替えてあげる必要があります。しかもテストケースごとにスタブが入れ替えられると便利でしょう。

そこでテスト用のエントリーポイント用のプロジェクトを作成しましょう。そしてそこでスタブを切り替えるようにします。Friendlyでテストアプリケーションを起動するとき、つぎのようにテスト用のエントリーポイントを相対パスで指定できます。指定するのはビルドされたexeになります。

cs
public static WindowsAppFriend Start()
{
//target path
var targetPath = @"..\..\..\..\AdventureWorks.Purchasing.App.Driver\bin\Debug\net6.0-windows\AdventureWorks.Purchasing.App.Driver.exe";
var info = new ProcessStartInfo(targetPath) { WorkingDirectory = Path.GetDirectoryName(targetPath)! };
var app = new WindowsAppFriend(Process.Start(info));
app.ResetTimeout();
return app;
}
　ここで起動されるプロセス環境変数に、NUnitのテスト名を設定して起動します。Friendlyの標準のテストフレームワークはNUnitです。個人的にはxUnitを使うことがおおいのですが、FluentAssertionsというOSSライブラリを使うと、テスト本体の書き方はフレームワーク非依存で、かつ「流ちょうに」記述できるためオススメです。

cs
var info = new ProcessStartInfo(targetPath) { WorkingDirectory = Path.GetDirectoryName(targetPath)! };
info.Environment["TestName"] = context.Test.FullName;
　テストのFullNameには先の例だと「Scenario.RePurchasingTest.再発注する」が設定されます。

これを起動されたテスト用のエンドポイントで読み取ってスタブを切り替えます。

cs
string? testName = Environment.GetEnvironmentVariable("TestName");
　testNameからifやcaseで分岐しても良いのですが、テストが増えてくると分岐の数が大変なことになります。

そこでスタブをDIコンテナーに登録するための共通処理を表すIContainerBuilderインターフェイスを作成します。

cs
public interface IContainerBuilder
{
void Build(IServiceCollection services);
}
　このインターフェイスの実装クラスを、テストケースごとにテスト名の名前空間に作成します。

cs
namespace AdventureWorks.Purchasing.App.Driver.Scenario.RePurchasingTest.再発注する;

public class ContainerBuilder : IContainerBuilder
{
public void Build(IServiceCollection services)
{
// 認証サービスを初期化する。
services.AddTransient<IAuthenticationService, AuthenticationService>();
services.AddTransient<IAuthenticationContext, AuthenticationContext>();
・・・・
　そしてテストのエントリーポイントで、テスト名の名前空間からIContainerBuilderを動的に生成して、Buildメソッドを呼び出します。

cs
var builder = ApplicationBuilder<App, MainWindow>.CreateBuilder();

try
{
string testName = Environment.GetEnvironmentVariable("TestName")!;
var builderName = $"AdventureWorks.Purchasing.App.Driver.{testName}.ContainerBuilder";
var builderTye = Type.GetType(builderName)!;
var builderInstance = Activator.CreateInstance(builderTye) as IContainerBuilder;
builderInstance!.Build(builder.Services);
・・・
　これでテストケースごとにスタブを切り替えることで、例えば要発注対象一覧画面の表示データを切り替えることが可能となります。

ビルド戦略
　ブランチ戦略で記載したように、開発中はPull Requestはdevelopブランチに対して行われます。developブランチはデプロイできる状態を可能な限り保ちます。

そのため、可能であればつぎのようなビルド戦略を取りたいと思います。

Pull RequestをトリガーにCIパイプラインが走ること
CIではビルドとテストを実施すること
テストはWPFにおいてはテスト戦略で記載した自動テストを実施すること
　バージョン管理システムがGitHubなので、CIパイプラインはGitHub Actionsを利用します。GitHub ActionsでWPFアプリケーションのビルドやUI自動テストを実施する方法は、次の記事で紹介しています。参考にどうぞ。

.NETのWPFアプリケーションをGithub Actionsでビルドする
Friendlyで作ったWPFの自動テストをGithub Actions上でテストする（.NET Framework編）
　この際、気を付けておかないといけないことの1つに、GitHub Actionsの実行環境があります。WPFの自動テストを実施しようとした場合、ユーザーインターフェイスを実行する必要があることから、ユーザープロセスでのテストが必要になります。そのためWindowsにログインした環境で、GitHubのテストエージェントをユーザープロセスで実行しておく必要があります。

またUIのテストは非常に時間がかかります。そのため規模が大きくなってきた場合、CIパイプラインの並列化を検討する必要があります。一般的にはクラウド上のVMなどを利用したくなりますが、Pull Requestがいつ来るか分からない中で、常にVMを起動しておくのはコストがかかります。

一番現実的なプランは、「使わなくなったパソコンを大量に確保しておいて、テストエージェントのプールを作ること」である場合も多いです。

またいくら並列化しても、場合によってはPull Requestが更新されるたびにCIを流すことは現実的ではなくなってくる可能性があります。その場合はブランチ戦略を含め、開発者ビュー全体を見直す必要があるかもしれません。

デプロイ戦略
　ビルドが終わったらデプロイします。ただ前述した通り、デプロイ戦略といってもクラウド上に展開するといった意味合いではありません。今回は、ビルドされたWPFアプリケーションやそのインストーラーを、ビルドバージョンごとに保管することを指します。実際にはWPFアプリケーションだけではなく、サーバーサイドや、データベース構築スクリプトなど、同一バージョンは一式セットで保管すると良いでしょう。

この時、コスト的な観点から、私はAzure BLOB Storageに保管しています。

リリース戦略
　今回の背景から、リリースは3種類のリリースを想定します。

開発環境におけるテストリリース（結合・システムテスト）
ユーザー受入テスト向けのリリース
本番リリース
　ただし、モジュールは完全に同じものをもちいて、かつ、可能な限りスクリプトで自動化します。ここまでの中でも触れてきましたが、環境によらず、基本的に実行モジュールはバイナリーレベルで完全に一致する形で運用したいと考えています。

環境に依存するものは環境変数で制御して、インストーラーや環境構築時に一度だけ設定するようにします。こうすることで、リリース誤りを最小限に抑えることができます。

おわりに
　さて要件定義編からここまで、最後までお付き合いいただきありがとうございました。私自身、意気揚々とこの原稿を書き始めたものの、実のところ書いてみるとこれまで自分の開発してきたシステムのアーキテクチャにおいて、検討が不十分と思われる箇所に気が付くことが多く非常に難産となった記事となりました。やや粗削りな箇所もあるかもしれませんが、その分多くのアイディアや知見が詰まっていると思います。

本稿では、詳細まで書きすぎてもむしろ混乱を招いてしまうと思われる点について、意図的に省略して記載した箇所もあります。動作するすべてのコードはGitHub上にあります。ぜひグレープシティさんからComponentOneと、SPREAD for WPFの試用ライセンスを取得して、ご自身の手で動かしながらソースをご覧ください。

また、本稿の内容について、ご意見・ご感想・ご指摘などございましたら、ぜひTwitterの@nuits_jpまでお寄せください。可能な限りご回答いたします。