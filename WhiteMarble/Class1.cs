using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using Mono.Mozilla;
using UnityEngine;

namespace WhiteMarble
{
    [BepInPlugin("vaffle.WhiteMarble", "WhiteMarble", "1.2.0")]
    public class Class1 : BaseUnityPlugin
    {
        private void Awake()
        {
            PrefabManager.OnVanillaPrefabsAvailable += this.MarblePieces;
            this.AddLocalizations();
        }
        private void MarblePieces()
        {
            this.TextureFind();
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("whitemarble_1x1", "blackmarble_1x1");
            this.ComponentFind(gameObject, "stone_hgih", "stone_low");
            PieceConfig pieceConfig = new PieceConfig();
            pieceConfig.Name = "$piece_whitemarble_1x1";
            pieceConfig.Description = "$piece_whitemarble_1x1_description";
            pieceConfig.PieceTable = PieceTables.Hammer;
            pieceConfig.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig.Category = "WhiteMarble";
            pieceConfig.AddRequirement(new RequirementConfig("BlackMarble", 2, 0, true));
            pieceConfig.AddRequirement(new RequirementConfig("BoneFragments", 1, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject, false, pieceConfig));
            GameObject gameObject2 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_2x1x1", "blackmarble_2x1x1");
            this.ComponentFind(gameObject2, "stone_high", "stone_low");
            PieceConfig pieceConfig2 = new PieceConfig();
            pieceConfig2.Name = "$piece_whitemarble_2x1x1";
            pieceConfig2.Description = "$piece_whitemarble_2x1x1_description";
            pieceConfig2.PieceTable = PieceTables.Hammer;
            pieceConfig2.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig2.Category = "WhiteMarble";
            pieceConfig2.AddRequirement(new RequirementConfig("BlackMarble", 4, 0, true));
            pieceConfig2.AddRequirement(new RequirementConfig("BoneFragments", 2, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject2, false, pieceConfig2));
            GameObject gameObject3 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_2x2x2", "blackmarble_2x2x2");
            this.ComponentFind(gameObject3, "stone_hgih", "stone_low");
            PieceConfig pieceConfig3 = new PieceConfig();
            pieceConfig3.Name = "$piece_whitemarble_2x2x2";
            pieceConfig3.Description = "$piece_whitemarble_2x2x2_description";
            pieceConfig3.PieceTable = PieceTables.Hammer;
            pieceConfig3.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig3.Category = "WhiteMarble";
            pieceConfig3.AddRequirement(new RequirementConfig("BlackMarble", 8, 0, true));
            pieceConfig3.AddRequirement(new RequirementConfig("BoneFragments", 4, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject3, false, pieceConfig3));
            GameObject gameObject4 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_floor", "blackmarble_floor");
            this.ComponentFind(gameObject4, "stone_high", "stone_low");
            PieceConfig pieceConfig4 = new PieceConfig();
            pieceConfig4.Name = "$piece_whitemarble_floor";
            pieceConfig4.Description = "$piece_whitemarble_floor_description";
            pieceConfig4.PieceTable = PieceTables.Hammer;
            pieceConfig4.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig4.Category = "WhiteMarble";
            pieceConfig4.AddRequirement(new RequirementConfig("BlackMarble", 8, 0, true));
            pieceConfig4.AddRequirement(new RequirementConfig("BoneFragments", 4, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject4, false, pieceConfig4));
            GameObject gameObject5 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_floor_triangle", "blackmarble_floor_triangle");
            this.ComponentFind(gameObject5, "stone_hgih", "stone_low");
            PieceConfig pieceConfig5 = new PieceConfig();
            pieceConfig5.Name = "$piece_whitemarble_floor_triangle";
            pieceConfig5.Description = "$piece_whitemarble_floor_triangle_description";
            pieceConfig5.PieceTable = PieceTables.Hammer;
            pieceConfig5.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig5.Category = "WhiteMarble";
            pieceConfig5.AddRequirement(new RequirementConfig("BlackMarble", 3, 0, true));
            pieceConfig5.AddRequirement(new RequirementConfig("BoneFragments", 2, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject5, false, pieceConfig5));
            GameObject gameObject6 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_stair", "blackmarble_stair");
            this.ComponentFind(gameObject6, "high", "low");
            PieceConfig pieceConfig6 = new PieceConfig();
            pieceConfig6.Name = "$piece_whitemarble_stair";
            pieceConfig6.Description = "$piece_whitemarble_stair_description";
            pieceConfig6.PieceTable = PieceTables.Hammer;
            pieceConfig6.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig6.Category = "WhiteMarble";
            pieceConfig6.AddRequirement(new RequirementConfig("BlackMarble", 8, 0, true));
            pieceConfig6.AddRequirement(new RequirementConfig("BoneFragments", 4, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject6, false, pieceConfig6));
            GameObject gameObject7 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_tip", "blackmarble_tip");
            this.ComponentFind(gameObject7, "stone_hgih", "stone_low");
            PieceConfig pieceConfig7 = new PieceConfig();
            pieceConfig7.Name = "$piece_whitemarble_tip";
            pieceConfig7.Description = "$piece_whitemarble_tip_description";
            pieceConfig7.PieceTable = PieceTables.Hammer;
            pieceConfig7.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig7.Category = "WhiteMarble";
            pieceConfig7.AddRequirement(new RequirementConfig("BlackMarble", 2, 0, true));
            pieceConfig7.AddRequirement(new RequirementConfig("BoneFragments", 1, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject7, false, pieceConfig7));
            GameObject gameObject8 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_base_1", "blackmarble_base_1");
            this.ComponentFind(gameObject8, "stone_high", "stone_low");
            PieceConfig pieceConfig8 = new PieceConfig();
            pieceConfig8.Name = "$piece_whitemarble_base_1";
            pieceConfig8.Description = "$piece_whitemarble_base_1_description";
            pieceConfig8.PieceTable = PieceTables.Hammer;
            pieceConfig8.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig8.Category = "WhiteMarble";
            pieceConfig8.AddRequirement(new RequirementConfig("BlackMarble", 5, 0, true));
            pieceConfig8.AddRequirement(new RequirementConfig("BoneFragments", 3, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject8, false, pieceConfig8));
            GameObject gameObject9 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_base_2", "blackmarble_base_2");
            this.ComponentFind(gameObject9, "stone_high", "stone_low");
            PieceConfig pieceConfig9 = new PieceConfig();
            pieceConfig9.Name = "$piece_whitemarble_base_2";
            pieceConfig9.Description = "$piece_whitemarble_base_2_description";
            pieceConfig9.PieceTable = PieceTables.Hammer;
            pieceConfig9.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig9.Category = "WhiteMarble";
            pieceConfig9.AddRequirement(new RequirementConfig("BlackMarble", 10, 0, true));
            pieceConfig9.AddRequirement(new RequirementConfig("BoneFragments", 5, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject9, false, pieceConfig9));
            GameObject gameObject10 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_basecorner", "blackmarble_basecorner");
            this.ComponentFind(gameObject10, "stone_high", "stone_low");
            PieceConfig pieceConfig10 = new PieceConfig();
            pieceConfig10.Name = "$piece_whitemarble_basecorner";
            pieceConfig10.Description = "$piece_whitemarble_basecorner_description";
            pieceConfig10.PieceTable = PieceTables.Hammer;
            pieceConfig10.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig10.Category = "WhiteMarble";
            pieceConfig10.AddRequirement(new RequirementConfig("BlackMarble", 6, 0, true));
            pieceConfig10.AddRequirement(new RequirementConfig("BoneFragments", 3, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject10, false, pieceConfig10));
            GameObject gameObject11 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_out_1", "blackmarble_out_1");
            this.ComponentFind(gameObject11, "stone_high", "stone_low");
            PieceConfig pieceConfig11 = new PieceConfig();
            pieceConfig11.Name = "$piece_whitemarble_out_1";
            pieceConfig11.Description = "$piece_whitemarble_out_1_description";
            pieceConfig11.PieceTable = PieceTables.Hammer;
            pieceConfig11.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig11.Category = "WhiteMarble";
            pieceConfig11.AddRequirement(new RequirementConfig("BlackMarble", 5, 0, true));
            pieceConfig11.AddRequirement(new RequirementConfig("BoneFragments", 3, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject11, false, pieceConfig11));
            GameObject gameObject12 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_out_2", "blackmarble_out_2");
            this.ComponentFind(gameObject12, "stone_high", "stone_low");
            PieceConfig pieceConfig12 = new PieceConfig();
            pieceConfig12.Name = "$piece_whitemarble_out_2";
            pieceConfig12.Description = "$piece_whitemarble_out_2_description";
            pieceConfig12.PieceTable = PieceTables.Hammer;
            pieceConfig12.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig12.Category = "WhiteMarble";
            pieceConfig12.AddRequirement(new RequirementConfig("BlackMarble", 10, 0, true));
            pieceConfig12.AddRequirement(new RequirementConfig("BoneFragments", 5, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject12, false, pieceConfig12));
            GameObject gameObject13 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_outcorner", "blackmarble_outcorner");
            this.ComponentFind(gameObject13, "stone_high", "stone_low");
            PieceConfig pieceConfig13 = new PieceConfig();
            pieceConfig13.Name = "$piece_whitemarble_outcorner";
            pieceConfig13.Description = "$piece_whitemarble_outcorner_description";
            pieceConfig13.PieceTable = PieceTables.Hammer;
            pieceConfig13.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig13.Category = "WhiteMarble";
            pieceConfig13.AddRequirement(new RequirementConfig("BlackMarble", 6, 0, true));
            pieceConfig12.AddRequirement(new RequirementConfig("BoneFragments", 3, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject13, false, pieceConfig13));
            GameObject gameObject14 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_arch", "blackmarble_arch");
            this.ComponentFind(gameObject14, "stone_high", "stone_low");
            PieceConfig pieceConfig14 = new PieceConfig();
            pieceConfig14.Name = "$piece_whitemarble_arch";
            pieceConfig14.Description = "$piece_whitemarble_arch_description";
            pieceConfig14.PieceTable = PieceTables.Hammer;
            pieceConfig14.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig14.Category = "WhiteMarble";
            pieceConfig14.AddRequirement(new RequirementConfig("BlackMarble", 5, 0, true));
            pieceConfig14.AddRequirement(new RequirementConfig("BoneFragments", 3, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject14, false, pieceConfig14));
            GameObject gameObject15 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_column_2", "blackmarble_column_2");
            this.ComponentFind(gameObject15, "stone_high", "stone_low");
            PieceConfig pieceConfig15 = new PieceConfig();
            pieceConfig15.Name = "$piece_whitemarble_column_2";
            pieceConfig15.Description = "$piece_whitemarble_column_2_description";
            pieceConfig15.PieceTable = PieceTables.Hammer;
            pieceConfig15.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig15.Category = "WhiteMarble";
            pieceConfig15.AddRequirement(new RequirementConfig("BlackMarble", 4, 0, true));
            pieceConfig15.AddRequirement(new RequirementConfig("BoneFragments", 2, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject15, false, pieceConfig15));
            GameObject gameObject16 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_2x2_enforced", "blackmarble_2x2_enforced");
            this.ComponentFind(gameObject16, "stone_hgih", "stone_low");
            PieceConfig pieceConfig16 = new PieceConfig();
            pieceConfig16.Name = "$piece_whitemarble_2x2_enforced";
            pieceConfig16.Description = "$piece_whitemarble_2x2_enforced_description";
            pieceConfig16.PieceTable = PieceTables.Hammer;
            pieceConfig16.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig16.Category = "WhiteMarble";
            pieceConfig16.AddRequirement(new RequirementConfig("BlackMarble", 2, 0, true));
            pieceConfig16.AddRequirement(new RequirementConfig("BoneFragments", 2, 0, true));
            pieceConfig16.AddRequirement(new RequirementConfig("Copper", 2, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject16, false, pieceConfig16));
            GameObject gameObject17 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_column_1", "blackmarble_column_1");
            this.ComponentFind(gameObject17, "stone_high", "stone_low");
            PieceConfig pieceConfig17 = new PieceConfig();
            pieceConfig17.Name = "$piece_whitemarble_column_1";
            pieceConfig17.Description = "$piece_whitemarble_column_1_description";
            pieceConfig17.PieceTable = PieceTables.Hammer;
            pieceConfig17.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig17.Category = "WhiteMarble";
            pieceConfig17.AddRequirement(new RequirementConfig("BlackMarble", 2, 0, true));
            pieceConfig17.AddRequirement(new RequirementConfig("BoneFragments", 1, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject17, false, pieceConfig17));
            GameObject gameObject18 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_floor_large", "blackmarble_floor_large");
            this.ComponentFind(gameObject18, "high", "low");
            PieceConfig pieceConfig18 = new PieceConfig();
            pieceConfig18.Name = "$piece_whitemarble_floor_large";
            pieceConfig18.Description = "$piece_whitemarble_floor_large_description";
            pieceConfig18.PieceTable = PieceTables.Hammer;
            pieceConfig18.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig18.Category = "WhiteMarble";
            pieceConfig18.AddRequirement(new RequirementConfig("BlackMarble", 32, 0, true));
            pieceConfig18.AddRequirement(new RequirementConfig("BoneFragments", 16, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject18, false, pieceConfig18));
            GameObject gameObject19 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_head_big01", "blackmarble_head_big01");
            this.ComponentFind(gameObject19, "head_high", "head_low");
            PieceConfig pieceConfig19 = new PieceConfig();
            pieceConfig19.Name = "$piece_whitemarble_head_big01";
            pieceConfig19.Description = "$piece_whitemarble_head_big01_description";
            pieceConfig19.PieceTable = PieceTables.Hammer;
            pieceConfig19.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig19.Category = "WhiteMarble";
            pieceConfig19.AddRequirement(new RequirementConfig("BlackMarble", 4, 0, true));
            pieceConfig19.AddRequirement(new RequirementConfig("BoneFragments", 2, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject19, false, pieceConfig19));
            GameObject gameObject20 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_head_big02", "blackmarble_head_big02");
            this.ComponentFind(gameObject20, "head_high", "head_low");
            PieceConfig pieceConfig20 = new PieceConfig();
            pieceConfig20.Name = "$piece_whitemarble_head_big02";
            pieceConfig20.Description = "$piece_whitemarble_head_big02_description";
            pieceConfig20.PieceTable = PieceTables.Hammer;
            pieceConfig20.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig20.Category = "WhiteMarble";
            pieceConfig20.AddRequirement(new RequirementConfig("BlackMarble", 4, 0, true));
            pieceConfig20.AddRequirement(new RequirementConfig("BoneFragments", 2, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject20, false, pieceConfig20));
            GameObject gameObject21 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_slope_1x2", "blackmarble_slope_1x2");
            this.ComponentFind(gameObject21, "stone_hgih", "stone_low");
            PieceConfig pieceConfig21 = new PieceConfig();
            pieceConfig21.Name = "$piece_whitemarble_slope_1x2";
            pieceConfig21.Description = "$piece_whitemarble_slope_1x2_description";
            pieceConfig21.PieceTable = PieceTables.Hammer;
            pieceConfig21.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig21.Category = "WhiteMarble";
            pieceConfig21.AddRequirement(new RequirementConfig("BlackMarble", 4, 0, true));
            pieceConfig21.AddRequirement(new RequirementConfig("BoneFragments", 2, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject21, false, pieceConfig21));
            GameObject gameObject22 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_slope_inverted_1x2", "blackmarble_slope_inverted_1x2");
            this.ComponentFind(gameObject22, "stone_hgih", "stone_low");
            PieceConfig pieceConfig22 = new PieceConfig();
            pieceConfig22.Name = "$piece_whitemarble_slope_inverted_1x2";
            pieceConfig22.Description = "$piece_whitemarble_slope_inverted_1x2_description";
            pieceConfig22.PieceTable = PieceTables.Hammer;
            pieceConfig22.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig22.Category = "WhiteMarble";
            pieceConfig22.AddRequirement(new RequirementConfig("BlackMarble", 4, 0, true));
            pieceConfig22.AddRequirement(new RequirementConfig("BoneFragments", 2, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject22, false, pieceConfig22));
            GameObject gameObject23 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_tile_floor_1x1", "blackmarble_tile_floor_1x1");
            this.ComponentFind(gameObject23, "stone_hgih", "stone_low");
            PieceConfig pieceConfig23 = new PieceConfig();
            pieceConfig23.Name = "$piece_whitemarble_tile_floor_1x1";
            pieceConfig23.Description = "$piece_whitemarble_tile_floor_1x1_description";
            pieceConfig23.PieceTable = PieceTables.Hammer;
            pieceConfig23.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig23.Category = "WhiteMarble";
            pieceConfig23.AddRequirement(new RequirementConfig("BlackMarble", 1, 0, true));
            pieceConfig23.AddRequirement(new RequirementConfig("BoneFragments", 1, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject23, false, pieceConfig23));
            GameObject gameObject24 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_tile_floor_2x2", "blackmarble_tile_floor_2x2");
            this.ComponentFind(gameObject24, "stone_hgih", "stone_low");
            PieceConfig pieceConfig24 = new PieceConfig();
            pieceConfig24.Name = "$piece_whitemarble_tile_floor_2x2";
            pieceConfig24.Description = "$piece_whitemarble_tile_floor_2x2_description";
            pieceConfig24.PieceTable = PieceTables.Hammer;
            pieceConfig24.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig24.Category = "WhiteMarble";
            pieceConfig24.AddRequirement(new RequirementConfig("BlackMarble", 4, 0, true));
            pieceConfig24.AddRequirement(new RequirementConfig("BoneFragments", 2, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject24, false, pieceConfig24));
            GameObject gameObject25 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_tile_wall_1x1", "blackmarble_tile_wall_1x1");
            this.ComponentFind(gameObject25, "stone_hgih", "stone_low");
            PieceConfig pieceConfig25 = new PieceConfig();
            pieceConfig25.Name = "$piece_whitemarble_tile_wall_1x1";
            pieceConfig25.Description = "$piece_whitemarble_tile_wall_1x1_description";
            pieceConfig25.PieceTable = PieceTables.Hammer;
            pieceConfig25.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig25.Category = "WhiteMarble";
            pieceConfig25.AddRequirement(new RequirementConfig("BlackMarble", 1, 0, true));
            pieceConfig25.AddRequirement(new RequirementConfig("BoneFragments", 1, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject25, false, pieceConfig25));
            GameObject gameObject26 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_tile_wall_2x2", "blackmarble_tile_wall_2x2");
            this.ComponentFind(gameObject26, "stone_hgih", "stone_low");
            PieceConfig pieceConfig26 = new PieceConfig();
            pieceConfig26.Name = "$piece_whitemarble_tile_wall_2x2";
            pieceConfig26.Description = "$piece_whitemarble_tile_wall_2x2_description";
            pieceConfig26.PieceTable = PieceTables.Hammer;
            pieceConfig26.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig26.Category = "WhiteMarble";
            pieceConfig26.AddRequirement(new RequirementConfig("BlackMarble", 4, 0, true));
            pieceConfig26.AddRequirement(new RequirementConfig("BoneFragments", 2, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject26, false, pieceConfig26));
            GameObject gameObject27 = PrefabManager.Instance.CreateClonedPrefab("whitemarble_tile_wall_2x4", "blackmarble_tile_wall_2x4");
            this.ComponentFind(gameObject27, "stone_hgih", "stone_low");
            PieceConfig pieceConfig27 = new PieceConfig();
            pieceConfig27.Name = "$piece_whitemarble_tile_wall_2x4";
            pieceConfig27.Description = "$piece_whitemarble_tile_wall_2x4_description";
            pieceConfig27.PieceTable = PieceTables.Hammer;
            pieceConfig27.CraftingStation = CraftingStations.Stonecutter;
            pieceConfig27.Category = "WhiteMarble";
            pieceConfig27.AddRequirement(new RequirementConfig("BlackMarble", 8, 0, true));
            pieceConfig27.AddRequirement(new RequirementConfig("BoneFragments", 4, 0, true));
            PieceManager.Instance.AddPiece(new CustomPiece(gameObject27, false, pieceConfig27));
            PrefabManager.OnVanillaPrefabsAvailable -= this.MarblePieces;
        }

