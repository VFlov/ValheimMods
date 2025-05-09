using System;
using UnityEngine;

// Token: 0x0200019E RID: 414
public class Odin : MonoBehaviour
{
	// Token: 0x06001874 RID: 6260 RVA: 0x000B6D7A File Offset: 0x000B4F7A
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
	}

	// Token: 0x06001875 RID: 6261 RVA: 0x000B6D88 File Offset: 0x000B4F88
	private void Update()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, this.m_despawnFarDistance);
		if (closestPlayer == null)
		{
			this.m_despawn.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			this.m_nview.Destroy();
			ZLog.Log("No player in range, despawning");
			return;
		}
		Vector3 forward = closestPlayer.transform.position - base.transform.position;
		forward.y = 0f;
		forward.Normalize();
		base.transform.rotation = Quaternion.LookRotation(forward);
		if (Vector3.Distance(closestPlayer.transform.position, base.transform.position) < this.m_despawnCloseDistance)
		{
			this.m_despawn.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			this.m_nview.Destroy();
			ZLog.Log("Player go too close,despawning");
			return;
		}
		this.m_time += Time.deltaTime;
		if (this.m_time > this.m_ttl)
		{
			this.m_despawn.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			this.m_nview.Destroy();
			ZLog.Log("timeout " + this.m_time.ToString() + " , despawning");
			return;
		}
	}

	// Token: 0x04001870 RID: 6256
	public float m_despawnCloseDistance = 20f;

	// Token: 0x04001871 RID: 6257
	public float m_despawnFarDistance = 50f;

	// Token: 0x04001872 RID: 6258
	public EffectList m_despawn = new EffectList();

	// Token: 0x04001873 RID: 6259
	public float m_ttl = 300f;

	// Token: 0x04001874 RID: 6260
	private float m_time;

	// Token: 0x04001875 RID: 6261
	private ZNetView m_nview;
}
