using System;
using UnityEngine;

// Token: 0x02000055 RID: 85
[ExecuteAlways]
public class HeatDistortImageEffect : MonoBehaviour
{
	// Token: 0x0600061B RID: 1563 RVA: 0x00033910 File Offset: 0x00031B10
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!this.m_initalized)
		{
			this.m_initalized = true;
			this.m_material = new Material(Shader.Find("Hidden/CameraHeatDistort"));
		}
		this.m_material.SetFloat(HeatDistortImageEffect.s_intensity, this.m_intensity);
		this.m_material.SetFloat(HeatDistortImageEffect.s_vignetteSmoothness, this.m_vignetteSmoothness);
		this.m_material.SetFloat(HeatDistortImageEffect.s_vignetteStrength, this.m_vignetteStrength);
		this.m_material.SetTexture(HeatDistortImageEffect.s_noiseTexture, this.m_noiseTexture);
		this.m_material.SetFloat(HeatDistortImageEffect.s_distortionStrength, this.m_distortionStrength);
		this.m_material.SetInt(HeatDistortImageEffect.s_segments, this.m_segments);
		this.m_material.SetFloat(HeatDistortImageEffect.s_stretch, this.m_stretch);
		this.m_material.SetFloat(HeatDistortImageEffect.s_scrollspeed, this.m_scrollSpeed);
		this.m_material.SetColor(HeatDistortImageEffect.s_color, this.m_color);
		Graphics.Blit(source, destination, this.m_material);
	}

	// Token: 0x040006F3 RID: 1779
	private static readonly int s_intensity = Shader.PropertyToID("_Intensity");

	// Token: 0x040006F4 RID: 1780
	private static readonly int s_vignetteStrength = Shader.PropertyToID("_VignetteStrength");

	// Token: 0x040006F5 RID: 1781
	private static readonly int s_vignetteSmoothness = Shader.PropertyToID("_VignetteSmoothness");

	// Token: 0x040006F6 RID: 1782
	private static readonly int s_noiseTexture = Shader.PropertyToID("_NoiseTexture");

	// Token: 0x040006F7 RID: 1783
	private static readonly int s_distortionStrength = Shader.PropertyToID("_DistortionStrength");

	// Token: 0x040006F8 RID: 1784
	private static readonly int s_segments = Shader.PropertyToID("_Segments");

	// Token: 0x040006F9 RID: 1785
	private static readonly int s_stretch = Shader.PropertyToID("_Stretch");

	// Token: 0x040006FA RID: 1786
	private static readonly int s_scrollspeed = Shader.PropertyToID("_ScrollSpeed");

	// Token: 0x040006FB RID: 1787
	private static readonly int s_color = Shader.PropertyToID("_Color");

	// Token: 0x040006FC RID: 1788
	private Material m_material;

	// Token: 0x040006FD RID: 1789
	private bool m_initalized;

	// Token: 0x040006FE RID: 1790
	[SerializeField]
	private Color m_color;

	// Token: 0x040006FF RID: 1791
	[SerializeField]
	[Range(0f, 1f)]
	public float m_intensity;

	// Token: 0x04000700 RID: 1792
	[SerializeField]
	[Range(-0.25f, 0.25f)]
	private float m_distortionStrength = 0.15f;

	// Token: 0x04000701 RID: 1793
	[SerializeField]
	[Range(0f, 15f)]
	private float m_vignetteStrength = 1f;

	// Token: 0x04000702 RID: 1794
	[SerializeField]
	[Range(0f, 1f)]
	private float m_vignetteSmoothness = 0.5f;

	// Token: 0x04000703 RID: 1795
	[SerializeField]
	private Texture2D m_noiseTexture;

	// Token: 0x04000704 RID: 1796
	[SerializeField]
	[Range(1f, 8f)]
	private int m_segments = 3;

	// Token: 0x04000705 RID: 1797
	[SerializeField]
	[Range(0f, 8f)]
	private float m_stretch = 1f;

	// Token: 0x04000706 RID: 1798
	[SerializeField]
	[Range(-1f, 1f)]
	private float m_scrollSpeed;
}
