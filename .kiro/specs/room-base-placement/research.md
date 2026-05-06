# Research & Design Decisions

## Summary
- **Feature**: `room-base-placement`
- **Discovery Scope**: Extension (既存 Redecorate 系への追加)
- **Key Findings**:
  - `Cat.Furniture.Furniture.SceneObject` は `IsoDraggableView` 型で定義されている (`Assets/Arts/Furniture/Scripts/Furniture.cs:27`)。Base プレハブには `IsoDraggableView` がアタッチされていないため、このフィールドは Base 家具では再利用できない (`BaseA.asset` 上でも `SceneObject: {fileID: 0}`)。
  - 既存の `FurniturePlacementService.PlaceFurniture / PlaceFloorFurnitureAt / PlaceWallFurnitureAt / PlaceFragmentedFurnitureAt` はすべてインスタンス化直後に `IsoDraggableView` の API (`SetUserFurnitureId`, `SetPlacementType`, `FootprintSize`, `PivotGridPosition`, `SetPosition` 等) を呼ぶ前提で書かれている。`PlacementType.Base` をこの経路で扱うと NullReferenceException 級の不具合が発生する。
  - 配置先となる `RoomBackGround` GameObject は既に `Assets/Scenes/Home.unity` に存在する (line 3681、Transform のみ、子 `RoomObject` を保持)。命名は `RoomBackGround` (大文字 G) であり、要件書本文の `RoomBackground` 表記と差異がある。
  - 既存マスター (`Resources/furnitures.csv`) と `Resources/user_furnitures.csv` には Base 種別のエントリが存在しない。Base 家具の動作確認には CSV 拡張も必要。
  - 永続化系 (`IsoGridSaveService` / `IsoGridLoadService` / `UserState.IsoGridSaveData`) は Floor / LeftWall / RightWall / FragmentedGrids しか保存・復元しない。Base の選択状態を維持するには保存スキーマを拡張する必要がある。

## Research Log

### Topic: `Furniture` ScriptableObject の Base 家具対応
- **Context**: Base プレハブは `IsoDraggableView` を持たないが、Floor / Wall は持つ。1 つの `Furniture` 型でどう参照するか。
- **Sources Consulted**:
  - `Assets/Arts/Furniture/Scripts/Furniture.cs`
  - `Assets/Arts/Furniture/Furnitures/Base/BaseA/BaseA.asset` / `BaseA.prefab`
  - `Assets/Arts/Furniture/Furnitures/Floor/MidBox/MidBox.prefab`
- **Findings**:
  - Floor / Wall プレハブは root に `IsoDraggableView` を持つ MonoBehaviour 構成、Base プレハブは `Transform` + `SpriteRenderer` のみ。
  - 既存コード上、`furniture.SceneObject` に対する null チェックは存在する (`PlaceFurniture` 冒頭) ため、`SceneObject` を null のままにしても Floor / Wall 既存処理は壊れない。
  - Floor / Wall 既存メソッドのシグネチャを変更しないことが要件 (5.8) で求められている。
- **Implications**:
  - `Furniture` SO に Base 用の別フィールド (`BaseSceneObject`) を追加し、Floor / Wall は `SceneObject` (IsoDraggableView)、Base は `BaseSceneObject` (`Transform` または `GameObject`) を参照する二系統構成にする。型継承や interface による統一は今回スコープでは過剰設計と判断。

### Topic: `RoomBackGround` GameObject の参照経路
- **Context**: PlaceBase 経路で Base プレハブを `RoomBackGround` の子として配置する必要がある。シーン上の特定 GameObject をどのように DI 経由でサービス層へ供給するか。
- **Sources Consulted**:
  - `Assets/Scenes/Home.unity` (line 3671-3702)
  - `Assets/Scripts/Home/Scope/HomeScope.cs`
  - 既存 View 層登録パターン (`CharacterView`, `IsoGridSettingsView` 等)
