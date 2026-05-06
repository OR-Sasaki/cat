# Requirements Document

## Project Description (Input)
@Assets/UI/Home/Closet/Textures/RedecorateSampleImage.png のように、リデコレートにファーニチュアタイプで絞り込めるようなタブを用意したい。デフォルトでは常に1つが選択されています。タブは4つで、ファーニチュアタイプと結びついており、ベース、フロア、スモール、ウォールの4つがある。Furniture.csを参照すること。また、クローゼットでは同じような実装がされており、クローゼットでいうマイナータブのみがファーニチュアでは実装されます。

## Introduction
Homeシーン内のリデコレート画面 (`Home.View.RedecorateUiView`) に、`Cat.Furniture.FurnitureType` (`Base`, `Floor`, `Small`, `Wall`) で家具を絞り込む単一階層のタブUIを導入する。タブは常に4つ表示され、画面オープン時には常に1つだけが選択状態となる。選択中タブに紐づく `FurnitureType` に該当する `Furniture` のみが `EnhancedScroller` のグリッドに描画される。

実装方針は既存の「クローゼットの小タブ」 (`Home.View.ClosetMinorTabsView` / `Home.View.ClosetMinorTabItemView` / `Home.State.ClosetTabState` / `Home.Service.ClosetTabService`) のパターンに準じ、大タブに相当する階層は持たない。タブUIのアイコン・背景画像は `Assets/UI/Home/Closet/Textures/` 配下に配置済みのアセット (`dressup_tab_*.png`, `Icons/dressup_tab_*` 等) を再利用し、本機能では新規アセットを追加しない。

既存のセル選択 → `Home.Service.FurniturePlacementService` 経由の家具配置 (`PlaceFurniture` / `PlaceBase`)、`Home.State.IsoGridState` および `Home.State.RoomBaseState` を踏まえた `Selected` 判定、`Home.Service.RedecorateTinyService` による Tiny 化フローはタブの絞り込み下でも変更なく維持される。

## Requirements

### Requirement 1: ファーニチュアタイプタブの基本表示
**Objective:** As a プレイヤー, I want リデコレート画面に `FurnitureType` ごとのタブが常に表示されること, so that 配置したい家具をタイプ別に素早く絞り込める

#### Acceptance Criteria
1. When `HomeState` が `Redecorate` に遷移し `RedecorateUiView` が開く, the Redecorate UI shall ファーニチュアタイプタブを画面内に表示する
2. The Redecorate UI shall ファーニチュアタイプタブとして `FurnitureType.Base`, `FurnitureType.Floor`, `FurnitureType.Small`, `FurnitureType.Wall` に対応する4つのタブを常に表示する
3. While ファーニチュアタイプタブが表示されている, the Redecorate UI shall 4つのタブのうち必ず1つだけが選択状態であることを保証する
4. The Redecorate UI shall タブを `Cat.Furniture.FurnitureType` 列挙の宣言順 (`Base` → `Floor` → `Small` → `Wall`) で左から並べる
5. The Redecorate UI shall タブをグリッド (`EnhancedScroller`) の上部に配置する

### Requirement 2: タブと FurnitureType のマッピング
**Objective:** As a プレイヤー, I want 各タブが想定通りの `FurnitureType` と1対1で結びついていること, so that 期待した分類で家具を一覧できる

#### Acceptance Criteria
1. While 1番目のタブが選択状態である, the Redecorate UI shall フィルタ対象として `FurnitureType.Base` を採用する
2. While 2番目のタブが選択状態である, the Redecorate UI shall フィルタ対象として `FurnitureType.Floor` を採用する
3. While 3番目のタブが選択状態である, the Redecorate UI shall フィルタ対象として `FurnitureType.Small` を採用する
4. While 4番目のタブが選択状態である, the Redecorate UI shall フィルタ対象として `FurnitureType.Wall` を採用する
5. The Redecorate UI shall 同一の `FurnitureType` を複数タブに重複させない
6. The Redecorate UI shall `Cat.Furniture.FurnitureType` のいずれの値とも結びつかないタブを表示しない

### Requirement 3: 初期 (デフォルト) 選択状態
**Objective:** As a プレイヤー, I want リデコレートを開いた直後に常に既定のタブが1つだけ選択されていること, so that 毎回同じ場所から操作を開始でき、選択ゼロのフィルタ状態が発生しない

#### Acceptance Criteria
1. When `RedecorateUiView` の `OnOpen` が発火する, the Redecorate UI shall 既定タブ (`FurnitureType.Floor`) を選択状態にする
2. When `RedecorateUiView` の `OnOpen` が発火する, the Redecorate Service shall 既定タブの `FurnitureType` に一致する `Furniture` のみをグリッドの表示対象とする
3. When リデコレートを閉じて再度開く, the Redecorate UI shall 前回のタブ選択状態を引き継がず再度デフォルト状態 (`FurnitureType.Floor`) に戻す
4. The Redecorate UI shall いかなるタイミングでもタブがどれも選択されていない状態を発生させない

