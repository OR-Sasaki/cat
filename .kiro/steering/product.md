# Product Overview

Unity 6 の 2D ゲーム。キャラクターの着せ替えと部屋のカスタマイズが中心。

## Screens

Logo → Title → Home。Home が着せ替え (Closet) / 模様替え (Redecorate) / メニュー (MenuDialog) を内包し、Shop / Timer / History は独立シーン。永続化は PlayerPrefs

## Core Capabilities

- **キャラクター**: 衣装と状態 (`UserEquippedOutfit`)。足元の丸影 `BlobShadowView` はタイマー完了ポップ中のみ非表示
- **着せ替え / 模様替え**: 所持している Outfit・家具をグリッド表示して即時適用する
- **アイソメトリックグリッド**: 家具配置の基盤 (Home の IsoGrid)
- **設置予告**: 家具ドラッグ中に対象面 (床 / 壁) のグリッド線と設置予告面を出す (置ける = 白 / 置けない = 赤)
- **操作フィードバック演出**: 着せ替えはもくもく雲 + キラキラ + 肉球で覆いピークで差し替え、家具設置は落下して弾む着地。いずれも約 0.5 秒の一続きの動き
- **タップエフェクト**: 全シーン共通で指先に波紋と粒 (`TapEffectView`)。入力は消費しない
- **ショップ**: 商品・ガチャの購入 (時限ショップのサイクル抽選を含む)。リワード広告の視聴でもアイテムを付与する (日次視聴上限あり)
- **タイマー**: Pomodoro を Timer シーンに統合、設定は TimerSetting ダイアログ。完了時に集中時間ぶんの毛糸玉を付与 (日次上限は JST 境界)
- **集中時間の記録**: 日別の秒数を永続化 (`TimerRecordService`)。History のカレンダー・月次集計・ストリークの単一ソース
- **ユーザー資産**: ポイント (`UserPointService`) とアイテム所持 (`UserItemInventoryService`)。初回付与は初期アイテムのみで、他はショップ等の獲得経路を通す
- **メニュー / 設定**: サウンド・通知の ON/OFF 永続化、毛糸残高の確認、規約類への導線
- **オーディオ**: BGM クロスフェードと SE。シーン別 BGM 自動切替とボタン SE 自動付与。音量・ON/OFF はメニュー設定と連動
- **マスターデータ**: CSV を `MasterDataImportService` が一元ロード
- **ダイアログ**: Addressables 経由の動的表示 (`IDialogService`)。確認・メッセージは `BaseDialogView` のプリセット

---
_目的とパターンのみ記す。機能の網羅列挙はしない_
