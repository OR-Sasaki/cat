#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cat.Character.Outfits;
using UnityEditor;
using UnityEngine;

namespace Cat.Character
{
    /// TempResource/Character/test_data/Item に置かれた服・アイテム画像から
    /// OutfitPart / Outfit アセットと outfits.csv 行を一括生成するエディタ拡張。
    /// テーマ名ベースの命名 (例: ClothDracula / HeadAccessoryBunny) で追加する。
    /// 冪等: 既存アセット・CSV行はスキップするので再実行・追加実行が安全。
    public static class OutfitBatchGenerator
    {
        /// 画像の取り込み元 (プロジェクトルート相対)
        const string SourceRelativeDir = "TempResource/Character/test_data/Item";
        const string OutfitPartsRoot = "Assets/Arts/Character/OutfitParts";
        const string OutfitsRoot = "Assets/Arts/Character/Outfits";
        const string OutfitsCsvPath = "Assets/Resources/outfits.csv";

        /// 袖・襟が無いテーマで再利用する空パーツ (既存アセット)
        const string EmptyClothBackPath = "Assets/Arts/Character/OutfitParts/ClothBack/ClothBack000/ClothBack000.asset";
        const string EmptyClothFrontPath = "Assets/Arts/Character/OutfitParts/ClothFront/ClothFront000/ClothFront000.asset";
        const string EmptyClothCollarPath = "Assets/Arts/Character/OutfitParts/ClothCollar/ClothCollar000/ClothCollar000.asset";

        static string SourceDir => Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, SourceRelativeDir);

        // 追加する Outfit の (type,name) をこの順で CSV に追記する
        static readonly List<(OutfitType type, string name)> _generatedOutfits = new();

        [MenuItem("Cat/Outfits/Generate From Item Textures")]
        static void Generate()
        {
            if (!Directory.Exists(SourceDir))
            {
                Debug.LogError($"[OutfitBatchGenerator] 取り込み元が見つかりません: {SourceDir}");
                return;
            }

            _generatedOutfits.Clear();

            // StartAssetEditing で囲むと Import が遅延され、直後の LoadAssetAtPath<Sprite> が
            // null を返してしまう。ForceSynchronousImport + SaveAndReimport で逐次同期取込する。
            GenerateHeadAccessories();
            GenerateHandAccessories();
            GenerateFaces();
            GenerateLegAccessories();
            GenerateCloths();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int addedRows = AppendCsvRows();

            EditorUtility.FocusProjectWindow();
            Debug.Log($"[OutfitBatchGenerator] 完了: Outfit {_generatedOutfits.Count} 件を処理, outfits.csv に {addedRows} 行追記");
        }

        // 頭アクセサリ (単一パーツ)
        static void GenerateHeadAccessories()
        {
            var items = new (string name, string src)[]
            {
                ("HeadAccessoryDracula", "item_head_dracula"),
                ("HeadAccessoryBunny", "item_head_bunny"),
                ("HeadAccessoryMother", "item_head_mother"),
                ("HeadAccessoryBurger", "item_head_burger"),
                ("HeadAccessoryOmeletRice", "item_head_Omelet rice"),
                ("HeadAccessoryPartyBlue", "item_head_partyblue"),
                ("HeadAccessoryPartyPink", "item_head_partypink"),
                ("HeadAccessoryHatBlue", "item_head_hatblue"),
                ("HeadAccessoryHatPink", "item_head_hatpink"),
                ("HeadAccessoryRibbonBlue", "item_head_ribbonblue"),
                ("HeadAccessoryRibbonPink", "item_head_ribbonpink"),
                ("HeadAccessoryMugiwaraBlue", "item_head_mugiwarablue"),
                ("HeadAccessoryMugiwaraPink", "item_head_mugiwarapink"),
            };
            foreach (var (name, src) in items)
            {
                GenerateSingle(OutfitType.HeadAccessory, PartType.HeadAccessory, name, src);
            }
        }

        // 手アクセサリ (単一パーツ)
        static void GenerateHandAccessories()
        {
            var items = new (string name, string src)[]
            {
                ("HandAccessoryDracula", "item_hand_dracula"),
                ("HandAccessoryBunny", "item_hand_bunny"),
                ("HandAccessoryMother", "item_hand_mother"),
                ("HandAccessoryOmeletRice", "item_hand_Omelet rice"),
                ("HandAccessoryPartyBlue", "item_hand_partyblue"),
                ("HandAccessoryPartyPink", "item_hand_partypink"),
            };
            foreach (var (name, src) in items)
            {
                GenerateSingle(OutfitType.HandAccessory, PartType.HandAccessory, name, src);
            }
        }

        // 顔 (単一パーツ)。テーマ名が無いので Face001 に続く連番。
        static void GenerateFaces()
        {
            var items = new (string name, string src)[]
            {
                ("Face002", "iten_face_1"),
                ("Face003", "iten_face_2"),
                ("Face004", "iten_face_3"),
                ("Face005", "iten_face_4"),
                ("Face006", "iten_face_5"),
                ("Face007", "iten_face_6"),
                ("Face008", "iten_face_7"),
            };
            foreach (var (name, src) in items)
            {
                GenerateSingle(OutfitType.Face, PartType.Face, name, src);
            }
        }

