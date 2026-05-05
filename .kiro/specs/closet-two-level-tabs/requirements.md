# Requirements Document

## Project Description (Input)
Closetでは、 @Assets/UI/Home/Closet/Textures/ClosetSampleImage1.png  のように、２段階のタブを実装したい。 大タブでは「体」「服」の２種類で分けられ、小タブでは、Cat.Character.OutfitType を選択する
体はBody, Face, Tail、「服」はCloth, HandAccessory, HeadAccessory, LegAccessory, Effectが該当する。
デフォルトでは、「体」のfaceが選択されていて欲しい。
画像はすでにプロジェクト内に配置されている

## Introduction
Homeシーン内のクローゼット画面 (`ClosetUiView`) に、装備カテゴリを2段階のタブで切り替えるUIを導入する。大タブは「体」「服」の2分類、小タブは `Cat.Character.OutfitType` を表す。
クローゼットを開いた直後は「体」 + `Face` が選択された状態で開始し、選択中の `OutfitType` に該当する `Outfit` のみがグリッドに並ぶ。既存の装備選択・即時プレビュー・`PlayerPrefs` 永続化のフローはタブによる絞り込みの下でも維持される。
タブUIに用いる画像 (例: `dressup_heading*.png`, `dressup_tab_*.png`, `Icons/dressup_tab_*` 各種) はすでに `Assets/UI/Home/Closet/Textures/` に配置済みで、本機能では新規アセットを追加しない。

## Requirements

### Requirement 1: 2段階タブの基本表示
**Objective:** As a プレイヤー, I want クローゼット画面に大タブと小タブの2段階タブが表示されること, so that 装備カテゴリを階層的に把握しながら絞り込みできる

#### Acceptance Criteria
1. When `HomeState` が `Closet` に遷移し `ClosetUiView` が開く, the Closet UI shall 大タブと小タブを同時に画面内に表示する
2. The Closet UI shall 大タブとして「体」「服」の2項目だけを表示する
3. The Closet UI shall 小タブとして現在選択中の大タブに対応する `OutfitType` 群のみを表示する
4. While 大タブと小タブが表示されている, the Closet UI shall いずれかの大タブが必ず1つだけ選択状態であることを保証する
5. While 大タブと小タブが表示されている, the Closet UI shall 表示中の小タブのうち必ず1つだけが選択状態であることを保証する

### Requirement 2: 大タブと小タブの構成 (OutfitType マッピング)
**Objective:** As a プレイヤー, I want 大タブと小タブの組み合わせがプロジェクト仕様通りに `OutfitType` と対応していること, so that 期待した分類で装備を探せる

#### Acceptance Criteria
1. While 大タブ「体」が選択状態である, the Closet UI shall 小タブとして `Body`, `Face`, `Tail`, `FaceMakeup` の4つだけを表示する
2. While 大タブ「服」が選択状態である, the Closet UI shall 小タブとして `Cloth`, `HandAccessory`, `HeadAccessory`, `LegAccessory`, `Effect` の5つだけを表示する
3. The Closet UI shall 大タブのいずれにも割り当てられていない `OutfitType` (将来 `OutfitType` が追加された場合) を小タブとして表示しない
4. The Closet UI shall 同一 `OutfitType` を複数の大タブに重複させない
5. The Closet UI shall `FaceMakeup` を大タブ「体」の小タブとして扱う (要件 2.1 と整合)

### Requirement 3: 初期 (デフォルト) 選択状態
**Objective:** As a プレイヤー, I want クローゼットを開いた直後に既定の選択状態 (「体」+ `Face`) で表示されること, so that 毎回同じ場所から操作を開始できる

#### Acceptance Criteria
1. When `ClosetUiView` の `OnOpen` が発火する, the Closet UI shall 大タブ「体」を選択状態にする
2. When `ClosetUiView` の `OnOpen` が発火する, the Closet UI shall 小タブ `Face` を選択状態にする
3. When `ClosetUiView` の `OnOpen` が発火する, the Closet Service shall `OutfitType.Face` に一致する `Outfit` のみをグリッドの表示対象とする
4. When クローゼットを閉じて再度開く, the Closet UI shall 前回のタブ選択状態を引き継がず再度デフォルト状態 (「体」+ `Face`) に戻す

### Requirement 4: 大タブ切替時の小タブ既定選択
**Objective:** As a プレイヤー, I want 大タブを切り替えたときに小タブが自動的に既定値を選択すること, so that 余計なタップ無しに対象カテゴリの中身をすぐに確認できる

