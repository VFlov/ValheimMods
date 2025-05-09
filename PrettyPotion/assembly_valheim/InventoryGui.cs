using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Token: 0x0200007A RID: 122
public class InventoryGui : MonoBehaviour
{
	// Token: 0x1700002D RID: 45
	// (get) Token: 0x060007DC RID: 2012 RVA: 0x00044655 File Offset: 0x00042855
	public static InventoryGui instance
	{
		get
		{
			return InventoryGui.m_instance;
		}
	}

	// Token: 0x060007DD RID: 2013 RVA: 0x0004465C File Offset: 0x0004285C
	private void Awake()
	{
		InventoryGui.m_instance = this;
		this.m_animator = base.GetComponent<Animator>();
		this.m_inventoryRoot.gameObject.SetActive(true);
		this.m_player.gameObject.SetActive(true);
		this.m_crafting.gameObject.SetActive(true);
		this.m_info.gameObject.SetActive(true);
		this.m_container.gameObject.SetActive(false);
		this.m_splitPanel.gameObject.SetActive(false);
		this.m_trophiesPanel.SetActive(false);
		this.m_variantDialog.gameObject.SetActive(false);
		this.m_skillsDialog.gameObject.SetActive(false);
		this.m_textsDialog.gameObject.SetActive(false);
		this.m_playerGrid = this.m_player.GetComponentInChildren<InventoryGrid>();
		this.m_containerGrid = this.m_container.GetComponentInChildren<InventoryGrid>();
		InventoryGrid playerGrid = this.m_playerGrid;
		playerGrid.m_onSelected = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>)Delegate.Combine(playerGrid.m_onSelected, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>(this.OnSelectedItem));
		InventoryGrid playerGrid2 = this.m_playerGrid;
		playerGrid2.m_onRightClick = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i>)Delegate.Combine(playerGrid2.m_onRightClick, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i>(this.OnRightClickItem));
		InventoryGrid containerGrid = this.m_containerGrid;
		containerGrid.m_onSelected = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>)Delegate.Combine(containerGrid.m_onSelected, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>(this.OnSelectedItem));
		InventoryGrid containerGrid2 = this.m_containerGrid;
		containerGrid2.m_onRightClick = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i>)Delegate.Combine(containerGrid2.m_onRightClick, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i>(this.OnRightClickItem));
		InventoryGrid playerGrid3 = this.m_playerGrid;
		playerGrid3.OnMoveToLowerInventoryGrid = (Action<Vector2i>)Delegate.Combine(playerGrid3.OnMoveToLowerInventoryGrid, new Action<Vector2i>(this.MoveToLowerInventoryGrid));
		InventoryGrid containerGrid3 = this.m_containerGrid;
		containerGrid3.OnMoveToUpperInventoryGrid = (Action<Vector2i>)Delegate.Combine(containerGrid3.OnMoveToUpperInventoryGrid, new Action<Vector2i>(this.MoveToUpperInventoryGrid));
		this.m_craftButton.onClick.AddListener(new UnityAction(this.OnCraftPressed));
		this.m_craftCancelButton.onClick.AddListener(new UnityAction(this.OnCraftCancelPressed));
		this.m_dropButton.onClick.AddListener(new UnityAction(this.OnDropOutside));
		this.m_takeAllButton.onClick.AddListener(new UnityAction(this.OnTakeAll));
		this.m_stackAllButton.onClick.AddListener(new UnityAction(this.OnStackAll));
		this.m_repairButton.onClick.AddListener(new UnityAction(this.OnRepairPressed));
		this.m_splitSlider.onValueChanged.AddListener(new UnityAction<float>(this.OnSplitSliderChanged));
		this.m_splitCancelButton.onClick.AddListener(new UnityAction(this.OnSplitCancel));
		this.m_splitOkButton.onClick.AddListener(new UnityAction(this.OnSplitOk));
		VariantDialog variantDialog = this.m_variantDialog;
		variantDialog.m_selected = (Action<int>)Delegate.Combine(variantDialog.m_selected, new Action<int>(this.OnVariantSelected));
		this.m_recipeListBaseSize = this.m_recipeListRoot.rect.height;
		this.m_trophieListBaseSize = this.m_trophieListRoot.rect.height;
		this.m_minStationLevelBasecolor = this.m_minStationLevelText.color;
		this.m_tabCraft.interactable = false;
		this.m_tabUpgrade.interactable = true;
	}

	// Token: 0x060007DE RID: 2014 RVA: 0x000449AC File Offset: 0x00042BAC
	private void MoveToLowerInventoryGrid(Vector2i previousGridPosition)
	{
		if (!this.m_inventoryGroup.IsActive)
		{
			return;
		}
		if (!this.IsContainerOpen())
		{
			return;
		}
		int num = (int)Math.Ceiling((double)((float)(this.m_playerGrid.GridWidth - this.m_containerGrid.GridWidth) / 2f));
		Vector2i selectionGridPosition = this.m_containerGrid.SelectionGridPosition;
		int a = Mathf.Max(0, previousGridPosition.x - num);
		selectionGridPosition.x = Mathf.Min(a, this.m_containerGrid.GridWidth - 1);
		this.m_containerGrid.SetSelection(selectionGridPosition);
		this.SetActiveGroup(this.m_activeGroup - 1, true);
	}

	// Token: 0x060007DF RID: 2015 RVA: 0x00044A48 File Offset: 0x00042C48
	private void MoveToUpperInventoryGrid(Vector2i previousGridPosition)
	{
		if (!this.m_inventoryGroup.IsActive)
		{
			return;
		}
		int num = (int)Math.Ceiling((double)((float)(this.m_playerGrid.GridWidth - this.m_containerGrid.GridWidth) / 2f));
		Vector2i selectionGridPosition = this.m_playerGrid.SelectionGridPosition;
		int a = Mathf.Max(0, previousGridPosition.x + num);
		int b = Mathf.Min(this.m_playerGrid.GridWidth - 1, previousGridPosition.x);
		selectionGridPosition.x = Mathf.Max(a, b);
		this.m_playerGrid.SetSelection(selectionGridPosition);
		this.SetActiveGroup(this.m_activeGroup + 1, true);
	}

	// Token: 0x060007E0 RID: 2016 RVA: 0x00044AE6 File Offset: 0x00042CE6
	private void OnDestroy()
	{
		InventoryGui.m_instance = null;
	}

	// Token: 0x060007E1 RID: 2017 RVA: 0x00044AF0 File Offset: 0x00042CF0
	private void Update()
	{
		bool @bool = this.m_animator.GetBool("visible");
		if (!@bool)
		{
			this.m_hiddenFrames++;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene() || localPlayer.IsTeleporting())
		{
			this.Hide();
			return;
		}
		if (this.m_craftTimer < 0f && (Chat.instance == null || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !Menu.IsVisible() && TextViewer.instance && !TextViewer.instance.IsVisible() && !localPlayer.InCutscene() && !GameCamera.InFreeFly() && !Minimap.IsOpen())
		{
			if (this.m_trophiesPanel.activeSelf && (ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape, true)))
			{
				this.m_trophiesPanel.SetActive(false);
			}
			else if (this.m_skillsDialog.gameObject.activeSelf && (ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape, true)))
			{
				this.m_skillsDialog.OnClose();
			}
			else if (this.m_textsDialog.gameObject.activeSelf && (ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape, true)))
			{
				this.m_textsDialog.gameObject.SetActive(false);
			}
			else if (this.m_splitPanel.gameObject.activeSelf && (ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape, true)))
			{
				this.m_splitPanel.gameObject.SetActive(false);
			}
			else if (this.m_variantDialog.gameObject.activeSelf && (ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape, true)))
			{
				this.m_variantDialog.gameObject.SetActive(false);
			}
			else if (@bool)
			{
				if (ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("JoyButtonY") || ZInput.GetKeyDown(KeyCode.Escape, true) || ZInput.GetButtonDown("Use"))
				{
					ZInput.ResetButtonStatus("Inventory");
					ZInput.ResetButtonStatus("JoyButtonB");
					ZInput.ResetButtonStatus("JoyButtonY");
					ZInput.ResetButtonStatus("Use");
					this.Hide();
				}
			}
			else if ((ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonY")) && !Hud.InRadial())
			{
				ZInput.ResetButtonStatus("Inventory");
				ZInput.ResetButtonStatus("JoyButtonY");
				localPlayer.ShowTutorial("inventory", true);
				this.Show(null, 1);
			}
		}
		if (@bool)
		{
			this.m_hiddenFrames = 0;
			this.UpdateGamepad();
			this.UpdateInventory(localPlayer);
			this.UpdateContainer(localPlayer);
			this.UpdateItemDrag();
			this.UpdateCharacterStats(localPlayer);
			this.UpdateInventoryWeight(localPlayer);
			this.UpdateContainerWeight();
			this.UpdateSplitDialog();
			this.UpdateRecipe(localPlayer, Time.deltaTime);
			this.UpdateRepair();
		}
	}

	// Token: 0x060007E2 RID: 2018 RVA: 0x00044DE4 File Offset: 0x00042FE4
	private void UpdateGamepad()
	{
		if (!this.m_inventoryGroup.IsActive)
		{
			return;
		}
		if (ZInput.GetButtonDown("JoyTabLeft"))
		{
			this.SetActiveGroup(this.m_activeGroup - 1, true);
		}
		if (ZInput.GetButtonDown("JoyTabRight"))
		{
			this.SetActiveGroup(this.m_activeGroup + 1, true);
		}
		if (this.m_activeGroup == 0 && !this.IsContainerOpen())
		{
			this.SetActiveGroup(1, true);
		}
		if (this.m_activeGroup == 3)
		{
			this.UpdateRecipeGamepadInput();
		}
	}

	// Token: 0x060007E3 RID: 2019 RVA: 0x00044E5C File Offset: 0x0004305C
	private void SetActiveGroup(int index, bool playSound = true)
	{
		if (!this.m_inventoryGroupCycling)
		{
			index = Mathf.Clamp(index, 0, this.m_uiGroups.Length - 1);
		}
		else
		{
			if (index == 0 && !this.IsContainerOpen())
			{
				index = this.m_uiGroups.Length - 1;
			}
			index = (index + this.m_uiGroups.Length) % this.m_uiGroups.Length;
		}
		this.m_activeGroup = index;
		for (int i = 0; i < this.m_uiGroups.Length; i++)
		{
			this.m_uiGroups[i].SetActive(i == this.m_activeGroup);
		}
		if (Player.m_localPlayer && playSound)
		{
			EffectList setActiveGroupEffects = this.m_setActiveGroupEffects;
			if (setActiveGroupEffects == null)
			{
				return;
			}
			setActiveGroupEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity, null, 1f, -1);
		}
	}

	// Token: 0x060007E4 RID: 2020 RVA: 0x00044F1C File Offset: 0x0004311C
	private void UpdateCharacterStats(Player player)
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		this.m_playerName.text = playerProfile.GetName();
		float bodyArmor = player.GetBodyArmor();
		this.m_armor.text = bodyArmor.ToString();
		this.m_pvp.interactable = player.CanSwitchPVP();
		player.SetPVP(this.m_pvp.isOn);
	}

	// Token: 0x060007E5 RID: 2021 RVA: 0x00044F80 File Offset: 0x00043180
	private void UpdateInventoryWeight(Player player)
	{
		int num = Mathf.CeilToInt(player.GetInventory().GetTotalWeight());
		int num2 = Mathf.CeilToInt(player.GetMaxCarryWeight());
		if (num <= num2)
		{
			this.m_weight.text = string.Format("{0}/{1}", num, num2);
			return;
		}
		if (Mathf.Sin(Time.time * 10f) > 0f)
		{
			this.m_weight.text = string.Format("<color=red>{0}</color>/{1}", num, num2);
			return;
		}
		this.m_weight.text = string.Format("{0}/{1}", num, num2);
	}

	// Token: 0x060007E6 RID: 2022 RVA: 0x0004502C File Offset: 0x0004322C
	private void UpdateContainerWeight()
	{
		if (this.m_currentContainer == null)
		{
			return;
		}
		int num = Mathf.CeilToInt(this.m_currentContainer.GetInventory().GetTotalWeight());
		this.m_containerWeight.text = num.ToString();
	}

	// Token: 0x060007E7 RID: 2023 RVA: 0x00045070 File Offset: 0x00043270
	private void UpdateInventory(Player player)
	{
		Inventory inventory = player.GetInventory();
		this.m_playerGrid.UpdateInventory(inventory, player, this.m_dragItem);
	}

	// Token: 0x060007E8 RID: 2024 RVA: 0x00045098 File Offset: 0x00043298
	private void UpdateContainer(Player player)
	{
		if (!this.m_animator.GetBool("visible"))
		{
			return;
		}
		if (this.m_currentContainer && this.m_currentContainer.IsOwner())
		{
			this.m_currentContainer.SetInUse(true);
			this.m_container.gameObject.SetActive(true);
			this.m_containerGrid.UpdateInventory(this.m_currentContainer.GetInventory(), null, this.m_dragItem);
			this.m_containerName.text = Localization.instance.Localize(this.m_currentContainer.GetInventory().GetName());
			if (this.m_firstContainerUpdate)
			{
				this.m_containerGrid.ResetView();
				this.m_firstContainerUpdate = false;
				this.m_containerHoldTime = 0f;
				this.m_containerHoldState = 0;
			}
			if (Vector3.Distance(this.m_currentContainer.transform.position, player.transform.position) > this.m_autoCloseDistance)
			{
				this.CloseContainer();
			}
			if (ZInput.GetButton("Use") || ZInput.GetButton("JoyUse"))
			{
				this.m_containerHoldTime += Time.deltaTime;
				if (this.m_containerHoldTime > this.m_containerHoldPlaceStackDelay && this.m_containerHoldState == 0)
				{
					this.m_currentContainer.StackAll();
					this.m_containerHoldState = 1;
					return;
				}
				if (this.m_containerHoldTime > this.m_containerHoldPlaceStackDelay + this.m_containerHoldExitDelay && this.m_containerHoldState == 1)
				{
					this.Hide();
					return;
				}
			}
			else if (this.m_containerHoldState >= 0)
			{
				this.m_containerHoldState = -1;
				return;
			}
		}
		else
		{
			this.m_container.gameObject.SetActive(false);
			if (this.m_dragInventory != null && this.m_dragInventory != Player.m_localPlayer.GetInventory())
			{
				this.SetupDragItem(null, null, 1);
			}
		}
	}

	// Token: 0x060007E9 RID: 2025 RVA: 0x00045250 File Offset: 0x00043450
	private RectTransform GetSelectedGamepadElement()
	{
		RectTransform gamepadSelectedElement = this.m_playerGrid.GetGamepadSelectedElement();
		if (gamepadSelectedElement)
		{
			return gamepadSelectedElement;
		}
		if (this.m_container.gameObject.activeSelf)
		{
			return this.m_containerGrid.GetGamepadSelectedElement();
		}
		return null;
	}

	// Token: 0x060007EA RID: 2026 RVA: 0x00045294 File Offset: 0x00043494
	private void UpdateItemDrag()
	{
		if (this.m_dragGo)
		{
			if (ZInput.IsGamepadActive() && !ZInput.IsMouseActive())
			{
				RectTransform selectedGamepadElement = this.GetSelectedGamepadElement();
				if (selectedGamepadElement)
				{
					Vector3[] array = new Vector3[4];
					selectedGamepadElement.GetWorldCorners(array);
					this.m_dragGo.transform.position = array[2] + new Vector3(0f, 32f, 0f);
				}
				else
				{
					this.m_dragGo.transform.position = new Vector3(-99999f, 0f, 0f);
				}
			}
			else
			{
				this.m_dragGo.transform.position = ZInput.mousePosition;
			}
			Image component = this.m_dragGo.transform.Find("icon").GetComponent<Image>();
			TMP_Text component2 = this.m_dragGo.transform.Find("name").GetComponent<TMP_Text>();
			TMP_Text component3 = this.m_dragGo.transform.Find("amount").GetComponent<TMP_Text>();
			component.sprite = this.m_dragItem.GetIcon();
			component2.text = this.m_dragItem.m_shared.m_name;
			component3.text = ((this.m_dragAmount > 1) ? this.m_dragAmount.ToString() : "");
			if (ZInput.GetMouseButton(1) || ZInput.GetButton("JoyButtonB"))
			{
				this.SetupDragItem(null, null, 1);
			}
		}
	}

	// Token: 0x060007EB RID: 2027 RVA: 0x00045400 File Offset: 0x00043600
	private void OnTakeAll()
	{
		if (Player.m_localPlayer.IsTeleporting())
		{
			return;
		}
		if (this.m_currentContainer)
		{
			this.SetupDragItem(null, null, 1);
			Inventory inventory = this.m_currentContainer.GetInventory();
			Player.m_localPlayer.GetInventory().MoveAll(inventory);
		}
	}

	// Token: 0x060007EC RID: 2028 RVA: 0x0004544C File Offset: 0x0004364C
	private void OnStackAll()
	{
		if (Player.m_localPlayer.IsTeleporting())
		{
			return;
		}
		if (this.m_currentContainer)
		{
			this.SetupDragItem(null, null, 1);
			this.m_currentContainer.GetInventory().StackAll(Player.m_localPlayer.GetInventory(), false);
		}
	}

	// Token: 0x060007ED RID: 2029 RVA: 0x00045498 File Offset: 0x00043698
	private void OnDropOutside()
	{
		if (this.m_dragGo)
		{
			ZLog.Log("Drop item " + this.m_dragItem.m_shared.m_name);
			if (!this.m_dragInventory.ContainsItem(this.m_dragItem))
			{
				this.SetupDragItem(null, null, 1);
				return;
			}
			if (Player.m_localPlayer.DropItem(this.m_dragInventory, this.m_dragItem, this.m_dragAmount))
			{
				this.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
				this.SetupDragItem(null, null, 1);
				this.UpdateCraftingPanel(false);
			}
		}
	}

	// Token: 0x060007EE RID: 2030 RVA: 0x00045542 File Offset: 0x00043742
	private void OnRightClickItem(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos)
	{
		if (item != null && Player.m_localPlayer)
		{
			Player.m_localPlayer.UseItem(grid.GetInventory(), item, true);
		}
	}

	// Token: 0x060007EF RID: 2031 RVA: 0x00045568 File Offset: 0x00043768
	private void OnSelectedItem(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer.IsTeleporting())
		{
			return;
		}
		if (this.m_dragGo)
		{
			this.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
			bool flag = localPlayer.IsItemEquiped(this.m_dragItem);
			bool flag2 = item != null && localPlayer.IsItemEquiped(item);
			Vector2i gridPos = this.m_dragItem.m_gridPos;
			if ((this.m_dragItem.m_shared.m_questItem || (item != null && item.m_shared.m_questItem)) && this.m_dragInventory != grid.GetInventory())
			{
				return;
			}
			if (!this.m_dragInventory.ContainsItem(this.m_dragItem))
			{
				this.SetupDragItem(null, null, 1);
				return;
			}
			localPlayer.RemoveEquipAction(item);
			localPlayer.RemoveEquipAction(this.m_dragItem);
			localPlayer.UnequipItem(this.m_dragItem, false);
			localPlayer.UnequipItem(item, false);
			bool flag3 = grid.DropItem(this.m_dragInventory, this.m_dragItem, this.m_dragAmount, pos);
			if (this.m_dragItem.m_stack < this.m_dragAmount)
			{
				this.m_dragAmount = this.m_dragItem.m_stack;
			}
			if (flag)
			{
				ItemDrop.ItemData itemAt = grid.GetInventory().GetItemAt(pos.x, pos.y);
				if (itemAt != null)
				{
					localPlayer.EquipItem(itemAt, false);
				}
				if (localPlayer.GetInventory().ContainsItem(this.m_dragItem))
				{
					localPlayer.EquipItem(this.m_dragItem, false);
				}
			}
			if (flag2)
			{
				ItemDrop.ItemData itemAt2 = this.m_dragInventory.GetItemAt(gridPos.x, gridPos.y);
				if (itemAt2 != null)
				{
					localPlayer.EquipItem(itemAt2, false);
				}
				if (localPlayer.GetInventory().ContainsItem(item))
				{
					localPlayer.EquipItem(item, false);
				}
			}
			if (flag3)
			{
				this.SetupDragItem(null, null, 1);
				this.UpdateCraftingPanel(false);
				return;
			}
		}
		else if (item != null)
		{
			if (mod == InventoryGrid.Modifier.Move)
			{
				if (item.m_shared.m_questItem)
				{
					return;
				}
				if (this.m_currentContainer != null)
				{
					localPlayer.RemoveEquipAction(item);
					localPlayer.UnequipItem(item, true);
					if (grid.GetInventory() == this.m_currentContainer.GetInventory())
					{
						localPlayer.GetInventory().MoveItemToThis(grid.GetInventory(), item);
					}
					else
					{
						this.m_currentContainer.GetInventory().MoveItemToThis(localPlayer.GetInventory(), item);
					}
					this.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
					return;
				}
				if (Player.m_localPlayer.DropItem(grid.GetInventory(), item, item.m_stack))
				{
					this.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
					return;
				}
			}
			else if (mod == InventoryGrid.Modifier.Drop)
			{
				if (Player.m_localPlayer.DropItem(grid.GetInventory(), item, item.m_stack))
				{
					this.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
					return;
				}
			}
			else
			{
				if (mod == InventoryGrid.Modifier.Split && item.m_stack > 1)
				{
					this.ShowSplitDialog(item, grid.GetInventory());
					return;
				}
				this.SetupDragItem(item, grid.GetInventory(), item.m_stack);
			}
		}
	}

	// Token: 0x060007F0 RID: 2032 RVA: 0x0004587F File Offset: 0x00043A7F
	public static bool IsVisible()
	{
		return InventoryGui.m_instance && InventoryGui.m_instance.m_hiddenFrames <= 1;
	}

	// Token: 0x060007F1 RID: 2033 RVA: 0x0004589F File Offset: 0x00043A9F
	public bool IsContainerOpen()
	{
		return this.m_currentContainer != null;
	}

	// Token: 0x060007F2 RID: 2034 RVA: 0x000458B0 File Offset: 0x00043AB0
	public void Show(Container container, int activeGroup = 1)
	{
		Hud.HidePieceSelection();
		this.m_animator.SetBool("visible", true);
		this.SetActiveGroup(activeGroup, false);
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			this.SetupCrafting();
		}
		this.m_currentContainer = container;
		this.m_hiddenFrames = 0;
		if (localPlayer)
		{
			this.m_openInventoryEffects.Create(localPlayer.transform.position, Quaternion.identity, null, 1f, -1);
		}
		Gogan.LogEvent("Screen", "Enter", "Inventory", 0L);
	}

	// Token: 0x060007F3 RID: 2035 RVA: 0x00045940 File Offset: 0x00043B40
	public void Hide()
	{
		if (!this.m_animator.GetBool("visible"))
		{
			return;
		}
		this.m_craftTimer = -1f;
		this.m_animator.SetBool("visible", false);
		this.m_trophiesPanel.SetActive(false);
		this.m_variantDialog.gameObject.SetActive(false);
		this.m_skillsDialog.gameObject.SetActive(false);
		this.m_textsDialog.gameObject.SetActive(false);
		this.m_splitPanel.gameObject.SetActive(false);
		this.SetupDragItem(null, null, 1);
		if (this.m_currentContainer)
		{
			this.m_currentContainer.SetInUse(false);
			this.m_currentContainer = null;
		}
		if (Player.m_localPlayer)
		{
			this.m_closeInventoryEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity, null, 1f, -1);
		}
		this.m_containerHoldTime = 0f;
		this.m_containerHoldState = 0;
		Gogan.LogEvent("Screen", "Exit", "Inventory", 0L);
	}

	// Token: 0x060007F4 RID: 2036 RVA: 0x00045A50 File Offset: 0x00043C50
	private void CloseContainer()
	{
		if (this.m_dragInventory != null && this.m_dragInventory != Player.m_localPlayer.GetInventory())
		{
			this.SetupDragItem(null, null, 1);
		}
		if (this.m_currentContainer)
		{
			this.m_currentContainer.SetInUse(false);
			this.m_currentContainer = null;
		}
		this.m_splitPanel.gameObject.SetActive(false);
		this.m_firstContainerUpdate = true;
		this.m_container.gameObject.SetActive(false);
	}

	// Token: 0x060007F5 RID: 2037 RVA: 0x00045AC9 File Offset: 0x00043CC9
	private void SetupCrafting()
	{
		this.UpdateCraftingPanel(true);
	}

	// Token: 0x060007F6 RID: 2038 RVA: 0x00045AD4 File Offset: 0x00043CD4
	private void UpdateCraftingPanel(bool focusView = false)
	{
		Player localPlayer = Player.m_localPlayer;
		if (!localPlayer.GetCurrentCraftingStation() && !localPlayer.NoCostCheat() && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost))
		{
			this.m_tabCraft.interactable = false;
			this.m_tabUpgrade.interactable = true;
			this.m_tabUpgrade.gameObject.SetActive(false);
		}
		else
		{
			this.m_tabUpgrade.gameObject.SetActive(true);
		}
		this.m_tempRecipes.Clear();
		localPlayer.GetAvailableRecipes(ref this.m_tempRecipes);
		this.UpdateRecipeList(this.m_tempRecipes);
		if (this.m_availableRecipes.Count <= 0)
		{
			this.SetRecipe(-1, focusView);
			return;
		}
		if (this.m_selectedRecipe.Recipe != null)
		{
			int selectedRecipeIndex = this.GetSelectedRecipeIndex(true);
			this.SetRecipe(selectedRecipeIndex, focusView);
			return;
		}
		this.SetRecipe(0, focusView);
	}

	// Token: 0x060007F7 RID: 2039 RVA: 0x00045BAC File Offset: 0x00043DAC
	private void UpdateRecipeList(List<Recipe> recipes)
	{
		Player localPlayer = Player.m_localPlayer;
		foreach (InventoryGui.RecipeDataPair recipeDataPair in this.m_availableRecipes)
		{
			UnityEngine.Object.Destroy(recipeDataPair.InterfaceElement);
		}
		this.m_availableRecipes.Clear();
		bool globalKey = ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost);
		if (this.InCraftTab())
		{
			using (List<Recipe>.Enumerator enumerator2 = recipes.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					Recipe recipe = enumerator2.Current;
					this.AddRecipeToList(localPlayer, recipe, null, localPlayer.HaveRequirements(recipe, false, 1, 1) || globalKey);
				}
				goto IL_188;
			}
		}
		for (int i = 0; i < recipes.Count; i++)
		{
			Recipe recipe2 = recipes[i];
			if (recipe2.m_item.m_itemData.m_shared.m_maxQuality > 1)
			{
				this.m_tempItemList.Clear();
				localPlayer.GetInventory().GetAllItems(recipe2.m_item.m_itemData.m_shared.m_name, this.m_tempItemList);
				foreach (ItemDrop.ItemData itemData in this.m_tempItemList)
				{
					bool canCraft = itemData.m_quality < itemData.m_shared.m_maxQuality && (localPlayer.HaveRequirements(recipe2, false, itemData.m_quality + 1, 1) || globalKey);
					this.AddRecipeToList(localPlayer, recipe2, itemData, canCraft);
				}
			}
		}
		IL_188:
		float num = (float)this.m_availableRecipes.Count * this.m_recipeListSpace;
		num = Mathf.Max(this.m_recipeListBaseSize, num);
		this.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		if (Player.m_localPlayer)
		{
			InventoryGui.SortMethod sortMethod = InventoryGui.SortMethod.Original;
			string value;
			InventoryGui.SortMethod sortMethod2;
			if (Player.m_localPlayer.TryGetUniqueKeyValue("sortcraft", out value) && Enum.TryParse<InventoryGui.SortMethod>(value, true, out sortMethod2))
			{
				sortMethod = sortMethod2;
			}
			switch (sortMethod)
			{
			case InventoryGui.SortMethod.Original:
				this.m_availableRecipes.Sort(delegate(InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
				{
					int num2 = InventoryGui.<UpdateRecipeList>g__byCraftable|29_4(a, b);
					if (num2 == 0)
					{
						num2 = InventoryGui.<UpdateRecipeList>g__bySortWeight|29_5(a, b);
					}
					if (num2 == 0)
					{
						num2 = a.Recipe.m_item.m_itemData.m_shared.m_name.CompareTo(b.Recipe.m_item.m_itemData.m_shared.m_name);
					}
					if (num2 == 0)
					{
						num2 = InventoryGui.<UpdateRecipeList>g__byLevel|29_7(a, b);
					}
					return num2;
				});
				break;
			case InventoryGui.SortMethod.Name:
				this.m_availableRecipes.Sort(delegate(InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
				{
					int num2 = InventoryGui.<UpdateRecipeList>g__byCraftable|29_4(a, b);
					if (num2 == 0)
					{
						num2 = InventoryGui.<UpdateRecipeList>g__bySortWeight|29_5(a, b);
					}
					if (num2 == 0)
					{
						num2 = InventoryGui.<UpdateRecipeList>g__byName|29_6(a, b);
					}
					if (num2 == 0)
					{
						num2 = InventoryGui.<UpdateRecipeList>g__byLevel|29_7(a, b);
					}
					return num2;
				});
				break;
			case InventoryGui.SortMethod.Type:
				this.m_availableRecipes.Sort(delegate(InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
				{
					int num2 = InventoryGui.<UpdateRecipeList>g__byCraftable|29_4(a, b);
					if (num2 == 0)
					{
						num2 = InventoryGui.<UpdateRecipeList>g__bySortWeight|29_5(a, b);
					}
					if (num2 == 0)
					{
						num2 = a.Recipe.m_item.m_itemData.m_shared.m_itemType.CompareTo(b.Recipe.m_item.m_itemData.m_shared.m_itemType);
					}
					if (num2 == 0)
					{
						num2 = InventoryGui.<UpdateRecipeList>g__byName|29_6(a, b);
					}
					if (num2 == 0)
					{
						num2 = InventoryGui.<UpdateRecipeList>g__byLevel|29_7(a, b);
					}
					return num2;
				});
				break;
			case InventoryGui.SortMethod.Weight:
				this.m_availableRecipes.Sort(delegate(InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
				{
					int num2 = InventoryGui.<UpdateRecipeList>g__byCraftable|29_4(a, b);
					if (num2 == 0)
					{
						num2 = InventoryGui.<UpdateRecipeList>g__bySortWeight|29_5(a, b);
					}
					if (num2 == 0)
					{
						num2 = a.Recipe.m_item.m_itemData.m_shared.m_weight.CompareTo(b.Recipe.m_item.m_itemData.m_shared.m_weight);
					}
					if (num2 == 0)
					{
						num2 = InventoryGui.<UpdateRecipeList>g__byName|29_6(a, b);
					}
					if (num2 == 0)
					{
						num2 = InventoryGui.<UpdateRecipeList>g__byLevel|29_7(a, b);
					}
					return num2;
				});
				break;
			}
			for (int j = 0; j < this.m_availableRecipes.Count; j++)
			{
				(this.m_availableRecipes[j].InterfaceElement.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)j * -this.m_recipeListSpace);
			}
		}
	}

	// Token: 0x060007F8 RID: 2040 RVA: 0x00045EF0 File Offset: 0x000440F0
	private void AddRecipeToList(Player player, Recipe recipe, ItemDrop.ItemData item, bool canCraft)
	{
		int count = this.m_availableRecipes.Count;
		GameObject element = UnityEngine.Object.Instantiate<GameObject>(this.m_recipeElementPrefab, this.m_recipeListRoot);
		element.SetActive(true);
		(element.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)count * -this.m_recipeListSpace);
		Image component = element.transform.Find("icon").GetComponent<Image>();
		component.sprite = recipe.m_item.m_itemData.GetIcon();
		component.color = (canCraft ? Color.white : new Color(1f, 0f, 1f, 0f));
		TMP_Text component2 = element.transform.Find("name").GetComponent<TMP_Text>();
		string text = Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name);
		if (recipe.m_amount > 1)
		{
			text += string.Format(" x{0}", recipe.m_amount);
		}
		component2.text = text;
		component2.color = (canCraft ? Color.white : new Color(0.66f, 0.66f, 0.66f, 1f));
		GuiBar component3 = element.transform.Find("Durability").GetComponent<GuiBar>();
		if (item != null && item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability())
		{
			component3.gameObject.SetActive(true);
			component3.SetValue(item.GetDurabilityPercentage());
		}
		else
		{
			component3.gameObject.SetActive(false);
		}
		TMP_Text component4 = element.transform.Find("QualityLevel").GetComponent<TMP_Text>();
		if (item != null)
		{
			component4.gameObject.SetActive(true);
			component4.text = item.m_quality.ToString();
		}
		else
		{
			component4.gameObject.SetActive(false);
		}
		element.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.OnSelectedRecipe(element);
		});
		this.m_availableRecipes.Add(new InventoryGui.RecipeDataPair(recipe, item, element, canCraft));
	}

	// Token: 0x060007F9 RID: 2041 RVA: 0x00046134 File Offset: 0x00044334
	private void OnSelectedRecipe(GameObject button)
	{
		int index = this.FindSelectedRecipe(button);
		this.SetRecipe(index, false);
	}

	// Token: 0x060007FA RID: 2042 RVA: 0x00046154 File Offset: 0x00044354
	private void UpdateRecipeGamepadInput()
	{
		if (this.m_availableRecipes.Count > 0)
		{
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				this.SetRecipe(Mathf.Min(this.m_availableRecipes.Count - 1, this.GetSelectedRecipeIndex(false) + 1), true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				this.SetRecipe(Mathf.Max(0, this.GetSelectedRecipeIndex(false) - 1), true);
			}
		}
	}

	// Token: 0x060007FB RID: 2043 RVA: 0x000461D8 File Offset: 0x000443D8
	private int GetSelectedRecipeIndex(bool acceptOneLevelHigher = false)
	{
		for (int i = 0; i < this.m_availableRecipes.Count; i++)
		{
			if (this.m_availableRecipes[i].Recipe == this.m_selectedRecipe.Recipe && this.m_availableRecipes[i].ItemData == this.m_selectedRecipe.ItemData)
			{
				return i;
			}
		}
		if (acceptOneLevelHigher && this.m_selectedRecipe.ItemData != null)
		{
			for (int j = 0; j < this.m_availableRecipes.Count; j++)
			{
				if (this.m_availableRecipes[j].Recipe == this.m_selectedRecipe.Recipe && this.<GetSelectedRecipeIndex>g__isOneLevelHigher|34_0(this.m_availableRecipes[j].ItemData, this.m_selectedRecipe.ItemData) && this.m_availableRecipes[j].ItemData.m_gridPos == this.m_selectedRecipe.ItemData.m_gridPos)
				{
					return j;
				}
			}
			for (int k = 0; k < this.m_availableRecipes.Count; k++)
			{
				if (this.m_availableRecipes[k].Recipe == this.m_selectedRecipe.Recipe && this.<GetSelectedRecipeIndex>g__isOneLevelHigher|34_0(this.m_availableRecipes[k].ItemData, this.m_selectedRecipe.ItemData))
				{
					return k;
				}
			}
		}
		return 0;
	}

	// Token: 0x060007FC RID: 2044 RVA: 0x0004635C File Offset: 0x0004455C
	private void SetRecipe(int index, bool center)
	{
		ZLog.Log("Setting selected recipe " + index.ToString());
		for (int i = 0; i < this.m_availableRecipes.Count; i++)
		{
			bool active = i == index;
			this.m_availableRecipes[i].InterfaceElement.transform.Find("selected").gameObject.SetActive(active);
		}
		if (center && index >= 0)
		{
			this.m_recipeEnsureVisible.CenterOnItem(this.m_availableRecipes[index].InterfaceElement.transform as RectTransform);
		}
		if (index < 0)
		{
			this.m_selectedRecipe = default(InventoryGui.RecipeDataPair);
			this.m_selectedVariant = 0;
			return;
		}
		InventoryGui.RecipeDataPair selectedRecipe = this.m_availableRecipes[index];
		if (selectedRecipe.Recipe != this.m_selectedRecipe.Recipe || selectedRecipe.ItemData != this.m_selectedRecipe.ItemData)
		{
			this.m_selectedRecipe = selectedRecipe;
			this.m_selectedVariant = 0;
		}
	}

	// Token: 0x060007FD RID: 2045 RVA: 0x00046458 File Offset: 0x00044658
	private void UpdateRecipe(Player player, float dt)
	{
		CraftingStation currentCraftingStation = player.GetCurrentCraftingStation();
		if (currentCraftingStation)
		{
			this.m_craftingStationName.text = Localization.instance.Localize(currentCraftingStation.m_name);
			this.m_craftingStationIcon.gameObject.SetActive(true);
			this.m_craftingStationIcon.sprite = currentCraftingStation.m_icon;
			int level = currentCraftingStation.GetLevel(true);
			this.m_craftingStationLevel.text = level.ToString();
			this.m_craftingStationLevelRoot.gameObject.SetActive(true);
		}
		else
		{
			this.m_craftingStationName.text = Localization.instance.Localize("$hud_crafting");
			this.m_craftingStationIcon.gameObject.SetActive(false);
			this.m_craftingStationLevelRoot.gameObject.SetActive(false);
		}
		if (this.m_selectedRecipe.Recipe)
		{
			this.m_recipeIcon.enabled = true;
			this.m_recipeName.enabled = true;
			this.m_recipeDecription.enabled = true;
			ItemDrop.ItemData itemData = this.m_selectedRecipe.ItemData;
			int num = (itemData != null) ? (itemData.m_quality + 1) : 1;
			bool flag = num <= this.m_selectedRecipe.Recipe.m_item.m_itemData.m_shared.m_maxQuality;
			bool flag2 = itemData == null && (ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyLStick"));
			int num2 = (itemData != null) ? itemData.m_variant : this.m_selectedVariant;
			this.m_recipeIcon.sprite = this.m_selectedRecipe.Recipe.m_item.m_itemData.m_shared.m_icons[num2];
			string text = Localization.instance.Localize(this.m_selectedRecipe.Recipe.m_item.m_itemData.m_shared.m_name);
			int num3 = flag2 ? this.m_multiCraftAmount : 1;
			int num4 = this.m_selectedRecipe.Recipe.m_amount * num3;
			if (this.m_selectedRecipe.Recipe.m_amount > 1)
			{
				text = string.Format("{0} x{1}", text, num4);
			}
			this.m_recipeName.text = text;
			this.m_recipeDecription.text = Localization.instance.Localize(ItemDrop.ItemData.GetTooltip(this.m_selectedRecipe.Recipe.m_item.m_itemData, num, true, (float)Game.m_worldLevel, num4));
			if (this.m_selectedRecipe.Recipe.m_requireOnlyOneIngredient)
			{
				TMP_Text recipeDecription = this.m_recipeDecription;
				recipeDecription.text += Localization.instance.Localize("\n\n<color=orange>$inventory_onlyoneingredient</color>");
			}
			if (itemData != null)
			{
				this.m_itemCraftType.gameObject.SetActive(true);
				if (itemData.m_quality >= itemData.m_shared.m_maxQuality)
				{
					this.m_itemCraftType.text = Localization.instance.Localize("$inventory_maxquality");
				}
				else
				{
					string text2 = Localization.instance.Localize(itemData.m_shared.m_name);
					this.m_itemCraftType.text = Localization.instance.Localize("$inventory_upgrade", new string[]
					{
						text2,
						(itemData.m_quality + 1).ToString()
					});
				}
			}
			else
			{
				this.m_itemCraftType.gameObject.SetActive(false);
			}
			this.m_variantButton.gameObject.SetActive(this.m_selectedRecipe.Recipe.m_item.m_itemData.m_shared.m_variants > 1 && this.m_selectedRecipe.ItemData == null);
			this.SetupRequirementList(num, player, flag, num3);
			int requiredStationLevel = this.m_selectedRecipe.Recipe.GetRequiredStationLevel(num);
			CraftingStation requiredStation = this.m_selectedRecipe.Recipe.GetRequiredStation(num);
			if (requiredStation != null && flag)
			{
				this.m_minStationLevelIcon.gameObject.SetActive(true);
				this.m_minStationLevelText.text = requiredStationLevel.ToString();
				if (currentCraftingStation == null || currentCraftingStation.GetLevel(true) < requiredStationLevel)
				{
					this.m_minStationLevelText.color = ((Mathf.Sin(Time.time * 10f) > 0f && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost)) ? Color.red : this.m_minStationLevelBasecolor);
				}
				else
				{
					this.m_minStationLevelText.color = this.m_minStationLevelBasecolor;
				}
			}
			else
			{
				this.m_minStationLevelIcon.gameObject.SetActive(false);
			}
			bool flag3 = player.HaveRequirements(this.m_selectedRecipe.Recipe, false, num, flag2 ? this.m_multiCraftAmount : 1);
			bool flag4 = true;
			bool flag5 = !requiredStation || (currentCraftingStation && currentCraftingStation.CheckUsable(player, false));
			this.m_craftButton.interactable = (((flag3 && flag5) || player.NoCostCheat() || (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost) && flag5)) && flag4 && flag);
			TMP_Text componentInChildren = this.m_craftButton.GetComponentInChildren<TMP_Text>();
			if (num > 1)
			{
				componentInChildren.text = Localization.instance.Localize("$inventory_upgradebutton");
			}
			else
			{
				componentInChildren.text = Localization.instance.Localize("$inventory_craftbutton");
				if (flag2)
				{
					TMP_Text tmp_Text = componentInChildren;
					tmp_Text.text = tmp_Text.text + " x " + this.m_multiCraftAmount.ToString();
				}
			}
			UITooltip component = this.m_craftButton.GetComponent<UITooltip>();
			if (!flag4)
			{
				component.m_text = Localization.instance.Localize("$inventory_full");
			}
			else if (!flag3 && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost))
			{
				component.m_text = Localization.instance.Localize("$msg_missingrequirement");
			}
			else if (!flag5)
			{
				component.m_text = Localization.instance.Localize("$msg_missingstation");
			}
			else
			{
				component.m_text = "";
			}
		}
		else
		{
			this.m_recipeIcon.enabled = false;
			this.m_recipeName.enabled = false;
			this.m_recipeDecription.enabled = false;
			this.m_qualityPanel.gameObject.SetActive(false);
			this.m_minStationLevelIcon.gameObject.SetActive(false);
			this.m_craftButton.GetComponent<UITooltip>().m_text = "";
			this.m_variantButton.gameObject.SetActive(false);
			this.m_itemCraftType.gameObject.SetActive(false);
			for (int i = 0; i < this.m_recipeRequirementList.Length; i++)
			{
				InventoryGui.HideRequirement(this.m_recipeRequirementList[i].transform);
			}
			this.m_craftButton.interactable = false;
		}
		if (this.m_craftTimer < 0f)
		{
			this.m_craftProgressPanel.gameObject.SetActive(false);
			this.m_craftButton.gameObject.SetActive(true);
			return;
		}
		float num5 = this.m_multiCrafting ? this.m_multiCraftDuration : this.m_craftDuration;
		if (currentCraftingStation != null && currentCraftingStation.m_craftingSkill != Skills.SkillType.None)
		{
			num5 *= 1f - Player.m_localPlayer.GetSkillFactor(currentCraftingStation.m_craftingSkill) * this.m_craftDurationSkillMaxDecrease;
		}
		this.m_craftButton.gameObject.SetActive(false);
		this.m_craftProgressPanel.gameObject.SetActive(true);
		this.m_craftProgressBar.SetMaxValue(num5);
		this.m_craftProgressBar.SetValue(this.m_craftTimer);
		this.m_craftTimer += dt;
		if (this.m_craftTimer >= num5)
		{
			this.DoCrafting(player);
			this.m_craftTimer = -1f;
		}
	}

	// Token: 0x060007FE RID: 2046 RVA: 0x00046BAC File Offset: 0x00044DAC
	private void SetupRequirementList(int quality, Player player, bool allowedQuality, int amount)
	{
		int i = 0;
		int num = this.m_recipeRequirementList.Length;
		Piece.Requirement[] resources = this.m_selectedRecipe.Recipe.m_resources;
		this.m_reqList.Clear();
		if (this.m_selectedRecipe.Recipe.m_requireOnlyOneIngredient)
		{
			this.m_reqList.Clear();
			foreach (Piece.Requirement requirement in resources)
			{
				if (player.IsKnownMaterial(requirement.m_resItem.m_itemData.m_shared.m_name) && requirement.GetAmount(quality) > 0)
				{
					this.m_reqList.Add(requirement);
				}
			}
		}
		else
		{
			foreach (Piece.Requirement requirement2 in resources)
			{
				if (requirement2.GetAmount(quality) > 0)
				{
					this.m_reqList.Add(requirement2);
				}
			}
		}
		int num2 = 0;
		if (this.m_reqList.Count > 4)
		{
			int num3 = (int)Mathf.Ceil((float)this.m_reqList.Count / (float)num);
			num2 = (int)Time.fixedTime % num3 * num;
		}
		if (allowedQuality)
		{
			for (int k = num2; k < this.m_reqList.Count; k++)
			{
				if (InventoryGui.SetupRequirement(this.m_recipeRequirementList[i].transform, this.m_reqList[k], player, true, quality, amount))
				{
					i++;
				}
				if (i >= this.m_recipeRequirementList.Length)
				{
					break;
				}
			}
		}
		while (i < num)
		{
			InventoryGui.HideRequirement(this.m_recipeRequirementList[i].transform);
			i++;
		}
	}

	// Token: 0x060007FF RID: 2047 RVA: 0x00046D28 File Offset: 0x00044F28
	private void SetupUpgradeItem(Recipe recipe, ItemDrop.ItemData item)
	{
		if (item == null)
		{
			this.m_upgradeItemIcon.sprite = recipe.m_item.m_itemData.m_shared.m_icons[this.m_selectedVariant];
			this.m_upgradeItemName.text = Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name);
			this.m_upgradeItemNextQuality.text = ((recipe.m_item.m_itemData.m_shared.m_maxQuality > 1) ? "1" : "");
			this.m_itemCraftType.text = Localization.instance.Localize("$inventory_new");
			this.m_upgradeItemDurability.gameObject.SetActive(recipe.m_item.m_itemData.m_shared.m_useDurability);
			if (recipe.m_item.m_itemData.m_shared.m_useDurability)
			{
				this.m_upgradeItemDurability.SetValue(1f);
				return;
			}
		}
		else
		{
			this.m_upgradeItemIcon.sprite = item.GetIcon();
			this.m_upgradeItemName.text = Localization.instance.Localize(item.m_shared.m_name);
			this.m_upgradeItemNextQuality.text = item.m_quality.ToString();
			this.m_upgradeItemDurability.gameObject.SetActive(item.m_shared.m_useDurability);
			if (item.m_shared.m_useDurability)
			{
				this.m_upgradeItemDurability.SetValue(item.GetDurabilityPercentage());
			}
			if (item.m_quality >= item.m_shared.m_maxQuality)
			{
				this.m_itemCraftType.text = Localization.instance.Localize("$inventory_maxquality");
				return;
			}
			this.m_itemCraftType.text = Localization.instance.Localize("$inventory_upgrade");
		}
	}

	// Token: 0x06000800 RID: 2048 RVA: 0x00046EF0 File Offset: 0x000450F0
	public static bool SetupRequirement(Transform elementRoot, Piece.Requirement req, Player player, bool craft, int quality, int craftMultiplier = 1)
	{
		Image component = elementRoot.transform.Find("res_icon").GetComponent<Image>();
		TMP_Text component2 = elementRoot.transform.Find("res_name").GetComponent<TMP_Text>();
		TMP_Text component3 = elementRoot.transform.Find("res_amount").GetComponent<TMP_Text>();
		UITooltip component4 = elementRoot.GetComponent<UITooltip>();
		if (req.m_resItem != null)
		{
			component.gameObject.SetActive(true);
			component2.gameObject.SetActive(true);
			component3.gameObject.SetActive(true);
			component.sprite = req.m_resItem.m_itemData.GetIcon();
			component.color = Color.white;
			component4.m_text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
			component2.text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
			int num = player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name, -1, true);
			int num2 = req.GetAmount(quality) * craftMultiplier;
			if (num2 <= 0)
			{
				InventoryGui.HideRequirement(elementRoot);
				return false;
			}
			component3.text = num2.ToString();
			if (num < num2 && ((!craft && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost)) || (craft && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost))))
			{
				component3.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : Color.white);
			}
			else
			{
				component3.color = Color.white;
			}
		}
		return true;
	}

	// Token: 0x06000801 RID: 2049 RVA: 0x00047090 File Offset: 0x00045290
	public static void HideRequirement(Transform elementRoot)
	{
		Image component = elementRoot.transform.Find("res_icon").GetComponent<Image>();
		TMP_Text component2 = elementRoot.transform.Find("res_name").GetComponent<TMP_Text>();
		Component component3 = elementRoot.transform.Find("res_amount").GetComponent<TMP_Text>();
		elementRoot.GetComponent<UITooltip>().m_text = "";
		component.gameObject.SetActive(false);
		component2.gameObject.SetActive(false);
		component3.gameObject.SetActive(false);
	}

	// Token: 0x06000802 RID: 2050 RVA: 0x00047114 File Offset: 0x00045314
	private void DoCrafting(Player player)
	{
		if (this.m_craftRecipe == null)
		{
			return;
		}
		int num = (this.m_craftUpgradeItem != null) ? (this.m_craftUpgradeItem.m_quality + 1) : 1;
		if (num > this.m_craftRecipe.m_item.m_itemData.m_shared.m_maxQuality)
		{
			return;
		}
		int num2 = this.m_multiCrafting ? this.m_multiCraftAmount : 1;
		int amount;
		ItemDrop.ItemData itemData;
		int num3 = this.m_craftRecipe.GetAmount(num, out amount, out itemData, num2);
		if (!player.HaveRequirements(this.m_craftRecipe, false, num, num2) && !player.NoCostCheat() && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost))
		{
			return;
		}
		if (this.m_craftUpgradeItem != null && !player.GetInventory().ContainsItem(this.m_craftUpgradeItem))
		{
			return;
		}
		if (this.m_craftRecipe.m_requireOnlyOneIngredient && itemData == null)
		{
			return;
		}
		CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
		int num4 = 0;
		if (currentCraftingStation != null && currentCraftingStation.m_craftingSkill != Skills.SkillType.None && this.m_craftRecipe.m_item.m_itemData.m_shared.m_maxStackSize > 1)
		{
			float skillFactor = Player.m_localPlayer.GetSkillFactor(currentCraftingStation.m_craftingSkill);
			for (int i = 0; i < num2; i++)
			{
				if (UnityEngine.Random.value < skillFactor * this.m_craftBonusChance)
				{
					num4 += this.m_craftBonusAmount;
					num3 += num4;
				}
			}
		}
		if (this.m_craftUpgradeItem == null && !player.GetInventory().CanAddItem(this.m_craftRecipe.m_item.gameObject, num3))
		{
			return;
		}
		if (this.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(this.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc))
		{
			player.Message(MessageHud.MessageType.Center, "$msg_dlcrequired", 0, null);
			return;
		}
		int variant = this.m_craftVariant;
		Vector2i gridPos = new Vector2i(-1, -1);
		if (this.m_craftUpgradeItem != null)
		{
			gridPos = this.m_craftUpgradeItem.m_gridPos;
			variant = this.m_craftUpgradeItem.m_variant;
			player.UnequipItem(this.m_craftUpgradeItem, true);
			player.GetInventory().RemoveItem(this.m_craftUpgradeItem);
		}
		long playerID = player.GetPlayerID();
		string playerName = player.GetPlayerName();
		if (player.GetInventory().AddItem(this.m_craftRecipe.m_item.gameObject.name, num3, num, variant, playerID, playerName, gridPos, false) != null)
		{
			if (!player.NoCostCheat() && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost))
			{
				int multiplier = this.m_multiCrafting ? this.m_multiCraftAmount : 1;
				if (itemData != null)
				{
					player.GetInventory().RemoveItem(itemData.m_shared.m_name, amount, itemData.m_quality, true);
				}
				else
				{
					player.ConsumeResources(this.m_craftRecipe.m_resources, num, -1, multiplier);
				}
			}
			this.UpdateCraftingPanel(false);
			if (this.m_craftRecipe.m_craftingStation != null && this.m_craftRecipe.m_craftingStation.m_craftingSkill != Skills.SkillType.None)
			{
				Player.m_localPlayer.RaiseSkill(this.m_craftRecipe.m_craftingStation.m_craftingSkill, (float)(this.m_multiCrafting ? this.m_multiCraftAmount : 1));
			}
			if (num4 > 0)
			{
				Vector3 vector = (currentCraftingStation ? currentCraftingStation.transform.position : Player.m_localPlayer.transform.position) + Vector3.up;
				DamageText.instance.ShowText(DamageText.TextType.Bonus, vector, string.Format("+{0}", num4), true);
				this.m_craftBonusEffect.Create(vector, Quaternion.identity, null, 1f, -1);
				ZLog.Log(string.Format("Bonus craft x{0}!", num4));
			}
		}
		if (currentCraftingStation)
		{
			currentCraftingStation.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity, null, 1f, -1);
		}
		else
		{
			this.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity, null, 1f, -1);
		}
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		playerProfile.IncrementStat(PlayerStatType.CraftsOrUpgrades, 1f);
		if (this.m_craftUpgradeItem == null)
		{
			playerProfile.IncrementStat(PlayerStatType.Crafts, 1f);
			playerProfile.m_itemCraftStats.IncrementOrSet(this.m_craftRecipe.m_item.m_itemData.m_shared.m_name, 1f);
		}
		else
		{
			playerProfile.IncrementStat(PlayerStatType.Upgrades, 1f);
		}
		Gogan.LogEvent("Game", "Crafted", this.m_craftRecipe.m_item.m_itemData.m_shared.m_name, (long)num);
	}

	// Token: 0x06000803 RID: 2051 RVA: 0x000475A0 File Offset: 0x000457A0
	private int FindSelectedRecipe(GameObject button)
	{
		for (int i = 0; i < this.m_availableRecipes.Count; i++)
		{
			if (this.m_availableRecipes[i].InterfaceElement == button)
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x06000804 RID: 2052 RVA: 0x000475E2 File Offset: 0x000457E2
	private void OnCraftCancelPressed()
	{
		if (this.m_craftTimer >= 0f)
		{
			this.m_craftTimer = -1f;
		}
	}

	// Token: 0x06000805 RID: 2053 RVA: 0x000475FC File Offset: 0x000457FC
	private void OnCraftPressed()
	{
		if (!this.m_selectedRecipe.Recipe)
		{
			return;
		}
		this.m_craftRecipe = this.m_selectedRecipe.Recipe;
		this.m_craftUpgradeItem = this.m_selectedRecipe.ItemData;
		this.m_craftVariant = this.m_selectedVariant;
		this.m_multiCrafting = (this.m_craftUpgradeItem == null && (ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyLStick")));
		int quality = (this.m_craftUpgradeItem != null) ? (this.m_craftUpgradeItem.m_quality + 1) : 1;
		int num;
		ItemDrop.ItemData itemData;
		int amount = this.m_craftRecipe.GetAmount(quality, out num, out itemData, this.m_multiCrafting ? this.m_multiCraftAmount : 1);
		if (this.m_craftUpgradeItem == null && !Player.m_localPlayer.GetInventory().CanAddItem(this.m_craftRecipe.m_item.gameObject, amount))
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$inventory_full", 0, null);
			return;
		}
		this.m_craftTimer = 0f;
		if (this.m_craftRecipe.m_craftingStation)
		{
			CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
			if (currentCraftingStation)
			{
				currentCraftingStation.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity, null, 1f, -1);
				return;
			}
		}
		else
		{
			this.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity, null, 1f, -1);
		}
	}

	// Token: 0x06000806 RID: 2054 RVA: 0x0004776E File Offset: 0x0004596E
	private void OnRepairPressed()
	{
		this.RepairOneItem();
		this.UpdateRepair();
		this.UpdateCraftingPanel(false);
	}

	// Token: 0x06000807 RID: 2055 RVA: 0x00047784 File Offset: 0x00045984
	private void UpdateRepair()
	{
		if (Player.m_localPlayer.GetCurrentCraftingStation() == null && !Player.m_localPlayer.NoCostCheat())
		{
			this.m_repairPanel.gameObject.SetActive(false);
			this.m_repairPanelSelection.gameObject.SetActive(false);
			this.m_repairButton.gameObject.SetActive(false);
			return;
		}
		this.m_repairButton.gameObject.SetActive(true);
		this.m_repairPanel.gameObject.SetActive(true);
		this.m_repairPanelSelection.gameObject.SetActive(true);
		if (this.HaveRepairableItems())
		{
			this.m_repairButton.interactable = true;
			this.m_repairButtonGlow.gameObject.SetActive(true);
			Color color = this.m_repairButtonGlow.color;
			color.a = 0.5f + Mathf.Sin(Time.time * 5f) * 0.5f;
			this.m_repairButtonGlow.color = color;
			return;
		}
		this.m_repairButton.interactable = false;
		this.m_repairButtonGlow.gameObject.SetActive(false);
	}

	// Token: 0x06000808 RID: 2056 RVA: 0x00047894 File Offset: 0x00045A94
	private void RepairOneItem()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
		if (currentCraftingStation == null && !Player.m_localPlayer.NoCostCheat())
		{
			return;
		}
		if (currentCraftingStation && !currentCraftingStation.CheckUsable(Player.m_localPlayer, false))
		{
			return;
		}
		this.m_tempWornItems.Clear();
		Player.m_localPlayer.GetInventory().GetWornItems(this.m_tempWornItems);
		foreach (ItemDrop.ItemData itemData in this.m_tempWornItems)
		{
			if (this.CanRepair(itemData))
			{
				Player.m_localPlayer.RaiseSkill(Skills.SkillType.Crafting, 1f - itemData.m_durability / itemData.GetMaxDurability());
				itemData.m_durability = itemData.GetMaxDurability();
				if (currentCraftingStation)
				{
					currentCraftingStation.m_repairItemDoneEffects.Create(currentCraftingStation.transform.position, Quaternion.identity, null, 1f, -1);
				}
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_repaired", new string[]
				{
					itemData.m_shared.m_name
				}), 0, null);
				return;
			}
		}
		Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No more item to repair", 0, null);
	}

	// Token: 0x06000809 RID: 2057 RVA: 0x000479F4 File Offset: 0x00045BF4
	private bool HaveRepairableItems()
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
		if (currentCraftingStation == null && !Player.m_localPlayer.NoCostCheat())
		{
			return false;
		}
		if (currentCraftingStation && !currentCraftingStation.CheckUsable(Player.m_localPlayer, false))
		{
			return false;
		}
		this.m_tempWornItems.Clear();
		Player.m_localPlayer.GetInventory().GetWornItems(this.m_tempWornItems);
		foreach (ItemDrop.ItemData item in this.m_tempWornItems)
		{
			if (this.CanRepair(item))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x0600080A RID: 2058 RVA: 0x00047ABC File Offset: 0x00045CBC
	private bool CanRepair(ItemDrop.ItemData item)
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		if (!item.m_shared.m_canBeReparied)
		{
			return false;
		}
		if (Player.m_localPlayer.NoCostCheat())
		{
			return true;
		}
		CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
		if (currentCraftingStation == null)
		{
			return false;
		}
		Recipe recipe = ObjectDB.instance.GetRecipe(item);
		return !(recipe == null) && (!(recipe.m_craftingStation == null) || !(recipe.m_repairStation == null)) && ((recipe.m_repairStation != null && recipe.m_repairStation.m_name == currentCraftingStation.m_name) || (recipe.m_craftingStation != null && recipe.m_craftingStation.m_name == currentCraftingStation.m_name) || item.m_worldLevel < Game.m_worldLevel) && Mathf.Min(currentCraftingStation.GetLevel(true), 4) >= recipe.m_minStationLevel;
	}

	// Token: 0x0600080B RID: 2059 RVA: 0x00047BB4 File Offset: 0x00045DB4
	private void SetupDragItem(ItemDrop.ItemData item, Inventory inventory, int amount)
	{
		if (this.m_dragGo)
		{
			UnityEngine.Object.Destroy(this.m_dragGo);
			this.m_dragGo = null;
			this.m_dragItem = null;
			this.m_dragInventory = null;
			this.m_dragAmount = 0;
		}
		if (item != null)
		{
			this.m_dragGo = UnityEngine.Object.Instantiate<GameObject>(this.m_dragItemPrefab, base.transform);
			this.m_dragItem = item;
			this.m_dragInventory = inventory;
			this.m_dragAmount = amount;
			this.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
			UITooltip.HideTooltip();
		}
	}

	// Token: 0x0600080C RID: 2060 RVA: 0x00047C4C File Offset: 0x00045E4C
	private void ShowSplitDialog(ItemDrop.ItemData item, Inventory fromIventory)
	{
		bool flag = ZInput.GetKey(KeyCode.LeftControl, true) || ZInput.GetKey(KeyCode.RightControl, true);
		this.m_splitSlider.minValue = 1f;
		this.m_splitSlider.maxValue = (float)item.m_stack;
		if (!flag)
		{
			this.m_splitSlider.value = (float)Mathf.CeilToInt((float)item.m_stack / 2f);
		}
		else if (this.m_splitSlider.value / (float)item.m_stack > 0.5f)
		{
			this.m_splitSlider.value = Mathf.Min(this.m_splitSlider.value, (float)item.m_stack);
		}
		this.m_splitIcon.sprite = item.GetIcon();
		this.m_splitIconName.text = Localization.instance.Localize(item.m_shared.m_name);
		this.m_splitPanel.gameObject.SetActive(true);
		this.m_splitItem = item;
		this.m_splitInventory = fromIventory;
		this.OnSplitSliderChanged(this.m_splitSlider.value);
	}

	// Token: 0x0600080D RID: 2061 RVA: 0x00047D58 File Offset: 0x00045F58
	private void OnSplitSliderChanged(float value)
	{
		this.m_splitAmount.text = ((int)value).ToString() + "/" + ((int)this.m_splitSlider.maxValue).ToString();
	}

	// Token: 0x0600080E RID: 2062 RVA: 0x00047D98 File Offset: 0x00045F98
	private void UpdateSplitDialog()
	{
		if (this.m_splitSlider.gameObject.activeInHierarchy)
		{
			for (int i = 0; i < 10; i++)
			{
				if (ZInput.GetKeyDown(KeyCode.Keypad0 + i, true) || ZInput.GetKeyDown(KeyCode.Alpha0 + i, true))
				{
					if (this.m_lastSplitInput + TimeSpan.FromSeconds((double)this.m_splitNumInputTimeoutSec) < DateTime.Now)
					{
						this.m_splitInput = "";
					}
					this.m_lastSplitInput = DateTime.Now;
					this.m_splitInput += i.ToString();
					int num;
					if (int.TryParse(this.m_splitInput, out num))
					{
						this.m_splitSlider.value = Mathf.Clamp((float)num, 1f, this.m_splitSlider.maxValue);
						this.OnSplitSliderChanged(this.m_splitSlider.value);
					}
				}
			}
			if (ZInput.GetKeyDown(KeyCode.LeftArrow, true) && this.m_splitSlider.value > 1f)
			{
				this.m_splitSlider.value -= 1f;
				this.OnSplitSliderChanged(this.m_splitSlider.value);
			}
			if (ZInput.GetKeyDown(KeyCode.RightArrow, true) && this.m_splitSlider.value < this.m_splitSlider.maxValue)
			{
				this.m_splitSlider.value += 1f;
				this.OnSplitSliderChanged(this.m_splitSlider.value);
			}
			if (ZInput.GetKeyDown(KeyCode.KeypadEnter, true) || ZInput.GetKeyDown(KeyCode.Return, true))
			{
				this.OnSplitOk();
			}
		}
	}

	// Token: 0x0600080F RID: 2063 RVA: 0x00047F2F File Offset: 0x0004612F
	private void OnSplitCancel()
	{
		this.m_splitItem = null;
		this.m_splitInventory = null;
		this.m_splitPanel.gameObject.SetActive(false);
	}

	// Token: 0x06000810 RID: 2064 RVA: 0x00047F50 File Offset: 0x00046150
	private void OnSplitOk()
	{
		this.SetupDragItem(this.m_splitItem, this.m_splitInventory, (int)this.m_splitSlider.value);
		this.m_splitItem = null;
		this.m_splitInventory = null;
		this.m_splitPanel.gameObject.SetActive(false);
	}

	// Token: 0x06000811 RID: 2065 RVA: 0x00047F8F File Offset: 0x0004618F
	public void OnOpenSkills()
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		this.m_skillsDialog.Setup(Player.m_localPlayer);
		Gogan.LogEvent("Screen", "Enter", "Skills", 0L);
	}

	// Token: 0x06000812 RID: 2066 RVA: 0x00047FC4 File Offset: 0x000461C4
	public void OnOpenTexts()
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		this.m_textsDialog.Setup(Player.m_localPlayer);
		Gogan.LogEvent("Screen", "Enter", "Texts", 0L);
	}

	// Token: 0x06000813 RID: 2067 RVA: 0x00047FF9 File Offset: 0x000461F9
	public void OnOpenTrophies()
	{
		this.m_trophiesPanel.SetActive(true);
		this.UpdateTrophyList();
		Gogan.LogEvent("Screen", "Enter", "Trophies", 0L);
	}

	// Token: 0x06000814 RID: 2068 RVA: 0x00048023 File Offset: 0x00046223
	public void OnCloseTrophies()
	{
		this.m_trophiesPanel.SetActive(false);
	}

	// Token: 0x06000815 RID: 2069 RVA: 0x00048034 File Offset: 0x00046234
	private void UpdateTrophyList()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		foreach (GameObject obj in this.m_trophyList)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_trophyList.Clear();
		List<string> trophies = Player.m_localPlayer.GetTrophies();
		float num = 0f;
		for (int i = 0; i < trophies.Count; i++)
		{
			string text = trophies[i];
			GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(text);
			if (itemPrefab == null)
			{
				ZLog.LogWarning("Missing trophy prefab:" + text);
			}
			else
			{
				ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_trophieElementPrefab, this.m_trophieListRoot);
				gameObject.SetActive(true);
				RectTransform rectTransform = gameObject.transform as RectTransform;
				rectTransform.anchoredPosition = new Vector2((float)component.m_itemData.m_shared.m_trophyPos.x * this.m_trophieListSpace, (float)component.m_itemData.m_shared.m_trophyPos.y * -this.m_trophieListSpace);
				num = Mathf.Min(num, rectTransform.anchoredPosition.y - this.m_trophieListSpace);
				string text2 = Localization.instance.Localize(component.m_itemData.m_shared.m_name);
				if (text2.CustomEndsWith(" trophy"))
				{
					text2 = text2.Remove(text2.Length - 7);
				}
				rectTransform.Find("icon_bkg/icon").GetComponent<Image>().sprite = component.m_itemData.GetIcon();
				rectTransform.Find("name").GetComponent<TMP_Text>().text = text2;
				rectTransform.Find("description").GetComponent<TMP_Text>().text = Localization.instance.Localize(component.m_itemData.m_shared.m_name + "_lore");
				this.m_trophyList.Add(gameObject);
			}
		}
		ZLog.Log("SIZE " + num.ToString());
		float size = Mathf.Max(this.m_trophieListBaseSize, -num);
		this.m_trophieListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		this.m_trophyListScroll.value = 1f;
	}

	// Token: 0x06000816 RID: 2070 RVA: 0x00048298 File Offset: 0x00046498
	public void OnShowVariantSelection()
	{
		this.m_variantDialog.Setup(this.m_selectedRecipe.Recipe.m_item.m_itemData);
		Gogan.LogEvent("Screen", "Enter", "VariantSelection", 0L);
	}

	// Token: 0x06000817 RID: 2071 RVA: 0x000482D0 File Offset: 0x000464D0
	private void OnVariantSelected(int index)
	{
		ZLog.Log("Item variant selected " + index.ToString());
		this.m_selectedVariant = index;
	}

	// Token: 0x06000818 RID: 2072 RVA: 0x000482EF File Offset: 0x000464EF
	public bool InUpradeTab()
	{
		return !this.m_tabUpgrade.interactable;
	}

	// Token: 0x06000819 RID: 2073 RVA: 0x000482FF File Offset: 0x000464FF
	public bool InCraftTab()
	{
		return !this.m_tabCraft.interactable;
	}

	// Token: 0x0600081A RID: 2074 RVA: 0x0004830F File Offset: 0x0004650F
	public void OnTabCraftPressed()
	{
		this.m_tabCraft.interactable = false;
		this.m_tabUpgrade.interactable = true;
		this.UpdateCraftingPanel(false);
	}

	// Token: 0x0600081B RID: 2075 RVA: 0x00048330 File Offset: 0x00046530
	public void OnTabUpgradePressed()
	{
		this.m_tabCraft.interactable = true;
		this.m_tabUpgrade.interactable = false;
		this.UpdateCraftingPanel(false);
	}

	// Token: 0x1700002E RID: 46
	// (get) Token: 0x0600081C RID: 2076 RVA: 0x00048351 File Offset: 0x00046551
	public int ActiveGroup
	{
		get
		{
			return this.m_activeGroup;
		}
	}

	// Token: 0x1700002F RID: 47
	// (get) Token: 0x0600081D RID: 2077 RVA: 0x00048359 File Offset: 0x00046559
	public bool IsSkillsPanelOpen
	{
		get
		{
			return this.m_skillsDialog.gameObject.activeInHierarchy;
		}
	}

	// Token: 0x17000030 RID: 48
	// (get) Token: 0x0600081E RID: 2078 RVA: 0x0004836B File Offset: 0x0004656B
	public bool IsTextPanelOpen
	{
		get
		{
			return this.m_textsDialog.gameObject.activeInHierarchy;
		}
	}

	// Token: 0x17000031 RID: 49
	// (get) Token: 0x0600081F RID: 2079 RVA: 0x0004837D File Offset: 0x0004657D
	public bool IsTrophisPanelOpen
	{
		get
		{
			return this.m_trophiesPanel.activeInHierarchy;
		}
	}

	// Token: 0x17000032 RID: 50
	// (get) Token: 0x06000820 RID: 2080 RVA: 0x0004838A File Offset: 0x0004658A
	public InventoryGrid ContainerGrid
	{
		get
		{
			return this.m_containerGrid;
		}
	}

	// Token: 0x06000822 RID: 2082 RVA: 0x00048500 File Offset: 0x00046700
	[CompilerGenerated]
	internal static int <UpdateRecipeList>g__byCraftable|29_4(InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
	{
		return b.CanCraft.CompareTo(a.CanCraft);
	}

	// Token: 0x06000823 RID: 2083 RVA: 0x00048523 File Offset: 0x00046723
	[CompilerGenerated]
	internal static int <UpdateRecipeList>g__bySortWeight|29_5(InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
	{
		return a.Recipe.m_listSortWeight.CompareTo(b.Recipe.m_listSortWeight);
	}

	// Token: 0x06000824 RID: 2084 RVA: 0x00048544 File Offset: 0x00046744
	[CompilerGenerated]
	internal static int <UpdateRecipeList>g__byName|29_6(InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
	{
		return Localization.instance.Localize(a.Recipe.m_item.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(b.Recipe.m_item.m_itemData.m_shared.m_name));
	}

	// Token: 0x06000825 RID: 2085 RVA: 0x000485A0 File Offset: 0x000467A0
	[CompilerGenerated]
	internal static int <UpdateRecipeList>g__byLevel|29_7(InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
	{
		if (b.ItemData != null && a.ItemData != null)
		{
			return b.ItemData.m_quality.CompareTo(a.ItemData.m_quality);
		}
		return 0;
	}

	// Token: 0x06000826 RID: 2086 RVA: 0x000485D4 File Offset: 0x000467D4
	[CompilerGenerated]
	private bool <GetSelectedRecipeIndex>g__isOneLevelHigher|34_0(ItemDrop.ItemData available, ItemDrop.ItemData selected)
	{
		return available != null && available.m_quality == this.m_selectedRecipe.ItemData.m_quality + 1 && available.m_dropPrefab == this.m_selectedRecipe.ItemData.m_dropPrefab && available.m_variant == this.m_selectedRecipe.ItemData.m_variant && available.m_stack == this.m_selectedRecipe.ItemData.m_stack;
	}

	// Token: 0x04000962 RID: 2402
	private List<Recipe> m_tempRecipes = new List<Recipe>();

	// Token: 0x04000963 RID: 2403
	private List<ItemDrop.ItemData> m_tempItemList = new List<ItemDrop.ItemData>();

	// Token: 0x04000964 RID: 2404
	private List<ItemDrop.ItemData> m_tempWornItems = new List<ItemDrop.ItemData>();

	// Token: 0x04000965 RID: 2405
	private static InventoryGui m_instance;

	// Token: 0x04000966 RID: 2406
	[Header("Gamepad")]
	public UIGroupHandler m_inventoryGroup;

	// Token: 0x04000967 RID: 2407
	public UIGroupHandler[] m_uiGroups = new UIGroupHandler[0];

	// Token: 0x04000968 RID: 2408
	private int m_activeGroup = 1;

	// Token: 0x04000969 RID: 2409
	[SerializeField]
	private bool m_inventoryGroupCycling;

	// Token: 0x0400096A RID: 2410
	[Header("Other")]
	public Transform m_inventoryRoot;

	// Token: 0x0400096B RID: 2411
	public RectTransform m_player;

	// Token: 0x0400096C RID: 2412
	public RectTransform m_crafting;

	// Token: 0x0400096D RID: 2413
	public RectTransform m_info;

	// Token: 0x0400096E RID: 2414
	public RectTransform m_container;

	// Token: 0x0400096F RID: 2415
	public GameObject m_dragItemPrefab;

	// Token: 0x04000970 RID: 2416
	public TMP_Text m_containerName;

	// Token: 0x04000971 RID: 2417
	public Button m_dropButton;

	// Token: 0x04000972 RID: 2418
	public Button m_takeAllButton;

	// Token: 0x04000973 RID: 2419
	public Button m_stackAllButton;

	// Token: 0x04000974 RID: 2420
	public float m_autoCloseDistance = 4f;

	// Token: 0x04000975 RID: 2421
	[Header("Crafting dialog")]
	public Button m_tabCraft;

	// Token: 0x04000976 RID: 2422
	public Button m_tabUpgrade;

	// Token: 0x04000977 RID: 2423
	public GameObject m_recipeElementPrefab;

	// Token: 0x04000978 RID: 2424
	public RectTransform m_recipeListRoot;

	// Token: 0x04000979 RID: 2425
	public Scrollbar m_recipeListScroll;

	// Token: 0x0400097A RID: 2426
	public float m_recipeListSpace = 30f;

	// Token: 0x0400097B RID: 2427
	public float m_craftBonusChance = 0.25f;

	// Token: 0x0400097C RID: 2428
	public int m_craftBonusAmount = 1;

	// Token: 0x0400097D RID: 2429
	public EffectList m_craftBonusEffect;

	// Token: 0x0400097E RID: 2430
	public float m_craftDurationSkillMaxDecrease = 0.6f;

	// Token: 0x0400097F RID: 2431
	public float m_craftDuration = 2f;

	// Token: 0x04000980 RID: 2432
	public int m_multiCraftAmount = 5;

	// Token: 0x04000981 RID: 2433
	public float m_multiCraftDuration = 6f;

	// Token: 0x04000982 RID: 2434
	public TMP_Text m_craftingStationName;

	// Token: 0x04000983 RID: 2435
	public Image m_craftingStationIcon;

	// Token: 0x04000984 RID: 2436
	public RectTransform m_craftingStationLevelRoot;

	// Token: 0x04000985 RID: 2437
	public TMP_Text m_craftingStationLevel;

	// Token: 0x04000986 RID: 2438
	public TMP_Text m_recipeName;

	// Token: 0x04000987 RID: 2439
	public TMP_Text m_recipeDecription;

	// Token: 0x04000988 RID: 2440
	public Image m_recipeIcon;

	// Token: 0x04000989 RID: 2441
	public GameObject[] m_recipeRequirementList = new GameObject[0];

	// Token: 0x0400098A RID: 2442
	public Button m_variantButton;

	// Token: 0x0400098B RID: 2443
	public Button m_craftButton;

	// Token: 0x0400098C RID: 2444
	public Button m_craftCancelButton;

	// Token: 0x0400098D RID: 2445
	public Transform m_craftProgressPanel;

	// Token: 0x0400098E RID: 2446
	public GuiBar m_craftProgressBar;

	// Token: 0x0400098F RID: 2447
	[Header("Repair")]
	public Button m_repairButton;

	// Token: 0x04000990 RID: 2448
	public Transform m_repairPanel;

	// Token: 0x04000991 RID: 2449
	public Image m_repairButtonGlow;

	// Token: 0x04000992 RID: 2450
	public Transform m_repairPanelSelection;

	// Token: 0x04000993 RID: 2451
	[Header("Upgrade")]
	public Image m_upgradeItemIcon;

	// Token: 0x04000994 RID: 2452
	public GuiBar m_upgradeItemDurability;

	// Token: 0x04000995 RID: 2453
	public TMP_Text m_upgradeItemName;

	// Token: 0x04000996 RID: 2454
	public TMP_Text m_upgradeItemQuality;

	// Token: 0x04000997 RID: 2455
	public GameObject m_upgradeItemQualityArrow;

	// Token: 0x04000998 RID: 2456
	public TMP_Text m_upgradeItemNextQuality;

	// Token: 0x04000999 RID: 2457
	public TMP_Text m_upgradeItemIndex;

	// Token: 0x0400099A RID: 2458
	public TMP_Text m_itemCraftType;

	// Token: 0x0400099B RID: 2459
	public RectTransform m_qualityPanel;

	// Token: 0x0400099C RID: 2460
	public Button m_qualityLevelDown;

	// Token: 0x0400099D RID: 2461
	public Button m_qualityLevelUp;

	// Token: 0x0400099E RID: 2462
	public TMP_Text m_qualityLevel;

	// Token: 0x0400099F RID: 2463
	public Image m_minStationLevelIcon;

	// Token: 0x040009A0 RID: 2464
	private Color m_minStationLevelBasecolor;

	// Token: 0x040009A1 RID: 2465
	public TMP_Text m_minStationLevelText;

	// Token: 0x040009A2 RID: 2466
	public ScrollRectEnsureVisible m_recipeEnsureVisible;

	// Token: 0x040009A3 RID: 2467
	[Header("Variants dialog")]
	public VariantDialog m_variantDialog;

	// Token: 0x040009A4 RID: 2468
	[Header("Skills dialog")]
	public SkillsDialog m_skillsDialog;

	// Token: 0x040009A5 RID: 2469
	[Header("Texts dialog")]
	public TextsDialog m_textsDialog;

	// Token: 0x040009A6 RID: 2470
	[Header("Split dialog")]
	public Transform m_splitPanel;

	// Token: 0x040009A7 RID: 2471
	public Slider m_splitSlider;

	// Token: 0x040009A8 RID: 2472
	public TMP_Text m_splitAmount;

	// Token: 0x040009A9 RID: 2473
	public Button m_splitCancelButton;

	// Token: 0x040009AA RID: 2474
	public Button m_splitOkButton;

	// Token: 0x040009AB RID: 2475
	public Image m_splitIcon;

	// Token: 0x040009AC RID: 2476
	public TMP_Text m_splitIconName;

	// Token: 0x040009AD RID: 2477
	[Header("Character stats")]
	public Transform m_infoPanel;

	// Token: 0x040009AE RID: 2478
	public TMP_Text m_playerName;

	// Token: 0x040009AF RID: 2479
	public TMP_Text m_armor;

	// Token: 0x040009B0 RID: 2480
	public TMP_Text m_weight;

	// Token: 0x040009B1 RID: 2481
	public TMP_Text m_containerWeight;

	// Token: 0x040009B2 RID: 2482
	public Toggle m_pvp;

	// Token: 0x040009B3 RID: 2483
	[Header("Trophies")]
	public GameObject m_trophiesPanel;

	// Token: 0x040009B4 RID: 2484
	public RectTransform m_trophieListRoot;

	// Token: 0x040009B5 RID: 2485
	public float m_trophieListSpace = 30f;

	// Token: 0x040009B6 RID: 2486
	public GameObject m_trophieElementPrefab;

	// Token: 0x040009B7 RID: 2487
	public Scrollbar m_trophyListScroll;

	// Token: 0x040009B8 RID: 2488
	[Header("Effects")]
	public EffectList m_moveItemEffects = new EffectList();

	// Token: 0x040009B9 RID: 2489
	public EffectList m_craftItemEffects = new EffectList();

	// Token: 0x040009BA RID: 2490
	public EffectList m_craftItemDoneEffects = new EffectList();

	// Token: 0x040009BB RID: 2491
	public EffectList m_openInventoryEffects = new EffectList();

	// Token: 0x040009BC RID: 2492
	public EffectList m_closeInventoryEffects = new EffectList();

	// Token: 0x040009BD RID: 2493
	public EffectList m_setActiveGroupEffects = new EffectList();

	// Token: 0x040009BE RID: 2494
	[HideInInspector]
	public InventoryGrid m_playerGrid;

	// Token: 0x040009BF RID: 2495
	private InventoryGrid m_containerGrid;

	// Token: 0x040009C0 RID: 2496
	private Animator m_animator;

	// Token: 0x040009C1 RID: 2497
	private Container m_currentContainer;

	// Token: 0x040009C2 RID: 2498
	private bool m_firstContainerUpdate = true;

	// Token: 0x040009C3 RID: 2499
	private float m_containerHoldTime;

	// Token: 0x040009C4 RID: 2500
	private float m_containerHoldPlaceStackDelay = 0.5f;

	// Token: 0x040009C5 RID: 2501
	private float m_containerHoldExitDelay = 0.5f;

	// Token: 0x040009C6 RID: 2502
	private int m_containerHoldState;

	// Token: 0x040009C7 RID: 2503
	private InventoryGui.RecipeDataPair m_selectedRecipe;

	// Token: 0x040009C8 RID: 2504
	private List<ItemDrop.ItemData> m_upgradeItems = new List<ItemDrop.ItemData>();

	// Token: 0x040009C9 RID: 2505
	private List<Piece.Requirement> m_reqList = new List<Piece.Requirement>();

	// Token: 0x040009CA RID: 2506
	private int m_selectedVariant;

	// Token: 0x040009CB RID: 2507
	private Recipe m_craftRecipe;

	// Token: 0x040009CC RID: 2508
	private ItemDrop.ItemData m_craftUpgradeItem;

	// Token: 0x040009CD RID: 2509
	private int m_craftVariant;

	// Token: 0x040009CE RID: 2510
	private List<InventoryGui.RecipeDataPair> m_availableRecipes = new List<InventoryGui.RecipeDataPair>();

	// Token: 0x040009CF RID: 2511
	private GameObject m_dragGo;

	// Token: 0x040009D0 RID: 2512
	private ItemDrop.ItemData m_dragItem;

	// Token: 0x040009D1 RID: 2513
	private Inventory m_dragInventory;

	// Token: 0x040009D2 RID: 2514
	private int m_dragAmount = 1;

	// Token: 0x040009D3 RID: 2515
	private ItemDrop.ItemData m_splitItem;

	// Token: 0x040009D4 RID: 2516
	private Inventory m_splitInventory;

	// Token: 0x040009D5 RID: 2517
	private float m_craftTimer = -1f;

	// Token: 0x040009D6 RID: 2518
	private bool m_multiCrafting;

	// Token: 0x040009D7 RID: 2519
	private float m_recipeListBaseSize;

	// Token: 0x040009D8 RID: 2520
	private int m_hiddenFrames = 9999;

	// Token: 0x040009D9 RID: 2521
	private string m_splitInput = "";

	// Token: 0x040009DA RID: 2522
	private DateTime m_lastSplitInput;

	// Token: 0x040009DB RID: 2523
	public float m_splitNumInputTimeoutSec = 0.5f;

	// Token: 0x040009DC RID: 2524
	private List<GameObject> m_trophyList = new List<GameObject>();

	// Token: 0x040009DD RID: 2525
	private float m_trophieListBaseSize;

	// Token: 0x02000280 RID: 640
	public enum SortMethod
	{
		// Token: 0x040021C5 RID: 8645
		Original,
		// Token: 0x040021C6 RID: 8646
		Name,
		// Token: 0x040021C7 RID: 8647
		Type,
		// Token: 0x040021C8 RID: 8648
		Weight,
		// Token: 0x040021C9 RID: 8649
		Count
	}

	// Token: 0x02000281 RID: 641
	private struct RecipeDataPair
	{
		// Token: 0x06002006 RID: 8198 RVA: 0x000E755E File Offset: 0x000E575E
		public RecipeDataPair(Recipe recipe, ItemDrop.ItemData data, GameObject element, bool canCraft)
		{
			this.Recipe = recipe;
			this.ItemData = data;
			this.InterfaceElement = element;
			this.CanCraft = canCraft;
		}

		// Token: 0x17000185 RID: 389
		// (get) Token: 0x06002007 RID: 8199 RVA: 0x000E757D File Offset: 0x000E577D
		// (set) Token: 0x06002008 RID: 8200 RVA: 0x000E7585 File Offset: 0x000E5785
		public Recipe Recipe { readonly get; private set; }

		// Token: 0x17000186 RID: 390
		// (get) Token: 0x06002009 RID: 8201 RVA: 0x000E758E File Offset: 0x000E578E
		// (set) Token: 0x0600200A RID: 8202 RVA: 0x000E7596 File Offset: 0x000E5796
		public ItemDrop.ItemData ItemData { readonly get; private set; }

		// Token: 0x17000187 RID: 391
		// (get) Token: 0x0600200B RID: 8203 RVA: 0x000E759F File Offset: 0x000E579F
		// (set) Token: 0x0600200C RID: 8204 RVA: 0x000E75A7 File Offset: 0x000E57A7
		public GameObject InterfaceElement { readonly get; private set; }

		// Token: 0x17000188 RID: 392
		// (get) Token: 0x0600200D RID: 8205 RVA: 0x000E75B0 File Offset: 0x000E57B0
		// (set) Token: 0x0600200E RID: 8206 RVA: 0x000E75B8 File Offset: 0x000E57B8
		public bool CanCraft { readonly get; private set; }
	}
}
