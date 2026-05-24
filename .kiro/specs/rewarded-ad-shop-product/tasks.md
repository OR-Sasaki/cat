# Implementation Plan: rewarded-ad-shop-product

## Branches

**Base**: `feature/rewarded-ad-shop-product`

| Branch | Tasks | Goal |
|--------|-------|------|
| `feature/rewarded-ad-shop-product-master-data` | 1 | マスターデータスキーマ拡張完了。既存 Yarn 商品の動作を破壊せず、Amount/DailyCap 列を受理する |
| `feature/rewarded-ad-shop-product-ad-service` | 2-5 | 広告 SDK 抽象と LevelPlay/Editor 実装、起動時初期化までが稼働。`IRewardedAdService.IsReady` がコンソールから確認できる |
| `feature/rewarded-ad-shop-product-shop-integration` | 6-11 | ショップ画面で視聴フロー全体が完結する。日次キャップ・残数表示・売り切れ統合まで動作 |

## Tasks

### Branch: `feature/rewarded-ad-shop-product-master-data`

- [x] 1. マスターデータスキーマと CSV パーサを 8 カラムに拡張
  - [x] 1.1 (P) ShopProduct / ProductData レコードに付与数と日次上限の属性を追加
    - ShopProduct に `Amount` (int) と `DailyCap` (int?) を末尾に追加
    - ProductData に `Amount` (int) を追加 (既存の `YarnAmount` とは別カラム、デフォルト 1)
    - 既存全コンパイル箇所が引き続き通るよう、コンストラクタ呼び出しに必要なデフォルト/明示値を補う
  - [x] 1.2 MasterDataImportService の shop_products パースを 8 カラム対応に拡張
    - カラム数下限を 6 → 8 に変更し、不足行はスキップしつつ警告ログ
    - `amount` パース: 空文字は 1、数値不正はスキップしつつ警告ログ
    - `daily_cap` パース: 空文字は null、数値不正はスキップしつつ警告ログ
    - 既存の Yarn / RewardAd カレンシー文字列パースは維持
  - [x] 1.3 shop_products.csv の既存全行を 8 カラム化
    - 既存 46 行に空 2 カラム (`,,`) を末尾付与
    - ヘッダ行を `id,name,item_type,item_id,price,currency_type,amount,daily_cap` に更新
    - リワード広告行はこのブランチでは追加しない (Task 10 で追加)
  - [x] 1.4 ShopService 経由で Amount が ProductData へ正しく伝搬することを確認
    - `BuildProductDataFromShopProduct` 相当箇所で `ShopProduct.Amount` を `ProductData.Amount` に転送
    - 既存ガチャ・時限ショップ・通常ショップ全動線で `Amount` が反映され、購入挙動が変わらないことを目視確認
  - _Requirements: 9.1, 9.2, 9.6, 9.7, 9.8_

### Branch: `feature/rewarded-ad-shop-product-ad-service`

- [x] 2. 広告 SDK 抽象と構成アセットを定義
  - [x] 2.1 (P) IRewardedAdService インターフェースと状態・結果列挙を新規定義
    - `RewardedAdState` 列挙: Uninitialized / Initializing / Loading / Ready / Showing / Failed
    - `RewardedAdResult` 列挙: Rewarded / Dismissed / DisplayFailed / NotReady
    - インターフェース API: `State`, `IsReady`, `StateChanged` event, `InitializeAsync(ct)`, `ShowAsync(placementName, ct)`
    - `Root.Service` 名前空間に配置、`#nullable enable` を付与
  - [x] 2.2 (P) RewardedAdConfig ScriptableObject を実装
    - Android / iOS 用 App Key と Rewarded Ad Unit ID、デフォルト Placement 名、最大再試行回数、初期/最大再試行間隔を SerializeField で保持
    - Resources/RewardedAdConfig.asset として配置できる形式
    - `GetAppKey()` / `GetRewardedAdUnitId()` をプリプロセッサで対象プラットフォームに切替
  - [x] 2.3 RewardedAdConfig.asset を Resources に作成し、LevelPlay ダッシュボード取得済みの実値を Inspector に入力
    - 実値は機密扱いでも CSV と同じくクライアント同梱で構わない (research.md の Security 判断に従う)
  - _Requirements: 1.5, 7.2, 7.3, 7.5, 8.1, 8.2, 8.3_

