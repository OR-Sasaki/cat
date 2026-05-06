# Requirements Document

## Project Description (Input)
PlacementType.Baseの挙動を実装したい。現在実装されているのはFloor, Wallです。PlacementTypeBaseとは、部屋そのもののことであり、シーン上にあるRoomBackgroundの子オブジェクトとして配置される。このオブジェクトはユーザーが動かすことができず、単に配置するか配置しないかだけを選ばれます。デフォルトでは初めに持っているベースが選ばれており、必ず一つ選択されている状態です。2つ以上選択されている状態はありえない。1つ選択している状態で他のものが選択されたら、最初に選択されているものは取り外される。RedecorateScrollerServiceでセルが選択されたときの挙動が実装されているが、PlacementTypeのBaseの場合は少し特殊で、FurniturePlacementServiceに新しくPlaceBaseというメソッドを用意し、それで部屋に配置するようにしてください。

なお、`PlacementType.Base` のシーンオブジェクト (プレハブ) には Floor / Wall とは異なり `IsoDraggableView` (IsoDraggable コンポーネント) はアタッチされない。Base はドラッグ操作・IsoGrid のセル管理・スナップ等の対象外であり、配置経路から `IsoDraggableView` への参照を持ち込まない。

## Introduction
Homeシーンの模様替え (Redecorate) における家具配置タイプとして、現在 `PlacementType.Floor` (床) と `PlacementType.Wall` (壁) が実装されている。
本仕様では、3 番目の配置タイプ `PlacementType.Base` (部屋そのもの = ベース) の挙動を新たに導入する。

`PlacementType.Base` の家具は「部屋の見た目そのもの」を表し、シーン階層上の `RoomBackground` オブジェクトの子として配置される。
Floor / Wall とは異なり、Base のシーンオブジェクトには `IsoDraggableView` がアタッチされておらず、プレイヤーがドラッグで動かすことはできず、IsoGrid のセルも占有しない。
ユーザー操作としては「配置するか / 配置しないか」のみで、常にちょうど 1 つの Base がアクティブ (選択中) であることを保証する。
新たな Base を選択した場合は、それまで配置されていた Base が自動的に取り外され、選択中のものに差し替わる。

実装は、既存の `Home.Service.RedecorateScrollerService` におけるセル選択ハンドラ (`OnCellViewSelected`) に Base 用の分岐を加え、
実配置は `Home.Service.FurniturePlacementService` に新設するメソッド `PlaceBase` に委譲する。
`PlaceBase` は `IsoDraggableView` を介さない独自経路として実装し、Floor / Wall (`PlaceFurniture` 系) の既存ロジックには影響を与えない。
これにより Floor / Wall と同様の「セル選択 → 配置サービス呼び出し」の流れを保ちつつ、
Base 固有の単一選択・差し替え・親オブジェクト指定の制約を成立させる。

## Constraints (前提)
- **Base のシーンオブジェクトには `IsoDraggableView` をアタッチしない**: `PlacementType.Base` のプレハブは `Transform` と `SpriteRenderer` のみで構成される純粋な GameObject であり、Floor / Wall と異なり `IsoDraggableView` (IsoDraggable コンポーネント) を保持しない。
- このため、Base 配置経路では `IsoDraggableView` への参照取得・メソッド呼び出し・状態書き込み・`FindObjectsByType<IsoDraggableView>` 等の検索を行わない。
- Floor / Wall 用に存在する `Cat.Furniture.Furniture.SceneObject` フィールド (`IsoDraggableView` 型) は Base 家具では使用しない (Base アセット側でも `null` のままで運用される)。Base 用のシーンオブジェクト参照手段の具体は本要件では規定せず、設計フェーズで定義する。

## Requirements

### Requirement 1: PlacementType.Base のシーン配置 (RoomBackground 配下への配置)
**Objective:** As a プレイヤー, I want `PlacementType.Base` のセルを選択したときに対応する Base が部屋の背景として配置されること, so that 部屋全体の雰囲気を切り替えられる

