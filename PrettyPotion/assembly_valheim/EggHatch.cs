using System;
using UnityEngine;

// Token: 0x02000019 RID: 25
public class EggHatch : MonoBehaviour
{
	// Token: 0x0600026A RID: 618 RVA: 0x00015DD3 File Offset: 0x00013FD3
	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (UnityEngine.Random.value <= this.m_chanceToHatch)
		{
			base.InvokeRepeating("CheckSpawn", UnityEngine.Random.Range(1f, 2f), 1f);
		}
	}

	// Token: 0x0600026B RID: 619 RVA: 0x00015E10 File Offset: 0x00014010
	private void CheckSpawn()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, this.m_triggerDistance);
		if (closestPlayer && !closestPlayer.InGhostMode())
		{
			this.Hatch();
		}
	}

	// Token: 0x0600026C RID: 620 RVA: 0x00015E68 File Offset: 0x00014068
	private void Hatch()
	{
		this.m_hatchEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		UnityEngine.Object.Instantiate<GameObject>(this.m_spawnPrefab, base.transform.TransformPoint(this.m_spawnOffset), Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f));
		this.m_nview.Destroy();
	}

	// Token: 0x04000370 RID: 880
	public float m_triggerDistance = 5f;

	// Token: 0x04000371 RID: 881
	[Range(0f, 1f)]
	public float m_chanceToHatch = 1f;

	// Token: 0x04000372 RID: 882
	public Vector3 m_spawnOffset = new Vector3(0f, 0.5f, 0f);

	// Token: 0x04000373 RID: 883
	public GameObject m_spawnPrefab;

	// Token: 0x04000374 RID: 884
	public EffectList m_hatchEffect;

	// Token: 0x04000375 RID: 885
	private ZNetView m_nview;
}
