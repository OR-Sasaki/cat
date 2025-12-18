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

            var csv = Resources.Load<TextAsset>("outfit");
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

            _masterDataState.IsImported = true;
        }
    }
}
