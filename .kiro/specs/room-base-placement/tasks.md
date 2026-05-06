# Implementation Plan

## Branches

**Base**: `feature/room-base-placement`

すべてのタスクをベースブランチで実装する。Base 配置経路は Floor/Wall とは物理的に分離しているが、起動時の単線フロー (Load → ApplyDefaultIfNeeded) と DI 配線・シーン/アセット整備が一体で初めて動作するため、単一 PR としてレビューする。

## サブエージェント活用方針

各タスクの詳細ブレットに `**サブエージェント活用**:` 行があるものは、Claude Code のサブエージェント (`Explore` / `general-purpose` 等) を併用するとコンテキスト整備や並列調査・雛形作成で効率化が見込める箇所。原則として実装本体はメインエージェントが担当し、サブエージェントは事前リサーチ・横断検索・テスト雛形生成等の補助に限定する。

- `Explore` (thoroughness: medium): 既存コードのパターン抽出・呼び出し関係調査・命名規約や DI 順序の確認
- `general-purpose`: 複数ファイルにまたがる調査と雛形生成 (例: 似たレイヤの新規ファイル作成セット)
- Unity Editor 操作 (シーン/プレハブ/アセット編集) はサブエージェント対象外。CLAUDE.md の指示に従い UnityMCP を優先利用する
- メインエージェントは **サブエージェントが返した要約を再度自分で検証** したうえで実装に着手する (鵜呑みにしない)

## Tasks

### Branch: `feature/room-base-placement`

- [x] 1. (P) Furniture SO と IsoGridSaveData にスキーマ拡張を加え、Base 用の参照と永続化フィールドを用意する
  - [x] 1.1 (P) Furniture SO に Base プレハブ参照フィールドを追加し、Floor/Wall とは独立した参照経路を確保する
    - 既存の SceneObject (IsoDraggableView 型) は据え置き、新規に Transform 型のフィールドを追加して Base プレハブの Root Transform を保持する
    - Floor/Wall アセット (LargeBox/MidBox/Painting 等) は新フィールドを null のまま運用し、Unity が自動追記するメタ差分のみが発生することをレビューで告知する
    - 不変条件として「PlacementType.Base のとき Base 用フィールド != null かつ SceneObject == null、Floor/Wall は逆」を SO クラス doc コメントで明示する
    - **サブエージェント活用**: `Explore` (medium) で `Furniture.SceneObject` の全参照箇所と既存 .asset ファイル (Floor/Wall/Base) の Serialized 構造を一括抽出し、新フィールド追加で破壊的変化が出ないかを事前確認する
  - [x] 1.2 (P) IsoGridSaveData に Base 配置 ID 用の int フィールドを追加し、後方互換可能な sentinel を定義する
    - [Serializable] スキーマに 1 フィールドを追加し、初期値 -1 を未配置 sentinel として設定
    - 旧セーブデータ復元時に欠損フィールドが 0 になる挙動を踏まえ、復元側で <= 0 をすべて未設定扱いするロジック前提を明文化する
    - JsonUtility 直列化との互換 (nullable 不可) を踏襲し、新キーは追加しない (PlayerPrefsKey.IsoGrid に同居)
  - _Requirements: 1.4, 2.1, 5.7_

- [x] 2. (P) Base 単一選択状態と RoomBackGround 参照を扱う State/View を新設する
  - [x] 2.1 (P) RoomBaseState を Home.State 配下に追加し、現在配置中の Base の userFurnitureId のみを保持する純粋データ State を定義する
    - 単一フィールド PlacedBaseUserFurnitureId を持ち、UnplacedId = -1 の sentinel を公開定数として定義する
    - SetPlaced(int) / Clear() / IsPlaced (== UnplacedId 以外) の最小 API のみ公開し、GameObject 参照は持たせない
    - 値変更は FurniturePlacementService.PlaceBase および IsoGridLoadService の復元経路からのみ行う運用ルールをクラス doc コメントで明示する
  - [x] 2.2 (P) RoomBackGroundView を Home.View 配下に追加し、Base 専用コンテナ Transform への型安全な参照を提供する
    - SerializeField で _baseRoot Transform を保持し、BaseRoot プロパティで読み取り専用露出する
    - クラス doc コメントと [Header] 注記で「BaseRoot 直下は Base インスタンス専用領域、デバッグ補助・装飾物の配置を禁止」を運用ルールとして明文化する (PlaceBase が無条件に全子破棄するため)
    - 既存 RoomBackGround 直下の RoomObject 等は BaseRoot の兄弟として残し、Base 配置経路の影響を受けない位置関係にする方針をクラスコメントで言及する
    - **サブエージェント活用**: `Explore` (quick) で既存 View 層 (`CharacterView` / `IsoGridSettingsView` 等) の `[SerializeField]` + `[Header]` + プロパティ露出パターンを 1 〜 2 件抽出し、命名・コメント様式を揃える
  - _Requirements: 1.1, 1.3, 2.1, 2.2, 7.1, 7.2, 7.4_

