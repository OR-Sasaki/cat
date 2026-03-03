# Requirements Document

## Introduction

本仕様は、家具（Furniture）の上に別の家具を配置可能にする機能を定義する。FragmentedIsoGridコンポーネントを新規実装し、IsoDragServiceと連携することで、家具の上面を配置可能なグリッド領域として扱えるようにする。

## Requirements

### Requirement 1: FragmentedIsoGridコンポーネント

**Objective:** As a プレイヤー, I want 家具の上面に別の家具を配置できるようにしたい, so that より自由度の高い部屋のカスタマイズができる

#### Acceptance Criteria

1. The FragmentedIsoGrid shall 家具オブジェクトにアタッチ可能なコンポーネントとして機能する
2. When FragmentedIsoGridがオブジェクトにアタッチされたとき, the system shall 2D Colliderも同時にアタッチされている状態を保証する
3. The FragmentedIsoGrid shall 配置可能なグリッド領域を定義する
4. The FragmentedIsoGrid shall 配置可能領域のサイズを持つ

### Requirement 2: ドラッグ終了時の配置判定

**Objective:** As a プレイヤー, I want ドラッグした家具をFragmentedIsoGridの上に配置したい, so that 家具を家具の上に置ける

#### Acceptance Criteria

1. When IsoDragServiceのEndFloorDrag()が呼び出されたとき, the system shall RayCastを実行してFragmentedIsoGridとの衝突をチェックする
2. When RayCastがFragmentedIsoGridに衝突したとき, the system shall そのFragmentedIsoGridへ家具を配置する
3. If RayCastがFragmentedIsoGridに衝突しなかったとき, the system shall 通常のフロア配置処理を継続する

### Requirement 3: 配置制約の適用

**Objective:** As a システム, I want FragmentedIsoGridに配置できる家具を制限したい, so that 壁配置用家具が誤って配置されない

#### Acceptance Criteria

1. When IsWallPlacementがtrueの家具をFragmentedIsoGridに配置しようとしたとき, the system shall 配置を拒否する
2. When IsWallPlacementがfalseの家具をFragmentedIsoGridに配置しようとしたとき, the system shall 配置を許可する
3. When 家具のfootprintがFragmentedIsoGridのサイズを超えるとき, the system shall 配置を拒否する
4. If 配置が拒否されたとき, the system shall 適切なフィードバックを提供する
