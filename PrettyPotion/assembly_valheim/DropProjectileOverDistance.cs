using System;
using UnityEngine;

// Token: 0x02000017 RID: 23
public class DropProjectileOverDistance : MonoBehaviour
{
	// Token: 0x0600025E RID: 606 RVA: 0x000156E2 File Offset: 0x000138E2
	private void Awake()
	{
		this.m_character = base.GetComponent<Character>();
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_projectilePrefab == null)
		{
			base.enabled = false;
		}
	}

	// Token: 0x0600025F RID: 607 RVA: 0x0001570C File Offset: 0x0001390C
	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		Vector3 vector = base.transform.position.Horizontal();
		this.m_distanceAccumulator += Vector3.Distance(this.lastPosition, vector);
		Vector3 vector2 = this.lastPosition.DirTo(vector);
		if (this.lastPosition != vector)
		{
			this.lastPosition = vector;
		}
		if (this.m_timeToForceSpawn > 0f)
		{
			this.m_spawnTimer += Time.deltaTime;
			if (this.m_spawnTimer > this.m_timeToForceSpawn)
			{
				this.SpawnProjectile(base.transform.position, vector2);
				this.m_distanceAccumulator -= this.m_distancePerProjectile;
				this.m_distanceAccumulator = Mathf.Max(this.m_distanceAccumulator, 0f);
			}
		}
		if (this.m_distanceAccumulator < this.m_distancePerProjectile)
		{
			return;
		}
		int num = Mathf.FloorToInt(this.m_distanceAccumulator / this.m_distancePerProjectile);
		for (int i = 0; i < Mathf.Min(3, num); i++)
		{
			this.SpawnProjectile(base.transform.position - vector2 * (float)i, vector2);
			this.m_distanceAccumulator -= this.m_distancePerProjectile;
			num--;
		}
		this.m_distanceAccumulator -= this.m_distancePerProjectile * (float)num;
	}

	// Token: 0x06000260 RID: 608 RVA: 0x00015868 File Offset: 0x00013A68
	private void SpawnProjectile(Vector3 point, Vector3 travelDirection)
	{
		this.m_spawnTimer = 0f;
		if (this.m_projectilePrefab.GetComponent<IProjectile>() == null)
		{
			ZLog.LogWarning("Attempted to spawn non-projectile");
		}
		point.y += this.m_spawnHeight;
		if (this.m_snapToGround)
		{
			float y;
			ZoneSystem.instance.GetSolidHeight(point, out y, 1000);
			point.y = y;
		}
		UnityEngine.Object.Instantiate<GameObject>(this.m_projectilePrefab, point, Quaternion.LookRotation(travelDirection)).GetComponent<IProjectile>().Setup(this.m_character, travelDirection * UnityEngine.Random.Range(this.m_minVelocity, this.m_maxVelocity), -1f, null, null, null);
	}

	// Token: 0x06000261 RID: 609 RVA: 0x0001590C File Offset: 0x00013B0C
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(0.76f, 0.52f, 0.55f);
		Gizmos.DrawLine(base.transform.position, base.transform.position + Vector3.up * this.m_spawnHeight);
		Vector3 vector = base.transform.position + base.transform.forward * this.m_distancePerProjectile;
		Gizmos.DrawLine(base.transform.position + Vector3.up * 0.5f * this.m_spawnHeight, vector + Vector3.up * 0.5f * this.m_spawnHeight);
		Gizmos.DrawLine(vector, vector + Vector3.up * this.m_spawnHeight);
	}

	// Token: 0x04000357 RID: 855
	public GameObject m_projectilePrefab;

	// Token: 0x04000358 RID: 856
	public float m_distancePerProjectile = 5f;

	// Token: 0x04000359 RID: 857
	public float m_spawnHeight = 1f;

	// Token: 0x0400035A RID: 858
	public bool m_snapToGround;

	// Token: 0x0400035B RID: 859
	[global::Tooltip("If higher than 0, will force a spawn if nothing has spawned in that amount of time.")]
	public float m_timeToForceSpawn = -1f;

	// Token: 0x0400035C RID: 860
	public float m_minVelocity;

	// Token: 0x0400035D RID: 861
	public float m_maxVelocity;

	// Token: 0x0400035E RID: 862
	private Character m_character;

	// Token: 0x0400035F RID: 863
	private ZNetView m_nview;

	// Token: 0x04000360 RID: 864
	private Vector3 lastPosition;

	// Token: 0x04000361 RID: 865
	private float m_distanceAccumulator;

	// Token: 0x04000362 RID: 866
	private float m_spawnTimer;

	// Token: 0x04000363 RID: 867
	private const int c_MaxSpawnsPerFrame = 3;
}
