# Requirements Document

## Project Description (Input)
LevelPlayを用いたリワード広告によるショップ商品の実装

## Introduction

ショップ画面における新しい商品獲得チャネルとして、リワード動画広告 (LevelPlay) の視聴と引き換えに所定のアイテム (家具・着せ替え・毛糸ポイント) をプレイヤーへ付与する機能を追加する。既存のショップ実装 (`Shop.Service.ShopService`, `Shop.State.ShopState`) には `CurrencyType.RewardAd` および `RewardAdProductList` というフックポイントが用意されており、本機能はこれらを稼働させると同時に、リワード広告 SDK の制御を担う新規 `Rewarded Ad Service` を `Root.Service` レイヤに導入する。報酬の付与は既存の `IUserPointService` / `IUserItemInventoryService` を再利用し、View → Service → State の依存方向と VContainer 登録規約を厳守する。Android / iOS の両プラットフォームに対応し、Unity Editor 上ではダミー実装に差し替えることでオフライン開発を可能にする。さらに、商品ごとの 1 日あたり視聴可能回数 (日次キャップ) を設け、リワード広告商品セルには残り視聴回数を表示する。

## Requirements

### Requirement 1: リワード広告SDKの初期化とライフサイクル管理
**Objective:** プレイヤーとして、ゲーム起動後できるだけ早くリワード広告が利用可能になることで、ショップで報酬を獲得できる機会をすぐに得たい

#### Acceptance Criteria
1. When アプリケーションが起動した, the Rewarded Ad Service shall LevelPlay SDK の初期化を非同期で開始する。
2. When LevelPlay SDK の初期化が成功した, the Rewarded Ad Service shall リワード広告ユニットの最初のロード要求を発行する。
3. If LevelPlay SDK の初期化に失敗した, the Rewarded Ad Service shall 初期化失敗状態を保持し、その後の視聴要求を即時に「準備未完了」として失敗で返却する。
4. While LevelPlay SDK の初期化が未完了である, the Rewarded Ad Service shall 「準備中」状態を呼び出し側へ表明する。
5. The Rewarded Ad Service shall 初期化および広告制御の API を `IRewardedAdService` 経由のみで提供し、ショップ層を含むいかなる呼び出し側からも LevelPlay SDK 型への直接参照を許可しない。

### Requirement 2: リワード広告のロードと自動再ロード
**Objective:** プレイヤーとして、ショップを開いた直後にも待ち時間なくリワード広告を視聴できるよう、広告がバックグラウンドで先読みされていてほしい

#### Acceptance Criteria
1. When LevelPlay SDK の初期化が完了した, the Rewarded Ad Service shall リワード広告ユニットを 1 件ロード開始する。
2. When リワード広告のロードが成功した, the Rewarded Ad Service shall 「視聴可能」状態を保持し、呼び出し側が `IsReady` プロパティで問い合わせ可能にする。
3. When リワード広告の再生が完了して閉じられた, the Rewarded Ad Service shall 次回視聴に備えて新規ロード要求を発行する。
4. If リワード広告のロードに失敗した, the Rewarded Ad Service shall 指数バックオフによって一定の上限回数まで再試行する。
5. If 連続するロード失敗回数が上限を超えた, the Rewarded Ad Service shall ロード再試行を停止し、エラー状態を呼び出し側へ表明する。
6. When 「視聴可能」状態が変化した, the Rewarded Ad Service shall 状態変化イベントを呼び出し側へ通知する。

### Requirement 3: ショップ画面におけるリワード広告商品の表示
**Objective:** プレイヤーとして、ショップ画面でどの商品がリワード広告視聴によって獲得できるかを一目で識別したい

