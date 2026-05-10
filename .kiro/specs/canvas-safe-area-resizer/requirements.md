# Requirements Document

## Project Description (Input)
キャンバスにアタッチするセーフエリアにぴったりはまるようなリサイズコンポーネントを作成してほしい。

## Introduction

Unity 6 プロジェクトの全シーンで再利用可能な、Canvas 配下の RectTransform をデバイスのセーフエリア（ノッチ・ホームインジケータ・ステータスバーなどを除外した安全な表示領域）にぴったり一致させる MonoBehaviour ベースのリサイズコンポーネントを提供する。

開発者は本コンポーネントを Canvas 配下の任意の RectTransform にアタッチするだけで、対象 RectTransform のアンカーが自動的にセーフエリアへ完全フィットするように設定され、画面の向き変更・解像度変更・SafeArea 値の動的変化にも追従する。これにより、Home / Title / Shop / Timer / History などの各シーンで一貫したセーフエリア対応が可能になり、デバイス固有の非表示領域に UI が被る不具合や、開発者ごとに散在する手動対応コードの重複を排除する。

## Requirements

### Requirement 1: セーフエリアへの自動フィット

**Objective:** As a UI 開発者, I want Canvas 配下の RectTransform をセーフエリアにぴったり合わせるコンポーネント, so that 端末固有のノッチや非表示領域を意識することなく安全な領域に UI を配置できる。

#### Acceptance Criteria
1. When 本コンポーネントが有効化された, the Canvas Safe Area Resizer shall 対象 RectTransform の anchorMin / anchorMax を `Screen.safeArea` を画面解像度で正規化した値に設定する
2. The Canvas Safe Area Resizer shall 対象 RectTransform の offsetMin / offsetMax を 0 に設定し、結果として描画矩形がセーフエリアに完全一致するようにする
3. The Canvas Safe Area Resizer shall RectTransform を `[RequireComponent]` として宣言し、RectTransform を持たない GameObject にはアタッチできないようにする
4. If 対象 GameObject の親階層に Canvas が存在しない, then the Canvas Safe Area Resizer shall `[CanvasSafeAreaResizer]` プレフィックス付き警告ログを出力し、アンカー書き込みを行わない

### Requirement 2: 画面状態変化への追従

**Objective:** As a UI 開発者, I want 画面の向き・解像度・セーフエリア値の動的変化に自動で追従すること, so that 端末回転・ウィンドウサイズ変更・マルチタスク復帰などの状況でもセーフエリアにフィットした状態を維持できる。

#### Acceptance Criteria
1. When `Screen.safeArea` の値が前回適用時から変化した, the Canvas Safe Area Resizer shall 対象 RectTransform のアンカーを再計算して反映する
2. When `Screen.width` または `Screen.height` が前回適用時から変化した, the Canvas Safe Area Resizer shall セーフエリアの正規化座標を再計算して反映する
3. When `Screen.orientation` が前回適用時から変化した, the Canvas Safe Area Resizer shall セーフエリアを再計算して反映する
4. While 本コンポーネントが有効である間, the Canvas Safe Area Resizer shall 上記の変化を継続的に監視し、いずれにも変化がない場合はアンカー書き込みをスキップする

### Requirement 3: 適用辺の個別制御

**Objective:** As a UI 開発者, I want セーフエリアの適用を上下左右の各辺ごとに有効・無効化できること, so that 「画面下端まで広げたい背景」「ステータスバー直下まで描きたい装飾」などのデザイン要件に応じて柔軟にレイアウトを調整できる。

#### Acceptance Criteria
1. The Canvas Safe Area Resizer shall インスペクター上で「上 / 下 / 左 / 右」の各辺について、セーフエリア適用の有効・無効をシリアライズ可能な真偽値として個別に設定できる
2. Where 特定の辺の適用が無効に設定されている, the Canvas Safe Area Resizer shall その辺については Canvas 全体の端 (アンカー値 0 または 1) を採用する
3. When 適用辺の設定値がインスペクターまたはコードから変更された, the Canvas Safe Area Resizer shall 次の更新タイミングで新しい設定をアンカーに反映する

### Requirement 4: Canvas Render Mode 互換

**Objective:** As a UI 開発者, I want Screen Space 系の Canvas Render Mode で同一コンポーネントが正しく動作すること, so that シーンごとに Canvas 構成 (Overlay / Camera) が異なっても同一コンポーネントを再利用できる。

#### Acceptance Criteria
1. While 親 Canvas の renderMode が Screen Space - Overlay または Screen Space - Camera である, the Canvas Safe Area Resizer shall `Screen.safeArea` と画面解像度から算出した正規化アンカーを対象 RectTransform に適用する
2. If 親 Canvas の renderMode が World Space である, then the Canvas Safe Area Resizer shall リサイズ処理を実行せず、`[CanvasSafeAreaResizer]` プレフィックス付き警告ログを出力する
3. When 親 Canvas の renderMode が実行中に変更された, the Canvas Safe Area Resizer shall 次の更新タイミングで新しい renderMode に応じた処理を行う

### Requirement 5: エディタでのプレビュー

**Objective:** As a UI 開発者, I want エディタ実行時のみならず編集中にも対象 RectTransform がセーフエリアにフィットする様子をプレビューできること, so that 実機ビルドや Play モード起動を待たずに Game ビュー上でセーフエリア対応の見た目を確認・微調整できる。

#### Acceptance Criteria
1. The Canvas Safe Area Resizer shall `[ExecuteAlways]` 属性を付与し、エディタ非実行時にも有効化・無効化に応じてリサイズ処理を実行する
2. When 開発者が Scene ビューまたは Inspector で対象 RectTransform のアンカー値を手動変更した, the Canvas Safe Area Resizer shall 次の更新タイミングで自身の計算結果でアンカーを上書きする
3. When 本コンポーネントが GameObject から取り除かれた, the Canvas Safe Area Resizer shall それ以降アンカー値の自動上書きを行わない

### Requirement 6: パフォーマンスとログ衛生

**Objective:** As a UI 開発者, I want コンポーネントが必要なときだけ計算と書き込みを行い、無駄な処理やログ出力で他機能を阻害しないこと, so that 複数シーン・多数の UI 階層に本コンポーネントが配置されてもフレームレートとログ可読性を維持できる。

#### Acceptance Criteria
1. While `Screen.safeArea`・画面サイズ・画面の向き・適用辺設定のいずれにも前回適用時から変化がない, the Canvas Safe Area Resizer shall RectTransform への書き込みを行わない
2. The Canvas Safe Area Resizer shall 警告およびエラーログ出力時に `[CanvasSafeAreaResizer]` のクラスコンテキストプレフィックスを付与する
3. The Canvas Safe Area Resizer shall 通常動作時に情報ログ (`Debug.Log`) を出力しない
