#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cat.Furniture;
using Home.View;
using NavMeshPlus.Components;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cat.FurnitureTools
{
    /// TempResource/家具 に置かれた家具画像から、家具1件あたり
    ///   1. Sprite (PNG を取り込み Sprite 設定)
    ///   2. prefab (シーン実体。Base/Floor/Small/Wall で構成が異なる)
    ///   3. Furniture.asset (ScriptableObject。prefab/Thumbnail を結線)
    ///   4. Addressables エントリ "Furnitures/{Name}"
    ///   5. furnitures.csv 行
    /// を一括生成するエディタ拡張。実行後、Cat > Furniture > Generate From Images。
    ///
    /// prefab は「概ね動く雛形」を生成する。次は家具ごとに Scene 上で手調整が必要:
    ///   - IsoDraggableView._footprintSize / _pivotGridPosition (占有マス数・ピボット)
    ///   - 見た目子オブジェクトの localScale / localPosition (グリッドへの位置合わせ)
    ///   - PolygonCollider2D の形状 (当たり判定シルエット。初期値は矩形近似)
    ///   - NotWalkable の NavMesh 範囲 (猫の回避領域。Floor のみ)
    ///   - Sprite の Pivot (初期は中央。床接地点に合わせたい場合は調整)
    ///
    /// 冪等: 既存の Furniture.asset があればスキップ。CSV は既存 name をスキップ。
    /// Addressables は CreateOrMoveEntry で上書き。再実行・追加実行が安全。
    public static class FurnitureBatchGenerator
    {
        /// 画像の取り込み元 (プロジェクトルート相対)
        const string SourceRelativeDir = "TempResource/家具";
        const string FurnituresRoot = "Assets/Arts/Furniture/Furnitures";
        const string FurnituresCsvPath = "Assets/Resources/furnitures.csv";
        const string AddressableGroupName = "Furnitures";

        /// URP Sprite-Lit-Default.mat (既存家具の SpriteRenderer と同一マテリアル)
        const string SpriteLitDefaultGuid = "a97c105638bdf8b4a8650670310a4cd3";
        /// Base(部屋背景)スプライトの描画順 (既存 BaseA/BaseB に合わせる)
        const int BaseSortingOrder = -47;

        enum Variant
        {
            Base,  // 部屋背景そのもの (SpriteRenderer のみ, PlacementType.Base)
            Floor, // 床置き大〜中物 (IsoDraggableView + グリッド + NavMesh)
            Small, // 卓上/小物/ゴミ系 (Floor と同構成, 1マス)
            Wall,  // 壁掛け (左右 ViewPivot, NavMesh なし)
        }

        readonly struct Def
        {
            public readonly string Name;      // 家具名 (アセット名 / Addressable / CSV name)
            public readonly string Src;       // 元画像ファイル名 (拡張子なし)
            public readonly Variant Variant;
            public readonly Vector2Int Footprint; // 占有マス数の初期値 (Base では未使用)

            public Def(string name, string src, Variant variant, int fx, int fy)
            {
                Name = name;
                Src = src;
                Variant = variant;
                Footprint = new Vector2Int(fx, fy);
            }
        }

        // === 生成対象の家具定義 (画像 → 家具名 / 種別 / フットプリント初期値) ===
        // Variant・Footprint は編集可。不要な行はコメントアウトすれば除外される。
        static readonly Def[] Defs =
        {
            // --- Base: 部屋背景そのもの (既存 BaseA / BaseB に続く連番) ---
            new("BaseC", "Room_base",      Variant.Base,  0, 0),
            new("BaseD", "Room_base_line", Variant.Base,  0, 0), // ※line 画像。重ね用オーバーレイの可能性あり—要確認

            // --- Wall: 壁掛け ---
            new("Door",      "door",       Variant.Wall,  2, 4),
            new("WallShelf", "wall shelf", Variant.Wall,  3, 1),
            new("Window1",   "window1",    Variant.Wall,  3, 3),
            new("Window2",   "window2",    Variant.Wall,  2, 3),
            new("Watch",     "watch",      Variant.Wall,  1, 1),
            new("Outlet",    "outlet",     Variant.Wall,  1, 1),
            new("Switch",    "swith",      Variant.Wall,  1, 1), // 元画像名は "swith" (typo) だが家具名は Switch

            // --- Floor: 床置き (大〜中) ---
            new("Bed",           "bed",            Variant.Floor, 4, 2),
            new("Table",         "table",          Variant.Floor, 2, 2),
            new("Chair",         "chair",          Variant.Floor, 1, 1),
            new("Bookshelf",     "bookshelf",      Variant.Floor, 2, 2),
            new("Refrigerator",  "refrigerator",   Variant.Floor, 2, 2),
            new("Stove",         "Stove",          Variant.Floor, 2, 2),
            new("Sink",          "sink",           Variant.Floor, 2, 2),
            new("CookingTable",  "cooking table",  Variant.Floor, 2, 2),
            new("SideTable",     "side table",     Variant.Floor, 1, 1),
            new("MicrowaveOven", "microwave oven", Variant.Floor, 2, 1),
            new("Cushion",       "cushion",        Variant.Floor, 2, 2),
            new("Yogibo",        "yogibo",         Variant.Floor, 3, 3),
            new("StuffedToy",    "stuffed toy",    Variant.Floor, 3, 3),
            new("HangerTree",    "hanger tree",    Variant.Floor, 1, 1),
            new("Houseplants",   "houseplants",    Variant.Floor, 1, 1),
            new("Lamp",          "lamp",           Variant.Floor, 1, 1),
            new("TrashCan",      "trash can",      Variant.Floor, 1, 1),
            new("Rug",           "rug",            Variant.Floor, 4, 3),
            new("Rug2",          "rug2",           Variant.Floor, 4, 3),
            new("Mat",           "mat",            Variant.Floor, 2, 1),
            new("CardboardC",    "cardboard C",    Variant.Floor, 2, 2),
            new("CardboardO",    "cardboard O",    Variant.Floor, 2, 2),
            new("CardboardW",    "cardboard W",    Variant.Floor, 2, 2),

            // --- Small: 卓上/小物/ゴミ系 (床置き扱い, 1マス) ---
            new("Apple",      "apple",       Variant.Small, 1, 1),
            new("AppleCore",  "apple core",  Variant.Small, 1, 1),
            new("BananaPeel", "banana peel", Variant.Small, 1, 1),
            new("Book",       "book",        Variant.Small, 1, 1),
            new("Cactus",     "cactus",      Variant.Small, 1, 1),
            new("Can",        "can",         Variant.Small, 1, 1),
            new("Cup",        "cup",         Variant.Small, 1, 1),
            new("Cup2",       "cup2",        Variant.Small, 1, 1),
            new("Rat",        "rat",         Variant.Small, 1, 1),
            new("Tissue",     "tissue",      Variant.Small, 1, 1),
            new("Vase",       "vase",        Variant.Small, 1, 1),
            new("Yarn",       "yarn",        Variant.Small, 1, 1),

            // 注: painting は既存 Wall/Painting 家具として登録済みのため対象外
        };

        static string SourceDir =>
            Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, SourceRelativeDir);

        static Material? SpriteMaterial =>
            AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(SpriteLitDefaultGuid));

        // CSV 追記用に生成した (csvType, name) を順に貯める
        static readonly List<(string type, string name)> Generated = new();

        [MenuItem("Cat/Furniture/Generate From Images")]
        static void Generate()
        {
            if (!Directory.Exists(SourceDir))
            {
                Debug.LogError($"[FurnitureBatchGenerator] 取り込み元が見つかりません: {SourceDir}");
                return;
            }

            Generated.Clear();
            int created = 0, skipped = 0;

            foreach (var def in Defs)
            {
                if (GenerateOne(def))
                {
                    created++;
                }
                else
                {
                    skipped++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int addedRows = AppendCsvRows();
            SaveAddressables();

            EditorUtility.FocusProjectWindow();
            Debug.Log($"[FurnitureBatchGenerator] 完了: 生成 {created} 件 / スキップ {skipped} 件, " +
                      $"furnitures.csv に {addedRows} 行追記");
        }

        // 家具1件を生成 (冪等)。新規生成したら true, 既存スキップなら false。
        static bool GenerateOne(in Def def)
        {
            var dir = $"{FurnituresRoot}/{FolderOf(def.Variant)}/{def.Name}";
            var assetPath = $"{dir}/{def.Name}.asset";

            if (AssetDatabase.LoadAssetAtPath<Cat.Furniture.Furniture>(assetPath) != null)
            {
                // 既存でも CSV には存在保証したいので記録 (AppendCsvRows 側で重複はスキップ)
                Generated.Add((CsvTypeOf(def.Variant), def.Name));
                return false;
            }

            Directory.CreateDirectory(dir);

            var sprite = ImportSprite(def, dir);
            if (sprite == null)
            {
                Debug.LogError($"[FurnitureBatchGenerator] スプライト取得失敗のため {def.Name} をスキップ");
                return false;
            }

            var prefabPath = $"{dir}/{def.Name}.prefab";
            var prefab = def.Variant switch
            {
                Variant.Base => BuildBasePrefab(def.Name, sprite, prefabPath),
                Variant.Wall => BuildWallPrefab(def.Name, sprite, def.Footprint, prefabPath),
                _ => BuildFloorPrefab(def.Name, sprite, def.Footprint, prefabPath), // Floor & Small
            };

            var furniture = ScriptableObject.CreateInstance<Cat.Furniture.Furniture>();
            furniture.FurnitureType = FurnitureTypeOf(def.Variant);
            furniture.PlacementType = PlacementTypeOf(def.Variant);
            furniture.Thumbnail = sprite;
            if (def.Variant == Variant.Base)
            {
                furniture.BaseSceneObject = prefab.transform;
                furniture.SceneObject = null;
            }
            else
            {
                furniture.SceneObject = prefab.GetComponent<IsoDraggableView>();
                furniture.BaseSceneObject = null;
            }
            AssetDatabase.CreateAsset(furniture, assetPath);

            AddAddressable(assetPath, def.Name);
            Generated.Add((CsvTypeOf(def.Variant), def.Name));
            return true;
        }

        // 画像をプロジェクトへコピー → Sprite 設定 → Sprite を返す (冪等)
        static Sprite? ImportSprite(in Def def, string dir)
        {
            var pngPath = $"{dir}/{def.Name}.png";
            if (!File.Exists(pngPath))
            {
                var sourcePng = Path.Combine(SourceDir, def.Src + ".png");
                if (!File.Exists(sourcePng))
                {
                    Debug.LogError($"[FurnitureBatchGenerator] 元画像が見つかりません: {sourcePng}");
                    return null;
                }
                File.Copy(sourcePng, pngPath);
            }

            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureAsSprite(pngPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        static void ConfigureAsSprite(string pngPath)
        {
            if (AssetImporter.GetAtPath(pngPath) is not TextureImporter importer)
            {
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        // --- prefab ビルダー ---

        // Base: SpriteRenderer のみ (部屋背景)。BaseSceneObject に root Transform を結線する。
        static GameObject BuildBasePrefab(string name, Sprite sprite, string prefabPath)
        {
            var root = new GameObject(name);
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (SpriteMaterial != null) sr.sharedMaterial = SpriteMaterial;
            sr.sortingOrder = BaseSortingOrder;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // Floor / Small: root(IsoDraggableView + 当たり判定) > ViewPivot(SortingGroup)
        //   > 見た目(SpriteRenderer) > FragmentedIsoGrid(上面グリッド), および NotWalkable(NavMesh)。
        static GameObject BuildFloorPrefab(string name, Sprite sprite, Vector2Int footprint, string prefabPath)
        {
            var root = new GameObject(name);
            var iso = root.AddComponent<IsoDraggableView>();
            var hit = root.AddComponent<PolygonCollider2D>();
            FitRectCollider(hit, sprite);

            var viewPivot = new GameObject("ViewPivot");
            viewPivot.transform.SetParent(root.transform, false);
            var sortingGroup = viewPivot.AddComponent<SortingGroup>();

            var visual = new GameObject(name);
            visual.transform.SetParent(viewPivot.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (SpriteMaterial != null) sr.sharedMaterial = SpriteMaterial;

            var gridGo = new GameObject("FragmentedIsoGrid");
            gridGo.transform.SetParent(visual.transform, false);
            var gridCol = gridGo.AddComponent<PolygonCollider2D>(); // FragmentedIsoGrid の RequireComponent を満たす
            FitRectCollider(gridCol, sprite);
            var fig = gridGo.AddComponent<FragmentedIsoGrid>();

            var notWalk = new GameObject("NotWalkable");
            notWalk.transform.SetParent(root.transform, false);
            var nwCol = notWalk.AddComponent<PolygonCollider2D>();
            FitRectCollider(nwCol, sprite);
            var nav = notWalk.AddComponent<NavMeshModifier>();

            var isoSo = new SerializedObject(iso);
            isoSo.FindProperty("_footprintSize").vector2IntValue = footprint;
            isoSo.FindProperty("_pivotGridPosition").vector2IntValue = footprint;
            isoSo.FindProperty("_viewPivot").objectReferenceValue = viewPivot.transform;
            isoSo.FindProperty("_sortingGroup").objectReferenceValue = sortingGroup;
            isoSo.ApplyModifiedPropertiesWithoutUndo();

            var figSo = new SerializedObject(fig);
            figSo.FindProperty("_size").vector2IntValue = footprint;
            figSo.FindProperty("_isoDraggableView").objectReferenceValue = iso;
            figSo.ApplyModifiedPropertiesWithoutUndo();

            var navSo = new SerializedObject(nav);
            navSo.FindProperty("m_OverrideArea").boolValue = true;
            navSo.FindProperty("m_Area").intValue = 1; // NotWalkable
            navSo.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // Wall: root(IsoDraggableView) > RightViewPivot / LeftViewPivot(それぞれ SortingGroup + Collider + 見た目)。
        //   Left は localScale.x = -1 で反転。NavMesh なし (床を塞がない)。
        static GameObject BuildWallPrefab(string name, Sprite sprite, Vector2Int footprint, string prefabPath)
        {
            var root = new GameObject(name);
            var iso = root.AddComponent<IsoDraggableView>();

            var right = MakeWallPivot(name, sprite, root.transform, mirror: false);
            var left = MakeWallPivot(name, sprite, root.transform, mirror: true);

            var isoSo = new SerializedObject(iso);
            isoSo.FindProperty("_footprintSize").vector2IntValue = footprint;
            isoSo.FindProperty("_pivotGridPosition").vector2IntValue = Vector2Int.zero;
            isoSo.FindProperty("_viewPivot").objectReferenceValue = root.transform;
            isoSo.FindProperty("_rightViewPivot").objectReferenceValue = right;
            isoSo.FindProperty("_leftViewPivot").objectReferenceValue = left;
            isoSo.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject MakeWallPivot(string name, Sprite sprite, Transform parent, bool mirror)
        {
            var pivot = new GameObject(mirror ? "LeftViewPivot" : "RightViewPivot");
            pivot.transform.SetParent(parent, false);
            if (mirror) pivot.transform.localScale = new Vector3(-1, 1, 1);
            pivot.AddComponent<SortingGroup>();
            var col = pivot.AddComponent<PolygonCollider2D>();
            FitRectCollider(col, sprite);

            var visual = new GameObject(name);
            visual.transform.SetParent(pivot.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (SpriteMaterial != null) sr.sharedMaterial = SpriteMaterial;
            return pivot;
        }

        // スプライト矩形を当たり判定の初期形状にする (実シルエットは Scene 上で手描き調整)
        static void FitRectCollider(PolygonCollider2D col, Sprite sprite)
        {
            var b = sprite.bounds;
            col.pathCount = 1;
            col.SetPath(0, new[]
            {
                new Vector2(b.min.x, b.min.y),
                new Vector2(b.max.x, b.min.y),
                new Vector2(b.max.x, b.max.y),
                new Vector2(b.min.x, b.max.y),
            });
        }

        // --- Variant → 各種マッピング ---

        static string FolderOf(Variant v) => v switch
        {
            Variant.Base => "Base",
            Variant.Floor => "Floor",
            Variant.Small => "Small",
            Variant.Wall => "Wall",
            _ => "Floor",
        };

        static FurnitureType FurnitureTypeOf(Variant v) => v switch
        {
            Variant.Base => FurnitureType.Base,
            Variant.Floor => FurnitureType.Floor,
            Variant.Small => FurnitureType.Small,
            Variant.Wall => FurnitureType.Wall,
            _ => FurnitureType.Floor,
        };

        static PlacementType PlacementTypeOf(Variant v) => v switch
        {
            Variant.Base => PlacementType.Base,
            Variant.Wall => PlacementType.Wall,
            _ => PlacementType.Floor, // Floor & Small は床配置
        };

        // furnitures.csv の type 列は配置カテゴリ (Base/Floor/Wall)。既存行に倣う (Small も床置き=Floor)。
        static string CsvTypeOf(Variant v) => v switch
        {
            Variant.Base => "Base",
            Variant.Wall => "Wall",
            _ => "Floor",
        };

        // --- Addressables ---

        static void AddAddressable(string assetPath, string name)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[FurnitureBatchGenerator] Addressables 設定が見つかりません。エントリ登録をスキップします。");
                return;
            }

            var group = settings.FindGroup(AddressableGroupName) ?? settings.DefaultGroup;
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry == null)
            {
                Debug.LogWarning($"[FurnitureBatchGenerator] Addressables エントリ生成に失敗: {assetPath}");
                return;
            }
            entry.SetAddress($"Furnitures/{name}", false);
        }

        static void SaveAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        // --- CSV ---

        // furnitures.csv に (id,type,name) を追記。既存 name はスキップし、id は最大+1 から採番。
        static int AppendCsvRows()
        {
            if (!File.Exists(FurnituresCsvPath))
            {
                Debug.LogError($"[FurnitureBatchGenerator] CSV が見つかりません: {FurnituresCsvPath}");
                return 0;
            }

            var lines = File.ReadAllLines(FurnituresCsvPath).ToList();
            var existingNames = new HashSet<string>();
            int maxId = 0;
            for (int i = 1; i < lines.Count; i++)
            {
                var cols = lines[i].Split(',');
                if (cols.Length < 3)
                {
                    continue;
                }
                if (int.TryParse(cols[0], out var id))
                {
                    maxId = Mathf.Max(maxId, id);
                }
                existingNames.Add(cols[2].Trim());
            }

            var sb = new StringBuilder();
            int added = 0;
            foreach (var (type, name) in Generated)
            {
                if (existingNames.Contains(name))
                {
                    continue;
                }
                existingNames.Add(name);
                sb.Append('\n').Append(++maxId).Append(',').Append(type).Append(',').Append(name);
                added++;
            }

            if (added > 0)
            {
                var text = File.ReadAllText(FurnituresCsvPath).TrimEnd('\r', '\n');
                File.WriteAllText(FurnituresCsvPath, text + sb + "\n");
                AssetDatabase.ImportAsset(FurnituresCsvPath);
            }
            return added;
        }
    }
}
