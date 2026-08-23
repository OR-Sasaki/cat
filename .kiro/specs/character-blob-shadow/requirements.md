# Requirements Document

## Project Description (Input)
character足元の丸影

## Introduction
キャラクター (`CharacterView`) の足元に丸影 (ブロブシャドウ) を表示する機能。キャラクターが地面に接地している感覚を与え、シーン内での存在感・視認性を高める。CharacterView が配置される全シーン (現状 Home / Timer) で一貫した見た目を提供する。

## Requirements

### Requirement 1: 丸影の表示
**Objective:** プレイヤーとして、キャラクターの足元に丸影が表示されてほしい。それによりキャラクターが地面に接地しているように見え、画面の見栄えが向上する

#### Acceptance Criteria
1. When キャラクターがシーン上に表示された時, the 丸影表示機能 shall キャラクターの足元に楕円形の丸影を表示する
2. The 丸影表示機能 shall CharacterView が配置される全シーン (Home / Timer) で丸影を表示する
3. The 丸影表示機能 shall 丸影をキャラクター本体 (全ての衣装パーツを含む) より背面、かつ床・背景より前面に描画する
4. The 丸影表示機能 shall 全シーンで同一の見た目 (形状・色・不透明度) の丸影を表示する

### Requirement 2: キャラクターへの追従
**Objective:** プレイヤーとして、キャラクターが動いても丸影が足元に付いてきてほしい。それにより影とキャラクターの位置がずれて見える違和感をなくせる

#### Acceptance Criteria
1. When キャラクターの位置が変化した時, the 丸影表示機能 shall 丸影をキャラクターの足元位置に追従させる
2. While キャラクターが移動している間, the 丸影表示機能 shall 丸影を足元からずれのない位置に表示し続ける
3. If キャラクターが非表示になった場合, then the 丸影表示機能 shall 丸影も非表示にする

### Requirement 3: 見た目の調整
**Objective:** 開発者として、丸影のサイズ・不透明度・足元からのオフセットを調整できるようにしたい。それによりコード変更なしにアートの要求に合わせて見た目を追い込める

#### Acceptance Criteria
1. The 丸影表示機能 shall 丸影のサイズ (横幅・縦幅) を Unity エディタ上で調整可能にする
2. The 丸影表示機能 shall 丸影の不透明度を Unity エディタ上で調整可能にする
3. The 丸影表示機能 shall キャラクター基準位置から丸影までのオフセットを Unity エディタ上で調整可能にする
4. When 調整値が変更された時, the 丸影表示機能 shall 変更後の値を丸影の表示に反映する

### Requirement 4: 着せ替えとの独立性
**Objective:** プレイヤーとして、衣装を着せ替えても丸影が変わらず表示されてほしい。それにより着せ替え操作中も見た目の一貫性が保たれる

#### Acceptance Criteria
1. When キャラクターの衣装 (Outfit) が変更された時, the 丸影表示機能 shall 丸影の表示を維持する
2. The 丸影表示機能 shall Outfit の適用・解除処理の影響を受けずに丸影の描画順 (キャラクターより背面) を維持する
