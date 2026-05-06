# Implementation Plan — redecorate-furniture-type-tabs

> 本タスク一覧は `requirements.md` / `design.md` / `research.md` を前提に、Closet マイナータブ流儀の単一階層タブを Redecorate 画面に追加する実装手順を定義する。
>
> **要件 ID 形式**: `N.M` (N = Requirement 番号、M = Acceptance Criteria 番号)。

## Branches

**Base**: `feature/redecorate-furniture-type-tabs`

機能サイズは S (1〜3 日)、依存ファイルが少なく単一 PR で完結可能なため、ベースブランチのみで全タスクを実装し、`main` へ直接 PR する。

## Multi-Agent 実行ガイド

`design.md` "Multi-Agent Implementation Notes" に基づく並列起動方針:

- **並列起動可能 (`(P)` マーカー)**: 新規ファイル単独作成タスク。`Agent(subagent_type=general-purpose, run_in_background=true)` で同時投入する。
- **シーケンシャル (マーカーなし)**: 既存ファイル編集 / 型依存 / DI 解決を伴うタスク。メインエージェントが順次 `Edit` / `Write` で処理する。
- **Editor / UnityMCP 担当**: Inspector 配線・Prefab 操作。Agent への分割は行わず、UnityMCP が利用可能な場合は半自動化、不可なら人手で実施。

## Tasks

### Branch: `feature/redecorate-furniture-type-tabs`

- [ ] 1. タブ State / Item View / Prefab 雛形を準備する
  - [x] 1.1 (P) 選択中 `FurnitureType` を保持する State クラスを新設する **[サブエージェント並列起動可能 — Bundle A]**
    - 単一フィールド `FurnitureType _current` と `UnityEvent<FurnitureType> Changed` を保持
    - クラス定数 `Default = FurnitureType.Floor` で初期化し、`Current` が常に `FurnitureType` 列挙の有効値であることを保証
    - `WriteCurrent(value)` は同値時 no-op、差分時のみ `Changed` を 1 回発火 (連続選択時の再構築抑止)
    - 永続化なし、書込窓口は新設の Tab Service のみ (運用契約)
    - 配置先: `Assets/Scripts/Home/State/`
    - 起動方法: `Agent(subagent_type=general-purpose, run_in_background=true)` で Bundle B と同時投入
  - [x] 1.2 (P) タブ 1 個分の View (アイコン + 背景色切替) を新設する **[サブエージェント並列起動可能 — Bundle B]**
    - `Button` / 背景 `Image` / アイコン `Image` の 3 SerializeField 構成
    - `Bind(Sprite icon, UnityAction onClick)` でアイコン割当 + クリック結線
    - `SetSelected(bool)` で背景色を `Color.white` / `Color.clear` 切替 (sprite 差替は行わない)
    - `OnDestroy` で `Button.onClick.RemoveAllListeners()`
    - Closet `ClosetMinorTabItemView` の構造を流用
    - 配置先: `Assets/Scripts/Home/View/`
    - 起動方法: Bundle A と同時並列で別 Agent 投入
  - [x] 1.3 タブ Item の Prefab を新設する **[Bundle C — Bundle B 完了後]**
    - Closet `Assets/UI/Home/Closet/Prefabs/TabItem.prefab` から派生コピーし `Assets/UI/Home/Redecorate/Prefabs/RedecorateTabItem.prefab` を作成
    - 付与スクリプトを `RedecorateTabItemView` (1.2 で新設) に差し替え (Closet との互換性は保持しない)
    - 構造: ルート (RectTransform + Button + 背景 Image + 新 View) / 子 Icon (RectTransform + Image)
    - 背景 Image: Raycast Target=true、初期 color=Color.clear
    - アイコン Image: Raycast Target=false、Preserve Aspect=true
    - 担当: UnityMCP (利用可能時) または人手で Editor 操作
  - _Requirements: 1.3, 1.4, 3.1, 3.4, 4.4, 6.1, 6.2, 6.5, 7.1, 7.5_

- [x] 2. タブ遷移 Service を新設し DI に登録する
  - [x] 2.1 タブ遷移 Service (`Select` / `ResetToDefault`) を新設する
    - コンストラクタで Tab State を `[Inject]` 受け取り
    - `Select(FurnitureType type)` は State の `WriteCurrent(type)` に委譲 (差分判定は State 側)
    - `ResetToDefault()` は `WriteCurrent(Default)` を呼ぶ (再オープン時のリセット用)
    - `IsoGridState` / `RoomBaseState` / `UserState` / `FurniturePlacementService` への書込みを行わない (副作用排除)
    - 配置先: `Assets/Scripts/Home/Service/`
    - 1.1 完了後に着手 (Tab State 型を参照するため)
  - [x] 2.2 `HomeScope` に Tab State + Tab Service を登録する
    - `builder.Register<{TabState}>(Lifetime.Scoped)`
    - `builder.Register<{TabService}>(Lifetime.Scoped)`
    - 既存の State → Service → EntryPoint グルーピング順を維持
    - View 系は `RedecorateUiView` の SerializeField 経由で参照されるため、追加の `RegisterComponent` は不要
  - _Requirements: 1.3, 3.1, 4.4, 5.4, 7.1, 7.3, 7.5_

