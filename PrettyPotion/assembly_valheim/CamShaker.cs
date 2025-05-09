using System;
using System.Collections;
using UnityEngine;

// Token: 0x0200004C RID: 76
public class CamShaker : MonoBehaviour
{
	// Token: 0x060005F3 RID: 1523 RVA: 0x000323A8 File Offset: 0x000305A8
	private void Start()
	{
		if (this.m_continous)
		{
			if (this.m_delay <= 0f)
			{
				base.StartCoroutine("TriggerContinous");
				return;
			}
			base.Invoke("DelayedTriggerContinous", this.m_delay);
			return;
		}
		else
		{
			if (this.m_delay <= 0f)
			{
				this.Trigger();
				return;
			}
			base.Invoke("Trigger", this.m_delay);
			return;
		}
	}

	// Token: 0x060005F4 RID: 1524 RVA: 0x0003240E File Offset: 0x0003060E
	private void DelayedTriggerContinous()
	{
		base.StartCoroutine("TriggerContinous");
	}

	// Token: 0x060005F5 RID: 1525 RVA: 0x0003241C File Offset: 0x0003061C
	private IEnumerator TriggerContinous()
	{
		float t = 0f;
		for (;;)
		{
			this.Trigger();
			t += Time.deltaTime;
			if (this.m_continousDuration > 0f && t > this.m_continousDuration)
			{
				break;
			}
			yield return null;
		}
		yield break;
		yield break;
	}

	// Token: 0x060005F6 RID: 1526 RVA: 0x0003242C File Offset: 0x0003062C
	private void Trigger()
	{
		if (GameCamera.instance)
		{
			if (this.m_localOnly)
			{
				ZNetView component = base.GetComponent<ZNetView>();
				if (component && component.IsValid() && !component.IsOwner())
				{
					return;
				}
			}
			GameCamera.instance.AddShake(base.transform.position, this.m_range, this.m_strength, this.m_continous);
		}
	}

	// Token: 0x040006A8 RID: 1704
	public float m_strength = 1f;

	// Token: 0x040006A9 RID: 1705
	public float m_range = 50f;

	// Token: 0x040006AA RID: 1706
	public float m_delay;

	// Token: 0x040006AB RID: 1707
	public bool m_continous;

	// Token: 0x040006AC RID: 1708
	public float m_continousDuration;

	// Token: 0x040006AD RID: 1709
	public bool m_localOnly;
}