#### Acceptance Criteria
1. When プレイヤーがショップ画面を開いた, the Shop Service shall マスターデータから `CurrencyType.RewardAd` の商品を抽出し、`ShopState.RewardAdProductList` を更新する。
2. When `ShopState.RewardAdProductList` が更新された, the Shop View shall リワード広告商品セルをショップ画面上のリワード広告枠に描画する。
3. While Rewarded Ad Service の状態が「視聴可能」かつ当該商品の日次残り回数が 1 以上である, the Shop View shall リワード広告商品セルの視聴ボタンを操作可能状態として表示する。
4. While Rewarded Ad Service の状態が「視聴不可」 (準備中・ロード失敗・視聴中のいずれか) である, the Shop View shall リワード広告商品セルの視聴ボタンを非操作状態として表示する。
5. If リワード広告商品が以下のいずれかに該当する, the Shop Service shall 当該セルを売り切れ状態として扱い、 the Shop View shall 既存の売り切れ表示を適用する:
   - `ItemType` が `Outfit` でかつ既に所持済みである
   - 当該商品の本日残り視聴回数が 0 である
6. The Shop Service shall 通常 (毛糸) 商品の購入フローと共存できる形でリワード広告商品を取り扱い、時限ショップ抽選 (`TimedShopLottery`) の対象からは除外を維持する。

### Requirement 4: リワード広告の視聴フロー
**Objective:** プレイヤーとして、リワード広告商品セルをタップしたら、確認・広告再生・報酬付与が一連のフローとして滞りなく完結してほしい

#### Acceptance Criteria
1. When プレイヤーがリワード広告商品セルをタップした, the Shop Service shall 視聴前確認ダイアログを `IDialogService` 経由で表示する。
2. When プレイヤーが確認ダイアログで承認を選んだ, the Shop Service shall `IRewardedAdService.ShowAsync` を呼び出してリワード広告を再生開始する。
3. When プレイヤーがリワード広告を最後まで視聴した, the Rewarded Ad Service shall 視聴結果として「報酬獲得」を呼び出し側へ返却する。
4. When 視聴結果として「報酬獲得」が返却された, the Shop Service shall 商品の `ItemType` に応じて以下のいずれかへ付与処理をディスパッチする:
   - `Furniture` の場合は `IUserItemInventoryService.AddFurniture(ItemId, 1)`
   - `Outfit` の場合は `IUserItemInventoryService.GrantOutfit(ItemId)`
   - `Point` の場合は `IUserPointService.AddYarn(マスター定義の付与数)`
5. If プレイヤーがリワード広告を最後まで視聴せずに閉じた, the Rewarded Ad Service shall 視聴結果として「途中離脱」を呼び出し側へ返却する。
6. If 視聴結果として「途中離脱」が返却された, the Shop Service shall 報酬付与を行わない。
7. When 報酬付与が完了した, the Shop Service shall 付与内容を反映した結果メッセージダイアログを `IDialogService` 経由で表示する。

### Requirement 5: 視聴結果の確定と二重付与防止
**Objective:** プレイヤーおよび運営として、1 回の広告視聴で報酬が確実に 1 回だけ付与されることを保証したい

#### Acceptance Criteria
1. The Rewarded Ad Service shall 1 回の `ShowAsync` 呼び出しに対して 1 件の終端結果のみを返却する。
2. While 1 回の視聴セッションが進行中である, the Shop Service shall 同じセルおよび他のリワード広告セルへの新規タップを受け付けない。
3. When LevelPlay SDK から `OnAdRewarded` と `OnAdClosed` が両方発火した, the Rewarded Ad Service shall それらの発火順序に依らず正しく 1 件の「報酬獲得」を確定し、二度通知しない。
4. If 同じ視聴セッションで `OnAdClosed` のみが発火し `OnAdRewarded` が発火しなかった, the Rewarded Ad Service shall 視聴結果として「途中離脱」を返却する。
5. If 視聴中に表示失敗イベント (`OnAdDisplayFailed`) が発生した, the Rewarded Ad Service shall 視聴結果として「表示失敗」を返却し、 the Shop Service shall 報酬付与を行わない。
6. The Shop Service shall 報酬付与処理を冪等に呼び出しても 1 セッション 1 回しか反映しないよう、視聴セッション単位で完了フラグを管理する。

