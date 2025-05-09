using System;
using UnityEngine;

// Token: 0x0200018E RID: 398
public class Leviathan : MonoBehaviour
{
	// Token: 0x060017C6 RID: 6086 RVA: 0x000B0DF0 File Offset: 0x000AEFF0
	private void Awake()
	{
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_zanimator = base.GetComponent<ZSyncAnimation>();
		this.m_animator = base.GetComponentInChildren<Animator>();
		if (base.GetComponent<MineRock>())
		{
			MineRock mineRock = this.m_mineRock;
			mineRock.m_onHit = (Action)Delegate.Combine(mineRock.m_onHit, new Action(this.OnHit));
		}
		if (this.m_nview.IsValid() && this.m_nview.IsOwner() && this.m_nview.GetZDO().GetBool(ZDOVars.s_dead, false))
		{
			this.m_nview.Destroy();
		}
	}

	// Token: 0x060017C7 RID: 6087 RVA: 0x000B0EA0 File Offset: 0x000AF0A0
	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		float liquidLevel = Floating.GetLiquidLevel(base.transform.position, this.m_waveScale, LiquidType.All);
		if (this.m_alignToWaterLevel)
		{
			if (liquidLevel > -100f)
			{
				Vector3 position = this.m_body.position;
				float num = Mathf.Clamp((liquidLevel - (position.y + this.m_floatOffset)) * this.m_movementSpeed * Time.fixedDeltaTime, -this.m_maxSpeed, this.m_maxSpeed);
				position.y += num;
				this.m_body.MovePosition(position);
			}
			else
			{
				Vector3 position2 = this.m_body.position;
				position2.y = 0f;
				this.m_body.MovePosition(Vector3.MoveTowards(this.m_body.position, position2, Time.deltaTime));
			}
		}
		if (this.m_animator.GetCurrentAnimatorStateInfo(0).IsTag("submerged"))
		{
			this.m_nview.Destroy();
		}
	}

	// Token: 0x060017C8 RID: 6088 RVA: 0x000B0FAC File Offset: 0x000AF1AC
	private void OnHit()
	{
		if (UnityEngine.Random.value <= this.m_hitReactionChance)
		{
			if (this.m_left)
			{
				return;
			}
			this.m_reactionEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			this.m_zanimator.SetTrigger("shake");
			base.Invoke("Leave", (float)this.m_leaveDelay);
		}
	}

	// Token: 0x060017C9 RID: 6089 RVA: 0x000B101C File Offset: 0x000AF21C
	private void Leave()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_left)
		{
			return;
		}
		this.m_left = true;
		this.m_leaveEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		this.m_zanimator.SetTrigger("dive");
		this.m_nview.GetZDO().Set(ZDOVars.s_dead, true);
	}

	// Token: 0x060017CA RID: 6090 RVA: 0x000B10A4 File Offset: 0x000AF2A4
	private void OnDestroy()
	{
		if (this.m_left && this.m_nview.IsValid() && !this.m_nview.IsOwner() && Player.GetPlayersInRangeXZ(base.transform.position, 40f) == 0)
		{
			this.m_nview.Destroy();
		}
	}

	// Token: 0x04001793 RID: 6035
	public float m_waveScale = 0.5f;

	// Token: 0x04001794 RID: 6036
	public float m_floatOffset;

	// Token: 0x04001795 RID: 6037
	public float m_movementSpeed = 0.1f;

	// Token: 0x04001796 RID: 6038
	public float m_maxSpeed = 1f;

	// Token: 0x04001797 RID: 6039
	public MineRock m_mineRock;

	// Token: 0x04001798 RID: 6040
	public float m_hitReactionChance = 0.25f;

	// Token: 0x04001799 RID: 6041
	public int m_leaveDelay = 5;

	// Token: 0x0400179A RID: 6042
	public EffectList m_reactionEffects = new EffectList();

	// Token: 0x0400179B RID: 6043
	public EffectList m_leaveEffects = new EffectList();

	// Token: 0x0400179C RID: 6044
	public bool m_alignToWaterLevel = true;

	// Token: 0x0400179D RID: 6045
	private Rigidbody m_body;

	// Token: 0x0400179E RID: 6046
	private ZNetView m_nview;

	// Token: 0x0400179F RID: 6047
	private ZSyncAnimation m_zanimator;

	// Token: 0x040017A0 RID: 6048
	private Animator m_animator;

	// Token: 0x040017A1 RID: 6049
	private bool m_left;
}
