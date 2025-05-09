using System;
using System.Collections;
using UnityEngine;

// Token: 0x0200015E RID: 350
public class BossStone : MonoBehaviour
{
	// Token: 0x0600154B RID: 5451 RVA: 0x0009C2FC File Offset: 0x0009A4FC
	private void Start()
	{
		if (this.m_mesh.materials[this.m_emissiveMaterialIndex].HasProperty("_EmissionColor"))
		{
			this.m_mesh.materials[this.m_emissiveMaterialIndex].SetColor("_EmissionColor", Color.black);
		}
		if (this.m_activeEffect)
		{
			this.m_activeEffect.SetActive(false);
		}
		this.SetActivated(this.m_itemStand.HaveAttachment(), false);
		base.InvokeRepeating("UpdateVisual", 1f, 1f);
	}

	// Token: 0x0600154C RID: 5452 RVA: 0x0009C388 File Offset: 0x0009A588
	private void UpdateVisual()
	{
		this.SetActivated(this.m_itemStand.HaveAttachment(), true);
	}

	// Token: 0x0600154D RID: 5453 RVA: 0x0009C39C File Offset: 0x0009A59C
	private void SetActivated(bool active, bool triggerEffect)
	{
		if (active == this.m_active)
		{
			return;
		}
		this.m_active = active;
		if (triggerEffect && active)
		{
			base.Invoke("DelayedAttachEffects_Step1", 1f);
			base.Invoke("DelayedAttachEffects_Step2", 5f);
			base.Invoke("DelayedAttachEffects_Step3", 11f);
			return;
		}
		if (this.m_activeEffect)
		{
			this.m_activeEffect.SetActive(active);
		}
		base.StopCoroutine("FadeEmission");
		base.StartCoroutine("FadeEmission");
	}

	// Token: 0x0600154E RID: 5454 RVA: 0x0009C420 File Offset: 0x0009A620
	private void DelayedAttachEffects_Step1()
	{
		this.m_activateStep1.Create(this.m_itemStand.transform.position, base.transform.rotation, null, 1f, -1);
	}

	// Token: 0x0600154F RID: 5455 RVA: 0x0009C450 File Offset: 0x0009A650
	private void DelayedAttachEffects_Step2()
	{
		this.m_activateStep2.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
	}

	// Token: 0x06001550 RID: 5456 RVA: 0x0009C47C File Offset: 0x0009A67C
	private void DelayedAttachEffects_Step3()
	{
		if (this.m_activeEffect)
		{
			this.m_activeEffect.SetActive(true);
		}
		this.m_activateStep3.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		base.StopCoroutine("FadeEmission");
		base.StartCoroutine("FadeEmission");
		Player.MessageAllInRange(base.transform.position, 20f, MessageHud.MessageType.Center, this.m_completedMessage, null);
	}

	// Token: 0x06001551 RID: 5457 RVA: 0x0009C4FF File Offset: 0x0009A6FF
	private IEnumerator FadeEmission()
	{
		if (this.m_mesh && this.m_mesh.materials[this.m_emissiveMaterialIndex].HasProperty("_EmissionColor"))
		{
			Color startColor = this.m_mesh.materials[this.m_emissiveMaterialIndex].GetColor("_EmissionColor");
			Color targetColor = this.m_active ? this.m_activeEmissiveColor : Color.black;
			for (float t = 0f; t < 1f; t += Time.deltaTime)
			{
				Color value = Color.Lerp(startColor, targetColor, t / 1f);
				this.m_mesh.materials[this.m_emissiveMaterialIndex].SetColor("_EmissionColor", value);
				yield return null;
			}
			startColor = default(Color);
			targetColor = default(Color);
		}
		ZLog.Log("Done fading color");
		yield break;
	}

	// Token: 0x06001552 RID: 5458 RVA: 0x0009C50E File Offset: 0x0009A70E
	public bool IsActivated()
	{
		return this.m_active;
	}

	// Token: 0x040014BB RID: 5307
	public ItemStand m_itemStand;

	// Token: 0x040014BC RID: 5308
	public GameObject m_activeEffect;

	// Token: 0x040014BD RID: 5309
	public EffectList m_activateStep1 = new EffectList();

	// Token: 0x040014BE RID: 5310
	public EffectList m_activateStep2 = new EffectList();

	// Token: 0x040014BF RID: 5311
	public EffectList m_activateStep3 = new EffectList();

	// Token: 0x040014C0 RID: 5312
	public string m_completedMessage = "";

	// Token: 0x040014C1 RID: 5313
	public MeshRenderer m_mesh;

	// Token: 0x040014C2 RID: 5314
	public int m_emissiveMaterialIndex;

	// Token: 0x040014C3 RID: 5315
	public Color m_activeEmissiveColor = Color.white;

	// Token: 0x040014C4 RID: 5316
	private bool m_active;

	// Token: 0x040014C5 RID: 5317
	private ZNetView m_nview;
}
