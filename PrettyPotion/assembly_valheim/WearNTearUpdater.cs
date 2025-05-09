using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001DE RID: 478
public class WearNTearUpdater : MonoBehaviour
{
	// Token: 0x06001B88 RID: 7048 RVA: 0x000CE510 File Offset: 0x000CC710
	private void Start()
	{
		this.m_sleepUntilNext = (this.m_sleepUntil = Time.time + 1f);
	}

	// Token: 0x06001B89 RID: 7049 RVA: 0x000CE538 File Offset: 0x000CC738
	private void Update()
	{
		float time = Time.time;
		float deltaTime = Time.deltaTime;
		if (time < this.m_sleepUntil)
		{
			return;
		}
		this.UpdateWearNTear(deltaTime, time);
	}

	// Token: 0x06001B8A RID: 7050 RVA: 0x000CE564 File Offset: 0x000CC764
	private void UpdateWearNTear(float deltaTime, float time)
	{
		List<WearNTear> allInstances = WearNTear.GetAllInstances();
		if (this.m_sleepUntilNext.Equals(this.m_sleepUntil))
		{
			this.m_sleepUntilNext = time + 1f;
			Shader.SetGlobalTexture(WearNTearUpdater.s_ashlandsWearTexture, this.m_ashlandsWearTexture);
			foreach (WearNTear wearNTear in allInstances)
			{
				if (wearNTear.enabled)
				{
					wearNTear.UpdateCover(deltaTime);
				}
			}
			foreach (WearNTear wearNTear2 in allInstances)
			{
				wearNTear2.UpdateAshlandsMaterialValues(time);
			}
			return;
		}
		int num = this.m_index;
		int num2 = 0;
		while (num2 < this.m_updatesPerFrame && allInstances.Count != 0 && num < allInstances.Count)
		{
			WearNTear wearNTear3 = allInstances[num];
			if (wearNTear3.enabled)
			{
				wearNTear3.UpdateWear(time);
			}
			num++;
			num2++;
		}
		this.m_index = ((num < allInstances.Count) ? num : 0);
		if (this.m_index == 0)
		{
			float num3 = this.m_sleepUntilNext - time;
			if (Utils.Abs(num3) >= 0.1f)
			{
				if (num3 < -0.8f)
				{
					this.m_updatesPerFrame += 20;
				}
				else if (num3 < -0.4f)
				{
					this.m_updatesPerFrame += 15;
				}
				else if (num3 < -0.2f)
				{
					this.m_updatesPerFrame += 10;
				}
				else if (num3 < 0f)
				{
					this.m_updatesPerFrame += 5;
				}
				else if (num3 > 0.8f)
				{
					this.m_updatesPerFrame -= 20;
				}
				else if (num3 > 0.6f)
				{
					this.m_updatesPerFrame -= 15;
				}
				else if (num3 > 0.3f)
				{
					this.m_updatesPerFrame -= 10;
				}
				else if (num3 > 0.2f)
				{
					this.m_updatesPerFrame -= 5;
				}
			}
			this.m_sleepUntil = this.m_sleepUntilNext;
			this.m_updatesPerFrame = Mathf.Max(this.m_updatesPerFrame, 5);
			this.m_updatesPerFrame = Mathf.Min(this.m_updatesPerFrame, 100);
		}
	}

	// Token: 0x04001C6B RID: 7275
	private static readonly int s_ashlandsWearTexture = Shader.PropertyToID("_AshlandsWearTexture");

	// Token: 0x04001C6C RID: 7276
	private int m_index;

	// Token: 0x04001C6D RID: 7277
	private float m_sleepUntil;

	// Token: 0x04001C6E RID: 7278
	private float m_sleepUntilNext;

	// Token: 0x04001C6F RID: 7279
	public Texture3D m_ashlandsWearTexture;

	// Token: 0x04001C70 RID: 7280
	private int m_updatesPerFrame = 50;

	// Token: 0x04001C71 RID: 7281
	private const int c_UpdatesPerFrame = 50;

	// Token: 0x04001C72 RID: 7282
	private const float c_WearNTearTime = 1f;
}
