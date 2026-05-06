# Gap Analysis — redecorate-furniture-type-tabs

## Summary
- **Feature**: `redecorate-furniture-type-tabs`
- **Discovery Scope**: Extension (既存 Redecorate UI への単一階層タブ機構の追加)
- **Key Findings**:
  - Closet 側で既に「2 段階タブ」が同型パターンで実装済み (`ClosetTabState` / `ClosetTabService` / `ClosetMinorTabsView` / `ClosetMinorTabItemView`)。本機能はこの「マイナータブ相当」だけを Redecorate 用に複製するスコープで設計できる。
  - Redecorate のフィルタ差し込み点は `RedecorateScrollerService.LoadData()` のループ内 1 箇所で完結し、既存の Base / Floor / Wall / Small 配置フローと `Selected` 復元 (`UpdateSelectionStates`) には変更不要。
  - 必要なアイコンアセットは `Assets/UI/Home/Closet/Textures/Icons/` に `makeovwr_tab_room_white.png` / `makeovwr_tab_floor_white.png` / `makeovwr_tab_accessories_white.png` / `makeovwr_tab_wall_white.png` の 4 枚が既に配置済みで、新規アセットは不要。
  - 既存 `TabItem.prefab` (Closet) は Closet 用に bind 済みなので Redecorate 側は別 Prefab (例: `RedecorateTabItem.prefab`) を新設するか、汎用化のために共通 `TabItem.prefab` を流用するかの設計判断が必要。

## Requirement-to-Asset Map

| 要件 | 既存アセット / 接続点 | ギャップタグ | 備考 |
|------|----------------------|-------------|------|
| R1 (タブ基本表示) | `RedecorateUiView` (拡張対象), `ClosetMinorTabsView` (流儀参考) | Missing (View 新規) | UI 部材の親 Transform を View に追加する |
| R1.4 (列挙宣言順) | `Cat.Furniture.FurnitureType` enum | Constraint | `Base=1, Floor=2, Small=3, Wall=4` を SSOT として参照 |
| R2 (FurnitureType マッピング) | enum 値そのものが SSOT | (ギャップなし) | Closet と異なり多対1マッピング不要 (1 タブ ↔ 1 type) |
| R3 (デフォルト Floor) | `ClosetTabState.DefaultMinor` (流儀参考) | Missing (State 新規) | `RedecorateTabState.Default = FurnitureType.Floor` |
| R3.3 (再オープンでリセット) | `ClosetScrollerService.OnOpen` の `ResetToDefault()` 流儀 | Constraint | 同パターンを `RedecorateScrollerService` に組込む |
| R4.1 (タブ切替フィルタ) | `RedecorateScrollerService.LoadData()` ループ内のフィルタ追加 | Constraint | `furniture.FurnitureType != current` をスキップ |
| R4.2 (スクロール位置リセット) | `EnhancedScroller.ReloadData(0f)` | (ギャップなし) | Closet と同様 |
| R4.3 (空状態) | `_data.Count == 0` で `GetNumberOfCells = 1` (ダミー行のみ) | (ギャップなし) | 自然成立 |
| R4.4 (連続選択時の再構築抑止) | `ClosetTabService` の差分のみ書込パターン | Missing (Service 新規) | 同パターンを Redecorate 側にコピー |
| R5.1 (Floor/Small/Wall タップで PlaceFurniture) | `RedecorateScrollerService.OnCellViewSelected` 既存実装 | (ギャップなし) | 変更不要 |
| R5.2 (Base タップで PlaceBase) | 同上 既存の Base 分岐 | (ギャップなし) | 変更不要 |
| R5.3 (Selected 判定の維持) | `RedecorateScrollerService.UpdateSelectionStates` | (ギャップなし) | フィルタ後の `_data` に対しても同関数で判定可能 |
| R5.4 (副作用排除) | 既存 State (IsoGridState/RoomBaseState/UserState) への書込み無し | (ギャップなし) | フィルタは読み取り専用 |
| R5.5 (Tiny 化破壊禁止) | `RedecorateTinyService` は `OnOpen` で `ResetTiny()` のみ | Constraint | タブ変更フックは Tiny に触れない設計とする |
| R6.1〜R6.4 (視覚) | `dressup_tab_open.png` / `dressup_tab_close.png` (背景), `Icons/makeovwr_tab_*` (アイコン) | (ギャップなし) | 4 枚のアイコンは Redecorate 専用で命名済 |
| R6.5 (Closet と整合) | `ClosetMinorTabItemView` の構造 | Constraint | 単一 Image の表示色制御 (Color.white / Color.clear) を踏襲 |
| R7.1〜R7.6 (規約) | `.kiro/steering/structure.md`, `tech.md` | Constraint | コーディング規約遵守 |

