using Jotunn.Entities;
using Jotunn.Managers;
using System;
using Jotunn;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ServerMods
{
    internal class Items
    {
        public static void CoinsEdit()
        {
            CustomItem customItem = new CustomItem(PrefabManager.Instance.GetPrefab("Coins"), false);
            customItem.ItemDrop.m_itemData.m_shared.m_weight = 0f;
            customItem.ItemDrop.m_itemData.m_shared.m_maxStackSize = 10000;
            PrefabManager.OnPrefabsRegistered -= Items.CoinsEdit;
        }

        // Token: 0x06000019 RID: 25 RVA: 0x0000257C File Offset: 0x0000077C
        public static void CultivatorEdit()
        {
            PieceTable component = PrefabManager.Instance.GetPrefab("_CultivatorPieceTable").GetComponent<PieceTable>();
            List<GameObject> list = new List<GameObject>();
            Jotunn.Logger.LogInfo(component.m_pieces.Count<GameObject>());
            component.m_pieces[0] = component.m_pieces[1];
            list = component.m_pieces;
            Jotunn.Logger.LogInfo(list.Count<GameObject>());
            list[0] = list[1];
            component.m_pieces = list;
            PrefabManager.OnPrefabsRegistered -= Items.CultivatorEdit;
        }

        // Token: 0x0600001A RID: 26 RVA: 0x00002610 File Offset: 0x00000810
        public static void StomEnemyAdd()
        {
            GameObject[] array = new GameObject[]
            {
                PrefabManager.Instance.GetPrefab("Beech_Stub"),
                PrefabManager.Instance.GetPrefab("OakStub")
            };
            GameObject prefab = PrefabManager.Instance.GetPrefab("Greydwarf");
            foreach (GameObject gameObject in array)
            {
                gameObject.AddComponent<SpawnArea>();
                SpawnArea component = gameObject.GetComponent<SpawnArea>();
                component.m_prefabs.Add(new SpawnArea.SpawnData
                {
                    m_prefab = prefab,
                    m_maxLevel = 5,
                    m_minLevel = 3,
                    m_weight = 1f
                });
                component.m_farRadius = 1000f;
                component.m_levelupChance = 100f;
                component.m_spawnIntervalSec = 300f;
                component.m_triggerDistance = 20f;
                component.m_spawnRadius = 2.28f;
                component.m_nearRadius = 20f;
                component.m_maxTotal = 7;
                PrefabManager.OnPrefabsRegistered -= Items.StomEnemyAdd;
            }
        }
    }
}
