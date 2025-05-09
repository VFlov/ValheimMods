using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000016 RID: 22
public class CreatureSpawner : MonoBehaviour
{
	// Token: 0x06000250 RID: 592 RVA: 0x00014D7C File Offset: 0x00012F7C
	private void Awake()
	{
		CreatureSpawner.m_creatureSpawners.Add(this);
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		base.InvokeRepeating("UpdateSpawner", (float)UnityEngine.Random.Range(this.m_spawnInterval / 2, this.m_spawnInterval), (float)this.m_spawnInterval);
	}

	// Token: 0x06000251 RID: 593 RVA: 0x00014DD4 File Offset: 0x00012FD4
	private void OnDestroy()
	{
		CreatureSpawner.m_creatureSpawners.Remove(this);
	}

	// Token: 0x06000252 RID: 594 RVA: 0x00014DE4 File Offset: 0x00012FE4
	private void UpdateSpawner()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_checkedLocation)
		{
			this.m_location = Location.GetLocation(base.transform.position, true);
			this.m_checkedLocation = true;
			if (this.m_location && this.m_location.m_blockSpawnGroups.Contains(this.m_spawnGroupID))
			{
				this.m_nview.Destroy();
				return;
			}
		}
		ZDOConnection connection = this.m_nview.GetZDO().GetConnection();
		bool flag = connection != null && connection.m_type == ZDOExtraData.ConnectionType.Spawned;
		if (this.m_respawnTimeMinuts <= 0f && flag)
		{
			return;
		}
		ZDOID connectionZDOID = this.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned);
		if (this.SpawnedCreatureStillExists(connectionZDOID))
		{
			return;
		}
		if (!this.CanRespawnNow(connectionZDOID))
		{
			return;
		}
		if (!this.m_spawnAtDay && EnvMan.IsDay())
		{
			return;
		}
		if (!this.m_spawnAtNight && EnvMan.IsNight())
		{
			return;
		}
		bool requireSpawnArea = this.m_requireSpawnArea;
		if (!this.m_spawnInPlayerBase && EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.PlayerBase, 0f))
		{
			return;
		}
		if (this.m_triggerNoise > 0f)
		{
			if (!Player.IsPlayerInRange(base.transform.position, this.m_triggerDistance, this.m_triggerNoise))
			{
				return;
			}
		}
		else if (!Player.IsPlayerInRange(base.transform.position, this.m_triggerDistance))
		{
			return;
		}
		if (this.CheckGlobalKeys())
		{
			return;
		}
		if (this.CheckGroupSpawnBlocked())
		{
			return;
		}
		if (this.m_spawnGroup != null)
		{
			this.m_spawnGroup.SpawnWeighted();
			return;
		}
		this.Spawn();
	}

	// Token: 0x06000253 RID: 595 RVA: 0x00014F74 File Offset: 0x00013174
	private bool CheckGlobalKeys()
	{
		List<string> globalKeys = ZoneSystem.instance.GetGlobalKeys();
		return (this.m_blockingGlobalKey != "" && globalKeys.Contains(this.m_blockingGlobalKey)) || (this.m_requiredGlobalKey != "" && !globalKeys.Contains(this.m_requiredGlobalKey));
	}

	// Token: 0x06000254 RID: 596 RVA: 0x00014FD4 File Offset: 0x000131D4
	private bool SpawnedCreatureStillExists(ZDOID spawnID)
	{
		if (!spawnID.IsNone() && ZDOMan.instance.GetZDO(spawnID) != null)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_aliveTime, ZNet.instance.GetTime().Ticks);
			return true;
		}
		return false;
	}

	// Token: 0x06000255 RID: 597 RVA: 0x00015024 File Offset: 0x00013224
	private bool CanRespawnNow(ZDOID spawnID)
	{
		if (this.m_respawnTimeMinuts > 0f)
		{
			DateTime time = ZNet.instance.GetTime();
			DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_aliveTime, 0L));
			if ((time - d).TotalMinutes < (double)this.m_respawnTimeMinuts)
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06000256 RID: 598 RVA: 0x00015080 File Offset: 0x00013280
	private bool HasSpawned()
	{
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return false;
		}
		ZDOConnection connection = this.m_nview.GetZDO().GetConnection();
		return connection != null && connection.m_type == ZDOExtraData.ConnectionType.Spawned;
	}

	// Token: 0x06000257 RID: 599 RVA: 0x000150CC File Offset: 0x000132CC
	private ZNetView Spawn()
	{
		Vector3 position = base.transform.position;
		float y;
		if (ZoneSystem.instance.FindFloor(position, out y))
		{
			position.y = y;
		}
		Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_creaturePrefab, position, rotation);
		ZNetView component = gameObject.GetComponent<ZNetView>();
		if (this.m_wakeUpAnimation)
		{
			ZSyncAnimation component2 = gameObject.GetComponent<ZSyncAnimation>();
			if (component2 != null)
			{
				component2.SetBool("wakeup", true);
			}
		}
		BaseAI component3 = gameObject.GetComponent<BaseAI>();
		if (component3 != null && this.m_setPatrolSpawnPoint)
		{
			component3.SetPatrolPoint();
		}
		int num = this.m_minLevel;
		int num2 = this.m_maxLevel;
		float num3 = this.m_levelupChance;
		if (this.m_location != null && !this.m_location.m_excludeEnemyLevelOverrideGroups.Contains(this.m_spawnGroupID))
		{
			if (this.m_location.m_enemyMinLevelOverride >= 0)
			{
				num = this.m_location.m_enemyMinLevelOverride;
			}
			if (this.m_location.m_enemyMaxLevelOverride >= 0)
			{
				num2 = this.m_location.m_enemyMaxLevelOverride;
			}
			if (this.m_location.m_enemyLevelUpOverride >= 0f)
			{
				num3 = this.m_location.m_enemyLevelUpOverride;
			}
		}
		if (num2 > 1)
		{
			Character component4 = gameObject.GetComponent<Character>();
			if (component4)
			{
				int num4 = num;
				while (num4 < num2 && UnityEngine.Random.Range(0f, 100f) <= SpawnSystem.GetLevelUpChance(num3))
				{
					num4++;
				}
				if (num4 > 1)
				{
					component4.SetLevel(num4);
				}
			}
			else
			{
				ItemDrop component5 = gameObject.GetComponent<ItemDrop>();
				if (component5)
				{
					int num5 = num;
					while (num5 < num2 && UnityEngine.Random.Range(0f, 100f) <= num3)
					{
						num5++;
					}
					if (num5 > 1)
					{
						component5.SetQuality(num5);
					}
				}
			}
		}
		this.m_nview.GetZDO().SetConnection(ZDOExtraData.ConnectionType.Spawned, component.GetZDO().m_uid);
		this.m_nview.GetZDO().Set(ZDOVars.s_aliveTime, ZNet.instance.GetTime().Ticks);
		this.SpawnEffect(gameObject);
		return component;
	}

	// Token: 0x06000258 RID: 600 RVA: 0x000152EC File Offset: 0x000134EC
	private void SpawnEffect(GameObject spawnedObject)
	{
		Character component = spawnedObject.GetComponent<Character>();
		Vector3 basePos = component ? component.GetCenterPoint() : (base.transform.position + Vector3.up * 0.75f);
		this.m_spawnEffects.Create(basePos, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x06000259 RID: 601 RVA: 0x00015349 File Offset: 0x00013549
	private float GetRadius()
	{
		return 0.75f;
	}

	// Token: 0x0600025A RID: 602 RVA: 0x00015350 File Offset: 0x00013550
	private void OnDrawGizmos()
	{
	}

	// Token: 0x0600025B RID: 603 RVA: 0x00015354 File Offset: 0x00013554
	private bool CheckGroupSpawnBlocked()
	{
		if (this.m_spawnGroupRadius <= 0f || this.m_maxGroupSpawned < 1)
		{
			return false;
		}
		if (this.m_spawnGroup == null)
		{
			CreatureSpawner.m_groupNew.Clear();
			CreatureSpawner.m_grouped.Clear();
			CreatureSpawner.m_groupUnchecked.Clear();
			CreatureSpawner.m_groupUnchecked.AddRange(CreatureSpawner.m_creatureSpawners);
			CreatureSpawner.m_groupUnchecked.Remove(this);
			CreatureSpawner.m_groupNew.Push(this);
			CreatureSpawner.m_grouped.Add(this);
			while (CreatureSpawner.m_groupNew.Count > 0)
			{
				CreatureSpawner creatureSpawner = CreatureSpawner.m_groupNew.Pop();
				for (int i = CreatureSpawner.m_groupUnchecked.Count - 1; i >= 0; i--)
				{
					CreatureSpawner creatureSpawner2 = CreatureSpawner.m_groupUnchecked[i];
					if (creatureSpawner.m_spawnGroupID == creatureSpawner2.m_spawnGroupID && Vector3.Distance(creatureSpawner.transform.position, creatureSpawner2.transform.position) <= creatureSpawner.m_spawnGroupRadius + creatureSpawner2.m_spawnGroupRadius)
					{
						CreatureSpawner.m_groupNew.Push(creatureSpawner2);
						CreatureSpawner.m_grouped.Add(creatureSpawner2);
						CreatureSpawner.m_groupUnchecked.Remove(creatureSpawner2);
					}
				}
			}
			CreatureSpawner.m_groups.Clear();
			foreach (CreatureSpawner creatureSpawner3 in CreatureSpawner.m_grouped)
			{
				if (creatureSpawner3.m_spawnGroup != null)
				{
					CreatureSpawner.m_groups.Add(creatureSpawner3.m_spawnGroup);
				}
			}
			CreatureSpawner.Group group = null;
			if (CreatureSpawner.m_groups.Count > 0)
			{
				if (CreatureSpawner.m_groups.Count > 1)
				{
					ZLog.Log(string.Format("{0} {1} merged for {2} spawners.", CreatureSpawner.m_groups.Count, "Group", CreatureSpawner.m_grouped.Count));
				}
				group = CreatureSpawner.m_groups[0];
			}
			else
			{
				group = new CreatureSpawner.Group();
			}
			foreach (CreatureSpawner creatureSpawner4 in CreatureSpawner.m_grouped)
			{
				group.Add(creatureSpawner4);
				creatureSpawner4.m_spawnGroup = group;
			}
			ZLog.Log(string.Format("{0} created for {1} spawners.", "Group", group.Count));
		}
		int num;
		int num2;
		this.m_spawnGroup.CountSpawns(out num, out num2);
		if (this.m_respawnTimeMinuts <= 0f)
		{
			if (num2 < this.m_maxGroupSpawned && num < this.m_maxGroupSpawned)
			{
				return false;
			}
			Terminal.Log(string.Format("Group spawnID #{0} blocked: I have not spawned, but someone else has made us reach the maximum, abort!", this.m_spawnGroupID));
			return true;
		}
		else
		{
			if (num < this.m_maxGroupSpawned)
			{
				return false;
			}
			Terminal.Log(string.Format("Group spawnID #{0} blocked: I allow respawning, but we are currently at our maxium, abort!", this.m_spawnGroupID));
			return true;
		}
	}

	// Token: 0x04000337 RID: 823
	private const float m_radius = 0.75f;

	// Token: 0x04000338 RID: 824
	public GameObject m_creaturePrefab;

	// Token: 0x04000339 RID: 825
	[Header("Level")]
	public int m_maxLevel = 1;

	// Token: 0x0400033A RID: 826
	public int m_minLevel = 1;

	// Token: 0x0400033B RID: 827
	public float m_levelupChance = 10f;

	// Token: 0x0400033C RID: 828
	[Header("Spawn settings")]
	public float m_respawnTimeMinuts = 20f;

	// Token: 0x0400033D RID: 829
	public float m_triggerDistance = 60f;

	// Token: 0x0400033E RID: 830
	public float m_triggerNoise;

	// Token: 0x0400033F RID: 831
	public bool m_spawnAtNight = true;

	// Token: 0x04000340 RID: 832
	public bool m_spawnAtDay = true;

	// Token: 0x04000341 RID: 833
	public bool m_requireSpawnArea;

	// Token: 0x04000342 RID: 834
	public bool m_spawnInPlayerBase;

	// Token: 0x04000343 RID: 835
	public bool m_wakeUpAnimation;

	// Token: 0x04000344 RID: 836
	public int m_spawnInterval = 5;

	// Token: 0x04000345 RID: 837
	public string m_requiredGlobalKey = "";

	// Token: 0x04000346 RID: 838
	public string m_blockingGlobalKey = "";

	// Token: 0x04000347 RID: 839
	public bool m_setPatrolSpawnPoint;

	// Token: 0x04000348 RID: 840
	[Header("Spawn group blocking")]
	[global::Tooltip("Spawners sharing the same ID within eachothers radiuses will be grouped together, and will never spawn more than the specified max group size. Weight will also be taken into account, prioritizing those with higher weight randomly.")]
	public int m_spawnGroupID;

	// Token: 0x04000349 RID: 841
	public int m_maxGroupSpawned = 1;

	// Token: 0x0400034A RID: 842
	public float m_spawnGroupRadius;

	// Token: 0x0400034B RID: 843
	public float m_spawnerWeight = 1f;

	// Token: 0x0400034C RID: 844
	[Space]
	public EffectList m_spawnEffects = new EffectList();

	// Token: 0x0400034D RID: 845
	private ZNetView m_nview;

	// Token: 0x0400034E RID: 846
	private CreatureSpawner.Group m_spawnGroup;

	// Token: 0x0400034F RID: 847
	private ZDOID m_lastSpawnID;

	// Token: 0x04000350 RID: 848
	private bool m_checkedLocation;

	// Token: 0x04000351 RID: 849
	private Location m_location;

	// Token: 0x04000352 RID: 850
	private static List<CreatureSpawner> m_creatureSpawners = new List<CreatureSpawner>();

	// Token: 0x04000353 RID: 851
	private static List<CreatureSpawner> m_groupUnchecked = new List<CreatureSpawner>();

	// Token: 0x04000354 RID: 852
	private static HashSet<CreatureSpawner> m_grouped = new HashSet<CreatureSpawner>();

	// Token: 0x04000355 RID: 853
	private static Stack<CreatureSpawner> m_groupNew = new Stack<CreatureSpawner>();

	// Token: 0x04000356 RID: 854
	private static List<CreatureSpawner.Group> m_groups = new List<CreatureSpawner.Group>();

	// Token: 0x0200022F RID: 559
	public class Group : HashSet<CreatureSpawner>
	{
		// Token: 0x06001ECF RID: 7887 RVA: 0x000E1618 File Offset: 0x000DF818
		public void CountSpawns(out int spawnedNow, out int spawnedEver)
		{
			spawnedNow = (spawnedEver = 0);
			foreach (CreatureSpawner creatureSpawner in this)
			{
				if (!(creatureSpawner.m_nview == null) && creatureSpawner.m_nview.GetZDO() != null)
				{
					creatureSpawner.m_lastSpawnID = creatureSpawner.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned);
					if (creatureSpawner.m_nview.GetZDO().GetConnectionType() != ZDOExtraData.ConnectionType.None)
					{
						spawnedEver++;
						creatureSpawner.m_lastSpawnID = creatureSpawner.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned);
						if (ZDOMan.instance.GetZDO(creatureSpawner.m_lastSpawnID) != null)
						{
							spawnedNow++;
						}
					}
				}
			}
		}

		// Token: 0x06001ED0 RID: 7888 RVA: 0x000E16E8 File Offset: 0x000DF8E8
		public void SpawnWeighted()
		{
			this.m_activeSpawners.Clear();
			float num = 0f;
			foreach (CreatureSpawner creatureSpawner in this)
			{
				if (creatureSpawner.m_lastSpawnID.IsNone() || (creatureSpawner.CanRespawnNow(creatureSpawner.m_lastSpawnID) && !creatureSpawner.SpawnedCreatureStillExists(creatureSpawner.m_lastSpawnID)))
				{
					this.m_activeSpawners.Add(creatureSpawner);
					num += creatureSpawner.m_spawnerWeight;
				}
			}
			float num2 = UnityEngine.Random.Range(0f, num);
			float num3 = 0f;
			foreach (CreatureSpawner creatureSpawner2 in this.m_activeSpawners)
			{
				num3 += creatureSpawner2.m_spawnerWeight;
				if (num2 < num3)
				{
					creatureSpawner2.Spawn();
					return;
				}
			}
			ZLog.LogError("No active spawners for group but something is still calling it!");
		}

		// Token: 0x04001F69 RID: 8041
		private readonly List<CreatureSpawner> m_activeSpawners = new List<CreatureSpawner>();
	}
}
