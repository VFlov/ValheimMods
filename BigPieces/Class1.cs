using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ServerSync;
using System.Collections.Generic;
using UnityEngine;
using static CharacterAnimEvent;
using static Player;

namespace BigPieces
{
    [BepInPlugin("vsp.BigPieces", "BigPieces", "1.1.1")]
    public class BigPieces : BaseUnityPlugin
    {
        private CustomLocalization Localization;

        static readonly ConfigSync configSync = new ConfigSync("vsp.BigPieces") { DisplayName = "BigPieces", CurrentVersion = "1.1.1", MinimumRequiredVersion = "1.1.1" };
        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

        private void ConfigAdd()
        {

            serverConfigLocked = config<Toggle>("General", "Lock Configuration", Toggle.On, new ConfigDescription("If On, the configuration is locked and can be changed by server admins only. Value 'true' enable server sinc"), true);
            configSync.AddLockingConfigEntry<Toggle>(serverConfigLocked);
            costMultiplier = config<float>("Parameters", "Recipe cost multiplier", 1, new ConfigDescription("If the value is 1, the cost will correspond to the size of the piece. If you want to reward players for using big pieces, set the value to less than 1.", new AcceptableValueRange<float>(0, 10)), true);

        }


        private void Awake()
        {
            ConfigAdd();
            PrefabManager.OnVanillaPrefabsAvailable += WoodBeam;
            PrefabManager.OnVanillaPrefabsAvailable += WoodPole;
            PrefabManager.OnVanillaPrefabsAvailable += WoodBeam45;
            PrefabManager.OnVanillaPrefabsAvailable += WoodFloor;
            PrefabManager.OnVanillaPrefabsAvailable += WoodStair;
            PrefabManager.OnVanillaPrefabsAvailable += WoodStairBiggest;
            PrefabManager.OnVanillaPrefabsAvailable += WoodWoodWallHalf;
            PrefabManager.OnVanillaPrefabsAvailable += WoodWoodWall;
            PrefabManager.OnVanillaPrefabsAvailable += WoodRoof45;
            PrefabManager.OnVanillaPrefabsAvailable += StoneFloor;
            PrefabManager.OnVanillaPrefabsAvailable += StoneFloorNew;
            PrefabManager.OnVanillaPrefabsAvailable += StoneStair;
            PrefabManager.OnVanillaPrefabsAvailable += BlackMarbleFloor;
            PrefabManager.OnVanillaPrefabsAvailable += BlackMarbleStair;
            PrefabManager.OnVanillaPrefabsAvailable += BackMarbleFloorNew;
            PrefabManager.OnVanillaPrefabsAvailable += BlackMarbleColumn1;
            PrefabManager.OnVanillaPrefabsAvailable += BlackMarbleColumn2;
            PrefabManager.OnVanillaPrefabsAvailable += BlackMarbleColumn3;
            PrefabManager.OnVanillaPrefabsAvailable += CrystalWall;
            AddLocalizations();
        }
        private void WoodBeam()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_wood_beam", "wood_beam");

            gameObject.transform.localScale = new Vector3(2, 1, 1);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_wood_beam";
            pieceConfig.Description = "$piece_big_wood_beam_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Wood", (int)(4 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= WoodBeam;
        }
        public void WoodPole()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_wood_pole2", "wood_pole2");

            gameObject.transform.localScale = new Vector3(1, 2, 1);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_wood_pole2";
            pieceConfig.Description = "$piece_big_wood_pole2_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Wood", (int)(4 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= WoodPole;
        }
        public void WoodBeam45()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_wood_beam_45", "wood_beam_45");

