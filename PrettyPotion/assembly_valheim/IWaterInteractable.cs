using System;
using UnityEngine;

// Token: 0x0200017D RID: 381
public interface IWaterInteractable
{
	// Token: 0x060016FA RID: 5882
	void SetLiquidLevel(float level, LiquidType type, Component liquidObj);

	// Token: 0x060016FB RID: 5883
	Transform GetTransform();

	// Token: 0x060016FC RID: 5884
	int Increment(LiquidType type);

	// Token: 0x060016FD RID: 5885
	int Decrement(LiquidType type);
}