        // 靴 (奥/手前の2パーツ)。B=奥(Back), F=手前(Front)。サムネは手前。
        static void GenerateLegAccessories()
        {
            var items = new (string name, string backSrc, string frontSrc)[]
            {
                ("LegAccessoryDracula", "item_shoes_draculaB", "item_shoes_draculaF"),
                ("LegAccessoryBunny", "item_shoes_bunnyB", "item_shoes_bunnyF"),
                ("LegAccessoryMother", "item_shoes_motherB", "item_shoes_motherF"),
                ("LegAccessoryBurger", "item_shoes_burgerB", "item_shoes_burgerF"),
                ("LegAccessoryOmeletRice", "item_shoes_Omelet riceB", "item_shoes_Omelet riceF"),
            };
            foreach (var (name, backSrc, frontSrc) in items)
            {
                var theme = name.Substring("LegAccessory".Length);
                var backPart = EnsurePartFromSource(backSrc, "LegAccessoryBack" + theme, PartType.LegAccessoryBack);
                var frontPart = EnsurePartFromSource(frontSrc, "LegAccessoryFront" + theme, PartType.LegAccessoryFront);
                CreateOutfit(OutfitType.LegAccessory, name, frontPart.Sprite, new[]
                {
                    (PartType.LegAccessoryBack, backPart),
                    (PartType.LegAccessoryFront, frontPart),
                });
            }
        }

        // 服 (胴体/奥袖/手前袖/襟の4パーツ)。袖・襟が無いテーマは空パーツ000を再利用。サムネは胴体。
        static void GenerateCloths()
        {
            // (outfit, bodySrc, backSrc?, backName?, frontSrc?, frontName?, collarSrc?, collarName?)
            var items = new (string name, string bodySrc,
                string? backSrc, string? backName,
                string? frontSrc, string? frontName,
                string? collarSrc, string? collarName)[]
            {
                ("ClothDracula", "item_body_dracula",
                    "item_body_draculasleeveB", "ClothBackDracula",
                    "item_body_draculasleeveF", "ClothFrontDracula",
                    null, null),
                ("ClothOmeletRice", "item_body_Omelet rice",
                    "item_body_Omelet riceB", "ClothBackOmeletRice",
                    "item_body_Omelet riceF", "ClothFrontOmeletRice",
                    null, null),
                ("ClothBurger", "item_body_burger",
                    null, null,
                    null, null,
                    "item_collar_burger", "ClothCollarBurger"),
                // T シャツは青/桃で袖 (ClothBackTshirt / ClothFrontTshirt) を共有する
                ("ClothTshirtBlue", "item_body_Tshirtblue",
                    "item_body_TshirtsleeveB", "ClothBackTshirt",
                    "item_body_TshirtsleeveF", "ClothFrontTshirt",
                    null, null),
                ("ClothTshirtPink", "item_body_Tshirtpink",
                    "item_body_TshirtsleeveB", "ClothBackTshirt",
                    "item_body_TshirtsleeveF", "ClothFrontTshirt",
                    null, null),
                ("ClothBunny", "item_body_bunny", null, null, null, null, null, null),
                ("ClothMother", "item_body_mother", null, null, null, null, null, null),
                ("ClothPartyBlue", "item_body_partyblue", null, null, null, null, null, null),
                ("ClothPartyPink", "item_body_partypink", null, null, null, null, null, null),
                ("ClothBellBlue", "item_body_bellblue", null, null, null, null, null, null),
                ("ClothBellPink", "item_body_bellpink", null, null, null, null, null, null),
            };

            var emptyBack = LoadExisting(EmptyClothBackPath);
            var emptyFront = LoadExisting(EmptyClothFrontPath);
            var emptyCollar = LoadExisting(EmptyClothCollarPath);

            foreach (var it in items)
            {
                var theme = it.name.Substring("Cloth".Length);
                var bodyPart = EnsurePartFromSource(it.bodySrc, "ClothBody" + theme, PartType.ClothBody);
                var backPart = it.backSrc != null
                    ? EnsurePartFromSource(it.backSrc, it.backName!, PartType.ClothBack)
                    : emptyBack;
                var frontPart = it.frontSrc != null
                    ? EnsurePartFromSource(it.frontSrc, it.frontName!, PartType.ClothFront)
                    : emptyFront;
                var collarPart = it.collarSrc != null
                    ? EnsurePartFromSource(it.collarSrc, it.collarName!, PartType.ClothCollar)
                    : emptyCollar;

                CreateOutfit(OutfitType.Cloth, it.name, bodyPart.Sprite, new[]
                {
                    (PartType.ClothBody, bodyPart),
                    (PartType.ClothBack, backPart),
                    (PartType.ClothFront, frontPart),
                    (PartType.ClothCollar, collarPart),
                });
            }
        }

