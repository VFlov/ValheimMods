using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000B0 RID: 176
public class LootSpawner : MonoBehaviour
{
	// Token: 0x06000B3A RID: 2874 RVA: 0x0005F35B File Offset: 0x0005D55B
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		base.InvokeRepeating("UpdateSpawner", 10f, 2f);
	}

	// Token: 0x06000B3B RID: 2875 RVA: 0x0005F38C File Offset: 0x0005D58C
	private void UpdateSpawner()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_spawnAtDay && EnvMan.IsDay())
		{
			return;
		}
		if (!this.m_spawnAtNight && EnvMan.IsNight())
		{
			return;
		}
		if (this.m_spawnWhenEnemiesCleared)
		{
			bool flag = LootSpawner.IsMonsterInRange(base.transform.position, this.m_enemiesCheckRange);
			if (flag && !this.m_seenEnemies)
			{
				this.m_seenEnemies = true;
			}
			if (flag || !this.m_seenEnemies)
			{
				return;
			}
		}
		long @long = this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L);
		DateTime time = ZNet.instance.GetTime();
		DateTime d = new DateTime(@long);
		TimeSpan timeSpan = time - d;
		if (this.m_respawnTimeMinuts <= 0f && @long != 0L)
		{
			return;
		}
		if (timeSpan.TotalMinutes < (double)this.m_respawnTimeMinuts)
		{
			return;
		}
		if (!Player.IsPlayerInRange(base.transform.position, 20f))
		{
			return;
		}
		List<GameObject> dropList = this.m_items.GetDropList();
		for (int i = 0; i < dropList.Count; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.3f;
			Vector3 position = base.transform.position + new Vector3(vector.x, 0.3f * (float)i, vector.y);
			Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
			UnityEngine.Object.Instantiate<GameObject>(dropList[i], position, rotation);
		}
		this.m_spawnEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		this.m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
		this.m_seenEnemies = false;
	}

	// Token: 0x06000B3C RID: 2876 RVA: 0x0005F550 File Offset: 0x0005D750
	public static bool IsMonsterInRange(Vector3 point, float range)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		float time = Time.time;
		foreach (Character character in allCharacters)
		{
			if (character.IsMonsterFaction(time) && Vector3.Distance(character.transform.position, point) < range)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06000B3D RID: 2877 RVA: 0x0005F5C8 File Offset: 0x0005D7C8
	private void OnDrawGizmos()
	{
	}

	// Token: 0x04000C75 RID: 3189
	public DropTable m_items = new DropTable();

	// Token: 0x04000C76 RID: 3190
	public EffectList m_spawnEffect = new EffectList();

	// Token: 0x04000C77 RID: 3191
	public float m_respawnTimeMinuts = 10f;

	// Token: 0x04000C78 RID: 3192
	public bool m_spawnAtNight = true;

	// Token: 0x04000C79 RID: 3193
	public bool m_spawnAtDay = true;

	// Token: 0x04000C7A RID: 3194
	public bool m_spawnWhenEnemiesCleared;

	// Token: 0x04000C7B RID: 3195
	public float m_enemiesCheckRange = 30f;

	// Token: 0x04000C7C RID: 3196
	private const float c_TriggerDistance = 20f;

	// Token: 0x04000C7D RID: 3197
	private ZNetView m_nview;

	// Token: 0x04000C7E RID: 3198
	private bool m_seenEnemies;
}
