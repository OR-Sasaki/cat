# CLAUDE.md

このファイルはClaude Code (claude.ai/code) がこのリポジトリで作業する際のガイダンスを提供します。

## プロジェクト概要

VContainerによる依存性注入とUniversal Render Pipeline (URP) を使用したUnity 2Dゲームプロジェクトです。各シーンに標準化されたフォルダ構造を持つシーンベースのアーキテクチャを採用しています。

## Unity情報

- **Unity バージョン**: Unity 6 (URP 17.2.0 ベース)
- **レンダーパイプライン**: Universal Render Pipeline (URP)
- **入力システム**: New Input System (1.14.2)
- **DIフレームワーク**: VContainer 1.17.0 (GitHubパッケージ経由)

## ビルドと実行

### プロジェクトを開く
1. Unity Hubでプロジェクトを開く
2. Unity 6 with URPを使用
3. 初期シーン: `Assets/Scenes/Logo.unity`

### ビルド設定
- ビルドシーンは `ProjectSettings/EditorBuildSettings.asset` で設定
- シーン順序: Logo → Title → Fade → Closet → History → Home → Redecorate → Shop → Timer

### Unity エディタでの実行
- `Assets/Scenes/Logo.unity` または任意のメインシーン (Title, Home など) を開く
- Unity エディタで再生ボタンを押す
- Fadeシーンはトランジション用に加算的にロードされるため、直接ロードしないこと

## アーキテクチャ

### シーンベース構造

各シーンは `Assets/Scripts/{シーン名}/` 配下に標準化されたフォルダ構造を持つ:
- **Manager/**: ビジネスロジックとオーケストレーション
- **Scope/**: VContainerの依存性注入設定
- **Service/**: サービス層の実装（ビジネス操作）
- **Starter/**: VContainerの `IStartable` を実装したエントリーポイント
- **State/**: 状態管理（データと状態）
- **View/**: UIとビューの実装（MonoBehaviour）

### 依存方向（厳密なルール）
```
View  →  Service  →  State
  ↓          ↓
Starter    Manager     (Scope が全体を構成)
```

- **許可**: 上位層 → 下位層の依存のみ
- **禁止**: 逆方向の依存（例: `State → Service`, `Service → View`）

### VContainer セットアップ

- **RootScope**: `Assets/Settings/VContainer/VContainerSettings.asset` で設定
- RootScopeの場所: `Assets/Scripts/Root/Scope/RootScope.cs`
- RootScopeは全シーンで利用可能なシングルトンサービスを提供:
  - `SceneLoader`: シーン遷移サービス
  - `SceneLoaderState`: シーン読み込み状態管理

### シーン遷移システム

シーン遷移はFadeシーンパターンを使用:
1. `SceneLoader.Load(targetSceneName, fadeOutDuration, fadeInDuration)` を呼び出す
2. Fadeシーンが加算的にロードされる
3. シーケンス: FadeOut → ターゲットをロード → FadeIn → Fadeをアンロード
4. 実装場所: `Assets/Scripts/Fade/` と `Assets/Scripts/Root/Service/SceneLoader.cs`

### 利用可能なシーン

シーン名は `Assets/Scripts/Utils/Const.cs` で定義:
- Fade (遷移シーン)
- Title
- Home
- Redecorate
- Closet
- Timer
- Shop
- History

## コーディング規約

参考: https://qiita.com/Ted-HM/items/1d4ecdc2a252fe745871

### アクセス修飾子と命名規則
- `private` アクセス修飾子は省略すること（デフォルト）
- privateフィールドにはアンダースコアプレフィックスを使用: `_fieldName`
- コンストラクタでのみ値が入るフィールドには `readonly` を付ける: `readonly FieldType _field;`

### コードスタイル
- 条件式ではパターンマッチングが推奨:
  - ✅ 正: `while (asyncLoad is { isDone: false })`
  - ❌ 誤: `while (asyncLoad != null && !asyncLoad.isDone)`

### エラーログ
常にクラスコンテキスト付きでエラーをログ出力:
```csharp
Debug.LogError($"[ClassName] {e.Message}\n{e.StackTrace}");
```

## 新しいシーンの作成

1. `Assets/Scripts/TemplateScene/` フォルダ構造をコピー
2. シーン名にリネーム（例: `NewScene`）
3. 対応するフォルダを作成: Manager, Scope, Service, Starter, State, View
4. `LifetimeScope` を継承したScopeクラスを実装
5. `Configure(IContainerBuilder builder)` で依存関係を登録
6. `Assets/Scripts/Utils/Const.cs` にシーン名定数を追加
7. ビルド設定にシーンを追加

## 開発ワークフロー

### シーン遷移のテスト
- サービス層から `SceneLoader.Load(sceneName)` を使用してシーン遷移
- カスタム期間でフェード遷移をテスト
- 各シーンスコープで依存関係が適切に注入されているか確認

### デバッグ
- エラーログは `[ClassName] {Message}\n{StackTrace}` の形式に従う
- 注入が失敗した場合はVContainerのバインディングを確認
- シーン読み込みが動作するようビルド設定にシーンが含まれているか確認

## プロジェクト構造

```
Assets/
├── Fonts/              # フォントアセット
├── Scenes/             # Unityシーンファイル
│   ├── Logo.unity      # 初期ローディングシーン
│   ├── Title.unity     # タイトル画面
│   ├── Fade.unity      # 遷移シーン（加算的にロード）
│   └── ...
├── Scripts/
│   ├── Root/           # グローバルサービス (RootScope)
│   │   ├── Scope/      # RootScope設定
│   │   ├── Service/    # SceneLoader
│   │   └── State/      # SceneLoaderState
│   ├── TemplateScene/  # 新規シーン用テンプレート
│   ├── Utils/          # Const.cs とユーティリティ
│   └── {SceneName}/    # 各シーンは標準構造に従う
├── Settings/
│   ├── VContainer/     # VContainer設定とRootScopeプレハブ
│   ├── UniversalRP.asset
│   └── ...
└── Textures/           # スプライトとテクスチャアセット
```
