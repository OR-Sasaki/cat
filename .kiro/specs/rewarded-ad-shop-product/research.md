# Research & Design Decisions: rewarded-ad-shop-product

---

## Summary

- **Feature**: `rewarded-ad-shop-product`
- **Discovery Scope**: Extension + Complex Integration (既存ショップへのリワード広告 SDK 統合)
- **Key Findings**:
  - 既存ショップに `CurrencyType.RewardAd` / `RewardAdProductList` / `_rewardAdCells` / `OnProductCellTappedAsync` の RewardAd 分岐スタブ が用意済み → 拡張 (Option C ハイブリッド) が最適
  - LevelPlay SDK 9.4.1 は `com.unity.services.levelplay` UPM パッケージとして導入済み、EDM4U は Google 公式 UPM 版で稼働中
  - 既存 `IClock` は UTC のみ提供 → JST 固定 (UTC+9) 境界での日次リセット判定を追加する必要あり
  - LevelPlay の全コールバックは Unity メインスレッドで発火するため、UniTask ベースの API が安全に組める
  - `OnAdRewarded` と `OnAdClosed` の発火順序は保証されないため、2 フラグ合流方式のステートマシンを設計する必要あり

## Research Log

### LevelPlay SDK 9.4.1 API 仕様の確認
- **Context**: SDK 抽象を設計するにあたり、初期化・ロード・表示・コールバックの API 表面を確定する必要があった
- **Sources Consulted**:
  - https://docs.unity.com/en-us/grow/levelplay/sdk/unity/package-integration
  - https://docs.unity.com/en-us/grow/levelplay/sdk/unity/rewarded-ad-integration-package
  - https://docs.unity.com/en-us/grow/levelplay/sdk/unity/migrate-from-unity-ads-to-levelplay
- **Findings**:
  - 初期化は `LevelPlay.Init(appKey)` 同期呼び出し + `LevelPlay.OnInitSuccess` / `OnInitFailed` イベント (Init 前に購読必須)
  - 各広告ユニットは `LevelPlayRewardedAd(adUnitId)` インスタンス + イベント購読 (`OnAdLoaded` / `OnAdLoadFailed` / `OnAdDisplayed` / `OnAdDisplayFailed` / `OnAdRewarded` / `OnAdClosed` / `OnAdClicked` / `OnAdInfoChanged`)
  - 表示は `IsAdReady()` && `!IsPlacementCapped(name)` を確認後 `ShowAd(placementName)`
  - **すべてのコールバックは Unity メインスレッドで発火**
  - `OnAdRewarded` と `OnAdClosed` の順序は不定、両方発火する設計
  - Test Mode フラグは存在しない (`SetMetaData("is_test_suite", "enable")` + `LaunchTestSuite()` でテストツール起動)
- **Implications**:
  - スレッドセーフ機構は不要、UniTaskCompletionSource をそのまま使える
  - 結果確定は「OnAdRewarded が来たか」「OnAdClosed が来たか」の 2 フラグ合流で判定するステートマシンが必要
  - Editor では SDK 呼び出しを完全に避け、スタブ実装で即時 `Rewarded` を返す方が安全

### iOS ATT (App Tracking Transparency) の取扱い方式
- **Context**: 要件 7-4 に「iOS では ATT 応答確定まで広告ロードを開始しない」とあり、Unity 上での ATT ダイアログ表示方式の確定が必要
- **Sources Consulted**: Unity LevelPlay 公式ドキュメント (package-integration), Apple HIG (ATT)
- **Findings**:
  - LevelPlay SDK 9.4.1 自体は ATT ダイアログを自動表示しない (アプリ側が能動的に呼び出す必要)
  - Unity 公式 `com.unity.ads.ios-support` パッケージ、もしくは自前で `Unity.Advertisements.IosSupport.ATTrackingStatusBinding` 経由で `RequestAuthorizationTracking` を呼ぶ方式が一般的
  - ATT 応答ステータスは `Unauthorized` / `Restricted` / `Denied` / `Authorized` / `NotDetermined` の 5 値
  - **Denied/Restricted でも広告自体は表示可能**だが、IDFA が取得できないため eCPM が下がる
- **Implications**:
  - ATT は **iOS ビルド時のみ** 呼び出し、結果に関係なく LevelPlay 初期化 → ロードへ進む
  - 既存 RootScope の起動直後に呼び出し、`UniTask<bool>` で完了を待ち合わせる関数を別途追加する設計が自然
  - 本機能の MVP では Unity 公式パッケージを採用 (自前 Native Plugin は保守コストが高い)

