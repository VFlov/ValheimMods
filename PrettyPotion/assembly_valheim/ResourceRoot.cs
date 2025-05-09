using System;
using UnityEngine;

// Token: 0x020001AF RID: 431
public class ResourceRoot : MonoBehaviour, Hoverable
{
	// Token: 0x0600192B RID: 6443 RVA: 0x000BC2DC File Offset: 0x000BA4DC
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_nview.Register<float>("RPC_Drain", new Action<long, float>(this.RPC_Drain));
		base.InvokeRepeating("UpdateTick", UnityEngine.Random.Range(0f, 10f), 10f);
	}

	// Token: 0x0600192C RID: 6444 RVA: 0x000BC340 File Offset: 0x000BA540
	public string GetHoverText()
	{
		float level = this.GetLevel();
		string text;
		if (level > this.m_highThreshold)
		{
			text = this.m_statusHigh;
		}
		else if (level > this.m_emptyTreshold)
		{
			text = this.m_statusLow;
		}
		else
		{
			text = this.m_statusEmpty;
		}
		return Localization.instance.Localize(text);
	}

	// Token: 0x0600192D RID: 6445 RVA: 0x000BC38A File Offset: 0x000BA58A
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x0600192E RID: 6446 RVA: 0x000BC392 File Offset: 0x000BA592
	public bool CanDrain(float amount)
	{
		return this.GetLevel() > amount;
	}

	// Token: 0x0600192F RID: 6447 RVA: 0x000BC39D File Offset: 0x000BA59D
	public bool Drain(float amount)
	{
		if (!this.CanDrain(amount))
		{
			return false;
		}
		this.m_nview.InvokeRPC("RPC_Drain", new object[]
		{
			amount
		});
		return true;
	}

	// Token: 0x06001930 RID: 6448 RVA: 0x000BC3CA File Offset: 0x000BA5CA
	private void RPC_Drain(long caller, float amount)
	{
		if (this.GetLevel() > amount)
		{
			this.ModifyLevel(-amount);
		}
	}

	// Token: 0x06001931 RID: 6449 RVA: 0x000BC3E0 File Offset: 0x000BA5E0
	private double GetTimeSinceLastUpdate()
	{
		DateTime time = ZNet.instance.GetTime();
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, time.Ticks));
		TimeSpan timeSpan = time - d;
		this.m_nview.GetZDO().Set(ZDOVars.s_lastTime, time.Ticks);
		double num = timeSpan.TotalSeconds;
		if (num < 0.0)
		{
			num = 0.0;
		}
		return num;
	}

	// Token: 0x06001932 RID: 6450 RVA: 0x000BC460 File Offset: 0x000BA660
	private void ModifyLevel(float mod)
	{
		float num = this.GetLevel();
		num += mod;
		num = Mathf.Clamp(num, 0f, this.m_maxLevel);
		this.m_nview.GetZDO().Set(ZDOVars.s_level, num);
	}

	// Token: 0x06001933 RID: 6451 RVA: 0x000BC4A0 File Offset: 0x000BA6A0
	public float GetLevel()
	{
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_level, this.m_maxLevel);
	}

	// Token: 0x06001934 RID: 6452 RVA: 0x000BC4C0 File Offset: 0x000BA6C0
	private void UpdateTick()
	{
		if (this.m_nview.IsOwner())
		{
			double timeSinceLastUpdate = this.GetTimeSinceLastUpdate();
			float mod = (float)((double)this.m_regenPerSec * timeSinceLastUpdate);
			this.ModifyLevel(mod);
		}
		float level = this.GetLevel();
		if (level < this.m_emptyTreshold || this.m_wasModified)
		{
			this.m_wasModified = true;
			float t = Utils.LerpStep(this.m_emptyTreshold, this.m_highThreshold, level);
			Color value = Color.Lerp(this.m_emptyColor, this.m_fullColor, t);
			MeshRenderer[] meshes = this.m_meshes;
			for (int i = 0; i < meshes.Length; i++)
			{
				Material[] materials = meshes[i].materials;
				for (int j = 0; j < materials.Length; j++)
				{
					materials[j].SetColor("_EmissiveColor", value);
				}
			}
		}
	}

	// Token: 0x06001935 RID: 6453 RVA: 0x000BC584 File Offset: 0x000BA784
	public bool IsLevelLow()
	{
		return this.GetLevel() < this.m_emptyTreshold;
	}

	// Token: 0x04001992 RID: 6546
	public string m_name = "$item_ancientroot";

	// Token: 0x04001993 RID: 6547
	public string m_statusHigh = "$item_ancientroot_full";

	// Token: 0x04001994 RID: 6548
	public string m_statusLow = "$item_ancientroot_half";

	// Token: 0x04001995 RID: 6549
	public string m_statusEmpty = "$item_ancientroot_empty";

	// Token: 0x04001996 RID: 6550
	public float m_maxLevel = 100f;

	// Token: 0x04001997 RID: 6551
	public float m_highThreshold = 50f;

	// Token: 0x04001998 RID: 6552
	public float m_emptyTreshold = 10f;

	// Token: 0x04001999 RID: 6553
	public float m_regenPerSec = 1f;

	// Token: 0x0400199A RID: 6554
	public Color m_fullColor = Color.white;

	// Token: 0x0400199B RID: 6555
	public Color m_emptyColor = Color.black;

	// Token: 0x0400199C RID: 6556
	public MeshRenderer[] m_meshes;

	// Token: 0x0400199D RID: 6557
	private ZNetView m_nview;

	// Token: 0x0400199E RID: 6558
	private bool m_wasModified;
}
