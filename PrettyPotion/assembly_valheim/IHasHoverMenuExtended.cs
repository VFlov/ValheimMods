using System;
using System.Collections.Generic;

// Token: 0x0200008D RID: 141
public interface IHasHoverMenuExtended
{
	// Token: 0x06000985 RID: 2437
	bool TryGetItems(Player player, Switch switchRef, out List<string> items);

	// Token: 0x06000986 RID: 2438
	bool CanUseItems(Player player, Switch switchRef, bool sendErrorMessage = true);
}