        // Token: 0x06000003 RID: 3 RVA: 0x00003248 File Offset: 0x00001448
        private void ComponentFind(GameObject prefab, string name1, string name2)
        {
            ExposedGameObjectExtension.FindDeepChild(prefab.gameObject, name1, Utils.IterativeSearchType.BreadthFirst).GetComponent<MeshRenderer>().material.mainTexture = Class1.whiteMarble;
            ExposedGameObjectExtension.FindDeepChild(prefab.gameObject, name2, Utils.IterativeSearchType.BreadthFirst).GetComponent<MeshRenderer>().material.mainTexture = Class1.whiteMarble;
        }

        // Token: 0x06000004 RID: 4 RVA: 0x00003298 File Offset: 0x00001498
        private void TextureFind()
        {
            /*
            if (File.Exists(Path.Combine(Path.GetDirectoryName(base.Info.Location), "Marble.jpg")))
            {
                Class1.whiteMarble = AssetUtils.LoadTexture(Path.Combine(Path.GetDirectoryName(base.Info.Location), "Marble.jpg"), true);
                return;
            }
            if (File.Exists(Path.Combine(Path.GetDirectoryName(base.Info.Location), "Marble.png")))
            {
                Class1.whiteMarble = AssetUtils.LoadTexture(Path.Combine(Path.GetDirectoryName(base.Info.Location), "Marble.png"), true);
                return;
            }
            Class1.whiteMarble = AssetUtils.LoadAssetBundleFromResources("marbletex").LoadAsset<Texture2D>("Assets/whiteMarble.png");
            */
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Загружаем поток из внедренного ресурса
            using (System.IO.Stream stream = assembly.GetManifestResourceStream("WhiteMarble.Resources.marble_tex.png"))
            {
                if (stream != null)
                {
                    // Читаем поток в массив байтов
                    byte[] imageData = new byte[stream.Length];
                    stream.Read(imageData, 0, (int)stream.Length);

                    // Создаем Texture2D
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(imageData); // Автоматически распознает PNG/JPG

                    // Применяем текстуру
                    Class1.whiteMarble = texture;
                    //GetComponent<Renderer>().material.mainTexture = texture;
                }
            }
        }

