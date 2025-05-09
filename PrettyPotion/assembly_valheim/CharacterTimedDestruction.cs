using System;
using UnityEngine;

// Token: 0x02000014 RID: 20
public class CharacterTimedDestruction : MonoBehaviour
{
	// Token: 0x06000247 RID: 583 RVA: 0x00014B89 File Offset: 0x00012D89
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_triggerOnAwake)
		{
			this.Trigger();
		}
	}

	// Token: 0x06000248 RID: 584 RVA: 0x00014BA5 File Offset: 0x00012DA5
	public void Trigger()
	{
		base.InvokeRepeating("DestroyNow", UnityEngine.Random.Range(this.m_timeoutMin, this.m_timeoutMax), 1f);
	}

	// Token: 0x06000249 RID: 585 RVA: 0x00014BC8 File Offset: 0x00012DC8
	public void Trigger(float timeout)
	{
		base.InvokeRepeating("DestroyNow", timeout, 1f);
	}

	// Token: 0x0600024A RID: 586 RVA: 0x00014BDC File Offset: 0x00012DDC
	private void DestroyNow()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		Character component = base.GetComponent<Character>();
		HitData hitData = new HitData();
		hitData.m_damage.m_damage = 99999f;
		hitData.m_point = base.transform.position;
		component.ApplyDamage(hitData, false, true, HitData.DamageModifier.Normal);
	}

	// Token: 0x0400032D RID: 813
	public float m_timeoutMin = 1f;

	// Token: 0x0400032E RID: 814
	public float m_timeoutMax = 1f;

	// Token: 0x0400032F RID: 815
	public bool m_triggerOnAwake;

	// Token: 0x04000330 RID: 816
	private ZNetView m_nview;

	// Token: 0x04000331 RID: 817
	private Character m_character;
}
