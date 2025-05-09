using System;
using UnityEngine;

// Token: 0x02000033 RID: 51
public class SE_Cozy : SE_Stats
{
	// Token: 0x060004D9 RID: 1241 RVA: 0x0002AF70 File Offset: 0x00029170
	private void OnEnable()
	{
		if (!string.IsNullOrEmpty(this.m_statusEffect))
		{
			this.m_statusEffectHash = this.m_statusEffect.GetStableHashCode();
		}
	}

	// Token: 0x060004DA RID: 1242 RVA: 0x0002AF90 File Offset: 0x00029190
	public override void Setup(Character character)
	{
		base.Setup(character);
		this.m_character.Message(MessageHud.MessageType.Center, "$se_resting_start", 0, null);
	}

	// Token: 0x060004DB RID: 1243 RVA: 0x0002AFAC File Offset: 0x000291AC
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (this.m_time > this.m_delay)
		{
			this.m_character.GetSEMan().AddStatusEffect(this.m_statusEffectHash, true, 0, 0f);
		}
	}

	// Token: 0x060004DC RID: 1244 RVA: 0x0002AFE4 File Offset: 0x000291E4
	public override string GetIconText()
	{
		Player player = this.m_character as Player;
		return Localization.instance.Localize("$se_rested_comfort:" + player.GetComfortLevel().ToString());
	}

	// Token: 0x04000557 RID: 1367
	[Header("__SE_Cozy__")]
	public float m_delay = 10f;

	// Token: 0x04000558 RID: 1368
	public string m_statusEffect = "";

	// Token: 0x04000559 RID: 1369
	private int m_statusEffectHash;

	// Token: 0x0400055A RID: 1370
	private int m_comfortLevel;

	// Token: 0x0400055B RID: 1371
	private float m_updateTimer;
}
