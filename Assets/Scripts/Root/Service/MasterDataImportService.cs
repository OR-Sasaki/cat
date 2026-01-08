using System.Linq;
using Root.State;
using UnityEngine;

namespace Root.Service
{
    public class MasterDataImportService
    {
        readonly MasterDataState _masterDataState;

        public MasterDataImportService(MasterDataState masterDataState)
        {
            _masterDataState = masterDataState;
        }

        public void Import()
        {
            if (_masterDataState.IsImported) return;

            ImportOutfits();
            ImportFurnitures();

            _masterDataState.IsImported = true;
        }

        void ImportOutfits()
        {
            var csv = Resources.Load<TextAsset>("outfits");
            if (csv is null)
            {
                Debug.LogError("[MasterDataImportService] outfit.csv not found");
                return;
            }

            var lines = csv.text.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
            _masterDataState.Outfits = lines.Select(line =>
            {
                var columns = line.Split(',');
                return new Outfit
                {
                    Id = uint.Parse(columns[0].Trim()),
                    Type = columns[1].Trim(),
                    Name = columns[2].Trim()
                };
            }).ToArray();
        }

        void ImportFurnitures()
        {
            var csv = Resources.Load<TextAsset>("furnitures");
            if (csv is null)
            {
                Debug.LogError("[MasterDataImportService] furniture.csv not found");
                return;
            }

            var lines = csv.text.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
            _masterDataState.Furnitures = lines.Select(line =>
            {
                var columns = line.Split(',');
                return new Furniture
                {
                    Id = uint.Parse(columns[0].Trim()),
                    Type = columns[1].Trim(),
                    Name = columns[2].Trim()
                };
            }).ToArray();
        }
    }
}
