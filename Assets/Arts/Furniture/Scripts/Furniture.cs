using Home.View;
using UnityEngine;

namespace Cat.Furniture
{
    public enum FurnitureType // アルファベット順に並べること
    {
        Base,
        Floor,
        Small,
        Wall,
    }

    public enum PlacementType // 配置場所
    {
        Floor, // 床に配置
        Wall,  // 壁に配置
    }

    [CreateAssetMenu(fileName = "Furniture", menuName = "Cat/Furniture")]
    public class Furniture : ScriptableObject
    {
        public FurnitureType FurnitureType;
        public PlacementType PlacementType;
        public Sprite Thumbnail;
        public IsoDraggableView SceneObject;
    }
}
