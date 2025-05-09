using System;
using UnityEngine;

// Token: 0x020001AD RID: 429
public class RandomSpeak : MonoBehaviour
{
	// Token: 0x06001906 RID: 6406 RVA: 0x000BB1E7 File Offset: 0x000B93E7
	private void Start()
	{
		base.InvokeRepeating("Speak", UnityEngine.Random.Range(0f, this.m_interval), this.m_interval);
	}

	// Token: 0x06001907 RID: 6407 RVA: 0x000BB20C File Offset: 0x000B940C
	private void Speak()
	{
		if (UnityEngine.Random.value > this.m_chance)
		{
			return;
		}
		if (this.m_texts.Length == 0)
		{
			return;
		}
		if (Player.m_localPlayer == null || Vector3.Distance(base.transform.position, Player.m_localPlayer.transform.position) > this.m_triggerDistance)
		{
			return;
		}
		if (this.m_onlyOnItemStand && !base.gameObject.GetComponentInParent<ItemStand>())
		{
			return;
		}
		float dayFraction = EnvMan.instance.GetDayFraction();
		if ((!this.m_invertTod && (dayFraction < this.m_minTOD || dayFraction > this.m_maxTOD)) || (this.m_invertTod && dayFraction > this.m_minTOD && dayFraction < this.m_maxTOD))
		{
			return;
		}
		this.m_speakEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		int num = this.m_indexFromDay ? (EnvMan.instance.GetDay() % this.m_texts.Length) : UnityEngine.Random.Range(0, this.m_texts.Length);
		string text = this.m_texts[num];
		Chat.instance.SetNpcText(base.gameObject, this.m_offset, this.m_cullDistance, this.m_ttl, this.m_topic, text, this.m_useLargeDialog);
		if (this.m_onlyOnce)
		{
			base.CancelInvoke("Speak");
		}
	}

	// Token: 0x0400195F RID: 6495
	public float m_interval = 5f;

	// Token: 0x04001960 RID: 6496
	public float m_chance = 0.5f;

	// Token: 0x04001961 RID: 6497
	public float m_triggerDistance = 5f;

	// Token: 0x04001962 RID: 6498
	public float m_cullDistance = 10f;

	// Token: 0x04001963 RID: 6499
	public float m_ttl = 10f;

	// Token: 0x04001964 RID: 6500
	public Vector3 m_offset = new Vector3(0f, 0f, 0f);

	// Token: 0x04001965 RID: 6501
	public EffectList m_speakEffects = new EffectList();

	// Token: 0x04001966 RID: 6502
	public bool m_useLargeDialog;

	// Token: 0x04001967 RID: 6503
	public bool m_onlyOnce;

	// Token: 0x04001968 RID: 6504
	public bool m_onlyOnItemStand;

	// Token: 0x04001969 RID: 6505
	public float m_minTOD;

	// Token: 0x0400196A RID: 6506
	public float m_maxTOD = 1f;

	// Token: 0x0400196B RID: 6507
	public bool m_invertTod;

	// Token: 0x0400196C RID: 6508
	public bool m_indexFromDay;

	// Token: 0x0400196D RID: 6509
	public string m_topic = "";

	// Token: 0x0400196E RID: 6510
	public string[] m_texts = new string[0];
}