- **Findings**:
  - `RoomBackGround` には現状 MonoBehaviour が一切付与されていない。
  - `HomeScope` は SerializeField で View / 設定オブジェクトを受け取り `RegisterComponent` / `RegisterInstance` で DI している (`CharacterView`, `IsoGridSettingsView` など)。
  - `IsoGridService` は `_isoGridSettingsView.gameObject.scene` を Home シーン参照として活用しており、シーン階層への参照は MonoBehaviour 経由が自然。
- **Implications**:
  - `Home.View.RoomBackGroundView : MonoBehaviour` を新設し、`RoomBackGround` GameObject にアタッチする。`HomeScope` は `[SerializeField] RoomBackGroundView _roomBackGroundView` として参照し `RegisterComponent` する。

### Topic: 単一選択の状態管理 (Floor/Wall とは別の保持先)
- **Context**: Base は IsoGrid のセルを占有しないため `IsoGridState` には保持できない。一方で「現在配置中の Base」は単一選択排他のため一意に保持する必要がある。
- **Sources Consulted**:
  - `Assets/Scripts/Home/State/IsoGridState.cs`
  - `Assets/Scripts/Home/Service/FurniturePlacementService.cs` (`RemoveFurniture` で `FindObjectsByType<IsoDraggableView>` 経由で検索する設計)
- **Findings**:
  - 既存の Floor/Wall 用は GameObject の検索を `FindObjectsByType<IsoDraggableView>` に依存しており、Base ではこの方式は使えない。
  - `RoomBackGround` の子は (デフォルトで存在する `RoomObject` を除き) Base 家具のみ、という運用ルールにすれば、子の探索のみで現在の Base GameObject を特定可能。
- **Implications**:
  - `Home.State.RoomBaseState` を新設し、現在配置中の `userFurnitureId` のみを保持する単純な State として運用する。GameObject 参照は state に持たせず、`RoomBackGroundView.transform` 配下から探索する。

### Topic: 起動時のデフォルト Base 配置
- **Context**: Home シーン初期化完了時に、最初に所持している Base を自動配置しなければならない (要件 3)。
- **Sources Consulted**:
  - `Assets/Scripts/Home/Starter/HomeStarter.cs` (Outfit 適用時の参考パターン)
  - `Assets/Scripts/Home/Starter/FurnitureAssetStarter.cs`
  - `Assets/Scripts/Home/Service/IsoGridLoadService.cs` (`FurnitureAssetState.OnLoaded` 待ちの実装)
- **Findings**:
  - 既存 Starter は `IStartable.Start` 内で `FurnitureAssetState.IsLoaded` をチェックし、未ロード時は `OnLoaded` を購読するパターンが確立している。
  - 永続化からの復元 (`IsoGridLoadService`) も同じパターンに従っている。
- **Implications** (revised after design review):
  - `Home.Service.RoomBaseDefaultService` を新設 (`IStartable` ではなく通常 Service)。`FurnitureAssetState.OnLoaded` を直接購読せず、`IsoGridLoadService.Load` の最末尾で `ApplyDefaultIfNeeded()` を同期呼び出しする post-load フック方式に確定。これにより Multicast Delegate の購読順や VContainer の `RegisterEntryPoint` 順序への依存を排除する。

### Topic: 保存・復元への Base 反映
- **Context**: 単一選択状態を Redecorate 終了時に保存し、再起動時に復元したい。
- **Sources Consulted**:
  - `Assets/Scripts/Home/Service/IsoGridSaveService.cs`
  - `Assets/Scripts/Home/Service/IsoGridLoadService.cs`
  - `Assets/Scripts/Root/State/UserState.cs` (`IsoGridSaveData` 構造)
- **Findings**:
  - `IsoGridSaveData` は JsonUtility で `PlayerPrefs` に直列化されている。
  - JsonUtility は nullable 値型を直列化しないため、未配置を表す sentinel (例: `-1`) または bool フラグ併用が必要。
  - 既存スキーマに新フィールドを追加しても、JsonUtility は欠損時はゼロ値で復元するため後方互換は保てる (古いセーブデータは `BaseUserFurnitureId == 0` または `-1` 扱い)。
