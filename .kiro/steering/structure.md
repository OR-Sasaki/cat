# Project Structure

## Scene Structure

`Assets/Scripts/{SceneName}/`
`Manager/` オーケストレーション・`Scope/` VContainer 設定・`Service/` ビジネス操作・`Starter/` IStartable・`State/` 状態データ・`View/` MonoBehaviour UI
薄いシーンは必要な層だけ置けば足りる (`Title/` は Scope + View のみ)。大規模機能もシーンフォルダ内の層に統合する (IsoGrid / Closet / Redecorate はすべて `Home/`)

## Root Services

`Assets/Scripts/Root/` (シーンと同じ層構造)
- **登録の単一ソース**: `Root/Scope/RootScope.cs` に共通サービスを `Lifetime.Singleton` 登録。一覧は書かず現物を参照する
- **シーン横断の見た目**: Outfit のロード / キャッシュ (`OutfitAssetService` / `OutfitAssetState`) と適用 (`CharacterOutfitService`) は Root 常駐。`CharacterView` を持つシーンが `CharacterOutfitStarter` を `RegisterEntryPoint` する。未装備部位は `default_outfits.csv` で補完
- **所持アイテムの単一ソース**: `UserItemInventoryService` (PlayerPrefs・数量ベース)。初回付与は `InitialItemService` のみ。個体単位の `UserFurnitureId` は `UserFurnitureInstanceService` が所持数から決定論的に採番 (`FurnitureId * SlotStride + slot + 1`)
- **Root 常駐の受動 View**: 判断を持たない共通 View (`AudioPlayerView` 等) は `RootScope.prefab` の子に置いて DDoL させ、`[SerializeField]` + `RegisterComponent` で注入する
- **DI 経路外**: 動的生成ボタン等は `AudioServiceHandle` の静的ブリッジ経由で `IAudioService` に到達する (`RegisterBuildCallback` で初期化)

## Patterns

### Testable Logic Assemblies
決定論的な純粋ロジックは `{Scene|Root}/{Feature}Logic/` の独立アセンブリ (`.asmdef`, `noEngineReferences: true`) に切り出し、`Tests/` に EditMode テストアセンブリ (`{Name}.Tests`, `includePlatforms: [Editor]`, `defineConstraints: [UNITY_INCLUDE_TESTS]`) を同居させる。座標は自前の値型 (`GridCell`) で受け、グリッド状態の問い合わせは `Func` で注入する。Unity 型との変換は呼び出し側 Service が担う
例: `Shop/RewardAdLogic/`、`Root/AudioLogic/`、`Home/GridPreviewLogic/`、`Home/OutfitEffectLogic/`

### In-Scene Effect / Preview Views
ワールド空間の演出・プレビューは「判断を持たない View」として View 層に置き、`RegisterComponent` で登録する。Service は結果 (何をどこに描くか / いつ再生するか) だけ渡す
- 演出 View は `Play(対象, ...)` の 1 メソッドのみ公開。状態変更を演出のピークに合わせるならコールバックで受ける (`OutfitChangeEffectView.Play(character, swapOutfit)`)
- View 欠落でも機能を止めたくない場合は View 側が `[Inject] Init` で `service.AttachView(this)` する (`GridPreviewView` → `GridPreviewService`)
- 同じ判定結果を複数が使う場合は、生成元を 1 つに絞った readonly struct で渡す (`DropTarget` は `IsoDragService` だけが生成)

### Dialog-based Feature Folders
シーンではないがシーン構造に準じるフォルダ (`TimerSetting/`, `Menu/`, `DebugPanel/`)
- プレハブは `Assets/UI/Dialog/{型名}.prefab`、`BaseDialog.prefab` のネストプレハブ。`Assets/UI/Dialog` が Addressables のフォルダエントリ (address: `Dialogs`) なので個別登録は不要
- UI をコード生成するダイアログはプレハブを器 (RectTransform + CanvasGroup + 本体スクリプト) に留め `[Inject] Construct` で組み立てる (`DebugPanelDialog` + `DebugUiFactory`)。`BaseDialogView` の `_animator` / `_closeButton` 未設定なら開閉アニメはスキップ
- 開くボタンは各シーンの View 層に置き `IDialogService.OpenAsync<TDialog>` を呼ぶ

