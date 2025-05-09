using System;
using UnityEngine;

// Token: 0x0200005F RID: 95
public class MistEmitter : MonoBehaviour
{
	// Token: 0x06000677 RID: 1655 RVA: 0x000364CA File Offset: 0x000346CA
	public void SetEmit(bool emit)
	{
		this.m_emit = emit;
	}

	// Token: 0x06000678 RID: 1656 RVA: 0x000364D3 File Offset: 0x000346D3
	private void Update()
	{
		if (!this.m_emit)
		{
			return;
		}
		this.m_placeTimer += Time.deltaTime;
		if (this.m_placeTimer > this.m_interval)
		{
			this.m_placeTimer = 0f;
			this.PlaceOne();
		}
	}

	// Token: 0x06000679 RID: 1657 RVA: 0x00036510 File Offset: 0x00034710
	private void PlaceOne()
	{
		Vector3 vector;
		if (MistEmitter.GetRandomPoint(base.transform.position, this.m_totalRadius, out vector))
		{
			int num = 0;
			float num2 = 6.2831855f / (float)this.m_rays;
			for (int i = 0; i < this.m_rays; i++)
			{
				float angle = (float)i * num2;
				if ((double)MistEmitter.GetPointOnEdge(vector, angle, this.m_testRadius).y < (double)vector.y - 0.1)
				{
					num++;
				}
			}
			if (num > this.m_rays / 4)
			{
				return;
			}
			if (EffectArea.IsPointInsideArea(vector, EffectArea.Type.Fire, this.m_testRadius))
			{
				return;
			}
			ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
			emitParams.position = vector + Vector3.up * this.m_placeOffset;
			this.m_psystem.Emit(emitParams, 1);
		}
	}

	// Token: 0x0600067A RID: 1658 RVA: 0x000365E4 File Offset: 0x000347E4
	private static bool GetRandomPoint(Vector3 center, float radius, out Vector3 p)
	{
		float f = UnityEngine.Random.value * 3.1415927f * 2f;
		float num = UnityEngine.Random.Range(0f, radius);
		p = center + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
		float num2;
		if (!ZoneSystem.instance.GetGroundHeight(p, out num2))
		{
			return false;
		}
		if (num2 < 30f)
		{
			return false;
		}
		float liquidLevel = Floating.GetLiquidLevel(p, 1f, LiquidType.All);
		if (num2 < liquidLevel)
		{
			return false;
		}
		p.y = num2;
		return true;
	}

	// Token: 0x0600067B RID: 1659 RVA: 0x00036678 File Offset: 0x00034878
	private static Vector3 GetPointOnEdge(Vector3 center, float angle, float radius)
	{
		Vector3 vector = center + new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
		vector.y = ZoneSystem.instance.GetGroundHeight(vector);
		if (vector.y < 30f)
		{
			vector.y = 30f;
		}
		return vector;
	}

	// Token: 0x04000772 RID: 1906
	public float m_interval = 1f;

	// Token: 0x04000773 RID: 1907
	public float m_totalRadius = 30f;

	// Token: 0x04000774 RID: 1908
	public float m_testRadius = 5f;

	// Token: 0x04000775 RID: 1909
	public int m_rays = 10;

	// Token: 0x04000776 RID: 1910
	public float m_placeOffset = 1f;

	// Token: 0x04000777 RID: 1911
	public ParticleSystem m_psystem;

	// Token: 0x04000778 RID: 1912
	private float m_placeTimer;

	// Token: 0x04000779 RID: 1913
	private bool m_emit = true;
}
