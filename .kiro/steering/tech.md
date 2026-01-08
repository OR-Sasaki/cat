# Technology Stack

## Architecture

シーンベースアーキテクチャ + VContainerによる依存性注入パターン。各シーンが独立したLifetimeScopeを持ち、RootScopeから全シーン共通のサービスを利用。

## Core Technologies

- **Platform**: Unity 6 (6000.x.x)
- **Render Pipeline**: Universal Render Pipeline (URP) 17.2.0
- **Language**: C# (.NET Standard 2.1)
- **DI Framework**: VContainer 1.17.0 (GitHub経由)

## Key Libraries

- **Async**: UniTask (async/await拡張、UniTaskVoid、CancellationToken対応)
- **Input System**: New Input System 1.14.2
- **Navigation**: NavMeshPlus (2D用NavMesh)
- **Animation**: 2D Animation 12.0.3, Cinemachine 3.1.5
- **Asset Management**: Addressables 2.7.6
- **Timeline**: Unity Timeline 1.8.9

## Development Standards

### Coding Conventions
- **Access Modifiers**: `private`は省略 (デフォルト)
- **Field Naming**: privateフィールドは `_fieldName` (アンダースコアプレフィックス)
- **Readonly**: コンストラクタでのみ初期化されるフィールドは `readonly`
- **Pattern Matching**: 推奨 - `while (asyncLoad is { isDone: false })`

### Error Logging
常にクラスコンテキスト付き:
```csharp
Debug.LogError($"[ClassName] {e.Message}\n{e.StackTrace}");
```

### Scene Naming
シーン名定数は `Assets/Scripts/Utils/Const.cs` で管理

## Development Environment

### Required Tools
- Unity 6 with URP support
- Unity Hub

### Common Commands
```bash
# Unity起動: Unity Hubからプロジェクト選択
# 初期シーン: Assets/Scenes/Logo.unity
```

## Key Technical Decisions

### VContainer DI Pattern
- **RootScope**: 全シーン共通のシングルトンサービス (`SceneLoader`, `PlayerPrefsService` など)
- **SceneScope**: 各シーンごとのスコープド登録 (`HomeScope`, `TitleScope` など)
- **Lifetime**: `Singleton` (RootScope), `Scoped` (SceneScope)

### Scene Transition System
Fadeシーンを加算的にロードし、FadeOut → ターゲットロード → FadeIn → Fadeアンロードの順でシーン遷移を実行

### Dependency Direction (厳密なルール)
```
View → Service → State
  ↓        ↓
Starter  Manager  (Scope が全体を構成)
```
逆方向の依存は禁止 (例: State → Service)

---
_Document standards and patterns, not every dependency_
