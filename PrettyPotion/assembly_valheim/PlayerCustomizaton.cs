using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x0200008A RID: 138
public class PlayerCustomizaton : MonoBehaviour
{
	// Token: 0x06000964 RID: 2404 RVA: 0x000525E4 File Offset: 0x000507E4
	private void OnEnable()
	{
		if (this.m_barberGui)
		{
			PlayerCustomizaton.m_barberInstance = this.m_barberGui;
			this.m_rootPanel.gameObject.SetActive(false);
		}
		if (this.m_maleToggle)
		{
			this.m_maleToggle.isOn = true;
		}
		if (this.m_femaleToggle)
		{
			this.m_femaleToggle.isOn = false;
		}
		this.m_beardPanel.gameObject.SetActive(true);
	}

	// Token: 0x06000965 RID: 2405 RVA: 0x00052660 File Offset: 0x00050860
	private bool LoadHair()
	{
		if (this.m_hairs == null && ObjectDB.instance)
		{
			this.m_beards = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Beard");
			this.m_hairs = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Hair");
			this.m_beards.RemoveAll((ItemDrop x) => x.name.Contains("_"));
			this.m_hairs.RemoveAll((ItemDrop x) => x.name.Contains("_"));
			this.m_beards.Sort((ItemDrop x, ItemDrop y) => Localization.instance.Localize(x.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(y.m_itemData.m_shared.m_name)));
			this.m_hairs.Sort((ItemDrop x, ItemDrop y) => Localization.instance.Localize(x.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(y.m_itemData.m_shared.m_name)));
			this.m_beards.Remove(this.m_noBeard);
			this.m_beards.Insert(0, this.m_noBeard);
			this.m_hairs.Remove(this.m_noHair);
			this.m_hairs.Insert(0, this.m_noHair);
			return true;
		}
		return this.m_hairs != null;
	}

	// Token: 0x06000966 RID: 2406 RVA: 0x000527B4 File Offset: 0x000509B4
	private void Update()
	{
		if (!this.LoadHair() || (this.m_rootPanel && !this.m_rootPanel.activeInHierarchy))
		{
			return;
		}
		if (this.GetPlayer() == null)
		{
			return;
		}
		this.m_selectedHair.text = Localization.instance.Localize(this.GetHair());
		this.m_selectedBeard.text = Localization.instance.Localize(this.GetBeard());
		if (this.m_skinHue)
		{
			Color c = Color.Lerp(this.m_skinColor0, this.m_skinColor1, this.m_skinHue.value);
			this.GetPlayer().SetSkinColor(Utils.ColorToVec3(c));
		}
		if (this.m_hairTone)
		{
			Color c2 = Color.Lerp(this.m_hairColor0, this.m_hairColor1, this.m_hairTone.value) * Mathf.Lerp(this.m_hairMinLevel, this.m_hairMaxLevel, this.m_hairLevel.value);
			this.GetPlayer().SetHairColor(Utils.ColorToVec3(c2));
		}
		if (PlayerCustomizaton.IsBarberGuiVisible())
		{
			if (InventoryGui.IsVisible() || Minimap.IsOpen() || Game.IsPaused())
			{
				PlayerCustomizaton.HideBarberGui();
			}
			if (ZInput.GetKeyDown(KeyCode.Escape, true) || Player.m_localPlayer.IsDead())
			{
				this.OnCancel();
			}
		}
	}

	// Token: 0x06000967 RID: 2407 RVA: 0x00052900 File Offset: 0x00050B00
	private Player GetPlayer()
	{
		if (Player.m_localPlayer)
		{
			return Player.m_localPlayer;
		}
		FejdStartup componentInParent = base.GetComponentInParent<FejdStartup>();
		if (componentInParent != null)
		{
			return componentInParent.GetPreviewPlayer();
		}
		return null;
	}

	// Token: 0x06000968 RID: 2408 RVA: 0x00052931 File Offset: 0x00050B31
	public void OnHairHueChange(float v)
	{
	}

