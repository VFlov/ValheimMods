using System;
using UnityEngine;

// Token: 0x0200014B RID: 331
public class StaticPhysics : SlowUpdate
{
	// Token: 0x0600142B RID: 5163 RVA: 0x0009474E File Offset: 0x0009294E
	public override void Awake()
	{
		base.Awake();
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_updateTime = Time.time + 20f;
		this.m_activeArea = ZoneSystem.instance.m_activeArea;
	}

	// Token: 0x0600142C RID: 5164 RVA: 0x00094783 File Offset: 0x00092983
	private bool ShouldUpdate(float time)
	{
		return time > this.m_updateTime;
	}

	// Token: 0x0600142D RID: 5165 RVA: 0x00094790 File Offset: 0x00092990
	public override void SUpdate(float time, Vector2i referenceZone)
	{
		if (this.m_falling || !this.ShouldUpdate(time) || ZNetScene.OutsideActiveArea(base.transform.position, referenceZone, this.m_activeArea))
		{
			return;
		}
		if (this.m_fall)
		{
			this.CheckFall();
		}
		if (this.m_pushUp)
		{
			this.PushUp();
		}
	}

	// Token: 0x0600142E RID: 5166 RVA: 0x000947E4 File Offset: 0x000929E4
	private void CheckFall()
	{
		float fallHeight = this.GetFallHeight();
		if (base.transform.position.y > fallHeight + 0.05f)
		{
			this.Fall();
		}
	}

	// Token: 0x0600142F RID: 5167 RVA: 0x00094818 File Offset: 0x00092A18
	private float GetFallHeight()
	{
		if (this.m_checkSolids)
		{
			float result;
			if (ZoneSystem.instance.GetSolidHeight(base.transform.position, this.m_fallCheckRadius, out result, base.transform))
			{
				return result;
			}
			return base.transform.position.y;
		}
		else
		{
			float result2;
			if (ZoneSystem.instance.GetGroundHeight(base.transform.position, out result2))
			{
				return result2;
			}
			return base.transform.position.y;
		}
	}

	// Token: 0x06001430 RID: 5168 RVA: 0x00094890 File Offset: 0x00092A90
	private void Fall()
	{
		this.m_falling = true;
		base.gameObject.isStatic = false;
		base.InvokeRepeating("FallUpdate", 0.05f, 0.05f);
	}

	// Token: 0x06001431 RID: 5169 RVA: 0x000948BC File Offset: 0x00092ABC
	private void FallUpdate()
	{
		float fallHeight = this.GetFallHeight();
		Vector3 position = base.transform.position;
		position.y -= 0.2f;
		if (position.y <= fallHeight)
		{
			position.y = fallHeight;
			this.StopFalling();
		}
		base.transform.position = position;
		if (this.m_nview && this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().SetPosition(base.transform.position);
		}
	}

	// Token: 0x06001432 RID: 5170 RVA: 0x00094951 File Offset: 0x00092B51
	private void StopFalling()
	{
		base.gameObject.isStatic = true;
		this.m_falling = false;
		base.CancelInvoke("FallUpdate");
	}

	// Token: 0x06001433 RID: 5171 RVA: 0x00094974 File Offset: 0x00092B74
	private void PushUp()
	{
		float num;
		if (ZoneSystem.instance.GetGroundHeight(base.transform.position, out num) && base.transform.position.y < num - 0.05f)
		{
			base.gameObject.isStatic = false;
			Vector3 position = base.transform.position;
			position.y = num;
			base.transform.position = position;
			base.gameObject.isStatic = true;
			if (this.m_nview && this.m_nview.IsValid() && this.m_nview.IsOwner())
			{
				this.m_nview.GetZDO().SetPosition(base.transform.position);
			}
		}
	}

	// Token: 0x170000B9 RID: 185
	// (get) Token: 0x06001434 RID: 5172 RVA: 0x00094A30 File Offset: 0x00092C30
	public bool IsFalling
	{
		get
		{
			return this.m_falling;
		}
	}

	// Token: 0x040013E8 RID: 5096
	public bool m_pushUp = true;

	// Token: 0x040013E9 RID: 5097
	public bool m_fall = true;

	// Token: 0x040013EA RID: 5098
	public bool m_checkSolids;

	// Token: 0x040013EB RID: 5099
	public float m_fallCheckRadius;

	// Token: 0x040013EC RID: 5100
	private ZNetView m_nview;

	// Token: 0x040013ED RID: 5101
	private const float m_fallSpeed = 4f;

	// Token: 0x040013EE RID: 5102
	private const float m_fallStep = 0.05f;

	// Token: 0x040013EF RID: 5103
	private float m_updateTime;

	// Token: 0x040013F0 RID: 5104
	private bool m_falling;

	// Token: 0x040013F1 RID: 5105
	private int m_activeArea;
}
