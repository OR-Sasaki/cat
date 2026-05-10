# Implementation Plan — canvas-safe-area-resizer

> 本タスク一覧は `requirements.md` / `design.md` / `research.md` を前提に、Canvas 配下の RectTransform を `Screen.safeArea` に追従させる単一責務 MonoBehaviour `CanvasSafeAreaResizer` (namespace `Root.View`) の実装手順を定義する。
>
> **要件 ID 形式**: `N.M` (N = Requirement 番号、M = Acceptance Criteria 番号)。

## Branches

**Base**: `feature/canvas-safe-area-resizer`

機能サイズは S (1〜3 日)、依存ファイルは新規追加 1 件のみで単一 PR で完結可能なため、ベースブランチのみで全タスクを実装し `main` へ直接 PR する。

## Multi-Agent 実行ガイド

本機能は単一ファイルへの追加が中心のため Sub-Agent 並列起動の利得は限定的。原則として **メインエージェントが順次 Edit / Write を進める** 方針とする。

- **シーケンシャル (マーカーなし)**: タスク 1〜3 は同一ファイル `CanvasSafeAreaResizer.cs` を逐次拡張するためメインエージェントが順次処理する。
- **並列起動可能 (`(P)` マーカー)**: タスク 5 のテスト 2 件のみ、独立した別ファイル (EditMode / PlayMode テスト) のため `Agent(subagent_type=general-purpose, run_in_background=true)` で同時投入可能。
- **Editor / UnityMCP 担当**: タスク 4 (シーン配線・Editor Play 検証)。Agent 分割は行わず、UnityMCP が利用可能な場合は半自動化、不可なら人手で実施。

## Tasks

### Branch: `feature/canvas-safe-area-resizer`

- [x] 1. コアコンポーネントを新設しアンカー算出と RectTransform 書込を実装する
  - [x] 1.1 必須属性と SerializeField を備えたコンポーネント骨格を作成する
    - 配置先: `Assets/Scripts/Root/View/CanvasSafeAreaResizer.cs` (namespace `Root.View`)
    - クラス属性: `[ExecuteAlways]` / `[RequireComponent(typeof(RectTransform))]` / `[DisallowMultipleComponent]` を付与
    - ファイル先頭に `#nullable enable` を付与し、`sealed class` として宣言
    - SerializeField: `_applyTop` / `_applyBottom` / `_applyLeft` / `_applyRight` (いずれも初期値 `true`)
    - private フィールド: 自身の `RectTransform` キャッシュ、直前適用値 (Rect / Vector2Int / ScreenOrientation)、`_isDirty` フラグ、警告出力済み判定 2 件 (Canvas 不在 / World Space)
    - `void Reset()` で `_rectTransform = GetComponent<RectTransform>()` を実行 (Inspector 配置時の初期化)
  - [x] 1.2 セーフエリアからアンカーを算出し RectTransform に適用するロジックを実装する
    - 純粋関数 `ComputeAnchors(Rect safeArea, Vector2 screenSize, bool applyTop, bool applyBottom, bool applyLeft, bool applyRight) -> (Vector2 anchorMin, Vector2 anchorMax)` を private static で定義し、テスト容易性を確保
    - 正規化式: `anchorMin.x = applyLeft ? safeArea.x / screenSize.x : 0`、`anchorMin.y = applyBottom ? safeArea.y / screenSize.y : 0`、`anchorMax.x = applyRight ? (safeArea.x + safeArea.width) / screenSize.x : 1`、`anchorMax.y = applyTop ? (safeArea.y + safeArea.height) / screenSize.y : 1`
    - `ApplyToRectTransform` 内部メソッドで `anchorMin` / `anchorMax` を書き込み、続けて `offsetMin = Vector2.zero` / `offsetMax = Vector2.zero` を設定
    - `screenSize.x` / `screenSize.y` のいずれかが 0 以下の場合は計算をスキップ (実機初期化前の保険)
  - [x] 1.3 強制再計算を行う public Apply メソッドを公開する
    - `public void Apply()` を 1 つだけ公開し、テストおよび外部からの明示的トリガに利用
    - `Apply()` 内部では「Canvas 解決 → RenderMode 判定 → ComputeAnchors → ApplyToRectTransform → スナップショット更新」の一連の流れを実行 (タスク 2 で詳細実装)
    - その他の public API は追加しない (インスペクター操作と Apply のみで完結する設計)
  - _Requirements: 1.1, 1.2, 1.3, 3.1, 3.2_