	// Token: 0x06000969 RID: 2409 RVA: 0x00052933 File Offset: 0x00050B33
	public void OnSkinHueChange(float v)
	{
	}

	// Token: 0x0600096A RID: 2410 RVA: 0x00052938 File Offset: 0x00050B38
	public void SetPlayerModel(int index)
	{
		Player player = this.GetPlayer();
		if (player == null)
		{
			return;
		}
		player.SetPlayerModel(index);
		if (index == 1)
		{
			this.ResetBeard();
		}
	}

	// Token: 0x0600096B RID: 2411 RVA: 0x00052968 File Offset: 0x00050B68
	public void OnHairLeft()
	{
		int num = this.GetHairIndex();
		for (int i = num - 1; i >= 0; i--)
		{
			if (this.m_hairs[i].m_itemData.m_shared.m_toolTier <= this.m_hairToolTier)
			{
				num = i;
				break;
			}
		}
		this.SetHair(num);
	}

	// Token: 0x0600096C RID: 2412 RVA: 0x000529B8 File Offset: 0x00050BB8
	public void OnHairRight()
	{
		int num = this.GetHairIndex();
		for (int i = num + 1; i < this.m_hairs.Count; i++)
		{
			if (this.m_hairs[i].m_itemData.m_shared.m_toolTier <= this.m_hairToolTier)
			{
				num = i;
				break;
			}
		}
		this.SetHair(num);
	}

	// Token: 0x0600096D RID: 2413 RVA: 0x00052A14 File Offset: 0x00050C14
	public void OnBeardLeft()
	{
		if (this.GetPlayer().GetPlayerModel() == 1)
		{
			return;
		}
		int num = this.GetBeardIndex();
		for (int i = num - 1; i >= 0; i--)
		{
			if (this.m_beards[i].m_itemData.m_shared.m_toolTier <= this.m_hairToolTier)
			{
				num = i;
				break;
			}
		}
		this.SetBeard(num);
	}

	// Token: 0x0600096E RID: 2414 RVA: 0x00052A74 File Offset: 0x00050C74
	public void OnBeardRight()
	{
		if (this.GetPlayer().GetPlayerModel() == 1)
		{
			return;
		}
		int num = this.GetBeardIndex();
		for (int i = num + 1; i < this.m_beards.Count; i++)
		{
			if (this.m_beards[i].m_itemData.m_shared.m_toolTier <= this.m_hairToolTier)
			{
				num = i;
				break;
			}
		}
		this.SetBeard(num);
	}

	// Token: 0x0600096F RID: 2415 RVA: 0x00052ADD File Offset: 0x00050CDD
	public void OnApply()
	{
		PlayerCustomizaton.m_barberInstance.m_rootPanel.gameObject.SetActive(false);
		Player.m_localPlayer.ResetAttachCameraPoint();
		Player.m_localPlayer.AttachStop();
		PlayerCustomizaton.m_barberWasHidden = false;
	}

	// Token: 0x06000970 RID: 2416 RVA: 0x00052B10 File Offset: 0x00050D10
	public void OnCancel()
	{
		this.GetPlayer().SetHair(PlayerCustomizaton.m_lastHair);
		this.GetPlayer().SetBeard(PlayerCustomizaton.m_lastBeard);
		this.GetPlayer().SetHairColor(PlayerCustomizaton.m_lastHairColor);
		PlayerCustomizaton.m_barberInstance.m_rootPanel.gameObject.SetActive(false);
		Player.m_localPlayer.ResetAttachCameraPoint();
		Player.m_localPlayer.AttachStop();
		PlayerCustomizaton.m_barberWasHidden = false;
	}

	// Token: 0x06000971 RID: 2417 RVA: 0x00052B7C File Offset: 0x00050D7C
	private void ResetBeard()
	{
		this.GetPlayer().SetBeard(this.m_noBeard.gameObject.name);
	}

