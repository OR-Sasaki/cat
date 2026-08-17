# Research & Design Decisions: audio-manager

## Summary
- **Feature**: `audio-manager`
- **Discovery Scope**: Extension (既存 Unity プロジェクトへの新規サブシステム追加。Light Discovery を実施)
- **Key Findings**:
  - オーディオ関連の既存コード・アセットはゼロ。ただし DI・永続化・DDoL ホスト・フェードの基盤は全て流用可能 (gap-analysis.md 参照)
  - `DialogContainer.LoadAndInstantiateAsync` が全ダイアログ生成の単一ファンネルであり、`_resolver.InjectGameObject(instance)` 実施済み。ここがダイアログ内ボタンへの SE 自動適用の唯一のフックポイントになる
  - `SceneManager.sceneLoaded` はシーン内全 Awake/OnEnable 後・Start 前に発火するため、シーン静的ボタンの一括走査タイミングとして安全。SceneScope 各派生クラスへの変更が不要になる
  - EnhancedScroller セル等の動的生成ボタンは DI 経路外で Instantiate されるため、走査では捕捉不能。プレハブへの事前コンポーネント付与 + 静的ハンドル経由の解決が現実解 (`DialogSampleButtonView` に `FindAnyObjectByType<RootScope>` で Root コンテナへアクセスする先例あり)

## Research Log

### AudioClip のロード方式 (Research Item 1)
- **Context**: gap-analysis Research Needed #1。Addressables vs Resources + ScriptableObject 対応表
- **Sources Consulted**: `RewardedAdConfig` (Resources + fail-fast 先例)、`OutfitAssetService` / `DialogContainer` (Addressables 先例)、Unity AudioClip loadType ドキュメント
- **Findings**:
  - ボタンクリック SE は**即時再生が必須**。Addressables の非同期ロードを挟むと初回再生に遅延が出る
  - 本ゲームは常駐 BGM + UI SE 中心で、クリップ総数・サイズは小さい (現状アセット未調達 = 数を設計で制御可能)
  - `RewardedAdConfig` の「ScriptableObject を Resources.Load、欠落時 fail-fast」パターンが確立済み
- **Implications**: ScriptableObject レジストリ (`AudioRegistry`) を Resources から同期ロードし全クリップを事前保持する。将来クリップ数が肥大化した場合は `IAudioService` の背後で Addressables 化できる (契約は不変)

### DontDestroyOnLoad ホストの DI 登録方法 (Research Item 2)
- **Context**: gap-analysis Research Needed #2。AudioSource をホストする MonoBehaviour の生成・登録方法
- **Sources Consulted**: `VContainerSettings.asset` (RootLifetimeScope プレハブ参照)、`HomeScope` の `RegisterComponent` パターン、VContainer ドキュメント
- **Findings**:
  - VContainer は `VContainerSettings.RootLifetimeScope` のプレハブを自動生成し **DontDestroyOnLoad** にする。このプレハブ = `RootScope` の GameObject
  - `RegisterComponentOnNewGameObject` でも実行時生成は可能だが、AudioSource 本数 (BGM 2 + SE プール) や設定をインスペクタで調整できない
- **Implications**: RootLifetimeScope プレハブの子に `AudioPlayerView` を事前配置し、`RootScope` の SerializeField → `RegisterComponent` で登録する (HomeScope の `_closetUiView` と同型)。Editor 作業が 1 回だけ発生する

### SE 同時再生の上限方式 (Research Item 3)
- **Context**: gap-analysis Research Needed #3。Requirement 2.4 (上限の明示) の実現方式
- **Sources Consulted**: Unity `AudioSource.PlayOneShot` 仕様
- **Findings**:
  - 単一 AudioSource + `PlayOneShot` は重ね再生できるが、同時発音数の把握・制御・個別停止が不可能で Req 2.4 を満たせない
  - AudioSource プール (固定 N 本、ラウンドロビン) なら上限が構造的に保証され、あふれた場合は最古のソースを奪って再生 (oldest-steal) できる
- **Implications**: `AudioPlayerView` に SE 用 AudioSource を N 本 (既定 8、インスペクタ調整可) 保持。選定ロジック (次に使うソースの決定) は純粋関数化してユニットテスト対象にする

### BGM クロスフェード方式 (Research Item 4)
- **Context**: gap-analysis Research Needed #4。Requirement 1.2 のフェード切替の実現方式
- **Sources Consulted**: DOTween `DOTween.To` / `DOTweenAsyncExtensions` (プロジェクト導入済み)、tech.md
- **Findings**:
  - AudioSource 2 本を A/B スワップし、旧を fade-out・新を fade-in で同時進行 (クロスフェード) すると無音区間が出ない
  - フェード中の音量設定変更は「フェード到達目標 = クリップ基準音量 × BGM カテゴリ音量」の再計算が必要