- [x] 3. タブコンテナ View を新設し画面ルートとの bind 経路を構築する
  - [x] 3.1 4 タブ静的バインドコンテナ View を新設する
    - 4 件の `RedecorateTabItemView` を SerializeField で静的保持 (`_baseTabItem` / `_floorTabItem` / `_smallTabItem` / `_wallTabItem`)
    - `Bind(TabService, TabState)` で各 Item に Sprite とクリックハンドラ (`Service.Select(<FurnitureType>)`) を結線
    - `TabState.Changed` を購読し、4 件の `SetSelected(bool)` を一括同期 (選択中のみ true)
    - `Bind` 冒頭で `SetSiblingIndex(0..3)` を `Base → Floor → Small → Wall` 順で強制 (Inspector 配線ぶれ保険)
    - 初回バインド時に `OnChanged(_state.Current)` を 1 回呼んで初期描画を同期
    - `OnDestroy` で `Changed` listener を解除
    - 配置先: `Assets/Scripts/Home/View/` (Closet `ClosetMajorTabsView` 流儀の静的バインド)
    - 1.1, 1.2, 2.1 完了後に着手 (3 者の型を同時参照)
  - [x] 3.2 `RedecorateUiView` に SerializeField とバインド経路を追加する
    - `[SerializeField] {TabsView} _tabsView` を 1 件追加
    - 既存 `Init` メソッドに Tab Service / Tab State の `[Inject]` 引数 2 件を追加
    - `_tabsView is null` のとき `Debug.LogError` でログ出力し、処理は継続 (Closet 流儀)
    - 既存の戻るボタン結線、Tiny 機能、`UiView` 由来の `OnOpen` 機構には変更を加えない
    - 3.1 完了後に着手 (TabsView 型を参照)
  - _Requirements: 1.1, 1.2, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.4, 6.3, 6.4, 7.1, 7.2, 7.3, 7.5, 7.6_

- [x] 4. `RedecorateScrollerService` にタブ購読・フィルタ・既定値復帰を組み込む
  - [x] 4.1 Tab State.Changed の購読と OnTabChanged ハンドラを実装する
    - コンストラクタに Tab State / Tab Service の `[Inject]` 引数を追加
    - `Start()` で `TabState.Changed.AddListener(OnTabChanged)` を 1 回登録
    - `OnTabChanged` のガード 3 段: `_suppressTabReload` / `_redecorateUiView.gameObject.activeInHierarchy` / `_furnitureAssetState.IsLoaded` をすべて満たす場合のみ `LoadData()` 実行
    - 既存 `OnCellViewSelected` の Base / Floor / Wall / Small 分岐 + Tiny 化処理には一切手を入れない
    - `IsoGridState` / `RoomBaseState` / `UserState` / `FurniturePlacementService` への書込みを追加しない
  - [x] 4.2 `LoadData()` のループ内に `FurnitureType` フィルタを差し込む
    - `_userState.UserFurnitures` を走査するループ内に `if (furniture.FurnitureType != _tabState.Current) continue;` を 1 行追加
    - 既存 `UpdateSelectionStates()` はフィルタ後 `_data` に対し従来どおり呼ぶ (Selected 復元の動線維持)
    - `LoadData()` 末尾の `Scroller.ReloadData()` を `Scroller.ReloadData(0f)` に置換 (タブ切替時のスクロール先頭リセット)
    - 既存の `SelectedChanged` listener 解除ループは維持し、フィルタ前の旧データに対して呼ぶ動線を変えない
    - 空状態 (`_data.Count == 0`) は既存の `GetNumberOfCells = 1` (ダミー行) で自然成立
  - [x] 4.3 `OnOpen` 経路を新設し既定値復帰 → Initialize を一括実行する
    - 旧 `_redecorateUiView.OnOpen.AddListener(Initialize)` を `OnOpen` ハンドラ購読に置換 (二重呼出防止)
    - `OnOpen` 内で `_suppressTabReload = true` を try/finally で立て、その間に `TabService.ResetToDefault()` を呼んで `OnTabChanged` の空走を抑止
    - finally 抜け後に既存 `Initialize()` を呼ぶ (Asset 未ロード時は既存の `OnLoaded += LoadData` 経路に乗る)
    - 同一未ロード window で Tab Changed が来た場合は 4.1 のガードで no-op となり、Asset ロード完了後の `LoadData` で `_tabState.Current` を反映する単一経路に収束
  - _Requirements: 3.2, 3.3, 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 5.3, 5.4, 5.5, 7.1, 7.4, 7.5_