- **Implications**:
  - `IsoGridSaveData` に `int BaseUserFurnitureId`(初期値 -1) を追加。Save 時に `RoomBaseState.PlacedBaseUserFurnitureId` を書き出し、Load 時に値が有効ならそれを使う、無効ならデフォルト Base 選択ロジックへフォールバックする。

### Topic: RedecorateScrollerService の選択状態判定
- **Context**: `UpdateSelectionStates` は `IsoGridState.EnumerateAllGrids()` 経由で配置中判定をしているが、Base はどこにも入っていない。
- **Sources Consulted**:
  - `Assets/Scripts/Home/Service/RedecorateScrollerService.cs`
- **Findings**:
  - 現状 `data.Selected = _isoGridState.EnumerateAllGrids().Any(g => g.ObjectPositions.ContainsKey(data.UserFurnitureId))` で判定。
  - `OnCellViewSelected` も `selectedData.Selected` で配置済みかを判定し、未配置なら `PlaceFurniture` を呼んでいる。
- **Implications**:
  - `UpdateSelectionStates` を拡張し、`PlacementType.Base` のデータは `RoomBaseState.PlacedBaseUserFurnitureId == data.UserFurnitureId` で判定する。
  - `OnCellViewSelected` に Base 専用分岐を追加し、`FurniturePlacementService.PlaceBase` を呼び出す。既選択 Base の再タップは何もしない (要件 4.3)。

### Topic: マスターデータ・ユーザーデータの Base 対応
- **Context**: 動作検証のため Base 家具を所持しているユーザー状態が必要。
- **Sources Consulted**:
  - `Assets/Resources/furnitures.csv`
  - `Assets/Resources/user_furnitures.csv`
  - `Assets/Scripts/Root/Service/MasterDataImportService.cs`
- **Findings**:
  - `furnitures.csv` には Floor 3 件 + Wall 1 件のみで Base なし。
  - `user_furnitures.csv` も同様に Base 種別なし。
  - `MasterDataImportService` は `furnitures.csv` の `id, type, name` を読み込む。`type` は string で、コード上は単に保持しているだけ (Furniture.FurnitureType 列挙との直接マッピングは Asset 側で別途実施)。
- **Implications**:
  - 動作確認時に `furnitures.csv` に `5,Base,BaseA` を追加、`user_furnitures.csv` に Base 所持エントリを追加する必要がある。これはデータ作業であり本要件のコード変更ではないが、設計時に手順として記載する。

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| 単一サービス + 別 SO フィールド (採用) | 既存 `FurniturePlacementService` に `PlaceBase` を追加、`Furniture` に `BaseSceneObject` を追加 | 既存パターンを踏襲。Floor/Wall 経路と物理的に分離されており既存実装に影響しない。 | `Furniture` SO に未使用フィールドが混在 (Floor/Wall は `BaseSceneObject` 不要、Base は `SceneObject` 不要) | 要件 5.1 と 5.7 を直接満たす最短経路 |
| 別サービス分離 | `RoomBaseService` を新設し、`FurniturePlacementService` には触れない | 関心分離が明瞭 | 要件 5.1 が「`FurniturePlacementService` に `PlaceBase` を新設」と指定 | 要件と矛盾するため不採用 |
| `Furniture` を抽象化し `FloorWallFurniture` / `BaseFurniture` で分割 | 型レベルで Base と Floor/Wall を分ける | 型安全性が高い | マスター CSV 連携・Addressables 規約・既存 ScriptableObject 資産の作り直しが必要で範囲が膨れる | 採用しない (オーバーエンジニアリング) |

## Design Decisions

### Decision: `Furniture` SO への Base 用プレハブフィールド追加
- **Context**: `SceneObject : IsoDraggableView` は Base プレハブを参照できない (Base には IsoDraggableView がアタッチされない)。
- **Alternatives Considered**:
  1. `SceneObject` を `GameObject` 型に格下げ — 既存の Floor/Wall 経路が大幅に書き換わる。要件 5.8 に反する。
  2. 別途 Addressables のレジストリを持つ — 既存の `FurnitureAssetState` パターンと不整合。
  3. `Furniture` に `BaseSceneObject` フィールド (Transform 型) を追加 — 既存パターン維持で最小差分。
