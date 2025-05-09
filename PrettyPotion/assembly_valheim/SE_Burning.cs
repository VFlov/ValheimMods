using System;
using UnityEngine;

// Token: 0x02000032 RID: 50
public class SE_Burning : StatusEffect
{
	// Token: 0x060004D4 RID: 1236 RVA: 0x0002AD5D File Offset: 0x00028F5D
	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	// Token: 0x060004D5 RID: 1237 RVA: 0x0002AD68 File Offset: 0x00028F68
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (this.m_fireDamageLeft > 0f && this.m_character.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectWet))
		{
			this.m_time += dt * 5f;
		}
		this.m_timer -= dt;
		if (this.m_timer <= 0f)
		{
			this.m_timer = this.m_damageInterval;
			HitData hitData = new HitData();
			hitData.m_point = this.m_character.GetCenterPoint();
			hitData.m_damage.m_fire = this.m_fireDamagePerHit;
			hitData.m_damage.m_spirit = this.m_spiritDamagePerHit;
			hitData.m_hitType = HitData.HitType.Burning;
			this.m_fireDamageLeft = Mathf.Max(0f, this.m_fireDamageLeft - this.m_fireDamagePerHit);
			this.m_spiritDamageLeft = Mathf.Max(0f, this.m_spiritDamageLeft - this.m_spiritDamagePerHit);
			this.m_character.ApplyDamage(hitData, true, false, HitData.DamageModifier.Normal);
			this.m_tickEffect.Create(this.m_character.transform.position, this.m_character.transform.rotation, null, 1f, -1);
		}
	}

	// Token: 0x060004D6 RID: 1238 RVA: 0x0002AE9C File Offset: 0x0002909C
	public bool AddFireDamage(float damage)
	{
		int num = (int)(this.m_ttl / this.m_damageInterval);
		if (damage / (float)num < 0.2f && this.m_fireDamageLeft == 0f)
		{
			return false;
		}
		this.m_fireDamageLeft += damage;
		this.m_fireDamagePerHit = this.m_fireDamageLeft / (float)num;
		this.ResetTime();
		return true;
	}

	// Token: 0x060004D7 RID: 1239 RVA: 0x0002AEF8 File Offset: 0x000290F8
	public bool AddSpiritDamage(float damage)
	{
		int num = (int)(this.m_ttl / this.m_damageInterval);
		if (damage / (float)num < 0.2f && this.m_spiritDamageLeft == 0f)
		{
			return false;
		}
		this.m_spiritDamageLeft += damage;
		this.m_spiritDamagePerHit = this.m_spiritDamageLeft / (float)num;
		this.ResetTime();
		return true;
	}

	// Token: 0x0400054F RID: 1359
	[Header("SE_Burning")]
	public float m_damageInterval = 1f;

	// Token: 0x04000550 RID: 1360
	private float m_timer;

	// Token: 0x04000551 RID: 1361
	private float m_fireDamageLeft;

	// Token: 0x04000552 RID: 1362
	private float m_fireDamagePerHit;

	// Token: 0x04000553 RID: 1363
	private float m_spiritDamageLeft;

	// Token: 0x04000554 RID: 1364
	private float m_spiritDamagePerHit;

	// Token: 0x04000555 RID: 1365
	public EffectList m_tickEffect = new EffectList();

	// Token: 0x04000556 RID: 1366
	private const float m_minimumDamageTick = 0.2f;
}
