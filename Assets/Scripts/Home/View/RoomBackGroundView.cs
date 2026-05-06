using UnityEngine;

namespace Home.View
{
    /// RoomBackGround GameObject にアタッチされる Marker View。
    /// _baseRoot 配下は Base インスタンス専用領域であり、FurniturePlacementService.PlaceBase が
    /// 呼び出されるたびに _baseRoot の全子が無条件に Object.Destroy される。
    /// デバッグ補助・装飾物等を _baseRoot 配下に配置することを禁止する。
    /// 既存の RoomBackGround 直下子オブジェクト (RoomObject 等) は _baseRoot の兄弟として配置し、
    /// Base 配置・破棄処理の影響を受けないようにする。
    public class RoomBackGroundView : MonoBehaviour
    {
        [Header("Base 配置専用ルート (このTransform直下にBase以外を置かないこと)")]
        [SerializeField] Transform _baseRoot;

        /// Base インスタンスの親 Transform。
        /// 直下の子は Base インスタンスのみが存在することを呼び出し側が保証する。
        /// PlaceBase はこの直下の全子を破棄する。
        public Transform BaseRoot => _baseRoot;
    }
}
