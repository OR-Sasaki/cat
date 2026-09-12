# Requirements Document

## Project Description (Input)

家具設置時のグリッド線表示

## Introduction

Home シーンの模様替え (Redecorate) で家具をドラッグして設置する際、配置先となるグリッドのマス目を線で可視化する機能。現在はエディタの Gizmo (`IsoGridGizmo`) でしかグリッドを確認できず、実機ではプレイヤーがどのマスに家具が収まるのか判断しづらい。ドラッグ中だけグリッド線を表示し、設置位置の見通しを良くする。

## Requirements

### Requirement 1: ドラッグ中のグリッド線表示
**Objective:** As a プレイヤー, I want 家具をドラッグしている間に配置先グリッドのマス目が見えること, so that どのマスに家具が収まるかを見ながら位置を決められる

#### Acceptance Criteria
1. When 家具のドラッグが開始される, the Home シーン shall 配置先グリッドのマス目を線で表示する
2. When 家具のドラッグが終了またはキャンセルされる, the Home シーン shall グリッド線を非表示にする
3. While 家具をドラッグしていない, the Home シーン shall グリッド線を表示しない
4. The グリッド線 shall `IsoGridSettingsView` の原点・セルサイズ・角度・グリッド幅・グリッド高さに一致する位置に描画される
5. The グリッド線 shall 実機ビルド (Android / iOS) でも表示される

### Requirement 2: 配置面に応じたグリッドの切り替え
**Objective:** As a プレイヤー, I want ドラッグ中の家具が置ける面のグリッドだけが表示されること, so that 関係のない面の線に惑わされない

#### Acceptance Criteria
1. While 床家具をドラッグしている, the Home シーン shall 床グリッドの線のみを表示する
2. While 壁家具をドラッグしている, the Home シーン shall 左壁・右壁の壁グリッドの線を表示し、床グリッドの線を表示しない
3. While 別の家具の上に置ける家具 (furniture-on-furniture) をドラッグしている, the Home シーン shall 床グリッドの線を表示する

### Requirement 3: 描画順と入力への非干渉
**Objective:** As a プレイヤー, I want グリッド線が家具や操作の邪魔をしないこと, so that 従来どおりの操作感で設置できる

#### Acceptance Criteria
1. The グリッド線 shall 床・壁の背景より手前、かつ配置済み家具およびドラッグ中の家具より奥に描画される
2. The グリッド線 shall タップ・ドラッグ入力を消費しない
3. While グリッド線が表示されている, the Home シーン shall 家具のドラッグ・スナップ・設置可否判定を従来どおり動作させる

### Requirement 4: 見た目の調整可能性
**Objective:** As a 開発者, I want グリッド線の色・太さ・透明度を Inspector で調整できること, so that 部屋の背景に合わせて視認性を後から詰められる

#### Acceptance Criteria
1. The グリッド線 View shall 線の色 (透明度含む) を Inspector から設定できる
2. The グリッド線 View shall 線の太さを Inspector から設定できる
3. If グリッド線 View がシーンに配置されていない, then the Home シーン shall エラーを出さずに家具のドラッグを従来どおり動作させる

### Requirement 5: 永続データへの非影響
**Objective:** As a 開発者, I want グリッド線が表示専用であること, so that セーブデータや配置ロジックに影響を与えない

#### Acceptance Criteria
1. The グリッド線表示 shall `IsoGridState` および家具配置のセーブデータを変更しない
2. The グリッド線表示 shall Redecorate モードの終了時に残存しない

### Requirement 6: 配置先の予告表示
**Objective:** As a プレイヤー, I want ドラッグ中に「今離すとどのマスに置かれるか」がマスの塗りで見えること, so that 離す前に配置結果と配置可否を判断できる

#### Acceptance Criteria
1. While 家具をドラッグしている, the Home シーン shall 現在の位置で離した場合に家具が占めるマス (フットプリント) の面を、周囲より明るく塗って表示する
2. When ドラッグ位置の変化でスナップ先のマスが変わる, the Home シーン shall 予告表示のマスを即座に追従させる
3. While フットプリントが配置可能である, the Home シーン shall 予告表示を配置可能を示す色 (通常色) で表示する
4. If フットプリント内に他の家具が占有しているマスが含まれる, then the Home シーン shall 予告表示全体を配置不可を示す色 (赤系) で表示する
5. If フットプリント内にグリッド範囲外のマスが含まれる, then the Home シーン shall グリッド範囲内のマスのみを配置不可を示す色で表示し、範囲外のマスは描画しない
6. If フットプリント全体がグリッド範囲外にある, then the Home シーン shall 予告表示を描画しない
7. While 別の家具の上に置ける位置をドラッグしている, the Home シーン shall その家具のグリッド (FragmentedIsoGrid) 上のマスに予告表示を行い、床グリッド上には行わない
8. While 壁家具をドラッグしている, the Home シーン shall スナップ先の壁グリッド上のマスに予告表示を行う
9. When 家具のドラッグが終了またはキャンセルされる, the Home シーン shall 予告表示を非表示にする
10. The 予告表示の配置可否判定 shall ドラッグ終了時の実際の配置可否判定 (`IsoGridService.CanPlace*`) と同じ結果になる
11. The 予告表示 shall 配置可能色・配置不可色を Inspector から設定できる
12. The 予告表示 shall グリッド線・配置済み家具・ドラッグ中の家具より手前に描画される