- [x] 3. FurniturePlacementService に PlaceBase を新設し、Base 配置の中核ロジックを集約する
  - [x] 3.1 PlaceBase メソッドを追加し、旧破棄 → 新生成 → State 同期を 1 コール内で完結させる
    - userFurnitureId と Furniture を受け取り、PlacementType == Base / BaseSceneObject != null を最初に検証して不一致なら警告ログ + false 返却で配置状態を不変に保つ
    - 検証通過後は RoomBackGroundView.BaseRoot の全子 Transform を foreach で Object.Destroy したうえで、Object.Instantiate(furniture.BaseSceneObject, BaseRoot) → SceneManager.MoveGameObjectToScene → RoomBaseState.SetPlaced を同期実行する
    - IsoDraggableView / IsoGridState / IsoGridService / FindObjectsByType<IsoDraggableView> のいずれも参照しない (Base プレハブには IsoDraggable コンポーネントが存在しない構造前提)
    - 警告ログは既存規約 [FurniturePlacementService] プレフィックス付き Debug.LogWarning で統一する
    - **サブエージェント活用**: `Explore` (medium) で既存 `PlaceFloorFurnitureAt` / `PlaceWallFurnitureAt` / `PlaceFragmentedFurnitureAt` の手順 (Instantiate → SceneManager.MoveGameObjectToScene → 状態書き込みの順序、エラーログ文言、戻り値規約) を抽出し、Base 経路を同じ命名・ログ規約で実装する
  - [x] 3.2 PlaceFurniture 冒頭に PlacementType.Base を弾く fail-fast を仕込み、誤って Floor 経路に流れないようにする
    - PlaceFurniture の先頭で Base なら警告ログ + null 返却し、既存 Wall/Floor 分岐ロジックには手を加えない
    - PlaceFloorFurnitureAt / PlaceWallFurnitureAt / PlaceFragmentedFurnitureAt および RemoveFurniture のシグネチャ・挙動を変更しないことを実装上の差分でも保証する
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.2, 4.2, 4.5, 5.1, 5.2, 5.3, 5.4, 5.6, 5.7, 5.8, 6.3, 6.4, 7.3_

- [x] 4. RoomBaseDefaultService を新設し、ロード後フックで Base 未配置時のみ既定 Base を配置する
  - 通常 Service として実装し、IStartable は実装せず FurnitureAssetState.OnLoaded を直接購読しないことで購読順依存を構造的に排除する
  - 公開メソッドは ApplyDefaultIfNeeded() のみで、IsoGridLoadService.Load 末尾から同期呼び出しされる契約とする
  - RoomBaseState.IsPlaced が true (= 復元済み) のときは即 return し、no-op として静かに完了する
  - false の場合は UserState.UserFurnitures を配列順に走査し、最初に見つかった PlacementType.Base の家具を MasterDataState.Furnitures と FurnitureAssetState.Get(name) で解決して PlaceBase に渡す
  - UserFurnitures が null または Base を 1 件も所持していないケースは警告ログを出さず no-op (Outfit 適用と同じ静的フォールバック挙動を踏襲)
  - [Inject] 付きコンストラクタで UserState / MasterDataState / FurnitureAssetState / FurniturePlacementService / RoomBaseState を受け取る (IL2CPP ストリッピング対策)
  - 配置に失敗した場合 (PlaceBase が false 返却) は再試行せず 1 回のフォールバックで終える
  - **サブエージェント活用**: `Explore` (medium) で `HomeStarter` の Outfit 適用ロジック (UserState / MasterDataState 走査の具体パターン)、`FurnitureAssetState.Get` のシグネチャと null ハンドリング、`UserState.UserFurnitures` のエントリ構造 (Id / FurnitureID 命名差異) を一括で確認し、`ApplyDefaultIfNeeded` 実装時の参照ミスを未然に防ぐ
  - _Requirements: 2.1, 3.1, 3.2, 3.3, 7.3, 7.4_

