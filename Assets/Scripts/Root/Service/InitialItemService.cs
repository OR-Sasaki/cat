#nullable enable

using System;
using System.Collections.Generic;
using Root.State;
using UnityEngine;
using VContainer;

namespace Root.Service
{
    /// 初回起動時にユーザーへ付与する初期アイテムの定義を Resources の CSV から解決する
    /// 家具は initial_furnitures.csv (furniture_id, count)、着せ替えは default_outfits.csv
    /// (アセット名) をマスタ経由で OutfitId に解決する
    /// マスタに存在しない ID / 名前は解決時に除外するため、呼び出し側は結果をそのまま付与してよい
    public class InitialItemService
    {
        const string InitialFurnituresCsvName = "initial_furnitures";
        const string DefaultOutfitsCsvName = "default_outfits";

        readonly MasterDataState _masterDataState;

        [Inject]
        public InitialItemService(MasterDataState masterDataState)
        {
            _masterDataState = masterDataState;
        }

        /// 初期所持家具の (FurnitureId, Count) 一覧
        public IReadOnlyList<InitialFurniture> GetInitialFurnitures()
        {
            var lines = LoadCsvLines(InitialFurnituresCsvName);
            if (lines is null) return Array.Empty<InitialFurniture>();

            var result = new List<InitialFurniture>();
            foreach (var line in lines)
            {
                var columns = line.Split(',');
                if (columns.Length < 2)
                {
                    Debug.LogWarning($"[InitialItemService] {InitialFurnituresCsvName}: 列数が不足しています: {line}");
                    continue;
                }

                if (!uint.TryParse(columns[0].Trim(), out var furnitureId)
                    || !int.TryParse(columns[1].Trim(), out var count))
                {
                    Debug.LogWarning($"[InitialItemService] {InitialFurnituresCsvName}: 数値の解析に失敗しました: {line}");
                    continue;
                }

                if (count <= 0) continue;

                if (!IsKnownFurnitureId(furnitureId))
                {
                    Debug.LogWarning($"[InitialItemService] {InitialFurnituresCsvName}: マスタに存在しない furniture_id={furnitureId}");
                    continue;
                }

                result.Add(new InitialFurniture(furnitureId, count));
            }

            return result;
        }

        /// 初期所持着せ替えの OutfitId 一覧
        /// default_outfits.csv はアセット名で定義されているためマスタ名と突き合わせて ID 化する
        public IReadOnlyList<uint> GetInitialOutfitIds()
        {
            var lines = LoadCsvLines(DefaultOutfitsCsvName);
            if (lines is null) return Array.Empty<uint>();

            var result = new List<uint>();
            foreach (var line in lines)
            {
                var columns = line.Split(',');
                if (columns.Length < 2) continue;

                var outfitName = columns[1].Trim();
                if (string.IsNullOrEmpty(outfitName)) continue;

                var outfitId = ResolveOutfitId(outfitName);
                if (outfitId is null)
                {
                    Debug.LogWarning($"[InitialItemService] {DefaultOutfitsCsvName}: マスタに存在しない outfit 名={outfitName}");
                    continue;
                }

                result.Add(outfitId.Value);
            }

            return result;
        }

        /// ヘッダー行と空行を除いた CSV 行を返す。CSV が無ければ null
        static string[]? LoadCsvLines(string csvName)
        {
            var csv = Resources.Load<TextAsset>(csvName);
            if (csv is null)
            {
                Debug.LogError($"[InitialItemService] {csvName}.csv not found");
                return null;
            }

            var lines = csv.text.Split('\n');
            var result = new List<string>(lines.Length);
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                result.Add(lines[i]);
            }

            return result.ToArray();
        }

        bool IsKnownFurnitureId(uint id)
        {
            var list = _masterDataState.Furnitures;
            if (list is null) return false;
            foreach (var furniture in list)
            {
                if (furniture.Id == id) return true;
            }
            return false;
        }

        uint? ResolveOutfitId(string outfitName)
        {
            var list = _masterDataState.Outfits;
            if (list is null) return null;
            foreach (var outfit in list)
            {
                if (outfit.Name == outfitName) return outfit.Id;
            }
            return null;
        }
    }

    /// 初期付与する家具 1 種とその個数
    public readonly struct InitialFurniture
    {
        public uint FurnitureId { get; }
        public int Count { get; }

        public InitialFurniture(uint furnitureId, int count)
        {
            FurnitureId = furnitureId;
            Count = count;
        }
    }
}
