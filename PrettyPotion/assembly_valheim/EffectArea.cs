using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000173 RID: 371
public class EffectArea : MonoBehaviour, IMonoUpdater
{
	// Token: 0x06001656 RID: 5718 RVA: 0x000A568C File Offset: 0x000A388C
	private void Awake()
	{
		if (!string.IsNullOrEmpty(this.m_statusEffect))
		{
			this.m_statusEffectHash = this.m_statusEffect.GetStableHashCode();
		}
		if (EffectArea.s_characterMask == 0)
		{
			EffectArea.s_characterMask = LayerMask.GetMask(new string[]
			{
				"character_trigger"
			});
		}
		this.m_collider = base.GetComponent<Collider>();
		this.m_collider.isTrigger = true;
		if ((this.m_type & EffectArea.Type.NoMonsters) != EffectArea.Type.None)
		{
			this.noMonsterArea = new KeyValuePair<Bounds, EffectArea>(this.m_collider.bounds, this);
			EffectArea.s_noMonsterAreas.Add(this.noMonsterArea);
			Bounds bounds = this.m_collider.bounds;
			bounds.Expand(new Vector3(15f, 15f, 15f));
			this.noMonsterCloseToArea = new KeyValuePair<Bounds, EffectArea>(bounds, this);
			EffectArea.s_noMonsterCloseToAreas.Add(this.noMonsterCloseToArea);
		}
		if ((this.m_type & EffectArea.Type.Burning) != EffectArea.Type.None)
		{
			Bounds bounds2 = this.m_collider.bounds;
			bounds2.Expand(new Vector3(0.25f, 0.25f, 0.25f));
			this.burnCloseToArea = new KeyValuePair<Bounds, EffectArea>(bounds2, this);
			EffectArea.s_BurningAreas.Add(this.burnCloseToArea);
		}
		this.m_isHeatType = this.m_type.HasFlag(EffectArea.Type.Heat);
		EffectArea.s_allAreas.Add(this);
	}

	// Token: 0x06001657 RID: 5719 RVA: 0x000A57DC File Offset: 0x000A39DC
	private void OnDestroy()
	{
		EffectArea.s_allAreas.Remove(this);
		if (EffectArea.s_noMonsterAreas.Contains(this.noMonsterArea))
		{
			EffectArea.s_noMonsterAreas.Remove(this.noMonsterArea);
		}
		if (EffectArea.s_noMonsterCloseToAreas.Contains(this.noMonsterCloseToArea))
		{
			EffectArea.s_noMonsterCloseToAreas.Remove(this.noMonsterCloseToArea);
		}
		if (EffectArea.s_BurningAreas.Contains(this.burnCloseToArea))
		{
			EffectArea.s_BurningAreas.Remove(this.burnCloseToArea);
		}
	}

	// Token: 0x06001658 RID: 5720 RVA: 0x000A585E File Offset: 0x000A3A5E
	protected virtual void OnEnable()
	{
		EffectArea.Instances.Add(this);
	}

	// Token: 0x06001659 RID: 5721 RVA: 0x000A586B File Offset: 0x000A3A6B
	protected virtual void OnDisable()
	{
		EffectArea.Instances.Remove(this);
	}

	// Token: 0x0600165A RID: 5722 RVA: 0x000A587C File Offset: 0x000A3A7C
	private void OnTriggerEnter(Collider other)
	{
		this.m_collisions++;
		if (!this.m_isHeatType && this.m_statusEffectHash == 0)
		{
			return;
		}
		Character component = other.GetComponent<Character>();
		if (!component || !component.IsOwner())
		{
			return;
		}
		if (this.m_playerOnly && !component.IsPlayer())
		{
			return;
		}
		if (!this.m_collidedWithCharacter.Contains(component))
		{
			this.m_collidedWithCharacter.Add(component);
		}
	}

