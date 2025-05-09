using System;
using TMPro;
using UnityEngine;

// Token: 0x0200006F RID: 111
public class CaptionItem : MonoBehaviour
{
	// Token: 0x06000719 RID: 1817 RVA: 0x0003AC98 File Offset: 0x00038E98
	public void Setup()
	{
		this.m_text = base.GetComponent<TextMeshProUGUI>();
		this.m_text.color = ClosedCaptions.Instance.GetCaptionColor(this.m_type);
		this.m_text.text = (this.m_captionText ?? "");
		MonoBehaviour.print(Localization.instance);
		this.Refresh();
	}

	// Token: 0x14000002 RID: 2
	// (add) Token: 0x0600071A RID: 1818 RVA: 0x0003ACF8 File Offset: 0x00038EF8
	// (remove) Token: 0x0600071B RID: 1819 RVA: 0x0003AD30 File Offset: 0x00038F30
	public event Action<CaptionItem> OnDestroyingCaption = delegate(CaptionItem <p0>)
	{
	};

	// Token: 0x0600071C RID: 1820 RVA: 0x0003AD65 File Offset: 0x00038F65
	private void OnDestroy()
	{
		Action<CaptionItem> onDestroyingCaption = this.OnDestroyingCaption;
		if (onDestroyingCaption == null)
		{
			return;
		}
		onDestroyingCaption(this);
	}

	// Token: 0x0600071D RID: 1821 RVA: 0x0003AD78 File Offset: 0x00038F78
	public void CustomUpdate(float dt)
	{
		this.m_timer -= dt;
		this.TimeSinceSpawn += dt;
		if (this.m_timer <= 0f)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		float a = Mathf.Clamp01(this.TimeSinceSpawn * 2f);
		float b = Mathf.Clamp01(this.m_timer * 4f);
		float num = Mathf.Min(a, b);
		Vector3 localScale = base.transform.localScale;
		localScale.y = num;
		localScale.x = 1f;
		localScale.z = 1f;
		base.transform.localScale = localScale;
		this.m_text.alpha = num;
	}

	// Token: 0x0600071E RID: 1822 RVA: 0x0003AE27 File Offset: 0x00039027
	public void Refresh()
	{
		if (this.m_dying)
		{
			return;
		}
		this.m_timer = ClosedCaptions.Instance.m_captionDuration;
	}

	// Token: 0x17000020 RID: 32
	// (get) Token: 0x0600071F RID: 1823 RVA: 0x0003AE42 File Offset: 0x00039042
	public bool Killed
	{
		get
		{
			return this.m_dying;
		}
	}

	// Token: 0x06000720 RID: 1824 RVA: 0x0003AE4A File Offset: 0x0003904A
	public void Kill()
	{
		this.m_dying = true;
		this.m_timer = Mathf.Min(this.m_timer, 0.5f);
	}

	// Token: 0x06000721 RID: 1825 RVA: 0x0003AE69 File Offset: 0x00039069
	public int GetImportance()
	{
		return (int)this.m_type;
	}

	// Token: 0x17000021 RID: 33
	// (get) Token: 0x06000722 RID: 1826 RVA: 0x0003AE71 File Offset: 0x00039071
	// (set) Token: 0x06000723 RID: 1827 RVA: 0x0003AE79 File Offset: 0x00039079
	public float TimeSinceSpawn { get; private set; }

	// Token: 0x04000844 RID: 2116
	public string m_captionText;

	// Token: 0x04000845 RID: 2117
	public ClosedCaptions.CaptionType m_type;

	// Token: 0x04000846 RID: 2118
	private TextMeshProUGUI m_text;

	// Token: 0x04000847 RID: 2119
	private float m_timer;

	// Token: 0x04000849 RID: 2121
	private bool m_dying;
}