### Requirement 4: タブ切替時のグリッドフィルタリング
**Objective:** As a プレイヤー, I want タブの切替に追従してグリッドが対応 `FurnitureType` の `Furniture` のみで再描画されること, so that 一覧から目的のタイプだけを閲覧できる

#### Acceptance Criteria
1. When 非選択タブがタップされ選択状態が変化する, the Redecorate Service shall 新たに選択されたタブの `FurnitureType` と一致する `Furniture` のみを `EnhancedScroller` の表示対象とする
2. When タブが切り替わる, the Redecorate UI shall グリッドのスクロール位置を先頭にリセットする
3. If 選択中タブの `FurnitureType` に該当する `Furniture` をユーザーが1件も所持していない, then the Redecorate UI shall グリッドにセルを1件も表示しない (空状態)
4. While 同一のタブが連続で選択状態である, the Redecorate Service shall 不要なグリッド再構築を行わない
5. When タブが切り替わる, the Redecorate Service shall 切替前タブのフィルタ条件下で構築されたセルデータの `SelectedChanged` リスナを解除した上でデータを破棄する

### Requirement 5: 既存配置・選択フローとの整合
**Objective:** As a プレイヤー, I want タブで絞り込んでいる状態でも家具のグリッドタップ → 配置・選択状態反映が従来通り動作すること, so that タブ追加によって既存の配置体験が損なわれない

#### Acceptance Criteria
1. While いずれかのタブが選択状態である, when プレイヤーがグリッド上の `FurnitureType.Floor` / `FurnitureType.Small` / `FurnitureType.Wall` のセルをタップする, the Redecorate Service shall 既存通り `FurniturePlacementService.PlaceFurniture` を呼び出す
2. While `FurnitureType.Base` のタブが選択状態である, when プレイヤーがグリッド上の Base セルを未選択状態でタップする, the Redecorate Service shall 既存通り `FurniturePlacementService.PlaceBase` を呼び出す
3. When タブが切り替わりグリッドが再構築される, the Redecorate Service shall `IsoGridState` (Floor/Wall/Small) および `RoomBaseState` (Base) に基づく既存の `Selected` 判定ロジックをそのまま新しいフィルタ結果セルに適用する
4. The Redecorate Service shall タブの絞り込みによって `IsoGridState`, `RoomBaseState`, `UserState.UserFurnitures` のいずれも変更しない
5. The Redecorate UI shall タブ操作によって既存の Tiny 化 (`RedecorateTinyService`) の挙動を破壊しない

### Requirement 6: タブの視覚的フィードバック
**Objective:** As a プレイヤー, I want 選択中のタブと非選択タブが視覚的に区別できること, so that 現在のフィルタ状態を一目で把握できる

#### Acceptance Criteria
1. While タブが選択状態である, the Redecorate UI shall 当該タブを選択中の見た目 (背景表示) で描画する
2. While タブが非選択状態である, the Redecorate UI shall 当該タブを非選択の見た目 (背景非表示) で描画する
3. The Redecorate UI shall 各タブのアイコンとして `Assets/UI/Home/Closet/Textures/Icons/` 配下の既存画像を使用する
4. The Redecorate UI shall タブの視覚状態に新規アセットを追加せず既存配置済みの `Assets/UI/Home/Closet/Textures/` 配下の画像のみを使用する
5. The Redecorate UI shall 選択中タブの視覚スタイルをクローゼットの小タブ (`ClosetMinorTabItemView`) と整合した方式で実装する

### Requirement 7: 既存アーキテクチャ・規約との整合
**Objective:** As a 開発者, I want タブUIの実装が既存のレイヤ構造とコーディング規約に従うこと, so that 既存コードベースの一貫性を保ちレビュー・保守を容易にする

#### Acceptance Criteria
1. The Redecorate タブ機能 shall 既存の依存方向 (View → Service → State) を逸脱しない
2. The Redecorate タブ機能 shall タブの選択状態を `Home.State` 配下の状態クラスに保持し、View 同士が直接相互依存しない
3. The Redecorate タブ機能 shall タブUI関連クラスの DI 登録を `Home.Scope.HomeScope` に統合する
4. The Redecorate タブ機能 shall 既存の `RedecorateScrollerService` のセル生成・選択・配置処理に対する変更を最小化し、フィルタ条件の差し込みおよびタブ変更通知の購読のみに留める
5. The Redecorate タブ機能 shall コーディング規約 (`_camelCase`, `private` 省略, `/// comment`, `[Inject]` 付与, UniTask の `CancellationToken` 末尾引数等) に準拠する
6. The Redecorate タブ機能 shall 大タブに相当する階層を実装せず、単一階層のタブのみで構成する (クローゼットでいうマイナータブ相当のみ)
