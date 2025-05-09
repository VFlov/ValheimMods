using System;
using UnityEngine;

// Token: 0x02000035 RID: 53
public class SE_Finder : StatusEffect
{
	// Token: 0x060004E6 RID: 1254 RVA: 0x0002B4A4 File Offset: 0x000296A4
	public override void UpdateStatusEffect(float dt)
	{
		this.m_updateBeaconTimer += dt;
		if (this.m_updateBeaconTimer > 1f)
		{
			this.m_updateBeaconTimer = 0f;
			Beacon beacon = Beacon.FindClosestBeaconInRange(this.m_character.transform.position);
			if (beacon != this.m_beacon)
			{
				this.m_beacon = beacon;
				if (this.m_beacon)
				{
					this.m_lastDistance = Utils.DistanceXZ(this.m_character.transform.position, this.m_beacon.transform.position);
					this.m_pingTimer = 0f;
				}
			}
		}
		if (this.m_beacon != null)
		{
			float num = Utils.DistanceXZ(this.m_character.transform.position, this.m_beacon.transform.position);
			float num2 = Mathf.Clamp01(num / this.m_beacon.m_range);
			float num3 = Mathf.Lerp(this.m_closeFrequency, this.m_distantFrequency, num2);
			this.m_pingTimer += dt;
			if (this.m_pingTimer > num3)
			{
				this.m_pingTimer = 0f;
				if (num2 < 0.2f)
				{
					this.m_pingEffectNear.Create(this.m_character.transform.position, this.m_character.transform.rotation, this.m_character.transform, 1f, -1);
				}
				else if (num2 < 0.6f)
				{
					this.m_pingEffectMed.Create(this.m_character.transform.position, this.m_character.transform.rotation, this.m_character.transform, 1f, -1);
				}
				else
				{
					this.m_pingEffectFar.Create(this.m_character.transform.position, this.m_character.transform.rotation, this.m_character.transform, 1f, -1);
				}
				this.m_lastDistance = num;
			}
		}
	}

	// Token: 0x0400056C RID: 1388
	[Header("SE_Finder")]
	public EffectList m_pingEffectNear = new EffectList();

	// Token: 0x0400056D RID: 1389
	public EffectList m_pingEffectMed = new EffectList();

	// Token: 0x0400056E RID: 1390
	public EffectList m_pingEffectFar = new EffectList();

	// Token: 0x0400056F RID: 1391
	public float m_closerTriggerDistance = 2f;

	// Token: 0x04000570 RID: 1392
	public float m_furtherTriggerDistance = 4f;

	// Token: 0x04000571 RID: 1393
	public float m_closeFrequency = 1f;

	// Token: 0x04000572 RID: 1394
	public float m_distantFrequency = 5f;

	// Token: 0x04000573 RID: 1395
	private float m_updateBeaconTimer;

	// Token: 0x04000574 RID: 1396
	private float m_pingTimer;

	// Token: 0x04000575 RID: 1397
	private Beacon m_beacon;

	// Token: 0x04000576 RID: 1398
	private float m_lastDistance;
}
