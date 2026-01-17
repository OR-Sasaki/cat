using System.Linq;
using Root.State;
using UnityEngine;
using VContainer.Unity;

namespace Root.Service
{
    public class UserDataImportService : IInitializable
    {
        readonly UserState _userState;

        public UserDataImportService(UserState userState)
        {
            _userState = userState;
        }

        public void Initialize()
        {
            ImportUserOutfits();
            ImportUserFurnitures();
        }

        void ImportUserOutfits()
        {
            var csv = Resources.Load<TextAsset>("user_outfits");
            if (csv is null)
            {
                Debug.LogError("[UserDataImportService] user_outfits.csv not found");
                return;
            }

            var lines = csv.text.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
            _userState.UserOutfits = lines.Select(line =>
            {
                var columns = line.Split(',');
                return new UserOutfit
                {
                    OutfitID = uint.Parse(columns[0].Trim())
                };
            }).ToArray();
        }

        void ImportUserFurnitures()
        {
            var csv = Resources.Load<TextAsset>("user_furnitures");
            if (csv is null)
            {
                Debug.LogError("[UserDataImportService] user_furnitures.csv not found");
                return;
            }

            var lines = csv.text.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
            _userState.UserFurnitures = lines.Select(line =>
            {
                var columns = line.Split(',');
                return new UserFurniture
                {
                    FurnitureID = uint.Parse(columns[1].Trim())
                };
            }).ToArray();
        }
    }
}