### 日次境界判定のタイムゾーン選択
- **Context**: Req 10-4 「日付境界 (ローカル時刻 0:00)」をどう実装するかの判断が必要
- **Sources Consulted**: 既存コード (`IClock.cs`, `TimedShopCycleCalculator.cs`), 既存仕様 (.kiro/steering/tech.md の Time Abstraction)
- **Findings**:
  - 既存 `IClock` は `DateTimeOffset.UtcNow` のみ提供
  - `TimedShopCycleCalculator.Calculate(IClock.UtcNow, ...)` は UTC ベースのサイクル算出で動作
  - ターゲットユーザー (steering.product.md: 日本語 UI、毛糸など日本的モチーフ) は日本中心
- **Implications**:
  - JST (UTC+9) 固定で実装するのが、海外渡航時/サマータイム変動時の予測不能性を回避できる
  - 実装は `clock.UtcNow.ToOffset(TimeSpan.FromHours(9)).Date` で当日日付を導出
  - 海外展開フェーズが来た場合は将来的に `IClock` 拡張 + マスター/ユーザー設定で時刻調整できる余地を残す

### Reward/Closed イベント順序非依存ステートマシンの設計
- **Context**: 公式ドキュメント上 `OnAdRewarded` と `OnAdClosed` の発火順序は保証されないと明記。1 セッションで 1 件の結果のみ通知するための同期機構が必要
- **Sources Consulted**: 公式ドキュメント, ironSource Mediation Demo Apps (Reward + Closed ハンドリング例)
- **Findings**:
  - 一般的なパターン: `bool _rewardedFired` / `bool _closedFired` の 2 フラグを 1 セッションごとに持ち、両方発火または `OnAdDisplayFailed` 発火で結果確定
  - `OnAdRewarded` のみ来て `OnAdClosed` が来ないケース (まれ) に備え、 fallback タイマー (例: 30 秒) を持つ実装もあり
- **Implications**:
  - `RewardedAdSession` 内部クラス: `UniTaskCompletionSource<RewardedAdResult> _tcs`, `bool _rewardedFired`, `bool _closedFired`, `bool _completed`
  - 「結果確定の判定ロジック」: `_rewardedFired && _closedFired` → `Rewarded`、`!_rewardedFired && _closedFired` → `Dismissed`、`OnAdDisplayFailed` 発火 → `DisplayFailed`
  - 二重 TrySetResult は無視 (`_completed` フラグで防御)

### マスター CSV スキーマ拡張方針
- **Context**: 要件 9 で「付与数」「日次視聴可能回数」を追加。後方互換性を維持しつつ既存パーサ (`MasterDataImportService.ImportShopProducts`) を拡張する方式選択
- **Sources Consulted**: 既存 `Assets/Resources/shop_products.csv` (46 行), `MasterDataImportService.cs`
- **Findings**:
  - 既存 6 カラム: `id,name,item_type,item_id,price,currency_type`
  - 拡張案: 末尾に `amount,daily_cap` の 2 カラムを追加 → 計 8 カラム
  - 既存 46 行は両カラム空文字でも互換 (Yarn 商品では未使用扱い)
  - パーサの `columns.Length < 6` を `< 8` に変更、空文字許容ロジックを追加
- **Implications**:
  - `ShopProduct` レコードに `int Amount` (default 1), `int? DailyCap` (null 許容) を追加
  - `ProductData` レコードにも `int Amount` を新規追加 (UI/付与処理で使用)
  - 既存 CSV の全 46 行に空 2 カラムを補充 (`,,` 末尾)
  - リワード広告行: `47,YarnSmallPack,point,0,0,reward_ad,100,5` のような形式 (`item_id=0` はサテライト、ItemId 未使用)

### Editor / Player 切替パターン
- **Context**: Req 7-1 「Editor では LevelPlay を呼ばずスタブで即時 Rewarded を返す」の実装方式
- **Sources Consulted**: 既存 `RootScope.cs`, `SystemClock` の登録パターン
- **Findings**:
  - VContainer 登録時に `#if UNITY_EDITOR` 分岐で `EditorRewardedAdService` vs `LevelPlayRewardedAdService` を切替可能
  - C# プリプロセッサ分岐はビルドプラットフォーム単位、`UNITY_ANDROID` / `UNITY_IOS` も使える
