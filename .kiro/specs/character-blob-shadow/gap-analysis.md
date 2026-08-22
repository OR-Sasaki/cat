# Gap Analysis: character-blob-shadow

分析日: 2026-08-22

## 1. 現状調査 (Current State)

### キャラクター構成 (実測)

| 資産 | パス | 役割 |
| --- | --- | --- |
| `CharacterView.prefab` (基底) | `Assets/Arts/Character/TestScene/CharacterView.prefab` | パーツ SpriteRenderer 16個 + `CharacterView` コンポーネント。`Root` 子以下にパーツ群 |
| `Character.prefab` (Home用) | `Assets/UI/Home/Prefabs/Character.prefab` | 上記をネスト。`CharacterWalk` + `NavMeshAgent` + `Flipper` (左右反転) を持つ。**Home.unity が使用** |
| `Character Variant.prefab` (Timer用) | `Assets/UI/Timer/Prefabs/Character Variant.prefab` | Character.prefab のバリアント。**Timer.unity が使用** |
| `Character.prefab` (Resources) | `Assets/Resources/Character.prefab` | CharacterView をネストするが、シーン・コードからの参照を確認できず (用途不明) |

### 描画順の実態

- ソートレイヤーは `Default` と `UI` の2つのみ (TagManager 確認済)
- キャラクターパーツは **Default レイヤー、sortingOrder 100〜1600** (`OutfitPartOrderSetting.GetOrder × 100`、最小は FrontFootLine=100... 実測プレハブでは 100〜1600)
- `CharacterView.SetOutfit` は着せ替え時に各パーツの `sortingOrder = order * 100` を**再設定する**が、対象は SerializeField された16個の SpriteRenderer のみ。影用に追加した SpriteRenderer には触れない → **Requirement 4 は構造的に自動達成**
- Home の家具は `SortingGroup` + `(x+y)*1000 + x` の動的 order (`IsoDraggableView.CalculateFragmentedSortingOrder`)

### 跳ね (バウンス) の実態

- `Walk.anim` / `Run.anim` は `Root` (CharacterView 直下の子) とその配下をアニメーションさせる
- 移動そのもの (`NavMeshAgent`) はプレハブルートを水平移動させ、跳ねは `Root` の localPosition で表現
- → **`Root` の兄弟 (CharacterView 直下) に影を置けば、跳ねの影響を受けず自動的に接地し続ける**。追加コードなしで「影は地面に残る」が成立する
- 左右反転は `Flipper` (CharacterView の親) の localScale.x 反転。影が Flipper 配下にあると一緒に反転するが、左右対称な楕円ならば実害なし (オフセット X≠0 を使う場合のみ注意)

## 2. Requirement-to-Asset Map

| 要件 | 既存資産 | ギャップ |
| --- | --- | --- |
| 1.1 足元に楕円影 | なし | **Missing**: 影スプライト (ソフトエッジ楕円) のテクスチャ資産が存在しない |
| 1.2 Home/Timer 両対応 | ネストプレハブ構造 | なし: 基底 `CharacterView.prefab` への追加が両シーンへ自動伝播 |
| 1.3 キャラより背面・床より前面 | sortingOrder 100〜1600 (Default) | **Unknown**: 床・背景 (Home RoomBackGround / Timer BackgroundScrollView) のレイヤーと order 未確認。影は order < 100 が候補 |
| 1.4 全シーン同一見た目 | 同上 | なし (基底プレハブ一元管理で担保) |
| 2.1-2.2 追従 | Transform 親子関係 | なし: 子オブジェクトなら自動追従 |
| 2.3 非表示連動 | GameObject Active 階層 | なし: 子オブジェクトなら自動連動 |
| 3.1-3.4 サイズ・不透明度・オフセット調整 | Transform / SpriteRenderer.color | **Constraint**: Transform scale・color.a・localPosition で素の調整は可能。要件の粒度 (横幅・縦幅・不透明度・オフセットを明示的に) を満たすには専用コンポーネントの有無を設計で判断 |
| 4.1-4.2 着せ替え独立 | `CharacterView.SetOutfit` の実装 | なし: 影 Renderer は SetOutfit の対象外 (前述) |

## 3. 実装アプローチ選択肢

### Option A: プレハブのみ (コードなし)
基底 `CharacterView.prefab` に影スプライト子オブジェクト (`Root` の兄弟) を追加するだけ。調整は Transform scale / localPosition / SpriteRenderer color で行う。

- ✅ 追加コードゼロ。全要件の AC を構造的に満たす
- ✅ 両シーンへ自動伝播、着せ替え・跳ね・非表示すべて自動対応
- ❌ 「横幅・縦幅・不透明度」という語彙での調整 UI はなく、Transform 数値の読み替えが必要
- ❌ 将来の跳ね連動 (縮小・減光) を入れる時にどのみちコンポーネントが要る

### Option B: 薄い View コンポーネント追加
Option A の構成に加え、`Cat.Character.BlobShadowView` (MonoBehaviour) を影オブジェクトへ付与。`[SerializeField]` で幅・高さ・不透明度・オフセットを持ち、`OnValidate` + 初期化時に SpriteRenderer / Transform へ反映。

- ✅ Requirement 3 の語彙どおりの Inspector 調整。アーティストの追い込みが容易
- ✅ 将来の跳ね連動 (高さ→スケール) の受け皿になる
- ❌ ロジックを持たない View が1ファイル増える (プロジェクト規約上は View 層として自然)

### Option C: ハイブリッド (段階導入)
まず Option A で表示を成立させ、アート調整の要望が出た時点で Option B のコンポーネントを後付けする。

- ✅ 最小コストで先に画面へ出せる
- ❌ 2段階の作業になり、spec としてはタスクが不定形になる

## 4. 工数・リスク

- **Effort: S (1日未満〜1日)** — 既存パターン内の作業。新規はテクスチャ1枚 + (Option B なら) View 1ファイル
- **Risk: Low** — 依存はプレハブ構造と sortingOrder のみ。DI・State・永続化に一切触れない

## 5. 設計フェーズへの推奨事項

**推奨アプローチ**: Option B (薄い View コンポーネント)。Requirement 3 の AC (サイズ・不透明度・オフセットの明示的調整) を語彙どおり満たし、コスト増はごく小さい。表示だけ先行させたい場合は Option A でも全 AC を解釈上満たせる。

**設計時の決定事項**:
1. 影の sortingOrder 値 (候補: Default レイヤーで 0〜50 の固定値)
2. 影の追加先はネストの基底 `CharacterView.prefab` (Home/Timer 同時対応の単一変更点)
3. 影スプライトの入手方法 (ソフトエッジ楕円テクスチャの新規作成)

**Research Needed (設計フェーズで確認)**:
- [ ] Home の床 (RoomBackGround 配下スプライト) と Timer の背景 (BackgroundScrollView) の sortingLayer / sortingOrder 実値 → 影 order の決定材料
- [ ] Home で家具 (SortingGroup, order最大数千) とキャラクターの前後関係の現行仕様 → 影が家具に対してどう見えるべきか
- [ ] `Assets/Resources/Character.prefab` の用途 (未使用なら影対応の対象外でよいか)
- [ ] `Flipper` 反転時の影オフセット挙動 (オフセット X を使う場合のみ)
- [ ] 影テクスチャの仕様 (サイズ・グラデーション) をアートへ確認、またはプレースホルダー自作
