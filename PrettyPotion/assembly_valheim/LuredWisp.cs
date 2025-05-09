using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000194 RID: 404
public class LuredWisp : MonoBehaviour
{
	// Token: 0x06001810 RID: 6160 RVA: 0x000B3990 File Offset: 0x000B1B90
	private void Awake()
	{
		LuredWisp.m_wisps.Add(this);
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_targetPoint = base.transform.position;
		this.m_time = (float)UnityEngine.Random.Range(0, 1000);
		base.InvokeRepeating("UpdateTarget", UnityEngine.Random.Range(0f, 2f), 2f);
	}

	// Token: 0x06001811 RID: 6161 RVA: 0x000B39F6 File Offset: 0x000B1BF6
	private void OnDestroy()
	{
		LuredWisp.m_wisps.Remove(this);
	}

	// Token: 0x06001812 RID: 6162 RVA: 0x000B3A04 File Offset: 0x000B1C04
	private void UpdateTarget()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_despawnTimer > 0f)
		{
			return;
		}
		WispSpawner bestSpawner = WispSpawner.GetBestSpawner(base.transform.position, this.m_maxLureDistance);
		if (bestSpawner == null || (this.m_despawnInDaylight && EnvMan.IsDaylight()))
		{
			this.m_despawnTimer = 3f;
			this.m_targetPoint = base.transform.position + Quaternion.Euler(-20f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * 100f;
			return;
		}
		this.m_despawnTimer = 0f;
		this.m_targetPoint = bestSpawner.m_spawnPoint.position;
	}

	// Token: 0x06001813 RID: 6163 RVA: 0x000B3AD6 File Offset: 0x000B1CD6
	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.UpdateMovement(this.m_targetPoint, Time.fixedDeltaTime);
	}

	// Token: 0x06001814 RID: 6164 RVA: 0x000B3B04 File Offset: 0x000B1D04
	private void UpdateMovement(Vector3 targetPos, float dt)
	{
		if (this.m_despawnTimer > 0f)
		{
			this.m_despawnTimer -= dt;
			if (this.m_despawnTimer <= 0f)
			{
				this.m_despawnEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				this.m_nview.Destroy();
				return;
			}
		}
		this.m_time += dt;
		float num = this.m_time * this.m_noiseSpeed;
		targetPos += new Vector3(Mathf.Sin(num * 4f), Mathf.Sin(num * 2f) * this.m_noiseDistanceYScale, Mathf.Cos(num * 5f)) * this.m_noiseDistance;
		Vector3 normalized = (targetPos - base.transform.position).normalized;
		this.m_ballVel += normalized * this.m_acceleration * dt;
		if (this.m_ballVel.magnitude > this.m_maxSpeed)
		{
			this.m_ballVel = this.m_ballVel.normalized * this.m_maxSpeed;
		}
		this.m_ballVel -= this.m_ballVel * this.m_friction;
		base.transform.position = base.transform.position + this.m_ballVel * dt;
	}

	// Token: 0x06001815 RID: 6165 RVA: 0x000B3C84 File Offset: 0x000B1E84
	public static int GetWispsInArea(Vector3 p, float r)
	{
		float num = r * r;
		int num2 = 0;
		foreach (LuredWisp luredWisp in LuredWisp.m_wisps)
		{
			if (Utils.DistanceSqr(p, luredWisp.transform.position) < num)
			{
				num2++;
			}
		}
		return num2;
	}

	// Token: 0x040017FE RID: 6142
	public bool m_despawnInDaylight = true;

	// Token: 0x040017FF RID: 6143
	public float m_maxLureDistance = 20f;

	// Token: 0x04001800 RID: 6144
	public float m_acceleration = 6f;

	// Token: 0x04001801 RID: 6145
	public float m_noiseDistance = 1.5f;

	// Token: 0x04001802 RID: 6146
	public float m_noiseDistanceYScale = 0.2f;

	// Token: 0x04001803 RID: 6147
	public float m_noiseSpeed = 0.5f;

	// Token: 0x04001804 RID: 6148
	public float m_maxSpeed = 40f;

	// Token: 0x04001805 RID: 6149
	public float m_friction = 0.03f;

	// Token: 0x04001806 RID: 6150
	public EffectList m_despawnEffects = new EffectList();

	// Token: 0x04001807 RID: 6151
	private static List<LuredWisp> m_wisps = new List<LuredWisp>();

	// Token: 0x04001808 RID: 6152
	private Vector3 m_ballVel = Vector3.zero;

	// Token: 0x04001809 RID: 6153
	private ZNetView m_nview;

	// Token: 0x0400180A RID: 6154
	private Vector3 m_targetPoint;

	// Token: 0x0400180B RID: 6155
	private float m_despawnTimer;

	// Token: 0x0400180C RID: 6156
	private float m_time;
}