- **Implications**:
  - `RootScope.Configure` で `#if UNITY_EDITOR` を使用してスタブを登録、それ以外は実機実装
  - 実機実装側でも `#if UNITY_ANDROID || UNITY_IOS` で LevelPlay 参照、それ以外プラットフォーム (Standalone) はスタブ実装にフォールバック
  - これにより Windows ビルドや WebGL 等で広告フローを呼んでもクラッシュしない

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| A: ShopService 全部入り | 既存 ShopService に LevelPlay 呼び出しと日次キャップを全部詰める | 新規ファイル最小 | ShopService 肥大化、テスト困難、再利用不可 | 却下 |
| B: Root + Shop に新規 Service 2 つ | LevelPlay 抽象を Root に、ショップ寄りロジックを Shop 内 `RewardAdShopService` に分離 | 完全分離、テスト容易 | ファイル数増、ShopService との責務調整必要 | 候補 |
| C (採用): Hybrid | LevelPlay 抽象は Root に新設、日次キャップ/視聴フローは ShopService 拡張 | レイヤ違いを保ちつつファイル数最小、既存 IsSoldOut/IsAffordable/TryGrantPurchasedItem を最大限再利用 | ShopService の責務微増 (許容範囲) | 採用 |

## Design Decisions

### Decision: 広告 SDK 抽象の配置 — `Root.Service.IRewardedAdService`
- **Context**: LevelPlay 呼び出しを直接 Shop 層から行うか、抽象経由にするか
- **Alternatives Considered**:
  1. Shop.Service 内に直接 LevelPlay 呼び出し — 実装は最短だがレイヤ違反
  2. Root.Service に抽象インターフェース + 実装複数 — 既存 `IUserPointService` 等と同じパターン
- **Selected Approach**: Option 2。`Root.Service.IRewardedAdService` インターフェースを定義し、`LevelPlayRewardedAdService` (Android/iOS) と `EditorRewardedAdService` (それ以外) を VContainer 登録時にプリプロセッサで切替
- **Rationale**: 既存 RootScope の Singleton 登録パターンに沿う。Editor 用スタブを差し替え可能にする最も単純な方法。将来ホーム画面など他シーンから広告を呼ぶ場合も再利用可能
- **Trade-offs**: ✅ 関心の分離、テスト容易、再利用性 / ❌ ファイル数増 (4 ファイル増)
- **Follow-up**: `RootScope.Configure` でのプリプロセッサ分岐の動作確認、IL2CPP ストリッピング対策の `[Inject]` 付与

### Decision: 日次キャップ管理の責務帰属 — `Shop.Service.ShopService` 拡張
- **Context**: 日次視聴回数の保持・判定・リセットをどこに置くか
- **Alternatives Considered**:
  1. 新規 `Shop.Service.RewardAdShopService` を作る
  2. 既存 `Shop.Service.ShopService` を拡張
- **Selected Approach**: Option 2。既存 `ShopService` に日次カウント `Dictionary<uint, int>` と `DateOnly _lastResetDate` を保持、`IsSoldOut` 判定と `GetDailyRemainingCount(productId)` API を追加
- **Rationale**: 「ショップ商品の状態判定」は既に ShopService の責務 (`IsSoldOut`, `IsAffordable`)。日次キャップは商品状態の一種なので自然な拡張。RewardAdShopService を別途作ると `ShopState.RewardAdProductList` の所有が分散する
- **Trade-offs**: ✅ ファイル数最小、責務凝集 / ❌ ShopService が約 600 行に成長
- **Follow-up**: 約 600 行を超える場合は責務分割を検討

### Decision: 日次境界タイムゾーン — JST 固定 (UTC+9)
- **Context**: 「ローカル時刻 0:00」をどの基準で判定するか
- **Alternatives Considered**:
  1. JST 固定 (UTC+9)
  2. 端末ロケール `TimeZoneInfo.Local`
- **Selected Approach**: JST 固定
- **Rationale**: ターゲットユーザーが日本中心、海外渡航時/サマータイム実施国/夏冬時刻変動で日付境界がブレるとマスターデータ運用が困難。JST 固定なら全ユーザーが同時刻にリセットされ予測可能
- **Trade-offs**: ✅ 予測可能、デバッグ容易 / ❌ 海外展開時に再設計が必要
- **Follow-up**: 海外展開時に `IClock` 拡張 or マスター/ユーザー設定で時刻調整できる余地を残しておく (現状はハードコード可)

### Decision: マスター CSV 拡張方式 — 末尾追加 2 カラム
- **Context**: `amount` と `daily_cap` を CSV にどう追加するか
- **Alternatives Considered**:
  1. 末尾追加 (列順: `..., currency_type, amount, daily_cap`)
  2. 関連性のあるカラム近傍 (`item_id, amount, ...`)