- **Implications**: BGM 用 AudioSource 2 本 + DOTween によるクロスフェード。フェード中の SetBgmVolume はトゥイーンを kill して目標値を張り替える設計とする

### ダイアログ / 動的生成ボタンの SE 適用経路 (Research Item 5)
- **Context**: gap-analysis Research Needed #5。Requirement 7.1/7.4。シーン走査で捕捉できないボタンへの対応
- **Sources Consulted**: `DialogContainer.cs` (L52-93)、`ClosetScrollerService` / `ProductCellView` (EnhancedScroller セル生成)、`DialogSampleButtonView` (RootScope 直接解決の先例)
- **Findings**:
  - ダイアログは全て `DialogContainer.LoadAndInstantiateAsync` を通る。`InjectGameObject` 直後に生成インスタンス配下の Button を走査すれば確実に捕捉できる
  - EnhancedScroller セルはスクローラ内部で Instantiate され、DI もフックも通らない。唯一確実なのはプレハブに自己配線コンポーネントを事前付与すること
  - 自己配線コンポーネントは MonoBehaviour のためコンストラクタ注入不可。`AudioServiceHandle` (RootScope のビルド時に実体を差し込む静的ホルダ) 経由で `IAudioService` を遅延解決する。静的アクセスはプロジェクト方針に反するが、DI 経路外生成物への唯一の橋として1箇所に限定する
- **Implications**: 3 経路で全ボタンをカバーする — (a) シーン静的: sceneLoaded 走査、(b) ダイアログ: DialogContainer フック、(c) 動的セル: `ButtonSe` コンポーネント事前付与。(a)(b) は override 用 `ButtonSe` が付いた Button をスキップする

### AudioMixer の要否 (Research Item 6)
- **Context**: gap-analysis Research Needed #6
- **Findings**:
  - 現要件はカテゴリ別音量 (BGM/SE) の直接制御で完結する。ダッキング・エフェクト・スナップショット等の要件はない
  - AudioSource.volume 直接制御はリニアで単純。AudioMixer は dB 変換 (`Mathf.Log10` ベース) の考慮が必要になり複雑化する
- **Implications**: AudioMixer は導入しない。将来必要になった場合も `AudioPlayerView` 内部の差し替えで済み、上位契約に影響しない

### シーンごとの BGM 割当方法 (追加調査)
- **Context**: Requirement 5.2/5.3。「どのシーンでどの BGM を鳴らすか」を誰が指示するか
- **Findings**:
  - 各シーンの Starter から `PlayBgm` を呼ぶ方式は全シーンへのコード追加が必要で、追加し忘れリスクがある (ボタン SE と同じ構図)
  - `sceneLoaded` フックは既にボタン走査で必要。同じフックで「シーン名 → BgmId」のマッピング表 (AudioRegistry 内) を引けば、シーン側のコードはゼロになる
  - Fade シーン (Additive) など割当のないシーンは「マッピングなし = BGM 変更なし」と解釈すれば、遷移中も BGM が継続する (Req 5.1/5.2 と整合)
- **Implications**: AudioRegistry にシーン名→BgmId マップを持たせ、`AudioSceneService` が sceneLoaded で自動再生する。`IAudioService.PlayBgm` の明示呼び出しも併存 (特殊演出用)

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| A: 各シーンに Audio 制御を分散 | 各 Scope/Starter が個別に AudioSource を持つ | 局所性 | BGM 継続不可 (Req 5)、重複実装 | 不採用。要件と根本的に矛盾 |
| B: Root 常駐サービス + 専用 View ホスト | `Root/` に Service/State/View を新設し RootScope 登録 | 既存層構造と完全整合、シーン跨ぎ再生、テスト分離可能 | Root 肥大化 (軽微) | **採用**。gap-analysis Option B/C 準拠 |
| C: シングルトン MonoBehaviour (静的 Instance) | 伝統的 AudioManager.Instance | 実装最速 | DI 方針違反、テスト不能 | 不採用。静的アクセスは ButtonSe の橋 1 箇所に限定 |

## Design Decisions

### Decision: 識別子は enum、対応表は ScriptableObject
- **Context**: Req 6.1/6.3。SE/BGM を一意な識別子で指定し、追加を容易にする
- **Alternatives Considered**:
  1. 文字列キー — タイポがコンパイル時に検出できない
  2. enum (`SeId` / `BgmId`) + `AudioRegistry` (ScriptableObject) でクリップ解決 — 型安全、Inspector で割当
- **Selected Approach**: enum + AudioRegistry。レジストリは `Resources/AudioRegistry.asset` から RootScope が同期ロードし fail-fast (RewardedAdConfig と同型)
- **Rationale**: 呼び出し側の型安全性と、アセット割当のコード外管理を両立。既存の Config-as-Asset パターンに整合
- **Trade-offs**: 新規 SE 追加時に enum 追記 + アセット割当の 2 手順が必要 (Req 6.3 の「識別子の追加のみ」の解釈として許容)。クリップ未割当は再生スキップ + エラーログ (Req 6.2)
- **Follow-up**: レジストリの `OnValidate` で enum 全値の割当漏れを警告する

