using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000130 RID: 304
[Serializable]
public class RandomEvent
{
	// Token: 0x06001362 RID: 4962 RVA: 0x0009028C File Offset: 0x0008E48C
	public RandomEvent Clone()
	{
		RandomEvent randomEvent = base.MemberwiseClone() as RandomEvent;
		randomEvent.m_spawn = new List<SpawnSystem.SpawnData>();
		foreach (SpawnSystem.SpawnData spawnData in this.m_spawn)
		{
			randomEvent.m_spawn.Add(spawnData.Clone());
		}
		return randomEvent;
	}

	// Token: 0x06001363 RID: 4963 RVA: 0x00090304 File Offset: 0x0008E504
	public bool Update(bool server, bool active, bool playerInArea, float dt)
	{
		if (this.m_pauseIfNoPlayerInArea && !playerInArea)
		{
			return false;
		}
		this.m_time += dt;
		if (this.m_duration > 0f && this.m_time > this.m_duration)
		{
			return true;
		}
		if (this.m_cameraShakeCurve.length > 0)
		{
			GameCamera.instance.AddShake(this.m_pos, this.m_eventRange, this.m_cameraShakeCurve.Evaluate(this.m_time), false);
		}
		return false;
	}

	// Token: 0x06001364 RID: 4964 RVA: 0x00090381 File Offset: 0x0008E581
	public void OnActivate()
	{
		this.m_active = true;
		if (this.m_firstActivation)
		{
			this.m_firstActivation = false;
			if (this.m_startMessage != "")
			{
				MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, this.m_startMessage, 0, null, false);
			}
		}
	}

	// Token: 0x06001365 RID: 4965 RVA: 0x000903BF File Offset: 0x0008E5BF
	public void OnDeactivate(bool end)
	{
		this.m_active = false;
		if (end && this.m_endMessage != "")
		{
			MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, this.m_endMessage, 0, null, false);
		}
	}

	// Token: 0x06001366 RID: 4966 RVA: 0x000903F1 File Offset: 0x0008E5F1
	public string GetHudText()
	{
		return this.m_startMessage;
	}

	// Token: 0x06001367 RID: 4967 RVA: 0x000903F9 File Offset: 0x0008E5F9
	public void OnStart()
	{
		this.m_time = 0f;
	}

	// Token: 0x06001368 RID: 4968 RVA: 0x00090406 File Offset: 0x0008E606
	public void OnStop()
	{
	}

	// Token: 0x06001369 RID: 4969 RVA: 0x00090408 File Offset: 0x0008E608
	public bool InEventBiome()
	{
		return (EnvMan.instance.GetCurrentBiome() & this.m_biome) > Heightmap.Biome.None;
	}

	// Token: 0x0600136A RID: 4970 RVA: 0x0009041E File Offset: 0x0008E61E
	public float GetTime()
	{
		return this.m_time;
	}

	// Token: 0x0600136B RID: 4971 RVA: 0x00090426 File Offset: 0x0008E626
	public override string ToString()
	{
		return string.Format("{0} {1}, enabled: {2}, random: {3}", new object[]
		{
			"RandomEvent",
			this.m_name,
			this.m_enabled,
			this.m_random
		});
	}

	// Token: 0x0400134B RID: 4939
	public string m_name = "";

	// Token: 0x0400134C RID: 4940
	public bool m_enabled = true;

	// Token: 0x0400134D RID: 4941
	public bool m_devDisabled;

	// Token: 0x0400134E RID: 4942
	public bool m_random = true;

	// Token: 0x0400134F RID: 4943
	public float m_duration = 60f;

	// Token: 0x04001350 RID: 4944
	public bool m_nearBaseOnly = true;

	// Token: 0x04001351 RID: 4945
	public bool m_pauseIfNoPlayerInArea = true;

	// Token: 0x04001352 RID: 4946
	public float m_eventRange = 96f;

	// Token: 0x04001353 RID: 4947
	public float m_standaloneInterval;

	// Token: 0x04001354 RID: 4948
	public float m_standaloneChance = 100f;

	// Token: 0x04001355 RID: 4949
	public float m_spawnerDelay;

	// Token: 0x04001356 RID: 4950
	public AnimationCurve m_cameraShakeCurve = new AnimationCurve
	{
		postWrapMode = WrapMode.Once,
		preWrapMode = WrapMode.Once
	};

	// Token: 0x04001357 RID: 4951
	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome;

	// Token: 0x04001358 RID: 4952
	[Header("( Keys required to be TRUE )")]
	public List<string> m_requiredGlobalKeys = new List<string>();

	// Token: 0x04001359 RID: 4953
	[global::Tooltip("If PlayerEvents is set, instead only spawn this event at players know who at least one of each item on each list (or player key below).")]
	public List<ItemDrop> m_altRequiredKnownItems = new List<ItemDrop>();

	// Token: 0x0400135A RID: 4954
	[global::Tooltip("If PlayerEvents is set, instead only spawn this event at players know who have ANY of these keys set (or any known items above).")]
	public List<string> m_altRequiredPlayerKeysAny = new List<string>();

	// Token: 0x0400135B RID: 4955
	[global::Tooltip("If PlayerEvents is set, instead only spawn this event at players know who have ALL of these keys set (or any known items above).")]
	public List<string> m_altRequiredPlayerKeysAll = new List<string>();

	// Token: 0x0400135C RID: 4956
	[Header("( Keys required to be FALSE )")]
	public List<string> m_notRequiredGlobalKeys = new List<string>();

	// Token: 0x0400135D RID: 4957
	[global::Tooltip("If PlayerEvents is set, instead require ALL of the items to be unknown on this list for a player to be able to get that event. (And below not known player keys)")]
	public List<ItemDrop> m_altRequiredNotKnownItems = new List<ItemDrop>();

	// Token: 0x0400135E RID: 4958
	[global::Tooltip("If PlayerEvents is set, instead require ALL of the playerkeys to be unknown on this list for a player to be able to get that event. (And above not known items)")]
	public List<string> m_altNotRequiredPlayerKeys = new List<string>();

	// Token: 0x0400135F RID: 4959
	[Space(20f)]
	public string m_startMessage = "";

	// Token: 0x04001360 RID: 4960
	public string m_endMessage = "";

	// Token: 0x04001361 RID: 4961
	public string m_forceMusic = "";

	// Token: 0x04001362 RID: 4962
	public string m_forceEnvironment = "";

	// Token: 0x04001363 RID: 4963
	public List<SpawnSystem.SpawnData> m_spawn = new List<SpawnSystem.SpawnData>();

	// Token: 0x04001364 RID: 4964
	private bool m_firstActivation = true;

	// Token: 0x04001365 RID: 4965
	private bool m_active;

	// Token: 0x04001366 RID: 4966
	[NonSerialized]
	public float m_time;

	// Token: 0x04001367 RID: 4967
	[NonSerialized]
	public Vector3 m_pos = Vector3.zero;
}
