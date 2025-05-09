using System;

// Token: 0x02000189 RID: 393
public interface Interactable
{
	// Token: 0x0600179B RID: 6043
	bool Interact(Humanoid user, bool hold, bool alt);

	// Token: 0x0600179C RID: 6044
	bool UseItem(Humanoid user, ItemDrop.ItemData item);
}