### Decision: 音量の永続化は PlayerPrefsKey.AudioSetting + 専用 DTO
- **Context**: Req 4.1–4.4
- **Selected Approach**: `AudioSettingData { float BgmVolume; float SeVolume; }` を `PlayerPrefsService` で JSON 保存。キー欠落・パース失敗時は既定値 (両方 1.0) で継続しエラーログ
- **Rationale**: 既存の enum キー + JSON パターンそのまま。`PlayerPrefsService.Load<T>` がキー欠落時に null を返す既知の穴は AudioService 側の null ガードで吸収
- **Trade-offs**: なし (確立パターンの適用)

### Decision: ボタン SE は 3 経路ハイブリッド + 静的ハンドル 1 箇所
- **Context**: Req 7 全体。ユーザー承認済みの「自動フック + 例外上書き + 文脈依存は明示呼び出し」方針の具体化
- **Selected Approach**: 上記 Research Log (Item 5) の通り。`ButtonSe` は「上書き (別 SE / SE なし)」と「動的生成プレハブへの事前付与」を 1 コンポーネントで兼ねる
- **Rationale**: 9 割のボタンは走査で自動化し人的コストゼロ。例外と動的生成のみ明示付与
- **Trade-offs**: `AudioServiceHandle` という静的ホルダを 1 つ許容する。DI 純度より運用コスト削減を優先
- **Follow-up**: sceneLoaded 走査が `FindObjectsByType<Button>(FindObjectsInactive.Include)` で非アクティブ UI (閉じている ClosetUiView 等) も捕捉できることを実装時に検証

### Decision: サウンド有効/無効は MenuSetting から AudioSetting へ移管 (2026-08-17 追記)
- **Context**: 2026-08 の他メンバー実装で `MenuDialog` が `MenuSettingData.soundEnabled` を `PlayerPrefsKey.MenuSetting` に永続化 (消費者なし)。サウンド設定の保存場所が本設計の `AudioSetting` と二重化する衝突が発生
- **Alternatives Considered**:
  1. MenuSetting 現状維持 + AudioService が soundEnabled を読むだけの疎結合 — 保存場所が二重のまま。設定変更の通知経路も別途必要
  2. サウンド有効/無効を `IAudioService.SetSoundEnabled` に移管し、MenuDialog は委譲。`MenuSettingData` は `notificationEnabled` のみ保持 — 責務が一元化
- **Selected Approach**: 案 2 (ユーザー裁定済み)。requirements.md に 3.6/3.7 を追加、Requirement 4 を「オーディオ設定の永続化」に一般化
- **Rationale**: オーディオ状態の単一ソースは AudioService であるべき。無効化は BGM 停止ではなく無音化 (再生継続) とし、再有効化の即復帰を保証
- **Trade-offs**: MenuDialog への改修が実装スコープに追加。既存 `MenuSetting.soundEnabled` の値は移行しない (開発段階のため許容)
- **Follow-up**: DebugPanel (`DebugUiFactory.CreateButton` の実行時生成ボタン) は SE 自動適用の対象外と設計に明記済み。`DialogContainer` の 2026-08 改修 (`PrepareForOpen` / `PreloadAsync`) 後も `InjectGameObject` フックポイントは有効と確認済み

## Risks & Mitigations
- **フェード中の音量変更で目標値がずれる** — トゥイーン kill → 目標再計算のシーケンスを AudioPlayerView 内に閉じ込め、ユニットテスト可能な音量計算関数に分離
- **走査漏れボタン (実行時 AddComponent されたボタン等)** — 現状該当なし。発生したら `ButtonSe` 事前付与で対応するルールを design.md に明記
- **AudioRegistry の割当漏れ** — OnValidate 警告 + 実行時は再生スキップ + エラーログ (Req 6.2) で進行不能を防止
- **DOTween トゥイーンがシーン遷移で kill される設定** — DDoL ホスト上のトゥイーンは `SetLink` の対象がシーンに属さないため影響なし。実装時に DOTween 設定 (KillOnSceneChange 系) を確認

## References
- `C:\repos\cat\.kiro\specs\audio-manager\gap-analysis.md` — 実装ギャップ分析 (本調査の前段)
- `Assets/Scripts/Root/Service/DialogContainer.cs` — ダイアログ生成ファンネル (フックポイント)
- `Assets/Scripts/Root/Service/RewardedAdConfig.cs` — Config-as-Asset + fail-fast の先例
- [Unity Manual: AudioSource](https://docs.unity3d.com/Manual/class-AudioSource.html) — PlayOneShot / volume 仕様
- [VContainer: Getting Started](https://vcontainer.hadashikick.jp/) — RootLifetimeScope の DDoL 挙動
