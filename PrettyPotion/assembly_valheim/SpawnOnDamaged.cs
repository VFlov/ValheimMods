using System;
using UnityEngine;

// Token: 0x020001BE RID: 446
public class SpawnOnDamaged : MonoBehaviour
{
	// Token: 0x06001A15 RID: 6677 RVA: 0x000C27A4 File Offset: 0x000C09A4
	private void Start()
	{
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDamaged = (Action)Delegate.Combine(wearNTear.m_onDamaged, new Action(this.OnDamaged));
		}
		Destructible component2 = base.GetComponent<Destructible>();
		if (component2)
		{
			Destructible destructible = component2;
			destructible.m_onDamaged = (Action)Delegate.Combine(destructible.m_onDamaged, new Action(this.OnDamaged));
		}
	}

	// Token: 0x06001A16 RID: 6678 RVA: 0x000C2813 File Offset: 0x000C0A13
	private void OnDamaged()
	{
		if (this.m_spawnOnDamage)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnDamage, base.transform.position, Quaternion.identity);
		}
	}

	// Token: 0x04001A94 RID: 6804
	public GameObject m_spawnOnDamage;
}
