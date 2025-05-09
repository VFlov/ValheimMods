using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000148 RID: 328
public class SpawnArea : MonoBehaviour
{
	// Token: 0x0600140A RID: 5130 RVA: 0x0009311B File Offset: 0x0009131B
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		base.InvokeRepeating("UpdateSpawn", 2f, 2f);
	}

	// Token: 0x0600140B RID: 5131 RVA: 0x00093140 File Offset: 0x00091340
	private void UpdateSpawn()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (ZNetScene.instance.OutsideActiveArea(base.transform.position))
		{
			return;
		}
		if (!Player.IsPlayerInRange(base.transform.position, this.m_triggerDistance))
		{
			return;
		}
		this.m_spawnTimer += 2f;
		if (this.m_spawnTimer > this.m_spawnIntervalSec)
		{
			this.m_spawnTimer = 0f;
			this.SpawnOne();
		}
	}

	// Token: 0x0600140C RID: 5132 RVA: 0x000931C0 File Offset: 0x000913C0
	private bool SpawnOne()
	{
		int num;
		int num2;
		this.GetInstances(out num, out num2);
		if (num >= this.m_maxNear || num2 >= this.m_maxTotal)
		{
			return false;
		}
		SpawnArea.SpawnData spawnData = this.SelectWeightedPrefab();
		if (spawnData == null)
		{
			return false;
		}
		Vector3 position;
		if (!this.FindSpawnPoint(spawnData.m_prefab, out position))
		{
			return false;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(spawnData.m_prefab, position, Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f));
		if (this.m_setPatrolSpawnPoint)
		{
			BaseAI component = gameObject.GetComponent<BaseAI>();
			if (component != null)
			{
				component.SetPatrolPoint();
			}
		}
		Character component2 = gameObject.GetComponent<Character>();
		if (spawnData.m_maxLevel > 1)
		{
			int num3 = spawnData.m_minLevel;
			while (num3 < spawnData.m_maxLevel && UnityEngine.Random.Range(0f, 100f) <= this.GetLevelUpChance())
			{
				num3++;
			}
			if (num3 > 1)
			{
				component2.SetLevel(num3);
			}
		}
		Vector3 centerPoint = component2.GetCenterPoint();
		this.m_spawnEffects.Create(centerPoint, Quaternion.identity, null, 1f, -1);
		return true;
	}

	// Token: 0x0600140D RID: 5133 RVA: 0x000932CC File Offset: 0x000914CC
	private bool FindSpawnPoint(GameObject prefab, out Vector3 point)
	{
		prefab.GetComponent<BaseAI>();
		for (int i = 0; i < 10; i++)
		{
			Vector3 vector = base.transform.position + Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * UnityEngine.Random.Range(0f, this.m_spawnRadius);
			float num;
			if (ZoneSystem.instance.FindFloor(vector, out num) && (!this.m_onGroundOnly || !ZoneSystem.instance.IsBlocked(vector)))
			{
				vector.y = num + 0.1f;
				point = vector;
				return true;
			}
		}
		point = Vector3.zero;
		return false;
	}

	// Token: 0x0600140E RID: 5134 RVA: 0x00093388 File Offset: 0x00091588
	private SpawnArea.SpawnData SelectWeightedPrefab()
	{
		if (this.m_prefabs.Count == 0)
		{
			return null;
		}
		float num = 0f;
		foreach (SpawnArea.SpawnData spawnData in this.m_prefabs)
		{
			num += spawnData.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (SpawnArea.SpawnData spawnData2 in this.m_prefabs)
		{
			num3 += spawnData2.m_weight;
			if (num2 <= num3)
			{
				return spawnData2;
			}
		}
		return this.m_prefabs[this.m_prefabs.Count - 1];
	}

	// Token: 0x0600140F RID: 5135 RVA: 0x00093474 File Offset: 0x00091674
	private void GetInstances(out int near, out int total)
	{
		near = 0;
		total = 0;
		Vector3 position = base.transform.position;
		foreach (BaseAI baseAI in BaseAI.BaseAIInstances)
		{
			if (this.IsSpawnPrefab(baseAI.gameObject))
			{
				float num = Utils.DistanceXZ(baseAI.transform.position, position);
				if (num < this.m_nearRadius)
				{
					near++;
				}
				if (num < this.m_farRadius)
				{
					total++;
				}
			}
		}
	}

	// Token: 0x06001410 RID: 5136 RVA: 0x00093510 File Offset: 0x00091710
	private bool IsSpawnPrefab(GameObject go)
	{
		string name = go.name;
		Character component = go.GetComponent<Character>();
		foreach (SpawnArea.SpawnData spawnData in this.m_prefabs)
		{
			if (name.CustomStartsWith(spawnData.m_prefab.name) && (!component || !component.IsTamed()))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001411 RID: 5137 RVA: 0x00093598 File Offset: 0x00091798
	public float GetLevelUpChance()
	{
		if (Game.m_worldLevel > 0 && Game.instance.m_worldLevelEnemyLevelUpExponent > 0f)
		{
			return Mathf.Min(70f, Mathf.Pow(this.m_levelupChance, (float)Game.m_worldLevel * Game.instance.m_worldLevelEnemyLevelUpExponent));
		}
		return this.m_levelupChance * Game.m_enemyLevelUpRate;
	}

	// Token: 0x06001412 RID: 5138 RVA: 0x000935F4 File Offset: 0x000917F4
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position, this.m_spawnRadius);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position, this.m_nearRadius);
	}

	// Token: 0x040013CD RID: 5069
	private const float dt = 2f;

	// Token: 0x040013CE RID: 5070
	public List<SpawnArea.SpawnData> m_prefabs = new List<SpawnArea.SpawnData>();

	// Token: 0x040013CF RID: 5071
	public float m_levelupChance = 15f;

	// Token: 0x040013D0 RID: 5072
	public float m_spawnIntervalSec = 30f;

	// Token: 0x040013D1 RID: 5073
	public float m_triggerDistance = 256f;

	// Token: 0x040013D2 RID: 5074
	public bool m_setPatrolSpawnPoint = true;

	// Token: 0x040013D3 RID: 5075
	public float m_spawnRadius = 2f;

	// Token: 0x040013D4 RID: 5076
	public float m_nearRadius = 10f;

	// Token: 0x040013D5 RID: 5077
	public float m_farRadius = 1000f;

	// Token: 0x040013D6 RID: 5078
	public int m_maxNear = 3;

	// Token: 0x040013D7 RID: 5079
	public int m_maxTotal = 20;

	// Token: 0x040013D8 RID: 5080
	public bool m_onGroundOnly;

	// Token: 0x040013D9 RID: 5081
	public EffectList m_spawnEffects = new EffectList();

	// Token: 0x040013DA RID: 5082
	private ZNetView m_nview;

	// Token: 0x040013DB RID: 5083
	private float m_spawnTimer;

	// Token: 0x02000333 RID: 819
	[Serializable]
	public class SpawnData
	{
		// Token: 0x04002452 RID: 9298
		public GameObject m_prefab;

		// Token: 0x04002453 RID: 9299
		public float m_weight;

		// Token: 0x04002454 RID: 9300
		[Header("Level")]
		public int m_maxLevel = 1;

		// Token: 0x04002455 RID: 9301
		public int m_minLevel = 1;
	}
}
