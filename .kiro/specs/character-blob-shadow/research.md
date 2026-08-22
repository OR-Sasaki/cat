# Research & Design Decisions: character-blob-shadow

## Summary
- **Feature**: `character-blob-shadow`
- **Discovery Scope**: Simple Addition (既存キャラクタープレハブへの表示要素追加)
- **Key Findings**:
  - アニメーションの作用点は2階層: Walk/Run の跳ねは `Root` 子、**Idle/Rest/RestLoop は空パスカーブで `CharacterView` ノード自体の localScale/localPosition を動かす** (Codex レビューで発覚)。影の配置は両方の影響外である `Flipper` 直下が正解
  - **`Character.prefab` ルートに SortingGroup がある** (Home: order 0、Timer シーンで 5 に上書き、Complete 演出中 8)。影の order 50 はグループ内の相対順としてのみ機能し、床・背景との前後はグループ order の既存挙動を継承する
  - Timer 用 `Character Variant.prefab` は Home 用 `Character.prefab` のバリアント (実測確認)。Home 用への追加1箇所で Timer へ伝播する

## Research Log

### 床・背景の描画順 (gap-analysis の Research Needed ①)
- **Context**: 影の sortingOrder を決めるため、キャラより背面・床より前面 (1.3) の実値裏取りが必要
- **Sources Consulted**: `Assets/Scenes/Home.unity` / `Assets/Scenes/Timer.unity` の `m_SortingOrder` 全数調査、`IsoDragService` / `IsoDraggableView` / `FurniturePlacementService`
- **Findings**:
  - Home.unity 内の直接配置レンダラーに背景系 order は存在せず (1000 の Canvas のみ)、床 (Base) は `FurniturePlacementService.PlaceBase` が動的配置し、壁配置は SortingGroup order 0
  - Timer.unity の背景スクロールスプライトは order 0 / 3 / 10 / 15
  - ソートレイヤーは `Default` / `UI` の2つのみ (TagManager 実測)。キャラも背景も Default
- **Implications**: 影は Default レイヤー・**固定 order 50** とする。~~背景最大 15 < 50 < キャラ最小 100~~ → **訂正 (Codex レビュー後)**: `Character.prefab` ルートに SortingGroup (Home 0 / Timer 5) があるため、order 50 は「グループ内でキャラパーツ (100〜1600) より背面」の保証のみを担う。床・背景との前後はグループ order による既存挙動 (キャラが床より前に見えている状態) をそのまま継承する

### Home 家具 (SortingGroup) との前後関係 (Research Needed ②)
- **Context**: 家具の order は `(x+y)*1000 + x` で数千に達する。影と家具の見え方の確認
- **Findings**: 家具 order は原点セル以外で 1000 以上となり、キャラパーツ (最大1600) をも上回る場合がある。これは既存挙動であり本機能のスコープ外
- **Implications**: 影 (order 50) は家具より常に背面 = 「床のデカール」として一貫した意味論になる。家具とキャラの前後問題は本 spec では扱わない

### `Assets/Resources/Character.prefab` の用途 (Research Needed ③)
- **Findings**: シーン・コード (`Resources.Load`)・Addressables 設定のいずれからも参照を確認できず。ただし基底 `CharacterView.prefab` をネストしているため、基底への影追加で自動的に追従する
- **Implications**: 個別対応は不要。削除判断は本 spec のスコープ外

### 跳ね・移動・反転の構造 (Research Needed ④)
- **Findings**:
  - `Walk.anim` / `Run.anim` のアニメーションパスは `Root` および `Root/*` のみ。跳ねは `Root` の localPosition で表現される
  - **`Idle.anim` / `Rest.anim` / `RestLoop.anim` は空パスカーブを含み、Animator の付いた `CharacterView` ノード自体の localScale (Rest は概ね X=1.06 / Y=0.9 まで) と localPosition をアニメーションする** (Codex レビューで発覚)
  - 水平移動は `NavMeshAgent` がプレハブルートを動かす。左右反転は `CharacterWalk` が `Flipper` の localScale.x を反転する
- **Implications**: ~~影を `CharacterView` 直下に置く~~ → **訂正**: CharacterView 直下では Idle の呼吸・Rest の寝そべりスケールを影が継承して変形する (1.4 違反)。配置は **`Flipper` 直下 (`CharacterView` の兄弟)** とし、跳ね (Root) とノードスケール (CharacterView) の両方の影響外に置く。移動追従 (ルート) と反転 (Flipper) は継承する。オフセット X≠0 は反転時に左右へ振れる制約を設計に明記する

### Timer 完了演出の影響 (Codex レビューで発覚)
- **Context**: `CompleteCharacterPopView` (Timer) の存在を初回調査で見落としていた
- **Findings**: 完了演出はキャラクタールートを `_popScaleMultiplier` (4.2) 倍に拡大し、SortingGroup order を 5→8 へ変更して「飛び出し」を演出する。影も一緒に4.2倍化・上昇する
- **Implications**: pop 開始時 (SortingGroup 変更と同じ箇所) に影 GameObject を `SetActive(false)` する変更を `CompleteCharacterPopView` へ追加する。null 許容 (未配線時は何もしない)。演出後の復帰は不要 (キャラは再表示されない)

### 影テクスチャ (Research Needed ⑤)
- **Findings**: プロジェクト内にソフトエッジ楕円/円のスプライト資産は存在しない
- **Implications**: ソフトエッジの白い放射状グラデーション円テクスチャ (256×256, 白→透明) を新規作成し、SpriteRenderer の color (黒 + α) で色味を制御する。白テクスチャにしておくことで色変更を Inspector で完結できる。アートから正式素材が来たら差し替えのみで対応可能

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| A: プレハブのみ | 影スプライト子を追加、Transform/color で調整 | コードゼロ | 調整語彙が要件 (幅・高さ・不透明度・オフセット) と一致しない | gap-analysis 参照 |
| B: 薄い View コンポーネント | A + `BlobShadowView` で調整値を Inspector 公開 | 要件 3 を語彙どおり満たす。将来の跳ね連動の受け皿 | View ファイル1つ追加 | **採用** |
| C: 段階導入 | A で先行し必要時に B | 最小先行 | 2段階作業で spec タスクが不定形 | 不採用 |

