using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000046 RID: 70
public class TriggerSpawner : MonoBehaviour
{
	// Token: 0x060005A0 RID: 1440 RVA: 0x0002FB39 File Offset: 0x0002DD39
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_nview.Register("Trigger", new Action<long>(this.RPC_Trigger));
		TriggerSpawner.m_allSpawners.Add(this);
	}

	// Token: 0x060005A1 RID: 1441 RVA: 0x0002FB6E File Offset: 0x0002DD6E
	private void OnDestroy()
	{
		TriggerSpawner.m_allSpawners.Remove(this);
	}

	// Token: 0x060005A2 RID: 1442 RVA: 0x0002FB7C File Offset: 0x0002DD7C
	public static void TriggerAllInRange(Vector3 p, float range)
	{
		ZLog.Log("Trigging spawners in range");
		foreach (TriggerSpawner triggerSpawner in TriggerSpawner.m_allSpawners)
		{
			if (Vector3.Distance(triggerSpawner.transform.position, p) < range)
			{
				triggerSpawner.Trigger();
			}
		}
	}

	// Token: 0x060005A3 RID: 1443 RVA: 0x0002FBEC File Offset: 0x0002DDEC
	private void Trigger()
	{
		this.m_nview.InvokeRPC("Trigger", Array.Empty<object>());
	}

	// Token: 0x060005A4 RID: 1444 RVA: 0x0002FC03 File Offset: 0x0002DE03
	private void RPC_Trigger(long sender)
	{
		ZLog.Log("Trigging " + base.gameObject.name);
		this.TrySpawning();
	}

	// Token: 0x060005A5 RID: 1445 RVA: 0x0002FC28 File Offset: 0x0002DE28
	private void TrySpawning()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_minSpawnInterval > 0f)
		{
			DateTime time = ZNet.instance.GetTime();
			DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L));
			TimeSpan timeSpan = time - d;
			if (timeSpan.TotalMinutes < (double)this.m_minSpawnInterval)
			{
				string str = "Not enough time passed ";
				TimeSpan timeSpan2 = timeSpan;
				ZLog.Log(str + timeSpan2.ToString());
				return;
			}
		}
		if (UnityEngine.Random.Range(0f, 100f) > this.m_spawnChance)
		{
			ZLog.Log("Spawn chance fail " + this.m_spawnChance.ToString());
			return;
		}
		this.Spawn();
	}

	// Token: 0x060005A6 RID: 1446 RVA: 0x0002FCE8 File Offset: 0x0002DEE8
	private bool Spawn()
	{
		Vector3 position = base.transform.position;
		float y;
		if (ZoneSystem.instance.FindFloor(position, out y))
		{
			position.y = y;
		}
		GameObject gameObject = this.m_creaturePrefabs[UnityEngine.Random.Range(0, this.m_creaturePrefabs.Length)];
		int num = this.m_maxSpawned + (int)(this.m_maxExtraPerPlayer * (float)Game.instance.GetPlayerDifficulty(base.transform.position));
		if (num > 0 && SpawnSystem.GetNrOfInstances(gameObject, base.transform.position, this.m_maxSpawnedRange, false, false) >= num)
		{
			return false;
		}
		Quaternion rotation = this.m_useSpawnerRotation ? base.transform.rotation : Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, position, rotation);
		gameObject2.GetComponent<ZNetView>();
		BaseAI component = gameObject2.GetComponent<BaseAI>();
		if (component != null)
		{
			if (this.m_setPatrolSpawnPoint)
			{
				component.SetPatrolPoint();
			}
			if (this.m_setHuntPlayer)
			{
				component.SetHuntPlayer(true);
			}
		}
		if (this.m_maxLevel > 1)
		{
			Character component2 = gameObject2.GetComponent<Character>();
			if (component2)
			{
				int num2 = this.m_minLevel;
				while (num2 < this.m_maxLevel && UnityEngine.Random.Range(0f, 100f) <= this.m_levelupChance)
				{
					num2++;
				}
				if (num2 > 1)
				{
					component2.SetLevel(num2);
				}
			}
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
		this.m_spawnEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		return true;
	}

	// Token: 0x060005A7 RID: 1447 RVA: 0x0002FE9C File Offset: 0x0002E09C
	private float GetRadius()
	{
		return 0.75f;
	}

	// Token: 0x060005A8 RID: 1448 RVA: 0x0002FEA3 File Offset: 0x0002E0A3
	private void OnDrawGizmos()
	{
	}

	// Token: 0x0400063B RID: 1595
	private const float m_radius = 0.75f;

	// Token: 0x0400063C RID: 1596
	public GameObject[] m_creaturePrefabs;

	// Token: 0x0400063D RID: 1597
	[Header("Level")]
	public int m_maxLevel = 1;

	// Token: 0x0400063E RID: 1598
	public int m_minLevel = 1;

	// Token: 0x0400063F RID: 1599
	public float m_levelupChance = 10f;

	// Token: 0x04000640 RID: 1600
	[Header("Spawn settings")]
	[Range(0f, 100f)]
	public float m_spawnChance = 100f;

	// Token: 0x04000641 RID: 1601
	public float m_minSpawnInterval = 10f;

	// Token: 0x04000642 RID: 1602
	public int m_maxSpawned = 10;

	// Token: 0x04000643 RID: 1603
	public float m_maxExtraPerPlayer;

	// Token: 0x04000644 RID: 1604
	public float m_maxSpawnedRange = 30f;

	// Token: 0x04000645 RID: 1605
	public bool m_setHuntPlayer;

	// Token: 0x04000646 RID: 1606
	public bool m_setPatrolSpawnPoint;

	// Token: 0x04000647 RID: 1607
	public bool m_useSpawnerRotation;

	// Token: 0x04000648 RID: 1608
	public EffectList m_spawnEffects = new EffectList();

	// Token: 0x04000649 RID: 1609
	private ZNetView m_nview;

	// Token: 0x0400064A RID: 1610
	private static List<TriggerSpawner> m_allSpawners = new List<TriggerSpawner>();
}
