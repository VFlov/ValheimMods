using System;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000152 RID: 338
[RequireComponent(typeof(RawImage))]
public class VirtualFrameBufferScaler : MonoBehaviour
{
	// Token: 0x06001470 RID: 5232 RVA: 0x000959F8 File Offset: 0x00093BF8
	private void Start()
	{
		this.m_rawImage = base.GetComponent<RawImage>();
		this.m_rawImage.raycastTarget = false;
		this.m_rawImage.maskable = false;
		this.m_rawImage.color = new Color(1f, 1f, 1f, 0f);
		this.m_virtualFrameBuffer = UnityEngine.Object.FindObjectOfType<VirtualFrameBuffer>();
		if (this.m_virtualFrameBuffer == null)
		{
			ZLog.LogError("Failed to find VirtualFrameBuffer");
			return;
		}
		this.m_virtualFrameBuffer.Subscribe(this);
	}

	// Token: 0x06001471 RID: 5233 RVA: 0x00095A7D File Offset: 0x00093C7D
	private void OnDestroy()
	{
		if (this.m_virtualFrameBuffer != null)
		{
			this.m_virtualFrameBuffer.Unsubscribe(this);
		}
	}

	// Token: 0x06001472 RID: 5234 RVA: 0x00095A99 File Offset: 0x00093C99
	public void OnBufferCreated(VirtualFrameBuffer virtualFrameBuffer, RenderTexture texture)
	{
		this.m_rawImage.texture = texture;
		this.m_rawImage.color = new Color(1f, 1f, 1f, 1f);
	}

	// Token: 0x06001473 RID: 5235 RVA: 0x00095ACB File Offset: 0x00093CCB
	public void OnBufferDestroyed(VirtualFrameBuffer virtualFrameBuffer)
	{
		this.m_rawImage.texture = null;
		this.m_rawImage.color = new Color(1f, 1f, 1f, 0f);
	}

	// Token: 0x04001425 RID: 5157
	private RawImage m_rawImage;

	// Token: 0x04001426 RID: 5158
	private VirtualFrameBuffer m_virtualFrameBuffer;
}
