# Implementation Plan

## Branches

**Base**: `feature/user-inventory-management`

All tasks are implemented in the base branch.

## Tasks

- [x] 1. 毛糸残高の状態保持と PlayerPrefs 基盤拡張を整える
  - [x] 1.1 (P) 毛糸残高を保持する State と内部書き込み API を用意する
    - `YarnBalance` の保持、初期値 0、外部からは読み取りのみ許可
    - Service からのみ呼ばれる書き込みパス (internal 可視性)
  - [x] 1.2 (P) PlayerPrefs のキー管理に新規エントリ 2 種を追加する
    - 既存 enum に `UserItemInventory` と `UserPoint` を追加 (後続の家具タスクでも利用)
  - [x] 1.3 (P) 毛糸残高の永続化スナップショットをバージョン番号付きで定義する
    - `Version = 1` と `YarnBalance` を持つシリアライザブル DTO
    - 非互換バージョン時は破棄して残高 0 で初期化する挙動をサービス側で使う前提
  - _Requirements: 4.1, 4.2, 4.3, 7.2, 7.5_

- [x] 2. 毛糸操作 Service を実装する
  - [x] 2.1 (P) 毛糸サービスのインターフェース契約を定義する
    - 残高取得・加算・減算・変更イベント・初期化/保存 (UniTask + CancellationToken) を網羅
    - 結果型 (正常/不正引数/残高不足/桁あふれ) と `PointOperationResult` を定義
  - [x] 2.2 毛糸サービスの実装を行う
    - コンストラクタで Load を呼び出し、保存データがあれば復元・なければ 0 初期化
    - 加算は `amount > 0` と `int.MaxValue` 超過チェック、減算は `amount > 0` と残高充足チェック
    - 成功時のみ残高変更通知を同期発行し、購読者例外は try-catch で吸収
    - 残高変更直後に PlayerPrefs 保存、保存例外はログ出力のみでインメモリ状態を保持
    - `[Inject]` 属性と `#nullable enable` を適用
  - _Requirements: 4.2, 4.3, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 6.2, 6.3, 7.2, 7.3, 7.5, 8.5, 8.7, 8.8, 8.9_

- [ ]* 3. 毛糸サービスのユニットテストを追加する
  - 加算: 正常 / 負数 / 0 / `int.MaxValue` 超過で Overflow
  - 減算: 正常 / 負数 / 0 / 残高不足で Insufficient
  - 変更通知の発火と、購読者が例外を投げた場合でも他購読者への通知継続
  - PlayerPrefs の非互換バージョンを読み込んだ際に残高が 0 で初期化される
  - _Requirements: 5.2, 5.3, 5.4, 5.5, 5.7, 6.3, 7.3, 7.5_
  - _Note: Skipped — テスト インフラ (asmdef / Assembly-CSharp 参照) の整備が本 spec 外のため別タスクで対応_

- [x] 4. 所持アイテムの状態保持と永続化 DTO を整える
  - [x] 4.1 (P) 家具所持数と着せ替え所持集合を保持する State を用意する
    - 家具: 数量ベースのマップ (外部公開は読み取りのみ)
    - 着せ替え: 所持 ID の集合 (外部公開は読み取りのみ)
    - Service からのみ呼ばれる書き込みパス (internal 可視性) とクリア操作
  - [x] 4.2 (P) 所持アイテムの永続化スナップショットをバージョン番号付きで定義する
    - `Version = 1`、家具は (ID, 数量) 配列、着せ替えは ID 配列で表現 (JsonUtility 制約対応)
    - 非互換バージョン時は破棄して空状態で初期化する前提
  - _Requirements: 2.6, 3.7, 7.1, 7.4_

- [x] 5. 所持アイテム Service を実装する
  - [x] 5.1 (P) 所持アイテムサービスのインターフェース契約を定義する
    - 家具: 所持数取得 / 全一覧取得 / 加算
    - 着せ替え: 所持判定 / 全一覧取得 / 付与 (冪等)
    - 家具・着せ替えそれぞれの変更イベントと、初期化/保存 (UniTask + CancellationToken)
    - 結果型 (正常 / 不正引数 / 未知 ID) を定義
  - [x] 5.2 Load 時の復元処理と装備整合補完を実装する
    - コンストラクタで Load を呼び出し、バージョン一致時のみ復元
    - 復元した ID のうち MasterData に存在しないものは破棄し、バージョン不一致時は空状態で初期化
    - `UserEquippedOutfitService` の現在装備中着せ替えが所持集合に含まれることを保証 (未所持なら所持集合へ補完)
    - `[Inject]` 属性と `#nullable enable` を適用
  - [x] 5.3 家具加算の書き込みパスを実装する
    - `amount > 0` と Master 存在チェック、State 更新、変更通知 (同期発行) を行い購読者例外を吸収、最後に保存
    - 未知 ID・不正 amount は結果型でエラー、State は不変
  - [x] 5.4 着せ替え付与の書き込みパスを実装する
    - Master 存在チェック、未所持時のみ State 追加と変更通知、既所持は冪等に成功返却 (通知・保存は発火しない)
    - 未知 ID は結果型でエラー、State は不変
  - _Requirements: 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 6.1, 6.3, 7.1, 7.3, 7.4, 8.5, 8.6, 8.7, 8.8, 8.9_

- [ ]* 6. 所持アイテムサービスのユニットテストを追加する
  - 家具加算: 正常 / 未知 ID / `amount <= 0` / 0 未満減算要求 (将来の減算 API 想定)
  - 着せ替え付与: 正常 / 冪等 (既所持) / 未知 ID
  - Load 時に Master に存在しない ID が破棄される
  - Load 時に装備中着せ替えが所持集合へ補完される
  - 家具・着せ替え変更通知の発火と、購読者例外の隔離
  - PlayerPrefs 非互換バージョン時に空状態で初期化される
  - _Requirements: 1.3, 1.4, 2.2, 2.3, 2.4, 2.5, 3.2, 3.3, 3.4, 3.5, 3.6, 6.3, 7.3, 7.4_
  - _Note: Skipped — テスト インフラ (asmdef / Assembly-CSharp 参照) の整備が本 spec 外のため別タスクで対応_

- [x] 7. 両 Service/State を RootScope に Singleton で一括登録する
  - `UserPointState` と `UserItemInventoryState` を単独 Singleton で登録
  - `UserPointService` と `UserItemInventoryService` をインターフェースおよび自身の型で解決可能にする (`.As<I...>().AsSelf()`)
  - 既存登録との順序整合を保つ (PlayerPrefsService・UserEquippedOutfitService の後ろに配置)
  - RootScope の初期化で両 Service のコンストラクタが正常に解決され、Load が完走することを Unity エディタ上で確認
  - _Requirements: 1.1, 4.1, 8.1, 8.2, 8.3, 8.4, 8.5_