## Design Decisions

### Decision: 影は Home 用 `Character.prefab` の `Flipper` 直下に配置する (Codex レビュー後に改訂)
- **Context**: 当初は基底 `CharacterView.prefab` の `Root` 兄弟としていたが、Idle/Rest/RestLoop が CharacterView ノード自体をスケールする事実が判明し、その配下では影が変形する
- **Alternatives Considered**:
  1. 基底 `CharacterView.prefab` の `Root` 兄弟 — Idle/Rest スケールを影が継承して変形 (棄却)
  2. Rest のルートスケールカーブを `Root` 側へ移す — アニメーション資産の改変はアート領域に踏み込みリスクが大きい (棄却)
- **Selected Approach**: `Character.prefab` (Home 用) の `Flipper` 直下、`CharacterView` の兄弟に `BlobShadow` 子を追加。Timer 用 Variant へはプレハブ継承で伝播
- **Rationale**: 跳ね (Root) / ノードスケール (CharacterView) の両方の影響外で、移動 (ルート)・反転 (Flipper)・Active 連動は継承される
- **Trade-offs**: 基底 `CharacterView.prefab` 単体 (TestScene) には影が付かないが、プレイヤーが目にする画面は Home / Timer のみなので許容
- **Follow-up**: Variant およびシーンインスタンスのオーバーライドで子が除去されていないことを実装時に確認

### (旧記録・棄却) Decision: 影は基底 `CharacterView.prefab` の `Root` 兄弟に配置する
- **Context**: Home / Timer 両対応 (1.2)・跳ね時の接地・追従 (2.1, 2.2)・非表示連動 (2.3)
- **Alternatives Considered**:
  1. 各シーンの Character プレハブへ個別追加 — 変更点が2箇所以上に分散
  2. 独立 GameObject + 追従スクリプト — 追従・非表示連動のコードが必要になり複雑化
- **Selected Approach**: 基底プレハブに `BlobShadow` 子 (SpriteRenderer + BlobShadowView) を追加。`Root` の兄弟のため跳ねアニメの影響を受けない
- **Rationale**: 親子関係だけで 1.2 / 2.1 / 2.2 / 2.3 / 4.1 が構造的に成立し、コード量が最小
- **Trade-offs**: `Flipper` 反転時にオフセット X が左右反転する (対称楕円 + X=0 運用で回避)
- **Follow-up**: Home / Timer 両シーンでの見た目確認 (実機/エディタ)

### Decision: sortingOrder は Default レイヤー固定値 50
- **Context**: 1.3 (キャラより背面・床より前面)、4.2 (着せ替え後も維持)
- **Selected Approach**: プレハブ上で SpriteRenderer に order 50 を設定。`CharacterView.SetOutfit` は SerializeField 済み16パーツしか触らないため、影の order は着せ替えで変化しない
- **Rationale**: 背景最大 15 < 50 < キャラ最小 100 の実測に基づく余裕を持った中間値
- **Trade-offs**: 将来床側 order が 50 以上になった場合は再調整が必要 (BlobShadowView では order を管理せずプレハブ設定とする)

### Decision: 調整コンポーネント `Cat.Character.BlobShadowView` を新設
- **Context**: 3.1〜3.4 (幅・高さ・不透明度・オフセットのエディタ調整と反映)
- **Selected Approach**: SerializeField (幅・高さ・不透明度・オフセット) を持ち、`OnValidate` と `Awake` で Transform / SpriteRenderer へ反映する受動 View。判断ロジックなし・DI 不要 (`autoInjectGameObjects` にも登録しない)
- **Rationale**: プロジェクトの View 層規約 (受動・ロジックなし) に適合。`AudioPlayerView` と同じ「受動ホスト」パターン
- **Trade-offs**: UnityEngine 依存のため EditMode 純ロジックテストの対象外 (検証は目視)。値→Transform 変換は自明な写像でありテスト分離するほどのロジックがない

### Codex 外部レビュー (2026-08-22, gpt-5.6-sol / reasoning high)
- 指摘6件のうち実測で裏付けが取れた3件 (SortingGroup の見落とし・Rest/Idle の空パススケール・Complete 演出の4.2倍化) を設計へ反映した
- 「2.3 の非表示の定義が狭い」指摘は、定義の明文化 (祖先 GameObject の非アクティブ化 + 完了演出の明示非表示) で対応
- `BlobShadowView` の構成エラー処理 (`RequireComponent` / 自動取得 / `[Min(0f)]` / 単位定義) と Timer 実態を反映した検証項目の拡充も採用

## Risks & Mitigations
- 床・家具側の order 変更で影が埋もれる/浮く — order 50 の根拠を design.md に明記し、変更時の再調整ポイントを一元化
- `Character Variant.prefab` (Timer) が該当子をオーバーライドで消し込む可能性 — 実装時に Variant のオーバーライドを確認し、両シーンで目視検証
- 影テクスチャの品質がアート基準に満たない — 白テクスチャ + color 制御にして差し替えコストを最小化

## References
- `.kiro/specs/character-blob-shadow/gap-analysis.md` — 実測調査の全記録
- `.kiro/steering/structure.md` — View 層規約・プレハブ配置規約
- `.kiro/steering/tech.md` — コーディング規約 (private省略・`_camelCase`・`/// comment`)
