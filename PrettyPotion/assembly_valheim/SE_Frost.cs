using System;
using UnityEngine;

// Token: 0x02000036 RID: 54
public class SE_Frost : StatusEffect
{
	// Token: 0x060004E8 RID: 1256 RVA: 0x0002B6FC File Offset: 0x000298FC
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
	}

	// Token: 0x060004E9 RID: 1257 RVA: 0x0002B708 File Offset: 0x00029908
	public void AddDamage(float damage)
	{
		float num = this.m_character.IsPlayer() ? this.m_freezeTimePlayer : this.m_freezeTimeEnemy;
		float num2 = Mathf.Clamp01(damage / this.m_character.GetMaxHealth()) * num;
		float num3 = this.m_ttl - this.m_time;
		if (num2 > num3)
		{
			this.m_ttl = num2;
			this.ResetTime();
			base.TriggerStartEffects();
		}
	}

	// Token: 0x060004EA RID: 1258 RVA: 0x0002B76C File Offset: 0x0002996C
	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
	{
		HitData.DamageModifiers damageModifiers = character.GetDamageModifiers(null);
		if (damageModifiers.m_frost == HitData.DamageModifier.Resistant || damageModifiers.m_frost == HitData.DamageModifier.VeryResistant || damageModifiers.m_frost == HitData.DamageModifier.Immune)
		{
			return;
		}
		float num = Mathf.Clamp01(this.m_time / this.m_ttl);
		num = Mathf.Pow(num, 2f);
		speed -= baseSpeed * Mathf.Lerp(1f - this.m_minSpeedFactor, 0f, num);
		if (speed < 0f)
		{
			speed = 0f;
		}
	}

	// Token: 0x04000577 RID: 1399
	[Header("SE_Frost")]
	public float m_freezeTimeEnemy = 10f;

	// Token: 0x04000578 RID: 1400
	public float m_freezeTimePlayer = 10f;

	// Token: 0x04000579 RID: 1401
	public float m_minSpeedFactor = 0.1f;
}
