using System;
using UnityEngine;

// Token: 0x020001B6 RID: 438
public interface IDoodadController
{
	// Token: 0x060019B0 RID: 6576
	void OnUseStop(Player player);

	// Token: 0x060019B1 RID: 6577
	void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block);

	// Token: 0x060019B2 RID: 6578
	Component GetControlledComponent();

	// Token: 0x060019B3 RID: 6579
	Vector3 GetPosition();

	// Token: 0x060019B4 RID: 6580
	bool IsValid();
}