- [x] 3. Editor 用スタブ実装と DI 切替の準備
  - [x] 3.1 (P) EditorRewardedAdService を実装
    - `InitializeAsync` は即時完了、`State` は常に Ready
    - `ShowAsync` は短いウェイト (100ms 程度) 後に `RewardedAdResult.Rewarded` を返却
    - クラスコンテキスト付きログでスタブ動作を出力
  - [x] 3.2 RootScope に IRewardedAdService の Editor 登録を追加
    - `#if UNITY_EDITOR` プリプロセッサで EditorRewardedAdService を Singleton 登録
    - RewardedAdConfig は Resources からロードして Component 登録
  - _Requirements: 7.1, 8.1_

- [x] 4. LevelPlay SDK 実機実装を構築
  - [x] 4.1 LevelPlayRewardedAdService のスケルトンと初期化を実装
    - VContainer 注入用コンストラクタに `[Inject]` を付与、`RewardedAdConfig` と `IClock` を依存に取る
    - LevelPlay の OnInitSuccess / OnInitFailed を Init 前に購読
    - `InitializeAsync` 内で `LevelPlay.Init(appKey)` を呼び、成功/失敗で State 遷移
    - 初期化失敗時は `State = Failed` に遷移、以降の ShowAsync は即時 NotReady を返却
    - 全 LevelPlay 型参照は `#if UNITY_ANDROID || UNITY_IOS` 配下に閉じ、それ以外プラットフォームでは内部 NoOp フォールバック
  - [x] 4.2 リワード広告ユニットのロードと指数バックオフ再試行を実装
    - 初期化成功直後に `LevelPlayRewardedAd` を生成し、OnAdLoaded / OnAdLoadFailed / OnAdDisplayed / OnAdDisplayFailed / OnAdRewarded / OnAdClosed を購読してからロード開始
    - 成功で `State = Ready`、失敗で指数バックオフ (1s → 2s → 4s → 8s → 16s → 32s、上限 60s) を最大 5 回再試行
    - 上限到達で `State = Failed`、StateChanged イベントで通知
  - [x] 4.3 視聴フローと順序非依存ステートマシンを実装
    - `ShowAsync` 開始時に内部 RewardedAdSession を 1 個生成、UniTaskCompletionSource<RewardedAdResult> を保持
    - `_rewardedFired` / `_closedFired` の 2 フラグで OnAdRewarded と OnAdClosed の合流を判定
    - OnAdDisplayFailed 発火時は DisplayFailed を返却して終了
    - 結果確定後の二重通知は `_completed` フラグで防御
    - 視聴完了 (Rewarded / Dismissed / DisplayFailed) 後、次回視聴のための再ロードを発行
  - [x] 4.4 状態変化通知とエラーハンドリングを完成
    - `StateChanged` イベントの購読者例外を try/catch で抑止 (UserPointService.FireYarnBalanceChanged と同パターン)
    - LevelPlay コールバック内処理は全てメインスレッド前提で記述 (LevelPlay 仕様)
    - クラスコンテキスト付きログで全異常パスを `Debug.LogError` / `Debug.LogWarning` 出力
  - [x] 4.5 RootScope に LevelPlayRewardedAdService の実機登録を追加
    - `#elif UNITY_ANDROID || UNITY_IOS` で LevelPlayRewardedAdService を Singleton 登録
    - `#else` で EditorRewardedAdService にフォールバック (Standalone / WebGL 等)
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 5.1, 5.3, 5.4, 5.5, 7.6, 8.4, 8.5_

