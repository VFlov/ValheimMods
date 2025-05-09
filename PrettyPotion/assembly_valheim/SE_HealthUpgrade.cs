using System;
using UnityEngine;

// Token: 0x02000038 RID: 56
public class SE_HealthUpgrade : StatusEffect
{
	// Token: 0x060004F1 RID: 1265 RVA: 0x0002BBCE File Offset: 0x00029DCE
	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	// Token: 0x060004F2 RID: 1266 RVA: 0x0002BBD8 File Offset: 0x00029DD8
	public override void Stop()
	{
		base.Stop();
		Player player = this.m_character as Player;
		if (!player)
		{
			return;
		}
		if (this.m_moreHealth > 0f)
		{
			player.SetMaxHealth(this.m_character.GetMaxHealth() + this.m_moreHealth, true);
			player.SetHealth(this.m_character.GetMaxHealth());
		}
		if (this.m_moreStamina > 0f)
		{
			player.SetMaxStamina(this.m_character.GetMaxStamina() + this.m_moreStamina, true);
		}
		this.m_upgradeEffect.Create(this.m_character.transform.position, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x04000588 RID: 1416
	[Header("Health")]
	public float m_moreHealth;

	// Token: 0x04000589 RID: 1417
	[Header("Stamina")]
	public float m_moreStamina;

	// Token: 0x0400058A RID: 1418
	public EffectList m_upgradeEffect = new EffectList();
}
