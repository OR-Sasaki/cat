#nullable enable

using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Root.Service
{
    /// 数量ベースの所持家具 (UserItemInventoryService) を、IsoGrid が要求する
    /// 一意な UserFurnitureId 単位のインスタンスへ射影する
    ///
    /// UserFurnitureId は (FurnitureId, スロット番号) から決定論的に採番するため、
    /// セッションを跨いでも同じ所持状態からは同じ ID が得られ、IsoGrid のセーブデータと整合する
    /// 所持数を減らす API は存在しない (加算のみ) ため、既存 ID が後からずれることはない
    ///
    /// 所持数のロードは MasterData の import 完了待ちで遅延しうるため、結果はキャッシュせず
    /// 呼び出しごとに再構築する (所持数は高々数百件、呼び出しも画面を開いた時のみ)
    public class UserFurnitureInstanceService
    {
        /// 1 種あたりのスロット上限。UserFurnitureId = FurnitureId * SlotStride + slot + 1
        /// 0 は IsoGrid のセル空き値、負値は Base 未配置 sentinel のため 1 以上になるよう +1 する
        public const int SlotStride = 10000;

        readonly IUserItemInventoryService _inventoryService;

        [Inject]
        public UserFurnitureInstanceService(IUserItemInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        /// 所持中の全家具インスタンスを FurnitureId 昇順・スロット昇順で列挙する
        public IReadOnlyList<UserFurnitureInstance> GetAll()
        {
            return Build();
        }

        /// UserFurnitureId から所持中のインスタンスを解決する (未所持・不正 ID は null)
        public UserFurnitureInstance? Find(int userFurnitureId)
        {
            if (userFurnitureId <= 0) return null;

            var furnitureId = DecodeFurnitureId(userFurnitureId);
            var slotIndex = DecodeSlotIndex(userFurnitureId);

            var count = _inventoryService.GetFurnitureCount(furnitureId);
            if (slotIndex >= count) return null;

            return new UserFurnitureInstance(userFurnitureId, furnitureId);
        }

        public static int Encode(uint furnitureId, int slotIndex)
        {
            return (int)furnitureId * SlotStride + slotIndex + 1;
        }

        public static uint DecodeFurnitureId(int userFurnitureId)
        {
            return (uint)((userFurnitureId - 1) / SlotStride);
        }

        public static int DecodeSlotIndex(int userFurnitureId)
        {
            return (userFurnitureId - 1) % SlotStride;
        }

        List<UserFurnitureInstance> Build()
        {
            var counts = _inventoryService.GetAllFurnitureCounts();

            var furnitureIds = new List<uint>(counts.Count);
            foreach (var kvp in counts)
            {
                furnitureIds.Add(kvp.Key);
            }
            // Dictionary の列挙順は不定のため、一覧表示順とセーブデータ整合のために昇順で固定する
            furnitureIds.Sort();

            var result = new List<UserFurnitureInstance>();
            foreach (var furnitureId in furnitureIds)
            {
                if (furnitureId > (int.MaxValue - SlotStride) / SlotStride)
                {
                    Debug.LogError($"[UserFurnitureInstanceService] furnitureId={furnitureId} は UserFurnitureId に符号化できません");
                    continue;
                }

                var count = counts[furnitureId];
                if (count > SlotStride)
                {
                    Debug.LogWarning($"[UserFurnitureInstanceService] furnitureId={furnitureId} の所持数 {count} がスロット上限 {SlotStride} を超えたため切り捨てます");
                    count = SlotStride;
                }

                for (var slotIndex = 0; slotIndex < count; slotIndex++)
                {
                    result.Add(new UserFurnitureInstance(Encode(furnitureId, slotIndex), furnitureId));
                }
            }

            return result;
        }
    }

    /// 所持家具 1 個体。UserFurnitureId は IsoGrid の配置・セーブで個体を同定するキー
    public readonly struct UserFurnitureInstance
    {
        public int UserFurnitureId { get; }
        public uint FurnitureId { get; }

        public UserFurnitureInstance(int userFurnitureId, uint furnitureId)
        {
            UserFurnitureId = userFurnitureId;
            FurnitureId = furnitureId;
        }
    }
}
