# Product Overview

Unity 6ベースの2Dゲームプロジェクト。キャラクターの着せ替えや部屋のカスタマイズを中心とした機能を持つ。

## Core Capabilities

- **シーン遷移**: フェード効果を用いた滑らかなシーン間の移動
- **依存性注入**: VContainerによる疎結合なアーキテクチャ
- **キャラクター管理**: プレイヤーの衣装や状態管理
- **マスターデータ管理**: ゲーム内データの一元管理
- **ダイアログシステム**: Addressables経由の動的ダイアログ表示、確認・メッセージ等のプリセット対応
- **アイソメトリックグリッド**: 2D NavMesh連携の斜視投影グリッドシステム

## Target Use Cases

- タイトル画面からホーム画面への遷移
- 着せ替え (Closet)、模様替え (Redecorate)、ショップ (Shop) などの各種機能シーン
- プレイヤーデータの永続化 (PlayerPrefs)

## Value Proposition

VContainerによる堅牢な依存性注入により、各シーンが独立しつつも共通サービスを利用可能。シーンベースのアーキテクチャで機能が明確に分離されている。

---
_Focus on patterns and purpose, not exhaustive feature lists_
