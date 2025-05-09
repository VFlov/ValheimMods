using System;
using UnityEngine;

// Token: 0x0200019C RID: 412
public class MusicLocation : MonoBehaviour
{
	// Token: 0x0600185B RID: 6235 RVA: 0x000B5FFC File Offset: 0x000B41FC
	private void Awake()
	{
		this.m_audioSource = base.GetComponent<AudioSource>();
		this.m_baseVolume = this.m_audioSource.volume;
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview)
		{
			this.m_nview.Register("SetPlayed", new Action<long>(this.SetPlayed));
		}
		if (this.m_addRadiusFromLocation)
		{
			Location componentInParent = base.GetComponentInParent<Location>();
			if (componentInParent != null)
			{
				this.m_radius += componentInParent.GetMaxRadius();
			}
		}
	}

	// Token: 0x0600185C RID: 6236 RVA: 0x000B6080 File Offset: 0x000B4280
	private void Update()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		float p_X = Vector3.Distance(base.transform.position, Player.m_localPlayer.transform.position);
		float target = 1f - Utils.SmoothStep(this.m_radius * 0.5f, this.m_radius, p_X);
		this.volume = Mathf.MoveTowards(this.volume, target, Time.deltaTime);
		float num = this.volume * this.m_baseVolume * MusicMan.m_masterMusicVolume;
		if (this.volume > 0f && !this.m_audioSource.isPlaying && !this.m_blockLoopAndFade)
		{
			if (this.m_oneTime && this.HasPlayed())
			{
				return;
			}
			if (this.m_notIfEnemies && BaseAI.HaveEnemyInRange(Player.m_localPlayer, base.transform.position, this.m_radius))
			{
				return;
			}
			this.m_audioSource.time = 0f;
			this.m_audioSource.Play();
		}
		if (!Settings.ContinousMusic && this.m_audioSource.loop)
		{
			this.m_audioSource.loop = false;
			this.m_blockLoopAndFade = true;
		}
		if (this.m_blockLoopAndFade || this.m_forceFade)
		{
			float num2 = this.m_audioSource.time - this.m_audioSource.clip.length + 1.5f;
			if (num2 > 0f)
			{
				num *= 1f - num2 / 1.5f;
			}
			if (Terminal.m_showTests)
			{
				Terminal.m_testList["Music location fade"] = num2.ToString() + " " + (1f - num2 / 1.5f).ToString();
			}
		}
		this.m_audioSource.volume = num;
		if (this.m_blockLoopAndFade && this.volume <= 0f)
		{
			this.m_blockLoopAndFade = false;
			this.m_audioSource.loop = true;
		}
		if (Terminal.m_showTests && this.m_audioSource.isPlaying)
		{
			Terminal.m_testList["Music location current"] = this.m_audioSource.name;
			Terminal.m_testList["Music location vol / volume"] = num.ToString() + " / " + this.volume.ToString();
			if (ZInput.GetKeyDown(KeyCode.N, true) && ZInput.GetKey(KeyCode.LeftShift, true))
			{
				this.m_audioSource.time = this.m_audioSource.clip.length - 4f;
			}
		}
		if (this.m_oneTime && this.volume > 0f && this.m_audioSource.time > this.m_audioSource.clip.length * 0.75f && !this.HasPlayed())
		{
			this.SetPlayed();
		}
	}

	// Token: 0x0600185D RID: 6237 RVA: 0x000B6336 File Offset: 0x000B4536
	private void SetPlayed()
	{
		this.m_nview.InvokeRPC("SetPlayed", Array.Empty<object>());
	}

	// Token: 0x0600185E RID: 6238 RVA: 0x000B634D File Offset: 0x000B454D
	private void SetPlayed(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_played, true);
		ZLog.Log("Setting location music as played");
	}

	// Token: 0x0600185F RID: 6239 RVA: 0x000B637D File Offset: 0x000B457D
	private bool HasPlayed()
	{
		return this.m_nview.GetZDO().GetBool(ZDOVars.s_played, false);
	}

	// Token: 0x06001860 RID: 6240 RVA: 0x000B6395 File Offset: 0x000B4595
	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.5f);
		Gizmos.DrawWireSphere(base.transform.position, this.m_radius);
	}

	// Token: 0x0400184F RID: 6223
	private float volume;

	// Token: 0x04001850 RID: 6224
	public bool m_addRadiusFromLocation = true;

	// Token: 0x04001851 RID: 6225
	public float m_radius = 10f;

	// Token: 0x04001852 RID: 6226
	public bool m_oneTime = true;

	// Token: 0x04001853 RID: 6227
	public bool m_notIfEnemies = true;

	// Token: 0x04001854 RID: 6228
	public bool m_forceFade;

	// Token: 0x04001855 RID: 6229
	private ZNetView m_nview;

	// Token: 0x04001856 RID: 6230
	private AudioSource m_audioSource;

	// Token: 0x04001857 RID: 6231
	private float m_baseVolume;

	// Token: 0x04001858 RID: 6232
	private bool m_blockLoopAndFade;
}
