# ギャップ分析: user-inventory-management

## スコープ前提

- **本スペックのゴール**: `IUserItemInventoryService` / `IUserPointService` のインターフェース (および最小限の実装) を RootScope に整備する。
- **対象外**: 既存モック (`UserState.UserOutfits` / `UserState.UserFurnitures` / `ShopState.YarnBalance`) の置き換え、および既存参照元 (`RedecorateScrollerService` / `IsoGridLoadService` / `ShopService`) の改修。これらは別タスクで実施する。
- したがって、本分析では「既存コードとの整合性」は制約として扱わず、**新規に追加する API 境界の妥当性**に焦点を置く。

## エグゼクティブサマリー

- 既存の `UserState.UserOutfits[]` / `UserState.UserFurnitures[]` / `ShopState.YarnBalance` はモック実装。本スペックで用意する Service/State が正式版の位置付けになる。
- **新規作成パターンは既存資産 (`UserEquippedOutfitService`, `PlayerPrefsService`) に準拠可能** — DI・永続化・イベント通知すべて確立済みのパターンで実装できる。
- **家具の所持モデル (数量 vs インスタンス) は自由設計が可能** — 既存モックは instance-based (`Id`+`FurnitureID`) だが、正式版では再設計できる。設計フェーズでの決定事項。
- **サーバ通信層 (ApiClient/ApiErrorHandler) は存在しない** — ローカル限定制約と整合。
- **通知パターンは `Action` / `UnityEvent`** — Observable は不使用。新サービスも同パターンに従う。

## 1. 現行コードベースの状態

### 1.1 既存モック (参考: 本スペックでは改修しない)

| クラス | ファイル | 位置付け |
|--------|----------|----------|
| `UserOutfit[]` in `UserState` | `Root/State/UserState.cs` | モック所持データ (CSV ロード) |
| `UserFurniture[]` in `UserState` | `Root/State/UserState.cs` | モック所持データ (`Id`+`FurnitureID` の instance-based) |
| `ShopState.YarnBalance` | `Shop/State/ShopState.cs` | モック毛糸残高 (初期値 10000) |

### 1.2 流用可能な既存資産

| 資産 | ファイル | 流用方針 |
|------|----------|----------|
| `UserEquippedOutfitService` | `Root/Service/UserEpuippedOutfitService.cs` | Service + State + PlayerPrefs 永続化の参照実装 |
| `PlayerPrefsService` | `Root/Service/PlayerPrefsService.cs` | `JsonUtility` ベースの永続化 API |
| `PlayerPrefsKey` enum | `Root/Service/PlayerPrefsService.cs` | キーの新規追加が必要 (`UserItemInventory`, `UserPoint` など) |
| `RootScope` | `Root/Scope/RootScope.cs` | Singleton 登録先 |
| `MasterDataState` | `Root/State/MasterDataState.cs` | 家具 ID / 着せ替え ID のバリデーション参照元 |
| `UserEquippedOutfitState` | `Root/State/UserEquippedOutfitState.cs` | 装備整合性チェックの参照元 (R3-6) |

### 1.3 既存パターン

**永続化**: `UserEquippedOutfitService` 参照
- コンストラクタで `Load()` 呼び出し、状態変更時に `Save()`
- 独自 DTO (`UserEquippedOutfitData`) を `JsonUtility` で直列化

**通知**: `Action` / `Action<T>` / `UnityEvent<T>`
- Observable / UniRx / R3 は未使用

**DI 登録**:
```csharp
builder.Register<UserEquippedOutfitState>(Lifetime.Singleton);
builder.Register<UserEquippedOutfitService>(Lifetime.Singleton);
```

## 2. 要件とのギャップ

| 要件 | ギャップ種別 | 備考 |
|------|--------------|------|
| R1 (Item 初期化) | Missing | PlayerPrefs 未保存時は空で初期化、保存済みなら復元 |
| R2 (家具) | Missing / Unknown | 所持モデル (数量 vs インスタンス) を設計フェーズで決定 |
| R3 (着せ替え) | Missing | `UserEquippedOutfitState` 整合チェックは新規ロジック |
| R4 (Point 初期化) | Missing | 残高 0 で初期化、保存済みなら復元 |
| R5 (毛糸) | Missing | 桁あふれ対策 (丸め or エラー) を決定必要 |
| R6 (変更通知) | Missing | `Action` イベントで実装 (既存パターン踏襲) |
| R7 (永続化) | Missing | バージョン管理付き DTO を新規パターンとして導入 |
| R8 (DI / 層構造) | 既存パターン踏襲 | ギャップなし |

