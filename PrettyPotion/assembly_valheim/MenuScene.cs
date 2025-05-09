using System;
using UnityEngine;

// Token: 0x02000196 RID: 406
[ExecuteInEditMode]
public class MenuScene : MonoBehaviour
{
	// Token: 0x06001822 RID: 6178 RVA: 0x000B40EF File Offset: 0x000B22EF
	private void Awake()
	{
		Shader.SetGlobalFloat("_Wet", 0f);
	}

	// Token: 0x06001823 RID: 6179 RVA: 0x000B4100 File Offset: 0x000B2300
	private void Update()
	{
		Shader.SetGlobalVector("_SkyboxSunDir", -this.m_dirLight.transform.forward);
		Shader.SetGlobalVector("_SunDir", -this.m_dirLight.transform.forward);
		Shader.SetGlobalColor("_SunFogColor", this.m_sunFogColor);
		Shader.SetGlobalColor("_SunColor", this.m_dirLight.color * this.m_dirLight.intensity);
		Shader.SetGlobalColor("_AmbientColor", RenderSettings.ambientLight);
		RenderSettings.fogColor = this.m_fogColor;
		RenderSettings.fogDensity = this.m_fogDensity;
		RenderSettings.ambientLight = this.m_ambientLightColor;
		Vector3 normalized = this.m_windDir.normalized;
		Shader.SetGlobalVector("_GlobalWindForce", normalized * this.m_windIntensity);
		Shader.SetGlobalVector("_GlobalWind1", new Vector4(normalized.x, normalized.y, normalized.z, this.m_windIntensity));
		Shader.SetGlobalVector("_GlobalWind2", Vector4.one);
		Shader.SetGlobalFloat("_GlobalWindAlpha", 0f);
	}

	// Token: 0x04001812 RID: 6162
	public Light m_dirLight;

	// Token: 0x04001813 RID: 6163
	public Color m_sunFogColor = Color.white;

	// Token: 0x04001814 RID: 6164
	public Color m_fogColor = Color.white;

	// Token: 0x04001815 RID: 6165
	public Color m_ambientLightColor = Color.white;

	// Token: 0x04001816 RID: 6166
	public float m_fogDensity = 1f;

	// Token: 0x04001817 RID: 6167
	public Vector3 m_windDir = Vector3.left;

	// Token: 0x04001818 RID: 6168
	public float m_windIntensity = 0.5f;
}
