using System;
using UnityEngine;

// Token: 0x02000112 RID: 274
public class DepthCamera : MonoBehaviour
{
	// Token: 0x0600113B RID: 4411 RVA: 0x00080721 File Offset: 0x0007E921
	private void Start()
	{
		this.m_camera = base.GetComponent<Camera>();
		base.InvokeRepeating("RenderDepth", this.m_updateInterval, this.m_updateInterval);
	}

	// Token: 0x0600113C RID: 4412 RVA: 0x00080748 File Offset: 0x0007E948
	private void RenderDepth()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 vector = (Player.m_localPlayer ? Player.m_localPlayer.transform.position : mainCamera.transform.position) + Vector3.up * this.m_offset;
		vector.x = Mathf.Round(vector.x);
		vector.y = Mathf.Round(vector.y);
		vector.z = Mathf.Round(vector.z);
		base.transform.position = vector;
		float lodBias = QualitySettings.lodBias;
		QualitySettings.lodBias = 10f;
		this.m_camera.RenderWithShader(this.m_depthShader, "RenderType");
		QualitySettings.lodBias = lodBias;
		Shader.SetGlobalTexture("_SkyAlphaTexture", this.m_texture);
		Shader.SetGlobalVector("_SkyAlphaPosition", base.transform.position);
	}

	// Token: 0x04001065 RID: 4197
	public Shader m_depthShader;

	// Token: 0x04001066 RID: 4198
	public float m_offset = 50f;

	// Token: 0x04001067 RID: 4199
	public RenderTexture m_texture;

	// Token: 0x04001068 RID: 4200
	public float m_updateInterval = 1f;

	// Token: 0x04001069 RID: 4201
	private Camera m_camera;
}