- [x] 2. Update ループと画面状態追従ロジックを実装する
  - [x] 2.1 親 Canvas 解決と RenderMode ガードを実装する
    - `GetComponentInParent<Canvas>(includeInactive: true)` で親 Canvas を取得し private フィールドにキャッシュ (`OnEnable` および親階層変化時に再取得)
    - 親 Canvas が `null` の場合は警告ログを 1 度だけ出力し、Apply 処理を early return する
    - `Canvas.renderMode` が `RenderMode.WorldSpace` の場合は警告ログを 1 度だけ出力し、Apply 処理を early return する
    - `RenderMode.ScreenSpaceOverlay` および `RenderMode.ScreenSpaceCamera` のいずれでも同一の正規化式で計算が成立することを保証 (両者共通の Apply 経路)
    - 親階層が変化したら警告抑止フラグを `false` に戻し、再評価できるようにする (`OnTransformParentChanged` を利用)
  - [x] 2.2 画面状態スナップショット比較と Update 早期 return を実装する
    - `Update()` 内で「`Screen.safeArea` / `Screen.width` / `Screen.height` / `Screen.orientation` / `_isDirty` のいずれかが直前適用時から変化したか」を判定
    - 変化なしフレームでは `RectTransform` への書込・キャッシュ更新・ログ出力を一切行わずに return
    - 変化検知時のみ Apply を実行し、Apply 完了後に直前適用値スナップショットを更新して `_isDirty = false` にリセット
    - スナップショット比較は構造体相当 (`Rect.Equals` / `int` / `enum` の単純比較) で副作用ゼロにする
  - [x] 2.3 警告ログの初回抑制とプレフィックスを共通化する
    - private ヘルパー `LogWarningOnce(ref bool emittedFlag, string message)` を 1 つだけ用意し、ログ出力時のみ `emittedFlag = true` でガード
    - 警告メッセージは `[CanvasSafeAreaResizer] No Canvas found in parents. Resizer disabled.` および `[CanvasSafeAreaResizer] World Space Canvas is not supported. Resizer disabled.` の 2 種を文字列定数化
    - `Debug.Log` (情報ログ) を新規追加しないことを実装規約として徹底 (警告/エラーのみ)
    - 親階層変化やコンポーネント再有効化時に `emittedFlag` をリセットして次回検知に備える
  - _Requirements: 1.4, 2.1, 2.2, 2.3, 2.4, 4.1, 4.2, 4.3, 6.1, 6.2, 6.3_

- [x] 3. OnValidate でフラグ変更を次フレーム Apply に反映する
  - `OnValidate()` を実装し、Inspector 値変更検知時に `_isDirty = true` を立てるのみとする (ここで `RectTransform` を直接書き換えない)
  - Edit Mode で `OnValidate` → 次の `Update` ティックで Apply が走り、Inspector のフラグ変更が即座に Game ビューへ反映される動線を確保 (`[ExecuteAlways]` の効果と組み合わせる)
  - `_isDirty` フラグは Apply 完了後に `false` にリセットする (ループ防止)
  - 開発者が Scene ビューや Inspector で `RectTransform` のアンカーを手動編集しても、次 `Update` で本コンポーネントが計算結果で上書きする挙動を担保 (差分判定の参照値は「直前に書き込んだ自身の計算結果」とし、外部の手動編集では `_lastApplied` が更新されないため次フレームで必ず差分検知される)
  - コンポーネント除去後の挙動は Unity 標準ライフサイクルに委ね、追加処理は実装しない (`OnDisable` / `OnDestroy` で特別なクリーンアップは不要)
  - _Requirements: 3.3, 5.1, 5.2, 5.3_

