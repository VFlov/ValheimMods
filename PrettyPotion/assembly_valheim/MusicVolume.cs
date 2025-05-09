using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200019D RID: 413
public class MusicVolume : MonoBehaviour
{
	// Token: 0x06001862 RID: 6242 RVA: 0x000B63F4 File Offset: 0x000B45F4
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview)
		{
			this.m_PlayCount = this.m_nview.GetZDO().GetInt(ZDOVars.s_plays, 0);
			this.m_nview.Register("RPC_PlayMusic", new Action<long>(this.RPC_PlayMusic));
		}
		if (this.m_addRadiusFromLocation)
		{
			Location componentInParent = base.GetComponentInParent<Location>();
			if (componentInParent != null)
			{
				this.m_radius += componentInParent.GetMaxRadius();
			}
		}
		if (this.m_fadeByProximity)
		{
			MusicVolume.m_proximityMusicVolumes.Add(this);
		}
	}

	// Token: 0x06001863 RID: 6243 RVA: 0x000B648A File Offset: 0x000B468A
	private void OnDestroy()
	{
		MusicVolume.m_proximityMusicVolumes.Remove(this);
	}

	// Token: 0x06001864 RID: 6244 RVA: 0x000B6498 File Offset: 0x000B4698
	private void RPC_PlayMusic(long sender)
	{
		bool flag = Vector3.Distance(Player.m_localPlayer.transform.position, base.transform.position) < this.m_radius + this.m_surroundingPlayersAdditionalRadius;
		if (flag)
		{
			this.PlayMusic();
		}
		if (this.m_nview && this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_plays, flag ? this.m_PlayCount : (this.m_PlayCount + 1), false);
		}
	}

	// Token: 0x06001865 RID: 6245 RVA: 0x000B6530 File Offset: 0x000B4730
	private void PlayMusic()
	{
		ZLog.Log("MusicLocation '" + base.name + "' Playing Music: " + this.m_musicName);
		this.m_PlayCount++;
		MusicMan.instance.LocationMusic(this.m_musicName);
		if (this.m_loopMusic)
		{
			this.m_isLooping = true;
		}
	}

	// Token: 0x06001866 RID: 6246 RVA: 0x000B658C File Offset: 0x000B478C
	private void Update()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		if (this.m_fadeByProximity)
		{
			return;
		}
		double timeSeconds = ZNet.instance.GetTimeSeconds();
		if (timeSeconds > this.m_lastEnterCheck + 1.0)
		{
			this.m_lastEnterCheck = timeSeconds;
			if (this.IsInside(Player.m_localPlayer.transform.position, false))
			{
				if (!this.m_lastWasInside)
				{
					this.m_lastWasInside = (this.m_lastWasInsideWide = true);
					this.OnEnter();
				}
			}
			else
			{
				if (this.m_lastWasInside)
				{
					this.m_lastWasInside = false;
					this.OnExit();
				}
				if (this.m_lastWasInsideWide && !this.IsInside(Player.m_localPlayer.transform.position, true))
				{
					this.m_lastWasInsideWide = false;
					this.OnExitWide();
				}
			}
		}
		if (this.m_isLooping && this.m_lastWasInside && !string.IsNullOrEmpty(this.m_musicName))
		{
			MusicMan.instance.LocationMusic(this.m_musicName);
		}
	}

	// Token: 0x06001867 RID: 6247 RVA: 0x000B6680 File Offset: 0x000B4880
	private void OnEnter()
	{
		ZLog.Log("MusicLocation.OnEnter: " + base.name);
		if (!string.IsNullOrEmpty(this.m_musicName) && (this.m_maxPlaysPerActivation == 0 || this.m_PlayCount < this.m_maxPlaysPerActivation) && UnityEngine.Random.Range(0f, 1f) <= this.m_musicChance && (this.m_musicCanRepeat || MusicMan.instance.m_lastLocationMusic != this.m_musicName))
		{
			if (this.m_nview)
			{
				this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_PlayMusic", Array.Empty<object>());
				return;
			}
			this.PlayMusic();
		}
	}

	// Token: 0x06001868 RID: 6248 RVA: 0x000B6729 File Offset: 0x000B4929
	private void OnExit()
	{
		ZLog.Log("MusicLocation.OnExit: " + base.name);
	}

	// Token: 0x06001869 RID: 6249 RVA: 0x000B6740 File Offset: 0x000B4940
	private void OnExitWide()
	{
		ZLog.Log("MusicLocation.OnExitWide: " + base.name);
		if (MusicMan.instance.m_lastLocationMusic == this.m_musicName && (this.m_stopMusicOnExit || this.m_loopMusic))
		{
			MusicMan.instance.LocationMusic(null);
		}
		this.m_isLooping = false;
	}

	// Token: 0x0600186A RID: 6250 RVA: 0x000B679C File Offset: 0x000B499C
	public bool IsInside(Vector3 point, bool checkOuter = false)
	{
		if (this.IsBox())
		{
			if (!checkOuter)
			{
				return this.GetInnerBounds().Contains(point);
			}
			return this.GetOuterBounds().Contains(point);
		}
		else
		{
			float num = Vector3.Distance(base.transform.position, point);
			if (checkOuter)
			{
				return num < this.m_radius + this.m_outerRadiusExtra;
			}
			return num < this.m_radius;
		}
	}

	// Token: 0x0600186B RID: 6251 RVA: 0x000B6804 File Offset: 0x000B4A04
	private void OnDrawGizmos()
	{
		if (!this.IsBox())
		{
			Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.5f);
			Gizmos.DrawWireSphere(base.transform.position, this.m_radius);
			Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.25f);
			Gizmos.DrawWireSphere(base.transform.position, this.m_radius + this.m_outerRadiusExtra);
			return;
		}
		Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.5f);
		Gizmos.DrawWireCube(this.GetInnerBounds().center, this.GetBox().size);
		Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.25f);
		Gizmos.DrawWireCube(this.GetOuterBounds().center, this.GetOuterBounds().size);
	}

	// Token: 0x0600186C RID: 6252 RVA: 0x000B6908 File Offset: 0x000B4B08
	private bool IsBox()
	{
		return this.GetBox().size.x != 0f;
	}

	// Token: 0x0600186D RID: 6253 RVA: 0x000B6932 File Offset: 0x000B4B32
	private Bounds GetBox()
	{
		if (!this.m_sizeFromRoom)
		{
			return this.m_boundsInner;
		}
		return new Bounds(Vector3.zero, this.m_sizeFromRoom.m_size);
	}

	// Token: 0x0600186E RID: 6254 RVA: 0x000B6964 File Offset: 0x000B4B64
	private Bounds GetInnerBounds()
	{
		Bounds box = this.GetBox();
		return new Bounds(box.center + base.transform.position, box.size);
	}

	// Token: 0x0600186F RID: 6255 RVA: 0x000B699C File Offset: 0x000B4B9C
	private Bounds GetOuterBounds()
	{
		Bounds box = this.GetBox();
		return new Bounds(box.center + base.transform.position, box.size + new Vector3(this.m_outerRadiusExtra, this.m_outerRadiusExtra, this.m_outerRadiusExtra));
	}

	// Token: 0x06001870 RID: 6256 RVA: 0x000B69F0 File Offset: 0x000B4BF0
	private float MinBoundDimension()
	{
		Bounds box = this.GetBox();
		if (box.size.x < box.size.y && box.size.x < box.size.z)
		{
			return box.size.x;
		}
		if (box.size.y >= box.size.z)
		{
			return box.size.z;
		}
		return box.size.y;
	}

	// Token: 0x06001871 RID: 6257 RVA: 0x000B6A78 File Offset: 0x000B4C78
	public static float UpdateProximityVolumes(AudioSource musicSource)
	{
		if (!Player.m_localPlayer)
		{
			return 1f;
		}
		float num = 0f;
		if (MusicVolume.m_lastProximityVolume != null && MusicVolume.m_lastProximityVolume.GetInnerBounds().Contains(Player.m_localPlayer.transform.position))
		{
			num = 1f;
		}
		else
		{
			MusicVolume.m_lastProximityVolume = null;
			MusicVolume.m_close.Clear();
			foreach (MusicVolume musicVolume in MusicVolume.m_proximityMusicVolumes)
			{
				if (musicVolume && musicVolume.IsInside(Player.m_localPlayer.transform.position, true))
				{
					MusicVolume.m_close.Add(musicVolume);
				}
			}
			if (MusicVolume.m_close.Count == 0)
			{
				MusicMan.instance.LocationMusic(null);
				return 1f;
			}
			foreach (MusicVolume musicVolume2 in MusicVolume.m_close)
			{
				if (musicVolume2.IsInside(Player.m_localPlayer.transform.position, false))
				{
					MusicVolume.m_lastProximityVolume = musicVolume2;
					num = 1f;
				}
			}
			if (num == 0f)
			{
				MusicVolume musicVolume3 = null;
				foreach (MusicVolume musicVolume4 in MusicVolume.m_close)
				{
					float num2;
					float num3;
					if (musicVolume4.IsBox())
					{
						num2 = Vector3.Distance(musicVolume4.GetInnerBounds().ClosestPoint(Player.m_localPlayer.transform.position), Player.m_localPlayer.transform.position);
						num3 = musicVolume4.m_outerRadiusExtra - num2;
					}
					else
					{
						float num4 = Vector3.Distance(musicVolume4.transform.position, Player.m_localPlayer.transform.position);
						num2 = num4 - musicVolume4.m_radius;
						num3 = musicVolume4.m_radius + musicVolume4.m_outerRadiusExtra - num4;
					}
					musicVolume4.m_proximity = 1f - Math.Min(1f, num2 / (num2 + num3));
					if (musicVolume3 == null || musicVolume4.m_proximity > musicVolume3.m_proximity)
					{
						musicVolume3 = musicVolume4;
					}
				}
				MusicVolume.m_lastProximityVolume = musicVolume3;
				num = musicVolume3.m_proximity;
			}
		}
		MusicMan.instance.LocationMusic(MusicVolume.m_lastProximityVolume.m_musicName);
		return num;
	}

	// Token: 0x04001859 RID: 6233
	private ZNetView m_nview;

	// Token: 0x0400185A RID: 6234
	public static List<MusicVolume> m_proximityMusicVolumes = new List<MusicVolume>();

	// Token: 0x0400185B RID: 6235
	private static MusicVolume m_lastProximityVolume;

	// Token: 0x0400185C RID: 6236
	private static List<MusicVolume> m_close = new List<MusicVolume>();

	// Token: 0x0400185D RID: 6237
	public bool m_addRadiusFromLocation = true;

	// Token: 0x0400185E RID: 6238
	public float m_radius = 10f;

	// Token: 0x0400185F RID: 6239
	public float m_outerRadiusExtra = 0.5f;

	// Token: 0x04001860 RID: 6240
	public float m_surroundingPlayersAdditionalRadius = 50f;

	// Token: 0x04001861 RID: 6241
	public Bounds m_boundsInner;

	// Token: 0x04001862 RID: 6242
	[global::Tooltip("Takes dimension from the room it's a part of and sets bounds to it's size.")]
	public Room m_sizeFromRoom;

	// Token: 0x04001863 RID: 6243
	[Header("Music")]
	public string m_musicName = "";

	// Token: 0x04001864 RID: 6244
	public float m_musicChance = 0.7f;

	// Token: 0x04001865 RID: 6245
	[global::Tooltip("If the music can play again before playing a different location music first.")]
	public bool m_musicCanRepeat = true;

	// Token: 0x04001866 RID: 6246
	public bool m_loopMusic;

	// Token: 0x04001867 RID: 6247
	public bool m_stopMusicOnExit;

	// Token: 0x04001868 RID: 6248
	public int m_maxPlaysPerActivation;

	// Token: 0x04001869 RID: 6249
	[global::Tooltip("Makes the music fade by distance between inner/outer bounds. With this enabled loop, repeat, stoponexit, chance, etc is ignored.")]
	public bool m_fadeByProximity;

	// Token: 0x0400186A RID: 6250
	[HideInInspector]
	public int m_PlayCount;

	// Token: 0x0400186B RID: 6251
	private double m_lastEnterCheck;

	// Token: 0x0400186C RID: 6252
	private bool m_lastWasInside;

	// Token: 0x0400186D RID: 6253
	private bool m_lastWasInsideWide;

	// Token: 0x0400186E RID: 6254
	private bool m_isLooping;

	// Token: 0x0400186F RID: 6255
	private float m_proximity;
}
