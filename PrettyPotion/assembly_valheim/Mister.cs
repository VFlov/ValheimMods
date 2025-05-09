using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200019A RID: 410
public class Mister : MonoBehaviour
{
	// Token: 0x0600184B RID: 6219 RVA: 0x000B5BCB File Offset: 0x000B3DCB
	private void Awake()
	{
	}

	// Token: 0x0600184C RID: 6220 RVA: 0x000B5BCD File Offset: 0x000B3DCD
	private void OnEnable()
	{
		Mister.m_instances.Add(this);
	}

	// Token: 0x0600184D RID: 6221 RVA: 0x000B5BDA File Offset: 0x000B3DDA
	private void OnDisable()
	{
		Mister.m_instances.Remove(this);
	}

	// Token: 0x0600184E RID: 6222 RVA: 0x000B5BE8 File Offset: 0x000B3DE8
	public static List<Mister> GetMisters()
	{
		return Mister.m_instances;
	}

	// Token: 0x0600184F RID: 6223 RVA: 0x000B5BF0 File Offset: 0x000B3DF0
	public static List<Mister> GetDemistersSorted(Vector3 refPoint)
	{
		foreach (Mister mister in Mister.m_instances)
		{
			mister.m_tempDistance = Vector3.Distance(mister.transform.position, refPoint);
		}
		Mister.m_instances.Sort((Mister a, Mister b) => a.m_tempDistance.CompareTo(b.m_tempDistance));
		return Mister.m_instances;
	}

	// Token: 0x06001850 RID: 6224 RVA: 0x000B5C80 File Offset: 0x000B3E80
	public static Mister FindMister(Vector3 p)
	{
		foreach (Mister mister in Mister.m_instances)
		{
			if (Vector3.Distance(mister.transform.position, p) < mister.m_radius)
			{
				return mister;
			}
		}
		return null;
	}

	// Token: 0x06001851 RID: 6225 RVA: 0x000B5CEC File Offset: 0x000B3EEC
	public static bool InsideMister(Vector3 p, float radius = 0f)
	{
		foreach (Mister mister in Mister.m_instances)
		{
			if (Vector3.Distance(mister.transform.position, p) < mister.m_radius + radius && p.y - radius < mister.transform.position.y + mister.m_height)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001852 RID: 6226 RVA: 0x000B5D7C File Offset: 0x000B3F7C
	public bool IsCompletelyInsideOtherMister(float thickness)
	{
		Vector3 position = base.transform.position;
		foreach (Mister mister in Mister.m_instances)
		{
			if (!(mister == this) && Vector3.Distance(position, mister.transform.position) + this.m_radius + thickness < mister.m_radius && position.y + this.m_height < mister.transform.position.y + mister.m_height)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001853 RID: 6227 RVA: 0x000B5E2C File Offset: 0x000B402C
	public bool Inside(Vector3 p, float radius)
	{
		return Vector3.Distance(p, base.transform.position) < radius && p.y - radius < base.transform.position.y + this.m_height;
	}

	// Token: 0x06001854 RID: 6228 RVA: 0x000B5E68 File Offset: 0x000B4068
	public static bool IsInsideOtherMister(Vector3 p, Mister ignore)
	{
		foreach (Mister mister in Mister.m_instances)
		{
			if (!(mister == ignore) && Vector3.Distance(p, mister.transform.position) < mister.m_radius && p.y < mister.transform.position.y + mister.m_height)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001855 RID: 6229 RVA: 0x000B5EFC File Offset: 0x000B40FC
	private void OnDrawGizmosSelected()
	{
	}

	// Token: 0x04001846 RID: 6214
	public float m_radius = 50f;

	// Token: 0x04001847 RID: 6215
	public float m_height = 10f;

	// Token: 0x04001848 RID: 6216
	private float m_tempDistance;

	// Token: 0x04001849 RID: 6217
	private static List<Mister> m_instances = new List<Mister>();
}
