using Home.View;
using UnityEngine;

namespace Cat.Furniture
{
    public enum FurnitureType
    {
        Base = 1,
        Floor = 2,
        Small = 3,
        Wall = 4,
    }

    public enum PlacementType // 配置場所
    {
        Floor = 1, // 床に配置
        Wall = 2,  // 壁に配置
        Base = 3, // 部屋そのもの
    }

    /// 家具の ScriptableObject 定義。
    /// 不変条件: PlacementType により参照すべきシーンオブジェクトフィールドが排他的に決まる。
    /// - PlacementType == Floor / Wall: SceneObject != null かつ BaseSceneObject == null
    /// - PlacementType == Base        : BaseSceneObject != null かつ SceneObject == null
    /// Base はドラッグ操作・IsoGrid 占有の対象外であり、Base プレハブには IsoDraggableView をアタッチしない。
    [CreateAssetMenu(fileName = "Furniture", menuName = "Cat/Furniture")]
    public class Furniture : ScriptableObject
    {
        public FurnitureType FurnitureType;
        public PlacementType PlacementType;
        public Sprite Thumbnail;
        /// Floor / Wall 配置用のシーンオブジェクト参照 (IsoDraggableView)。Base 家具では null のまま運用する。
        public IsoDraggableView SceneObject;
        /// Base 配置用のシーンオブジェクトのルート Transform。Floor / Wall 家具では null のまま運用する。
        public Transform BaseSceneObject;
    }
}
