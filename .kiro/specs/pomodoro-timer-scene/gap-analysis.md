# ギャップ分析: Pomodoroタイマーシーン

## 分析サマリー

- **既存の基盤**: Timer シーン・フォルダ構造（`Assets/Scripts/Timer/`）は scaffold 済みだが、実装は `TimerScope` と `ReturnButtonView` のみ。タイマー設定（`TimerSettingData`）とシーン遷移フロー（`HomeFooterView` → ダイアログ → シーン遷移）は完成している
- **主要ギャップ**: Pomodoroステート管理、タイマーカウントダウンロジック、UI 3面切り替え、キャラクターアニメーション、背景スクロールのすべてが未実装
- **アーキテクチャ適合性**: 既存のシーンベース + VContainer パターン（Service/State/View/Starter/Scope）にそのまま従って実装可能
- **外部ライブラリ不要**: UniTask + AnimationCurve による手動 Tween で UI スライドアニメーション実現可能（DOTween 等は未導入）
- **データ受け渡し課題**: ホームシーンで設定した `TimerSettingData` をタイマーシーンで読み取る仕組みが必要（PlayerPrefs 経由で既に永続化済みのため読み出すだけで可能）

---

## 1. 要件-既存アセット対応マップ

| 要件 | 既存アセット | ギャップ |
|------|-------------|---------|
| **R1: ステート管理** | なし | **Missing** - ステートマシン（Focus/Break/Complete）の新規実装が必要 |
| **R2: タイマー表示** | なし | **Missing** - カウントダウン/カウントアップロジック、時計表示 UI が必要 |
| **R3: ユーザー操作** | `ReturnButtonView`（ホーム遷移ボタン） | **Partial** - 一時停止、休憩/集中切替ボタンが未実装 |
| **R4: ボタン制御** | なし | **Missing** - ステートに応じたボタン表示/非表示ロジックが必要 |
| **R5: サイクル管理** | `TimerSettingData`（focusTime, breakTime, sets） | **Partial** - データ定義済み。セット数の進行管理ロジックが未実装 |
| **R6: UI切り替え演出** | `FadeView`（AnimationCurve 使用例あり） | **Missing** - スライドイン/アウトアニメーションの新規実装。FadeView のパターンを参考にできる |
| **R7: キャラクターアニメ** | `CharacterView`（着せ替えスプライト管理） | **Missing** - 走行アニメーション用の Animator/Animation 制御が必要。既存の `CharacterView` はスプライト差し替えのみ |
| **R8: 背景スクロール** | なし | **Missing** - 右→左スクロール背景の新規実装が必要 |
| **R9: シーン遷移** | `SceneLoader` + `FadeService`（完全実装済み） | **None** - そのまま利用可能 |

---

## 2. 既存コードベースの統合ポイント

### 利用可能な既存コンポーネント

| コンポーネント | 場所 | 利用方法 |
|--------------|------|---------|
| `SceneLoader` | `Root/Service/SceneLoader.cs` | ホームへの帰還に使用 |
| `PlayerPrefsService` | `Root/Service/PlayerPrefsService.cs` | `TimerSettingData` の読み出し |
| `TimerSettingData` | `TimerSetting/State/TimerSettingData.cs` | focusTime, breakTime, sets の取得 |
| `SceneScope` | `Root/Scope/SceneScope.cs` | `TimerScope` の基底クラス（既に継承済み） |
| `TimerScope` | `Timer/Scope/TimerScope.cs` | DI 設定（拡張が必要） |
| `ReturnButtonView` | `Timer/View/ReturnButtonView.cs` | ホーム遷移ボタン（既存。完了画面でも再利用可能だが、ステート制御の統合が必要） |
| `Const.SceneName.Timer` | `Utils/Const.cs` | シーン名定数（定義済み） |
| `AnimationCurve` パターン | `Fade/View/FadeView.cs` | UI スライドアニメーションの参考実装 |

### データフロー

```
[HomeFooterView] → TimerSettingDialog → PlayerPrefs に保存
                → SceneLoader.Load("Timer")
                    ↓
[TimerScene] → PlayerPrefsService.Load<TimerSettingData>() → Pomodoro開始
```

### 制約事項

- **Tween ライブラリ未導入**: DOTween / LeanTween は使われていない。`AnimationCurve` + コルーチン / UniTask による手動アニメーションが既存パターン
- **Animator 未使用**: 既存コードに `Animator` コンポーネントの直接操作はない（`CharacterView` はスプライト差し替えのみ）。タイマーシーン用のキャラクターアニメーションは Unity Animation/Animator で新規セットアップが必要
- **UniTask の使用**: 非同期処理は UniTask が標準。タイマーカウントダウンは `UniTask.Delay` や `Update` ループで実装可能