#### Acceptance Criteria
1. When プレイヤーが Redecorate グリッドで `PlacementType.Base` のセルを選択する, the Furniture Placement Service shall 当該 Base を `RoomBackground` オブジェクトの子として Home シーンに配置する
2. The Furniture Placement Service shall `PlacementType.Base` の家具を `IsoGridState` (Floor / LeftWall / RightWall / FragmentedGrids) のいずれにも登録しない
3. While `PlacementType.Base` の家具がシーンに配置されている, the Furniture Placement Service shall 当該シーンオブジェクトの親が `RoomBackground` オブジェクトのままであることを保証する
4. If 配置対象 Base のシーンオブジェクト (プレハブ) 参照が `null` である, then the Furniture Placement Service shall 配置を行わずに警告ログを出力する

### Requirement 2: 常に 1 つだけ選択されている状態の保証 (単一選択不変条件)
**Objective:** As a プレイヤー, I want Base が常にちょうど 1 つだけ選択 (= 配置) されていること, so that 部屋に背景が無い状態や複数の背景が重なる状態を発生させない

#### Acceptance Criteria
1. While Home シーンが操作可能な状態である, the Room Base System shall シーン階層上で `PlacementType.Base` の家具がちょうど 1 つだけアクティブに配置されている状態を保証する
2. While Home シーンが操作可能な状態である, the Room Base System shall `PlacementType.Base` の家具がシーン上に 2 つ以上同時に配置された状態を発生させない
3. The Redecorate UI shall 現在配置中の Base に対応するセルを選択中表示にし、それ以外の Base セルを非選択表示にする
4. The Redecorate UI shall `PlacementType.Base` のセルに対する「未配置」状態 (= どの Base も選択されていない状態) をユーザー操作で発生可能にしない

### Requirement 3: 起動時のデフォルト Base 選択
**Objective:** As a プレイヤー, I want Home シーンを開いた時点で既定の Base が自動的に部屋に配置されていること, so that 初回起動でも空の背景にならず、即座に模様替えを開始できる

#### Acceptance Criteria
1. When Home シーンの初期化が完了する, the Room Base System shall ユーザーが所持している `PlacementType.Base` の家具のうちいずれか 1 つを既定として選択し、`RoomBackground` の子として配置する
2. The Room Base System shall 既定として選択する Base を、ユーザー所持データ (`UserState.UserFurnitures`) のうち `PlacementType.Base` を持つ最初のエントリとして決定する
3. While ユーザーが `PlacementType.Base` の家具を 1 つも所持していない, the Room Base System shall シーンに Base を配置せず、Redecorate UI 上でも Base 選択中表示を行わない (Requirement 2.1 の例外)
4. When Home シーンの初期化が完了する, the Redecorate UI shall 既定として配置された Base に対応するセルを選択中表示にする

### Requirement 4: Base セル選択時の差し替え (排他選択)
**Objective:** As a プレイヤー, I want 別の Base セルを選んだときに、現在配置中の Base が自動的に取り外され新しい Base に差し替わること, so that 1 タップで部屋の背景を切り替えられる

#### Acceptance Criteria
1. When プレイヤーが現在選択中ではない `PlacementType.Base` のセルをタップする, the Redecorate Scroller Service shall 当該セルの Base を新たに `RoomBackground` の子として配置する
2. When プレイヤーが現在選択中ではない `PlacementType.Base` のセルをタップする, the Redecorate Scroller Service shall 直前まで配置されていた Base のシーンオブジェクトを Home シーンから取り外す
3. When プレイヤーが既に選択中である `PlacementType.Base` のセルを再度タップする, the Redecorate Scroller Service shall 配置中の Base を取り外さず、現在の Base の状態を維持する (= Requirement 2 を破らない)
4. When `PlacementType.Base` セルの選択が切り替わる, the Redecorate UI shall 旧 Base セルを非選択表示に、新 Base セルを選択中表示に更新する
5. While Base の差し替え処理が進行中である, the Room Base System shall シーンに Base が 0 個または 2 個以上存在する中間状態を観測可能にしない (新 Base の配置と旧 Base の取り外しは原子的に完了する)

### Requirement 5: FurniturePlacementService.PlaceBase の責務
**Objective:** As a 開発者, I want Base 専用の配置ロジックを `FurniturePlacementService.PlaceBase` メソッドとして集約すること, so that Floor / Wall と同様に「配置タイプ別メソッドに委譲する」既存パターンを保てる

