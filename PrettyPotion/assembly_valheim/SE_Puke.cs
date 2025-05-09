using System;
using UnityEngine;

// Token: 0x0200003A RID: 58
public class SE_Puke : SE_Stats
{
	// Token: 0x060004F7 RID: 1271 RVA: 0x0002BE47 File Offset: 0x0002A047
	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	// Token: 0x060004F8 RID: 1272 RVA: 0x0002BE50 File Offset: 0x0002A050
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		this.m_removeTimer += dt;
		if (this.m_removeTimer > this.m_removeInterval)
		{
			this.m_removeTimer = 0f;
			if ((this.m_character as Player).RemoveOneFood())
			{
				Hud.instance.DamageFlash();
			}
		}
	}

	// Token: 0x04000593 RID: 1427
	[Header("__SE_Puke__")]
	public float m_removeInterval = 1f;

	// Token: 0x04000594 RID: 1428
	private float m_removeTimer;
}