#### Acceptance Criteria
1. When 大タブ「服」がタップされ非選択状態から選択状態になる, the Closet UI shall 小タブ `Cloth` を選択状態にする
2. When 大タブ「体」がタップされ非選択状態から選択状態になる, the Closet UI shall 小タブ `Face` を選択状態にする
3. When すでに選択状態の大タブが再度タップされる, the Closet UI shall 現在の小タブ選択状態を変更しない
4. When 大タブが切り替わる, the Closet Service shall 新たに選択された小タブの `OutfitType` でグリッドを再構築する

### Requirement 5: 小タブ切替時のグリッドフィルタリング
**Objective:** As a プレイヤー, I want 小タブの切替に追従してグリッドが対応 `OutfitType` の `Outfit` のみで再描画されること, so that 一覧から目的のカテゴリだけを閲覧できる

#### Acceptance Criteria
1. When 小タブがタップされ選択状態が変化する, the Closet Service shall 選択中 `OutfitType` と一致する `Outfit` のみを `EnhancedScroller` の表示対象とする
2. When 小タブが切り替わる, the Closet UI shall グリッドのスクロール位置を先頭にリセットする
3. If 選択中の小タブの `OutfitType` に該当する `Outfit` がマスターに1件も存在しない, then the Closet UI shall グリッドにセルを1件も表示しない (空状態)
4. While 同一の小タブが連続で選択状態である, the Closet Service shall 不要なグリッド再構築を行わない

### Requirement 6: 既存の装備選択・永続化フローとの整合
**Objective:** As a プレイヤー, I want タブで絞り込んでいる状態でもセル選択による着替えと永続化が従来通り動作すること, so that タブ追加によって既存体験が損なわれない

#### Acceptance Criteria
1. While いずれかの小タブが選択状態である, when プレイヤーがグリッド上のセルをタップする, the Closet Service shall `CharacterView.SetOutfit` で当該 `Outfit` を即時反映する
2. While いずれかの小タブが選択状態である, when プレイヤーがグリッド上のセルをタップする, the Closet Service shall `UserEquippedOutfitService.Equip` を呼び `PlayerPrefs` に永続化する
3. When 小タブまたは大タブが切り替わりグリッドが再構築される, the Closet Service shall 表示中 `OutfitType` で現在装備中の `Outfit` が存在する場合は該当セルを選択中状態 (選択枠表示) にする
4. The Closet Service shall タブで絞り込み中も `UserEquippedOutfitState` の他 `OutfitType` の装備情報を変更しない

### Requirement 7: タブの視覚的フィードバック
**Objective:** As a プレイヤー, I want 選択中の大タブ・小タブが視覚的に区別できること, so that 現在のフィルタ状態を一目で把握できる

#### Acceptance Criteria
1. While 大タブが選択状態である, the Closet UI shall 当該大タブを選択中の見た目 (背景および前面アイコン) で表示する
2. While 大タブが非選択状態である, the Closet UI shall 当該大タブを非選択の見た目で表示する
3. While 小タブが選択状態である, the Closet UI shall 当該小タブを選択中の見た目 (アイコン色など) で表示する
4. While 小タブが非選択状態である, the Closet UI shall 当該小タブを非選択の見た目で表示する
5. The Closet UI shall タブの視覚状態に既存配置済みの `Assets/UI/Home/Closet/Textures/` 配下の画像のみを使用する (新規アセット追加禁止)

### Requirement 8: 既存アーキテクチャ・規約との整合
**Objective:** As a 開発者, I want タブUIの実装が既存のレイヤ構造とコーディング規約に従うこと, so that 既存コードベースの一貫性を保ちレビュー・保守を容易にする

#### Acceptance Criteria
1. The Closet タブ機能 shall 既存の依存方向 (View → Service → State) を逸脱しない
2. The Closet タブ機能 shall タブの状態を `Home.State` 配下の状態クラスに保持し View が直接相互依存しない
3. The Closet タブ機能 shall タブUIの組立を `Home.Scope.HomeScope` の DI 登録に統合する
4. The Closet タブ機能 shall 既存の `ClosetScrollerService` のセル生成・選択処理に対する変更を最小化する (フィルタ条件の差し込みに留める)
5. The Closet タブ機能 shall コーディング規約 (`_camelCase`, `private` 省略, `/// comment`, `[Inject]` 付与等) に準拠する