- [ ] 4. シーン組み込みと Editor Play 検証を行う **[人手 / UnityMCP 担当]**
  - [ ] 4.1 サンプル Canvas に SafeAreaPanel を配置しコンポーネントをアタッチする
    - 検証用シーン (例: `Assets/Scenes/Home.unity`) の Canvas 直下に空 GameObject `SafeAreaPanel` を新規追加
    - `SafeAreaPanel` の `RectTransform` を Stretch (anchorMin = (0,0), anchorMax = (1,1), offsetMin = offsetMax = 0) に初期設定
    - `SafeAreaPanel` に `CanvasSafeAreaResizer` をアタッチし、適用辺フラグはデフォルト (全 true) のまま
    - 既存 UI 要素から代表的な 1〜2 件を `SafeAreaPanel` の子に再配置して動作観察用とする (恒久移行は本タスク外)
    - 担当: UnityMCP 利用可能時は MCP 経由、不可なら Editor 手動操作
  - [ ] 4.2 Device Simulator + Editor Play モードで E2E シナリオを検証する
    - **基本フィット**: Game ビュー (Window > General > Device Simulator) で iPhone 14 Pro 等のノッチ機種を選択し、`SafeAreaPanel` の矩形がセーフエリア (ノッチ・ホームインジケータを除く) と完全一致すること (Req 1.1, 1.2, 4.1)
    - **画面回転**: 縦 → 横 → 縦と切替し、各回転後に `SafeAreaPanel` がセーフエリアに追従していること (Req 2.3, 4.3)
    - **解像度切替**: Device Simulator で別端末 (例: iPad / Pixel) に切替し、追従していること (Req 2.1, 2.2)
    - **辺別 ON/OFF**: Inspector で `_applyBottom = false` に変更し、即座に `SafeAreaPanel` の下端が画面下端まで延びること (`anchorMin.y == 0`)。`_applyTop` 等も同様に確認 (Req 3.1, 3.2, 3.3, 5.2)
    - **Canvas 不在検知**: 一時的に `SafeAreaPanel` を Canvas 階層外に移動し、Console に `[CanvasSafeAreaResizer] No Canvas found in parents.` 警告が **1 度だけ** 出ること (Req 1.4, 6.2)
    - **World Space スキップ**: `Fade.unity` を Screen Space - Overlay のまま検証し、別途 World Space Canvas を一時作成して配下に置いた場合に `[CanvasSafeAreaResizer] World Space Canvas is not supported.` 警告が出てアンカーが書き換わらないこと (Req 4.2)
    - **Edit Mode プレビュー**: Play モードを停止し、Inspector でフラグを切替えても Game ビューが追従すること (`[ExecuteAlways]` 動作、Req 5.1)
    - **手動編集の上書き**: Scene ビューで手動で `anchorMin` を弄っても次フレームで本コンポーネントが上書きすること (Req 5.2)
    - **コンポーネント除去**: `CanvasSafeAreaResizer` を `Remove Component` しても直前のアンカー値が保たれ、以降は書換が起きないこと (Req 5.3)
    - **差分なしフレーム**: Profiler / Debug ブレークポイントで「画面状態が変化していないフレームでは `ApplyToRectTransform` が呼ばれない」ことを確認 (Req 2.4, 6.1)
    - **ログ衛生**: 通常動作で `Debug.Log` が出力されないことを Console フィルタで確認 (Req 6.3)
  - _Requirements: 1.1, 1.2, 1.4, 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 4.1, 4.2, 4.3, 5.1, 5.2, 5.3, 6.1, 6.2, 6.3_

