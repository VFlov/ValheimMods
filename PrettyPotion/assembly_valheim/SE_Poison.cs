using System;
using UnityEngine;

// Token: 0x02000039 RID: 57
public class SE_Poison : StatusEffect
{
	// Token: 0x060004F4 RID: 1268 RVA: 0x0002BC98 File Offset: 0x00029E98
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		this.m_timer -= dt;
		if (this.m_timer <= 0f)
		{
			this.m_timer = this.m_damageInterval;
			HitData hitData = new HitData();
			hitData.m_point = this.m_character.GetCenterPoint();
			hitData.m_damage.m_poison = this.m_damagePerHit;
			hitData.m_hitType = HitData.HitType.Poisoned;
			this.m_damageLeft -= this.m_damagePerHit;
			this.m_character.ApplyDamage(hitData, true, false, HitData.DamageModifier.Normal);
		}
	}

	// Token: 0x060004F5 RID: 1269 RVA: 0x0002BD24 File Offset: 0x00029F24
	public void AddDamage(float damage)
	{
		if (damage >= this.m_damageLeft)
		{
			this.m_damageLeft = damage;
			float num = this.m_character.IsPlayer() ? this.m_TTLPerDamagePlayer : this.m_TTLPerDamage;
			this.m_ttl = this.m_baseTTL + Mathf.Pow(this.m_damageLeft * num, this.m_TTLPower);
			int num2 = (int)(this.m_ttl / this.m_damageInterval);
			this.m_damagePerHit = this.m_damageLeft / (float)num2;
			ZLog.Log(string.Concat(new string[]
			{
				"Poison damage: ",
				this.m_damageLeft.ToString(),
				" ttl:",
				this.m_ttl.ToString(),
				" hits:",
				num2.ToString(),
				" dmg perhit:",
				this.m_damagePerHit.ToString()
			}));
			this.ResetTime();
		}
	}

	// Token: 0x0400058B RID: 1419
	[Header("SE_Poison")]
	public float m_damageInterval = 1f;

	// Token: 0x0400058C RID: 1420
	public float m_baseTTL = 2f;

	// Token: 0x0400058D RID: 1421
	public float m_TTLPerDamagePlayer = 2f;

	// Token: 0x0400058E RID: 1422
	public float m_TTLPerDamage = 2f;

	// Token: 0x0400058F RID: 1423
	public float m_TTLPower = 0.5f;

	// Token: 0x04000590 RID: 1424
	private float m_timer;

	// Token: 0x04000591 RID: 1425
	private float m_damageLeft;

	// Token: 0x04000592 RID: 1426
	private float m_damagePerHit;
}