## Existing Code Inventory

### 流用 / 参考対象 (Closet)
| 種類 | パス | 役割 | 備考 |
|------|------|------|------|
| State | `Assets/Scripts/Home/State/ClosetTabState.cs` | 大 / 小タブ選択保持 + マッピング | Redecorate 側は `MajorTab` 相当を持たないため簡素化 |
| Service | `Assets/Scripts/Home/Service/ClosetTabService.cs` | 選択遷移 + デフォルト復帰 | 同型を `RedecorateTabService` として複製 |
| View | `Assets/Scripts/Home/View/ClosetMinorTabsView.cs` | 動的構築コンテナ | Redecorate は固定 4 タブのため `ClosetMajorTabsView` (静的) に近い形式が候補 |
| View | `Assets/Scripts/Home/View/ClosetMinorTabItemView.cs` | アイコン + 背景表示切替 | そのまま流儀流用可 (Color.white/clear) |
| View | `Assets/Scripts/Home/View/ClosetMajorTabsView.cs` | 静的バインド (体/服) | 4 タブ静的列挙パターンの参考 |

### 拡張対象 (Redecorate)
| 種類 | パス | 必要な変更 |
|------|------|------------|
| View | `Assets/Scripts/Home/View/RedecorateUiView.cs` | `RedecorateTabsView` への SerializeField 参照と `Init` での Bind を追加 |
| Service | `Assets/Scripts/Home/Service/RedecorateScrollerService.cs` | `RedecorateTabState.Changed` を購読、`LoadData` 内に `furniture.FurnitureType != current` フィルタ、`OnOpen` で `ResetToDefault` 呼出 |
| Scope | `Assets/Scripts/Home/Scope/HomeScope.cs` | `RedecorateTabState` (Scoped) と `RedecorateTabService` (Scoped) を `builder.Register` |

### 既存アセット利用可否
| アセット | 想定用途 | 確認結果 |
|----------|---------|---------|
| `Assets/UI/Home/Closet/Textures/Icons/makeovwr_tab_room_white.png` | Base タブ アイコン | 既存・流用可 |
| `Assets/UI/Home/Closet/Textures/Icons/makeovwr_tab_floor_white.png` | Floor タブ アイコン | 既存・流用可 |
| `Assets/UI/Home/Closet/Textures/Icons/makeovwr_tab_accessories_white.png` | Small タブ アイコン | 既存・流用可 |
| `Assets/UI/Home/Closet/Textures/Icons/makeovwr_tab_wall_white.png` | Wall タブ アイコン | 既存・流用可 |
| `Assets/UI/Home/Closet/Textures/dressup_tab_open.png` | 選択中背景 | 既存・流用可 |
| `Assets/UI/Home/Closet/Textures/dressup_tab_close.png` | 非選択背景 (透過運用なら不要) | 既存・流用可 |
| `Assets/UI/Home/Closet/Prefabs/TabItem.prefab` | タブ 1 個分の Prefab 候補 | Closet 配線済 (`ClosetMinorTabItemView` が付与) のため、Redecorate 専用 Prefab 別出しが安全 |

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| **A. Closet パターン複製 (推奨)** | `RedecorateTabState` + `RedecorateTabService` + `RedecorateTabsView` + `RedecorateTabItemView` を新設し、`ClosetMinorTab*` の構造を Redecorate 側にコピー | 既存依存方向 (View→Service→State) を厳密遵守、テスト容易、Closet 規約と一貫 | 新規ファイル 4 件 + 1 Prefab | R7.1〜R7.6 を素直に満たす |
| B. Closet タブ機構を共通化 | `TabState<T>` / `TabService<T>` を generic で抽出し Closet と Redecorate で共用 | コード重複削減 | リファクタが Closet 側に波及、本機能の最小変更原則 (R7.4) と相反 | 採用すると Closet 側を再 QA する必要があり、本 spec の範囲外として非推奨 |
| C. RedecorateScrollerService に統合 | タブ状態を Service 内 private field で持ち、View はコールバックを直接渡す | 追加クラス 0 件 | View → Service → State の単方向依存違反、テスト性低下、要件 R7.2 違反 | 不採用 |
| D. 動的構築 vs 静的 SerializeField | (A 内のサブ判断) `ClosetMinorTabsView` は動的 Instantiate、`ClosetMajorTabsView` は静的 4 SerializedField。本機能は固定 4 タブ。 | 静的: Inspector で参照明示・型安全 / 動的: enum 追加に強い | 静的: enum 追加で View 側修正必須 / 動的: アイコンマッピング (`IconEntry[]`) の二重保守 | 推奨は **静的 SerializeField (4 タブ固定)**。`FurnitureType` の追加は仕様変更とみなす |

