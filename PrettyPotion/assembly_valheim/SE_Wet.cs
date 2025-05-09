using System;
using UnityEngine;

// Token: 0x02000040 RID: 64
public class SE_Wet : SE_Stats
{
	// Token: 0x0600052A RID: 1322 RVA: 0x0002D5BC File Offset: 0x0002B7BC
	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	// Token: 0x0600052B RID: 1323 RVA: 0x0002D5C8 File Offset: 0x0002B7C8
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (!this.m_character.m_tolerateWater)
		{
			this.m_timer += dt;
			if (this.m_timer > this.m_damageInterval)
			{
				this.m_timer = 0f;
				HitData hitData = new HitData();
				hitData.m_point = this.m_character.transform.position;
				hitData.m_damage.m_damage = this.m_waterDamage;
				hitData.m_hitType = HitData.HitType.Water;
				this.m_character.Damage(hitData);
			}
		}
		if (this.m_character.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectCampFire))
		{
			this.m_time += dt * 10f;
		}
		if (this.m_character.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectBurning))
		{
			this.m_time += dt * 50f;
		}
	}

	// Token: 0x040005E1 RID: 1505
	[Header("__SE_Wet__")]
	public float m_waterDamage;

	// Token: 0x040005E2 RID: 1506
	public float m_damageInterval = 0.5f;

	// Token: 0x040005E3 RID: 1507
	private float m_timer;
}