	// Token: 0x0600165B RID: 5723 RVA: 0x000A58EC File Offset: 0x000A3AEC
	private void OnTriggerExit(Collider other)
	{
		this.m_collisions--;
		Character component = other.GetComponent<Character>();
		if (component != null)
		{
			this.m_collidedWithCharacter.Remove(component);
		}
	}

	// Token: 0x0600165C RID: 5724 RVA: 0x000A5924 File Offset: 0x000A3B24
	public void CustomFixedUpdate(float deltaTime)
	{
		if (this.m_collisions <= 0 || this.m_collidedWithCharacter.Count == 0)
		{
			return;
		}
		if (ZNet.instance == null)
		{
			return;
		}
		foreach (Character character in this.m_collidedWithCharacter)
		{
			if (this.m_statusEffectHash != 0)
			{
				character.GetSEMan().AddStatusEffect(this.m_statusEffectHash, true, 0, 0f);
			}
			if (this.m_isHeatType)
			{
				character.OnNearFire(base.transform.position);
			}
		}
	}

	// Token: 0x0600165D RID: 5725 RVA: 0x000A59D0 File Offset: 0x000A3BD0
	public float GetRadius()
	{
		Collider collider = this.m_collider;
		SphereCollider sphereCollider = collider as SphereCollider;
		float result;
		if (sphereCollider == null)
		{
			CapsuleCollider capsuleCollider = collider as CapsuleCollider;
			if (capsuleCollider == null)
			{
				result = this.m_collider.bounds.size.magnitude;
			}
			else
			{
				result = capsuleCollider.radius;
			}
		}
		else
		{
			result = sphereCollider.radius;
		}
		return result;
	}

	// Token: 0x0600165E RID: 5726 RVA: 0x000A5A2C File Offset: 0x000A3C2C
	public static EffectArea IsPointInsideNoMonsterArea(Vector3 p)
	{
		foreach (KeyValuePair<Bounds, EffectArea> keyValuePair in EffectArea.s_noMonsterAreas)
		{
			if (keyValuePair.Key.Contains(p))
			{
				return keyValuePair.Value;
			}
		}
		return null;
	}

	// Token: 0x0600165F RID: 5727 RVA: 0x000A5A98 File Offset: 0x000A3C98
	public static EffectArea IsPointCloseToNoMonsterArea(Vector3 p)
	{
		foreach (KeyValuePair<Bounds, EffectArea> keyValuePair in EffectArea.s_noMonsterCloseToAreas)
		{
			if (keyValuePair.Key.Contains(p))
			{
				return keyValuePair.Value;
			}
		}
		return null;
	}

	// Token: 0x06001660 RID: 5728 RVA: 0x000A5B04 File Offset: 0x000A3D04
	public static EffectArea IsPointInsideArea(Vector3 p, EffectArea.Type type, float radius = 0f)
	{
		if (type == EffectArea.Type.Burning && radius.Equals(0.25f))
		{
			return EffectArea.GetBurningAreaPointPlus025(p);
		}
		int num = Physics.OverlapSphereNonAlloc(p, radius, EffectArea.m_tempColliders, EffectArea.s_characterMask);
		for (int i = 0; i < num; i++)
		{
			EffectArea component = EffectArea.m_tempColliders[i].GetComponent<EffectArea>();
			if (component && (component.m_type & type) != EffectArea.Type.None)
			{
				return component;
			}
		}
		return null;
	}