- **Selected Approach**: 末尾追加
- **Rationale**: 既存 46 行へのインパクトを最小化、`columns.Length < 6` → `< 8` の変更でパース失敗扱いを維持できる
- **Trade-offs**: ✅ 後方互換性、変更最小 / ❌ カラム順が意味的グルーピングと一致しない
- **Follow-up**: 拡張後の CSV ファイル全 46 行を空 2 カラム追加で更新

### Decision: ATT 連携方式 — Unity 公式 ios-support パッケージ
- **Context**: iOS ATT ダイアログ表示の実装方式
- **Alternatives Considered**:
  1. Unity 公式 `com.unity.ads.ios-support` パッケージ採用
  2. 自前 Native Plugin (Objective-C / Swift)
  3. LevelPlay SDK 任せ (= 自動ダイアログなし → 表示しない)
- **Selected Approach**: Option 1
- **Rationale**: 保守コスト最小、Apple のポリシー変更追従はパッケージ側で吸収される。LevelPlay は ATT 自動表示しないため明示的に呼ぶ必要あり
- **Trade-offs**: ✅ 保守コスト最小 / ❌ パッケージ 1 つ追加
- **Follow-up**: `Packages/manifest.json` に `com.unity.ads.ios-support` を追加 (iOS ビルド条件付き利用)

### Decision: `n/m` 残数表示の UI 実装 — `ProductCellView` 共通拡張
- **Context**: 残数テキスト用 UI をどこに持たせるか
- **Alternatives Considered**:
  1. `ProductCellView` に `_remainingCountText` SerializeField を追加、Yarn 商品では非表示
  2. `RewardAdProductCellView` 派生クラスを新設
- **Selected Approach**: Option 1
- **Rationale**: 既存セル Prefab を流用、SerializeField 未割当時は警告のみで動作継続 (既存 `_dimOverlay` パターンと同じ)。派生クラスを増やすと `_rewardAdCells: List<RewardAdProductCellView>` への切替が必要となり影響範囲が大きい
- **Trade-offs**: ✅ ファイル数最小、Prefab 設計容易 / ❌ Yarn セルにも空フィールドが残る
- **Follow-up**: Yarn 商品では `SetRemainingCount` を呼ばないことで非表示維持

## Risks & Mitigations

- **LevelPlay 9.4.x → 10.x のメジャーアップグレード** — 抽象インターフェース (`IRewardedAdService`) を介すことで影響範囲を SDK アダプタクラスに局所化
- **iOS App Store 提出時の Privacy Manifest 要件未対応** — LevelPlay SDK 9.1.0 以降は SKAdNetwork ID 自動管理。提出前に PrivacyInfo.xcprivacy の必要記述事項を再確認するチェックリストを実装ガイドに記載
- **日次カウント永続化の不整合** — マスター再インポートで `productId` が変わると過去カウントが宙ぶらりんに。カウント取得時に「マスターに存在しない productId のエントリは無視」する防御を入れる
- **`OnAdRewarded` のみ来て `OnAdClosed` が来ないケース** — `ShowAsync` 開始から一定時間 (例: 5 分) で `RewardedAdResult.DisplayFailed` を返すタイムアウト機構を追加 (MVP では省略可、設計上の余地として記載)
- **Editor スタブが過剰報酬を永続化する懸念** — Editor 実装でも日次キャップを尊重 → 1 日上限まで付与してリセット待ち、本番と同じ運用を維持。永続化自体は本番と同じ PlayerPrefs を使用

## References

- [LevelPlay Package Integration](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/package-integration) — UPM 導入、初期化、Test Suite
- [LevelPlay Rewarded Ad Integration](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/rewarded-ad-integration-package) — `LevelPlayRewardedAd` API、コールバック仕様、メインスレッド保証
- [Migrate from Unity Ads to LevelPlay](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/migrate-from-unity-ads-to-levelplay) — API マッピング、移行手順
- 既存実装参照: `Assets/Scripts/Shop/Service/ShopService.cs`, `Assets/Scripts/Shop/View/ShopView.cs`, `Assets/Scripts/Shop/State/ShopState.cs`, `Assets/Scripts/Shop/View/ProductCellView.cs`, `Assets/Scripts/Root/Service/MasterDataImportService.cs`, `Assets/Scripts/Root/Service/UserPointService.cs`, `Assets/Scripts/Root/Service/PlayerPrefsService.cs`, `Assets/Scripts/Root/Service/IClock.cs`, `Assets/Scripts/Root/Scope/RootScope.cs`
- 既存ステアリング: `.kiro/steering/tech.md` (Time Abstraction, DI Constructors), `.kiro/steering/structure.md` (Dependency Direction)
