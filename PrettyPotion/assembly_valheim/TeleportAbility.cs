using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000009 RID: 9
public class TeleportAbility : MonoBehaviour, IProjectile
{
	// Token: 0x0600006B RID: 107 RVA: 0x000081D4 File Offset: 0x000063D4
	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		this.m_owner = owner;
		GameObject gameObject = this.FindTarget();
		if (gameObject)
		{
			Vector3 position = gameObject.transform.position;
			if (ZoneSystem.instance.FindFloor(position, out position.y))
			{
				this.m_owner.transform.position = position;
				this.m_owner.transform.rotation = gameObject.transform.rotation;
				if (this.m_message.Length > 0)
				{
					Player.MessageAllInRange(base.transform.position, 100f, MessageHud.MessageType.Center, this.m_message, null);
				}
			}
		}
		ZNetScene.instance.Destroy(base.gameObject);
	}

	// Token: 0x0600006C RID: 108 RVA: 0x00008280 File Offset: 0x00006480
	private GameObject FindTarget()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag(this.m_targetTag);
		List<GameObject> list = new List<GameObject>();
		foreach (GameObject gameObject in array)
		{
			if (Vector3.Distance(gameObject.transform.position, this.m_owner.transform.position) <= this.m_maxTeleportRange)
			{
				list.Add(gameObject);
			}
		}
		if (list.Count == 0)
		{
			ZLog.Log("No valid telport target in range");
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	// Token: 0x0600006D RID: 109 RVA: 0x00008306 File Offset: 0x00006506
	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	// Token: 0x04000151 RID: 337
	public string m_targetTag = "";

	// Token: 0x04000152 RID: 338
	public string m_message = "";

	// Token: 0x04000153 RID: 339
	public float m_maxTeleportRange = 100f;

	// Token: 0x04000154 RID: 340
	private Character m_owner;
}