	// Token: 0x06001661 RID: 5729 RVA: 0x000A5B6C File Offset: 0x000A3D6C
	public static bool IsPointPlus025InsideBurningArea(Vector3 p)
	{
		foreach (KeyValuePair<Bounds, EffectArea> keyValuePair in EffectArea.s_BurningAreas)
		{
			if (keyValuePair.Key.Contains(p))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001662 RID: 5730 RVA: 0x000A5BD0 File Offset: 0x000A3DD0
	private static EffectArea GetBurningAreaPointPlus025(Vector3 p)
	{
		foreach (KeyValuePair<Bounds, EffectArea> keyValuePair in EffectArea.s_BurningAreas)
		{
			if (keyValuePair.Key.Contains(p))
			{
				return keyValuePair.Value;
			}
		}
		return null;
	}

	// Token: 0x06001663 RID: 5731 RVA: 0x000A5C3C File Offset: 0x000A3E3C
	public static int GetBaseValue(Vector3 p, float radius)
	{
		int num = 0;
		int num2 = Physics.OverlapSphereNonAlloc(p, radius, EffectArea.m_tempColliders, EffectArea.s_characterMask);
		for (int i = 0; i < num2; i++)
		{
			EffectArea component = EffectArea.m_tempColliders[i].GetComponent<EffectArea>();
			if (component && (component.m_type & EffectArea.Type.PlayerBase) != EffectArea.Type.None)
			{
				num++;
			}
		}
		return num;
	}

	// Token: 0x06001664 RID: 5732 RVA: 0x000A5C8D File Offset: 0x000A3E8D
	public static List<EffectArea> GetAllAreas()
	{
		return EffectArea.s_allAreas;
	}

	// Token: 0x170000C0 RID: 192
	// (get) Token: 0x06001665 RID: 5733 RVA: 0x000A5C94 File Offset: 0x000A3E94
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x040015F9 RID: 5625
	private KeyValuePair<Bounds, EffectArea> noMonsterArea;

	// Token: 0x040015FA RID: 5626
	private KeyValuePair<Bounds, EffectArea> noMonsterCloseToArea;

	// Token: 0x040015FB RID: 5627
	private KeyValuePair<Bounds, EffectArea> burnCloseToArea;

	// Token: 0x040015FC RID: 5628
	[BitMask(typeof(EffectArea.Type))]
	public EffectArea.Type m_type;

	// Token: 0x040015FD RID: 5629
	public string m_statusEffect = "";

	// Token: 0x040015FE RID: 5630
	public bool m_playerOnly;

	// Token: 0x040015FF RID: 5631
	private int m_statusEffectHash;

	// Token: 0x04001600 RID: 5632
	private Collider m_collider;

	// Token: 0x04001601 RID: 5633
	private int m_collisions;

	// Token: 0x04001602 RID: 5634
	private List<Character> m_collidedWithCharacter = new List<Character>();

	// Token: 0x04001603 RID: 5635
	private bool m_isHeatType;

	// Token: 0x04001604 RID: 5636
	private static int s_characterMask = 0;

	// Token: 0x04001605 RID: 5637
	private static readonly List<EffectArea> s_allAreas = new List<EffectArea>();

	// Token: 0x04001606 RID: 5638
	private static readonly List<KeyValuePair<Bounds, EffectArea>> s_noMonsterAreas = new List<KeyValuePair<Bounds, EffectArea>>();

	// Token: 0x04001607 RID: 5639
	private static readonly List<KeyValuePair<Bounds, EffectArea>> s_noMonsterCloseToAreas = new List<KeyValuePair<Bounds, EffectArea>>();

	// Token: 0x04001608 RID: 5640
	private static readonly List<KeyValuePair<Bounds, EffectArea>> s_BurningAreas = new List<KeyValuePair<Bounds, EffectArea>>();

	// Token: 0x04001609 RID: 5641
	private static Collider[] m_tempColliders = new Collider[128];

	// Token: 0x0200035A RID: 858
	[Flags]
	public enum Type : byte
	{
		// Token: 0x04002589 RID: 9609
		None = 0,
		// Token: 0x0400258A RID: 9610
		Heat = 1,
		// Token: 0x0400258B RID: 9611
		Fire = 2,
		// Token: 0x0400258C RID: 9612
		PlayerBase = 4,
		// Token: 0x0400258D RID: 9613
		Burning = 8,
		// Token: 0x0400258E RID: 9614
		Teleport = 16,
		// Token: 0x0400258F RID: 9615
		NoMonsters = 32,
		// Token: 0x04002590 RID: 9616
		WarmCozyArea = 64,
		// Token: 0x04002591 RID: 9617
		PrivateProperty = 128
	}
}
