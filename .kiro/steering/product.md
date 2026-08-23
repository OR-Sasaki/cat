# Product Overview

Unity 6ベースの2Dゲームプロジェクト。キャラクターの着せ替えや部屋のカスタマイズを中心とした機能を持つ。

## Core Capabilities

- **シーン遷移**: フェード効果を用いた滑らかなシーン間の移動
- **依存性注入**: VContainerによる疎結合なアーキテクチャ
- **キャラクター管理**: プレイヤーの衣装や状態管理 (UserEquippedOutfit)
- **マスターデータ管理**: ゲーム内データの一元管理 (MasterDataImportService)
- **ダイアログシステム**: Addressables経由の動的ダイアログ表示。DialogService/IDialogService、BaseDialogView継承による確認・メッセージ等のプリセット対応、BackdropView連携
- **アイソメトリックグリッド**: Homeシーン内のIsoGrid機能 (Service/State/View)
- **ショップ**: 商品・ガチャの表示・購入機能 (時限ショップのサイクル抽選を含む)
- **リワード広告**: LevelPlay SDK 経由のリワード広告視聴でアイテムを付与 (Shop に統合、日次視聴上限を管理)。`IRewardedAdService` で SDK を抽象化
- **タイマー**: タイマー機能および設定 (TimerSettingダイアログ経由)、Pomodoro機能はTimerシーン内に統合
- **集中時間の記録**: 日別の集中時間 (秒) を永続化 (TimerRecordService)。Historyシーンのカレンダー表示・ストリーク集計の単一ソース
- **ユーザー資産管理**: ポイント (UserPointService) とアイテム所持 (UserItemInventoryService) の状態保持・スナップショット。初回起動時の付与は初期アイテム (InitialItemService) のみで、それ以外はショップ等の獲得経路を通す
- **メニュー / 設定**: ホーム右上のメニューボタンから開く設定ダイアログ (MenuDialog)。サウンド・通知のON/OFFを永続化し、所持金 (毛糸) 残高の確認と規約類への導線を持つ
- **タップエフェクト**: 画面のどこを押しても指先で波紋 (リング) と粒が弾ける共通演出 (`TapEffectView`)。RootScope 常駐の最前面 Canvas で全シーン共通に動き、入力は消費しない
- **オーディオ (BGM / SE)**: `IAudioService` による BGM クロスフェード再生と SE 再生。シーンロードに応じた BGM 自動切替 (`AudioSceneService`) とボタンクリック SE の自動付与 (`ButtonSeAttacher`)。音量・ON/OFF 設定は PlayerPrefs に永続化
- **ブロブシャドウ**: キャラクター足元の楕円影 (`BlobShadowView`)。Character プレハブに常駐し、Timer の完了ポップ演出中は非表示にする
- **時刻抽象**: テスト容易性と決定論のため `IClock` 経由で現在時刻を取得

## Target Use Cases

- ロゴ画面 → タイトル画面 → ホーム画面への遷移
- 着せ替え (Closet)、模様替え (Redecorate) はHomeシーン内のUI機能として統合
- ショップ (Shop): 商品・ガチャ機能
- タイマー (Timer): タイマー機能 (設定はTimerSettingDialogで実施)
- 履歴 (History): 集中時間のカレンダー表示・月次集計・連続日数 (ストリーク) 表示
- メニュー (Menu): 設定トグル・所持金確認・規約類の確認 (ホームシーン上のダイアログ)
- プレイヤーデータの永続化 (PlayerPrefs)
- ダイアログを介したユーザーインタラクション (確認、メッセージ通知)

## Value Proposition

VContainerによる堅牢な依存性注入により、各シーンが独立しつつも共通サービスを利用可能。シーンベースのアーキテクチャで機能が明確に分離されている。

---
_Focus on patterns and purpose, not exhaustive feature lists_
_更新: 2026-08-23 — オーディオ (BGM/SE) とブロブシャドウを追加_
