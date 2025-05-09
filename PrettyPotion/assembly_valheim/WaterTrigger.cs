using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001DA RID: 474
public class WaterTrigger : MonoBehaviour, IMonoUpdater
{
	// Token: 0x06001B32 RID: 6962 RVA: 0x000CB67F File Offset: 0x000C987F
	private void Start()
	{
		this.m_cooldownTimer = UnityEngine.Random.Range(0f, 2f);
	}

	// Token: 0x06001B33 RID: 6963 RVA: 0x000CB696 File Offset: 0x000C9896
	private void OnEnable()
	{
		WaterTrigger.Instances.Add(this);
	}

	// Token: 0x06001B34 RID: 6964 RVA: 0x000CB6A3 File Offset: 0x000C98A3
	private void OnDisable()
	{
		WaterTrigger.Instances.Remove(this);
	}

	// Token: 0x06001B35 RID: 6965 RVA: 0x000CB6B4 File Offset: 0x000C98B4
	public void CustomUpdate(float deltaTime, float time)
	{
		this.m_cooldownTimer += deltaTime;
		if (this.m_cooldownTimer <= this.m_cooldownDelay)
		{
			return;
		}
		Transform transform = base.transform;
		Vector3 position = transform.position;
		if (Floating.IsUnderWater(position, ref this.m_previousAndOut))
		{
			this.m_effects.Create(position, transform.rotation, transform, 1f, -1);
			this.m_cooldownTimer = 0f;
		}
	}

	// Token: 0x170000CC RID: 204
	// (get) Token: 0x06001B36 RID: 6966 RVA: 0x000CB71F File Offset: 0x000C991F
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04001BF7 RID: 7159
	public EffectList m_effects = new EffectList();

	// Token: 0x04001BF8 RID: 7160
	public float m_cooldownDelay = 2f;

	// Token: 0x04001BF9 RID: 7161
	private float m_cooldownTimer;

	// Token: 0x04001BFA RID: 7162
	private WaterVolume m_previousAndOut;
}
