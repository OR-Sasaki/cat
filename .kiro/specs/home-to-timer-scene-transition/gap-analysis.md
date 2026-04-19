# ギャップ分析: ホーム画面からタイマー画面へのシーン遷移

## 分析サマリー

- **既存のシーン遷移基盤（SceneLoader / FadeService / FadeManager）は完全に機能しており**、タイマーシーンへの遷移自体は既に`HomeFooterView`で実装済み（`sceneLoader.Load(Const.SceneName.Timer)`）
- **ダイアログシステム（DialogService / BaseDialogView / BackdropView）も成熟しており**、タイマー設定ダイアログは既存パターンに従って実装可能
- **TimerSettingフォルダは空のディレクトリ構造のみ存在**（Manager/Scope/Service/Starter/State/View）、.csファイルなし
- **TimerScopeのConfigure()は空**で、DI登録が必要
- **設定値の永続化にはPlayerPrefsServiceが利用可能**だが、TimerSetting用のPlayerPrefsKeyが未定義

---

## 1. 要件とアセットのマッピング

### Requirement 1: タイマー設定ダイアログの表示

| 技術要素 | 既存アセット | ステータス |
|---------|------------|---------|
| ダイアログ表示 | `DialogService.OpenAsync<TDialog, TArgs>()` | ✅ 利用可能 |
| 背面ブロック | `BackdropView` (alpha=0.5+, blocksRaycasts) | ✅ 利用可能 |
| ×ボタンによる閉じる | `BaseDialogView.RequestClose(DialogResult.Cancel)` | ✅ パターン確立 |
| タイマー設定ダイアログUI | — | ❌ **Missing**: 新規作成が必要 |
| フッターのタイマーボタン連携 | `HomeFooterView._timerButton` | ⚠️ **要変更**: 現在は直接`SceneLoader.Load()`を呼んでいる |

**ギャップ**: `HomeFooterView`のタイマーボタンは現在ダイアログなしで直接シーン遷移する。ダイアログ表示を挟むように変更が必要。

### Requirement 2: ポモドーロタイマーの設定項目

| 技術要素 | 既存アセット | ステータス |
|---------|------------|---------|
| 設定UI（集中/休憩/セット回数） | — | ❌ **Missing**: 新規ダイアログView |
| 設定値のデータモデル | — | ❌ **Missing**: 新規データクラス |
| 永続化 | `PlayerPrefsService` (JSON) | ⚠️ **要拡張**: `PlayerPrefsKey`にTimerSetting追加が必要 |
| デフォルト値/バリデーション | — | ❌ **Missing**: 新規ロジック |

**ギャップ**: 設定値を保持するデータクラス、ダイアログView、永続化キーがすべて未実装。

### Requirement 3: ダイアログからタイマーシーンへの遷移

| 技術要素 | 既存アセット | ステータス |
|---------|------------|---------|
| フェード遷移 | `SceneLoader.Load()` → `FadeManager` → `FadeService` | ✅ 利用可能 |
| 多重遷移防止 | Fadeシーンの全画面CanvasGroup (blocksRaycasts) | ✅ **既存で対応済み**: Fadeシーン加算ロード時に全画面Canvasが背面タップをブロック |
| ダイアログ→遷移の連携 | — | ❌ **Missing**: Startボタンのハンドラで`DialogResult.Ok`→`SceneLoader.Load()`の流れを実装 |

**ギャップ**: ダイアログのStartボタン押下後にシーン遷移を開始するフロー制御が必要。

### Requirement 4: タイマーシーンの画面表示

| 技術要素 | 既存アセット | ステータス |
|---------|------------|---------|
| TimerScope (DI) | `Timer/Scope/TimerScope.cs` | ⚠️ **要拡張**: Configure()が空 |
| タイマーUI | — | ❌ **Missing**: 新規View |
| 戻るボタン | — | ❌ **Missing**: 新規UI要素 |

**ギャップ**: タイマーシーンのUI全体と、戻るボタンの実装が必要。

### Requirement 5: タイマーからホームへの復帰遷移

| 技術要素 | 既存アセット | ステータス |
|---------|------------|---------|
| フェード遷移 | `SceneLoader.Load(Const.SceneName.Home)` | ✅ 利用可能 |
| ホーム初期状態復帰 | `HomeState.ForceSetState(State.Home)` | ✅ 利用可能 |
| 多重遷移防止 | Fadeシーンの全画面CanvasGroup | ✅ **既存で対応済み** |

**ギャップ**: 戻るボタンの実装と、ホームシーン復帰時の初期状態設定。

### Requirement 6: フェード演出

| 技術要素 | 既存アセット | ステータス |
|---------|------------|---------|
| フェードアウト/イン | `FadeService.FadeOut/FadeIn()` (デフォルト0.5s) | ✅ 完全対応 |
| Fadeシーンアンロード | `FadeService.UnloadFadeScene()` | ✅ 完全対応 |

**ギャップ**: なし。既存実装が要件を完全に満たす。

### Requirement 7: エラー時の安全な動作

| 技術要素 | 既存アセット | ステータス |
|---------|------------|---------|
| エラーログ出力 | `Debug.LogError($"[ClassName]...")` パターン | ✅ パターン確立 |
| フェード解除 | — | ⚠️ **要調査**: FadeManagerのエラーハンドリングがどうなっているか |

