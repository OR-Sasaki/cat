# Research Log: shop-scene-mock

## Summary

ショップシーンのモック実装に関する技術調査。既存のプロジェクトパターン（VContainer DI、シーンベースアーキテクチャ、DialogService）との統合方法、およびLayout Groupを使った手動配置セルの自動レイアウト実現可能性を調査した。

**Discovery Type**: Light（既存システムの拡張）

## Research Log

### Topic 1: 既存シーンアーキテクチャパターン

**Source**: `Assets/Scripts/Home/`

**Findings**:
- `HomeScope`: `SceneScope`を継承、SerializeFieldでViewをバインド、`RegisterComponent`/`RegisterInstance`で登録
- `HomeStarter`: `IStartable`を実装、コンストラクタインジェクション
- 依存の方向: View → Service → State
- EntryPointはServiceとStarterの両方に使用可能

**Implications**:
- ShopScopeはHomeScopeと同じパターンで実装
- ShopStarterはIStartableを実装し、初期化ロジックを担当

### Topic 2: DialogServiceの仕組み

**Source**: `Assets/Scripts/Root/Service/DialogService.cs`, `IDialogService.cs`, `BaseDialogView.cs`

**Findings**:
- `IDialogService.OpenAsync<TDialog, TArgs>(args, cancellationToken)`でダイアログを開く
- `BaseDialogView<TArgs>`を継承してダイアログ実装
- Addressables経由でロード: `Dialogs/{TypeName}.prefab`
- `DialogResult`: Ok, Cancel, Close
- `IDialogArgs`インターフェースで引数を定義

**Implications**:
- 汎用確認ダイアログは`BaseDialogView<CommonConfirmDialogArgs>`を継承
- `CommonConfirmDialogArgs`にタイトル、メッセージ、ボタンテキストを含める
- 既存の`SampleConfirmDialog`は引数なしのため、新規実装が必要

### Topic 3: Layout Groupによる手動配置セルの自動レイアウト

**Source**: [Unity UI Layout Groups Explained](https://www.hallgrimgames.com/blog/2018/10/16/unity-layout-groups-explained), [Layout Element Documentation](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-LayoutElement.html)

**Findings**:
- `VerticalLayoutGroup`/`HorizontalLayoutGroup`は子要素の異なるサイズをサポート
- `LayoutElement`コンポーネントで`preferredWidth`/`preferredHeight`を個別設定可能
- `GridLayoutGroup`は固定セルサイズのみ対応（異なるサイズ不可）
- 手動配置されたセルに対してもLayout Groupは適用可能

**Implications**:
- カテゴリ内セルコンテナに`VerticalLayoutGroup`を使用
- 横長1セル: `LayoutElement`で全幅指定
- 3セル行: `HorizontalLayoutGroup`でラップし、各セルが1/3幅
- 手動配置後も自動レイアウトで配置が調整される

### Topic 4: SceneLoaderの使用方法

**Source**: `Assets/Scripts/Root/Service/SceneLoader.cs`

**Findings**:
- `SceneLoader.Load(targetSceneName)`で遷移
- Fadeシーン経由でトランジション
- `Const.SceneName.Shop`は既に定義済み

**Implications**:
- 戻るボタンで前のシーン（おそらくHome）に遷移
- 遷移先シーン名の取得方法は要検討（固定値 or State保持）

## Architecture Patterns Evaluation

### Pattern: セルレイアウト方式

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| A. 完全手動配置（Layout Groupなし） | セルを手動で絶対位置配置 | 完全な制御 | 画面サイズ対応が困難 |
| B. VerticalLayoutGroup + 行ラッパー | 縦方向にVerticalLayoutGroup、横方向は行ごとにHorizontalLayoutGroupでラップ | 異なるサイズ混在可能、自動調整 | 行ラッパーが必要 |
| C. GridLayoutGroup | 固定グリッド | シンプル | 異なるサイズ不可 |

**Decision**: Option B改（VerticalLayoutGroup + セクション分離）を採用。カテゴリ内でWideSection（横長セル）とGridSection（GridLayoutGroup）を分離し、それぞれに適したLayout Groupを適用する。

### Pattern: 確認ダイアログ

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| A. SampleConfirmDialog拡張 | 既存クラスに引数対応を追加 | 変更少 | 既存コードに影響 |
| B. CommonConfirmDialog新規作成 | BaseDialogView<CommonConfirmDialogArgs>を継承した新規クラス | 既存に影響なし、汎用性高 | 新規実装コスト |

**Decision**: Option B（CommonConfirmDialog新規作成）を採用。既存の`SampleConfirmDialog`に影響を与えず、汎用的な確認ダイアログを提供。

## Design Decisions

| Decision | Rationale | Alternatives Considered |
|----------|-----------|-----------------------|
| ShopScopeはSceneScopeを継承 | 既存パターンに準拠 | 独自実装 |
| CommonConfirmDialogは新規作成 | 既存コードへの影響回避、引数対応 | SampleConfirmDialog拡張 |
| セルレイアウトにVerticalLayoutGroup使用 | 異なるサイズ混在対応、自動調整 | GridLayoutGroup、手動配置 |
| 戻り先シーンはHome固定 | モック段階ではシンプルに | State保持、履歴管理 |

## Risks and Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Layout Groupと手動配置の競合 | セル位置がずれる | Low | LayoutElementで明示的にサイズ指定 |
| CommonConfirmDialogのAddressables登録漏れ | ダイアログが開けない | Medium | セットアップ手順をドキュメント化 |
| 毛糸残高不足時のUI更新漏れ | UX低下 | Low | ShopStateの変更監視パターンを使用 |
