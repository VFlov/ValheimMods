using System;
using UnityEngine;

// Token: 0x02000131 RID: 305
public class ReflectionUpdate : MonoBehaviour
{
	// Token: 0x170000A6 RID: 166
	// (get) Token: 0x0600136D RID: 4973 RVA: 0x00090572 File Offset: 0x0008E772
	public static ReflectionUpdate instance
	{
		get
		{
			return ReflectionUpdate.m_instance;
		}
	}

	// Token: 0x0600136E RID: 4974 RVA: 0x00090579 File Offset: 0x0008E779
	private void Start()
	{
		ReflectionUpdate.m_instance = this;
		this.m_current = this.m_probe1;
	}

	// Token: 0x0600136F RID: 4975 RVA: 0x0009058D File Offset: 0x0008E78D
	private void OnDestroy()
	{
		ReflectionUpdate.m_instance = null;
	}

	// Token: 0x06001370 RID: 4976 RVA: 0x00090598 File Offset: 0x0008E798
	public void UpdateReflection()
	{
		Vector3 vector = ZNet.instance.GetReferencePosition();
		vector += Vector3.up * this.m_reflectionHeight;
		this.m_current = ((this.m_current == this.m_probe1) ? this.m_probe2 : this.m_probe1);
		this.m_current.transform.position = vector;
		this.m_renderID = this.m_current.RenderProbe();
	}

	// Token: 0x06001371 RID: 4977 RVA: 0x00090610 File Offset: 0x0008E810
	private void Update()
	{
		float deltaTime = Time.deltaTime;
		this.m_updateTimer += deltaTime;
		if (this.m_updateTimer > this.m_interval)
		{
			this.m_updateTimer = 0f;
			this.UpdateReflection();
		}
		if (this.m_current.IsFinishedRendering(this.m_renderID))
		{
			float num = Mathf.Clamp01(this.m_updateTimer / this.m_transitionDuration);
			num = Mathf.Pow(num, this.m_power);
			if (this.m_probe1 == this.m_current)
			{
				this.m_probe1.importance = 1;
				this.m_probe2.importance = 0;
				Vector3 size = this.m_probe1.size;
				size.x = 2000f * num;
				size.y = 1000f * num;
				size.z = 2000f * num;
				this.m_probe1.size = size;
				this.m_probe2.size = new Vector3(2001f, 1001f, 2001f);
				return;
			}
			this.m_probe1.importance = 0;
			this.m_probe2.importance = 1;
			Vector3 size2 = this.m_probe2.size;
			size2.x = 2000f * num;
			size2.y = 1000f * num;
			size2.z = 2000f * num;
			this.m_probe2.size = size2;
			this.m_probe1.size = new Vector3(2001f, 1001f, 2001f);
		}
	}

	// Token: 0x04001368 RID: 4968
	private static ReflectionUpdate m_instance;

	// Token: 0x04001369 RID: 4969
	public ReflectionProbe m_probe1;

	// Token: 0x0400136A RID: 4970
	public ReflectionProbe m_probe2;

	// Token: 0x0400136B RID: 4971
	public float m_interval = 3f;

	// Token: 0x0400136C RID: 4972
	public float m_reflectionHeight = 5f;

	// Token: 0x0400136D RID: 4973
	public float m_transitionDuration = 3f;

	// Token: 0x0400136E RID: 4974
	public float m_power = 1f;

	// Token: 0x0400136F RID: 4975
	private ReflectionProbe m_current;

	// Token: 0x04001370 RID: 4976
	private int m_renderID;

	// Token: 0x04001371 RID: 4977
	private float m_updateTimer;
}
