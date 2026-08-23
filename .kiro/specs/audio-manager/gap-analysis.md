# Gap Analysis: audio-manager

## 1. 現状調査 (Current State)

### 既存のオーディオ関連資産
- **コード**: `Assets/Scripts` 全域に AudioSource / AudioClip / Audio 系のコードは**一切存在しない**。完全な新規実装となる
- **アセット**: `.mp3 / .ogg / .wav` ファイルはプロジェクトに**未配置**。SE/BGM アセットの調達・インポート設定も未着手
- **`ProjectSettings` の AudioManager 設定**: 既定のまま (調整履歴なし)

### 再利用可能な既存基盤

| 基盤 | 場所 | audio-manager との関係 |
| --- | --- | --- |
| RootScope (DI) | `Assets/Scripts/Root/Scope/RootScope.cs` | 全シーン共通サービスの登録先。`.As<IXxx>().AsSelf()` 登録パターンあり |
| RootLifetimeScope プレハブ | `VContainerSettings.asset` が参照 | VContainer により自動生成 + **DontDestroyOnLoad**。AudioSource ホストの候補 |
| PlayerPrefsService | `Root/Service/PlayerPrefsService.cs` | `PlayerPrefsKey` enum + JSON 保存。音量設定の永続化に流用可能 |
| SceneLoader / Fade | `Root/Service/SceneLoader.cs` | Fade シーンを Additive ロード後にターゲットへ遷移。BGM 継続には DontDestroyOnLoad ホストが必須 |
| SceneScope 基底 | 各シーン Scope が継承 | ボタン自動フックの EntryPoint 登録を基底に集約できる可能性 |
| Addressables ロード | `DialogContainer`, `OutfitAssetService`, `FurnitureAssetStarter` | AudioClip の動的ロードに転用可能なパターンが確立済み |
| Config-as-Asset | `RewardedAdConfig` (`Resources.Load` + fail-fast) | SE/BGM の識別子→クリップ対応表 (ScriptableObject) の先例 |
| DOTween + UniTask | `Assets/Plugins/Demigiant/` | BGM フェード (`DOTween.To` で volume 補間) に利用可能 |
| IClock / EditMode テスト | `Shop/RewardAdLogic/` パターン | 純粋ロジック (音量クランプ等) のテスト先例 |

### 規約上の制約
- 依存方向 `View → Service → State` 厳守。AudioService (Root/Service) を View/Service 層から呼ぶ構造は規約に整合
- UniTask 非同期メソッドは末尾 `CancellationToken`、`[Inject]` 付与、`_camelCase`、`/// comment` 形式
- `PlayerPrefsService.Load<T>` は**キー欠落時に null/例外の考慮がない** (既存サービスは各自 try-catch や初期化で対処) — Requirement 4.3/4.4 のフォールバックは呼び出し側で実装する必要あり (Constraint)

### UI ボタンの分布 (Requirement 7 関連)
- `Button` 参照は **26 ファイル / 51 箇所**。シーン静的配置とプレハブ動的生成が混在
- 動的生成系: EnhancedScroller セル (`ProductCellView`, `GachaCellView`, `RewardAdProductCellView`, `DayCellView`, Closet セル/タブ)、**Addressables 経由で実行時生成されるダイアログ** (`DialogContainer` → `BaseDialogView` 派生)
- ダイアログのボタンはシーン開始時走査では**捕捉不能** — プレハブへの上書きコンポーネント付与、または `DialogContainer` の生成フックで対応が必要 (Gap)

## 2. 要件実現性分析 (Requirement-to-Asset Map)

| 要件 | 必要な技術要素 | 既存資産 | ギャップ |
| --- | --- | --- | --- |
| 1. BGM再生管理 | 永続 AudioSource、フェード制御 | RootLifetimeScope (DDoL)、DOTween | **Missing**: AudioSource ホスト View、フェードロジック |
| 2. SE再生管理 | 多重再生 (PlayOneShot or Source プール)、上限制御 | なし | **Missing**: 全て新規。上限方式は Unknown (Research Needed) |
| 3. 音量制御 | カテゴリ別音量状態、クランプ | State パターン先例多数 | **Missing**: AudioState。実装は容易 |
| 4. 音量永続化 | PlayerPrefs 保存/復元 | PlayerPrefsService + enum キー | **Constraint**: キー欠落時フォールバックは呼び出し側実装 |
| 5. シーン遷移連携 | シーンをまたぐ再生継続 | RootScope Singleton + DDoL プレハブ | **Missing**: ホストへの AudioSource 配置のみ |
| 6. アセット管理 | 識別子→クリップ解決、欠落時 fail-safe | Addressables / Resources 両パターン先例 | **Unknown**: ロード方式の選定 (Research Needed) |
| 7. ボタンSE自動適用 | シーン走査 + 上書きコンポーネント + 動的生成対応 | SceneScope 基底、各セルプレハブ | **Missing**: 全て新規。ダイアログ生成フックが追加課題 |

