using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000067 RID: 103
public class SmokeSpawner : MonoBehaviour, IMonoUpdater
{
	// Token: 0x060006B9 RID: 1721 RVA: 0x00038418 File Offset: 0x00036618
	private void Awake()
	{
		this.m_time = UnityEngine.Random.Range(0f, this.m_interval);
		if (this.m_stopFireOnStart)
		{
			foreach (Fire fire in Fire.s_fires)
			{
				if (fire && Vector3.Distance(fire.transform.position, base.transform.position) < this.m_spawnRadius)
				{
					ZNetScene.instance.Destroy(fire.gameObject);
				}
			}
		}
	}

	// Token: 0x060006BA RID: 1722 RVA: 0x000384BC File Offset: 0x000366BC
	private void OnEnable()
	{
		SmokeSpawner.Instances.Add(this);
	}

	// Token: 0x060006BB RID: 1723 RVA: 0x000384C9 File Offset: 0x000366C9
	private void OnDisable()
	{
		SmokeSpawner.Instances.Remove(this);
	}

	// Token: 0x060006BC RID: 1724 RVA: 0x000384D7 File Offset: 0x000366D7
	public void CustomUpdate(float deltaTime, float time)
	{
		this.m_time += deltaTime;
		if (this.m_time > this.m_interval)
		{
			this.m_time = 0f;
			this.Spawn(time);
		}
	}

	// Token: 0x060006BD RID: 1725 RVA: 0x00038508 File Offset: 0x00036708
	private void Spawn(float time)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || Vector3.Distance(localPlayer.transform.position, base.transform.position) > 64f)
		{
			this.m_lastSpawnTime = time;
			return;
		}
		if (this.TestBlocked())
		{
			return;
		}
		if (Smoke.GetTotalSmoke() > 100)
		{
			Smoke.FadeOldest();
		}
		Vector3 vector = base.transform.position;
		if (this.m_spawnRadius > 0f)
		{
			Vector2 vector2 = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(this.m_spawnRadius / 2f, this.m_spawnRadius);
			vector += new Vector3(vector2.x, 0f, vector2.y);
		}
		UnityEngine.Object.Instantiate<GameObject>(this.m_smokePrefab, vector, UnityEngine.Random.rotation);
		this.m_lastSpawnTime = time;
	}

	// Token: 0x060006BE RID: 1726 RVA: 0x000385DE File Offset: 0x000367DE
	private bool TestBlocked()
	{
		return Physics.CheckSphere(base.transform.position, this.m_testRadius, this.m_testMask.value);
	}

	// Token: 0x060006BF RID: 1727 RVA: 0x00038606 File Offset: 0x00036806
	public bool IsBlocked()
	{
		if (!base.gameObject.activeInHierarchy)
		{
			return this.TestBlocked();
		}
		return Time.time - this.m_lastSpawnTime > 4f;
	}

	// Token: 0x060006C0 RID: 1728 RVA: 0x0003862F File Offset: 0x0003682F
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Utils.DrawGizmoCircle(base.transform.position, this.m_spawnRadius, 16);
	}

	// Token: 0x1700001B RID: 27
	// (get) Token: 0x060006C1 RID: 1729 RVA: 0x00038653 File Offset: 0x00036853
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x040007CA RID: 1994
	private static Collider[] s_colliders = new Collider[30];

	// Token: 0x040007CB RID: 1995
	private const float m_minPlayerDistance = 64f;

	// Token: 0x040007CC RID: 1996
	private const int m_maxGlobalSmoke = 100;

	// Token: 0x040007CD RID: 1997
	private const float m_blockedMinTime = 4f;

	// Token: 0x040007CE RID: 1998
	public GameObject m_smokePrefab;

	// Token: 0x040007CF RID: 1999
	public float m_interval = 0.5f;

	// Token: 0x040007D0 RID: 2000
	public LayerMask m_testMask;

	// Token: 0x040007D1 RID: 2001
	public float m_testRadius = 0.5f;

	// Token: 0x040007D2 RID: 2002
	public float m_spawnRadius;

	// Token: 0x040007D3 RID: 2003
	public bool m_stopFireOnStart;

	// Token: 0x040007D4 RID: 2004
	private float m_lastSpawnTime;

	// Token: 0x040007D5 RID: 2005
	private float m_time;
}
