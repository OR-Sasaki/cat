using System.IO;
using UnityEditor;
using UnityEngine;

namespace Cat.Character
{
    public static class OutfitPartCreator
    {
        static readonly string BaseDir = "Assets/Arts/Character/Parts/Tail";

        [MenuItem("Assets/Create/OutfitPart From Textures", true)]
        private static bool ValidateCreateOutfitParts()
        {
            // Texture2D が1つ以上選択されているときのみ有効化
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D)
                    return true;
            }
            return false;
        }

        [MenuItem("Assets/Create/OutfitPart From Textures")]
        private static void CreateOutfitParts()
        {
            var textures = Selection.objects;
            int createdCount = 0;

            foreach (var obj in textures)
            {
                if (!(obj is Texture2D texture))
                    continue;

                string texturePath = AssetDatabase.GetAssetPath(texture);
                string name = texture.name;
                string textureName = char.ToUpper(name[0]) + name[1..];

                // 出力フォルダを作成
                string targetDir = Path.Combine(BaseDir, textureName).Replace("\\", "/");
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                // Texture コピー先
                string ext = Path.GetExtension(texturePath);
                string textureTargetPath = Path.Combine(targetDir, $"{textureName}{ext}").Replace("\\", "/");

                // OutfitPart 保存パス
                string assetPath = Path.Combine(targetDir, $"{textureName}.asset").Replace("\\", "/");

                // Textureをコピー
                if (!File.Exists(textureTargetPath))
                {
                    File.Copy(texturePath, textureTargetPath);
                    AssetDatabase.ImportAsset(textureTargetPath);
                    Debug.Log($"[Copy] {textureTargetPath}");
                }

                // コピー先からSpriteを取得
                Sprite sprite = null;
                foreach (var s in AssetDatabase.LoadAllAssetsAtPath(textureTargetPath))
                {
                    if (s is Sprite)
                    {
                        sprite = (Sprite)s;
                        break;
                    }
                }

                if (sprite == null)
                {
                    Debug.LogWarning($"Spriteが見つかりませんでした: {textureTargetPath}");
                    continue;
                }

                // 既存チェック
                if (File.Exists(assetPath))
                {
                    Debug.LogWarning($"既に存在します: {assetPath}");
                    continue;
                }

                // OutfitPartを生成
                var outfitPart = ScriptableObject.CreateInstance<OutfitPart>();
                outfitPart.Sprite = sprite;
                outfitPart.PartType = PartType.Tail;

                AssetDatabase.CreateAsset(outfitPart, assetPath);
                createdCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Debug.Log($"OutfitPart作成完了: {createdCount}個");
        }
    }
}
