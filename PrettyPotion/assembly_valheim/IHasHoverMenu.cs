using System;
using System.Collections.Generic;

// Token: 0x0200008C RID: 140
public interface IHasHoverMenu
{
	// Token: 0x06000983 RID: 2435
	bool TryGetItems(Player player, out List<string> items);

	// Token: 0x06000984 RID: 2436
	bool CanUseItems(Player player, bool sendErrorMessage = true);
}