**ギャップ**: FadeManager/FadeServiceのエラーハンドリングを確認し、必要に応じて追加。

---

## 2. 実装アプローチの選択肢

### Option A: 既存コンポーネントの拡張（最小変更）

**対象**: `HomeFooterView`を修正し、タイマー設定ダイアログを新規作成

- **変更ファイル**:
  - `HomeFooterView.cs`: タイマーボタンの動作を「直接遷移」→「ダイアログ表示→遷移」に変更
  - `PlayerPrefsService.cs`: `PlayerPrefsKey`にTimerSetting追加
  - `TimerScope.cs`: 必要なサービスをDI登録
- **新規ファイル**:
  - `TimerSettingDialog.cs` (Root/View): `BaseDialogView<TimerSettingDialogArgs>`を継承
  - `TimerSettingDialogArgs.cs` (Root/View): 設定値の引数record
  - `TimerSettingData.cs`: 永続化用データクラス
  - `TimerView.cs` (Timer/View): 戻るボタンを含むタイマーシーンのUI
- **HomeFooterViewへの影響**:
  - `IDialogService`を注入に追加
  - タイマーボタンのリスナーをasyncハンドラに変更

**トレードオフ**:
- ✅ 変更ファイル数が最少
- ✅ 既存パターンに完全に従う
- ❌ `HomeFooterView`にダイアログ制御ロジックが入る
- ❌ タイマー設定ダイアログがRoot/Viewに配置される（ドメイン的にはTimer機能）

### Option B: 新規コンポーネント構成（TimerSettingフォルダ活用）

**対象**: TimerSettingフォルダ構造を活用して独立した機能として実装

- **新規ファイル（TimerSetting配下）**:
  - `TimerSetting/View/TimerSettingDialog.cs`: ダイアログUI
  - `TimerSetting/State/TimerSettingState.cs`: 設定値の状態管理
  - `TimerSetting/Service/TimerSettingService.cs`: 設定値の読み書き・バリデーション
- **変更ファイル**:
  - `HomeFooterView.cs`: ダイアログ表示フローへ変更
  - `PlayerPrefsService.cs`: PlayerPrefsKey追加
  - `TimerScope.cs`: DI登録追加
- **新規ファイル（Timer配下）**:
  - `Timer/View/TimerView.cs`: 戻るボタンUI

**トレードオフ**:
- ✅ TimerSettingフォルダ構造が既に用意されている
- ✅ 関心の分離が明確
- ✅ 将来のタイマー設定拡張に対応しやすい
- ❌ ファイル数が増える
- ❌ ダイアログはAddressablesでロードされるため、TimerSettingのネームスペースでもRoot経由で利用可能にする必要がある

### Option C: ハイブリッド（推奨候補）

**対象**: ダイアログViewはRoot/Viewパターンに従い、設定ロジックはTimerSetting配下に配置

- **Root/View**: `TimerSettingDialog.cs`（BaseDialogView継承、Addressablesで管理）
- **TimerSetting/State**: `TimerSettingState.cs`（設定値データ）
- **TimerSetting/Service**: `TimerSettingService.cs`（永続化・バリデーション）
- **Timer/View**: `TimerView.cs`（タイマーシーンUI）
- **HomeFooterView**: ダイアログ→遷移フロー

**トレードオフ**:
- ✅ ダイアログの配置がプロジェクトパターン（Addressables `Dialogs/`）と整合
- ✅ ビジネスロジックはTimerSetting配下で分離
- ❌ ダイアログViewとロジックが異なるフォルダに分散

---

## 3. 実装複雑度とリスク

### 工数見積り: **S（1〜3日）**

**根拠**: 既存の成熟したパターン（DialogService、SceneLoader、BaseDialogView）をそのまま踏襲できる。新規作成は主にダイアログUIと設定データクラス。アーキテクチャ変更は不要。

### リスク: **Low**

**根拠**:
- 使用技術はすべてプロジェクト内で確立済み
- シーン遷移・ダイアログの両基盤が安定
- スコープが明確で、他機能への影響が限定的
- 多重遷移防止もFadeシーンのCanvasで既に対応済み

---

## 4. 要調査事項（Research Needed）

1. **TimerSettingダイアログのPrefab/Addressables設定**: `Dialogs/TimerSettingDialog.prefab`のAddressables登録方法（UnityMCPで実施可能か）
3. **ダイアログからの設定値受け渡し**: DialogResult（Ok/Cancel）だけでは設定値を返せないため、`TimerSettingState`経由で共有するか、カスタムResultを使うか
4. **数値入力UI**: 集中時間・休憩時間・セット回数の入力UIの形式（スライダー、+/-ボタン、テキスト入力等）—— UIデザインに依存

---

## 5. 設計フェーズへの推奨事項

### 推奨アプローチ: **Option C（ハイブリッド）**

- ダイアログViewは既存の`Dialogs/`Addressablesパターンに合わせてRoot/Viewに配置
- 設定ロジック・状態管理はTimerSetting配下で独立管理
- `HomeFooterView`はダイアログ表示の起点として最小限の変更

### 設計フェーズでの重要決定事項

1. **設定値の受け渡しパターン**: ダイアログ→シーン遷移時の設定値共有方法
2. **PlayerPrefsKeyの拡張**: 既存enumへの追加でよいか
4. **TimerSettingServiceのスコープ**: RootScope（全シーン共通）vs TimerSettingScene固有