- [x] 5. iOS ATT 連携と起動エントリポイントを構築
  - [x] 5.1 com.unity.ads.ios-support パッケージ導入と Info.plist 設定
    - `Packages/manifest.json` に `com.unity.ads.ios-support` を追加
    - iOS 向け `NSUserTrackingUsageDescription` を Player Settings (もしくは Info.plist 拡張機構) で日本語文言設定
    - 既存 EDM4U 解決結果と競合しないことを確認
  - [x] 5.2 iOS ビルドで ATT 応答待ち後に SDK 初期化を開始するフローを LevelPlayRewardedAdService に組込み
    - `#if UNITY_IOS && !UNITY_EDITOR` で `ATTrackingStatusBinding.RequestAuthorizationTracking` を呼び、応答完了まで `LevelPlay.Init` を遅延
    - Denied / Restricted を含むあらゆる応答ステータスでも初期化は継続する (eCPM は下がるが広告自体は表示可能)
  - [x] 5.3 起動時 InitializeAsync 自動発火用 IStartable を新設して RootScope に登録
    - VContainer の `RegisterEntryPoint<RewardedAdServiceStarter>()` で起動フックに乗せる
    - InitializeAsync を `UniTaskVoid` で `Forget()` 発火し、UI スレッドをブロックしない
    - 例外捕捉 + クラスコンテキスト付きログ
  - _Requirements: 1.1, 7.4_

### Branch: `feature/rewarded-ad-shop-product-shop-integration`

- [x] 6. 日次キャップ永続化基盤と JST ヘルパを整備
  - [x] 6.1 (P) PlayerPrefsKey に RewardAdDailyCount を追加
    - 既存 enum 末尾に追加し、`PlayerPrefsKey.RewardAdDailyCount` を新規キーとして使用可能にする
  - [x] 6.2 (P) RewardAdDailyCountSnapshot シリアライズ型を新規定義
    - `Version` (const 1) / `JstDate` (string YYYY-MM-DD) / `Entries` (productId と count の配列)
    - JsonUtility 対応のため `[Serializable]` 付与、ネスト型 `DailyCountEntry` を含む
  - [x] 6.3 (P) RewardAdShopConstants 定数クラスを新設
    - `DefaultDailyCap` = 5 (要件 9-4 の既定値)
    - 必要なら最大プレイスメント名定数も含める
  - [x] 6.4 (P) JST 日付変換ヘルパを新設
    - `IClock.UtcNow` から JST (`UTC+9`) の `DateOnly` を取得するユーティリティを Shop 内 or Root.Service 内に配置
    - 単体テストしやすい純粋関数として実装（Unity に DateOnly が無いため yyyy-MM-dd 文字列で実装）
  - _Requirements: 9.4, 10.1, 10.5_

- [x] 7. ShopService に日次キャップ管理を統合
  - [x] 7.1 ShopService に日次カウントのインメモリ保持を追加
    - productId → カウントの Dictionary、最終リセット日付 (JST 文字列) を保持
    - IRewardedAdService / PlayerPrefsService を `[Inject]` 経由で依存追加
  - [x] 7.2 起動時に永続化スナップショットをロードし JST 日付不一致なら全リセット
    - PlayerPrefsService 経由で `RewardAdDailyCountSnapshot` をロード
    - Version 不一致または保存日付 != 現在 JST 日付なら全カウント 0 にしてから永続化
    - マスター上に存在しない productId のエントリは破棄
  - [x] 7.3 GetDailyRemainingCount と IsRewardAdAvailable API を実装
    - 残り = (DailyCap ?? RewardAdShopConstants.DefaultDailyCap) - 当日カウント
    - IsRewardAdAvailable は「IRewardedAdService.IsReady かつ 残り ≥ 1」で判定
  - [x] 7.4 IsSoldOut と IsAffordable に RewardAd 商品の判定を追加
    - IsSoldOut: 既存 Outfit 既所持判定に加え、`CurrencyType.RewardAd` かつ 残り 0 で売り切れ扱い
    - IsAffordable の RewardAd 分岐を `IsRewardAdAvailable(productId)` 結果に置換
  - [x] 7.5 報酬付与成功時にカウント増分と即時永続化を実施
    - カウント増分後、JST 日付境界判定 (起動後に翌日に達した場合) も同時に評価し、必要ならリセットしてから増分
    - 永続化失敗時はエラーログのみ、UI 表示は阻害しない
  - [x] 7.6 マスター再インポート時の整合性を確保
    - `MasterDataImportService.Imported` イベントもしくは Initialize 経由で再構築時、既存カウントから消滅 productId を排除（Import は冪等のため Initialize 時の Reconcile で消滅 productId を破棄）
  - _Requirements: 3.3, 3.4, 3.5, 9.4, 9.5, 10.1, 10.2, 10.3, 10.4, 10.6, 10.7_

