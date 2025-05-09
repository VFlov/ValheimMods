using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200016E RID: 366
public class DropOnDestroyed : MonoBehaviour
{
	// Token: 0x0600160C RID: 5644 RVA: 0x000A2A50 File Offset: 0x000A0C50
	private void Awake()
	{
		IDestructible component = base.GetComponent<IDestructible>();
		Destructible destructible = component as Destructible;
		if (destructible)
		{
			Destructible destructible2 = destructible;
			destructible2.m_onDestroyed = (Action)Delegate.Combine(destructible2.m_onDestroyed, new Action(this.OnDestroyed));
		}
		WearNTear wearNTear = component as WearNTear;
		if (wearNTear)
		{
			WearNTear wearNTear2 = wearNTear;
			wearNTear2.m_onDestroyed = (Action)Delegate.Combine(wearNTear2.m_onDestroyed, new Action(this.OnDestroyed));
		}
	}

	// Token: 0x0600160D RID: 5645 RVA: 0x000A2AC4 File Offset: 0x000A0CC4
	private void OnDestroyed()
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		Vector3 position = base.transform.position;
		if (position.y < groundHeight)
		{
			position.y = groundHeight + 0.1f;
		}
		List<GameObject> dropList = this.m_dropWhenDestroyed.GetDropList();
		for (int i = 0; i < dropList.Count; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
			Vector3 position2 = position + Vector3.up * this.m_spawnYOffset + new Vector3(vector.x, this.m_spawnYStep * (float)i, vector.y);
			Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
			ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate<GameObject>(dropList[i], position2, rotation));
		}
	}

	// Token: 0x040015B4 RID: 5556
	[Header("Drops")]
	public DropTable m_dropWhenDestroyed = new DropTable();

	// Token: 0x040015B5 RID: 5557
	public float m_spawnYOffset = 0.5f;

	// Token: 0x040015B6 RID: 5558
	public float m_spawnYStep = 0.3f;
}
