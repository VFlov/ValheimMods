using System;
using UnityEngine;

// Token: 0x0200003D RID: 61
public class SE_Smoke : StatusEffect
{
	// Token: 0x0600050A RID: 1290 RVA: 0x0002C352 File Offset: 0x0002A552
	public override bool CanAdd(Character character)
	{
		return !character.m_tolerateSmoke && base.CanAdd(character);
	}

	// Token: 0x0600050B RID: 1291 RVA: 0x0002C368 File Offset: 0x0002A568
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		this.m_timer += dt;
		if (this.m_timer > this.m_damageInterval)
		{
			this.m_timer = 0f;
			HitData hitData = new HitData();
			hitData.m_point = this.m_character.GetCenterPoint();
			hitData.m_damage = this.m_damage;
			hitData.m_hitType = HitData.HitType.Smoke;
			this.m_character.ApplyDamage(hitData, true, false, HitData.DamageModifier.Normal);
		}
	}

	// Token: 0x040005A4 RID: 1444
	[Header("SE_Burning")]
	public HitData.DamageTypes m_damage;

	// Token: 0x040005A5 RID: 1445
	public float m_damageInterval = 1f;

	// Token: 0x040005A6 RID: 1446
	private float m_timer;
}