- [x] 8. ShopService に視聴フローと付与処理を実装
  - [x] 8.1 RewardAdProductList 構築処理を Initialize に統合
    - `MasterDataState.ShopProducts` から `CurrencyType.RewardAd` を抽出し ProductData に変換
    - `ShopProduct.Id` 昇順で並び替えて `ShopState.RewardAdProductList` に格納
    - 通常 (Yarn) 商品の購入フロー・時限ショップ抽選には引き続き混入させない (既存 `SplitShopProductsForTimedShop` の挙動を維持)
  - [x] 8.2 OnProductCellTappedAsync の RewardAd 分岐を実装
    - 既存スタブ (現状 `return;`) を新規メソッド呼び出しに置換
    - 視聴前確認ダイアログ (CommonConfirmDialog) を IDialogService 経由で表示
    - 承認時に IRewardedAdService.ShowAsync を呼ぶ
    - 視聴セッション内のキャンセル/失敗/中断/成功を結果ごとに分岐
  - [x] 8.3 TryGrantPurchasedItem に ItemType.Point 分岐を追加
    - `_userPointService.AddYarn(data.Amount)` を呼び、エラー時はクラスコンテキスト付きログ
    - 戻り値の IsSuccess を呼び出し側に伝搬し、結果メッセージの文言切替に使用
  - [x] 8.4 視聴セッション間の多重タップ防止を実装
    - ShopView の `_isProcessing` と並行して、ShopService 内でも進行中の productId を保持し、別セルの新規タップを弾く
  - [x] 8.5 失敗時メッセージダイアログを実装
    - DisplayFailed: 「広告を再生できませんでした」 (CommonMessageDialog)
    - Dismissed: 「広告の視聴が中断されました」 (誘導文を含めない、要件 6-3)
    - 付与失敗: 既存 Yarn 商品と同じく結果メッセージ内に「（アイテムの付与に失敗しました）」を併記
  - _Requirements: 3.1, 3.6, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 5.2, 5.6, 6.2, 6.3, 6.5, 9.3_

- [x] 9. ShopView と ProductCellView を残数表示・状態連動に拡張
  - [x] 9.1 ProductCellView に残数テキスト表示 API を追加
    - `_remainingCountText` を SerializeField で追加、未割当時は警告ログのみ
    - `SetRemainingCount(int remaining, int dailyCap)` を新規 API として追加、テキストは `{n}/{m}` 形式
    - `Setup` で既定状態は非表示 (SetActive(false))、RewardAd セルでのみアクティブ化
  - [x] 9.2 ShopView.SetupCategoryCells と RefreshCategoryAppearance に残数更新を統合
    - RewardAd セルのみ `SetRemainingCount` を呼び、Yarn セルでは呼ばない
    - 既存 SetSoldOut / SetDimmed / SetInteractable の呼び順は維持し、売り切れ表示が残数 0 と連動することを確認
  - [x] 9.3 IRewardedAdService.StateChanged を ShopView が購読しセル状態を再評価
    - Ready 遷移時/Failed 遷移時に RefreshAllCellsAppearance を呼ぶ
    - 起動時の長時間未初期化を検知して RewardAd 枠を「準備中」状態として表示する経路を確保 (要件 6-4)（未準備時は IsRewardAdAvailable=false により非操作・Dim 表示で「準備中」を表現）
  - [x] 9.4 視聴完了時と JST 境界またぎ時の残数即時更新を結線
    - ShopService 内のカウント増分後、ShopView に通知し残数テキストを再計算 (`ShopState` の新規イベントを追加する、もしくは ShopView 側で定期/イベント駆動で取得)
    - JST 日付境界またぎを検知した瞬間 (起動時/視聴成立時の遅延判定でも可) に全 RewardAd セルの残数を `m/m` に戻す（Tick で JST 境界を検知し OnRewardAdCountsChanged を発火）
  - _Requirements: 3.2, 3.4, 6.1, 6.4, 11.1, 11.2, 11.3, 11.4, 11.5_