- [x] 5. 永続化系を Base 対応に拡張し、保存・復元・デフォルト適用の単線フローを成立させる
  - [x] 5.1 IsoGridSaveService.Save に RoomBaseState の現在 ID 書き込みを追加する
    - コンストラクタ DI に RoomBaseState を [Inject] 経由で追加し、既存 4 種 (Floor/LeftWall/RightWall/FragmentedGrids) の保存処理は変更しない
    - IsoGridSaveData 構築時に BaseUserFurnitureId = _roomBaseState.PlacedBaseUserFurnitureId を設定する
    - 既存の保存ログメッセージに Base ID を加え、状況把握をしやすくする
  - [x] 5.2 IsoGridLoadService に LoadBaseObject と post-load フック呼び出しを追加し、Base 復元と既定適用までを単一エントリで完結させる
    - 既存の Floor/Wall/Fragmented 復元処理の後に LoadBaseObject(saveData.BaseUserFurnitureId) を呼ぶ (saveData != null 経路のみ)
    - LoadBaseObject では <= 0 を未設定として早期 return し、UserFurnitures → MasterDataState → FurnitureAssetState を独自に辿って PlacementType == Base かつ BaseSceneObject != null を検証してから PlaceBase に委譲する (既存 GetFurnitureAsset には一切手を加えない)
    - saveData == null の早期 return 経路を含む Load の最末尾で _roomBaseDefaultService.ApplyDefaultIfNeeded() を必ず 1 回呼び出すことで OnLoaded 多重購読を排除した単線フローを成立させる
    - コンストラクタ DI に RoomBaseState と RoomBaseDefaultService を [Inject] 経由で追加し、既存 OnLoaded の購読は IsoGridLoadService 単独に維持する
    - **サブエージェント活用**: `Explore` (medium) で `IsoGridLoadService.Load` の現在のフロー (Floor/Wall/Fragmented それぞれの Load 補助メソッド構造、`GetFurnitureAsset` の null ハンドリング、`OnLoaded` 解除の位置) を読み解き、`LoadBaseObject` を既存スタイルに沿わせて実装する
  - _Requirements: 2.1, 3.1, 3.3, 5.5, 5.8, 7.1, 7.3_

- [x] 6. RedecorateScrollerService に Base 専用分岐を追加し、選択中表示と差し替え操作を整える
  - [x] 6.1 OnCellViewSelected に PlacementType.Base 分岐を追加し、未選択時のみ PlaceBase に委譲する
    - 既選択 Base セルの再タップは何もしないことで Base 取り外し UI を提供しない (Base が常に 1 つアクティブな不変条件を維持)
    - Base ブランチではカメラ移動・Tiny 化を呼ばず、PlaceBase 後に UpdateSelectionStates だけを実行して return する
    - Floor/Wall の既存 PlaceFurniture 経路には手を加えない
    - **サブエージェント活用**: `Explore` (medium) で `OnCellViewSelected` の既存ガード・選択判定・Floor/Wall 分岐後のカメラ移動 / Tiny 化処理を読み込み、Base ブランチをどの位置に挟めば既存挙動を 1 行も書き換えずに済むかを事前に決める
  - [x] 6.2 UpdateSelectionStates の判定を PlacementType ごとに分岐させる
    - PlacementType.Base のデータは _roomBaseState.PlacedBaseUserFurnitureId == data.UserFurnitureId で Selected を決定する
    - Floor/Wall は既存の _isoGridState.EnumerateAllGrids() ベース判定を据え置き、影響を与えない
    - コンストラクタ DI に RoomBaseState を [Inject] 経由で追加する
  - _Requirements: 2.3, 2.4, 3.4, 4.1, 4.3, 4.4, 5.5, 6.5, 7.3, 7.4_