#### Acceptance Criteria
1. The Furniture Placement Service shall `PlacementType.Base` の家具をシーンへ配置する責務を `PlaceBase` メソッドとして公開する
2. The `PlaceBase` メソッド shall ユーザー所持 ID (`userFurnitureId`) と対象 `Cat.Furniture.Furniture` を入力として受け取る
3. The `PlaceBase` メソッド shall シーン上に既に存在する `PlacementType.Base` の家具を取り外したうえで新しい Base を配置する (Requirement 4.5 と整合)
4. If `PlaceBase` メソッドに渡された `Furniture` の `PlacementType` が `Base` 以外である, then the Furniture Placement Service shall 配置を行わずに警告ログを出力する
5. The Redecorate Scroller Service shall `PlacementType.Base` のセル選択時、Floor / Wall 用 (`PlaceFurniture`) の経路ではなく `PlaceBase` 経路を呼び出す
6. The `PlaceBase` メソッド shall `IsoDraggableView` への参照取得・メソッド呼び出し・状態書き込み・`FindObjectsByType<IsoDraggableView>` 等の検索を一切行わない (Base プレハブには `IsoDraggableView` がアタッチされていないため)
7. The `PlaceBase` メソッド shall `Cat.Furniture.Furniture.SceneObject` フィールド (`IsoDraggableView` 型) に依存せず、Base 用のシーンオブジェクト参照経路を独自に解決する
8. The Furniture Placement Service shall Floor / Wall 既存メソッド (`PlaceFurniture`, `PlaceFloorFurnitureAt`, `PlaceWallFurnitureAt`, `PlaceFragmentedFurnitureAt`) のシグネチャと振る舞いを本要件で変更しない

### Requirement 6: ユーザー操作・グリッド占有の制約 (ドラッグ不可・グリッド非占有)
**Objective:** As a プレイヤー, I want 部屋の Base はドラッグで動かせず、Floor / Wall の家具配置に影響を与えないこと, so that 背景の上に家具を自由に配置できる

#### Acceptance Criteria
1. The Room Base System shall `PlacementType.Base` のプレハブに `IsoDraggableView` (IsoDraggable コンポーネント) を一切アタッチしない
2. While `PlacementType.Base` の家具がシーンに配置されている, the Iso Drag System shall 当該家具を検出・操作対象として認識しない (= シーン上に `IsoDraggableView` が存在しないためドラッグ経路に入らない)
3. The Furniture Placement Service shall `PlacementType.Base` の家具を配置しても `IsoGridState` の Floor / LeftWall / RightWall / FragmentedGrids のいずれのセルも占有させない
4. While `PlacementType.Base` の家具が配置されている, the Furniture Placement Service shall Floor / Wall / Fragmented 家具を Base が原因で配置不可と判定させない
5. The Room Base System shall `PlacementType.Base` の家具を Redecorate UI 上で「未配置に戻す」(取り外しのみ) のユーザー操作を提供しない (Requirement 2.4 と整合)

### Requirement 7: 既存アーキテクチャ・規約との整合
**Objective:** As a 開発者, I want Base 配置の実装が既存のレイヤ構造・命名規約・DI パターンに従うこと, so that 既存コードベースの一貫性を保ちレビュー・保守を容易にする

#### Acceptance Criteria
1. The Room Base System shall 依存方向 (View → Service → State) を逸脱せず、Service が State を経由してデータをやり取りする
2. The Room Base System shall Base 専用の状態を保持する場合は `Home.State` 配下に配置し、View に直接保持させない
3. The Room Base System shall 配置・差し替えのオーケストレーションを `Home.Service` 配下のサービス層で実行する (View には UI 反映のみを残す)
4. The Room Base System shall コーディング規約 (`_camelCase`, `private` 省略, `/// comment`, `[Inject]` 付与, `#nullable enable` 利用時の取扱い等) に準拠する
5. The Room Base System shall 既存の `Home.Scope.HomeScope` の DI 登録パターンに沿って必要な新規 State / Service を登録する