## Implementation Approach Decisions (草案)

> 設計フェーズで最終確定する候補。

### Decision Candidates

1. **タブ状態を `Home.State.RedecorateTabState` に集約**
   - 役割: 選択中 `FurnitureType`、`UnityEvent<FurnitureType> Changed`、`Default = FurnitureType.Floor` を保持
   - 代替案: `HomeState` 拡張 → 粒度違いで不適 / View 内 private → 依存方向違反

2. **遷移ロジックを `Home.Service.RedecorateTabService` に集約**
   - API: `Select(FurnitureType)`, `ResetToDefault()`
   - 差分書込のみ Invoke (R4.4 を担保)
   - 代替案: View 直接書換 → 依存方向違反

3. **フィルタ差し込みは `RedecorateScrollerService.LoadData()` のループ内**
   - `_userState.UserFurnitures` を走査する既存ループに `furniture.FurnitureType != _redecorateTabState.Current` でスキップ条件を追加
   - `_redecorateTabState.Changed` 購読時は `LoadData()` を呼び `Scroller.ReloadData(0f)` でスクロール位置リセット
   - `OnOpen` 時に `_redecorateTabService.ResetToDefault()` を実行 (Closet と同じ `_suppressReload` ガードで二重実行抑止)
   - 代替案: 別 Service に分離 → 既存責務分割で複雑化

4. **タブビューは静的 4 SerializeField (`ClosetMajorTabsView` 流儀)**
   - `RedecorateTabsView` は `RedecorateTabItemView _baseTabItem`, `_floorTabItem`, `_smallTabItem`, `_wallTabItem` を SerializeField
   - 各 `Bind` で `RedecorateTabService.Select(<FurnitureType>)` を結線
   - 代替案: 動的 (`ClosetMinorTabsView` 流儀) → 4 件固定では過剰、Inspector で参照明示できる利点を喪失

5. **タブアイテムは `ClosetMinorTabItemView` 流儀 (背景 Image の表示切替)**
   - 単一 Image の `color = white | clear` で選択切替 (R6.5)
   - 代替案: `ClosetMajorTabItemView` 流儀 (Sprite 切替) → 選択中アイコン色が現状未準備のため不適

6. **DI 統合は `HomeScope` に 2 行追加**
   - `builder.Register<RedecorateTabState>(Lifetime.Scoped);`
   - `builder.Register<RedecorateTabService>(Lifetime.Scoped);`
   - View はシリアライズ参照経由 (`RedecorateUiView` の SerializeField → `Init` でコンテナ View に bind)。新規 `RegisterComponent` は不要。

## Risks & Mitigations

- **リスク R-1**: Tiny 状態 (`RedecorateUiView.IsTiny`) と タブ切替の干渉
  - 影響: タブ切替で `LoadData` を呼んだ際に Tiny を予期せずリセットしてしまう
  - 緩和: `RedecorateTabService.Select` 経路から Tiny に触れない / `RedecorateTinyService` 側のロジックは `OnOpen` イベントの購読だけに限定する (現状維持)。タブ変更経路では `_tinyAnimator` を呼ばない。

- **リスク R-2**: `OnOpen` 時の `ResetToDefault` と `LoadData` の二重実行
  - 影響: 不要な `ReloadData` で 2 回再構築される / リスナー登録漏れ
  - 緩和: Closet の `_suppressMinorReload` パターンを `RedecorateScrollerService` 側に複製 (`_suppressTabReload` フラグ)。`OnOpen` ハンドラ内で `ResetToDefault` を try/finally で囲み、その直後に明示的に `LoadData` を呼ぶ。

- **リスク R-3**: Base 配置の特殊フローと再構築タイミング
  - 影響: `Base` タブ表示中に Base 家具を配置した場合、`UpdateSelectionStates` が `RoomBaseState` ベースで判定するため期待通りに動くか要確認
  - 緩和: `LoadData` 末尾で `UpdateSelectionStates()` を呼ぶ既存パターンを踏襲。Base タブ ⇄ Floor/Wall/Small タブ切替時にも `UpdateSelectionStates` が一貫して動作することを E2E で確認 (Design Phase で動作シーケンスを明文化)。

- **リスク R-4**: タブ Prefab の重複 (Closet `TabItem.prefab` を再利用すると Closet スクリプトが付くため非互換)
  - 影響: Closet 用の `ClosetMinorTabItemView` が Redecorate プレハブに残る
  - 緩和: Redecorate 専用に新規 Prefab `RedecorateTabItem.prefab` を作成 (Closet `TabItem.prefab` から派生コピー)。スクリプトのみ `RedecorateTabItemView` に差し替える。

