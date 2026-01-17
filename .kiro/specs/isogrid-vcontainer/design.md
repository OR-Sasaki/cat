# IsoGrid VContainer統合 設計ドキュメント

## 概要

IsoGrid関連コンポーネントをVContainerのライフサイクル管理下に置き、HomeScope経由でDI（依存性注入）を行う。

---

## 1. 現状分析

### 1.1 現在のファイル構成

```
Assets/Scripts/IsoGrid/
├── IsoGridCell.cs         # struct: セルデータ
├── IsoGridSystem.cs       # MonoBehaviour: グリッド管理
├── IsoGridGizmo.cs        # MonoBehaviour: エディタ用Gizmo
├── IsoDragManager.cs      # MonoBehaviour: 入力管理
├── IsoDraggable.cs        # MonoBehaviour: ドラッグ可能オブジェクト
└── IsoDraggableGizmo.cs   # MonoBehaviour: ドラッグプレビュー
```

### 1.2 現在の問題点

| 問題 | 詳細 |
|------|------|
| 名前空間の不統一 | `Cat`名前空間を使用。Homeシーン専用なのに`Home.*`ではない |
| 依存解決がFindで行われている | `FindFirstObjectByType<IsoGridSystem>()`でグローバル検索 |
| ライフサイクル管理不在 | VContainerの管理外。起動順序やDispose処理が制御できない |
| シーン構造規約違反 | Standalone Systemsパターンだが、実質Homeシーン専用 |

### 1.3 依存関係図（現状）

```
IsoDragManager ──(Raycast)──→ IsoDraggable
                                   │
                                   ├─(FindFirstObjectByType)→ IsoGridSystem
                                   │
                                   └→ SpriteRenderer
```

---

## 2. 設計方針

### 2.1 基本方針

1. **HomeシーンのView層として再配置**: IsoGridはHomeシーンでのみ使用するため、`Home/View/`に移動
2. **MonoBehaviourは維持**: シーン上のTransformやCollider参照が必要なため、純粋なC#クラスへの変換は行わない
3. **DIでの依存解決**: `FindFirstObjectByType`を廃止し、VContainerのDIを使用
4. **ITickableへの移行検討**: IsoDragManagerの`Update()`を`ITickable.Tick()`に変更

### 2.2 採用パターン

VContainerでMonoBehaviourをDI管理するパターン：

| パターン | 用途 | 適用先 |
|---------|------|--------|
| `RegisterComponent<T>` | シーン上の既存コンポーネントを登録 | IsoGridSystemView, IsoDragManagerView |
| `[Inject]`属性 | コンポーネントに依存を注入 | IsoDraggable（動的生成オブジェクト） |

---

## 3. ファイル移動計画

### 3.1 移動先マッピング

| 移動元 | 移動先 | 理由 |
|--------|--------|------|
| `IsoGrid/IsoGridCell.cs` | `Home/State/IsoGridCell.cs` | データ構造はState層 |
| `IsoGrid/IsoGridSystem.cs` | `Home/View/IsoGridSystemView.cs` | シーン上のMonoBehaviour |
| `IsoGrid/IsoDragManager.cs` | `Home/Service/IsoDragService.cs` | 入力処理サービス |
| `IsoGrid/IsoDraggable.cs` | `Home/View/IsoDraggableView.cs` | ドラッグ可能オブジェクト |
| `IsoGrid/IsoGridGizmo.cs` | `Home/View/Editor/IsoGridGizmo.cs` | エディタ専用 |
| `IsoGrid/IsoDraggableGizmo.cs` | `Home/View/Editor/IsoDraggableGizmo.cs` | エディタ専用 |

### 3.2 ディレクトリ構成（変更後）

```
Assets/Scripts/Home/
├── Scope/
│   └── HomeScope.cs          # ← 修正: IsoGrid関連を登録
├── Service/
│   ├── IsoDragService.cs     # ← 新規: IsoDragManagerから変換
│   └── ...
├── State/
│   ├── IsoGridCell.cs        # ← 移動
│   └── ...
└── View/
    ├── IsoGridSystemView.cs  # ← 移動＆リネーム
    ├── IsoDraggableView.cs   # ← 移動＆リネーム
    ├── Editor/               # ← 新規ディレクトリ
    │   ├── IsoGridGizmo.cs
    │   └── IsoDraggableGizmo.cs
    └── ...
```

---

## 4. クラス名・名前空間の変更

### 4.1 変更一覧

| 旧クラス名 | 新クラス名 | 旧名前空間 | 新名前空間 |
|-----------|-----------|-----------|-----------|
| `IsoGridCell` | `IsoGridCell` (変更なし) | `Cat` | `Home.State` |
| `IsoGridSystem` | `IsoGridSystemView` | `Cat` | `Home.View` |
| `IsoDragManager` | `IsoDragService` | `Cat` | `Home.Service` |
| `IsoDraggable` | `IsoDraggableView` | `Cat` | `Home.View` |
| `IsoGridGizmo` | `IsoGridGizmo` (変更なし) | `Cat` | `Home.View.Editor` |
| `IsoDraggableGizmo` | `IsoDraggableGizmo` (変更なし) | `Cat` | `Home.View.Editor` |