### 複雑性シグナル
- 大半は確立済みパターンの適用 (DI 登録、State/Service 分離、PlayerPrefs 永続化)
- 新規性が高いのは (a) DontDestroyOnLoad な MonoBehaviour View の DI 登録方法、(b) ボタン自動フックの走査タイミング設計、(c) SE 同時再生の上限方式

## 3. 実装アプローチ選択肢

### Option A: 既存コンポーネント拡張のみ
該当せず。オーディオ機能の受け皿となる既存コンポーネントが存在しないため、純粋な拡張アプローチは**成立しない**。

### Option B: 新規コンポーネント中心 (推奨候補)
`Root/` 配下に標準層構造で新規作成し、既存ファイルへの変更は登録・キー追加に限定する。

- **新規**: `Root/Service/AudioService (IAudioService)`、`Root/State/AudioState`、`Root/View/AudioPlayerView` (AudioSource ホスト)、SE/BGM 識別子定義、音量設定データ、`Root/Service/ButtonSeService` (自動フック) + 上書きコンポーネント (`ButtonSeOverride`)
- **既存変更 (最小)**: `RootScope.Configure` への登録追加、`PlayerPrefsKey` へ `AudioSetting` 追加、SceneScope または各 Scope への自動フック EntryPoint 登録、RootLifetimeScope プレハブへの AudioSource 配置 (Editor 作業)
- ✅ 責務分離が明快、既存機能への影響ほぼゼロ、プロジェクトの層構造と完全整合
- ❌ ファイル数は増える (ただしプロジェクト規約上は正道)

### Option C: ハイブリッド (段階導入)
Option B の構成を **Phase 分割**で導入する。

- **Phase 1**: BGM/SE コア (Req 1,2,3,4,5,6) — AudioService + ホスト + 永続化
- **Phase 2**: ボタン SE 自動適用 (Req 7) — 自動フック + 上書きコンポーネント + 動的生成対応
- ✅ Phase 1 単独でも価値が出る。Req 7 の設計リスク (ダイアログ/動的セル対応) を切り離せる
- ✅ ユーザー承認済みの「ハイブリッド方針」(自動フック + 例外上書き + 文脈依存は明示呼び出し) と整合
- ❌ 計画がやや複雑化

## 4. 工数・リスク評価

| 項目 | 評価 | 根拠 |
| --- | --- | --- |
| 工数 | **M (3–7日)** | 確立済みパターンの適用が主体だが、新規領域 (DDoL ホスト、自動フック、フェード) が3点あり S には収まらない |
| リスク | **Low–Medium** | アーキテクチャ変更なし・既知技術のみ (Low 要素)。SE 上限方式とダイアログボタン捕捉が設計未確定 (Medium 要素) |

## 5. 設計フェーズへの推奨事項

### 推奨アプローチ
**Option C (Option B の構成 + Phase 分割)**。層構造・DI 登録・永続化はすべて既存規約をなぞり、Req 7 を独立フェーズに分離する。

### Research Needed (設計フェーズで解決すべき項目)
1. **AudioClip のロード方式**: Addressables (動的・メモリ効率) vs Resources + ScriptableObject 対応表 (単純・fail-fast 先例あり)。クリップ数と使用頻度から選定
2. **DontDestroyOnLoad ホストの DI 登録方法**: RootLifetimeScope プレハブへ事前配置 + `RegisterComponent` vs `RegisterComponentOnNewGameObject` での実行時生成
3. **SE 同時再生の上限方式**: 単一 AudioSource + `PlayOneShot` (上限は実質なし) vs AudioSource プール (上限明示・個別停止可能)
4. **BGM クロスフェードの方式**: AudioSource 2本のスワップ (クロスフェード) vs 1本でフェードアウト→イン (シーケンシャル)。Requirement 1.2 の表現と要調整
5. **ダイアログ/動的生成ボタンの SE 適用経路**: プレハブへの `ButtonSeOverride` 事前付与 vs `DialogContainer`/生成側でのフック
6. **AudioMixer の要否**: 現要件はカテゴリ別 volume 直接制御で足りるが、将来のダッキング等を見込むなら AudioMixer 導入も選択肢

### 設計時の注意
- `PlayerPrefsService.Load<T>` のキー欠落時挙動 (null/例外) に対するフォールバックを AudioService 側で必ず実装 (Req 4.3/4.4)
- アプリのバックグラウンド移行時の挙動 (`Application.focusChanged` 等) は現要件外 — 必要なら要件追加を検討
