using System;
using TMPro;
using UnityEngine;
using Valheim.SettingsGui;

// Token: 0x0200007D RID: 125
public class KeyHints : MonoBehaviour
{
	// Token: 0x0600083B RID: 2107 RVA: 0x00048C26 File Offset: 0x00046E26
	private void OnDestroy()
	{
		KeyHints.m_instance = null;
	}

	// Token: 0x17000033 RID: 51
	// (get) Token: 0x0600083C RID: 2108 RVA: 0x00048C2E File Offset: 0x00046E2E
	public static KeyHints instance
	{
		get
		{
			return KeyHints.m_instance;
		}
	}

	// Token: 0x0600083D RID: 2109 RVA: 0x00048C35 File Offset: 0x00046E35
	private void Awake()
	{
		KeyHints.m_instance = this;
		this.ApplySettings();
	}

	// Token: 0x0600083E RID: 2110 RVA: 0x00048C44 File Offset: 0x00046E44
	public void SetGamePadBindings()
	{
		if (this.m_cycleSnapKey != null)
		{
			this.m_cycleSnapKey.text = "$hud_cyclesnap  <mspace=0.6em>$KEY_PrevSnap / $KEY_NextSnap</mspace>";
			Localization.instance.Localize(this.m_cycleSnapKey.transform);
		}
		if (this.m_buildMenuKey != null)
		{
			Localization.instance.RemoveTextFromCache(this.m_buildMenuKey);
			InputLayout inputLayout = ZInput.InputLayout;
			if (inputLayout != InputLayout.Default)
			{
				if (inputLayout - InputLayout.Alternative1 <= 1)
				{
					this.m_buildMenuKey.text = "$hud_buildmenu  <mspace=0.6em>$KEY_BuildMenu</mspace>";
				}
			}
			else
			{
				this.m_buildMenuKey.text = "$hud_buildmenu  <mspace=0.6em>$KEY_Use</mspace>";
			}
			Localization.instance.Localize(this.m_buildMenuKey.transform);
		}
		if (this.m_buildRotateKey != null)
		{
			Localization.instance.RemoveTextFromCache(this.m_buildRotateKey);
			InputLayout inputLayout = ZInput.InputLayout;
			if (inputLayout != InputLayout.Default)
			{
				if (inputLayout - InputLayout.Alternative1 <= 1)
				{
					this.m_buildRotateKey.text = "$hud_rotate  <mspace=0.6em>$KEY_LTrigger / $KEY_RTrigger</mspace>";
				}
			}
			else
			{
				this.m_buildRotateKey.text = "$hud_rotate  <mspace=0.6em>$KEY_Block + $KEY_RStick</mspace>";
			}
			Localization.instance.Localize(this.m_buildRotateKey.transform);
		}
		if (this.m_dodgeKey != null)
		{
			Localization.instance.RemoveTextFromCache(this.m_dodgeKey);
			InputLayout inputLayout = ZInput.InputLayout;
			if (inputLayout != InputLayout.Default)
			{
				if (inputLayout - InputLayout.Alternative1 <= 1)
				{
					this.m_dodgeKey.text = "$settings_dodge  <mspace=0.6em>$KEY_Block + $KEY_Dodge</mspace>";
				}
			}
			else
			{
				this.m_dodgeKey.text = "$settings_dodge  <mspace=0.6em>$KEY_Block + $KEY_Jump</mspace>";
			}
			Localization.instance.Localize(this.m_dodgeKey.transform);
		}
		this.m_radialKeyHints.UpdateGamepadHints();
	}

	// Token: 0x0600083F RID: 2111 RVA: 0x00048DC1 File Offset: 0x00046FC1
	private void Start()
	{
	}

	// Token: 0x06000840 RID: 2112 RVA: 0x00048DC3 File Offset: 0x00046FC3
	public void ApplySettings()
	{
		this.m_keyHintsEnabled = (PlayerPrefs.GetInt("KeyHints", 1) == 1);
		this.SetGamePadBindings();
	}

