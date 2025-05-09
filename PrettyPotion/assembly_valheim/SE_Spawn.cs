using System;
using UnityEngine;

// Token: 0x0200003E RID: 62
public class SE_Spawn : StatusEffect
{
	// Token: 0x0600050D RID: 1293 RVA: 0x0002C3F0 File Offset: 0x0002A5F0
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (this.m_spawned)
		{
			return;
		}
		if (this.m_time > this.m_delay)
		{
			this.m_spawned = true;
			Vector3 position = this.m_character.transform.TransformVector(this.m_spawnOffset);
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_prefab, position, Quaternion.identity);
			Projectile component = gameObject.GetComponent<Projectile>();
			if (component)
			{
				component.Setup(this.m_character, Vector3.zero, -1f, null, null, null);
			}
			this.m_spawnEffect.Create(gameObject.transform.position, gameObject.transform.rotation, null, 1f, -1);
		}
	}

	// Token: 0x040005A7 RID: 1447
	[Header("__SE_Spawn__")]
	public float m_delay = 10f;

	// Token: 0x040005A8 RID: 1448
	public GameObject m_prefab;

	// Token: 0x040005A9 RID: 1449
	public Vector3 m_spawnOffset = new Vector3(0f, 0f, 0f);

	// Token: 0x040005AA RID: 1450
	public EffectList m_spawnEffect = new EffectList();

	// Token: 0x040005AB RID: 1451
	private bool m_spawned;
}