- **Selected Approach**: 案 3。`public Transform BaseSceneObject;` を `Furniture` に追加。`PlacementType` ごとにどのフィールドを使うかを `FurniturePlacementService` 側で振り分ける。
- **Rationale**: 要件 5.7 (PlaceBase は `SceneObject` (`IsoDraggableView` 型) に依存しない) を満たしつつ、Floor/Wall への影響をゼロにできる。
- **Trade-offs**: `Furniture` に未使用になりうるフィールドが混在する。代わりに既存 SO 資産 (`LargeBox.asset` 等) のシリアライズ互換性は保てる (新フィールドは null のまま)。
- **Follow-up**: BaseA.asset の `BaseSceneObject` に `BaseA.prefab` の Transform を割り当てる作業をタスクに含める。

### Decision: 現在配置中 Base は `RoomBaseState` に `userFurnitureId` のみ保持
- **Context**: Base GameObject の参照を State に持たせるか、毎回シーン階層から探索するか。
- **Alternatives Considered**:
  1. State に GameObject 参照を保持 — 破棄時の dangling 参照リスクと、State レイヤに Unity 依存型を持ち込む。
  2. State には `int? PlacedBaseUserFurnitureId` のみ、GameObject は `RoomBackGround` の子から取得 — 軽量、State は純粋データ。
- **Selected Approach**: 案 2。State は ID のみ。Service 側で `RoomBackGroundView.transform` の子を破棄/生成する。
- **Rationale**: 既存 `IsoGridState` も座標データのみを持つ純粋 State であり、パターン整合性が高い。
- **Trade-offs**: Service 側で「`RoomBackGround` の子は Base 家具インスタンスのみ」という暗黙の不変条件に依存する。設計でこの不変条件を明文化する。
- **Follow-up**: `RoomBackGround` 配下の既存子オブジェクト (`RoomObject`) と Base インスタンスが衝突しないよう、Base インスタンスは別の親 (`RoomBackGround` 直下の "Base" コンテナ Transform) に配置するか、子オブジェクトの命名規約で識別する選択肢がある。設計では `RoomBackGround` 直下に Base 用コンテナ Transform を切る案を採用する。

### Decision: デフォルト配置を担う `RoomBaseDefaultService` (post-load フック)
- **Context**: 起動時に必ず 1 つ Base を配置するロジックの所在。`RedecorateScrollerService.Initialize` は Closet/Redecorate を開いたタイミングで動くため、初期配置の責務には不適切。
- **Alternatives Considered**:
  1. `HomeStarter` に組み込む — Outfit 適用と Base 配置で関心が異なる。
  2. `IsoGridLoadService` 自体に組み込む — Load 経路と「デフォルト適用」の責務が混在し、テスト困難。
  3. 専用 `RoomBaseStarter : IStartable` を新設し `FurnitureAssetState.OnLoaded` を独自購読 — 当初案。デザインレビューで「`IsoGridLoadService` と `OnLoaded` 購読順が衝突するリスク (Multicast Delegate / VContainer 登録順依存)」を指摘された。
  4. 専用 `RoomBaseDefaultService` (通常 Service) を新設し、`IsoGridLoadService.Load` の最末尾で `ApplyDefaultIfNeeded()` を同期呼び出し — レビュー後の最終案。
- **Selected Approach**: 案 4。`RoomBaseDefaultService` は `IStartable` ではなく Service。`OnLoaded` は購読しない。呼び出しは `IsoGridLoadService.Load` から明示的に行う。
- **Rationale**: Multicast Delegate 購読順 (≒ VContainer 登録順) への依存を構造的に排除でき、起動時の単線フローでデバッグが容易。`RegisterEntryPoint` も不要で `Register` のみで済む。
- **Trade-offs**: `IsoGridLoadService` が `RoomBaseDefaultService` に直接依存する (結合は増えるが双方向ではない)。一方、デザインレビューで指摘された競合リスクの根を断てる利点が上回る。
- **Follow-up**: `IsoGridLoadService.Load` の最末尾で `_roomBaseDefaultService.ApplyDefaultIfNeeded()` を呼ぶこと、`saveData == null` の早期 return 経路でも必ず呼ばれるよう実装で担保する。

