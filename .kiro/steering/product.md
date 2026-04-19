# Product Overview

Unity 6ベースの2Dゲームプロジェクト。キャラクターの着せ替えや部屋のカスタマイズを中心とした機能を持つ。

## Core Capabilities

- **シーン遷移**: フェード効果を用いた滑らかなシーン間の移動
- **依存性注入**: VContainerによる疎結合なアーキテクチャ
- **キャラクター管理**: プレイヤーの衣装や状態管理 (UserEquippedOutfit)
- **マスターデータ管理**: ゲーム内データの一元管理 (MasterDataImportService)
- **ダイアログシステム**: Addressables経由の動的ダイアログ表示。DialogService/IDialogService、BaseDialogView継承による確認・メッセージ等のプリセット対応、BackdropView連携
- **アイソメトリックグリッド**: Homeシーン内のIsoGrid機能 (Service/State/View)
- **ショップ**: 商品・ガチャの表示・購入機能
- **タイマー**: タイマー機能および設定 (TimerSettingダイアログ経由)

## Target Use Cases

- ロゴ画面 → タイトル画面 → ホーム画面への遷移
- 着せ替え (Closet)、模様替え (Redecorate) はHomeシーン内のUI機能として統合
- ショップ (Shop): 商品・ガチャ機能
- タイマー (Timer): タイマー機能 (設定はTimerSettingDialogで実施)
- 履歴 (History): 履歴閲覧機能
- プレイヤーデータの永続化 (PlayerPrefs)
- ダイアログを介したユーザーインタラクション (確認、メッセージ通知)

## Value Proposition

VContainerによる堅牢な依存性注入により、各シーンが独立しつつも共通サービスを利用可能。シーンベースのアーキテクチャで機能が明確に分離されている。

---
_Focus on patterns and purpose, not exhaustive feature lists_