	// Token: 0x06000972 RID: 2418 RVA: 0x00052B99 File Offset: 0x00050D99
	private void SetBeard(int index)
	{
		if (index < 0 || index >= this.m_beards.Count)
		{
			return;
		}
		this.GetPlayer().SetBeard(this.m_beards[index].gameObject.name);
	}

	// Token: 0x06000973 RID: 2419 RVA: 0x00052BD0 File Offset: 0x00050DD0
	private void SetHair(int index)
	{
		ZLog.Log("Set hair " + index.ToString());
		if (index < 0 || index >= this.m_hairs.Count)
		{
			return;
		}
		this.GetPlayer().SetHair(this.m_hairs[index].gameObject.name);
	}

	// Token: 0x06000974 RID: 2420 RVA: 0x00052C28 File Offset: 0x00050E28
	private int GetBeardIndex()
	{
		string beard = this.GetPlayer().GetBeard();
		for (int i = 0; i < this.m_beards.Count; i++)
		{
			if (this.m_beards[i].gameObject.name == beard)
			{
				return i;
			}
		}
		return 0;
	}

	// Token: 0x06000975 RID: 2421 RVA: 0x00052C78 File Offset: 0x00050E78
	private int GetHairIndex()
	{
		string hair = this.GetPlayer().GetHair();
		for (int i = 0; i < this.m_hairs.Count; i++)
		{
			if (this.m_hairs[i].gameObject.name == hair)
			{
				return i;
			}
		}
		return 0;
	}

	// Token: 0x06000976 RID: 2422 RVA: 0x00052CC8 File Offset: 0x00050EC8
	private string GetHair()
	{
		return this.m_hairs[this.GetHairIndex()].m_itemData.m_shared.m_name;
	}

	// Token: 0x06000977 RID: 2423 RVA: 0x00052CEA File Offset: 0x00050EEA
	private string GetBeard()
	{
		return this.m_beards[this.GetBeardIndex()].m_itemData.m_shared.m_name;
	}

	// Token: 0x06000978 RID: 2424 RVA: 0x00052D0C File Offset: 0x00050F0C
	public static bool IsBarberGuiVisible()
	{
		return PlayerCustomizaton.m_barberInstance && PlayerCustomizaton.m_barberInstance.m_rootPanel && PlayerCustomizaton.m_barberInstance.m_rootPanel.gameObject.activeInHierarchy;
	}

	// Token: 0x06000979 RID: 2425 RVA: 0x00052D44 File Offset: 0x00050F44
	public static void ShowBarberGui()
	{
		if (!PlayerCustomizaton.m_barberInstance)
		{
			return;
		}
		if (!PlayerCustomizaton.m_barberWasHidden)
		{
			PlayerCustomizaton.m_lastHair = PlayerCustomizaton.m_barberInstance.GetPlayer().GetHair();
			PlayerCustomizaton.m_lastBeard = PlayerCustomizaton.m_barberInstance.GetPlayer().GetBeard();
			PlayerCustomizaton.m_lastHairColor = PlayerCustomizaton.m_barberInstance.GetPlayer().GetHairColor();
			Player.m_localPlayer.HideHandItems(false, true);
			Vector3 hairColor = PlayerCustomizaton.m_barberInstance.GetPlayer().GetHairColor();
			float value = 0f;
			float value2 = 0f;
			float num = 100f;
			for (float num2 = 0f; num2 < 1f; num2 += 0.02f)
			{
				for (float num3 = PlayerCustomizaton.m_barberInstance.m_hairMinLevel; num3 < PlayerCustomizaton.m_barberInstance.m_hairMaxLevel; num3 += 0.02f)
				{
					Vector3 vector = Utils.ColorToVec3(Color.Lerp(PlayerCustomizaton.m_barberInstance.m_hairColor0, PlayerCustomizaton.m_barberInstance.m_hairColor1, num2) * Mathf.Lerp(PlayerCustomizaton.m_barberInstance.m_hairMinLevel, PlayerCustomizaton.m_barberInstance.m_hairMaxLevel, num3));
					float num4 = Mathf.Abs(vector.x - hairColor.x) + Mathf.Abs(vector.y - hairColor.y) + Mathf.Abs(vector.z - hairColor.z);
					if (num4 < num)
					{
						num = num4;
						value = num2;
						value2 = num3;
					}
				}
			}
			PlayerCustomizaton.m_barberInstance.m_hairTone.value = value;
			PlayerCustomizaton.m_barberInstance.m_hairLevel.value = value2;
		}
		PlayerCustomizaton.m_barberInstance.m_rootPanel.gameObject.SetActive(true);
		PlayerCustomizaton.m_barberInstance.m_apply.Select();
	}

