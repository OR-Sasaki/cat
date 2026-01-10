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

    [CreateAssetMenu(fileName = "Furniture", menuName = "Cat/Furniture")]
    public class Furniture : ScriptableObject
    {
        public FurnitureType FurnitureType;
        public Sprite Thumbnail;
        public IsoDraggableView SceneObject;
    }
}