### Requirement 6: 失敗時のユーザー通知
**Objective:** プレイヤーとして、リワード広告が利用できないときに何が起きているのかを明確に知りたい

#### Acceptance Criteria
1. If リワード広告の準備が未完了な状態でプレイヤーが視聴ボタンに触れようとした, the Shop View shall ボタンを非操作状態としタップ自体を成立させない。
2. If リワード広告の表示に失敗した, the Shop Service shall 「広告を再生できませんでした」旨のメッセージダイアログを `IDialogService` 経由で表示する。
3. If 視聴結果として「途中離脱」が返却された, the Shop Service shall 「広告の視聴が中断されました」旨のみを伝えるメッセージダイアログを `IDialogService` 経由で表示する。
4. If LevelPlay SDK が一定時間内に初期化を完了しない, the Shop View shall リワード広告枠を「準備中」状態として表示する。
5. If 報酬付与処理が失敗した, the Shop Service shall エラーをログに記録し、結果メッセージにて付与失敗を表示する。

### Requirement 7: プラットフォーム別挙動とテスト容易性
**Objective:** 開発者として、Unity Editor 上でも実機ビルド上でもリワード広告フローを安全に検証できるようにしたい

#### Acceptance Criteria
1. Where 実行環境が Unity Editor である, the Rewarded Ad Service shall LevelPlay SDK を呼び出さず、即時に「報酬獲得」を返却するスタブ実装に切り替わる。
2. Where ビルドプラットフォームが Android である, the Rewarded Ad Service shall Android 用 App Key および Rewarded Ad Unit ID を使用する。
3. Where ビルドプラットフォームが iOS である, the Rewarded Ad Service shall iOS 用 App Key および Rewarded Ad Unit ID を使用する。
4. Where ビルドプラットフォームが iOS である, the Rewarded Ad Service shall ATT (App Tracking Transparency) のユーザー応答が確定するまで広告ロードを開始しない。
5. The Rewarded Ad Service shall App Key / Ad Unit ID をコードリテラルではなく構成アセット (ScriptableObject 等) から取得する。
6. The Rewarded Ad Service shall 全コールバック処理を Unity メインスレッド上で完了させ、`UniTask` ベースの呼び出し側 API がメインスレッド前提の処理を安全に行えるようにする。

### Requirement 8: アーキテクチャと依存方向の遵守
**Objective:** 開発者として、リワード広告機能の追加が既存の VContainer ベースの依存方向ルールおよびコーディング規約を破らないようにしたい

#### Acceptance Criteria
1. The Rewarded Ad Service shall `Root.Service` 名前空間に配置され、`RootScope` で `Lifetime.Singleton` として登録される。
2. The Rewarded Ad Service shall `IRewardedAdService` インターフェースを実装し、Shop 層を含む全ての呼び出し側はインターフェース経由のみで利用する。
3. The Rewarded Ad Service shall 非同期 API として `UniTask<RewardedAdResult> ShowAsync(string placementName, CancellationToken ct)` を提供する。
4. The Rewarded Ad Service shall View 層 (`MonoBehaviour` を含む) を直接参照せず、View → Service → State の依存方向を維持する。
5. The Rewarded Ad Service shall VContainer 注入用コンストラクタに `[Inject]` 属性を付与し、IL2CPP のストリッピング対策を満たす。

### Requirement 9: マスターデータ定義とリワード広告商品の構成
**Objective:** 運営として、リワード広告で提供する商品種別・付与数・日次上限回数を、コードを変更せずマスターデータのみで管理したい