---

## 3. 実装アプローチ

### Option A: フラットなService/State構成

**概要**: 1つの `PomodoroService` と `PomodoroState` でステート管理・タイマーロジック・サイクル管理をまとめる

**新規ファイル**:
- `Timer/State/PomodoroState.cs` — ステート、残り時間、セット進行、合計集中時間
- `Timer/Service/PomodoroService.cs` — タイマーロジック、ステート遷移
- `Timer/Starter/TimerStarter.cs` — エントリーポイント（設定読み込み → タイマー開始）
- `Timer/View/TimerDisplayView.cs` — 時計表示・メッセージ表示
- `Timer/View/TimerControlView.cs` — 操作ボタン群（一時停止、休憩、集中）
- `Timer/View/TimerUiSlideView.cs` — 集中/休憩/完了 UI のスライドアニメーション
- `Timer/View/BackgroundScrollView.cs` — 背景スクロール
- `Timer/View/TimerCharacterView.cs` — キャラクターアニメーション制御

**トレードオフ**:
- ✅ ファイル数が少なく見通しが良い
- ✅ 既存パターン（HomeStarter, HomeScope 等）と一致
- ❌ `PomodoroService` が肥大化するリスク

### Option B: ステートパターンによる分離

**概要**: ステートごと（Focus/Break/Complete）に振る舞いを分離し、ステートマシンで管理

**新規ファイル**: Option A + 以下
- `Timer/State/ITimerPhase.cs` — フェーズインターフェース
- `Timer/State/FocusPhase.cs`, `BreakPhase.cs`, `CompletePhase.cs`
- `Timer/Service/PomodoroStateMachine.cs` — ステートマシン

**トレードオフ**:
- ✅ 各フェーズのロジックが明確に分離
- ✅ 拡張性が高い（新ステート追加が容易）
- ❌ ファイル数が増える
- ❌ この規模ではオーバーエンジニアリングの可能性

### Option C: ハイブリッド（推奨候補）

**概要**: Option A ベースだが、ステート遷移ロジックを `PomodoroService` に enum ベースのシンプルなステートマシンとして実装。View 層は責務ごとに分離

**新規ファイル**: Option A と同じ

**ポイント**:
- `PomodoroState` に `enum PomodoroPhase { Focus, Break, Complete }` を持つ
- `PomodoroService` がフェーズ遷移を管理し、イベント or コールバックで View に通知
- View は `TimerUiSlideView` が 3 面の切り替えアニメーションを担当

**トレードオフ**:
- ✅ シンプルだが拡張可能
- ✅ 既存コードベースの複雑度に合致
- ✅ enum + switch で見通しが良い
- ❌ Service が多機能になるが、Manager 層で分割可能

---

## 4. 要調査事項（デザインフェーズへ持ち越し）

| 項目 | 理由 |
|------|------|
| **キャラクターアニメーションの実装方式** | Unity Animation (Sprite Animation) vs Animator Controller のどちらが適切か。既存の `CharacterView` はスプライト差し替えのみで、ランニングアニメーションのための仕組みがない。アセットの有無も確認が必要 |
| **背景スクロールの実装方式** | UV スクロール vs Transform 移動 vs パララックス。テクスチャアセットの形式に依存 |
| **UI スライド方向と配置** | 3面 UI の初期配置（画面外に待機）と RectTransform のアンカー設計 |
| **ReturnButtonView の統合** | 既存の `ReturnButtonView` を完了画面のボタンとしても使うか、新たなView に統合するか |
| **タイマー精度** | `Time.deltaTime` ベース vs `UniTask.Delay` ベース。バックグラウンド復帰時の挙動 |

---

## 5. 実装複雑度 & リスク

- **工数**: **M（3〜7日）** — 新規パターンは少ないが、UI アニメーション・キャラクターアニメーション・背景スクロールの 3 つのビジュアル要素を含むため
- **リスク**: **Medium** — アニメーションアセット（走行スプライト、背景テクスチャ）の準備状況に依存。ロジック面はシンプルだが、ビジュアル面でのアセット不足が実装を遅延させる可能性あり

---

## 6. デザインフェーズへの推奨事項

1. **Option C（ハイブリッド）** をベースにデザインを進めることを推奨。既存コードベースの複雑度に合致し、過剰な抽象化を避けつつ拡張性を確保できる
2. **アセット確認を先行**: キャラクター走行アニメーション素材、背景テクスチャの有無を確認し、なければ仮素材での実装を計画
3. **TimerSettingData の受け渡し**: PlayerPrefs 経由の読み出しで十分（追加の仕組み不要）
4. **ReturnButtonView**: 新しい統合 View に組み込み、ステートに応じた表示制御を行う方が一貫性が高い
