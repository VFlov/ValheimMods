using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Token: 0x02000151 RID: 337
[RequireComponent(typeof(Camera))]
public class VirtualFrameBuffer : MonoBehaviour
{
	// Token: 0x06001462 RID: 5218 RVA: 0x00095607 File Offset: 0x00093807
	public void Subscribe(VirtualFrameBufferScaler subscriber)
	{
		this.m_subscribers.Add(subscriber);
		if (this.m_renderTexture != null)
		{
			subscriber.OnBufferCreated(this, this.m_renderTexture);
		}
		this.UpdateCameraTarget();
	}

	// Token: 0x06001463 RID: 5219 RVA: 0x00095636 File Offset: 0x00093836
	public void Unsubscribe(VirtualFrameBufferScaler subscriber)
	{
		this.m_subscribers.Remove(subscriber);
		if (this.m_renderTexture != null)
		{
			subscriber.OnBufferDestroyed(this);
		}
		this.UpdateCameraTarget();
	}

	// Token: 0x06001464 RID: 5220 RVA: 0x00095660 File Offset: 0x00093860
	private void Update()
	{
		this.UpdateCameraTarget();
	}

	// Token: 0x06001465 RID: 5221 RVA: 0x00095668 File Offset: 0x00093868
	private void CreateClearCamera()
	{
		this.m_clearCamera = new GameObject
		{
			transform = 
			{
				parent = base.transform
			}
		}.AddComponent<Camera>();
		this.m_clearCamera.cullingMask = 0;
		this.m_clearCamera.allowHDR = false;
		this.m_clearCamera.allowMSAA = false;
		this.m_clearCamera.renderingPath = RenderingPath.Forward;
		this.m_clearCamera.clearFlags = CameraClearFlags.Color;
		this.m_clearCamera.backgroundColor = Color.black;
	}

	// Token: 0x06001466 RID: 5222 RVA: 0x000956E4 File Offset: 0x000938E4
	private void DestroyClearCameraIfExists()
	{
		if (this.m_clearCamera == null)
		{
			return;
		}
		UnityEngine.Object.Destroy(this.m_clearCamera.gameObject);
	}

	// Token: 0x06001467 RID: 5223 RVA: 0x00095705 File Offset: 0x00093905
	private void UpdateCurrentRenderScale()
	{
		if (!VirtualFrameBuffer.m_autoRenderScale)
		{
			return;
		}
		VirtualFrameBuffer.m_global3DRenderScale = Mathf.Clamp01(96f / Screen.dpi);
	}

	// Token: 0x06001468 RID: 5224 RVA: 0x00095724 File Offset: 0x00093924
	private static Resolution GetHighestSupportedResolution()
	{
		Resolution[] resolutions = Screen.resolutions;
		int num = 0;
		int num2 = resolutions[num].width * resolutions[num].height;
		for (int i = 1; i < resolutions.Length; i++)
		{
			int num3 = resolutions[i].width * resolutions[i].height;
			if (num3 > num2)
			{
				num = i;
				num2 = num3;
			}
		}
		return resolutions[num];
	}

	// Token: 0x06001469 RID: 5225 RVA: 0x00095790 File Offset: 0x00093990
	private void UpdateCameraTarget()
	{
		if (this.m_camera == null)
		{
			this.m_camera = base.GetComponent<Camera>();
		}
		this.UpdateCurrentRenderScale();
		bool flag = VirtualFrameBuffer.m_global3DRenderScale < 1f && this.m_subscribers.Count > 0;
		if (flag)
		{
			if (!this.m_isUsingScaledRendering)
			{
				this.CreateClearCamera();
			}
			this.ReassignTextureIfNeeded();
		}
		else if (this.m_isUsingScaledRendering)
		{
			this.ReleaseTextureIfExists();
			this.DestroyClearCameraIfExists();
		}
		this.m_isUsingScaledRendering = flag;
	}

	// Token: 0x0600146A RID: 5226 RVA: 0x00095810 File Offset: 0x00093A10
	private void ReassignTextureIfNeeded()
	{
		Vector2Int vector2Int = new Vector2Int(Mathf.RoundToInt((float)Screen.width * VirtualFrameBuffer.m_global3DRenderScale), Mathf.RoundToInt((float)Screen.height * VirtualFrameBuffer.m_global3DRenderScale));
		if (vector2Int.x < 8 || vector2Int.y < 8)
		{
			if (vector2Int.y < vector2Int.x)
			{
				vector2Int = new Vector2Int(Mathf.RoundToInt(8f * ((float)Screen.width / (float)Screen.height)), 8);
			}
			else
			{
				vector2Int = new Vector2Int(8, Mathf.RoundToInt(8f * ((float)Screen.height / (float)Screen.width)));
			}
		}
		if (this.m_renderTexture == null || vector2Int != new Vector2Int(this.m_renderTexture.width, this.m_renderTexture.height))
		{
			this.RecreateAndAssignRenderTexture(vector2Int);
		}
	}

	// Token: 0x0600146B RID: 5227 RVA: 0x000958E4 File Offset: 0x00093AE4
	private void RecreateAndAssignRenderTexture(Vector2Int viewportResolution)
	{
		this.ReleaseTextureIfExists();
		this.m_renderTexture = new RenderTexture(viewportResolution.x, viewportResolution.y, 24, DefaultFormat.HDR);
		this.m_renderTexture.Create();
		this.m_camera.targetTexture = this.m_renderTexture;
		for (int i = 0; i < this.m_subscribers.Count; i++)
		{
			this.m_subscribers[i].OnBufferCreated(this, this.m_renderTexture);
		}
	}

	// Token: 0x0600146C RID: 5228 RVA: 0x00095960 File Offset: 0x00093B60
	private void ReleaseTextureIfExists()
	{
		if (this.m_renderTexture == null)
		{
			return;
		}
		for (int i = 0; i < this.m_subscribers.Count; i++)
		{
			this.m_subscribers[i].OnBufferDestroyed(this);
		}
		this.m_camera.targetTexture = null;
		this.m_renderTexture.Release();
		this.m_renderTexture = null;
	}

	// Token: 0x0600146D RID: 5229 RVA: 0x000959C2 File Offset: 0x00093BC2
	private void OnDestroy()
	{
		this.ReleaseTextureIfExists();
		this.DestroyClearCameraIfExists();
	}

	// Token: 0x0400141E RID: 5150
	public static bool m_autoRenderScale = false;

	// Token: 0x0400141F RID: 5151
	public static float m_global3DRenderScale = 1f;

	// Token: 0x04001420 RID: 5152
	private Camera m_camera;

	// Token: 0x04001421 RID: 5153
	private Camera m_clearCamera;

	// Token: 0x04001422 RID: 5154
	private RenderTexture m_renderTexture;

	// Token: 0x04001423 RID: 5155
	private bool m_isUsingScaledRendering;

	// Token: 0x04001424 RID: 5156
	private List<VirtualFrameBufferScaler> m_subscribers = new List<VirtualFrameBufferScaler>();
}
