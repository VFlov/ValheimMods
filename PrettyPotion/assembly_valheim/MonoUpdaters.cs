using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000BB RID: 187
public class MonoUpdaters : MonoBehaviour
{
	// Token: 0x06000BA0 RID: 2976 RVA: 0x000616DD File Offset: 0x0005F8DD
	private void Awake()
	{
		MonoUpdaters.s_instance = this;
	}

	// Token: 0x06000BA1 RID: 2977 RVA: 0x000616E5 File Offset: 0x0005F8E5
	private void OnDestroy()
	{
		MonoUpdaters.s_instance = null;
	}

	// Token: 0x06000BA2 RID: 2978 RVA: 0x000616F0 File Offset: 0x0005F8F0
	private void FixedUpdate()
	{
		MonoUpdaters.s_updateCount++;
		float fixedDeltaTime = Time.fixedDeltaTime;
		if (WaterVolume.Instances.Count > 0)
		{
			WaterVolume.StaticUpdate();
		}
		this.m_update.CustomFixedUpdate(ZSyncTransform.Instances, "MonoUpdaters.FixedUpdate.ZSyncTransform", fixedDeltaTime);
		this.m_update.CustomFixedUpdate(ZSyncAnimation.Instances, "MonoUpdaters.FixedUpdate.ZSyncAnimation", fixedDeltaTime);
		this.m_update.CustomFixedUpdate(Floating.Instances, "MonoUpdaters.FixedUpdate.Floating", fixedDeltaTime);
		this.m_update.CustomFixedUpdate(Ship.Instances, "MonoUpdaters.FixedUpdate.Ship", fixedDeltaTime);
		this.m_update.CustomFixedUpdate(Fish.Instances, "MonoUpdaters.FixedUpdate.Fish", fixedDeltaTime);
		this.m_update.CustomFixedUpdate(CharacterAnimEvent.Instances, "MonoUpdaters.FixedUpdate.CharacterAnimEvent", fixedDeltaTime);
		this.m_updateAITimer += fixedDeltaTime;
		if (this.m_updateAITimer >= 0.05f)
		{
			this.m_ai.UpdateAI(BaseAI.Instances, "MonoUpdaters.FixedUpdate.BaseAI", 0.05f);
			this.m_updateAITimer -= 0.05f;
		}
		this.m_update.CustomFixedUpdate(Character.Instances, "MonoUpdaters.FixedUpdate.Character", fixedDeltaTime);
		this.m_update.CustomFixedUpdate(Aoe.Instances, "MonoUpdaters.FixedUpdate.Aoe", fixedDeltaTime);
		this.m_update.CustomFixedUpdate(EffectArea.Instances, "MonoUpdaters.FixedUpdate.EffectArea", fixedDeltaTime);
		this.m_update.CustomFixedUpdate(RandomFlyingBird.Instances, "MonoUpdaters.FixedUpdate.RandomFlyingBird", fixedDeltaTime);
		this.m_update.CustomFixedUpdate(MeleeWeaponTrail.Instances, "MonoUpdaters.FixedUpdate.MeleeWeaponTrail", fixedDeltaTime);
	}

	// Token: 0x06000BA3 RID: 2979 RVA: 0x0006185C File Offset: 0x0005FA5C
	private void Update()
	{
		MonoUpdaters.s_updateCount++;
		float deltaTime = Time.deltaTime;
		float time = Time.time;
		this.m_waterVolumeInstances.AddRange(WaterVolume.Instances);
		if (this.m_waterVolumeInstances.Count > 0)
		{
			WaterVolume.StaticUpdate();
			foreach (WaterVolume waterVolume in this.m_waterVolumeInstances)
			{
				waterVolume.UpdateFloaters();
			}
			foreach (WaterVolume waterVolume2 in this.m_waterVolumeInstances)
			{
				waterVolume2.UpdateMaterials();
			}
			this.m_waterVolumeInstances.Clear();
		}
		this.m_update.CustomUpdate(Smoke.Instances, "MonoUpdaters.Update.Smoke", deltaTime, time);
		this.m_update.CustomUpdate(ZSFX.Instances, "MonoUpdaters.Update.ZSFX", deltaTime, time);
		this.m_update.CustomUpdate(VisEquipment.Instances, "MonoUpdaters.Update.VisEquipment", deltaTime, time);
		this.m_update.CustomUpdate(FootStep.Instances, "MonoUpdaters.Update.FootStep", deltaTime, time);
		this.m_update.CustomUpdate(InstanceRenderer.Instances, "MonoUpdaters.Update.InstanceRenderer", deltaTime, time);
		this.m_update.CustomUpdate(WaterTrigger.Instances, "MonoUpdaters.Update.WaterTrigger", deltaTime, time);
		this.m_update.CustomUpdate(LightFlicker.Instances, "MonoUpdaters.Update.LightFlicker", deltaTime, time);
		this.m_update.CustomUpdate(SmokeSpawner.Instances, "MonoUpdaters.Update.SmokeSpawner", deltaTime, time);
		this.m_update.CustomUpdate(CraftingStation.Instances, "MonoUpdaters.Update.CraftingStation", deltaTime, time);
	}

	// Token: 0x06000BA4 RID: 2980 RVA: 0x00061A00 File Offset: 0x0005FC00
	private void LateUpdate()
	{
		float deltaTime = Time.deltaTime;
		if (WaterVolume.Instances.Count > 0)
		{
			WaterVolume.StaticUpdate();
		}
		this.m_update.CustomLateUpdate(ZSyncTransform.Instances, "MonoUpdaters.LateUpdate.ZSyncTransform", deltaTime);
		this.m_update.CustomLateUpdate(CharacterAnimEvent.Instances, "MonoUpdaters.LateUpdate.CharacterAnimEvent", deltaTime);
		this.m_update.CustomLateUpdate(Heightmap.Instances, "MonoUpdaters.LateUpdate.Heightmap", deltaTime);
		this.m_update.CustomLateUpdate(ShipEffects.Instances, "MonoUpdaters.LateUpdate.ShipEffects", deltaTime);
		this.m_update.CustomLateUpdate(Tail.Instances, "MonoUpdaters.LateUpdate.Tail", deltaTime);
		this.m_update.CustomLateUpdate(LineAttach.Instances, "MonoUpdaters.LateUpdate.LineAttach", deltaTime);
	}

	// Token: 0x17000052 RID: 82
	// (get) Token: 0x06000BA5 RID: 2981 RVA: 0x00061AA9 File Offset: 0x0005FCA9
	public static int UpdateCount
	{
		get
		{
			return MonoUpdaters.s_updateCount;
		}
	}

	// Token: 0x04000CD4 RID: 3284
	private static MonoUpdaters s_instance;

	// Token: 0x04000CD5 RID: 3285
	private readonly List<IMonoUpdater> m_update = new List<IMonoUpdater>();

	// Token: 0x04000CD6 RID: 3286
	private readonly List<IUpdateAI> m_ai = new List<IUpdateAI>();

	// Token: 0x04000CD7 RID: 3287
	private readonly List<WaterVolume> m_waterVolumeInstances = new List<WaterVolume>();

	// Token: 0x04000CD8 RID: 3288
	private static int s_updateCount;

	// Token: 0x04000CD9 RID: 3289
	private float m_updateAITimer;
}
