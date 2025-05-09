using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000013 RID: 19
[RequireComponent(typeof(Character))]
public class CharacterDrop : MonoBehaviour
{
	// Token: 0x06000241 RID: 577 RVA: 0x00014878 File Offset: 0x00012A78
	private void Start()
	{
		this.m_character = base.GetComponent<Character>();
		if (this.m_character)
		{
			Character character = this.m_character;
			character.m_onDeath = (Action)Delegate.Combine(character.m_onDeath, new Action(this.OnDeath));
		}
	}

	// Token: 0x06000242 RID: 578 RVA: 0x000148C5 File Offset: 0x00012AC5
	public void SetDropsEnabled(bool enabled)
	{
		this.m_dropsEnabled = enabled;
	}

	// Token: 0x06000243 RID: 579 RVA: 0x000148D0 File Offset: 0x00012AD0
	private void OnDeath()
	{
		if (!this.m_dropsEnabled)
		{
			return;
		}
		List<KeyValuePair<GameObject, int>> drops = this.GenerateDropList();
		Vector3 centerPos = this.m_character.GetCenterPoint() + base.transform.TransformVector(this.m_spawnOffset);
		CharacterDrop.DropItems(drops, centerPos, 0.5f);
	}

	// Token: 0x06000244 RID: 580 RVA: 0x0001491C File Offset: 0x00012B1C
	public List<KeyValuePair<GameObject, int>> GenerateDropList()
	{
		List<KeyValuePair<GameObject, int>> list = new List<KeyValuePair<GameObject, int>>();
		int num = this.m_character ? Mathf.Max(1, (int)Mathf.Pow(2f, (float)(this.m_character.GetLevel() - 1))) : 1;
		foreach (CharacterDrop.Drop drop in this.m_drops)
		{
			if (!(drop.m_prefab == null))
			{
				float num2 = drop.m_chance;
				if (drop.m_levelMultiplier)
				{
					num2 *= (float)num;
				}
				if (UnityEngine.Random.value <= num2)
				{
					int num3 = drop.m_dontScale ? UnityEngine.Random.Range(drop.m_amountMin, drop.m_amountMax) : Game.instance.ScaleDrops(drop.m_prefab, drop.m_amountMin, drop.m_amountMax);
					if (drop.m_levelMultiplier)
					{
						num3 *= num;
					}
					if (drop.m_onePerPlayer)
					{
						num3 = ZNet.instance.GetNrOfPlayers();
					}
					if (num3 > 100)
					{
						num3 = 100;
					}
					if (num3 > 0)
					{
						list.Add(new KeyValuePair<GameObject, int>(drop.m_prefab, num3));
					}
				}
			}
		}
		return list;
	}

	// Token: 0x06000245 RID: 581 RVA: 0x00014A54 File Offset: 0x00012C54
	public static void DropItems(List<KeyValuePair<GameObject, int>> drops, Vector3 centerPos, float dropArea)
	{
		foreach (KeyValuePair<GameObject, int> keyValuePair in drops)
		{
			for (int i = 0; i < keyValuePair.Value; i++)
			{
				Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
				Vector3 b = UnityEngine.Random.insideUnitSphere * dropArea;
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(keyValuePair.Key, centerPos + b, rotation);
				ItemDrop component = gameObject.GetComponent<ItemDrop>();
				if (component != null)
				{
					component.m_itemData.m_worldLevel = (int)((byte)Game.m_worldLevel);
				}
				Rigidbody component2 = gameObject.GetComponent<Rigidbody>();
				if (component2)
				{
					Vector3 insideUnitSphere = UnityEngine.Random.insideUnitSphere;
					if (insideUnitSphere.y < 0f)
					{
						insideUnitSphere.y = -insideUnitSphere.y;
					}
					component2.AddForce(insideUnitSphere * 5f, ForceMode.VelocityChange);
				}
			}
		}
	}

	// Token: 0x04000327 RID: 807
	public Vector3 m_spawnOffset = Vector3.zero;

	// Token: 0x04000328 RID: 808
	public List<CharacterDrop.Drop> m_drops = new List<CharacterDrop.Drop>();

	// Token: 0x04000329 RID: 809
	private const float m_dropArea = 0.5f;

	// Token: 0x0400032A RID: 810
	private const float m_vel = 5f;

	// Token: 0x0400032B RID: 811
	private bool m_dropsEnabled = true;

	// Token: 0x0400032C RID: 812
	private Character m_character;

	// Token: 0x0200022E RID: 558
	[Serializable]
	public class Drop
	{
		// Token: 0x04001F62 RID: 8034
		public GameObject m_prefab;

		// Token: 0x04001F63 RID: 8035
		public int m_amountMin = 1;

		// Token: 0x04001F64 RID: 8036
		public int m_amountMax = 1;

		// Token: 0x04001F65 RID: 8037
		public float m_chance = 1f;

		// Token: 0x04001F66 RID: 8038
		public bool m_onePerPlayer;

		// Token: 0x04001F67 RID: 8039
		public bool m_levelMultiplier = true;

		// Token: 0x04001F68 RID: 8040
		public bool m_dontScale;
	}
}
