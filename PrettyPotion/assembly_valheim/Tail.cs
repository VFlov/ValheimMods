using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000042 RID: 66
public class Tail : MonoBehaviour, IMonoUpdater
{
	// Token: 0x06000559 RID: 1369 RVA: 0x0002DAEC File Offset: 0x0002BCEC
	private void Awake()
	{
		foreach (Transform transform in this.m_tailJoints)
		{
			float distance = Vector3.Distance(transform.parent.position, transform.position);
			Vector3 position = transform.position;
			Tail.TailSegment tailSegment = new Tail.TailSegment();
			tailSegment.transform = transform;
			tailSegment.pos = position;
			tailSegment.rot = transform.rotation;
			tailSegment.distance = distance;
			this.m_positions.Add(tailSegment);
		}
	}

	// Token: 0x0600055A RID: 1370 RVA: 0x0002DB90 File Offset: 0x0002BD90
	private void OnEnable()
	{
		Tail.Instances.Add(this);
	}

	// Token: 0x0600055B RID: 1371 RVA: 0x0002DB9D File Offset: 0x0002BD9D
	private void OnDisable()
	{
		Tail.Instances.Remove(this);
	}

	// Token: 0x0600055C RID: 1372 RVA: 0x0002DBAC File Offset: 0x0002BDAC
	public void CustomLateUpdate(float dt)
	{
		for (int i = 0; i < this.m_positions.Count; i++)
		{
			Tail.TailSegment tailSegment = this.m_positions[i];
			if (this.m_waterSurfaceCheck)
			{
				float liquidLevel = Floating.GetLiquidLevel(tailSegment.pos, 1f, LiquidType.All);
				if (tailSegment.pos.y + this.m_tailRadius > liquidLevel)
				{
					Tail.TailSegment tailSegment2 = tailSegment;
					tailSegment2.pos.y = tailSegment2.pos.y - this.m_gravity * dt;
				}
				else
				{
					Tail.TailSegment tailSegment3 = tailSegment;
					tailSegment3.pos.y = tailSegment3.pos.y - this.m_gravityInWater * dt;
				}
			}
			else
			{
				Tail.TailSegment tailSegment4 = tailSegment;
				tailSegment4.pos.y = tailSegment4.pos.y - this.m_gravity * dt;
			}
			Vector3 vector = tailSegment.transform.parent.position + tailSegment.transform.parent.up * tailSegment.distance * 0.5f;
			Vector3 vector2 = Vector3.Normalize(vector - tailSegment.pos);
			vector2 = Vector3.RotateTowards(-tailSegment.transform.parent.up, vector2, 0.017453292f * this.m_maxAngle, 1f);
			Vector3 vector3 = vector - vector2 * tailSegment.distance * 0.5f;
			if (this.m_groundCheck)
			{
				float groundHeight = ZoneSystem.instance.GetGroundHeight(vector3);
				if (vector3.y - this.m_tailRadius < groundHeight)
				{
					vector3.y = groundHeight + this.m_tailRadius;
				}
			}
			vector3 = Vector3.Lerp(tailSegment.pos, vector3, this.m_smoothness);
			if (vector == vector3)
			{
				return;
			}
			Vector3 normalized = (vector - vector3).normalized;
			Vector3 rhs = Vector3.Cross(Vector3.up, -normalized);
			Quaternion quaternion = Quaternion.LookRotation(Vector3.Cross(-normalized, rhs), -normalized);
			quaternion = Quaternion.Slerp(tailSegment.rot, quaternion, this.m_smoothness);
			tailSegment.transform.position = vector3;
			tailSegment.transform.rotation = quaternion;
			tailSegment.pos = vector3;
			tailSegment.rot = quaternion;
		}
	}

	// Token: 0x17000010 RID: 16
	// (get) Token: 0x0600055D RID: 1373 RVA: 0x0002DDCE File Offset: 0x0002BFCE
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x040005FD RID: 1533
	public List<Transform> m_tailJoints = new List<Transform>();

	// Token: 0x040005FE RID: 1534
	public float m_maxAngle = 80f;

	// Token: 0x040005FF RID: 1535
	public float m_gravity = 2f;

	// Token: 0x04000600 RID: 1536
	public float m_gravityInWater = 0.1f;

	// Token: 0x04000601 RID: 1537
	public bool m_waterSurfaceCheck;

	// Token: 0x04000602 RID: 1538
	public bool m_groundCheck;

	// Token: 0x04000603 RID: 1539
	public float m_smoothness = 0.1f;

	// Token: 0x04000604 RID: 1540
	public float m_tailRadius;

	// Token: 0x04000605 RID: 1541
	public Character m_character;

	// Token: 0x04000606 RID: 1542
	public Rigidbody m_characterBody;

	// Token: 0x04000607 RID: 1543
	public Rigidbody m_tailBody;

	// Token: 0x04000608 RID: 1544
	private readonly List<Tail.TailSegment> m_positions = new List<Tail.TailSegment>();

	// Token: 0x02000247 RID: 583
	private class TailSegment
	{
		// Token: 0x04001FF4 RID: 8180
		public Transform transform;

		// Token: 0x04001FF5 RID: 8181
		public Vector3 pos;

		// Token: 0x04001FF6 RID: 8182
		public Quaternion rot;

		// Token: 0x04001FF7 RID: 8183
		public float distance;
	}
}