### Decision: 永続化は `IsoGridSaveData` を拡張
- **Context**: Base ID を別キーで PlayerPrefs に保存するか、既存セーブデータに含めるか。
- **Alternatives Considered**:
  1. 新たな PlayerPrefs キー (`PlayerPrefsKey.RoomBase`) を追加 — 同じ模様替え情報が分散。
  2. 既存 `IsoGridSaveData` にフィールド追加 — 一括保存で整合性高い。
- **Selected Approach**: 案 2。`IsoGridSaveData.BaseUserFurnitureId : int = -1` を追加。
- **Rationale**: 既存セーブ/ロード経路に乗せられ、Redecorate 終了時の `IsoGridSaveService.Save` に自然に統合できる。
- **Trade-offs**: スキーマ拡張だが、`-1` を sentinel として後方互換可能 (古いセーブは復元時に Base が `-1`/0 となり、デフォルト配置にフォールバックする)。
- **Follow-up**: `IsoGridSaveService` の `Save` ログメッセージにも Base ID を追記する。

## Risks & Mitigations
- **Risk: `RoomBackGround` 直下の既存 `RoomObject` を誤って破棄する** — Base 用の子コンテナ Transform (`Bases` など) を `RoomBackGround` 配下に新設し、Base インスタンスはこのコンテナ配下にのみ生成・破棄するルールにすることで隔離する。
- **Risk: マスター CSV / ユーザー所持 CSV に Base 種別がない状態で実装すると動作確認できない** — 設計フェーズで CSV 拡張手順を明示し、タスク化する。
- **Risk: 既存 `Furniture` ScriptableObject 資産 (LargeBox 等) の SerializedObject へ新フィールドを追加することによるエディタ上のメタ差分** — Unity が自動で `BaseSceneObject: {fileID: 0}` を追記するだけで、機能影響はない。レビュー時に変更ファイルが増えることをチームに告知する。
- **Risk: 起動時のロード順 (FurnitureAssetState 完了 → IsoGridLoadService Load → `ApplyDefaultIfNeeded` post-load フック) が崩れる** — `IsoGridLoadService.Load` の最末尾で同期呼び出しに統一し、`OnLoaded` の多重購読を避けることで構造的に解決した (デザインレビュー指摘 1 反映)。
- **Risk: `IsoGridLoadService.GetFurnitureAsset` が Base アセット (`SceneObject == null`) を弾く既存挙動と衝突する** — Base 復元を独立メソッド `LoadBaseObject` に分離し、`GetFurnitureAsset` には一切手を加えない方針に確定 (デザインレビュー指摘 2 反映)。
- **Risk: `RoomBackGroundView.BaseRoot` 配下に Base 以外の GameObject が混入すると `PlaceBase` 実行時に破棄される** — これは要件 2.2 / 4.5 を構造保証するための意図的な「全子破棄」設計。`[Header]` 注記とクラス doc コメント、設計書本文の三箇所で運用ルール (BaseRoot 配下は Base 専用) を明示する (デザインレビュー指摘 3 反映)。

## References
- `Assets/Arts/Furniture/Scripts/Furniture.cs` — 現状の `Furniture` SO 定義
- `Assets/Scripts/Home/Service/FurniturePlacementService.cs` — Floor/Wall/Fragmented の既存配置パターン
- `Assets/Scripts/Home/Service/RedecorateScrollerService.cs` — セル選択時の処理エントリポイント
- `Assets/Scripts/Home/Service/IsoGridLoadService.cs` / `IsoGridSaveService.cs` — 永続化パターン
- `Assets/Scenes/Home.unity` — `RoomBackGround` GameObject の現状階層
- `.kiro/steering/structure.md` / `.kiro/steering/tech.md` — DI / 命名 / 依存方向の規約
