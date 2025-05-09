using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001D8 RID: 472
public class Vegvisir : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06001B15 RID: 6933 RVA: 0x000CA1E8 File Offset: 0x000C83E8
	public string GetHoverText()
	{
		return Localization.instance.Localize(string.Concat(new string[]
		{
			this.m_name,
			" ",
			this.m_hoverName,
			"\n[<color=yellow><b>$KEY_Use</b></color>] ",
			this.m_useText
		}));
	}

	// Token: 0x06001B16 RID: 6934 RVA: 0x000CA235 File Offset: 0x000C8435
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x06001B17 RID: 6935 RVA: 0x000CA240 File Offset: 0x000C8440
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		foreach (Vegvisir.VegvisrLocation vegvisrLocation in this.m_locations)
		{
			Game.instance.DiscoverClosestLocation(vegvisrLocation.m_locationName, base.transform.position, vegvisrLocation.m_pinName, (int)vegvisrLocation.m_pinType, vegvisrLocation.m_showMap, vegvisrLocation.m_discoverAll);
			Gogan.LogEvent("Game", "Vegvisir", vegvisrLocation.m_locationName, 0L);
		}
		if (!string.IsNullOrEmpty(this.m_setsGlobalKey))
		{
			ZoneSystem.instance.SetGlobalKey(this.m_setsGlobalKey);
		}
		if (!string.IsNullOrEmpty(this.m_setsPlayerKey))
		{
			Player player = character as Player;
			if (player != null)
			{
				player.AddUniqueKey(this.m_setsPlayerKey);
			}
		}
		return true;
	}

	// Token: 0x06001B18 RID: 6936 RVA: 0x000CA31C File Offset: 0x000C851C
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x04001BB8 RID: 7096
	public string m_name = "$piece_vegvisir";

	// Token: 0x04001BB9 RID: 7097
	public string m_useText = "$piece_register_location";

	// Token: 0x04001BBA RID: 7098
	public string m_hoverName = "Pin";

	// Token: 0x04001BBB RID: 7099
	public string m_setsGlobalKey = "";

	// Token: 0x04001BBC RID: 7100
	public string m_setsPlayerKey = "";

	// Token: 0x04001BBD RID: 7101
	public List<Vegvisir.VegvisrLocation> m_locations = new List<Vegvisir.VegvisrLocation>();

	// Token: 0x02000396 RID: 918
	[Serializable]
	public class VegvisrLocation
	{
		// Token: 0x040026BE RID: 9918
		public string m_locationName = "";

		// Token: 0x040026BF RID: 9919
		public string m_pinName = "Pin";

		// Token: 0x040026C0 RID: 9920
		public Minimap.PinType m_pinType;

		// Token: 0x040026C1 RID: 9921
		[global::Tooltip("Discovers all locations of given name, rather than just the closest one.")]
		public bool m_discoverAll;

		// Token: 0x040026C2 RID: 9922
		public bool m_showMap = true;
	}
}