        static void GenerateSingle(OutfitType outfitType, PartType partType, string outfitName, string src)
        {
            var part = EnsurePartFromSource(src, outfitName, partType);
            CreateOutfit(outfitType, outfitName, part.Sprite, new[] { (partType, part) });
        }

        // 画像をプロジェクトへコピー → Sprite 設定 → OutfitPart を生成 (冪等)。
        // 同名パーツが既にあれば読み込んで返す (T シャツ袖の共有をここで吸収)。
        static OutfitPart EnsurePartFromSource(string sourceStem, string partName, PartType partType)
        {
            var targetDir = $"{OutfitPartsRoot}/{partType}/{partName}";
            var assetPath = $"{targetDir}/{partName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<OutfitPart>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            Directory.CreateDirectory(targetDir);

            var pngPath = $"{targetDir}/{partName}.png";
            if (!File.Exists(pngPath))
            {
                var sourcePng = Path.Combine(SourceDir, sourceStem + ".png");
                if (!File.Exists(sourcePng))
                {
                    Debug.LogError($"[OutfitBatchGenerator] 元画像が見つかりません: {sourcePng}");
                    return CreateEmptyPart(assetPath, partType);
                }
                File.Copy(sourcePng, pngPath);
            }

            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureAsSprite(pngPath);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (sprite == null)
            {
                Debug.LogWarning($"[OutfitBatchGenerator] Sprite を取得できませんでした: {pngPath}");
            }

            var part = ScriptableObject.CreateInstance<OutfitPart>();
            part.PartType = partType;
            part.Sprite = sprite;
            AssetDatabase.CreateAsset(part, assetPath);
            return part;
        }

        static OutfitPart CreateEmptyPart(string assetPath, PartType partType)
        {
            var part = ScriptableObject.CreateInstance<OutfitPart>();
            part.PartType = partType;
            AssetDatabase.CreateAsset(part, assetPath);
            return part;
        }

        static void ConfigureAsSprite(string pngPath)
        {
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer == null)
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

        // Outfit アセットを生成 (冪等)。パーツはフィールド名 = PartType + "Part" で束ねる。
        static void CreateOutfit(OutfitType outfitType, string name, Sprite? thumbnail,
            (PartType partType, OutfitPart part)[] parts)
        {
            var dir = $"{OutfitsRoot}/{outfitType}";
            var assetPath = $"{dir}/{name}.asset";

            if (AssetDatabase.LoadAssetAtPath<Outfit>(assetPath) == null)
            {
                Directory.CreateDirectory(dir);

                Outfit outfit = outfitType switch
                {
                    OutfitType.Cloth => ScriptableObject.CreateInstance<Cat.Character.Outfits.Cloth>(),
                    OutfitType.Face => ScriptableObject.CreateInstance<Face>(),
                    OutfitType.HandAccessory => ScriptableObject.CreateInstance<HandAccessory>(),
                    OutfitType.HeadAccessory => ScriptableObject.CreateInstance<HeadAccessory>(),
                    OutfitType.LegAccessory => ScriptableObject.CreateInstance<LegAccessory>(),
                    _ => null!,
                };
                if (outfit == null)
                {
                    Debug.LogError($"[OutfitBatchGenerator] 未対応の OutfitType: {outfitType}");
                    return;
                }

                outfit.Thumbnail = thumbnail;
                foreach (var (partType, part) in parts)
                {
                    var field = outfit.GetType().GetField($"{partType}Part");
                    if (field == null)
                    {
                        Debug.LogWarning($"[OutfitBatchGenerator] {outfitType} に {partType}Part フィールドがありません");
                        continue;
                    }
                    field.SetValue(outfit, part);
                }
                AssetDatabase.CreateAsset(outfit, assetPath);
            }

            _generatedOutfits.Add((outfitType, name));
        }

        static OutfitPart LoadExisting(string assetPath)
        {
            var part = AssetDatabase.LoadAssetAtPath<OutfitPart>(assetPath);
            if (part == null)
            {
                Debug.LogError($"[OutfitBatchGenerator] 空パーツが見つかりません: {assetPath}");
            }
            return part;
        }

        // outfits.csv に (id,type,name) を追記。既存 name はスキップし、id は最大+1 から採番。
        static int AppendCsvRows()
        {
            if (!File.Exists(OutfitsCsvPath))
            {
                Debug.LogError($"[OutfitBatchGenerator] CSV が見つかりません: {OutfitsCsvPath}");
                return 0;
            }

            var lines = File.ReadAllLines(OutfitsCsvPath).ToList();
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
            foreach (var (type, name) in _generatedOutfits)
            {
                if (existingNames.Contains(name))
                {
                    continue;
                }
                existingNames.Add(name);
                sb.Append('\n').Append(++maxId).Append(',').Append(type.ToString()).Append(',').Append(name);
                added++;
            }

            if (added > 0)
            {
                var text = File.ReadAllText(OutfitsCsvPath).TrimEnd('\r', '\n');
                File.WriteAllText(OutfitsCsvPath, text + sb + "\n");
                AssetDatabase.ImportAsset(OutfitsCsvPath);
            }
            return added;
        }
    }
}