	// Token: 0x0600097A RID: 2426 RVA: 0x00052EF2 File Offset: 0x000510F2
	public static void HideBarberGui()
	{
		PlayerCustomizaton.m_barberWasHidden = true;
		PlayerCustomizaton.m_barberInstance.m_rootPanel.gameObject.SetActive(false);
	}

	// Token: 0x0600097B RID: 2427 RVA: 0x00052F0F File Offset: 0x0005110F
	public static bool BarberBlocksLook()
	{
		return PlayerCustomizaton.IsBarberGuiVisible() && !ZInput.GetKey(KeyCode.Mouse1, true) && !ZInput.IsGamepadActive();
	}

	// Token: 0x04000B0F RID: 2831
	public static PlayerCustomizaton m_barberInstance;

	// Token: 0x04000B10 RID: 2832
	private static string m_lastHair;

	// Token: 0x04000B11 RID: 2833
	private static string m_lastBeard;

	// Token: 0x04000B12 RID: 2834
	private static Vector3 m_lastHairColor;

	// Token: 0x04000B13 RID: 2835
	public Color m_skinColor0 = Color.white;

	// Token: 0x04000B14 RID: 2836
	public Color m_skinColor1 = Color.white;

	// Token: 0x04000B15 RID: 2837
	public Color m_hairColor0 = Color.white;

	// Token: 0x04000B16 RID: 2838
	public Color m_hairColor1 = Color.white;

	// Token: 0x04000B17 RID: 2839
	public float m_hairMaxLevel = 1f;

	// Token: 0x04000B18 RID: 2840
	public float m_hairMinLevel = 0.1f;

	// Token: 0x04000B19 RID: 2841
	public TMP_Text m_selectedBeard;

	// Token: 0x04000B1A RID: 2842
	public TMP_Text m_selectedHair;

	// Token: 0x04000B1B RID: 2843
	public Slider m_skinHue;

	// Token: 0x04000B1C RID: 2844
	public Slider m_hairLevel;

	// Token: 0x04000B1D RID: 2845
	public Slider m_hairTone;

	// Token: 0x04000B1E RID: 2846
	public RectTransform m_beardPanel;

	// Token: 0x04000B1F RID: 2847
	public Toggle m_maleToggle;

	// Token: 0x04000B20 RID: 2848
	public Toggle m_femaleToggle;

	// Token: 0x04000B21 RID: 2849
	public ItemDrop m_noHair;

	// Token: 0x04000B22 RID: 2850
	public ItemDrop m_noBeard;

	// Token: 0x04000B23 RID: 2851
	public GameObject m_rootPanel;

	// Token: 0x04000B24 RID: 2852
	public Button m_apply;

	// Token: 0x04000B25 RID: 2853
	public Button m_cancel;

	// Token: 0x04000B26 RID: 2854
	public PlayerCustomizaton m_barberGui;

	// Token: 0x04000B27 RID: 2855
	public int m_hairToolTier;

	// Token: 0x04000B28 RID: 2856
	private List<ItemDrop> m_beards;

	// Token: 0x04000B29 RID: 2857
	private List<ItemDrop> m_hairs;

	// Token: 0x04000B2A RID: 2858
	private static bool m_barberWasHidden;
}