        // Token: 0x06000005 RID: 5 RVA: 0x0000334C File Offset: 0x0000154C
        public void AddLocalizations()
        {
            this.Localization = LocalizationManager.Instance.GetLocalization();
            CustomLocalization localization = this.Localization;
            string text = "English";
            localization.AddTranslation(text, new Dictionary<string, string>
            {
                {
                    "piece_whitemarble_1x1",
                    "Marble Block"
                },
                {
                    "piece_whitemarble_1x1_description",
                    "Marble Block 1х1"
                },
                {
                    "piece_whitemarble_2x1x1",
                    "Marble Block"
                },
                {
                    "piece_whitemarble_2x1x1_description",
                    "Marble Block 2х1х1"
                },
                {
                    "piece_whitemarble_2x2x2",
                    "Marble Block"
                },
                {
                    "piece_whitemarble_2x2x2_description",
                    "Marble Block 2х2х2"
                },
                {
                    "piece_whitemarble_floor",
                    "Marble Floor"
                },
                {
                    "piece_whitemarble_floor_description",
                    "Just a Marble Floor"
                },
                {
                    "piece_whitemarble_floor_triangle",
                    "Marble floor triangle"
                },
                {
                    "piece_whitemarble_floor_triangle_description",
                    "Just a marble floor triangle"
                },
                {
                    "piece_whitemarble_stair",
                    "Marble Stair"
                },
                {
                    "piece_whitemarble_stair_description",
                    "Just a marble stair"
                },
                {
                    "piece_whitemarble_tip",
                    "Marble Tip"
                },
                {
                    "piece_whitemarble_tip_description",
                    "Just a Marble Tip"
                },
                {
                    "piece_whitemarble_base_1",
                    "Marble Base"
                },
                {
                    "piece_whitemarble_base_1_description",
                    "Marble base"
                },
                {
                    "piece_whitemarble_base_2",
                    "Marble Base 2"
                },
                {
                    "piece_whitemarble_base_2_description",
                    "Marble base"
                },
                {
                    "piece_whitemarble_basecorner",
                    "Marble Basecorner"
                },
                {
                    "piece_whitemarble_basecorner_description",
                    "Just a marble basecorner"
                },
                {
                    "piece_whitemarble_out_1",
                    "Marble Out"
                },
                {
                    "piece_whitemarble_out_1_description",
                    "Just a marble out"
                },
                {
                    "piece_whitemarble_out_2",
                    "Marble Out 2"
                },
                {
                    "piece_whitemarble_out_2_description",
                    "Just a bigger stone stair"
                },
                {
                    "piece_whitemarble_outcorner",
                    "Black marble floor big"
                },
                {
                    "piece_whitemarble_outcorner_description",
                    "Just a marble out"
                },
                {
                    "piece_whitemarble_arch",
                    "Marble Arch"
                },
                {
                    "piece_whitemarble_arch_description",
                    "Just a marble arch"
                },
                {
                    "piece_whitemarble_column_2",
                    "Marble Column 2"
                },
                {
                    "piece_whitemarble_column_2_description",
                    "Just a marble column"
                },
                {
                    "piece_whitemarble_2x2_enforced",
                    "Marble enforced 2x2"
                },
                {
                    "piece_whitemarble_2x2_enforced_description",
                    "Enforced marble"
                },
                {
                    "piece_whitemarble_column_1",
                    "Marble Column"
                },
                {
                    "piece_whitemarble_column_1_description",
                    "Just a marble column"
                },
                {
                    "piece_whitemarble_floor_large",
                    "Marble Floor 2"
                },
                {
                    "piece_whitemarble_floor_large_description",
                    "Marble floor large"
                },
                {
                    "piece_whitemarble_head_big01",
                    "Marble Carved"
                },
                {
                    "piece_whitemarble_head_big01_description",
                    "Carved marble head"
                },
                {
                    "piece_whitemarble_head_big02",
                    "Marble Carved 2"
                },
                {
                    "piece_whitemarble_head_big02_description",
                    "Carved marble head 2"
                },
                {
                    "piece_whitemarble_slope_1x2",
                    "Marble Slope"
                },
                {
                    "piece_whitemarble_slope_1x2_description",
                    "Marble Slope 1x2"
                },
                {
                    "piece_whitemarble_slope_inverted_1x2",
                    "Marble Slope"
                },
                {
                    "piece_whitemarble_slope_inverted_1x2_description",
                    "Just a inverted marble slope 1x2"
                },
                {
                    "piece_whitemarble_tile_floor_1x1",
                    "Marble Tile Floor"
                },
                {
                    "piece_whitemarble_tile_floor_1x1_description",
                    "Just a marble title floor 1x1"
                },
                {
                    "piece_whitemarble_tile_floor_2x2",
                    "Marble Tile Floor 2"
                },
                {
                    "piece_whitemarble_tile_floor_2x2_description",
                    "Just a marble title floor 2x2"
                },
                {
                    "piece_whitemarble_tile_wall_1x1",
                    "Marble Tile Wall"
                },
                {
                    "piece_whitemarble_tile_wall_1x1_description",
                    "Just a marble tile wall 1x1"
                },
                {
                    "piece_whitemarble_tile_wall_2x2",
                    "Marble Tile Wall 2"
                },
                {
                    "piece_whitemarble_tile_wall_2x2_description",
                    "Just a marble tile wall 2x2"
                },
                {
                    "piece_whitemarble_tile_wall_2x4",
                    "Marble Tile Wall 3"
                },
                {
                    "piece_whitemarble_tile_wall_2x4_description",
                    "Just a marble tile wall 2x4"
                }
            });
            CustomLocalization localization2 = this.Localization;
            text = "Russian";
            localization2.AddTranslation(text, new Dictionary<string, string>
            {
                {
                    "piece_whitemarble_1x1",
                    "Блок мрамора"
                },
                {
                    "piece_whitemarble_1x1_description",
                    "Блок мрамора 1х1"
                },
                {
                    "piece_whitemarble_2x1x1",
                    "Блок мрамора"
                },
                {
                    "piece_whitemarble_2x1x1_description",
                    "Блок мрамора 2х1х1"
                },
                {
                    "piece_whitemarble_2x2x2",
                    "Блок мрамора "
                },
                {
                    "piece_whitemarble_2x2x2_description",
                    "Блок мрамора 2х2х2"
                },
                {
                    "piece_whitemarble_floor",
                    "Пол из мрамора"
                },
                {
                    "piece_whitemarble_floor_description",
                    "Мраморный пол"
                },
                {
                    "piece_whitemarble_floor_triangle",
                    "Мраморный пол треугольный"
                },
                {
                    "piece_whitemarble_floor_triangle_description",
                    "Треугольный мраморный пол"
                },
                {
                    "piece_whitemarble_stair",
                    "Мраморные ступеньки"
                },
                {
                    "piece_whitemarble_stair_description",
                    "Ступеньки сделанные из мрамора"
                },
                {
                    "piece_whitemarble_tip",
                    "Угол из мрамора"
                },
                {
                    "piece_whitemarble_tip_description",
                    "Угловой блок из мрамора"
                },
                {
                    "piece_whitemarble_base_1",
                    "Мраморная опора"
                },
                {
                    "piece_whitemarble_base_1_description",
                    "Опора из мрамора"
                },
                {
                    "piece_whitemarble_base_2",
                    "Мраморная опора 2"
                },
                {
                    "piece_whitemarble_base_2_description",
                    "Опора из мрамора"
                },
                {
                    "piece_whitemarble_basecorner",
                    "Угловая мраморная опора"
                },
                {
                    "piece_whitemarble_basecorner_description",
                    "Угловая опора из мрамора"
                },
                {
                    "piece_whitemarble_out_1",
                    "Stone floor biggest"
                },
                {
                    "piece_whitemarble_out_1_description",
                    "Just a bigger stone floor"
                },
                {
                    "piece_whitemarble_out_2",
                    "Stone stair biggest"
                },
                {
                    "piece_whitemarble_out_2_description",
                    "Just a bigger stone stair"
                },
                {
                    "piece_whitemarble_outcorner",
                    "Black marble floor big"
                },
                {
                    "piece_whitemarble_outcorner_description",
                    "Just a bigger black marble foor"
                },
                {
                    "piece_whitemarble_arch",
                    "Black marble stair big"
                },
                {
                    "piece_whitemarble_arch_description",
                    "Just a bigger black marble stair"
                },
                {
                    "piece_whitemarble_column_2",
                    "Колонна из мрамора 2"
                },
                {
                    "piece_whitemarble_column_2_description",
                    "Колонна сделанная из мрамора"
                },
                {
                    "piece_whitemarble_2x2_enforced",
                    "Усиленный блок мрамора"
                },
                {
                    "piece_whitemarble_2x2_enforced_description",
                    "Усиленный блок мрамора 2х2"
                },
                {
                    "piece_whitemarble_column_1",
                    "Колонна из мрамора"
                },
                {
                    "piece_whitemarble_column_1_description",
                    "Колонна сделанная из мрамора"
                },
                {
                    "piece_whitemarble_floor_large",
                    "Пол из мрамора"
                },
                {
                    "piece_whitemarble_floor_large_description",
                    "Большой пол сделанный из мрамора"
                },
                {
                    "piece_whitemarble_head_big01",
                    "Узорчатый блок мрамора"
                },
                {
                    "piece_whitemarble_head_big01_description",
                    "Блок мрамора с вырезом"
                },
                {
                    "piece_whitemarble_head_big02",
                    "CУзорчатый блок мрамора 2"
                },
                {
                    "piece_whitemarble_head_big02_description",
                    "Блок мрамора с вырезом 2"
                },
                {
                    "piece_whitemarble_slope_1x2",
                    "Угловой блок мрамора"
                },
                {
                    "piece_whitemarble_slope_1x2_description",
                    "Скошенный мраморный блок 1х2"
                },
                {
                    "piece_whitemarble_slope_inverted_1x2",
                    "Скошенный мраморный блок 2"
                },
                {
                    "piece_whitemarble_slope_inverted_1x2_description",
                    "Скошенный мраморный блок 1х2"
                },
                {
                    "piece_whitemarble_tile_floor_1x1",
                    "Мраморная плитка 1х1"
                },
                {
                    "piece_whitemarble_tile_floor_1x1_description",
                    "Мраморный блок малой толщины"
                },
                {
                    "piece_whitemarble_tile_floor_2x2",
                    "Мраморная плитка 2х2"
                },
                {
                    "piece_whitemarble_tile_floor_2x2_description",
                    "Мраморный блок малой толщины"
                },
                {
                    "piece_whitemarble_tile_wall_1x1",
                    "Мраморная стена 1х1"
                },
                {
                    "piece_whitemarble_tile_wall_1x1_description",
                    "Мраморная стена малой толщины"
                },
                {
                    "piece_whitemarble_tile_wall_2x2",
                    "Мраморная стена 2х2"
                },
                {
                    "piece_whitemarble_tile_wall_2x2_description",
                    "Мраморная стена малой толщины"
                },
                {
                    "piece_whitemarble_tile_wall_2x4",
                    "Мраморная стена 2х4"
                },
                {
                    "piece_whitemarble_tile_wall_2x4_description",
                    "Мраморная стена малой толщины"
                }
            });
        }
        private static Texture2D whiteMarble;
        private CustomLocalization Localization;
    }
}