- **リスク R-5**: 既定タブを Floor とするか他タイプとするかの確定がまだ
  - 影響: 仕様レビューで覆る場合、`RedecorateTabState.Default` 値の差し替えのみで対応可能 (1 行)
  - 緩和: 設計フェーズで再確認。要件 R3.1 / R3.3 は `Default` 値の差し替えに対し透過的。

- **リスク R-6**: `FurnitureType` enum に将来 5 番目が追加された場合
  - 影響: 静的 4 SerializeField 型のタブビューは破綻する
  - 緩和: 本 spec 範囲外として明記。追加発生時に動的構築 (`ClosetMinorTabsView` 流儀) へ移行する手順を `research.md` の Follow-up に残す。

## Research Needed (Design Phase へ持ち越し)

1. タブ表示位置の最終決定: サンプル画像 (`RedecorateSampleImage.png`) はグリッド上部にタブ + Tiny ボタン同居の構図に見える。`RedecorateUiView` の既存 Tiny ボタン (`_tinyButton`) との Z-order・レイアウトの関係を Inspector で確認 (Design Phase)。
2. 既定タブを Floor 以外 (Base 等) にする可能性。要件 3.1 が固定値前提のため、早期合意推奨。
3. `RedecorateTabItem.prefab` の新規作成是非と、Closet `TabItem.prefab` から複製するか、最小構成 (`Image` + `Button` + `Image(icon)`) で新規構築するかの判断。
4. Tab 並び (`Base` → `Floor` → `Small` → `Wall`) を enum 順とサンプル画像の表示順がズレた場合の優先度 (要件は enum 順を明文化済み)。

## Implementation Complexity & Risk

- **Effort**: **S (1〜3 日)**
  - 既存 Closet タブパターンを 1:1 で複製でき、既存 `RedecorateScrollerService` への侵襲は購読 + フィルタ条件 1 行のみで済むため最小。
  - Prefab + Inspector 配線が含まれる前提でも 3 日に収まる見込み。

- **Risk**: **Low**
  - 既存パターンの拡張、依存方向違反なし、外部依存追加なし。
  - 主要リスクは Tiny / Base 配置との相互作用 (R-1, R-3) で、いずれも既存コード読解で対処可能な範囲。

## Recommendations for Design Phase

- **Preferred Approach**: Option A (Closet パターン複製) + 静的 4 SerializeField タブコンテナ
- **主要決定事項**:
  1. `Home.State.RedecorateTabState` (新規) — 選択中 `FurnitureType` + 既定値 + 変更通知
  2. `Home.Service.RedecorateTabService` (新規) — `Select` / `ResetToDefault` API
  3. `Home.View.RedecorateTabsView` (新規) — 4 SerializeField 静的バインド
  4. `Home.View.RedecorateTabItemView` (新規) — 単一 Image の色切替で選択表示
  5. `RedecorateUiView` 拡張 — SerializeField 追加 + `Init` で Bind
  6. `RedecorateScrollerService` 拡張 — 購読 + フィルタ + `OnOpen` での `ResetToDefault`
  7. `HomeScope` に 2 行追加 (State + Service の Register)
- **Carry-forward Research**:
  - 既定タブの最終確定 (Floor を案として要承認)
  - サンプル画像と一致するレイアウト構成 (タブ位置 / Tiny ボタンとの共存) の Prefab 設計
  - 新規 `RedecorateTabItem.prefab` の最小構成 (or 既存 `TabItem.prefab` 複製) の判断

## References
- `.kiro/steering/closet.md` — 拡張時の参照点 (タブ実装ガイド §299, §304)
- `.kiro/steering/structure.md` / `tech.md` — 層構成・コーディング規約
- `Assets/Arts/Furniture/Scripts/Furniture.cs` — `FurnitureType` enum 定義
- `Assets/Scripts/Home/View/ClosetMinorTabsView.cs` — 動的タブコンテナの参考実装
- `Assets/Scripts/Home/View/ClosetMajorTabsView.cs` — 静的タブコンテナの参考実装
- `Assets/Scripts/Home/State/ClosetTabState.cs` — タブ状態 SSOT の参考実装
- `Assets/Scripts/Home/Service/ClosetTabService.cs` — タブ遷移 Service の参考実装
- `Assets/Scripts/Home/Service/RedecorateScrollerService.cs` — フィルタ差し込み対象
- `Assets/UI/Home/Closet/Textures/Icons/makeovwr_tab_*.png` — Redecorate 用既存アイコン 4 種