---

## 5. コード修正詳細

### 5.1 IsoGridSystemView（旧IsoGridSystem）

```csharp
// 変更前
namespace Cat
{
    public class IsoGridSystem : MonoBehaviour
    {
        void Awake() { ... }
        void Update() { UpdateFloorCellsDebugView(); }
    }
}

// 変更後
namespace Home.View
{
    public class IsoGridSystemView : MonoBehaviour
    {
        // Awake → Initialize（IInitializableではなく、MonoBehaviourのまま）
        void Awake()
        {
            UpdateAxisVectors();
            InitializeCells();
        }

        // Update内のデバッグ処理は残す（パフォーマンス影響小）
        void Update() { UpdateFloorCellsDebugView(); }
    }
}
```

**変更点**:
- 名前空間: `Cat` → `Home.View`
- クラス名: `IsoGridSystem` → `IsoGridSystemView`
- 内部ロジックは変更なし

---

### 5.2 IsoDragService（旧IsoDragManager）

```csharp
// 変更前
namespace Cat
{
    public class IsoDragManager : MonoBehaviour
    {
        Camera _mainCamera;

        void Awake() { _mainCamera = Camera.main; }
        void Update() { ... }
    }
}

// 変更後
namespace Home.Service
{
    public class IsoDragService : ITickable, IInitializable
    {
        readonly IsoGridSystemView _gridSystem;
        readonly HomeState _homeState;

        Camera _mainCamera;
        IsoDraggableView _currentDraggable;
        bool _isActive;

        [SerializeField] LayerMask _draggableLayerMask = -1;

        // コンストラクタでDI
        public IsoDragService(IsoGridSystemView gridSystem, HomeState homeState)
        {
            _gridSystem = gridSystem;
            _homeState = homeState;
        }

        public void Initialize()
        {
            _mainCamera = Camera.main;
            _homeState.OnStateChange.AddListener(OnStateChange);
        }

        void OnStateChange(HomeState.State previous, HomeState.State current)
        {
            // Redecorate状態のときのみアクティブ
            _isActive = current == HomeState.State.Redecorate;
        }

        public void Tick()
        {
            if (!_isActive) return;
            // 既存のUpdate処理をここに移動
        }
    }
}
```

**変更点**:
- `MonoBehaviour` → 純粋なC#クラス + `ITickable`, `IInitializable`
- コンストラクタDIで`IsoGridSystemView`と`HomeState`を受け取る
- `Update()` → `Tick()`
- HomeState.State.Redecorateのときのみ動作するように制御
- `LayerMask`はHomeScopeでの設定か、別途設定クラスを用意

**課題**: `LayerMask`はシリアライズできないため、以下のいずれかで対応：
1. HomeScopeで設定を渡す
2. `IsoDragServiceSettings`ScriptableObjectを作成
3. 定数として定義

---

### 5.3 IsoDraggableView（旧IsoDraggable）

```csharp
// 変更前
namespace Cat
{
    public class IsoDraggable : MonoBehaviour
    {
        IsoGridSystem _gridSystem;

        void Awake()
        {
            _gridSystem = FindFirstObjectByType<IsoGridSystem>();
        }
    }
}

// 変更後
namespace Home.View
{
    public class IsoDraggableView : MonoBehaviour
    {
        [Inject] IsoGridSystemView _gridSystem;

        // 代替案: publicメソッドで設定
        // public void SetGridSystem(IsoGridSystemView gridSystem)
        // {
        //     _gridSystem = gridSystem;
        // }

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            // FindFirstObjectByTypeを削除

            #if UNITY_EDITOR
            if (GetComponent<IsoDraggableGizmo>() == null)
            {
                gameObject.AddComponent<IsoDraggableGizmo>();
            }
            #endif
        }
    }
}
```

**変更点**:
- 名前空間: `Cat` → `Home.View`
- クラス名: `IsoDraggable` → `IsoDraggableView`
- `FindFirstObjectByType<IsoGridSystem>()` → `[Inject]`属性でDI

**DI方法の選択肢**:

| 方法 | メリット | デメリット |
|------|---------|-----------|
| `[Inject]`属性 | VContainer標準パターン | 動的生成時に`IObjectResolver.Inject()`が必要 |
| `SetGridSystem()`メソッド | シンプル、明示的 | DIの恩恵が薄れる |
| `FindFirstObjectByType`維持 | 変更最小 | VContainer統合の意味が薄れる |

**推奨**: `[Inject]`属性を使用し、シーン上の既存オブジェクトはHomeScopeで自動Inject。

---

### 5.4 HomeScope の修正

