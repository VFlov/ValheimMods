using System;
using UnityEngine;

// Token: 0x020001B0 RID: 432
public class RopeAttachment : MonoBehaviour, Interactable, Hoverable
{
	// Token: 0x06001937 RID: 6455 RVA: 0x000BC615 File Offset: 0x000BA815
	private void Awake()
	{
		this.m_boatBody = base.GetComponentInParent<Rigidbody>();
	}

	// Token: 0x06001938 RID: 6456 RVA: 0x000BC623 File Offset: 0x000BA823
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_puller)
		{
			this.m_puller = null;
			ZLog.Log("Detached rope");
		}
		else
		{
			this.m_puller = character;
			ZLog.Log("Attached rope");
		}
		return true;
	}

	// Token: 0x06001939 RID: 6457 RVA: 0x000BC65C File Offset: 0x000BA85C
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x0600193A RID: 6458 RVA: 0x000BC65F File Offset: 0x000BA85F
	public string GetHoverText()
	{
		return this.m_hoverText;
	}

	// Token: 0x0600193B RID: 6459 RVA: 0x000BC667 File Offset: 0x000BA867
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x0600193C RID: 6460 RVA: 0x000BC670 File Offset: 0x000BA870
	private void FixedUpdate()
	{
		if (this.m_puller && Vector3.Distance(this.m_puller.transform.position, base.transform.position) > this.m_pullDistance)
		{
			Vector3 position = ((this.m_puller.transform.position - base.transform.position).normalized * this.m_maxPullVel - this.m_boatBody.GetPointVelocity(base.transform.position)) * this.m_pullForce;
			this.m_boatBody.AddForceAtPosition(base.transform.position, position);
		}
	}

	// Token: 0x0400199F RID: 6559
	public string m_name = "Rope";

	// Token: 0x040019A0 RID: 6560
	public string m_hoverText = "Pull";

	// Token: 0x040019A1 RID: 6561
	public float m_pullDistance = 5f;

	// Token: 0x040019A2 RID: 6562
	public float m_pullForce = 1f;

	// Token: 0x040019A3 RID: 6563
	public float m_maxPullVel = 1f;

	// Token: 0x040019A4 RID: 6564
	private Rigidbody m_boatBody;

	// Token: 0x040019A5 RID: 6565
	private Character m_puller;
}