- [ ] 7. HomeScope DI 登録とシーン/アセット側のセットアップを行い、機能が起動可能な状態にする
  - [x] 7.1 HomeScope に新規 State/View/Service の登録を追加する
    - SerializeField として RoomBackGroundView _roomBackGroundView を追加し Configure 内で builder.RegisterComponent(_roomBackGroundView) を呼ぶ
    - RoomBaseState と RoomBaseDefaultService をそれぞれ Lifetime.Scoped で Register する (RegisterEntryPoint は使わない)
    - 既存の VContainer 登録順・パターンに沿わせ、IsoGridSaveService / IsoGridLoadService の追加依存はコンストラクタ自動解決に任せる
    - **サブエージェント活用**: `Explore` (quick) で `HomeScope` の現在の `[SerializeField]` 並びと `Configure` での登録順 (RegisterComponent / Register / RegisterEntryPoint のグルーピング) を抽出し、新規 3 種をどのブロックに足すかを揃える
  - [ ] 7.2 Home.unity の RoomBackGround GameObject に RoomBackGroundView をアタッチし、Base 専用コンテナを整備する
    - RoomBackGround 直下に新規子 Transform Bases を作成し、_baseRoot にアサインする (LocalPosition は (0, 0, 0) を推奨)
    - 既存子 RoomObject は Bases の兄弟として残し、Base 配置経路 (全子破棄) の影響を受けない階層関係にする
    - HomeScope コンポーネントの SerializeField に RoomBackGroundView をシーン上で割り当てる
    - **サブエージェント非対象**: シーンの GameObject 階層編集と SerializeField のドラッグ割り当ては Unity Editor (UnityMCP) を直接使用する
  - [ ] 7.3 (P) BaseA.asset の BaseSceneObject に Base プレハブの Root Transform を割り当て、Base プレハブ側の構造前提を確認する
    - 既存 Floor/Wall アセット (LargeBox/MidBox/Painting 等) は BaseSceneObject を null のまま据え置く
    - Base プレハブには IsoDraggableView (IsoDraggable コンポーネント) が一切アタッチされていないことを点検し、付与されている場合は外す
    - 他 SO の SceneObject フィールドに Base プレハブを誤って割り当てていないこと、Base SO の SceneObject は null のままであることを目視で確認する
    - **サブエージェント非対象**: `.asset` の Inspector アサインは Unity Editor 操作が必要なため、UnityMCP もしくは手動アサインで対応する
  - _Requirements: 6.1, 6.2, 7.1, 7.4, 7.5_

- [ ]* 8. PlaceBase / RoomBaseDefaultService / RedecorateScrollerService 周辺のテストを整備する
  - **サブエージェント活用 (タスク全体)**: 各サブタスクは独立したテストファイルとして書けるため、`general-purpose` エージェントを 3 並列で立ち上げ、それぞれが既存のテストフレームワーク設定 (Assembly Definition、NUnit/Unity Test Framework の有無、モック化方針) を確認しつつ雛形を生成 → メインエージェントが最終的な統合と通し実行を担当する
  - [ ]* 8.1 (P) FurniturePlacementService.PlaceBase の単体テストを追加する
    - 正常系で BaseRoot 配下に GameObject が 1 個生成され、RoomBaseState.PlacedBaseUserFurnitureId が更新されることを検証 (Acceptance: 1.1, 1.3, 2.1)
    - PlacementType != Base または BaseSceneObject == null で false 返却 + 状態不変を検証 (Acceptance: 1.4, 5.4)
    - 既存 Base 配置中に別 ID で呼び出すと旧 GameObject が破棄され新 GameObject に置き換わり、中間状態が観測されないことを検証 (Acceptance: 2.2, 4.2, 4.5)
  - [ ]* 8.2 (P) 起動シーケンスの統合テストを追加する
    - 保存データに有効 Base ID あり: ロードで配置され RoomBaseDefaultService が no-op であることを検証 (Acceptance: 3.1)
    - 保存データに Base ID 無し / 旧データ互換: RoomBaseDefaultService が UserFurnitures 先頭の Base を配置することを検証 (Acceptance: 3.1, 3.2)
    - Base 未所持ユーザー: 何も配置されず Redecorate UI 上で Base セルの選択中表示が無いことを検証 (Acceptance: 3.3)
  - [ ]* 8.3 (P) RedecorateScrollerService の選択挙動テストを追加する
    - Base セル A → B のタップ順で最終的に B のみが配置中表示となり、A が非選択表示に戻ることを検証 (Acceptance: 4.1, 4.4, 2.3)
    - 既選択 Base セル再タップで配置状態が変化しないことを検証 (Acceptance: 4.3, 6.5)
    - UpdateSelectionStates が Base のみ RoomBaseState 経由、Floor/Wall は IsoGridState 経由で判定されることを検証 (Acceptance: 2.3, 5.5)
  - _Requirements: 1.1, 1.3, 1.4, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 4.1, 4.2, 4.3, 4.4, 4.5, 5.4, 5.5, 6.5_