#### Acceptance Criteria
1. The Master Data Schema shall `CurrencyType.RewardAd` を取り得る `ShopProduct` レコードを既存マスターデータ (`shop_products.csv` 等) に追加可能とする。
2. The Master Data Schema shall `ShopProduct` に「付与数」フィールドを追加し、 `ItemType.Point` の商品では当該フィールドの値を `IUserPointService.AddYarn` の引数として利用できるようにする。
3. The Master Data Schema shall `ShopProduct` に「日次視聴可能回数」フィールドを追加し、リワード広告商品ごとに値を指定できるようにする。
4. If `ShopProduct` の「日次視聴可能回数」フィールドが未指定もしくは 0 以下である, the Shop Service shall リワード広告商品向けの既定値 (例えば定数 `RewardAdDailyCapDefault`) を適用する。
5. When マスターデータの再インポートが行われた, the Shop Service shall `ShopState.RewardAdProductList` を再構築し、最新の商品集合・付与数・日次上限を反映する。
6. The Rewarded Ad Service shall 視聴結果の付与対象商品をマスター由来の `ItemId` / `ItemType` / 付与数のみで決定し、画面上の表示情報 (アイコンや名称) には依存しない。
7. Where 同一 `ItemType` の商品が複数定義されている, the Shop View shall それらをリワード広告枠内で `ShopProduct.Id` 昇順で表示する。
8. The Master Data Schema shall 既存の非リワード広告商品 (`Yarn` 通貨) における付与数フィールドの扱いを破壊せず、後方互換性を維持する。

### Requirement 10: 日次視聴回数の管理とリセット
**Objective:** プレイヤーおよび運営として、1 商品あたりの 1 日視聴回数に上限を設けることで、報酬経済のバランスを保ちつつ過度な広告露出を避けたい

#### Acceptance Criteria
1. The Shop Service shall リワード広告商品ごとに「当日の視聴消化回数」をクライアント側で保持し、永続化する。
2. When リワード広告商品に対する報酬付与が成功した, the Shop Service shall 当該商品の「当日の視聴消化回数」を 1 加算し、永続化する。
3. While ある商品の「当日の視聴消化回数」が当該商品のマスター定義 (もしくは既定値) と等しい, the Shop Service shall 当該商品を「本日視聴不可」状態として扱い、視聴フローを開始させない。
4. When 日付境界 (ローカル時刻 0:00) を跨いだ, the Shop Service shall 全リワード広告商品の「当日の視聴消化回数」を 0 にリセットする。
5. The Shop Service shall 日付判定に直接 `DateTime.Now` / `DateTime.UtcNow` を呼ばず、必ず `IClock` 経由で現在時刻を取得する。
6. If リセット後初めて当該商品が参照された, the Shop Service shall 永続化されたリセット日付と現在日付の差分を検出して遡及的にリセットを適用する。
7. The Shop Service shall リワード広告商品が時限ショップ抽選結果のサイクル更新によって変動しても、消化回数を商品 ID 基準で保持し、誤って別商品にカウントが流用されないようにする。

### Requirement 11: 残り視聴回数の表示
**Objective:** プレイヤーとして、リワード広告商品セル上で本日あと何回まで視聴できるかを一目で把握したい

#### Acceptance Criteria
1. The Shop View shall リワード広告商品セルに専用テキストを設け、当該商品の残り視聴回数を `n/m` 形式 (n = 残り回数、m = 1 日上限) で表示する。
2. When 視聴フローが完了し付与が成立した, the Shop View shall 当該セルの `n/m` テキスト表示を再計算し最新値に更新する。
3. While 当該商品の本日残り回数が 0 である, the Shop View shall `n/m` テキストを `0/m` として表示し、Requirement 3 のクライテリア 5 に従って既存の売り切れ表示を併用する。
4. When 日付境界を跨いで消化回数がリセットされた, the Shop View shall 開いているショップ画面の `n/m` テキストを `m/m` に更新する。
5. The Shop View shall `n/m` テキストの取得をマジックナンバーではなく Shop Service が提供する API (例: `GetDailyRemainingCount(productId)`) 経由で行う。
