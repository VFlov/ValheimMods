using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000EF RID: 239
public class ZSyncAnimation : MonoBehaviour, IMonoUpdater
{
	// Token: 0x06000FAC RID: 4012 RVA: 0x00075148 File Offset: 0x00073348
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_animator = base.GetComponentInChildren<Animator>();
		this.m_animator.logWarnings = false;
		this.m_nview.Register<string>("SetTrigger", new Action<long, string>(this.RPC_SetTrigger));
		ZDO zdo = this.m_nview.GetZDO();
		bool flag = zdo != null && zdo.IsOwner();
		this.m_boolHashes = new int[this.m_syncBools.Count];
		this.m_boolDefaults = new bool[this.m_syncBools.Count];
		for (int i = 0; i < this.m_syncBools.Count; i++)
		{
			this.m_boolHashes[i] = ZSyncAnimation.GetHash(this.m_syncBools[i]);
			this.m_boolDefaults[i] = this.m_animator.GetBool(this.m_boolHashes[i]);
		}
		this.m_floatHashes = new int[this.m_syncFloats.Count];
		this.m_floatDefaults = new float[this.m_syncFloats.Count];
		for (int j = 0; j < this.m_syncFloats.Count; j++)
		{
			this.m_floatHashes[j] = ZSyncAnimation.GetHash(this.m_syncFloats[j]);
			this.m_floatDefaults[j] = this.m_animator.GetFloat(this.m_floatHashes[j]);
		}
		this.m_intHashes = new int[this.m_syncInts.Count];
		this.m_intDefaults = new int[this.m_syncInts.Count];
		for (int k = 0; k < this.m_syncInts.Count; k++)
		{
			this.m_intHashes[k] = ZSyncAnimation.GetHash(this.m_syncInts[k]);
			this.m_intDefaults[k] = this.m_animator.GetInteger(this.m_intHashes[k]);
		}
		if (flag)
		{
			this.m_animSpeed = zdo.GetFloat(ZSyncAnimation.s_animSpeedID, 1f);
			this.m_animator.speed = this.m_animSpeed;
		}
		if (zdo == null)
		{
			base.enabled = false;
			return;
		}
		this.SyncParameters(Time.fixedDeltaTime, false);
	}

	// Token: 0x06000FAD RID: 4013 RVA: 0x00075359 File Offset: 0x00073559
	private void OnEnable()
	{
		ZSyncAnimation.Instances.Add(this);
	}

	// Token: 0x06000FAE RID: 4014 RVA: 0x00075366 File Offset: 0x00073566
	private void OnDisable()
	{
		ZSyncAnimation.Instances.Remove(this);
	}

	// Token: 0x06000FAF RID: 4015 RVA: 0x00075374 File Offset: 0x00073574
	public static int GetHash(string name)
	{
		return Animator.StringToHash(name);
	}

	// Token: 0x06000FB0 RID: 4016 RVA: 0x0007537C File Offset: 0x0007357C
	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.SyncParameters(fixedDeltaTime, false);
	}

	// Token: 0x06000FB1 RID: 4017 RVA: 0x00075394 File Offset: 0x00073594
	private void SyncParameters(float fixedDeltaTime, bool init = false)
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (this.m_nview.IsOwner())
		{
			float speed = this.m_animator.speed;
			if (init || !this.m_animSpeed.Equals(speed))
			{
				this.m_animSpeed = speed;
				zdo.Set(ZSyncAnimation.s_animSpeedID, this.m_animSpeed);
			}
			return;
		}
		if (!init && !this.m_nview.HasOwner())
		{
			return;
		}
		for (int i = 0; i < this.m_boolHashes.Length; i++)
		{
			int num = this.m_boolHashes[i];
			bool @bool = zdo.GetBool(438569 + num, this.m_boolDefaults[i]);
			this.m_animator.SetBool(num, @bool);
		}
		for (int j = 0; j < this.m_floatHashes.Length; j++)
		{
			int num2 = this.m_floatHashes[j];
			float @float = zdo.GetFloat(438569 + num2, this.m_floatDefaults[j]);
			if (this.m_smoothCharacterSpeeds && (num2 == ZSyncAnimation.s_forwardSpeedID || num2 == ZSyncAnimation.s_sidewaySpeedID))
			{
				this.m_animator.SetFloat(num2, @float, 0.2f, fixedDeltaTime);
			}
			else
			{
				this.m_animator.SetFloat(num2, @float);
			}
		}
		for (int k = 0; k < this.m_intHashes.Length; k++)
		{
			int num3 = this.m_intHashes[k];
			int @int = zdo.GetInt(438569 + num3, this.m_intDefaults[k]);
			this.m_animator.SetInteger(num3, @int);
		}
		float float2 = zdo.GetFloat(ZSyncAnimation.s_animSpeedID, 1f);
		this.m_animator.speed = float2;
	}

	// Token: 0x06000FB2 RID: 4018 RVA: 0x00075527 File Offset: 0x00073727
	public void SetTrigger(string name)
	{
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetTrigger", new object[]
		{
			name
		});
	}

	// Token: 0x06000FB3 RID: 4019 RVA: 0x00075548 File Offset: 0x00073748
	public void SetBool(string name, bool value)
	{
		int hash = ZSyncAnimation.GetHash(name);
		this.SetBool(hash, value);
	}

	// Token: 0x06000FB4 RID: 4020 RVA: 0x00075564 File Offset: 0x00073764
	public void SetBool(int hash, bool value)
	{
		if (this.m_animator.GetBool(hash) == value)
		{
			return;
		}
		this.m_animator.SetBool(hash, value);
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(438569 + hash, value);
		}
	}

	// Token: 0x06000FB5 RID: 4021 RVA: 0x000755C0 File Offset: 0x000737C0
	public void SetFloat(string name, float value)
	{
		int hash = ZSyncAnimation.GetHash(name);
		this.SetFloat(hash, value);
	}

	// Token: 0x06000FB6 RID: 4022 RVA: 0x000755DC File Offset: 0x000737DC
	public void SetFloat(int hash, float value)
	{
		if (Mathf.Abs(this.m_animator.GetFloat(hash) - value) < 0.01f)
		{
			return;
		}
		if (this.m_smoothCharacterSpeeds && (hash == ZSyncAnimation.s_forwardSpeedID || hash == ZSyncAnimation.s_sidewaySpeedID))
		{
			this.m_animator.SetFloat(hash, value, 0.2f, Time.fixedDeltaTime);
		}
		else
		{
			this.m_animator.SetFloat(hash, value);
		}
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(438569 + hash, value);
		}
	}

	// Token: 0x06000FB7 RID: 4023 RVA: 0x00075674 File Offset: 0x00073874
	public void SetInt(string name, int value)
	{
		int hash = ZSyncAnimation.GetHash(name);
		this.SetInt(hash, value);
	}

	// Token: 0x06000FB8 RID: 4024 RVA: 0x00075690 File Offset: 0x00073890
	public void SetInt(int hash, int value)
	{
		if (this.m_animator.GetInteger(hash) == value)
		{
			return;
		}
		this.m_animator.SetInteger(hash, value);
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(438569 + hash, value, false);
		}
	}

	// Token: 0x06000FB9 RID: 4025 RVA: 0x000756ED File Offset: 0x000738ED
	private void RPC_SetTrigger(long sender, string name)
	{
		this.m_animator.SetTrigger(name);
	}

	// Token: 0x06000FBA RID: 4026 RVA: 0x000756FB File Offset: 0x000738FB
	public void SetSpeed(float speed)
	{
		this.m_animator.speed = speed;
	}

	// Token: 0x06000FBB RID: 4027 RVA: 0x00075709 File Offset: 0x00073909
	public bool IsOwner()
	{
		return this.m_nview.IsValid() && this.m_nview.IsOwner();
	}

	// Token: 0x17000082 RID: 130
	// (get) Token: 0x06000FBC RID: 4028 RVA: 0x00075725 File Offset: 0x00073925
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04000F0F RID: 3855
	private ZNetView m_nview;

	// Token: 0x04000F10 RID: 3856
	private Animator m_animator;

	// Token: 0x04000F11 RID: 3857
	public List<string> m_syncBools = new List<string>();

	// Token: 0x04000F12 RID: 3858
	public List<string> m_syncFloats = new List<string>();

	// Token: 0x04000F13 RID: 3859
	public List<string> m_syncInts = new List<string>();

	// Token: 0x04000F14 RID: 3860
	public bool m_smoothCharacterSpeeds = true;

	// Token: 0x04000F15 RID: 3861
	private static readonly int s_forwardSpeedID = ZSyncAnimation.GetHash("forward_speed");

	// Token: 0x04000F16 RID: 3862
	private static readonly int s_sidewaySpeedID = ZSyncAnimation.GetHash("sideway_speed");

	// Token: 0x04000F17 RID: 3863
	private static readonly int s_animSpeedID = ZSyncAnimation.GetHash("anim_speed");

	// Token: 0x04000F18 RID: 3864
	private int[] m_boolHashes;

	// Token: 0x04000F19 RID: 3865
	private bool[] m_boolDefaults;

	// Token: 0x04000F1A RID: 3866
	private int[] m_floatHashes;

	// Token: 0x04000F1B RID: 3867
	private float[] m_floatDefaults;

	// Token: 0x04000F1C RID: 3868
	private int[] m_intHashes;

	// Token: 0x04000F1D RID: 3869
	private int[] m_intDefaults;

	// Token: 0x04000F1E RID: 3870
	private float m_animSpeed;

	// Token: 0x04000F1F RID: 3871
	private const int c_ZDOSalt = 438569;
}
