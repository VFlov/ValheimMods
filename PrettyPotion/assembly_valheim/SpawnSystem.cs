using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000149 RID: 329
public class SpawnSystem : MonoBehaviour
{
	// Token: 0x06001414 RID: 5140 RVA: 0x000936C8 File Offset: 0x000918C8
	private void Awake()
	{
		SpawnSystem.m_instances.Add(this);
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_heightmap = Heightmap.FindHeightmap(base.transform.position);
		base.InvokeRepeating("UpdateSpawning", 10f, 1f);
	}

	// Token: 0x06001415 RID: 5141 RVA: 0x00093717 File Offset: 0x00091917
	private void OnDestroy()
	{
		SpawnSystem.m_instances.Remove(this);
	}

	// Token: 0x06001416 RID: 5142 RVA: 0x00093728 File Offset: 0x00091928
	private void UpdateSpawning()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (Player.m_localPlayer == null)
		{
			return;
		}
		SpawnSystem.m_tempNearPlayers.Clear();
		this.GetPlayersInZone(SpawnSystem.m_tempNearPlayers);
		if (SpawnSystem.m_tempNearPlayers.Count == 0)
		{
			return;
		}
		DateTime time = ZNet.instance.GetTime();
		foreach (SpawnSystemList spawnSystemList in this.m_spawnLists)
		{
			this.UpdateSpawnList(spawnSystemList.m_spawners, time, false);
		}
		List<SpawnSystem.SpawnData> currentSpawners = RandEventSystem.instance.GetCurrentSpawners();
		if (currentSpawners != null)
		{
			this.UpdateSpawnList(currentSpawners, time, true);
		}
	}

	// Token: 0x06001417 RID: 5143 RVA: 0x000937F0 File Offset: 0x000919F0
	private void UpdateSpawnList(List<SpawnSystem.SpawnData> spawners, DateTime currentTime, bool eventSpawners)
	{
		string str = eventSpawners ? "e_" : "b_";
		this.m_pheromoneList.Clear();
		foreach (Player player in Player.GetAllPlayers())
		{
			foreach (StatusEffect statusEffect in player.GetSEMan().GetStatusEffects())
			{
				SE_Stats se_Stats = statusEffect as SE_Stats;
				if (se_Stats != null && se_Stats.m_pheromoneTarget != null)
				{
					this.m_pheromoneList.Add(se_Stats);
				}
			}
		}
		int num = 0;
		foreach (SpawnSystem.SpawnData spawnData in spawners)
		{
			num++;
			if (spawnData.m_enabled && this.m_heightmap.HaveBiome(spawnData.m_biome))
			{
				int stableHashCode = (str + spawnData.m_prefab.name + num.ToString()).GetStableHashCode();
				DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(stableHashCode, 0L));
				TimeSpan timeSpan = currentTime - d;
				int num2 = Mathf.Min((spawnData.m_maxSpawned == 0) ? 1 : spawnData.m_maxSpawned, (int)(timeSpan.TotalSeconds / (double)spawnData.m_spawnInterval));
				if (num2 > 0)
				{
					this.m_nview.GetZDO().Set(stableHashCode, currentTime.Ticks);
				}
				for (int i = 0; i < num2; i++)
				{
					Vector3 vector;
					Player player2;
					if (this.FindBaseSpawnPoint(spawnData, SpawnSystem.m_tempNearPlayers, out vector, out player2))
					{
						int num3 = spawnData.m_maxSpawned;
						float num4 = spawnData.m_spawnChance;
						int minLevelOverride = -1;
						float num5 = 1f;
						foreach (SE_Stats se_Stats2 in this.m_pheromoneList)
						{
							if (se_Stats2.m_pheromoneTarget == spawnData.m_prefab && se_Stats2.m_character != null && Vector3.Distance(vector, se_Stats2.m_character.transform.position) < 100f)
							{
								if (se_Stats2.m_pheromoneSpawnChanceOverride > 0f)
								{
									num4 = se_Stats2.m_pheromoneSpawnChanceOverride;
								}
								if (se_Stats2.m_pheromoneMaxInstanceOverride > 0)
								{
									num3 = se_Stats2.m_pheromoneMaxInstanceOverride;
								}
								if (se_Stats2.m_pheromoneSpawnMinLevel > 0)
								{
									minLevelOverride = se_Stats2.m_pheromoneSpawnMinLevel;
								}
								if (se_Stats2.m_pheromoneLevelUpMultiplier != 1f)
								{
									num5 *= se_Stats2.m_pheromoneLevelUpMultiplier;
								}
							}
						}
						if (UnityEngine.Random.Range(0f, 100f) <= num4)
						{
							if ((!string.IsNullOrEmpty(spawnData.m_requiredGlobalKey) && !ZoneSystem.instance.GetGlobalKey(spawnData.m_requiredGlobalKey)) || (spawnData.m_requiredEnvironments.Count > 0 && !EnvMan.instance.IsEnvironment(spawnData.m_requiredEnvironments)) || (!spawnData.m_spawnAtDay && EnvMan.IsDay()) || (!spawnData.m_spawnAtNight && EnvMan.IsNight()))
							{
								break;
							}
							int num6 = 0;
							if (num3 > 0)
							{
								num6 = SpawnSystem.GetNrOfInstances(spawnData.m_prefab, Vector3.zero, 0f, eventSpawners, false);
								if (num6 >= num3)
								{
									break;
								}
							}
							if (spawnData.m_spawnDistance <= 0f || !SpawnSystem.HaveInstanceInRange(spawnData.m_prefab, vector, spawnData.m_spawnDistance))
							{
								int num7 = Mathf.Min(UnityEngine.Random.Range(spawnData.m_groupSizeMin, spawnData.m_groupSizeMax + 1), (spawnData.m_maxSpawned > 0) ? (spawnData.m_maxSpawned - num6) : 100);
								float d2 = (num7 > 1) ? spawnData.m_groupRadius : 0f;
								int num8 = 0;
								for (int j = 0; j < num7 * 2; j++)
								{
									Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
									Vector3 a = vector + new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y) * d2;
									if (this.IsSpawnPointGood(spawnData, ref a))
									{
										this.Spawn(spawnData, a + Vector3.up * (spawnData.m_groundOffset + UnityEngine.Random.Range(0f, spawnData.m_groundOffsetRandom)), eventSpawners, minLevelOverride, num5);
										num8++;
										if (num8 >= num7)
										{
											break;
										}
									}
								}
								ZLog.Log("Spawned " + spawnData.m_prefab.name + " x " + num8.ToString());
							}
						}
					}
				}
			}
		}
	}

	// Token: 0x06001418 RID: 5144 RVA: 0x00093CEC File Offset: 0x00091EEC
	private void Spawn(SpawnSystem.SpawnData critter, Vector3 spawnPoint, bool eventSpawner, int minLevelOverride = -1, float levelUpMultiplier = 1f)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(critter.m_prefab, spawnPoint, Quaternion.identity);
		if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("spawns"))
		{
			Terminal.Log(string.Format("Spawning {0} at {1}", critter.m_prefab.name, spawnPoint));
			Chat.instance.SendPing(spawnPoint);
		}
		BaseAI component = gameObject.GetComponent<BaseAI>();
		if (component != null && critter.m_huntPlayer && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs))
		{
			component.SetHuntPlayer(true);
		}
		if (critter.m_levelUpMinCenterDistance <= 0f || spawnPoint.magnitude > critter.m_levelUpMinCenterDistance)
		{
			int num = critter.m_minLevel;
			float num2 = SpawnSystem.GetLevelUpChance(critter);
			if (minLevelOverride >= 0)
			{
				num = minLevelOverride;
			}
			if (levelUpMultiplier != 1f)
			{
				num2 *= levelUpMultiplier;
			}
			while (num < critter.m_maxLevel && UnityEngine.Random.Range(0f, 100f) <= num2)
			{
				num++;
			}
			if (num > 1)
			{
				Character component2 = gameObject.GetComponent<Character>();
				if (component2 != null)
				{
					component2.SetLevel(num);
				}
				if (gameObject.GetComponent<Fish>() != null)
				{
					ItemDrop component3 = gameObject.GetComponent<ItemDrop>();
					if (component3 != null)
					{
						component3.SetQuality(num);
					}
				}
			}
		}
		MonsterAI monsterAI = component as MonsterAI;
		if (monsterAI != null)
		{
			if (!critter.m_spawnAtDay)
			{
				monsterAI.SetDespawnInDay(true);
			}
			if (eventSpawner)
			{
				monsterAI.SetEventCreature(true);
			}
		}
	}

	// Token: 0x06001419 RID: 5145 RVA: 0x00093E38 File Offset: 0x00092038
	private bool IsSpawnPointGood(SpawnSystem.SpawnData spawn, ref Vector3 spawnPoint)
	{
		Vector3 vector;
		Heightmap.Biome biome;
		Heightmap.BiomeArea biomeArea;
		Heightmap heightmap;
		ZoneSystem.instance.GetGroundData(ref spawnPoint, out vector, out biome, out biomeArea, out heightmap);
		if ((spawn.m_biome & biome) == Heightmap.Biome.None)
		{
			return false;
		}
		if ((spawn.m_biomeArea & biomeArea) == (Heightmap.BiomeArea)0)
		{
			return false;
		}
		if (ZoneSystem.instance.IsBlocked(spawnPoint))
		{
			return false;
		}
		float num = spawnPoint.y - 30f;
		if (num < spawn.m_minAltitude || num > spawn.m_maxAltitude)
		{
			return false;
		}
		float num2 = Mathf.Cos(0.017453292f * spawn.m_maxTilt);
		float num3 = Mathf.Cos(0.017453292f * spawn.m_minTilt);
		if (vector.y < num2 || vector.y > num3)
		{
			return false;
		}
		if (spawn.m_minDistanceFromCenter > 0f || spawn.m_maxDistanceFromCenter > 0f)
		{
			float num4 = Utils.LengthXZ(spawnPoint);
			if (spawn.m_minDistanceFromCenter > 0f && num4 < spawn.m_minDistanceFromCenter)
			{
				return false;
			}
			if (spawn.m_maxDistanceFromCenter > 0f && num4 > spawn.m_maxDistanceFromCenter)
			{
				return false;
			}
		}
		float range = (spawn.m_spawnRadiusMin > 0f) ? spawn.m_spawnRadiusMin : 40f;
		if (!spawn.m_canSpawnCloseToPlayer && Player.IsPlayerInRange(spawnPoint, range))
		{
			return false;
		}
		if (!spawn.m_insidePlayerBase && EffectArea.IsPointInsideArea(spawnPoint, EffectArea.Type.PlayerBase, 0f))
		{
			return false;
		}
		if (!spawn.m_inForest || !spawn.m_outsideForest)
		{
			bool flag = WorldGenerator.InForest(spawnPoint);
			if (!spawn.m_inForest && flag)
			{
				return false;
			}
			if (!spawn.m_outsideForest && !flag)
			{
				return false;
			}
		}
		if (!spawn.m_inLava || !spawn.m_outsideLava)
		{
			if (!spawn.m_inLava && ZoneSystem.instance.IsLava(spawnPoint, true))
			{
				return false;
			}
			if (!spawn.m_outsideLava && !ZoneSystem.instance.IsLava(spawnPoint, false))
			{
				return false;
			}
		}
		if (spawn.m_minOceanDepth != spawn.m_maxOceanDepth && heightmap != null)
		{
			float oceanDepth = heightmap.GetOceanDepth(spawnPoint);
			if (oceanDepth < spawn.m_minOceanDepth || oceanDepth > spawn.m_maxOceanDepth)
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x0600141A RID: 5146 RVA: 0x00094058 File Offset: 0x00092258
	private bool FindBaseSpawnPoint(SpawnSystem.SpawnData spawn, List<Player> allPlayers, out Vector3 spawnCenter, out Player targetPlayer)
	{
		float minInclusive = (spawn.m_spawnRadiusMin > 0f) ? spawn.m_spawnRadiusMin : 40f;
		float maxInclusive = (spawn.m_spawnRadiusMax > 0f) ? spawn.m_spawnRadiusMax : 80f;
		for (int i = 0; i < 20; i++)
		{
			Player player = allPlayers[UnityEngine.Random.Range(0, allPlayers.Count)];
			Vector3 a = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward;
			Vector3 vector = player.transform.position + a * UnityEngine.Random.Range(minInclusive, maxInclusive);
			if (this.IsSpawnPointGood(spawn, ref vector))
			{
				spawnCenter = vector;
				targetPlayer = player;
				return true;
			}
		}
		spawnCenter = Vector3.zero;
		targetPlayer = null;
		return false;
	}

	// Token: 0x0600141B RID: 5147 RVA: 0x0009412C File Offset: 0x0009232C
	private int GetNrOfInstances(string prefabName)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		int num = 0;
		foreach (Character character in allCharacters)
		{
			if (character.gameObject.name.CustomStartsWith(prefabName) && this.InsideZone(character.transform.position, 0f))
			{
				num++;
			}
		}
		return num;
	}

	// Token: 0x0600141C RID: 5148 RVA: 0x000941AC File Offset: 0x000923AC
	private void GetPlayersInZone(List<Player> players)
	{
		foreach (Player player in Player.GetAllPlayers())
		{
			if (this.InsideZone(player.transform.position, 0f))
			{
				players.Add(player);
			}
		}
	}

	// Token: 0x0600141D RID: 5149 RVA: 0x00094218 File Offset: 0x00092418
	private void GetPlayersNearZone(List<Player> players, float marginDistance)
	{
		foreach (Player player in Player.GetAllPlayers())
		{
			if (this.InsideZone(player.transform.position, marginDistance))
			{
				players.Add(player);
			}
		}
	}

	// Token: 0x0600141E RID: 5150 RVA: 0x00094280 File Offset: 0x00092480
	private bool IsPlayerTooClose(List<Player> players, Vector3 point, float minDistance)
	{
		using (List<Player>.Enumerator enumerator = players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Vector3.Distance(enumerator.Current.transform.position, point) < minDistance)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x0600141F RID: 5151 RVA: 0x000942E0 File Offset: 0x000924E0
	private bool InPlayerRange(List<Player> players, Vector3 point, float minDistance, float maxDistance)
	{
		bool result = false;
		foreach (Player player in players)
		{
			float num = Utils.DistanceXZ(player.transform.position, point);
			if (num < minDistance)
			{
				return false;
			}
			if (num < maxDistance)
			{
				result = true;
			}
		}
		return result;
	}

	// Token: 0x06001420 RID: 5152 RVA: 0x0009434C File Offset: 0x0009254C
	private static bool HaveInstanceInRange(GameObject prefab, Vector3 centerPoint, float minDistance)
	{
		string name = prefab.name;
		if (prefab.GetComponent<BaseAI>() != null)
		{
			foreach (BaseAI baseAI in BaseAI.BaseAIInstances)
			{
				if (baseAI.gameObject.name.CustomStartsWith(name) && Utils.DistanceXZ(baseAI.transform.position, centerPoint) < minDistance)
				{
					return true;
				}
			}
			return false;
		}
		foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("spawned"))
		{
			if (gameObject.gameObject.name.CustomStartsWith(name) && Utils.DistanceXZ(gameObject.transform.position, centerPoint) < minDistance)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001421 RID: 5153 RVA: 0x00094430 File Offset: 0x00092630
	public static int GetNrOfInstances(GameObject prefab)
	{
		return SpawnSystem.GetNrOfInstances(prefab, Vector3.zero, 0f, false, false);
	}

	// Token: 0x06001422 RID: 5154 RVA: 0x00094444 File Offset: 0x00092644
	public static int GetNrOfInstances(GameObject prefab, Vector3 center, float maxRange, bool eventCreaturesOnly = false, bool procreationOnly = false)
	{
		string b = prefab.name + "(Clone)";
		if (prefab.GetComponent<BaseAI>() != null)
		{
			List<BaseAI> baseAIInstances = BaseAI.BaseAIInstances;
			int num = 0;
			foreach (BaseAI baseAI in baseAIInstances)
			{
				if (!(baseAI.gameObject.name != b) && (maxRange <= 0f || Vector3.Distance(center, baseAI.transform.position) <= maxRange))
				{
					if (eventCreaturesOnly)
					{
						MonsterAI monsterAI = baseAI as MonsterAI;
						if (monsterAI && !monsterAI.IsEventCreature())
						{
							continue;
						}
					}
					if (procreationOnly)
					{
						Procreation component = baseAI.GetComponent<Procreation>();
						if (component && !component.ReadyForProcreation())
						{
							continue;
						}
					}
					num++;
				}
			}
			return num;
		}
		GameObject[] array = GameObject.FindGameObjectsWithTag("spawned");
		int num2 = 0;
		foreach (GameObject gameObject in array)
		{
			if (gameObject.name.CustomStartsWith(b) && (maxRange <= 0f || Vector3.Distance(center, gameObject.transform.position) <= maxRange))
			{
				num2++;
			}
		}
		return num2;
	}

	// Token: 0x06001423 RID: 5155 RVA: 0x00094580 File Offset: 0x00092780
	private bool InsideZone(Vector3 point, float extra = 0f)
	{
		float num = 32f + extra;
		Vector3 position = base.transform.position;
		return point.x >= position.x - num && point.x <= position.x + num && point.z >= position.z - num && point.z <= position.z + num;
	}

	// Token: 0x06001424 RID: 5156 RVA: 0x000945E6 File Offset: 0x000927E6
	private bool HaveGlobalKeys(SpawnSystem.SpawnData ev)
	{
		return string.IsNullOrEmpty(ev.m_requiredGlobalKey) || ZoneSystem.instance.GetGlobalKey(ev.m_requiredGlobalKey);
	}

	// Token: 0x06001425 RID: 5157 RVA: 0x00094607 File Offset: 0x00092807
	public static float GetLevelUpChance(SpawnSystem.SpawnData creature)
	{
		return SpawnSystem.GetLevelUpChance((creature.m_overrideLevelupChance >= 0f) ? creature.m_overrideLevelupChance : 0f);
	}

	// Token: 0x06001426 RID: 5158 RVA: 0x00094628 File Offset: 0x00092828
	public static float GetLevelUpChance(float levelUpChanceOverride = 0f)
	{
		float num = (levelUpChanceOverride > 0f) ? levelUpChanceOverride : 10f;
		if (Game.m_worldLevel > 0 && Game.instance.m_worldLevelEnemyLevelUpExponent > 0f)
		{
			return Mathf.Min(70f, Mathf.Pow(num, (float)Game.m_worldLevel * Game.instance.m_worldLevelEnemyLevelUpExponent));
		}
		return num * Game.m_enemyLevelUpRate;
	}

	// Token: 0x040013DC RID: 5084
	private static List<SpawnSystem> m_instances = new List<SpawnSystem>();

	// Token: 0x040013DD RID: 5085
	private const float m_spawnDistanceMin = 40f;

	// Token: 0x040013DE RID: 5086
	private const float m_spawnDistanceMax = 80f;

	// Token: 0x040013DF RID: 5087
	private const float m_levelupChance = 10f;

	// Token: 0x040013E0 RID: 5088
	public List<SpawnSystemList> m_spawnLists = new List<SpawnSystemList>();

	// Token: 0x040013E1 RID: 5089
	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();

	// Token: 0x040013E2 RID: 5090
	private static List<Player> m_tempNearPlayers = new List<Player>();

	// Token: 0x040013E3 RID: 5091
	private ZNetView m_nview;

	// Token: 0x040013E4 RID: 5092
	private Heightmap m_heightmap;

	// Token: 0x040013E5 RID: 5093
	private List<SE_Stats> m_pheromoneList = new List<SE_Stats>();

	// Token: 0x02000334 RID: 820
	[Serializable]
	public class SpawnData
	{
		// Token: 0x06002268 RID: 8808 RVA: 0x000EDAEC File Offset: 0x000EBCEC
		public SpawnSystem.SpawnData Clone()
		{
			SpawnSystem.SpawnData spawnData = base.MemberwiseClone() as SpawnSystem.SpawnData;
			spawnData.m_requiredEnvironments = new List<string>(this.m_requiredEnvironments);
			return spawnData;
		}

		// Token: 0x04002456 RID: 9302
		public string m_name = "";

		// Token: 0x04002457 RID: 9303
		public bool m_enabled = true;

		// Token: 0x04002458 RID: 9304
		public bool m_devDisabled;

		// Token: 0x04002459 RID: 9305
		public GameObject m_prefab;

		// Token: 0x0400245A RID: 9306
		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		// Token: 0x0400245B RID: 9307
		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		// Token: 0x0400245C RID: 9308
		[Header("Total nr of instances (if near player is set, only instances within the max spawn radius is counted)")]
		public int m_maxSpawned = 1;

		// Token: 0x0400245D RID: 9309
		[Header("How often do we spawn")]
		public float m_spawnInterval = 4f;

		// Token: 0x0400245E RID: 9310
		[Header("Chanse to spawn each spawn interval")]
		[Range(0f, 100f)]
		public float m_spawnChance = 100f;

		// Token: 0x0400245F RID: 9311
		[Header("Minimum distance to another instance")]
		public float m_spawnDistance = 10f;

		// Token: 0x04002460 RID: 9312
		[Header("Spawn range ( 0 = use global setting )")]
		public float m_spawnRadiusMin;

		// Token: 0x04002461 RID: 9313
		public float m_spawnRadiusMax;

		// Token: 0x04002462 RID: 9314
		[Header("Only spawn if this key is set")]
		public string m_requiredGlobalKey = "";

		// Token: 0x04002463 RID: 9315
		[Header("Only spawn if this environment is active")]
		public List<string> m_requiredEnvironments = new List<string>();

		// Token: 0x04002464 RID: 9316
		[Header("Group spawning")]
		public int m_groupSizeMin = 1;

		// Token: 0x04002465 RID: 9317
		public int m_groupSizeMax = 1;

		// Token: 0x04002466 RID: 9318
		public float m_groupRadius = 3f;

		// Token: 0x04002467 RID: 9319
		[Header("Time of day & Environment")]
		public bool m_spawnAtNight = true;

		// Token: 0x04002468 RID: 9320
		public bool m_spawnAtDay = true;

		// Token: 0x04002469 RID: 9321
		[Header("Altitude")]
		public float m_minAltitude = -1000f;

		// Token: 0x0400246A RID: 9322
		public float m_maxAltitude = 1000f;

		// Token: 0x0400246B RID: 9323
		[Header("Terrain tilt")]
		public float m_minTilt;

		// Token: 0x0400246C RID: 9324
		public float m_maxTilt = 35f;

		// Token: 0x0400246D RID: 9325
		[Header("Areas")]
		public bool m_inForest = true;

		// Token: 0x0400246E RID: 9326
		public bool m_outsideForest = true;

		// Token: 0x0400246F RID: 9327
		public bool m_inLava;

		// Token: 0x04002470 RID: 9328
		public bool m_outsideLava = true;

		// Token: 0x04002471 RID: 9329
		public bool m_canSpawnCloseToPlayer;

		// Token: 0x04002472 RID: 9330
		public bool m_insidePlayerBase;

		// Token: 0x04002473 RID: 9331
		[Header("Ocean depth ")]
		public float m_minOceanDepth;

		// Token: 0x04002474 RID: 9332
		public float m_maxOceanDepth;

		// Token: 0x04002475 RID: 9333
		[Header("States")]
		public bool m_huntPlayer;

		// Token: 0x04002476 RID: 9334
		public float m_groundOffset = 0.5f;

		// Token: 0x04002477 RID: 9335
		public float m_groundOffsetRandom;

		// Token: 0x04002478 RID: 9336
		[Header("Distance from center")]
		public float m_minDistanceFromCenter;

		// Token: 0x04002479 RID: 9337
		public float m_maxDistanceFromCenter;

		// Token: 0x0400247A RID: 9338
		[Header("Level")]
		public int m_maxLevel = 1;

		// Token: 0x0400247B RID: 9339
		public int m_minLevel = 1;

		// Token: 0x0400247C RID: 9340
		public float m_levelUpMinCenterDistance;

		// Token: 0x0400247D RID: 9341
		public float m_overrideLevelupChance = -1f;

		// Token: 0x0400247E RID: 9342
		[HideInInspector]
		public bool m_foldout;
	}
}
