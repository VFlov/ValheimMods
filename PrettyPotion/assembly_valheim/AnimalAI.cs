using System;
using UnityEngine;

// Token: 0x0200000B RID: 11
public class AnimalAI : BaseAI
{
	// Token: 0x06000072 RID: 114 RVA: 0x0000836F File Offset: 0x0000656F
	protected override void Awake()
	{
		base.Awake();
		this.m_updateTargetTimer = UnityEngine.Random.Range(0f, 2f);
	}

	// Token: 0x06000073 RID: 115 RVA: 0x0000838C File Offset: 0x0000658C
	protected override void OnDamaged(float damage, Character attacker)
	{
		base.OnDamaged(damage, attacker);
		this.SetAlerted(true);
	}

	// Token: 0x06000074 RID: 116 RVA: 0x000083A0 File Offset: 0x000065A0
	public override bool UpdateAI(float dt)
	{
		if (!base.UpdateAI(dt))
		{
			return false;
		}
		if (this.m_afraidOfFire && base.AvoidFire(dt, null, true))
		{
			return true;
		}
		this.m_updateTargetTimer -= dt;
		if (this.m_updateTargetTimer <= 0f)
		{
			this.m_updateTargetTimer = (Character.IsCharacterInRange(base.transform.position, 32f) ? 2f : 10f);
			Character character = base.FindEnemy();
			if (character)
			{
				this.m_target = character;
			}
		}
		if (this.m_target && this.m_target.IsDead())
		{
			this.m_target = null;
		}
		if (this.m_target)
		{
			bool flag = base.CanSenseTarget(this.m_target);
			base.SetTargetInfo(this.m_target.GetZDOID());
			if (flag)
			{
				this.SetAlerted(true);
			}
		}
		else
		{
			base.SetTargetInfo(ZDOID.None);
		}
		if (base.IsAlerted())
		{
			this.m_inDangerTimer += dt;
			if (this.m_inDangerTimer > this.m_timeToSafe)
			{
				this.m_target = null;
				this.SetAlerted(false);
			}
		}
		if (this.m_target)
		{
			base.Flee(dt, this.m_target.transform.position);
			this.m_target.OnTargeted(false, false);
		}
		else
		{
			base.IdleMovement(dt);
		}
		return true;
	}

	// Token: 0x06000075 RID: 117 RVA: 0x000084F6 File Offset: 0x000066F6
	protected override void SetAlerted(bool alert)
	{
		if (alert)
		{
			this.m_inDangerTimer = 0f;
		}
		base.SetAlerted(alert);
	}

	// Token: 0x04000157 RID: 343
	private const float m_updateTargetFarRange = 32f;

	// Token: 0x04000158 RID: 344
	private const float m_updateTargetIntervalNear = 2f;

	// Token: 0x04000159 RID: 345
	private const float m_updateTargetIntervalFar = 10f;

	// Token: 0x0400015A RID: 346
	public float m_timeToSafe = 4f;

	// Token: 0x0400015B RID: 347
	private Character m_target;

	// Token: 0x0400015C RID: 348
	private float m_inDangerTimer;

	// Token: 0x0400015D RID: 349
	private float m_updateTargetTimer;
}