### 2.1 設計フェーズで決定すべき事項

#### 家具の所持モデル (R2)
- **案 α**: 数量管理 (`Dictionary<uint, int>` — FurnitureID → count)
  - ✅ 要件 R2 の「所持数」表現に自然、API がシンプル
  - ❌ IsoGrid 配置のような個体識別が必要になった際に拡張コスト
- **案 β**: インスタンス管理 (`List<FurnitureInstance>` — 各々が一意 Id を持つ)
  - ✅ 配置情報との親和性が高い (将来の別タスク移行時にスムーズ)
  - ❌ 「所持数」はカウント集計となり API が一段重い
- **案 γ**: 両対応 (内部はインスタンス、`GetCount(id)` などの集計 API を提供)
  - ✅ 外部から見れば数量 API、内部はインスタンス
  - ❌ 実装コスト増

**Research Needed**: 本スペックは「インターフェース整備」が主目的のため、将来の別タスクで `UserState.UserFurnitures` (instance-based) を移行することを見据えると **案 γ または案 β** が有力。

#### 毛糸残高の桁あふれ処理 (R5-7)
- **案 A**: `int.MaxValue` を超える加算は上限で丸め、正常結果を返す
- **案 B**: 不正引数エラーとして扱い、残高不変
- **推奨**: 案 B (通貨系は暗黙の丸めより明示エラーが安全)

#### PlayerPrefs スナップショットのバージョン形式 (R7)
- DTO に `int Version` フィールドを含める
- 非互換バージョンは破棄 + 空状態初期化 (既存 `IsoGridSaveData` に先例なし → 新規パターン)

## 3. 実装アプローチ

### Option A: 既存モック拡張
既存 `UserState` 配列 / `ShopState.YarnBalance` をそのまま活用し Service のみ追加。

**評価**: **非推奨** — ユーザー明示のスコープ (モックとは別に正式版を整備) に反する。

### Option B: 新規コンポーネント作成 **[推奨]**

新規ファイル:
```
Root/
├── Service/
│   ├── IUserItemInventoryService.cs
│   ├── UserItemInventoryService.cs
│   ├── IUserPointService.cs
│   └── UserPointService.cs
├── State/
│   ├── UserItemInventoryState.cs
│   └── UserPointState.cs
└── Service/PlayerPrefsService.cs (enum に 2 キー追加)
```

既存ファイルへの変更:
- `RootScope.cs`: 4 行追加 (State + Service × 2)
- `PlayerPrefsService.cs`: `PlayerPrefsKey` enum に 2 エントリ追加

**既存モックとの共存**:
- `UserState.UserOutfits` / `UserState.UserFurnitures` / `ShopState.YarnBalance` は**そのまま残す**
- 別タスクで段階的に移行する前提
- 本スペックでは新サービスが**独立して動作**することのみ検証

**トレードオフ**:
- ✅ スコープ明確、既存機能への影響ゼロ
- ✅ 層構造遵守、テスタブル
- ✅ 将来の移行タスクで段階的に旧コードを差し替え可能
- ❌ 一時的にモックと正式版の二重管理状態になる (既知のトレードオフ)

### Option C: ハイブリッド
**評価**: 本スペックのスコープ制約により該当ケースなし。

## 4. 努力・リスク評価

| 項目 | 評価 | 根拠 |
|------|------|------|
| **工数** | **S (1〜3日)** | 既存パターン踏襲 + 新規 4 クラス + PlayerPrefs 連携。ユニットテスト含む |
| **リスク** | **Low** | 既存機能に影響しない (共存前提)。既存パターンで実装可能 |

## 5. 設計フェーズへの申し送り事項

### 決定事項候補
1. **家具所持モデル**: 数量 / インスタンス / ハイブリッドのいずれか
2. **毛糸の桁あふれ処理**: 丸め / エラーのいずれか
3. **PlayerPrefs DTO のバージョン形式**: `Version` フィールドの型とマイグレーション戦略

### Research Needed
1. **家具インスタンス ID の採番戦略** (案 β/γ 採用時): 単調増加 int / GUID / etc.
2. **通知の発行タイミング**: 同期発行 / 次フレーム / Tick ベース

### 推奨アプローチ
**Option B** で新規 4 クラス + `PlayerPrefsKey` 追記。既存モックには一切触らない。将来の別タスクで `UserState` / `ShopState` からの移行を段階的に実施する。
