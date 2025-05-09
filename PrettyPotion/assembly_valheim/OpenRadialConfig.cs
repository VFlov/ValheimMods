using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valheim.UI;

// Token: 0x0200008B RID: 139
[CreateAssetMenu(fileName = "OpenRadialConfig", menuName = "Valheim/Radial/Group Config/Open Radial Config")]
public class OpenRadialConfig : ScriptableObject, IRadialConfig
{
	// Token: 0x1700003E RID: 62
	// (get) Token: 0x0600097D RID: 2429 RVA: 0x00052F85 File Offset: 0x00051185
	public string LocalizedName
	{
		get
		{
			return "Something Went Wrong If You're Seeing This";
		}
	}

	// Token: 0x1700003F RID: 63
	// (get) Token: 0x0600097E RID: 2430 RVA: 0x00052F8C File Offset: 0x0005118C
	public Sprite Sprite
	{
		get
		{
			return null;
		}
	}

	// Token: 0x0600097F RID: 2431 RVA: 0x00052F90 File Offset: 0x00051190
	public void InitRadialConfig(RadialBase radial)
	{
		radial.OnInteractionDelay = delegate(float delay)
		{
			PlayerController.SetTakeInputDelay(delay);
		};
		radial.ShouldAnimateIn = true;
		if (ZInput.GetButton("OpenEmote"))
		{
			radial.Open(RadialData.SO.EmoteGroupConfig, RadialData.SO.MainGroupConfig);
			return;
		}
		if (!this.TryOpenNonDefaultRadials(radial))
		{
			radial.Open(RadialData.SO.MainGroupConfig, null);
		}
	}

	// Token: 0x06000980 RID: 2432 RVA: 0x0005300C File Offset: 0x0005120C
	private bool TryOpenNonDefaultRadials(RadialBase radial)
	{
		Player localPlayer = Player.m_localPlayer;
		if (!localPlayer)
		{
			return false;
		}
		GameObject hoverObject = localPlayer.GetHoverObject();
		if (!hoverObject)
		{
			return false;
		}
		Catapult catapult;
		ShieldGenerator shieldGenerator;
		Chair chair;
		Switch @switch;
		if ((hoverObject.TryGetComponentInParent(out catapult) || hoverObject.TryGetComponentInParent(out shieldGenerator) || hoverObject.TryGetComponentInParent(out chair)) && !hoverObject.TryGetComponent<Switch>(out @switch))
		{
			return false;
		}
		if ((hoverObject ? hoverObject.GetComponentInParent<Interactable>() : null) == null)
		{
			return false;
		}
		if (hoverObject.GetComponentInParent<OfferingBowl>() || hoverObject.GetComponentInParent<Fermenter>() || hoverObject.GetComponentInParent<ItemStand>() || hoverObject.GetComponentInParent<Catapult>())
		{
			this.OpenItemMenu(radial, localPlayer, null, hoverObject);
			return true;
		}
		IHasHoverMenu hasHoverMenu;
		if (hoverObject.TryGetComponentInParent(out hasHoverMenu))
		{
			List<string> list;
			if (!hasHoverMenu.TryGetItems(localPlayer, out list))
			{
				return false;
			}
			if (list.Count > 0)
			{
				this.OpenItemMenu(radial, localPlayer, list, hoverObject);
				return true;
			}
			if (RadialData.SO.OpenNormalRadialWhenHoverMenuFails)
			{
				return false;
			}
			radial.QueuedClose();
			return true;
		}
		else
		{
			IHasHoverMenuExtended hasHoverMenuExtended;
			if (!hoverObject.TryGetComponentInParent(out hasHoverMenuExtended))
			{
				return false;
			}
			List<string> list2;
			if (!hasHoverMenuExtended.TryGetItems(localPlayer, hoverObject.GetComponent<Switch>(), out list2))
			{
				return false;
			}
			if (list2.Count > 0)
			{
				this.OpenItemMenu(radial, localPlayer, list2, hoverObject);
				return true;
			}
			if (RadialData.SO.OpenNormalRadialWhenHoverMenuFails)
			{
				return false;
			}
			radial.QueuedClose();
			return true;
		}
	}

	// Token: 0x06000981 RID: 2433 RVA: 0x0005314C File Offset: 0x0005134C
	private void OpenItemMenu(RadialBase radial, Player player, List<string> items, GameObject hoverObject)
	{
		radial.HoverObject = hoverObject;
		ItemGroupConfig itemGroupConfig = UnityEngine.Object.Instantiate<ItemGroupConfig>(RadialData.SO.ItemGroupConfig);
		itemGroupConfig.GroupName = "allitems";
		if (items == null)
		{
			radial.Open(itemGroupConfig, null);
			return;
		}
		Inventory inventory = player.GetInventory();
		if (string.Equals(items[0], "type", StringComparison.OrdinalIgnoreCase))
		{
			ItemDrop.ItemData.ItemType[] array = (from t in items.Select(delegate(string i)
			{
				ItemDrop.ItemData.ItemType result;
				if (!Enum.TryParse<ItemDrop.ItemData.ItemType>(i, out result))
				{
					return ItemDrop.ItemData.ItemType.None;
				}
				return result;
			})
			where t > ItemDrop.ItemData.ItemType.None
			select t).ToArray<ItemDrop.ItemData.ItemType>();
			if (RadialData.SO.AllowSingleItemHoverMenu)
			{
				itemGroupConfig.ItemTypes = array;
				Piece piece;
				itemGroupConfig.GroupName = (hoverObject.TryGetComponentInParent(out piece) ? piece.m_name : "$piece_useitem");
			}
			int num = inventory.CountItemsByType(array, -1, true, true);
			if (num <= 1)
			{
				if (num > 0)
				{
					itemGroupConfig.m_customItemList = items.Skip(0).ToList<string>();
				}
			}
			else
			{
				itemGroupConfig.ItemTypes = array;
				Piece piece2;
				itemGroupConfig.GroupName = (hoverObject.TryGetComponentInParent(out piece2) ? piece2.m_name : "$piece_useitem");
			}
		}
		else
		{
			itemGroupConfig.m_customItemList = items;
			if (inventory.CountItemsByName(items.ToArray(), -1, true, true) > 1 || RadialData.SO.AllowSingleItemHoverMenu)
			{
				Piece piece3;
				itemGroupConfig.GroupName = (hoverObject.TryGetComponentInParent(out piece3) ? piece3.m_name : "$piece_useitem");
			}
		}
		radial.Open(itemGroupConfig, null);
	}
}
