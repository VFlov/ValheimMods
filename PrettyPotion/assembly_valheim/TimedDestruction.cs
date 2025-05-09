using System;
using UnityEngine;

// Token: 0x020001CE RID: 462
public class TimedDestruction : MonoBehaviour
{
	// Token: 0x06001A93 RID: 6803 RVA: 0x000C5AF1 File Offset: 0x000C3CF1
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_triggerOnAwake)
		{
			this.Trigger();
		}
	}

	// Token: 0x06001A94 RID: 6804 RVA: 0x000C5B0D File Offset: 0x000C3D0D
	public void Trigger()
	{
		base.InvokeRepeating("DestroyNow", this.m_timeout, 1f);
	}

	// Token: 0x06001A95 RID: 6805 RVA: 0x000C5B25 File Offset: 0x000C3D25
	public void Trigger(float timeout)
	{
		base.InvokeRepeating("DestroyNow", timeout, 1f);
	}

	// Token: 0x06001A96 RID: 6806 RVA: 0x000C5B38 File Offset: 0x000C3D38
	private void DestroyNow()
	{
		if (this.m_nview)
		{
			if (!this.m_nview.IsValid())
			{
				return;
			}
			if (!this.m_nview.HasOwner() && this.m_forceTakeOwnershipAndDestroy)
			{
				this.m_nview.ClaimOwnership();
			}
			if (this.m_nview.IsOwner())
			{
				ZNetScene.instance.Destroy(base.gameObject);
				return;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	// Token: 0x04001AF5 RID: 6901
	public float m_timeout = 1f;

	// Token: 0x04001AF6 RID: 6902
	public bool m_triggerOnAwake;

	// Token: 0x04001AF7 RID: 6903
	[global::Tooltip("If there are objects that you always want to destroy, even if there is no owner, check this. For instance, fires in the ashlands may be created by cinder rain outside of ownership-zones, so they must be deleted even if no owner exists.")]
	public bool m_forceTakeOwnershipAndDestroy;

	// Token: 0x04001AF8 RID: 6904
	private ZNetView m_nview;
}
