using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001C0 RID: 448
public class StaticTarget : MonoBehaviour
{
	// Token: 0x06001A1C RID: 6684 RVA: 0x000C2965 File Offset: 0x000C0B65
	public virtual bool IsPriorityTarget()
	{
		return this.m_primaryTarget;
	}

	// Token: 0x06001A1D RID: 6685 RVA: 0x000C296D File Offset: 0x000C0B6D
	public virtual bool IsRandomTarget()
	{
		return this.m_randomTarget;
	}

	// Token: 0x06001A1E RID: 6686 RVA: 0x000C2978 File Offset: 0x000C0B78
	public Vector3 GetCenter()
	{
		if (!this.m_haveCenter)
		{
			List<Collider> allColliders = this.GetAllColliders();
			this.m_localCenter = Vector3.zero;
			foreach (Collider collider in allColliders)
			{
				if (collider)
				{
					this.m_localCenter += collider.bounds.center;
				}
			}
			this.m_localCenter /= (float)this.m_colliders.Count;
			this.m_localCenter = base.transform.InverseTransformPoint(this.m_localCenter);
			this.m_haveCenter = true;
		}
		return base.transform.TransformPoint(this.m_localCenter);
	}

	// Token: 0x06001A1F RID: 6687 RVA: 0x000C2A50 File Offset: 0x000C0C50
	public List<Collider> GetAllColliders()
	{
		if (this.m_colliders == null)
		{
			Collider[] componentsInChildren = base.GetComponentsInChildren<Collider>();
			this.m_colliders = new List<Collider>();
			this.m_colliders.Capacity = componentsInChildren.Length;
			foreach (Collider collider in componentsInChildren)
			{
				if (collider.enabled && collider.gameObject.activeInHierarchy && !collider.isTrigger)
				{
					this.m_colliders.Add(collider);
				}
			}
		}
		return this.m_colliders;
	}

	// Token: 0x06001A20 RID: 6688 RVA: 0x000C2AC8 File Offset: 0x000C0CC8
	public Vector3 FindClosestPoint(Vector3 point)
	{
		List<Collider> allColliders = this.GetAllColliders();
		if (allColliders.Count == 0)
		{
			return base.transform.position;
		}
		float num = 9999999f;
		Vector3 result = Vector3.zero;
		foreach (Collider collider in allColliders)
		{
			if (collider)
			{
				MeshCollider meshCollider = collider as MeshCollider;
				Vector3 vector = (meshCollider && !meshCollider.convex) ? collider.ClosestPointOnBounds(point) : collider.ClosestPoint(point);
				float num2 = Vector3.Distance(point, vector);
				if (num2 < num)
				{
					result = vector;
					num = num2;
				}
			}
		}
		return result;
	}

	// Token: 0x04001A97 RID: 6807
	[Header("Static target")]
	public bool m_primaryTarget;

	// Token: 0x04001A98 RID: 6808
	public bool m_randomTarget = true;

	// Token: 0x04001A99 RID: 6809
	private List<Collider> m_colliders;

	// Token: 0x04001A9A RID: 6810
	private Vector3 m_localCenter;

	// Token: 0x04001A9B RID: 6811
	private bool m_haveCenter;
}
