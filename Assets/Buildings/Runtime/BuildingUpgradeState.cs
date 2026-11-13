using System;
using System.IO;
using UnityEngine;

namespace ParadiseTycoon.Buildings
{
    // The shared game state implements this contract when it takes over storage and wave state.
    public interface IBuildingUpgradeState
    {
        event Action Changed;
        int Coins { get; }
        bool IsWaveActive { get; }
        int GetBuildingLevel(string buildingId, int initialLevel);

        // Validate wave state, balance and expected level, then persist coins and level together.
        bool TryPurchaseUpgrade(string buildingId, int expectedLevel, int cost);
    }

    internal sealed class LocalBuildingUpgradeState : IBuildingUpgradeState
    {
        [Serializable]
        sealed class SavedBuilding
        {
            public string id;
            public int level;
        }

        [Serializable]
        sealed class SaveData
        {
            public int version = 1;
            public int coins;
            public SavedBuilding[] buildings;
        }

        readonly string path;
        readonly ResortBuilding[] buildings;
        SaveData data;
        bool canSave = true;

        public event Action Changed;
        public int Coins => data.coins;
        public bool IsWaveActive { get; private set; }

        public LocalBuildingUpgradeState(string path, int initialCoins, ResortBuilding[] buildings)
        {
            this.path = path;
            this.buildings = buildings;
            data = new SaveData
            {
                coins = initialCoins,
                buildings = Array.ConvertAll(buildings, building =>
                    new SavedBuilding { id = building.BuildingId, level = building.InitialLevel })
            };

            try
            {
                if (!File.Exists(path)) return;
                var saved = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (saved == null || saved.version != 1 || saved.coins < 0 || saved.buildings == null)
                    throw new InvalidDataException("Invalid building upgrade save.");

                foreach (var building in buildings)
                {
                    var entry = Array.Find(saved.buildings, value => value != null && value.id == building.BuildingId);
                    if (entry == null || entry.level < building.InitialLevel || entry.level > building.MaxLevel)
                        throw new InvalidDataException("Invalid level for " + building.BuildingId);
                }

                data = saved;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                canSave = false;
                Debug.LogWarning("Could not read building upgrade save: " + exception.Message);
            }
        }

        public int GetBuildingLevel(string buildingId, int initialLevel)
        {
            return Array.Find(data.buildings, value => value != null && value.id == buildingId)?.level ?? initialLevel;
        }

        public bool TryPurchaseUpgrade(string buildingId, int expectedLevel, int cost)
        {
            if (!canSave || IsWaveActive || expectedLevel < 1 || cost <= 0 || Coins < cost) return false;
            var building = Array.Find(buildings, item => item.BuildingId == buildingId);
            if (building?.GetLevel(expectedLevel + 1)?.Cost != cost) return false;

            var current = Array.Find(data.buildings, value => value != null && value.id == buildingId);
            if (current == null || current.level != expectedLevel) return false;

            // Publish the new state only after its complete snapshot has replaced the previous save.
            int previousCoins = data.coins;
            data.coins -= cost;
            current.level++;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string pending = path + ".tmp";
                File.WriteAllText(pending, JsonUtility.ToJson(data));
                if (File.Exists(path))
                    File.Replace(pending, path, null);
                else
                    File.Move(pending, path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                data.coins = previousCoins;
                current.level = expectedLevel;
                Debug.LogWarning("Could not save building upgrade: " + exception.Message);
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        public void SetWaveActive(bool active)
        {
            if (active == IsWaveActive) return;
            IsWaveActive = active;
            Changed?.Invoke();
        }
    }
}
