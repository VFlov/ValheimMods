using System;
using UnityEngine;

// Token: 0x0200004E RID: 78
public class DistantFogEmitter : MonoBehaviour
{
	// Token: 0x060005FD RID: 1533 RVA: 0x000329A0 File Offset: 0x00030BA0
	public void SetEmit(bool emit)
	{
		this.m_emit = emit;
	}

	// Token: 0x060005FE RID: 1534 RVA: 0x000329AC File Offset: 0x00030BAC
	private void Update()
	{
		if (!this.m_emit)
		{
			return;
		}
		if (WorldGenerator.instance == null)
		{
			return;
		}
		this.m_placeTimer += Time.deltaTime;
		if (this.m_placeTimer > this.m_interval)
		{
			this.m_placeTimer = 0f;
			int num = Mathf.Max(0, this.m_particles - this.TotalNrOfParticles());
			num /= 4;
			for (int i = 0; i < num; i++)
			{
				this.PlaceOne();
			}
		}
	}

	// Token: 0x060005FF RID: 1535 RVA: 0x00032A20 File Offset: 0x00030C20
	private int TotalNrOfParticles()
	{
		int num = 0;
		foreach (ParticleSystem particleSystem in this.m_psystems)
		{
			num += particleSystem.particleCount;
		}
		return num;
	}

	// Token: 0x06000600 RID: 1536 RVA: 0x00032A54 File Offset: 0x00030C54
	private void PlaceOne()
	{
		Vector3 a;
		if (this.GetRandomPoint(base.transform.position, out a))
		{
			ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
			emitParams.position = a + Vector3.up * this.m_placeOffset;
			this.m_psystems[UnityEngine.Random.Range(0, this.m_psystems.Length)].Emit(emitParams, 1);
		}
	}

	// Token: 0x06000601 RID: 1537 RVA: 0x00032AB8 File Offset: 0x00030CB8
	private bool GetRandomPoint(Vector3 center, out Vector3 p)
	{
		float f = UnityEngine.Random.value * 3.1415927f * 2f;
		float num = Mathf.Sqrt(UnityEngine.Random.value) * (this.m_maxRadius - this.m_minRadius) + this.m_minRadius;
		p = center + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
		p.y = WorldGenerator.instance.GetHeight(p.x, p.z);
		if (p.y < 30f)
		{
			if (this.m_skipWater)
			{
				return false;
			}
			if (UnityEngine.Random.value > this.m_waterSpawnChance)
			{
				return false;
			}
			p.y = 30f;
		}
		else if (p.y > this.m_mountainLimit)
		{
			if (UnityEngine.Random.value > this.m_mountainSpawnChance)
			{
				return false;
			}
		}
		else if (UnityEngine.Random.value > this.m_landSpawnChance)
		{
			return false;
		}
		return true;
	}

	// Token: 0x040006B9 RID: 1721
	public float m_interval = 1f;

	// Token: 0x040006BA RID: 1722
	public float m_minRadius = 100f;

	// Token: 0x040006BB RID: 1723
	public float m_maxRadius = 500f;

	// Token: 0x040006BC RID: 1724
	public float m_mountainSpawnChance = 1f;

	// Token: 0x040006BD RID: 1725
	public float m_landSpawnChance = 0.5f;

	// Token: 0x040006BE RID: 1726
	public float m_waterSpawnChance = 0.25f;

	// Token: 0x040006BF RID: 1727
	public float m_mountainLimit = 120f;

	// Token: 0x040006C0 RID: 1728
	public float m_emitStep = 10f;

	// Token: 0x040006C1 RID: 1729
	public int m_emitPerStep = 10;

	// Token: 0x040006C2 RID: 1730
	public int m_particles = 100;

	// Token: 0x040006C3 RID: 1731
	public float m_placeOffset = 1f;

	// Token: 0x040006C4 RID: 1732
	public ParticleSystem[] m_psystems;

	// Token: 0x040006C5 RID: 1733
	public bool m_skipWater;

	// Token: 0x040006C6 RID: 1734
	private float m_placeTimer;

	// Token: 0x040006C7 RID: 1735
	private bool m_emit = true;

	// Token: 0x040006C8 RID: 1736
	private Vector3 m_lastPosition = Vector3.zero;
}