	// Token: 0x06000841 RID: 2113 RVA: 0x00048DE4 File Offset: 0x00046FE4
	private void Update()
	{
		this.UpdateHints();
		if (ZInput.GetKeyDown(KeyCode.F9, true))
		{
			ZInput.instance.ChangeLayout(GamepadMapController.NextLayout(ZInput.InputLayout));
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("Changed controller layout to: " + GamepadMapController.GetLayoutStringId(ZInput.InputLayout)), 0, null);
			this.ApplySettings();
		}
	}

	// Token: 0x06000842 RID: 2114 RVA: 0x00048E4C File Offset: 0x0004704C
	private void UpdateHints()
	{
		Player localPlayer = Player.m_localPlayer;
		if (!this.m_keyHintsEnabled || localPlayer == null || localPlayer.IsDead() || Chat.instance.IsChatDialogWindowVisible() || Game.IsPaused() || (InventoryGui.instance != null && (InventoryGui.instance.IsSkillsPanelOpen || InventoryGui.instance.IsTrophisPanelOpen || InventoryGui.instance.IsTextPanelOpen)))
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(false);
			this.m_barberHints.SetActive(false);
			this.m_radialHints.SetActive(false);
			return;
		}
		bool activeSelf = this.m_buildHints.activeSelf;
		bool activeSelf2 = this.m_buildHints.activeSelf;
		ItemDrop.ItemData currentWeapon = localPlayer.GetCurrentWeapon();
		if (InventoryGui.IsVisible())
		{
			bool flag = InventoryGui.instance.IsContainerOpen();
			bool flag2 = InventoryGui.instance.ActiveGroup == 0;
			ItemDrop.ItemData itemData = flag2 ? InventoryGui.instance.ContainerGrid.GetGamepadSelectedItem() : InventoryGui.instance.m_playerGrid.GetGamepadSelectedItem();
			bool flag3 = itemData != null && itemData.IsEquipable();
			bool flag4 = itemData != null && itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable;
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(!flag);
			this.m_inventoryWithContainerHints.SetActive(flag);
			for (int i = 0; i < this.m_equipButtons.Length; i++)
			{
				this.m_equipButtons[i].SetActive(flag4 || (flag3 && !flag2));
			}
			this.m_fishingHints.SetActive(false);
			this.m_barberHints.SetActive(false);
			this.m_radialHints.SetActive(false);
		}
		else if (Hud.instance.m_radialMenu.Active)
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(false);
			this.m_barberHints.SetActive(false);
			this.m_radialHints.SetActive(true);
		}
		else if (localPlayer.InPlaceMode())
		{
			if (ZInput.IsNonClassicFunctionality())
			{
				string str = Localization.instance.Localize("$hud_altplacement  <mspace=0.6em>$KEY_AltKeys + $KEY_AltPlace</mspace>");
				string str2 = localPlayer.AlternativePlacementActive ? Localization.instance.Localize("$hud_off") : Localization.instance.Localize("$hud_on");
				this.m_buildAlternativePlacingKey.text = str + " " + str2;
			}
			if (Hud.IsPieceSelectionVisible())
			{
				if (ZInput.IsGamepadActive())
				{
					this.m_closeMenuHintGP.SetActive(true);
				}
				else
				{
					this.m_closeMenuHintKB.SetActive(true);
				}
			}
			else
			{
				this.m_closeMenuHintGP.SetActive(false);
				this.m_closeMenuHintKB.SetActive(false);
			}
			this.m_buildHints.SetActive(true);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(false);
			this.m_barberHints.SetActive(false);
			this.m_radialHints.SetActive(false);
		}
		else if (PlayerCustomizaton.IsBarberGuiVisible())
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(false);
			this.m_barberHints.SetActive(true);
			this.m_radialHints.SetActive(false);
		}
		else if (localPlayer.GetDoodadController() != null)
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(false);
			this.m_barberHints.SetActive(false);
			this.m_radialHints.SetActive(false);
		}
		else if (currentWeapon != null && currentWeapon.m_shared.m_animationState == ItemDrop.ItemData.AnimationState.FishingRod)
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(true);
			this.m_radialHints.SetActive(false);
		}
		else if (currentWeapon != null && (currentWeapon != localPlayer.m_unarmedWeapon.m_itemData || localPlayer.IsTargeted()))
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(true);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(false);
			this.m_barberHints.SetActive(false);
			this.m_radialHints.SetActive(false);
			bool flag5 = currentWeapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow && currentWeapon.m_shared.m_skillType != Skills.SkillType.Crossbows;
			bool active = !flag5 && currentWeapon.HavePrimaryAttack();
			bool active2 = !flag5 && currentWeapon.HaveSecondaryAttack();
			this.m_bowDrawGP.SetActive(flag5);
			this.m_bowDrawKB.SetActive(flag5);
			this.m_primaryAttackGP.SetActive(active);
			this.m_primaryAttackKB.SetActive(active);
			this.m_secondaryAttackGP.SetActive(active2);
			this.m_secondaryAttackKB.SetActive(active2);
		}
		else
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(false);
			this.m_barberHints.SetActive(false);
			this.m_radialHints.SetActive(false);
		}
		if (this.m_radialKeyHints.isActiveAndEnabled)
		{
			this.m_radialKeyHints.UpdateRadialHints(Hud.instance.m_radialMenu);
		}
	}

	// Token: 0x040009F1 RID: 2545
	private static KeyHints m_instance;

	// Token: 0x040009F2 RID: 2546
	[Header("Key hints")]
	public GameObject m_buildHints;

	// Token: 0x040009F3 RID: 2547
	public GameObject m_combatHints;

	// Token: 0x040009F4 RID: 2548
	public GameObject m_inventoryHints;

	// Token: 0x040009F5 RID: 2549
	public GameObject m_inventoryWithContainerHints;

	// Token: 0x040009F6 RID: 2550
	public GameObject m_fishingHints;

	// Token: 0x040009F7 RID: 2551
	public GameObject m_barberHints;

	// Token: 0x040009F8 RID: 2552
	public GameObject m_radialHints;

	// Token: 0x040009F9 RID: 2553
	public GameObject[] m_equipButtons;

	// Token: 0x040009FA RID: 2554
	public GameObject m_primaryAttackGP;

	// Token: 0x040009FB RID: 2555
	public GameObject m_primaryAttackKB;

	// Token: 0x040009FC RID: 2556
	public GameObject m_secondaryAttackGP;

	// Token: 0x040009FD RID: 2557
	public GameObject m_secondaryAttackKB;

	// Token: 0x040009FE RID: 2558
	public GameObject m_closeMenuHintKB;

	// Token: 0x040009FF RID: 2559
	public GameObject m_closeMenuHintGP;

	// Token: 0x04000A00 RID: 2560
	public GameObject m_bowDrawGP;

	// Token: 0x04000A01 RID: 2561
	public GameObject m_bowDrawKB;

	// Token: 0x04000A02 RID: 2562
	private bool m_keyHintsEnabled = true;

	// Token: 0x04000A03 RID: 2563
	public TextMeshProUGUI m_buildMenuKey;

	// Token: 0x04000A04 RID: 2564
	public TextMeshProUGUI m_buildRotateKey;

	// Token: 0x04000A05 RID: 2565
	public TextMeshProUGUI m_buildAlternativePlacingKey;

	// Token: 0x04000A06 RID: 2566
	public TextMeshProUGUI m_dodgeKey;

	// Token: 0x04000A07 RID: 2567
	public TextMeshProUGUI m_cycleSnapKey;

	// Token: 0x04000A08 RID: 2568
	public KeyHintsRadial m_radialKeyHints;
}