```csharp
// 変更後
namespace Home.Scope
{
    public class HomeScope : SceneScope
    {
        [SerializeField] CharacterView _characterView;
        [SerializeField] HomeUiView _homeUiView;
        [SerializeField] ClosetUiView _closetUiView;
        [SerializeField] RedecorateUiView _redecorateUiView;
        [SerializeField] CameraView _cameraView;

        // 追加: IsoGrid関連
        [SerializeField] IsoGridSystemView _isoGridSystemView;

        protected override void Configure(IContainerBuilder builder)
        {
            // 既存の登録
            builder.RegisterInstance(_characterView);
            builder.RegisterComponent(_homeUiView);
            builder.RegisterComponent(_closetUiView);
            builder.RegisterComponent(_redecorateUiView);
            builder.RegisterComponent(_cameraView);

            // 追加: IsoGrid関連
            builder.RegisterComponent(_isoGridSystemView);

            // State
            builder.Register<HomeState>(Lifetime.Scoped);
            builder.Register<OutfitAssetState>(Lifetime.Scoped);
            builder.Register<FurnitureAssetState>(Lifetime.Scoped);

            // Service
            builder.Register<HomeStateSetService>(Lifetime.Scoped);

            // EntryPoint
            builder.RegisterEntryPoint<OutfitAssetStarter>();
            builder.RegisterEntryPoint<FurnitureAssetStarter>();
            builder.RegisterEntryPoint<ClosetScrollerService>();
            builder.RegisterEntryPoint<RedecorateScrollerService>();
            builder.RegisterEntryPoint<RedecorateCameraService>();
            builder.RegisterEntryPoint<HomeViewService>();
            builder.RegisterEntryPoint<HomeStarter>();

            // 追加: IsoDragService
            builder.RegisterEntryPoint<IsoDragService>();
        }
    }
}
```

---

### 5.5 IsoDraggableViewへのInject処理

シーン上に既存のIsoDraggableViewオブジェクトがある場合、以下のいずれかの方法でInjectする：

**方法A: シーン起動時に一括Inject（推奨）**

```csharp
// Home/Starter/IsoDraggableStarter.cs（新規作成）
namespace Home.Starter
{
    public class IsoDraggableStarter : IStartable
    {
        readonly IObjectResolver _resolver;

        public IsoDraggableStarter(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public void Start()
        {
            // シーン上の全IsoDraggableViewにInject
            var draggables = Object.FindObjectsByType<IsoDraggableView>(FindObjectsSortMode.None);
            foreach (var draggable in draggables)
            {
                _resolver.Inject(draggable);
            }
        }
    }
}
```

**方法B: IsoDragService経由で設定**

```csharp
// IsoDragService内
void HandlePointerDown(Vector3 worldPos)
{
    var draggable = RaycastForDraggable(worldPos);
    if (draggable == null) return;

    // GridSystemの参照を渡す
    draggable.SetGridSystem(_gridSystem);

    _currentDraggable = draggable;
    _currentDraggable.BeginDrag(worldPos);
}
```

---

## 6. 実装タスク

### Phase 1: ファイル移動とリネーム
1. [ ] `IsoGridCell.cs` → `Home/State/IsoGridCell.cs`
2. [ ] `IsoGridSystem.cs` → `Home/View/IsoGridSystemView.cs`
3. [ ] `IsoDragManager.cs` → `Home/Service/IsoDragService.cs`
4. [ ] `IsoDraggable.cs` → `Home/View/IsoDraggableView.cs`
5. [ ] `IsoGridGizmo.cs` → `Home/View/Editor/IsoGridGizmo.cs`
6. [ ] `IsoDraggableGizmo.cs` → `Home/View/Editor/IsoDraggableGizmo.cs`
7. [ ] 旧`Assets/Scripts/IsoGrid/`ディレクトリを削除

### Phase 2: 名前空間とクラス名の変更
1. [ ] 各ファイルの名前空間を変更
2. [ ] クラス名をリネーム
3. [ ] 参照箇所を更新

### Phase 3: DI統合
1. [ ] `IsoDragService`を`ITickable`, `IInitializable`に変換
2. [ ] `IsoDraggableView`に`[Inject]`属性を追加
3. [ ] `IsoDraggableStarter`を新規作成
4. [ ] `HomeScope`にIsoGrid関連を登録

### Phase 4: 動作確認
1. [ ] シーン上のGameObjectの参照を再設定
2. [ ] ドラッグ操作の動作確認
3. [ ] グリッド配置の動作確認

---

## 7. 注意事項

### 7.1 metaファイルの扱い
- Unityエディタ上でファイル移動を行い、GUIDを維持する
- コマンドラインで移動する場合は`.meta`ファイルも一緒に移動

### 7.2 シーン上の参照
- `IsoGridSystemView`と`IsoDragManager`はシーン上のGameObjectにアタッチされている
- リネーム後、シーンファイル内の参照が壊れる可能性がある
- GUIDベースでの参照を維持するため、metaファイルの管理に注意

### 7.3 プレハブの修正
- `IsoDraggableView`がプレハブに使われている場合、プレハブの参照更新が必要