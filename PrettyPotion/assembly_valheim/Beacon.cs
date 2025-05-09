using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200015A RID: 346
public class Beacon : MonoBehaviour
{
	// Token: 0x06001522 RID: 5410 RVA: 0x0009B6EE File Offset: 0x000998EE
	private void Awake()
	{
		Beacon.m_instances.Add(this);
	}

	// Token: 0x06001523 RID: 5411 RVA: 0x0009B6FB File Offset: 0x000998FB
	private void OnDestroy()
	{
		Beacon.m_instances.Remove(this);
	}

	// Token: 0x06001524 RID: 5412 RVA: 0x0009B70C File Offset: 0x0009990C
	public static Beacon FindClosestBeaconInRange(Vector3 point)
	{
		Beacon beacon = null;
		float num = 999999f;
		foreach (Beacon beacon2 in Beacon.m_instances)
		{
			float num2 = Vector3.Distance(point, beacon2.transform.position);
			if (num2 < beacon2.m_range && (beacon == null || num2 < num))
			{
				beacon = beacon2;
				num = num2;
			}
		}
		return beacon;
	}

	// Token: 0x06001525 RID: 5413 RVA: 0x0009B790 File Offset: 0x00099990
	public static void FindBeaconsInRange(Vector3 point, List<Beacon> becons)
	{
		foreach (Beacon beacon in Beacon.m_instances)
		{
			if (Vector3.Distance(point, beacon.transform.position) < beacon.m_range)
			{
				becons.Add(beacon);
			}
		}
	}

	// Token: 0x0400149B RID: 5275
	public float m_range = 20f;

	// Token: 0x0400149C RID: 5276
	private static List<Beacon> m_instances = new List<Beacon>();
}