- [ ] 10. shop_products.csv に実リワード広告商品を登録し UI 枠を準備
  - [x] 10.1 リワード広告商品の新規行を CSV に追加
    - 例: 毛糸ポイント 1 件 (`item_type=point`, `amount=100`, `daily_cap=5`)、家具 1 件、着せ替え 1 件
    - 各行で `currency_type=reward_ad`、`price=0` を統一
  - [ ] 10.2 ShopView の `_rewardAdCells` に必要数の ProductCellView を Inspector で割り当て（**手動: Editor 作業が必要**）
    - リワード広告商品の最大想定数に合わせて Prefab を配置
    - 残数テキスト要素 (TMP_Text) を Prefab に追加して `_remainingCountText` に結線
  - [ ] 10.3 実機 / Editor で表示順 (ProductId 昇順) とアイコン解決を目視確認（**手動: Editor/実機作業が必要**）
    - 既存 IconPath 解決 (`Furnitures/{name}` / `Outfits/{name}`) が機能することを確認
    - Point 商品のアイコン解決は新規アイコン (毛糸アイコン) を Addressables に登録する場合は別途用意 (本タスクで合わせて準備)
  - _Requirements: 9.1, 9.2, 9.3_

- [ ] 11. テストと実機検証
  - [ ] 11.1 (P) ShopService の日次カウント関連ユニットテストを追加
    - GetDailyRemainingCount: カップ未指定 (既定値適用) / カップ指定 / カウント 0 / カウント上限の各ケース
    - IsSoldOut: Outfit 既所持 / RewardAd 残数 0 / 通常状態の判定
    - マスター再インポート時の存在しない productId 排除
  - [ ] 11.2 (P) JST 境界判定ヘルパのユニットテスト
    - UTC 14:59 → JST 23:59 同日、UTC 15:00 → JST 翌日 0:00
    - 月末/年末越え (UTC 末日 14:59 / 15:00)
  - [ ] 11.3 (P) MasterDataImportService の 8 カラム拡張テスト
    - 8 カラム正常 / 6 カラム不足でスキップ / 空 amount → 1 / 空 daily_cap → null / 数値不正でスキップの各ケース
    - 既存 Yarn 商品行が新カラム空でも正常ロードされること
  - [ ] 11.4 Editor 上で全フローの統合テスト (手動)
    - スタブ広告で視聴 → 報酬付与 → 残数減 → 永続化反映 → アプリ再起動で永続化復元 → JST 境界またぎでリセット
    - DailyCap 到達後の売り切れ表示確認
    - 確認ダイアログでキャンセル時の挙動 (付与なし、カウント不変)
  - [ ] 11.5* Android / iOS 実機検証
    - Android 実機: 起動 → ロード → ショップ → 視聴 → 付与
    - iOS 実機: ATT ダイアログ → 各応答 (Authorized / Denied / Restricted) で広告ロード継続
    - DisplayFailed / Dismissed のケースを実機で再現 (機内モード等)
    - _Requirements: 5.4, 7.4_ (受入基準を実機で最終確認するための任意の追加検証)
  - _Requirements: 5.1, 5.3, 5.5, 5.6, 9.5, 10.1, 10.2, 10.3, 10.4, 10.6_