### Screen Safe Area
画面端に寄せた UI (戻るボタン / ヘッダー / フッター / タブ) は Canvas 直下の `SafeArea` 配下に置く。`SafeAreaView` (`Root/View/`) が `Screen.safeArea` を毎フレーム anchor へ反映する。全画面背景だけは `SafeArea` の外でノッチ下まで塗る。Android は `androidRenderOutsideSafeArea: 1` でカットアウト内まで描画するため対応漏れがそのまま欠けになる。コード生成する Canvas (`DebugOpenButtonView` 等) も `SafeArea` を 1 枚挟む。中央アンカーのみのシーン (Title / Logo / Timer) は対象外、確認は Device Simulator

### Root-Resident Overlay Effects
全シーン常駐の演出 (タップエフェクト等) は View のみの機能フォルダ + 専用 Overlay Canvas プレハブを `RootScope.prefab` にネストする。sortingOrder は最前面 (TapEffect 30000 > ダイアログ 1000+ > フェード 999)。GraphicRaycaster を付けず `raycastTarget = false` で入力を奪わない

### Debug-Only Features
RegisterEntryPoint を `#if UNITY_EDITOR || DEVELOPMENT_BUILD` で囲み、リリースでは起点ごと消す。クラス本体は条件コンパイルしない (プレハブのスクリプト参照が切れる)。DebugPanel の透明ボタン (画面左上) は raycast を奪うため各シーン左上の UI と重ねない (Shop / History の戻るボタンはセーフエリア上端から 90px 以降)。Canvas の sortingOrder は DialogCanvas (1000) より下

### Demo / Sandbox Scenes
演出の比較検討用シーン (`Assets/Scenes/EffectDemo.unity` + `Assets/Scripts/EffectDemo/`) は DI・層構造の対象外で、`Const.SceneName` にもビルドにも含めない。採用案は本番の View 層へ書き直して移す (デモのコードを本番から参照しない)

## Naming / Dependency

- Files / Classes: PascalCase でシーン名 + 役割 (`HomeScope`, `TitleStarter`)
- Fields: `_camelCase` (private) / `PascalCase` (public)。Constants: PascalCase (static class 内)
- Namespace: `{SceneName}.{Layer}` (`Home.Service`) またはプロジェクト共通の `Cat`
- シーン名は `Assets/Scripts/Utils/Const.cs` の定数のみ使う (マジックストリング禁止)

```
View  →  Service  →  State
  ↓          ↓
Starter    Manager
```
上位層 → 下位層のみ許可。逆方向 (State → Service, Service → View) は禁止

## Scene View への注入 (3 経路)

- **他層から解決される View**: Scope の `[SerializeField]` + `RegisterComponent(_view)` (`HomeUiView`)。Service がコンストラクタで受け取れる
- **自分で完結する View**: Scope の `autoInjectGameObjects` に登録し、View 側に `[Inject] public void Init(...)` (`HomeFooterView`)。親 GameObject 単位で再帰注入されるため、登録済み祖先の子を重複登録しない
- **動的生成ダイアログ**: `DialogContainer` が Instantiate 後に `InjectGameObject` するので登録不要。`[Inject] Construct` は `Awake` の後に走る

## Assets (非自明なもののみ)

- `Assets/Arts/Character/Scripts/` — `CharacterView`, `Outfit` 系, `BlobShadowView` (足元の丸影)
- `Assets/Resources/` — `Resources.Load` 対象 (マスター CSV, 各種 Config SO)
- `Assets/ImportedAssets/` / `Assets/LevelPlay/` — 外部アセット / SDK
- `Assets/Settings/VContainer/` — VContainerSettings.asset

---
_パターンのみ記す。パターンに従う新規ファイルは本書の更新を要しない_