- [ ] 5. Inspector 配線と Editor Play E2E 検証を行う **[人手 / UnityMCP 担当]**
  - [ ] 5.1 Prefab・SerializeField・Hierarchy を配線する
    - `RedecorateTabItem.prefab`: ルート Image (背景) と Icon 子 Image が SerializeField の `_backgroundImage` / `_iconImage` に参照されているか確認
    - 背景 Image: Raycast Target=true、初期 color=Color.clear (alpha=0 でもクリック判定維持)
    - アイコン Image: Raycast Target=false、Preserve Aspect=true
    - `RedecorateTabsView` Prefab 内で 4 件の `RedecorateTabItemView` GameObject の sibling 順序が `Base` → `Floor` → `Small` → `Wall` であることを目視確認 (コード側 `SetSiblingIndex` でも保険)
    - 4 タブ Sprite に既存アセットを割当: Base = `makeovwr_tab_room_white.png` / Floor = `makeovwr_tab_floor_white.png` / Small = `makeovwr_tab_accessories_white.png` / Wall = `makeovwr_tab_wall_white.png` (新規アセット追加禁止)
    - `RedecorateUiView._tabsView` フィールドが `RedecorateTabsView` を参照していること確認
    - 担当: UnityMCP 利用可能時は MCP 経由、不可なら Editor 手動操作
  - [ ] 5.2 Editor Play モードで E2E シナリオを実機検証する
    - 既定タブ表示: Home → Redecorate を開き、Floor タブが選択中 + Floor 家具のみグリッド表示されること
    - タブ切替フィルタ: Base / Floor / Small / Wall を順にタップし、各 `FurnitureType` の家具のみが描画されかつスクロールが先頭に戻ること
    - 空状態: 所持していない `FurnitureType` のタブを選んだ際、グリッドが空 (ダミー行のみ) になること
    - 再オープンリセット: タブを Wall に切替 → 戻る → 再度開いて Floor に戻っていること
    - Base 配置整合: Base タブ選択 → Base 家具タップで部屋が切替わり、`RoomBaseState.PlacedBaseUserFurnitureId` 由来の Selected が反映されること
    - Tiny 化非干渉: Tiny 中にタブ切替を行っても Tiny が解除されず、切替後も Tiny 操作が引き続き動作すること
    - Changed 発火回数: 同一タブ再タップ 0 / 異タブタップ 1 / `OnOpen` 0〜1 (Default 一致時 0、変化時 1) を Inspector または `Debug.Log` で確認
    - フィルタ後セル数 = `_userState.UserFurnitures.Count(f => f.FurnitureType == current)` と一致すること
    - 副作用排除: `IsoGridState` / `RoomBaseState` / `UserState.UserFurnitures` がタブ操作前後で完全一致 (タブ操作で State が変化しないこと)
    - Visual: 4 タブの非選択時 (背景 Color.clear) / 選択時 (Color.white) が `ClosetMinorTabItemView` と整合していること
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4, 6.5, 7.6_

## Requirements Coverage Summary

| Requirement | カバータスク |
|-------------|-------------|
| 1.1, 1.5 | 3.2, 5.1, 5.2 |
| 1.2, 1.4, 2.1〜2.6 | 3.1, 5.2 |
| 1.3, 3.4, 4.4 | 1.1, 3.1, 4.1, 5.2 |
| 3.1 | 1.1, 5.2 |
| 3.2, 3.3 | 4.2, 4.3, 5.2 |
| 4.1, 4.2, 4.5 | 4.2, 5.2 |
| 4.3 | 4.2, 5.2 |
| 5.1, 5.2, 5.3 | 4.1, 4.2, 5.2 |
| 5.4 | 2.1, 4.1, 5.2 |
| 5.5 | 4.1, 5.2 |
| 6.1, 6.2, 6.5 | 1.2, 5.1, 5.2 |
| 6.3, 6.4 | 3.1, 5.1, 5.2 |
| 7.1〜7.6 | 1.1, 1.2, 2.1, 2.2, 3.1, 3.2, 4.1, 4.2, 4.3, 5.2 |

全 7 要件 (40 Acceptance Criteria) がいずれかのタスクでカバーされている。
