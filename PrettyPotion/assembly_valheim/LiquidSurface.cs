using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000190 RID: 400
public class LiquidSurface : MonoBehaviour
{
	// Token: 0x060017D6 RID: 6102 RVA: 0x000B1480 File Offset: 0x000AF680
	private void Awake()
	{
		this.m_liquid = base.GetComponentInParent<LiquidVolume>();
	}

	// Token: 0x060017D7 RID: 6103 RVA: 0x000B148E File Offset: 0x000AF68E
	private void FixedUpdate()
	{
		this.UpdateFloaters();
	}

	// Token: 0x060017D8 RID: 6104 RVA: 0x000B1496 File Offset: 0x000AF696
	public LiquidType GetLiquidType()
	{
		return this.m_liquid.m_liquidType;
	}

	// Token: 0x060017D9 RID: 6105 RVA: 0x000B14A3 File Offset: 0x000AF6A3
	public float GetSurface(Vector3 p)
	{
		return this.m_liquid.GetSurface(p);
	}

	// Token: 0x060017DA RID: 6106 RVA: 0x000B14B4 File Offset: 0x000AF6B4
	private void OnTriggerEnter(Collider collider)
	{
		IWaterInteractable component = collider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component != null)
		{
			component.Increment(this.m_liquid.m_liquidType);
			if (!this.m_inWater.Contains(component))
			{
				this.m_inWater.Add(component);
			}
		}
	}

	// Token: 0x060017DB RID: 6107 RVA: 0x000B14FC File Offset: 0x000AF6FC
	private void UpdateFloaters()
	{
		if (this.m_inWater.Count == 0)
		{
			return;
		}
		LiquidSurface.s_inWaterRemoveIndices.Clear();
		for (int i = 0; i < this.m_inWater.Count; i++)
		{
			IWaterInteractable waterInteractable = this.m_inWater[i];
			if (waterInteractable == null)
			{
				LiquidSurface.s_inWaterRemoveIndices.Add(i);
			}
			else
			{
				Transform transform = waterInteractable.GetTransform();
				if (transform)
				{
					float surface = this.m_liquid.GetSurface(transform.position);
					waterInteractable.SetLiquidLevel(surface, this.m_liquid.m_liquidType, this);
				}
				else
				{
					LiquidSurface.s_inWaterRemoveIndices.Add(i);
				}
			}
		}
		for (int j = LiquidSurface.s_inWaterRemoveIndices.Count - 1; j >= 0; j--)
		{
			this.m_inWater.RemoveAt(LiquidSurface.s_inWaterRemoveIndices[j]);
		}
	}

	// Token: 0x060017DC RID: 6108 RVA: 0x000B15C8 File Offset: 0x000AF7C8
	private void OnTriggerExit(Collider collider)
	{
		IWaterInteractable component = collider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component != null)
		{
			if (component.Decrement(this.m_liquid.m_liquidType) == 0)
			{
				component.SetLiquidLevel(-10000f, this.m_liquid.m_liquidType, this);
			}
			this.m_inWater.Remove(component);
		}
	}

	// Token: 0x060017DD RID: 6109 RVA: 0x000B161C File Offset: 0x000AF81C
	private void OnDestroy()
	{
		foreach (IWaterInteractable waterInteractable in this.m_inWater)
		{
			if (waterInteractable != null && waterInteractable.Decrement(this.m_liquid.m_liquidType) == 0)
			{
				waterInteractable.SetLiquidLevel(-10000f, this.m_liquid.m_liquidType, this);
			}
		}
		this.m_inWater.Clear();
	}

	// Token: 0x040017B2 RID: 6066
	private LiquidVolume m_liquid;

	// Token: 0x040017B3 RID: 6067
	private readonly List<IWaterInteractable> m_inWater = new List<IWaterInteractable>();

	// Token: 0x040017B4 RID: 6068
	private static readonly List<int> s_inWaterRemoveIndices = new List<int>();
}
