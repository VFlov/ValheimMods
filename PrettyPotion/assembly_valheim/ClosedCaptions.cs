using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000070 RID: 112
public class ClosedCaptions : MonoBehaviour
{
	// Token: 0x06000725 RID: 1829 RVA: 0x0003AEB0 File Offset: 0x000390B0
	private void Awake()
	{
		if (ClosedCaptions.m_instance == null)
		{
			ClosedCaptions.m_instance = this;
			ClosedCaptions.Valid = true;
			foreach (object obj in base.transform)
			{
				UnityEngine.Object.Destroy(((Transform)obj).gameObject);
			}
			this.m_image = base.GetComponent<Image>();
			this.m_bgAlpha = this.m_image.color.a;
			Color color = this.m_image.color;
			color.a = 0f;
			this.m_image.color = color;
			return;
		}
		UnityEngine.Object.DestroyImmediate(this);
	}

	// Token: 0x06000726 RID: 1830 RVA: 0x0003AF74 File Offset: 0x00039174
	private void Update()
	{
		foreach (CaptionItem captionItem in this.m_captionItems)
		{
			captionItem.CustomUpdate(Time.deltaTime);
		}
		Color color = this.m_image.color;
		float b = (this.m_captionItems.Count == 0) ? 0f : this.m_bgAlpha;
		color.a = Mathf.Lerp(color.a, b, Time.deltaTime / 0.25f);
		this.m_image.color = color;
	}

	// Token: 0x06000727 RID: 1831 RVA: 0x0003B01C File Offset: 0x0003921C
	public void RegisterCaption(ZSFX sfx, ClosedCaptions.CaptionType type = ClosedCaptions.CaptionType.Default)
	{
	}

	// Token: 0x06000728 RID: 1832 RVA: 0x0003B02C File Offset: 0x0003922C
	private string GetCaptionText(ZSFX sfx)
	{
		string text = Localization.instance.Localize(sfx.m_closedCaptionToken);
		if (sfx.m_secondaryCaptionToken.Length > 0)
		{
			text = text + " " + Localization.instance.Localize(sfx.m_secondaryCaptionToken);
		}
		return text;
	}

	// Token: 0x06000729 RID: 1833 RVA: 0x0003B075 File Offset: 0x00039275
	private void RemoveCaption(CaptionItem cc)
	{
		this.m_captionItems.Remove(cc);
	}

	// Token: 0x0600072A RID: 1834 RVA: 0x0003B084 File Offset: 0x00039284
	public Color GetCaptionColor(ClosedCaptions.CaptionType type)
	{
		switch (type)
		{
		default:
			return ClosedCaptions.Instance.m_defaultColor;
		case ClosedCaptions.CaptionType.Wildlife:
			return ClosedCaptions.Instance.m_wildlifeColor;
		case ClosedCaptions.CaptionType.Enemy:
			return ClosedCaptions.Instance.m_enemyColor;
		case ClosedCaptions.CaptionType.Boss:
			return ClosedCaptions.Instance.m_bossColor;
		}
	}

	// Token: 0x17000022 RID: 34
	// (get) Token: 0x0600072B RID: 1835 RVA: 0x0003B0D2 File Offset: 0x000392D2
	public static ClosedCaptions Instance
	{
		get
		{
			return ClosedCaptions.m_instance;
		}
	}

	// Token: 0x17000023 RID: 35
	// (get) Token: 0x0600072C RID: 1836 RVA: 0x0003B0D9 File Offset: 0x000392D9
	// (set) Token: 0x0600072D RID: 1837 RVA: 0x0003B0E0 File Offset: 0x000392E0
	public static bool Valid { get; private set; }

	// Token: 0x0400084A RID: 2122
	private static ClosedCaptions m_instance;

	// Token: 0x0400084C RID: 2124
	public float m_captionDuration = 5f;

	// Token: 0x0400084D RID: 2125
	public int m_maxCaptionLines = 4;

	// Token: 0x0400084E RID: 2126
	public GameObject m_captionPrefab;

	// Token: 0x0400084F RID: 2127
	[Header("Directional Indicators")]
	public float m_maxFuzziness = 15f;

	// Token: 0x04000850 RID: 2128
	public float m_maxFuzzinessDistance = 50f;

	// Token: 0x04000851 RID: 2129
	public AnimationCurve m_fuzzCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	// Token: 0x04000852 RID: 2130
	public GameObject m_indicatorContainer;

	// Token: 0x04000853 RID: 2131
	public GameObject m_directionalIndicatorPrefab;

	// Token: 0x04000854 RID: 2132
	[Header("Type Colors")]
	[ColorUsage(false)]
	public Color m_defaultColor = Color.white;

	// Token: 0x04000855 RID: 2133
	[ColorUsage(false)]
	public Color m_wildlifeColor = new Color(0.78f, 0.43f, 0.65f);

	// Token: 0x04000856 RID: 2134
	[ColorUsage(false)]
	public Color m_enemyColor = new Color(0.8f, 0.24f, 0.04f);

	// Token: 0x04000857 RID: 2135
	[ColorUsage(false)]
	public Color m_bossColor = new Color(0.34f, 0.24f, 0.62f);

	// Token: 0x04000858 RID: 2136
	private List<CaptionItem> m_captionItems = new List<CaptionItem>();

	// Token: 0x04000859 RID: 2137
	private List<CaptionItem> m_lowestImportance = new List<CaptionItem>();

	// Token: 0x0400085A RID: 2138
	private Image m_image;

	// Token: 0x0400085B RID: 2139
	private float m_bgAlpha;

	// Token: 0x0200026D RID: 621
	public enum CaptionType
	{
		// Token: 0x040020DA RID: 8410
		[InspectorName("Misc.")]
		Default,
		// Token: 0x040020DB RID: 8411
		[InspectorName("Wildlife, Enemy Idles")]
		Wildlife,
		// Token: 0x040020DC RID: 8412
		Enemy,
		// Token: 0x040020DD RID: 8413
		Boss
	}
}