            gameObject.transform.localScale = new Vector3(2, 2, 1);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_wood_beam_45";
            pieceConfig.Description = "$piece_big_wood_beam_45_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Wood", (int)(4 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= WoodBeam45;
        }
        private void WoodFloor()
        {

            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_wood_floor", "wood_floor");

            gameObject.transform.localScale = new Vector3(2, 1, 2);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_wood_floor";
            pieceConfig.Description = "$piece_big_wood_floor_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Wood", (int)(8 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= WoodFloor;
        }
        public void WoodStair()
        {

            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_wood_stair", "wood_stair");

            gameObject.transform.localScale = new Vector3(1.5f, 1, 1.5f);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_wood_stair";
            pieceConfig.Description = "$piece_big_wood_stair_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Wood", (int)(8 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= WoodStair;
        }
        public void WoodStairBiggest()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_wood_stair_biggest", "wood_stair");

            gameObject.transform.localScale = new Vector3(1.5f, 2, 1.5f);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_wood_stair_biggest";
            pieceConfig.Description = "$piece_big_wood_stair_biggest_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Wood", (int)(8 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= WoodStairBiggest;
        }
        public void WoodWoodWallHalf()
        {

            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_wood_wall_half", "wood_wall_half");

            gameObject.transform.localScale = new Vector3(2, 1, 1);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_wood_wall_half";
            pieceConfig.Description = "$piece_big_wood_wall_half_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Wood", (int)(2 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= WoodWoodWallHalf;
        }
        public void WoodWoodWall()
        {

            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_wood_wall", "woodwall");

            gameObject.transform.localScale = new Vector3(2, 2, 1);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_wood_wall";
            pieceConfig.Description = "$piece_big_wood_wall_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Wood", (int)(8 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= WoodWoodWall;
        }
        public void WoodRoof45()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_wood_roof_45", "wood_roof_45");

            gameObject.transform.localScale = new Vector3(2, 2, 2);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_wood_roof_45";
            pieceConfig.Description = "$piece_big_wood_roof_45_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Wood", (int)(8 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= WoodRoof45;
        }
        public void StoneFloor()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_stone_floor_2x2", "stone_floor_2x2");

            gameObject.transform.localScale = new Vector3(2, 1, 2);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_stone_floor_2x2";
            pieceConfig.Description = "$piece_big_stone_floor_2x2_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Stone", (int)(24 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= StoneFloor;
        }
        public void StoneFloorNew()
        {
            GameObject gameObject = PrefabManager.Instance.GetPrefab("stone_floor");
            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_stone_floor";
            pieceConfig.Description = "$piece_stone_floor_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Stone", (int)(24 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= StoneFloorNew;
        }
        public void StoneStair()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_stone_stair", "stone_stair");

            gameObject.transform.localScale = new Vector3(2, 2, 2);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_stone_stair";
            pieceConfig.Description = "$piece_big_stone_stair_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Stone", (int)(32 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= StoneStair;
        }
        public void BlackMarbleFloor()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_blackmarble_floor", "blackmarble_floor");

            gameObject.transform.localScale = new Vector3(2, 1, 2);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_blackmarble_floor";
            pieceConfig.Description = "$piece_big_blackmarble_floor_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("BlackMarble", (int)(16 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= BlackMarbleFloor;
        }
        public void BlackMarbleStair()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_blackmarble_stair", "blackmarble_stair");

            gameObject.transform.localScale = new Vector3(2, 2, 2);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_blackmarble_stair";
            pieceConfig.Description = "$piece_big_blackmarble_stair_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("BlackMarble", (int)(16 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= BlackMarbleStair;
        }
        public void BackMarbleFloorNew()
        {
            GameObject gameObject = PrefabManager.Instance.GetPrefab("blackmarble_floor_large");

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_blackmarble_floor_large";
            pieceConfig.Description = "$piece_blackmarble_floor_large_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("BlackMarble", (int)(32 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= BackMarbleFloorNew;
        }

        public void BlackMarbleColumn1()
        {
            GameObject gameObject = PrefabManager.Instance.GetPrefab("blackmarble_column_1");

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_blackmarble_column_1";
            pieceConfig.Description = "$piece_blackmarble_column_1_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("BlackMarble", (int)(2 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= BlackMarbleColumn1;
        }
        public void BlackMarbleColumn2()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_blackmarble_column_2", "blackmarble_column_2");

            gameObject.transform.localScale = new Vector3(1, 2, 1);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_blackmarble_column_2";
            pieceConfig.Description = "$piece_big_blackmarble_column_2_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("BlackMarble", (int)(8 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= BlackMarbleColumn2;
        }
        public void BlackMarbleColumn3()
        {
            GameObject gameObject = PrefabManager.Instance.GetPrefab("blackmarble_column_3");

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_blackmarble_column_3";
            pieceConfig.Description = "$piece_blackmarble_column_3_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("BlackMarble", (int)(32 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= BlackMarbleColumn3;
        }

        public void CrystalWall()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("big_crystal_wall_1x1", "crystal_wall_1x1");

            gameObject.transform.localScale = new Vector3(2, 2, 1);

            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_big_crystal_wall_1x1";
            pieceConfig.Description = "$piece_big_crystal_wall_1x1";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Workbench;
            pieceConfig.Category = "Bigger";
            pieceConfig.AddRequirement(new RequirementConfig("Crystal", (int)(8 * costMultiplier.Value), 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, fixReference: false, pieceConfig));
            PrefabManager.OnVanillaPrefabsAvailable -= CrystalWall;
        }

        public void AddLocalizations()
        {
            Localization = LocalizationManager.Instance.GetLocalization();
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                {"piece_big_wood_beam", "Wood beam" },
                {"piece_big_wood_beam_description", "Just a bigger wood beam" },
                {"piece_big_wood_pole2", "Wood pole biggest" },
                {"piece_big_wood_pole2_description", "Just a bigger wood pole" },
                {"piece_big_wood_beam_45", "Wood pole 45 biggest" },
                {"piece_big_wood_beam_45_description", "Just a bigger wood pole 45" },
                {"piece_big_wood_floor", "Wood floor" },
                {"piece_big_wood_floor_description", "Just a bigger wood floor" },
                {"piece_big_wood_stair", "Wood stair" },
                {"piece_big_wood_stair_description", "Just a bigger wood stair" },
                {"piece_big_wood_stair_biggest", "Wood stair" },
                {"piece_big_wood_stair_biggest_description", "Just a biggest wood stair" },
                {"piece_big_wood_wall_half", "Wood stair biggest" },
                {"piece_big_wood_wall_half_description", "Just a bigger wood wall half" },
                {"piece_big_wood_wall", "Wood stair biggest" },
                {"piece_big_wood_wall_description", "Just a bigger wood wall" },
                {"piece_big_wood_roof_45", "Wood roof45 biggest" },
                {"piece_big_wood_roof_45_description", "Just a bigger wood roof 45" },
                {"piece_big_stone_floor_2x2", "Stone floor biggest" },
                {"piece_big_stone_floor_2x2_description", "Just a bigger stone floor" },
                {"piece_stone_floor", "Stone floor biggest" },
                {"piece_stone_floor_description", "Just a bigger stone floor" },
                {"piece_big_stone_stair", "Stone stair biggest" },
                {"piece_big_stone_stair_description", "Just a bigger stone stair" },
                {"piece_big_blackmarble_floor", "Black marble floor big" },
                {"piece_big_blackmarble_floor_description", "Just a bigger black marble foor" },
                {"piece_big_blackmarble_stair", "Black marble stair big" },
                {"piece_big_blackmarble_stair_description", "Just a bigger black marble stair" },
                {"piece_blackmarble_floor_large", "Black marble floor biggest" },
                {"piece_blackmarble_floor_large_description", "Just a bigger black marble floor" },
                {"piece_blackmarble_column_1", "Black marble column small" },
                {"piece_blackmarble_column_1_description", "Just a small black marble column" },
                {"piece_big_blackmarble_column_2", "Black marble column biggest" },
                {"piece_big_blackmarble_column_2_description", "Just a bigger black marble column" },
                {"piece_blackmarble_column_3", "Black marble column" },
                {"piece_blackmarble_column_3_description", "Just a big black marble column" },
                {"piece_big_crystal_wall_1x1", "Crystal wall biggest" },
                {"piece_big_crystal_wall_1x1_description", "Just a bigger crystal wall" },
            });
            Localization.AddTranslation("Russian", new Dictionary<string, string>
            {
                {"piece_big_wood_beam", "Деревянная балка" },
                {"piece_big_wood_beam_description", "Большая деревянная балка" },
                {"piece_big_wood_pole2", "Деревянный столб" },
                {"piece_big_wood_pole2_description", "Большой деревянный столб" },
                {"piece_big_wood_beam_45", "Деревянный столб 45" },
                {"piece_big_wood_beam_45_description", "Большой деревянный столб 45" },
                {"piece_big_wood_floor", "Деревянный пол" },
                {"piece_big_wood_floor_description", "Большой деревянный пол" },
                {"piece_big_wood_stair", "Деревянные ступеньки" },
                {"piece_big_wood_stair_description", "Скошенные большие деревянные ступеньки" },
                {"piece_big_wood_stair_biggest", "Деревянные ступеньки" },
                {"piece_big_wood_stair_biggest_description", "Большие деревянные ступеньки" },
                {"piece_big_wood_wall_half", "Деревянная стена (половина)" },
                {"piece_big_wood_wall_half_description", "Большая деревянная стена (половина)" },
                {"piece_big_wood_wall", "Деревянная стена" },
                {"piece_big_wood_wall_description", "Большая деревянная стена" },
                {"piece_big_wood_roof_45", "Деревянная крыша 45" },
                {"piece_big_wood_roof_45_description", "Большая деревянная крыша 45" },
                {"piece_big_stone_floor_2x2", "Каменный пол" },
                {"piece_big_stone_floor_2x2_description", "Большой каменный пол" },
                {"piece_stone_floor", "Каменный пол 2" },
                {"piece_stone_floor_description", "Большой каменный пол 2" },
                {"piece_big_stone_stair", "Каменные ступеньки" },
                {"piece_big_stone_stair_description", "Большие каменный ступеньки" },
                {"piece_big_blackmarble_floor", "Пол из черного мрамора" },
                {"piece_big_blackmarble_floor_description", "Большой пол из черного мрамора" },
                {"piece_big_blackmarble_stair", "Ступеньки из черного мрамора" },
                {"piece_big_blackmarble_stair_description", "Большие ступеньки из черного мрамора" },
                {"piece_blackmarble_floor_large", "Пол из черного мрамора" },
                {"piece_blackmarble_floor_large_description", "Огромный пол из черного мрамора" },
                {"piece_blackmarble_column_1", "Колонна из черного мрамора" },
                {"piece_blackmarble_column_1_description", "Маленькая колонна из черного мрамора" },
                {"piece_big_blackmarble_column_2", "Колонна из черного мрамора" },
                {"piece_big_blackmarble_column_2_description", "Большая колонна из черного мрамора "},
                {"piece_blackmarble_column_3", "Колонна из черного мрамора" },
                {"piece_blackmarble_column_3_description", "Огромная колонна из черного мрамора" },
                {"piece_big_crystal_wall_1x1", "Кристальная стена" },
                {"piece_big_crystal_wall_1x1_description", "Большая кристальная стена" },
            });
        }
        private static ConfigEntry<Toggle> serverConfigLocked;
        private static ConfigEntry<float> costMultiplier;
        public enum Toggle
        {

            On = 1,
            Off = 0
        }

    }
}