- [ ] 5. 自動テストを追加する **[任意 / 後追い可]**
  - [x]* 5.1 (P) ComputeAnchors の純粋関数テストを EditMode で実装する
    - Test 用 asmdef (`Tests/EditMode/Tests.EditMode.asmdef` 等) と `Tests/EditMode/Root/View/CanvasSafeAreaResizerTests.cs` を新設
    - フル領域 (`safeArea == screen`) で `anchorMin == (0,0)` / `anchorMax == (1,1)` (Req 1.1, 1.2)
    - 上ノッチケース (`safeArea.height < screen.height`) で `anchorMax.y < 1` (Req 1.1, 4.1)
    - 各辺 OFF パターン × 4 で対応辺が 0 / 1 にクランプされる (Req 3.1, 3.2)
    - `screenSize` が 0 のケースで例外を投げず安全に既定値を返す
    - 起動方法: 別 Agent で 5.2 と並列実行可能 (別ファイル)
    - **任意理由**: コア実装 (Task 1.2) で要件 1.1, 1.2, 3.1, 3.2 はすでに満たされているため、回帰防止用の補助テスト
  - [ ]* 5.2 (P) Update ループと警告挙動の PlayMode テストを実装する
    - Test 用 asmdef (`Tests/PlayMode/Tests.PlayMode.asmdef` 等) と `Tests/PlayMode/Root/View/CanvasSafeAreaResizerPlayModeTests.cs` を新設
    - Canvas 配下に動的生成した `SafeAreaPanel` で `OnEnable` 直後にアンカーが書き込まれる (Req 1.1)
    - 親 Canvas 不在時に `LogAssert.Expect(LogType.Warning, ...)` で警告 1 回検出後、`anchorMin/Max` が初期値のままであること (Req 1.4)
    - World Space Canvas で同様に警告 + 書換なし (Req 4.2)
    - 直前と同条件のフレームで `RectTransform.hasChanged` が立たないこと (Req 2.4, 6.1)
    - フラグ動的トグル後、次フレームでアンカーが反映されること (Req 3.3)
    - 起動方法: 別 Agent で 5.1 と並列実行可能 (別ファイル)
    - **任意理由**: Task 4.2 の Editor Play 検証で要件 1.4, 2.4, 3.3, 4.2, 6.1 はすでにカバーされているため、CI 自動化用の補助テスト
  - _Requirements: 1.1, 1.2, 1.4, 2.4, 3.1, 3.2, 3.3, 4.1, 4.2, 6.1_

## Requirements Coverage Summary

| Requirement | カバータスク |
|-------------|-------------|
| 1.1 | 1.2, 1.3, 4.2, 5.1, 5.2 |
| 1.2 | 1.2, 4.2, 5.1 |
| 1.3 | 1.1 |
| 1.4 | 2.1, 2.3, 4.2, 5.2 |
| 2.1 | 2.2, 4.2 |
| 2.2 | 2.2, 4.2 |
| 2.3 | 2.2, 4.2 |
| 2.4 | 2.2, 4.2, 5.2 |
| 3.1 | 1.1, 1.2, 4.2, 5.1 |
| 3.2 | 1.2, 4.2, 5.1 |
| 3.3 | 3, 4.2, 5.2 |
| 4.1 | 2.1, 4.2, 5.1 |
| 4.2 | 2.1, 2.3, 4.2, 5.2 |
| 4.3 | 2.1, 2.2, 4.2 |
| 5.1 | 1.1, 3, 4.2 |
| 5.2 | 2.2, 3, 4.2 |
| 5.3 | 3, 4.2 |
| 6.1 | 2.2, 4.2, 5.2 |
| 6.2 | 2.1, 2.3, 4.2 |
| 6.3 | 2.3, 4.2 |

全 6 要件 (20 Acceptance Criteria) がいずれかのタスクでカバーされている。Task 5 の 2 サブタスクは任意 (`*`) のため、必須実装範囲は Task 1〜4 で完結する。
