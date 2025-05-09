using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SoftReferenceableAssets;
using Splatform;
using UnityEngine;
using UnityEngine.Rendering;

// Token: 0x02000027 RID: 39
public class Player : Humanoid
{
	// Token: 0x06000303 RID: 771 RVA: 0x0001AED0 File Offset: 0x000190D0
	protected override void Awake()
	{
		base.Awake();
		Player.s_players.Add(this);
		this.m_skills = base.GetComponent<Skills>();
		this.SetupAwake();
		this.m_equipmentModifierValues = new float[Player.s_equipmentModifierSources.Length];
		if (Player.s_equipmentModifierSourceFields == null)
		{
			Player.s_equipmentModifierSourceFields = new FieldInfo[Player.s_equipmentModifierSources.Length];
			for (int i = 0; i < Player.s_equipmentModifierSources.Length; i++)
			{
				Player.s_equipmentModifierSourceFields[i] = typeof(ItemDrop.ItemData.SharedData).GetField(Player.s_equipmentModifierSources[i], BindingFlags.Instance | BindingFlags.Public);
			}
			if (Player.s_equipmentModifierSources.Length != Player.s_equipmentModifierTooltips.Length)
			{
				ZLog.LogError("Equipment modifier tooltip missmatch in player!");
			}
		}
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_placeRayMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece",
			"piece_nonsolid",
			"terrain",
			"vehicle"
		});
		this.m_placeWaterRayMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece",
			"piece_nonsolid",
			"terrain",
			"Water",
			"vehicle"
		});
		this.m_removeRayMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece",
			"piece_nonsolid",
			"terrain",
			"vehicle"
		});
		this.m_interactMask = LayerMask.GetMask(new string[]
		{
			"item",
			"piece",
			"piece_nonsolid",
			"Default",
			"static_solid",
			"Default_small",
			"character",
			"character_net",
			"terrain",
			"vehicle"
		});
		this.m_autoPickupMask = LayerMask.GetMask(new string[]
		{
			"item"
		});
		Inventory inventory = this.m_inventory;
		inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(this.OnInventoryChanged));
		if (Player.s_attackMask == 0)
		{
			Player.s_attackMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"piece_nonsolid",
				"terrain",
				"character",
				"character_net",
				"character_ghost",
				"hitbox",
				"character_noenv",
				"vehicle"
			});
		}
		this.m_nview.Register("OnDeath", new Action<long>(this.RPC_OnDeath));
		if (this.m_nview.IsOwner())
		{
			this.m_nview.Register<int, string, int>("Message", new Action<long, int, string, int>(this.RPC_Message));
			this.m_nview.Register<bool, bool>("OnTargeted", new Action<long, bool, bool>(this.RPC_OnTargeted));
			this.m_nview.Register<float>("UseStamina", new Action<long, float>(this.RPC_UseStamina));
			if (MusicMan.instance)
			{
				MusicMan.instance.TriggerMusic("Wakeup");
			}
			this.UpdateKnownRecipesList();
			this.UpdateAvailablePiecesList();
			this.SetupPlacementGhost();
			this.m_dodgeInvincibleCached = this.m_nview.GetZDO().GetBool(ZDOVars.s_dodgeinv, false);
		}
		this.m_placeRotation = UnityEngine.Random.Range(0, 16);
		float f = UnityEngine.Random.Range(0f, 6.2831855f);
		base.SetLookDir(new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f)), 0f);
		this.FaceLookDirection();
		this.AddQueuedKeys();
		this.UpdateCurrentSeason();
		this.m_attackTowardsPlayerLookDir = (PlatformPrefs.GetInt("AttackTowardsPlayerLookDir", 1) == 1);
	}

	// Token: 0x06000304 RID: 772 RVA: 0x0001B2B2 File Offset: 0x000194B2
	protected override void OnEnable()
	{
		base.OnEnable();
	}

	// Token: 0x06000305 RID: 773 RVA: 0x0001B2BA File Offset: 0x000194BA
	protected override void OnDisable()
	{
		base.OnDisable();
	}

	// Token: 0x06000306 RID: 774 RVA: 0x0001B2C4 File Offset: 0x000194C4
	public void SetLocalPlayer()
	{
		if (Player.m_localPlayer == this)
		{
			return;
		}
		Player.m_localPlayer = this;
		Game.instance.IncrementPlayerStat(PlayerStatType.WorldLoads, 1f);
		ZNet.instance.SetReferencePosition(base.transform.position);
		EnvMan.instance.SetForceEnvironment("");
		this.AddQueuedKeys();
	}

	// Token: 0x06000307 RID: 775 RVA: 0x0001B320 File Offset: 0x00019520
	private void AddQueuedKeys()
	{
		if (Player.m_addUniqueKeyQueue.Count > 0)
		{
			foreach (string name in Player.m_addUniqueKeyQueue)
			{
				this.AddUniqueKey(name);
			}
			Player.m_addUniqueKeyQueue.Clear();
		}
	}

	// Token: 0x06000308 RID: 776 RVA: 0x0001B38C File Offset: 0x0001958C
	public void SetPlayerID(long playerID, string name)
	{
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.GetPlayerID() != 0L)
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_playerID, playerID);
		this.m_nview.GetZDO().Set(ZDOVars.s_playerName, name);
	}

	// Token: 0x06000309 RID: 777 RVA: 0x0001B3DC File Offset: 0x000195DC
	public long GetPlayerID()
	{
		if (!this.m_nview.IsValid())
		{
			return 0L;
		}
		return this.m_nview.GetZDO().GetLong(ZDOVars.s_playerID, 0L);
	}

	// Token: 0x0600030A RID: 778 RVA: 0x0001B405 File Offset: 0x00019605
	public string GetPlayerName()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		return this.m_nview.GetZDO().GetString(ZDOVars.s_playerName, "...");
	}

	// Token: 0x0600030B RID: 779 RVA: 0x0001B434 File Offset: 0x00019634
	public override string GetHoverText()
	{
		return "";
	}

	// Token: 0x0600030C RID: 780 RVA: 0x0001B43B File Offset: 0x0001963B
	public override string GetHoverName()
	{
		return CensorShittyWords.FilterUGC(this.GetPlayerName(), UGCType.CharacterName, this.GetPlayerID());
	}

	// Token: 0x0600030D RID: 781 RVA: 0x0001B44F File Offset: 0x0001964F
	protected override void Start()
	{
		base.Start();
		base.InvalidateCachedLiquidDepth();
	}

	// Token: 0x0600030E RID: 782 RVA: 0x0001B460 File Offset: 0x00019660
	protected override void OnDestroy()
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo != null && ZNet.instance != null)
		{
			ZLog.LogWarning(string.Concat(new string[]
			{
				"Player destroyed sec:",
				zdo.GetSector().ToString(),
				"  pos:",
				base.transform.position.ToString(),
				"  zdopos:",
				zdo.GetPosition().ToString(),
				"  ref ",
				ZNet.instance.GetReferencePosition().ToString()
			}));
		}
		if (this.m_placementGhost)
		{
			UnityEngine.Object.Destroy(this.m_placementGhost);
			this.m_placementGhost = null;
		}
		base.OnDestroy();
		Player.s_players.Remove(this);
		if (Player.m_localPlayer == this)
		{
			ZLog.LogWarning("Local player destroyed");
			Player.m_localPlayer = null;
		}
	}

	// Token: 0x0600030F RID: 783 RVA: 0x0001B574 File Offset: 0x00019774
	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		this.UpdateAwake(fixedDeltaTime);
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.UpdateTargeted(fixedDeltaTime);
		if (this.m_nview.IsOwner())
		{
			if (Player.m_localPlayer != this)
			{
				ZLog.Log("Destroying old local player");
				ZNetScene.instance.Destroy(base.gameObject);
				return;
			}
			if (this.IsDead())
			{
				return;
			}
			this.UpdateActionQueue(fixedDeltaTime);
			this.PlayerAttackInput(fixedDeltaTime);
			this.UpdateAttach();
			this.UpdateDoodadControls(fixedDeltaTime);
			this.UpdateCrouch(fixedDeltaTime);
			this.UpdateDodge(fixedDeltaTime);
			this.UpdateCover(fixedDeltaTime);
			this.UpdateStations(fixedDeltaTime);
			this.UpdateGuardianPower(fixedDeltaTime);
			this.UpdateBaseValue(fixedDeltaTime);
			this.UpdateStats(fixedDeltaTime);
			this.UpdateTeleport(fixedDeltaTime);
			this.AutoPickup(fixedDeltaTime);
			this.EdgeOfWorldKill(fixedDeltaTime);
			this.UpdateBiome(fixedDeltaTime);
			this.UpdateStealth(fixedDeltaTime);
			if (GameCamera.instance && this.m_attachPointCamera == null && Vector3.Distance(GameCamera.instance.transform.position, base.transform.position) < 2f)
			{
				base.SetVisible(false);
			}
			AudioMan.instance.SetIndoor(this.InShelter() || ShieldGenerator.IsInsideShield(base.transform.position));
		}
	}

	// Token: 0x06000310 RID: 784 RVA: 0x0001B6C0 File Offset: 0x000198C0
	private void Update()
	{
		bool flag = InventoryGui.IsVisible();
		if (ZInput.InputLayout != InputLayout.Default && ZInput.IsGamepadActive() && !flag && (ZInput.GetButtonUp("JoyAltPlace") && ZInput.GetButton("JoyAltKeys")))
		{
			this.m_altPlace = !this.m_altPlace;
			if (MessageHud.instance != null)
			{
				string str = Localization.instance.Localize("$hud_altplacement");
				string str2 = this.m_altPlace ? Localization.instance.Localize("$hud_on") : Localization.instance.Localize("$hud_off");
				MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, str + " " + str2, 0, null, false);
			}
		}
		this.UpdateClothFix();
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		bool flag2 = this.TakeInput();
		this.UpdateHover();
		if (flag2)
		{
			if (Player.m_debugMode && global::Console.instance.IsCheatsEnabled())
			{
				if (ZInput.GetKeyDown(KeyCode.Z, true))
				{
					this.ToggleDebugFly();
				}
				if (ZInput.GetKeyDown(KeyCode.B, true))
				{
					this.ToggleNoPlacementCost();
				}
				if (ZInput.GetKeyDown(KeyCode.K, true))
				{
					global::Console.instance.TryRunCommand("killenemies", false, false);
				}
				if (ZInput.GetKeyDown(KeyCode.L, true))
				{
					global::Console.instance.TryRunCommand("removedrops", false, false);
				}
			}
			bool alt = (ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive()) ? ZInput.GetButton("JoyAltKeys") : (ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace"));
			if ((ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse")) && !Hud.InRadial())
			{
				if (this.m_hovering)
				{
					this.Interact(this.m_hovering, false, alt);
				}
				else if (this.m_doodadController != null)
				{
					this.StopDoodadControl();
				}
			}
			else if ((ZInput.GetButton("Use") || ZInput.GetButton("JoyUse")) && !Hud.InRadial() && this.m_hovering)
			{
				this.Interact(this.m_hovering, true, alt);
			}
			bool flag3 = !Hud.InRadial() && ZInput.GetButtonUp("JoyHide") && ZInput.GetButtonLastPressedTimer("JoyHide") < 0.33f;
			if ((ZInput.InputLayout != InputLayout.Default && ZInput.IsGamepadActive()) ? (!this.InPlaceMode() && flag3 && !ZInput.GetButton("JoyAltKeys")) : (ZInput.GetButtonDown("Hide") || (flag3 && !ZInput.GetButton("JoyAltKeys") && !this.InPlaceMode())))
			{
				if (base.GetRightItem() != null || base.GetLeftItem() != null)
				{
					if (!this.InAttack() && !this.InDodge())
					{
						base.HideHandItems(false, true);
					}
				}
				else if ((!base.IsSwimming() || base.IsOnGround()) && !this.InDodge())
				{
					base.ShowHandItems(false, true);
				}
			}
			if (ZInput.GetButtonDown("ToggleWalk") && !Hud.InRadial())
			{
				base.SetWalk(!base.GetWalk());
				if (base.GetWalk())
				{
					this.Message(MessageHud.MessageType.TopLeft, "$msg_walk $hud_on", 0, null);
				}
				else
				{
					this.Message(MessageHud.MessageType.TopLeft, "$msg_walk $hud_off", 0, null);
				}
			}
			this.HandleRadialInput();
			bool flag4 = ZInput.IsGamepadActive() && !ZInput.GetButton("JoyAltKeys");
			bool flag5 = ZInput.InputLayout == InputLayout.Default && ZInput.GetButtonDown("JoyGP");
			bool flag6 = ZInput.IsNonClassicFunctionality() && ZInput.GetButton("JoyLStick") && ZInput.GetButton("JoyRStick");
			if (!Hud.InRadial() && !Hud.IsPieceSelectionVisible() && (ZInput.GetButtonDown("GP") || (flag4 && (flag5 || flag6))))
			{
				this.StartGuardianPower();
			}
			bool flag7 = ZInput.GetButtonDown("JoyAutoPickup") && ZInput.GetButton("JoyAltKeys");
			if (ZInput.GetButtonDown("AutoPickup") || flag7)
			{
				Player.m_enableAutoPickup = !Player.m_enableAutoPickup;
				this.Message(MessageHud.MessageType.TopLeft, "$hud_autopickup:" + (Player.m_enableAutoPickup ? "$hud_on" : "$hud_off"), 0, null);
			}
			if (ZInput.GetButtonDown("Hotbar1"))
			{
				this.UseHotbarItem(1);
			}
			if (ZInput.GetButtonDown("Hotbar2"))
			{
				this.UseHotbarItem(2);
			}
			if (ZInput.GetButtonDown("Hotbar3"))
			{
				this.UseHotbarItem(3);
			}
			if (ZInput.GetButtonDown("Hotbar4"))
			{
				this.UseHotbarItem(4);
			}
			if (ZInput.GetButtonDown("Hotbar5"))
			{
				this.UseHotbarItem(5);
			}
			if (ZInput.GetButtonDown("Hotbar6"))
			{
				this.UseHotbarItem(6);
			}
			if (ZInput.GetButtonDown("Hotbar7"))
			{
				this.UseHotbarItem(7);
			}
			if (ZInput.GetButtonDown("Hotbar8"))
			{
				this.UseHotbarItem(8);
			}
		}
		this.UpdatePlacement(flag2, Time.deltaTime);
		this.UpdateStats();
	}

	// Token: 0x06000311 RID: 785 RVA: 0x0001BB78 File Offset: 0x00019D78
	private void UpdateClothFix()
	{
		float magnitude = base.GetVelocity().magnitude;
		if (magnitude > 0.01f && this.m_lastVelocity < 0.01f)
		{
			base.ResetCloth();
			Terminal.Increment("resetcloth", 1);
		}
		this.m_lastVelocity = magnitude;
	}

	// Token: 0x06000312 RID: 786 RVA: 0x0001BBC4 File Offset: 0x00019DC4
	private void HandleRadialInput()
	{
		if (!Hud.InRadial())
		{
			if (!Hud.instance.m_radialMenu.CanOpen)
			{
				Hud.instance.m_radialMenu.CanOpen = (ZInput.GetButtonDown("JoyRadial") || ZInput.GetButtonDown("OpenRadial") || ZInput.GetButtonDown("OpenEmote"));
			}
			if ((!ZInput.GetButton("JoyAltKeys") && ZInput.GetButtonPressedTimer("JoyRadial") > 0.33f) || ZInput.GetButtonDown("OpenRadial") || ZInput.GetButtonDown("OpenEmote"))
			{
				Hud.instance.m_radialMenu.Open(Hud.instance.m_config, null);
				return;
			}
			if ((!this.InPlaceMode() && !ZInput.GetButton("JoyAltKeys") && ZInput.GetButtonUp("JoySit")) || ZInput.GetButtonDown("Sit"))
			{
				if (this.InEmote() && this.IsSitting())
				{
					this.StopEmote();
					return;
				}
				this.StartEmote("sit", false);
				return;
			}
		}
		else if (!Hud.instance.m_radialMenu.CanThrow && (ZInput.GetButtonDown("JoyRadialSecondaryInteract") || ZInput.GetButtonDown("RadialSecondaryInteract")))
		{
			Hud.instance.m_radialMenu.CanThrow = true;
		}
	}

	// Token: 0x06000313 RID: 787 RVA: 0x0001BCFC File Offset: 0x00019EFC
	private void UpdateStats()
	{
		if (this.IsDebugFlying())
		{
			return;
		}
		this.m_statCheck += Time.deltaTime;
		if (this.m_statCheck < 0.5f)
		{
			return;
		}
		this.m_statCheck = 0f;
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		playerProfile.IncrementStat(this.IsSafeInHome() ? PlayerStatType.TimeInBase : PlayerStatType.TimeOutOfBase, 0.5f);
		float num = Vector3.Distance(base.transform.position, this.m_lastDistCheck);
		if (num > 1f)
		{
			if (num < 20f)
			{
				playerProfile.IncrementStat(PlayerStatType.DistanceTraveled, num);
				if (Ship.GetLocalShip() != null)
				{
					playerProfile.IncrementStat(PlayerStatType.DistanceSail, 1f);
				}
				else if (base.IsOnGround())
				{
					playerProfile.IncrementStat(base.IsRunning() ? PlayerStatType.DistanceRun : PlayerStatType.DistanceWalk, num);
				}
				else
				{
					playerProfile.IncrementStat(PlayerStatType.DistanceAir, num);
				}
			}
			this.m_lastDistCheck = base.transform.position;
		}
	}

	// Token: 0x06000314 RID: 788 RVA: 0x0001BDE8 File Offset: 0x00019FE8
	private float GetBuildStamina()
	{
		float num = base.GetRightItem().m_shared.m_attack.m_attackStamina;
		num *= 1f + this.GetEquipmentHomeItemModifier();
		this.m_seman.ModifyHomeItemStaminaUsage(num, ref num, true);
		if (this.m_buildPieces.m_skill != Skills.SkillType.None)
		{
			float skillFactor = this.GetSkillFactor(this.m_buildPieces.m_skill);
			num -= num * 0.5f * skillFactor;
		}
		return num;
	}

	// Token: 0x06000315 RID: 789 RVA: 0x0001BE58 File Offset: 0x0001A058
	private void UpdatePlacement(bool takeInput, float dt)
	{
		this.UpdateWearNTearHover();
		if (this.InPlaceMode() && !this.IsDead())
		{
			if (!takeInput)
			{
				return;
			}
			this.UpdateBuildGuiInput();
			if (Hud.IsPieceSelectionVisible())
			{
				return;
			}
			ItemDrop.ItemData rightItem = base.GetRightItem();
			if ((ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltKeys")) && (ZInput.GetButtonDown("JoyLStick") || ZInput.GetButtonDown("JoyRStick") || ZInput.GetButtonDown("JoyButtonA") || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("JoyButtonX") || ZInput.GetButtonDown("JoyButtonY") || ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyDPadRight")))
			{
				this.m_blockRemove = true;
			}
			if ((ZInput.GetButtonDown("Remove") || ZInput.GetButtonDown("JoyRemove")) && (ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltKeys")) && (ZInput.InputLayout == InputLayout.Default || !ZInput.IsGamepadActive()))
			{
				this.CopyPiece();
				this.m_blockRemove = true;
			}
			else if (!this.m_blockRemove && (ZInput.GetButtonUp("Remove") || ZInput.GetButtonUp("JoyRemove")))
			{
				this.m_removePressedTime = Time.time;
			}
			if (!ZInput.GetButton("AltPlace") && !ZInput.GetButton("JoyAltKeys"))
			{
				this.m_blockRemove = false;
			}
			Piece hoveringPiece = this.GetHoveringPiece();
			Feast feast;
			if (hoveringPiece)
			{
				Feast component = hoveringPiece.GetComponent<Feast>();
				if (component != null)
				{
					feast = component;
					goto IL_181;
				}
			}
			feast = null;
			IL_181:
			Feast exists = feast;
			ItemDrop itemDrop;
			if (hoveringPiece)
			{
				ItemDrop component2 = hoveringPiece.GetComponent<ItemDrop>();
				if (component2 != null)
				{
					itemDrop = component2;
					goto IL_19B;
				}
			}
			itemDrop = null;
			IL_19B:
			ItemDrop itemDrop2 = itemDrop;
			bool flag = (rightItem.m_shared.m_buildPieces.m_canRemovePieces && (!hoveringPiece || (!exists && (!itemDrop2 || !itemDrop2.IsPiece())))) || (rightItem.m_shared.m_buildPieces.m_canRemoveFeasts && (!hoveringPiece || exists || (itemDrop2 && itemDrop2.IsPiece())));
			if (Time.time - this.m_removePressedTime < 0.2f && flag && Time.time - this.m_lastToolUseTime > this.m_removeDelay)
			{
				this.m_removePressedTime = -9999f;
				if (this.HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
				{
					if (this.RemovePiece())
					{
						this.m_lastToolUseTime = Time.time;
						base.AddNoise(50f);
						this.UseStamina(this.GetBuildStamina());
						if (this.m_buildPieces.m_skill != Skills.SkillType.None && this.m_buildRemoveDebt < 20)
						{
							this.m_buildRemoveDebt++;
						}
						if (rightItem.m_shared.m_useDurability)
						{
							rightItem.m_durability -= this.GetPlaceDurability(rightItem);
						}
						rightItem.m_shared.m_destroyEffect.Create(hoveringPiece.transform.position, Quaternion.identity, null, 1f, -1);
					}
				}
				else
				{
					Hud.instance.StaminaBarEmptyFlash();
				}
			}
			if ((ZInput.GetButtonDown("Attack") || ZInput.GetButtonDown("JoyPlace")) && !Hud.InRadial())
			{
				this.m_placePressedTime = Time.time;
			}
			if (Time.time - this.m_placePressedTime < 0.2f && Time.time - this.m_lastToolUseTime > this.m_placeDelay)
			{
				this.m_placePressedTime = -9999f;
				if (ZInput.GetButton("JoyAltKeys"))
				{
					this.CopyPiece();
					this.m_blockRemove = true;
				}
				else
				{
					Piece selectedPiece = this.m_buildPieces.GetSelectedPiece();
					if (selectedPiece != null)
					{
						if (this.HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
						{
							if (selectedPiece.m_repairPiece)
							{
								this.Repair(rightItem, selectedPiece);
							}
							else if (selectedPiece.m_removePiece && flag)
							{
								this.RemovePiece();
							}
							else if (this.m_placementGhost != null)
							{
								if (this.m_noPlacementCost || this.HaveRequirements(selectedPiece, Player.RequirementMode.CanBuild))
								{
									if (this.TryPlacePiece(selectedPiece))
									{
										this.m_lastToolUseTime = Time.time;
										if (!ZoneSystem.instance.GetGlobalKey(selectedPiece.FreeBuildKey()))
										{
											this.ConsumeResources(selectedPiece.m_resources, 0, -1, 1);
										}
										this.UseStamina(this.GetBuildStamina());
										if (this.m_buildPieces.m_skill != Skills.SkillType.None)
										{
											if (this.m_buildRemoveDebt > 0)
											{
												this.m_buildRemoveDebt--;
											}
											else
											{
												this.RaiseSkill(this.m_buildPieces.m_skill, 1f);
											}
										}
										if (rightItem.m_shared.m_useDurability)
										{
											rightItem.m_durability -= this.GetPlaceDurability(rightItem);
										}
										rightItem.m_shared.m_buildEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
									}
								}
								else
								{
									this.Message(MessageHud.MessageType.Center, "$msg_missingrequirement", 0, null);
								}
							}
						}
						else
						{
							Hud.instance.StaminaBarEmptyFlash();
						}
					}
				}
			}
			if (this.m_placementGhost)
			{
				IPieceMarker component3 = this.m_placementGhost.gameObject.GetComponent<IPieceMarker>();
				if (component3 != null)
				{
					component3.ShowBuildMarker();
				}
			}
			if (hoveringPiece)
			{
				IPieceMarker component4 = hoveringPiece.gameObject.GetComponent<IPieceMarker>();
				if (component4 != null)
				{
					component4.ShowHoverMarker();
				}
			}
			if (this.m_placementGhost)
			{
				Piece component5 = this.m_placementGhost.GetComponent<Piece>();
				if (component5 != null && component5.m_canRotate && this.m_placementGhost.activeInHierarchy)
				{
					this.m_scrollCurrAmount += ZInput.GetMouseScrollWheel();
					if (this.m_scrollCurrAmount > this.m_scrollAmountThreshold)
					{
						this.m_scrollCurrAmount = 0f;
						this.m_placeRotation++;
					}
					if (this.m_scrollCurrAmount < -this.m_scrollAmountThreshold)
					{
						this.m_scrollCurrAmount = 0f;
						this.m_placeRotation--;
					}
				}
			}
			float num = 0f;
			bool flag2 = false;
			if (ZInput.IsGamepadActive())
			{
				switch (ZInput.InputLayout)
				{
				case InputLayout.Default:
					num = ZInput.GetJoyRightStickX(true);
					flag2 = (ZInput.GetButton("JoyRotate") && Mathf.Abs(num) > 0.5f);
					break;
				case InputLayout.Alternative1:
				{
					bool button = ZInput.GetButton("JoyRotate");
					bool button2 = ZInput.GetButton("JoyRotateRight");
					flag2 = (button || button2);
					if (button)
					{
						num = 0.5f;
					}
					else if (button2)
					{
						num = -0.5f;
					}
					break;
				}
				case InputLayout.Alternative2:
				{
					object obj = ZInput.GetButtonLastPressedTimer("JoyRotate") < 0.33f && ZInput.GetButtonUp("JoyRotate");
					bool button3 = ZInput.GetButton("JoyRotateRight");
					object obj2 = obj;
					flag2 = ((obj2 | button3) != null);
					if (obj2 != null)
					{
						num = 0.5f;
					}
					else if (button3)
					{
						num = -0.5f;
					}
					break;
				}
				}
			}
			if (flag2)
			{
				if (this.m_rotatePieceTimer == 0f)
				{
					if (num < 0f)
					{
						this.m_placeRotation++;
					}
					else
					{
						this.m_placeRotation--;
					}
				}
				else if (this.m_rotatePieceTimer > 0.25f)
				{
					if (num < 0f)
					{
						this.m_placeRotation++;
					}
					else
					{
						this.m_placeRotation--;
					}
					this.m_rotatePieceTimer = 0.17f;
				}
				this.m_rotatePieceTimer += dt;
			}
			else
			{
				this.m_rotatePieceTimer = 0f;
			}
			using (Dictionary<Material, float>.Enumerator enumerator = this.m_ghostRippleDistance.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<Material, float> keyValuePair = enumerator.Current;
					keyValuePair.Key.SetFloat("_RippleDistance", ZInput.GetKey(KeyCode.LeftControl, true) ? keyValuePair.Value : 0f);
				}
				return;
			}
		}
		if (this.m_placementGhost)
		{
			this.m_placementGhost.SetActive(false);
		}
	}

	// Token: 0x06000316 RID: 790 RVA: 0x0001C640 File Offset: 0x0001A840
	private float GetPlaceDurability(ItemDrop.ItemData tool)
	{
		float num = tool.m_shared.m_useDurabilityDrain;
		if (tool.m_shared.m_placementDurabilitySkill != Skills.SkillType.None)
		{
			float skillFactor = this.GetSkillFactor(tool.m_shared.m_placementDurabilitySkill);
			num -= num * tool.m_shared.m_placementDurabilityMax * skillFactor;
		}
		return num;
	}

	// Token: 0x06000317 RID: 791 RVA: 0x0001C68C File Offset: 0x0001A88C
	private void UpdateBuildGuiInputAlternative1()
	{
		if (!Hud.IsPieceSelectionVisible() && ZInput.GetButtonDown("JoyBuildMenu") && !PlayerController.HasInputDelay && !Hud.InRadial())
		{
			for (int i = 0; i < this.m_buildPieces.m_selectedPiece.Length; i++)
			{
				this.m_buildPieces.m_lastSelectedPiece[i] = this.m_buildPieces.m_selectedPiece[i];
			}
			Hud.instance.TogglePieceSelection();
			return;
		}
		if (Hud.IsPieceSelectionVisible())
		{
			if (ZInput.GetKeyDown(KeyCode.Escape, true) || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("BuildMenu"))
			{
				for (int j = 0; j < this.m_buildPieces.m_selectedPiece.Length; j++)
				{
					this.m_buildPieces.m_selectedPiece[j] = this.m_buildPieces.m_lastSelectedPiece[j];
				}
				Hud.HidePieceSelection();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyButtonA"))
			{
				Hud.HidePieceSelection();
				this.PlayButtonSound();
			}
			this.m_scrollCurrAmount += ZInput.GetMouseScrollWheel();
			if (ZInput.GetButtonDown("JoyTabLeft") || ZInput.GetButtonDown("TabLeft") || this.m_scrollCurrAmount > this.m_scrollAmountThreshold)
			{
				this.m_scrollCurrAmount = 0f;
				this.m_buildPieces.PrevCategory();
				this.UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyTabRight") || ZInput.GetButtonDown("TabRight") || this.m_scrollCurrAmount < -this.m_scrollAmountThreshold)
			{
				this.m_scrollCurrAmount = 0f;
				this.m_buildPieces.NextCategory();
				this.UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyLStickLeft") || ZInput.GetButtonDown("JoyDPadLeft"))
			{
				this.m_buildPieces.LeftPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickRight") || ZInput.GetButtonDown("JoyDPadRight"))
			{
				this.m_buildPieces.RightPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				this.m_buildPieces.UpPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				this.m_buildPieces.DownPiece();
				this.SetupPlacementGhost();
			}
		}
	}

	// Token: 0x06000318 RID: 792 RVA: 0x0001C8C4 File Offset: 0x0001AAC4
	private void UpdateBuildGuiInput()
	{
		if (ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive())
		{
			this.UpdateBuildGuiInputAlternative1();
			return;
		}
		if (!Hud.IsPieceSelectionVisible())
		{
			if (Hud.instance.IsQuickPieceSelectEnabled())
			{
				if (!Hud.IsPieceSelectionVisible() && ZInput.GetButtonDown("BuildMenu") && !PlayerController.HasInputDelay && !Hud.InRadial())
				{
					Hud.instance.TogglePieceSelection();
				}
			}
			else if (ZInput.GetButtonDown("BuildMenu") && !PlayerController.HasInputDelay && !Hud.InRadial())
			{
				Hud.instance.TogglePieceSelection();
			}
			if (ZInput.GetButtonDown("JoyUse") && !PlayerController.HasInputDelay && !Hud.InRadial())
			{
				Hud.instance.TogglePieceSelection();
				return;
			}
		}
		else if (Hud.IsPieceSelectionVisible())
		{
			if (ZInput.GetKeyDown(KeyCode.Escape, true) || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("BuildMenu"))
			{
				Hud.HidePieceSelection();
			}
			if (ZInput.GetButtonDown("JoyUse"))
			{
				Hud.HidePieceSelection();
				this.PlayButtonSound();
			}
			this.m_scrollCurrAmount += ZInput.GetMouseScrollWheel();
			if (ZInput.GetButtonDown("JoyTabLeft") || ZInput.GetButtonDown("TabLeft") || this.m_scrollCurrAmount > this.m_scrollAmountThreshold)
			{
				this.m_scrollCurrAmount = 0f;
				this.m_buildPieces.PrevCategory();
				this.UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyTabRight") || ZInput.GetButtonDown("TabRight") || this.m_scrollCurrAmount < -this.m_scrollAmountThreshold)
			{
				this.m_scrollCurrAmount = 0f;
				this.m_buildPieces.NextCategory();
				this.UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyLStickLeft") || ZInput.GetButtonDown("JoyDPadLeft"))
			{
				this.m_buildPieces.LeftPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickRight") || ZInput.GetButtonDown("JoyDPadRight"))
			{
				this.m_buildPieces.RightPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				this.m_buildPieces.UpPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				this.m_buildPieces.DownPiece();
				this.SetupPlacementGhost();
			}
		}
	}

	// Token: 0x06000319 RID: 793 RVA: 0x0001CAFE File Offset: 0x0001ACFE
	private void PlayButtonSound()
	{
		if (Player.m_localPlayer)
		{
			EffectList buttonEffects = this.m_buttonEffects;
			if (buttonEffects == null)
			{
				return;
			}
			buttonEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity, null, 1f, -1);
		}
	}

	// Token: 0x0600031A RID: 794 RVA: 0x0001CB38 File Offset: 0x0001AD38
	public bool SetSelectedPiece(Piece p)
	{
		Vector2Int selectedPiece;
		int buildCategory;
		if (this.m_buildPieces.GetPieceIndex(p, out selectedPiece, out buildCategory))
		{
			this.SetBuildCategory(buildCategory);
			this.SetSelectedPiece(selectedPiece);
			return true;
		}
		return false;
	}

	// Token: 0x0600031B RID: 795 RVA: 0x0001CB68 File Offset: 0x0001AD68
	public void SetSelectedPiece(Vector2Int p)
	{
		if (this.m_buildPieces && this.m_buildPieces.GetSelectedIndex() != p)
		{
			this.m_buildPieces.SetSelected(p);
			this.SetupPlacementGhost();
		}
	}

	// Token: 0x0600031C RID: 796 RVA: 0x0001CB9C File Offset: 0x0001AD9C
	public Piece GetPiece(Vector2Int p)
	{
		if (!(this.m_buildPieces != null))
		{
			return null;
		}
		return this.m_buildPieces.GetPiece(p);
	}

	// Token: 0x0600031D RID: 797 RVA: 0x0001CBBA File Offset: 0x0001ADBA
	public bool IsPieceAvailable(Piece piece)
	{
		return this.m_buildPieces != null && this.m_buildPieces.IsPieceAvailable(piece);
	}

	// Token: 0x0600031E RID: 798 RVA: 0x0001CBD8 File Offset: 0x0001ADD8
	public Piece GetSelectedPiece()
	{
		if (!(this.m_buildPieces != null))
		{
			return null;
		}
		return this.m_buildPieces.GetSelectedPiece();
	}

	// Token: 0x0600031F RID: 799 RVA: 0x0001CBF5 File Offset: 0x0001ADF5
	private void LateUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateEmote();
		if (this.m_nview.IsOwner())
		{
			ZNet.instance.SetReferencePosition(base.transform.position);
			this.UpdatePlacementGhost(false);
		}
	}

	// Token: 0x06000320 RID: 800 RVA: 0x0001CC34 File Offset: 0x0001AE34
	public void UpdateEvents()
	{
		if (!RandEventSystem.instance)
		{
			return;
		}
		this.m_readyEvents.Clear();
		foreach (RandomEvent randomEvent in RandEventSystem.instance.m_events)
		{
			if (RandEventSystem.instance.PlayerIsReadyForEvent(this, randomEvent))
			{
				this.m_readyEvents.Add(randomEvent.m_name);
			}
		}
		if (ZNet.instance)
		{
			RandEventSystem.SetRandomEventsNeedsRefresh();
			ZNet.instance.m_serverSyncedPlayerData["possibleEvents"] = string.Join(",", this.m_readyEvents);
		}
	}

	// Token: 0x06000321 RID: 801 RVA: 0x0001CCF0 File Offset: 0x0001AEF0
	private void SetupAwake()
	{
		if (this.m_nview.GetZDO() == null)
		{
			this.m_animator.SetBool("wakeup", false);
			return;
		}
		bool @bool = this.m_nview.GetZDO().GetBool(ZDOVars.s_wakeup, true);
		this.m_animator.SetBool("wakeup", @bool);
		if (@bool)
		{
			this.m_wakeupTimer = 0f;
		}
	}

	// Token: 0x06000322 RID: 802 RVA: 0x0001CD54 File Offset: 0x0001AF54
	private void UpdateAwake(float dt)
	{
		if (this.m_wakeupTimer >= 0f)
		{
			this.m_wakeupTimer += dt;
			if (this.m_wakeupTimer > 1f)
			{
				this.m_wakeupTimer = -1f;
				this.m_animator.SetBool("wakeup", false);
				if (this.m_nview.IsOwner())
				{
					this.m_nview.GetZDO().Set(ZDOVars.s_wakeup, false);
				}
			}
		}
	}

	// Token: 0x06000323 RID: 803 RVA: 0x0001CDC8 File Offset: 0x0001AFC8
	private void EdgeOfWorldKill(float dt)
	{
		if (this.IsDead())
		{
			return;
		}
		float num = global::Utils.DistanceXZ(Vector3.zero, base.transform.position);
		float num2 = 10420f;
		if (num > num2 && (base.IsSwimming() || base.transform.position.y < 30f))
		{
			Vector3 a = Vector3.Normalize(base.transform.position);
			float d = global::Utils.LerpStep(num2, 10500f, num) * 10f;
			this.m_body.MovePosition(this.m_body.position + a * d * dt);
		}
		if (num > num2 && base.transform.position.y < -10f)
		{
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = 99999f;
			hitData.m_hitType = HitData.HitType.EdgeOfWorld;
			base.Damage(hitData);
		}
	}

	// Token: 0x06000324 RID: 804 RVA: 0x0001CEB0 File Offset: 0x0001B0B0
	private void AutoPickup(float dt)
	{
		if (this.IsTeleporting())
		{
			return;
		}
		if (!Player.m_enableAutoPickup)
		{
			return;
		}
		Vector3 vector = base.transform.position + Vector3.up;
		foreach (Collider collider in Physics.OverlapSphere(vector, this.m_autoPickupRange, this.m_autoPickupMask))
		{
			if (collider.attachedRigidbody)
			{
				ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
				FloatingTerrainDummy floatingTerrainDummy = null;
				if (component == null && (floatingTerrainDummy = collider.attachedRigidbody.gameObject.GetComponent<FloatingTerrainDummy>()) && floatingTerrainDummy)
				{
					component = floatingTerrainDummy.m_parent.gameObject.GetComponent<ItemDrop>();
				}
				if (!(component == null) && component.m_autoPickup && !component.IsPiece() && !this.HaveUniqueKey(component.m_itemData.m_shared.m_name) && component.GetComponent<ZNetView>().IsValid())
				{
					if (!component.CanPickup(true))
					{
						component.RequestOwn();
					}
					else if (!component.InTar())
					{
						component.Load();
						if (this.m_inventory.CanAddItem(component.m_itemData, -1) && component.m_itemData.GetWeight(-1) + this.m_inventory.GetTotalWeight() <= this.GetMaxCarryWeight())
						{
							float num = Vector3.Distance(component.transform.position, vector);
							if (num <= this.m_autoPickupRange)
							{
								if (num < 0.3f)
								{
									base.Pickup(component.gameObject, true, true);
								}
								else
								{
									Vector3 a = Vector3.Normalize(vector - component.transform.position);
									float d = 15f;
									Vector3 b = a * d * dt;
									component.transform.position += b;
									if (floatingTerrainDummy)
									{
										floatingTerrainDummy.transform.position += b;
									}
								}
							}
						}
					}
				}
			}
		}
	}

	// Token: 0x06000325 RID: 805 RVA: 0x0001D0CC File Offset: 0x0001B2CC
	private void PlayerAttackInput(float dt)
	{
		if (this.InPlaceMode())
		{
			return;
		}
		ItemDrop.ItemData currentWeapon = base.GetCurrentWeapon();
		this.UpdateWeaponLoading(currentWeapon, dt);
		if (currentWeapon != null && currentWeapon.m_shared.m_attack.m_bowDraw)
		{
			this.UpdateAttackBowDraw(currentWeapon, dt);
		}
		else
		{
			if (this.m_attack)
			{
				this.m_queuedAttackTimer = 0.5f;
				this.m_queuedSecondAttackTimer = 0f;
			}
			if (this.m_secondaryAttack)
			{
				this.m_queuedSecondAttackTimer = 0.5f;
				this.m_queuedAttackTimer = 0f;
			}
			this.m_queuedAttackTimer -= Time.fixedDeltaTime;
			this.m_queuedSecondAttackTimer -= Time.fixedDeltaTime;
			if ((this.m_queuedAttackTimer > 0f || this.m_attackHold) && this.StartAttack(null, false))
			{
				this.m_queuedAttackTimer = 0f;
			}
			if ((this.m_queuedSecondAttackTimer > 0f || this.m_secondaryAttackHold) && this.StartAttack(null, true))
			{
				this.m_queuedSecondAttackTimer = 0f;
			}
		}
		if (this.m_currentAttack != null && this.m_currentAttack.m_loopingAttack && !(this.m_currentAttackIsSecondary ? this.m_secondaryAttackHold : this.m_attackHold))
		{
			this.m_currentAttack.Abort();
		}
	}

	// Token: 0x06000326 RID: 806 RVA: 0x0001D200 File Offset: 0x0001B400
	private void UpdateWeaponLoading(ItemDrop.ItemData weapon, float dt)
	{
		if (weapon == null || !weapon.m_shared.m_attack.m_requiresReload)
		{
			this.SetWeaponLoaded(null);
			return;
		}
		if (this.m_weaponLoaded == weapon)
		{
			return;
		}
		if (weapon.m_shared.m_attack.m_requiresReload && !this.IsReloadActionQueued())
		{
			if (!base.TryUseEitr(weapon.m_shared.m_attack.m_reloadEitrDrain))
			{
				return;
			}
			this.QueueReloadAction();
		}
	}

	// Token: 0x06000327 RID: 807 RVA: 0x0001D270 File Offset: 0x0001B470
	private void CancelReloadAction()
	{
		foreach (Player.MinorActionData minorActionData in this.m_actionQueue)
		{
			if (minorActionData.m_type == Player.MinorActionData.ActionType.Reload)
			{
				this.m_actionQueue.Remove(minorActionData);
				break;
			}
		}
	}

	// Token: 0x06000328 RID: 808 RVA: 0x0001D2D4 File Offset: 0x0001B4D4
	public override void ResetLoadedWeapon()
	{
		this.SetWeaponLoaded(null);
		foreach (Player.MinorActionData minorActionData in this.m_actionQueue)
		{
			if (minorActionData.m_type == Player.MinorActionData.ActionType.Reload)
			{
				this.m_actionQueue.Remove(minorActionData);
				break;
			}
		}
	}

	// Token: 0x06000329 RID: 809 RVA: 0x0001D340 File Offset: 0x0001B540
	private void SetWeaponLoaded(ItemDrop.ItemData weapon)
	{
		if (weapon == this.m_weaponLoaded)
		{
			return;
		}
		this.m_weaponLoaded = weapon;
		this.m_nview.GetZDO().Set(ZDOVars.s_weaponLoaded, weapon != null);
	}

	// Token: 0x0600032A RID: 810 RVA: 0x0001D36C File Offset: 0x0001B56C
	public override bool IsWeaponLoaded()
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.m_nview.IsOwner())
		{
			return this.m_nview.GetZDO().GetBool(ZDOVars.s_weaponLoaded, false);
		}
		return this.m_weaponLoaded != null;
	}

	// Token: 0x0600032B RID: 811 RVA: 0x0001D3AC File Offset: 0x0001B5AC
	private void UpdateAttackBowDraw(ItemDrop.ItemData weapon, float dt)
	{
		if (this.m_blocking || this.InMinorAction() || this.IsAttached())
		{
			this.m_attackDrawTime = -1f;
			if (!string.IsNullOrEmpty(weapon.m_shared.m_attack.m_drawAnimationState))
			{
				this.m_zanim.SetBool(weapon.m_shared.m_attack.m_drawAnimationState, false);
			}
			return;
		}
		float num = weapon.GetDrawStaminaDrain();
		float drawEitrDrain = weapon.GetDrawEitrDrain();
		if ((double)base.GetAttackDrawPercentage() >= 1.0)
		{
			num *= 0.5f;
		}
		num += num * this.GetEquipmentAttackStaminaModifier();
		this.m_seman.ModifyAttackStaminaUsage(num, ref num, true);
		bool flag = num <= 0f || this.HaveStamina(0f);
		bool flag2 = drawEitrDrain <= 0f || this.HaveEitr(0f);
		if (this.m_attackDrawTime < 0f)
		{
			if (!this.m_attackHold)
			{
				this.m_attackDrawTime = 0f;
				return;
			}
		}
		else
		{
			if (this.m_attackHold && flag && this.m_attackDrawTime >= 0f)
			{
				if (this.m_attackDrawTime == 0f)
				{
					if (!weapon.m_shared.m_attack.StartDraw(this, weapon))
					{
						this.m_attackDrawTime = -1f;
						return;
					}
					weapon.m_shared.m_holdStartEffect.Create(base.transform.position, Quaternion.identity, base.transform, 1f, -1);
				}
				this.m_attackDrawTime += Time.fixedDeltaTime;
				if (!string.IsNullOrEmpty(weapon.m_shared.m_attack.m_drawAnimationState))
				{
					this.m_zanim.SetBool(weapon.m_shared.m_attack.m_drawAnimationState, true);
					this.m_zanim.SetFloat("drawpercent", base.GetAttackDrawPercentage());
				}
				this.UseStamina(num * dt);
				this.UseEitr(drawEitrDrain * dt);
				return;
			}
			if (this.m_attackDrawTime > 0f)
			{
				if (flag && flag2)
				{
					this.StartAttack(null, false);
				}
				if (!string.IsNullOrEmpty(weapon.m_shared.m_attack.m_drawAnimationState))
				{
					this.m_zanim.SetBool(weapon.m_shared.m_attack.m_drawAnimationState, false);
				}
				this.m_attackDrawTime = 0f;
			}
		}
	}

	// Token: 0x0600032C RID: 812 RVA: 0x0001D5E2 File Offset: 0x0001B7E2
	protected override bool HaveQueuedChain()
	{
		return (this.m_queuedAttackTimer > 0f || this.m_attackHold) && base.GetCurrentWeapon() != null && this.m_currentAttack != null && this.m_currentAttack.CanStartChainAttack();
	}

	// Token: 0x0600032D RID: 813 RVA: 0x0001D618 File Offset: 0x0001B818
	private void UpdateBaseValue(float dt)
	{
		this.m_baseValueUpdateTimer += dt;
		if (this.m_baseValueUpdateTimer > 2f)
		{
			this.m_baseValueUpdateTimer = 0f;
			this.m_baseValue = EffectArea.GetBaseValue(base.transform.position, 20f);
			this.m_comfortLevel = SE_Rested.CalculateComfortLevel(this);
			if (this.m_baseValueOld == this.m_baseValue)
			{
				return;
			}
			this.m_baseValueOld = this.m_baseValue;
			ZNet.instance.m_serverSyncedPlayerData["baseValue"] = this.m_baseValue.ToString();
			this.m_nview.GetZDO().Set(ZDOVars.s_baseValue, this.m_baseValue, false);
			RandEventSystem.SetRandomEventsNeedsRefresh();
		}
	}

	// Token: 0x0600032E RID: 814 RVA: 0x0001D6D0 File Offset: 0x0001B8D0
	public int GetComfortLevel()
	{
		if (this.m_nview == null)
		{
			return 0;
		}
		return this.m_comfortLevel;
	}

	// Token: 0x0600032F RID: 815 RVA: 0x0001D6E8 File Offset: 0x0001B8E8
	public int GetBaseValue()
	{
		if (!this.m_nview.IsValid())
		{
			return 0;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_baseValue;
		}
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_baseValue, 0);
	}

	// Token: 0x06000330 RID: 816 RVA: 0x0001D723 File Offset: 0x0001B923
	public bool IsSafeInHome()
	{
		return this.m_safeInHome;
	}

	// Token: 0x06000331 RID: 817 RVA: 0x0001D72C File Offset: 0x0001B92C
	private void UpdateBiome(float dt)
	{
		if (this.InIntro())
		{
			return;
		}
		if (this.m_biomeTimer == 0f)
		{
			Location location = Location.GetLocation(base.transform.position, false);
			if (location != null && !string.IsNullOrEmpty(location.m_discoverLabel))
			{
				this.AddKnownLocationName(location.m_discoverLabel);
			}
		}
		this.m_biomeTimer += dt;
		if (this.m_biomeTimer > 1f)
		{
			this.m_biomeTimer = 0f;
			Heightmap.Biome biome = Heightmap.FindBiome(base.transform.position);
			if (this.m_currentBiome != biome)
			{
				this.m_currentBiome = biome;
				this.AddKnownBiome(biome);
			}
		}
	}

	// Token: 0x06000332 RID: 818 RVA: 0x0001D7CB File Offset: 0x0001B9CB
	public Heightmap.Biome GetCurrentBiome()
	{
		return this.m_currentBiome;
	}

	// Token: 0x06000333 RID: 819 RVA: 0x0001D7D4 File Offset: 0x0001B9D4
	public override void RaiseSkill(Skills.SkillType skill, float value = 1f)
	{
		if (skill == Skills.SkillType.None)
		{
			return;
		}
		float num = 1f;
		this.m_seman.ModifyRaiseSkill(skill, ref num);
		value *= num;
		this.m_skills.RaiseSkill(skill, value);
	}

	// Token: 0x06000334 RID: 820 RVA: 0x0001D80C File Offset: 0x0001BA0C
	private void UpdateStats(float dt)
	{
		if (this.InIntro() || this.IsTeleporting())
		{
			return;
		}
		this.m_timeSinceDeath += dt;
		this.UpdateModifiers();
		this.UpdateFood(dt, false);
		bool flag = this.IsEncumbered();
		float maxStamina = this.GetMaxStamina();
		float num = 1f;
		if (this.IsBlocking())
		{
			num *= 0.8f;
		}
		if ((base.IsSwimming() && !base.IsOnGround()) || this.InAttack() || this.InDodge() || this.m_wallRunning || flag)
		{
			num = 0f;
		}
		float num2 = (this.m_staminaRegen + (1f - this.m_stamina / maxStamina) * this.m_staminaRegen * this.m_staminaRegenTimeMultiplier) * num;
		float num3 = 1f;
		this.m_seman.ModifyStaminaRegen(ref num3);
		num2 *= num3;
		this.m_staminaRegenTimer -= dt;
		if (this.m_stamina < maxStamina && this.m_staminaRegenTimer <= 0f)
		{
			this.m_stamina = Mathf.Min(maxStamina, this.m_stamina + num2 * dt * Game.m_staminaRegenRate);
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_stamina, this.m_stamina);
		float maxEitr = this.GetMaxEitr();
		float num4 = 1f;
		if (this.IsBlocking())
		{
			num4 *= 0.8f;
		}
		if (this.InAttack() || this.InDodge())
		{
			num4 = 0f;
		}
		float num5 = (this.m_eiterRegen + (1f - this.m_eitr / maxEitr) * this.m_eiterRegen) * num4;
		float num6 = 1f;
		this.m_seman.ModifyEitrRegen(ref num6);
		num6 += this.GetEquipmentEitrRegenModifier();
		num5 *= num6;
		this.m_eitrRegenTimer -= dt;
		if (this.m_eitr < maxEitr && this.m_eitrRegenTimer <= 0f)
		{
			this.m_eitr = Mathf.Min(maxEitr, this.m_eitr + num5 * dt);
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_eitr, this.m_eitr);
		if (flag)
		{
			if (this.m_moveDir.magnitude > 0.1f)
			{
				this.UseStamina(this.m_encumberedStaminaDrain * dt);
			}
			this.m_seman.AddStatusEffect(SEMan.s_statusEffectEncumbered, false, 0, 0f);
			this.ShowTutorial("encumbered", false);
		}
		else
		{
			this.m_seman.RemoveStatusEffect(SEMan.s_statusEffectEncumbered, false);
		}
		if (!this.HardDeath())
		{
			this.m_seman.AddStatusEffect(SEMan.s_statusEffectSoftDeath, false, 0, 0f);
		}
		else
		{
			this.m_seman.RemoveStatusEffect(SEMan.s_statusEffectSoftDeath, false);
		}
		this.UpdateEnvStatusEffects(dt);
	}

	// Token: 0x06000335 RID: 821 RVA: 0x0001DAA8 File Offset: 0x0001BCA8
	public float GetEquipmentEitrRegenModifier()
	{
		float num = 0f;
		if (this.m_chestItem != null)
		{
			num += this.m_chestItem.m_shared.m_eitrRegenModifier;
		}
		if (this.m_legItem != null)
		{
			num += this.m_legItem.m_shared.m_eitrRegenModifier;
		}
		if (this.m_helmetItem != null)
		{
			num += this.m_helmetItem.m_shared.m_eitrRegenModifier;
		}
		if (this.m_shoulderItem != null)
		{
			num += this.m_shoulderItem.m_shared.m_eitrRegenModifier;
		}
		if (this.m_leftItem != null)
		{
			num += this.m_leftItem.m_shared.m_eitrRegenModifier;
		}
		if (this.m_rightItem != null)
		{
			num += this.m_rightItem.m_shared.m_eitrRegenModifier;
		}
		if (this.m_utilityItem != null)
		{
			num += this.m_utilityItem.m_shared.m_eitrRegenModifier;
		}
		return num;
	}

	// Token: 0x06000336 RID: 822 RVA: 0x0001DB7C File Offset: 0x0001BD7C
	private void UpdateEnvStatusEffects(float dt)
	{
		this.m_nearFireTimer += dt;
		HitData.DamageModifiers damageModifiers = base.GetDamageModifiers(null);
		bool flag = this.m_nearFireTimer < 0.25f;
		bool flag2 = this.m_seman.HaveStatusEffect(SEMan.s_statusEffectBurning);
		bool flag3 = this.InShelter();
		HitData.DamageModifier modifier = damageModifiers.GetModifier(HitData.DamageType.Frost);
		bool flag4 = EnvMan.IsFreezing();
		bool flag5 = EnvMan.IsCold();
		bool flag6 = EnvMan.IsWet();
		bool flag7 = this.IsSensed();
		bool flag8 = this.m_seman.HaveStatusEffect(SEMan.s_statusEffectWet);
		bool flag9 = this.IsSitting();
		bool flag10 = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.WarmCozyArea, 1f);
		bool flag11 = ShieldGenerator.IsInsideShield(base.transform.position);
		bool flag12 = flag4 && !flag && !flag3;
		bool flag13 = (flag5 && !flag) || (flag4 && flag && !flag3) || (flag4 && !flag && flag3);
		if (modifier == HitData.DamageModifier.Resistant || modifier == HitData.DamageModifier.VeryResistant || flag10)
		{
			flag12 = false;
			flag13 = false;
		}
		if (flag6 && !this.m_underRoof && !flag11)
		{
			this.m_seman.AddStatusEffect(SEMan.s_statusEffectWet, true, 0, 0f);
		}
		if (flag3)
		{
			this.m_seman.AddStatusEffect(SEMan.s_statusEffectShelter, false, 0, 0f);
		}
		else
		{
			this.m_seman.RemoveStatusEffect(SEMan.s_statusEffectShelter, false);
		}
		if (flag)
		{
			this.m_seman.AddStatusEffect(SEMan.s_statusEffectCampFire, false, 0, 0f);
		}
		else
		{
			this.m_seman.RemoveStatusEffect(SEMan.s_statusEffectCampFire, false);
		}
		bool flag14 = !flag7 && (flag9 || flag3) && !flag13 && !flag12 && (!flag8 || flag10) && !flag2 && flag;
		if (flag14)
		{
			this.m_seman.AddStatusEffect(SEMan.s_statusEffectResting, false, 0, 0f);
		}
		else
		{
			this.m_seman.RemoveStatusEffect(SEMan.s_statusEffectResting, false);
		}
		this.m_safeInHome = (flag14 && flag3 && (float)this.GetBaseValue() >= 1f);
		if (flag12)
		{
			if (!this.m_seman.RemoveStatusEffect(SEMan.s_statusEffectCold, true))
			{
				this.m_seman.AddStatusEffect(SEMan.s_statusEffectFreezing, false, 0, 0f);
				return;
			}
		}
		else if (flag13)
		{
			if (!this.m_seman.RemoveStatusEffect(SEMan.s_statusEffectFreezing, true) && this.m_seman.AddStatusEffect(SEMan.s_statusEffectCold, false, 0, 0f))
			{
				this.ShowTutorial("cold", false);
				return;
			}
		}
		else
		{
			this.m_seman.RemoveStatusEffect(SEMan.s_statusEffectCold, false);
			this.m_seman.RemoveStatusEffect(SEMan.s_statusEffectFreezing, false);
		}
	}

	// Token: 0x06000337 RID: 823 RVA: 0x0001DE10 File Offset: 0x0001C010
	public bool CanEat(ItemDrop.ItemData item, bool showMessages)
	{
		foreach (Player.Food food in this.m_foods)
		{
			if (food.m_item.m_shared.m_name == item.m_shared.m_name)
			{
				if (food.CanEatAgain())
				{
					return true;
				}
				if (showMessages)
				{
					this.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_nomore", new string[]
					{
						item.m_shared.m_name
					}), 0, null);
				}
				return false;
			}
		}
		using (List<Player.Food>.Enumerator enumerator = this.m_foods.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.CanEatAgain())
				{
					return true;
				}
			}
		}
		if (this.m_foods.Count >= 3)
		{
			if (showMessages)
			{
				this.Message(MessageHud.MessageType.Center, "$msg_isfull", 0, null);
			}
			return false;
		}
		return true;
	}

	// Token: 0x06000338 RID: 824 RVA: 0x0001DF2C File Offset: 0x0001C12C
	private Player.Food GetMostDepletedFood()
	{
		Player.Food food = null;
		foreach (Player.Food food2 in this.m_foods)
		{
			if (food2.CanEatAgain() && (food == null || food2.m_time < food.m_time))
			{
				food = food2;
			}
		}
		return food;
	}

	// Token: 0x06000339 RID: 825 RVA: 0x0001DF98 File Offset: 0x0001C198
	public void ClearFood()
	{
		this.m_foods.Clear();
	}

	// Token: 0x0600033A RID: 826 RVA: 0x0001DFA5 File Offset: 0x0001C1A5
	public bool RemoveOneFood()
	{
		if (this.m_foods.Count == 0)
		{
			return false;
		}
		this.m_foods.RemoveAt(UnityEngine.Random.Range(0, this.m_foods.Count));
		return true;
	}

	// Token: 0x0600033B RID: 827 RVA: 0x0001DFD4 File Offset: 0x0001C1D4
	public bool EatFood(ItemDrop.ItemData item)
	{
		if (!this.CanEat(item, false))
		{
			return false;
		}
		string text = "";
		if (item.m_shared.m_food > 0f)
		{
			text = text + " +" + item.m_shared.m_food.ToString() + " $item_food_health ";
		}
		if (item.m_shared.m_foodStamina > 0f)
		{
			text = text + " +" + item.m_shared.m_foodStamina.ToString() + " $item_food_stamina ";
		}
		if (item.m_shared.m_foodEitr > 0f)
		{
			text = text + " +" + item.m_shared.m_foodEitr.ToString() + " $item_food_eitr ";
		}
		this.Message(MessageHud.MessageType.Center, text, 0, null);
		foreach (Player.Food food in this.m_foods)
		{
			if (food.m_item.m_shared.m_name == item.m_shared.m_name)
			{
				if (food.CanEatAgain())
				{
					food.m_time = item.m_shared.m_foodBurnTime;
					food.m_health = item.m_shared.m_food;
					food.m_stamina = item.m_shared.m_foodStamina;
					food.m_eitr = item.m_shared.m_foodEitr;
					this.UpdateFood(0f, true);
					return true;
				}
				return false;
			}
		}
		if (this.m_foods.Count < 3)
		{
			Player.Food food2 = new Player.Food();
			food2.m_name = item.m_dropPrefab.name;
			food2.m_item = item;
			food2.m_time = item.m_shared.m_foodBurnTime;
			food2.m_health = item.m_shared.m_food;
			food2.m_stamina = item.m_shared.m_foodStamina;
			food2.m_eitr = item.m_shared.m_foodEitr;
			this.m_foods.Add(food2);
			this.UpdateFood(0f, true);
			return true;
		}
		Player.Food mostDepletedFood = this.GetMostDepletedFood();
		if (mostDepletedFood != null)
		{
			mostDepletedFood.m_name = item.m_dropPrefab.name;
			mostDepletedFood.m_item = item;
			mostDepletedFood.m_time = item.m_shared.m_foodBurnTime;
			mostDepletedFood.m_health = item.m_shared.m_food;
			mostDepletedFood.m_stamina = item.m_shared.m_foodStamina;
			this.UpdateFood(0f, true);
			return true;
		}
		Game.instance.IncrementPlayerStat(PlayerStatType.FoodEaten, 1f);
		return false;
	}

	// Token: 0x0600033C RID: 828 RVA: 0x0001E274 File Offset: 0x0001C474
	private void UpdateFood(float dt, bool forceUpdate)
	{
		this.m_foodUpdateTimer += dt;
		if (this.m_foodUpdateTimer >= 1f || forceUpdate)
		{
			this.m_foodUpdateTimer -= 1f;
			foreach (Player.Food food in this.m_foods)
			{
				food.m_time -= 1f;
				float num = Mathf.Clamp01(food.m_time / food.m_item.m_shared.m_foodBurnTime);
				num = Mathf.Pow(num, 0.3f);
				food.m_health = food.m_item.m_shared.m_food * num;
				food.m_stamina = food.m_item.m_shared.m_foodStamina * num;
				food.m_eitr = food.m_item.m_shared.m_foodEitr * num;
				if (food.m_time <= 0f)
				{
					this.Message(MessageHud.MessageType.Center, "$msg_food_done", 0, null);
					this.m_foods.Remove(food);
					break;
				}
			}
			float health;
			float stamina;
			float num2;
			this.GetTotalFoodValue(out health, out stamina, out num2);
			this.SetMaxHealth(health, true);
			this.SetMaxStamina(stamina, true);
			this.SetMaxEitr(num2, true);
			if (num2 > 0f)
			{
				this.ShowTutorial("eitr", false);
			}
		}
		if (!forceUpdate)
		{
			this.m_foodRegenTimer += dt;
			if (this.m_foodRegenTimer >= 10f)
			{
				this.m_foodRegenTimer = 0f;
				float num3 = 0f;
				foreach (Player.Food food2 in this.m_foods)
				{
					num3 += food2.m_item.m_shared.m_foodRegen;
				}
				if (num3 > 0f)
				{
					float num4 = 1f;
					this.m_seman.ModifyHealthRegen(ref num4);
					num3 *= num4;
					base.Heal(num3, true);
				}
			}
		}
	}

	// Token: 0x0600033D RID: 829 RVA: 0x0001E4AC File Offset: 0x0001C6AC
	private void GetTotalFoodValue(out float hp, out float stamina, out float eitr)
	{
		hp = this.m_baseHP;
		stamina = this.m_baseStamina;
		eitr = 0f;
		foreach (Player.Food food in this.m_foods)
		{
			hp += food.m_health;
			stamina += food.m_stamina;
			eitr += food.m_eitr;
		}
	}

	// Token: 0x0600033E RID: 830 RVA: 0x0001E530 File Offset: 0x0001C730
	public float GetBaseFoodHP()
	{
		return this.m_baseHP;
	}

	// Token: 0x0600033F RID: 831 RVA: 0x0001E538 File Offset: 0x0001C738
	public List<Player.Food> GetFoods()
	{
		return this.m_foods;
	}

	// Token: 0x06000340 RID: 832 RVA: 0x0001E540 File Offset: 0x0001C740
	public void OnSpawned(bool spawnValkyrie)
	{
		this.m_spawnEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		if (spawnValkyrie)
		{
			this.SetIntro(true);
			this.SpawnValkyrie();
		}
	}

	// Token: 0x06000341 RID: 833 RVA: 0x0001E578 File Offset: 0x0001C778
	private void SpawnValkyrie()
	{
		this.m_valkyrie.Load();
		UnityEngine.Object.Instantiate<GameObject>(this.m_valkyrie.Asset, base.transform.position, Quaternion.identity).GetComponent<ZNetView>().HoldReferenceTo(this.m_valkyrie);
		this.m_valkyrie.Release();
	}

	// Token: 0x06000342 RID: 834 RVA: 0x0001E5D4 File Offset: 0x0001C7D4
	protected override bool CheckRun(Vector3 moveDir, float dt)
	{
		if (!base.CheckRun(moveDir, dt))
		{
			return false;
		}
		bool flag = this.HaveStamina(0f);
		float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Run);
		float num = Mathf.Lerp(1f, 0.5f, skillFactor);
		float num2 = this.m_runStaminaDrain * num;
		num2 -= num2 * this.GetEquipmentMovementModifier();
		num2 += num2 * this.GetEquipmentRunStaminaModifier();
		this.m_seman.ModifyRunStaminaDrain(num2, ref num2, moveDir, true);
		this.UseStamina(dt * num2 * Game.m_moveStaminaRate);
		if (this.HaveStamina(0f))
		{
			this.m_runSkillImproveTimer += dt;
			if (this.m_runSkillImproveTimer > 1f)
			{
				this.m_runSkillImproveTimer = 0f;
				this.RaiseSkill(Skills.SkillType.Run, 1f);
			}
			this.ClearActionQueue();
			return true;
		}
		if (flag)
		{
			Hud.instance.StaminaBarEmptyFlash();
		}
		return false;
	}

	// Token: 0x06000343 RID: 835 RVA: 0x0001E6AC File Offset: 0x0001C8AC
	private void UpdateModifiers()
	{
		if (Player.s_equipmentModifierSourceFields == null)
		{
			return;
		}
		for (int i = 0; i < this.m_equipmentModifierValues.Length; i++)
		{
			float num = 0f;
			if (this.m_rightItem != null)
			{
				num += (float)Player.s_equipmentModifierSourceFields[i].GetValue(this.m_rightItem.m_shared);
			}
			if (this.m_leftItem != null)
			{
				num += (float)Player.s_equipmentModifierSourceFields[i].GetValue(this.m_leftItem.m_shared);
			}
			if (this.m_chestItem != null)
			{
				num += (float)Player.s_equipmentModifierSourceFields[i].GetValue(this.m_chestItem.m_shared);
			}
			if (this.m_legItem != null)
			{
				num += (float)Player.s_equipmentModifierSourceFields[i].GetValue(this.m_legItem.m_shared);
			}
			if (this.m_helmetItem != null)
			{
				num += (float)Player.s_equipmentModifierSourceFields[i].GetValue(this.m_helmetItem.m_shared);
			}
			if (this.m_shoulderItem != null)
			{
				num += (float)Player.s_equipmentModifierSourceFields[i].GetValue(this.m_shoulderItem.m_shared);
			}
			if (this.m_utilityItem != null)
			{
				num += (float)Player.s_equipmentModifierSourceFields[i].GetValue(this.m_utilityItem.m_shared);
			}
			this.m_equipmentModifierValues[i] = num;
		}
	}

	// Token: 0x06000344 RID: 836 RVA: 0x0001E7FC File Offset: 0x0001C9FC
	public void AppendEquipmentModifierTooltips(ItemDrop.ItemData item, StringBuilder sb)
	{
		for (int i = 0; i < this.m_equipmentModifierValues.Length; i++)
		{
			object value = Player.s_equipmentModifierSourceFields[i].GetValue(item.m_shared);
			if (value is float)
			{
				float num = (float)value;
				if (num != 0f)
				{
					sb.AppendFormat(string.Concat(new string[]
					{
						"\n",
						Player.s_equipmentModifierTooltips[i],
						": <color=orange>",
						(num * 100f).ToString("+0;-0"),
						"%</color> ($item_total:<color=yellow>",
						(this.GetEquipmentModifierPlusSE(i) * 100f).ToString("+0;-0"),
						"%</color>)"
					}), Array.Empty<object>());
				}
			}
		}
	}

	// Token: 0x06000345 RID: 837 RVA: 0x0001E8C4 File Offset: 0x0001CAC4
	public void OnSkillLevelup(Skills.SkillType skill, float level)
	{
		this.m_skillLevelupEffects.Create(this.m_head.position, this.m_head.rotation, this.m_head, 1f, -1);
	}

	// Token: 0x06000346 RID: 838 RVA: 0x0001E8F4 File Offset: 0x0001CAF4
	protected override void OnJump()
	{
		this.ClearActionQueue();
		float num = this.m_jumpStaminaUsage - this.m_jumpStaminaUsage * this.GetEquipmentMovementModifier() + this.m_jumpStaminaUsage * this.GetEquipmentJumpStaminaModifier();
		this.m_seman.ModifyJumpStaminaUsage(num, ref num, true);
		this.UseStamina(num * Game.m_moveStaminaRate);
		Game.instance.IncrementPlayerStat(PlayerStatType.Jumps, 1f);
	}

	// Token: 0x06000347 RID: 839 RVA: 0x0001E958 File Offset: 0x0001CB58
	protected override void OnSwimming(Vector3 targetVel, float dt)
	{
		base.OnSwimming(targetVel, dt);
		if (targetVel.magnitude > 0.1f)
		{
			float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Swim);
			float num = Mathf.Lerp(this.m_swimStaminaDrainMinSkill, this.m_swimStaminaDrainMaxSkill, skillFactor);
			num += num * this.GetEquipmentSwimStaminaModifier();
			this.m_seman.ModifySwimStaminaUsage(num, ref num, true);
			this.UseStamina(dt * num * Game.m_moveStaminaRate);
			this.m_swimSkillImproveTimer += dt;
			if (this.m_swimSkillImproveTimer > 1f)
			{
				this.m_swimSkillImproveTimer = 0f;
				this.RaiseSkill(Skills.SkillType.Swim, 1f);
			}
		}
		if (!this.HaveStamina(0f))
		{
			this.m_drownDamageTimer += dt;
			if (this.m_drownDamageTimer > 1f)
			{
				this.m_drownDamageTimer = 0f;
				float damage = Mathf.Ceil(base.GetMaxHealth() / 20f);
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = damage;
				hitData.m_point = base.GetCenterPoint();
				hitData.m_dir = Vector3.down;
				hitData.m_pushForce = 10f;
				hitData.m_hitType = HitData.HitType.Drowning;
				base.Damage(hitData);
				Vector3 position = base.transform.position;
				position.y = base.GetLiquidLevel();
				this.m_drownEffects.Create(position, base.transform.rotation, null, 1f, -1);
			}
		}
	}

	// Token: 0x06000348 RID: 840 RVA: 0x0001EAC0 File Offset: 0x0001CCC0
	protected override bool TakeInput()
	{
		bool result = (!Chat.instance || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !TextInput.IsVisible() && (!StoreGui.IsVisible() && !InventoryGui.IsVisible() && !Menu.IsVisible() && (!TextViewer.instance || !TextViewer.instance.IsVisible()) && !Minimap.IsOpen() && !GameCamera.InFreeFly()) && !PlayerCustomizaton.IsBarberGuiVisible();
		if (this.IsDead() || this.InCutscene() || this.IsTeleporting())
		{
			result = false;
		}
		return result;
	}

	// Token: 0x06000349 RID: 841 RVA: 0x0001EB58 File Offset: 0x0001CD58
	public void UseHotbarItem(int index)
	{
		ItemDrop.ItemData itemAt = this.m_inventory.GetItemAt(index - 1, 0);
		if (itemAt != null)
		{
			base.UseItem(null, itemAt, false);
		}
	}

	// Token: 0x0600034A RID: 842 RVA: 0x0001EB84 File Offset: 0x0001CD84
	public bool RequiredCraftingStation(Recipe recipe, int qualityLevel, bool checkLevel)
	{
		CraftingStation requiredStation = recipe.GetRequiredStation(qualityLevel);
		if (requiredStation != null)
		{
			if (this.m_currentStation == null)
			{
				return false;
			}
			if (requiredStation.m_name != this.m_currentStation.m_name)
			{
				return false;
			}
			if (checkLevel)
			{
				int requiredStationLevel = recipe.GetRequiredStationLevel(qualityLevel);
				if (this.m_currentStation.GetLevel(true) < requiredStationLevel)
				{
					return false;
				}
			}
		}
		else if (this.m_currentStation != null && !this.m_currentStation.m_showBasicRecipies)
		{
			return false;
		}
		return true;
	}

	// Token: 0x0600034B RID: 843 RVA: 0x0001EC08 File Offset: 0x0001CE08
	public bool HaveRequirements(Recipe recipe, bool discover, int qualityLevel, int amount = 1)
	{
		if (discover)
		{
			if (recipe.m_craftingStation && !this.KnowStationLevel(recipe.m_craftingStation.m_name, recipe.m_minStationLevel))
			{
				return false;
			}
		}
		else if (!this.RequiredCraftingStation(recipe, qualityLevel, true))
		{
			return false;
		}
		return (recipe.m_item.m_itemData.m_shared.m_dlc.Length <= 0 || DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc)) && this.HaveRequirementItems(recipe, discover, qualityLevel, amount);
	}

	// Token: 0x0600034C RID: 844 RVA: 0x0001ECA0 File Offset: 0x0001CEA0
	private bool HaveRequirementItems(Recipe piece, bool discover, int qualityLevel, int amount = 1)
	{
		foreach (Piece.Requirement requirement in piece.m_resources)
		{
			if (requirement.m_resItem)
			{
				if (discover)
				{
					if (requirement.m_amount > 0)
					{
						if (piece.m_requireOnlyOneIngredient)
						{
							if (this.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
							{
								return true;
							}
						}
						else if (!this.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
						{
							return false;
						}
					}
				}
				else
				{
					int num = requirement.GetAmount(qualityLevel) * amount;
					int num2 = 0;
					for (int j = 1; j < requirement.m_resItem.m_itemData.m_shared.m_maxQuality + 1; j++)
					{
						int num3 = this.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name, j, true);
						if (num3 > num2)
						{
							num2 = num3;
						}
					}
					if (piece.m_requireOnlyOneIngredient)
					{
						if (num2 >= num)
						{
							return true;
						}
					}
					else if (num2 < num)
					{
						return false;
					}
				}
			}
		}
		return !piece.m_requireOnlyOneIngredient;
	}

	// Token: 0x0600034D RID: 845 RVA: 0x0001EDC4 File Offset: 0x0001CFC4
	public ItemDrop.ItemData GetFirstRequiredItem(Inventory inventory, Recipe recipe, int qualityLevel, out int amount, out int extraAmount, int craftMultiplier = 1)
	{
		foreach (Piece.Requirement requirement in recipe.m_resources)
		{
			if (requirement.m_resItem)
			{
				int num = requirement.GetAmount(qualityLevel) * craftMultiplier;
				for (int j = 0; j <= requirement.m_resItem.m_itemData.m_shared.m_maxQuality; j++)
				{
					if (this.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name, j, true) >= num)
					{
						amount = num;
						extraAmount = requirement.m_extraAmountOnlyOneIngredient;
						return inventory.GetItem(requirement.m_resItem.m_itemData.m_shared.m_name, j, false);
					}
				}
			}
		}
		amount = 0;
		extraAmount = 0;
		return null;
	}

	// Token: 0x0600034E RID: 846 RVA: 0x0001EE8C File Offset: 0x0001D08C
	public bool HaveRequirements(Piece piece, Player.RequirementMode mode)
	{
		if (piece.m_craftingStation)
		{
			if (mode == Player.RequirementMode.IsKnown || mode == Player.RequirementMode.CanAlmostBuild)
			{
				if (!this.m_knownStations.ContainsKey(piece.m_craftingStation.m_name))
				{
					return false;
				}
			}
			else if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, base.transform.position) && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
			{
				return false;
			}
		}
		if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
		{
			return false;
		}
		if (mode != Player.RequirementMode.IsKnown && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
		{
			return true;
		}
		foreach (Piece.Requirement requirement in piece.m_resources)
		{
			if (requirement.m_resItem && requirement.m_amount > 0)
			{
				if (mode == Player.RequirementMode.IsKnown)
				{
					if (!this.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
					{
						return false;
					}
				}
				else if (mode == Player.RequirementMode.CanAlmostBuild)
				{
					if (!this.m_inventory.HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name, true))
					{
						return false;
					}
				}
				else if (mode == Player.RequirementMode.CanBuild && this.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name, -1, true) < requirement.m_amount)
				{
					return false;
				}
			}
		}
		return true;
	}

	// Token: 0x0600034F RID: 847 RVA: 0x0001EFF0 File Offset: 0x0001D1F0
	public void ConsumeResources(Piece.Requirement[] requirements, int qualityLevel, int itemQuality = -1, int multiplier = 1)
	{
		foreach (Piece.Requirement requirement in requirements)
		{
			if (requirement.m_resItem)
			{
				int num = requirement.GetAmount(qualityLevel) * multiplier;
				if (num > 0)
				{
					this.m_inventory.RemoveItem(requirement.m_resItem.m_itemData.m_shared.m_name, num, itemQuality, true);
				}
			}
		}
	}

	// Token: 0x06000350 RID: 848 RVA: 0x0001F050 File Offset: 0x0001D250
	private void UpdateHover()
	{
		if (this.InPlaceMode() || this.IsDead() || this.m_doodadController != null)
		{
			this.m_hovering = null;
			this.m_hoveringCreature = null;
			return;
		}
		this.FindHoverObject(out this.m_hovering, out this.m_hoveringCreature);
	}

	// Token: 0x06000351 RID: 849 RVA: 0x0001F08B File Offset: 0x0001D28B
	public bool IsMaterialKnown(string sharedName)
	{
		return this.m_knownMaterial.Contains(sharedName);
	}

	// Token: 0x06000352 RID: 850 RVA: 0x0001F09C File Offset: 0x0001D29C
	private bool CheckCanRemovePiece(Piece piece)
	{
		if (!this.m_noPlacementCost && piece.m_craftingStation != null && !CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, base.transform.position) && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
		{
			this.Message(MessageHud.MessageType.Center, "$msg_missingstation", 0, null);
			return false;
		}
		return true;
	}

	// Token: 0x06000353 RID: 851 RVA: 0x0001F100 File Offset: 0x0001D300
	private bool CopyPiece()
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, this.m_removeRayMask) && Vector3.Distance(raycastHit.point, this.m_eye.position) < this.m_maxPlaceDistance)
		{
			Piece piece = raycastHit.collider.GetComponentInParent<Piece>();
			if (piece == null && raycastHit.collider.GetComponent<Heightmap>())
			{
				piece = TerrainModifier.FindClosestModifierPieceInRange(raycastHit.point, 2.5f);
			}
			if (piece)
			{
				if (this.SetSelectedPiece(piece))
				{
					this.m_placeRotation = (int)Math.Round((double)(piece.transform.rotation.eulerAngles.y / this.m_placeRotationDegrees));
					return true;
				}
				this.Message(MessageHud.MessageType.Center, "$msg_missingrequirement", 0, null);
				return false;
			}
		}
		return false;
	}

	// Token: 0x06000354 RID: 852 RVA: 0x0001F1F0 File Offset: 0x0001D3F0
	private bool RemovePiece()
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, this.m_removeRayMask) && Vector3.Distance(raycastHit.point, this.m_eye.position) < this.m_maxPlaceDistance)
		{
			Piece piece = raycastHit.collider.GetComponentInParent<Piece>();
			if (piece == null && raycastHit.collider.GetComponent<Heightmap>())
			{
				piece = TerrainModifier.FindClosestModifierPieceInRange(raycastHit.point, 2.5f);
			}
			if (piece)
			{
				if (!piece.m_canBeRemoved)
				{
					return false;
				}
				if (Location.IsInsideNoBuildLocation(piece.transform.position))
				{
					this.Message(MessageHud.MessageType.Center, "$msg_nobuildzone", 0, null);
					return false;
				}
				if (!PrivateArea.CheckAccess(piece.transform.position, 0f, true, false))
				{
					this.Message(MessageHud.MessageType.Center, "$msg_privatezone", 0, null);
					return false;
				}
				if (!this.CheckCanRemovePiece(piece))
				{
					return false;
				}
				ZNetView component = piece.GetComponent<ZNetView>();
				if (component == null)
				{
					return false;
				}
				if (!piece.CanBeRemoved())
				{
					this.Message(MessageHud.MessageType.Center, "$msg_cantremovenow", 0, null);
					return false;
				}
				IRemoved component2 = piece.GetComponent<IRemoved>();
				if (component2 != null)
				{
					component2.OnRemoved();
				}
				WearNTear component3 = piece.GetComponent<WearNTear>();
				if (component3)
				{
					component3.Remove(false);
				}
				else
				{
					ZLog.Log("Removing non WNT object with hammer " + piece.name);
					component.ClaimOwnership();
					piece.DropResources(null);
					piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, piece.gameObject.transform, 1f, -1);
					this.m_removeEffects.Create(piece.transform.position, Quaternion.identity, null, 1f, -1);
					ZNetScene.instance.Destroy(piece.gameObject);
				}
				ItemDrop.ItemData rightItem = base.GetRightItem();
				if (rightItem != null)
				{
					this.FaceLookDirection();
					this.m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
				}
				return true;
			}
		}
		return false;
	}

	// Token: 0x06000355 RID: 853 RVA: 0x0001F40B File Offset: 0x0001D60B
	public void FaceLookDirection()
	{
		base.transform.rotation = base.GetLookYaw();
		Physics.SyncTransforms();
	}

	// Token: 0x06000356 RID: 854 RVA: 0x0001F424 File Offset: 0x0001D624
	public bool TryPlacePiece(Piece piece)
	{
		this.UpdatePlacementGhost(true);
		switch (this.m_placementStatus)
		{
		case Player.PlacementStatus.Invalid:
		case Player.PlacementStatus.NoRayHits:
			this.Message(MessageHud.MessageType.Center, "$msg_invalidplacement", 0, null);
			return false;
		case Player.PlacementStatus.BlockedbyPlayer:
			this.Message(MessageHud.MessageType.Center, "$msg_blocked", 0, null);
			return false;
		case Player.PlacementStatus.NoBuildZone:
			this.Message(MessageHud.MessageType.Center, "$msg_nobuildzone", 0, null);
			return false;
		case Player.PlacementStatus.PrivateZone:
			this.Message(MessageHud.MessageType.Center, "$msg_privatezone", 0, null);
			return false;
		case Player.PlacementStatus.MoreSpace:
			this.Message(MessageHud.MessageType.Center, "$msg_needspace", 0, null);
			return false;
		case Player.PlacementStatus.NoTeleportArea:
			this.Message(MessageHud.MessageType.Center, "$msg_noteleportarea", 0, null);
			return false;
		case Player.PlacementStatus.ExtensionMissingStation:
			this.Message(MessageHud.MessageType.Center, "$msg_extensionmissingstation", 0, null);
			return false;
		case Player.PlacementStatus.WrongBiome:
			this.Message(MessageHud.MessageType.Center, "$msg_wrongbiome", 0, null);
			return false;
		case Player.PlacementStatus.NeedCultivated:
			this.Message(MessageHud.MessageType.Center, "$msg_needcultivated", 0, null);
			return false;
		case Player.PlacementStatus.NeedDirt:
			this.Message(MessageHud.MessageType.Center, "$msg_needdirt", 0, null);
			return false;
		case Player.PlacementStatus.NotInDungeon:
			this.Message(MessageHud.MessageType.Center, "$msg_notindungeon", 0, null);
			return false;
		default:
			ZLog.Log("Placed " + piece.gameObject.name);
			Gogan.LogEvent("Game", "PlacedPiece", piece.gameObject.name, 0L);
			Game.instance.IncrementPlayerStat(PlayerStatType.Builds, 1f);
			this.PlacePiece(piece, this.m_placementGhost.transform.position, this.m_placementGhost.transform.rotation, true);
			return true;
		}
	}

	// Token: 0x06000357 RID: 855 RVA: 0x0001F59C File Offset: 0x0001D79C
	public void PlacePiece(Piece piece, Vector3 pos, Quaternion rot, bool doAttack = true)
	{
		GameObject gameObject = piece.gameObject;
		TerrainModifier.SetTriggerOnPlaced(true);
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, pos, rot);
		TerrainModifier.SetTriggerOnPlaced(false);
		CraftingStation componentInChildren = gameObject2.GetComponentInChildren<CraftingStation>();
		if (componentInChildren)
		{
			this.AddKnownStation(componentInChildren);
		}
		Piece component = gameObject2.GetComponent<Piece>();
		if (component)
		{
			component.SetCreator(this.GetPlayerID());
		}
		PrivateArea component2 = gameObject2.GetComponent<PrivateArea>();
		if (component2 != null)
		{
			component2.Setup(Game.instance.GetPlayerProfile().GetName());
		}
		WearNTear component3 = gameObject2.GetComponent<WearNTear>();
		if (component3 != null)
		{
			component3.OnPlaced();
		}
		if (doAttack)
		{
			ItemDrop.ItemData rightItem = base.GetRightItem();
			if (rightItem != null)
			{
				this.FaceLookDirection();
				this.m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
			}
		}
		if (piece.m_randomInitBuildRotation)
		{
			this.m_placeRotation = UnityEngine.Random.Range(0, 16);
		}
		ItemDrop component4 = gameObject2.gameObject.GetComponent<ItemDrop>();
		if (component4 != null)
		{
			component4.MakePiece(true);
		}
		Player.m_placed.Clear();
		gameObject2.GetComponents<IPlaced>(Player.m_placed);
		foreach (IPlaced placed in Player.m_placed)
		{
			placed.OnPlaced();
		}
		piece.m_placeEffect.Create(pos, rot, gameObject2.transform, 1f, -1);
		base.AddNoise(50f);
	}

	// Token: 0x06000358 RID: 856 RVA: 0x0001F704 File Offset: 0x0001D904
	public override bool IsPlayer()
	{
		return true;
	}

	// Token: 0x06000359 RID: 857 RVA: 0x0001F708 File Offset: 0x0001D908
	public void GetBuildSelection(out Piece go, out Vector2Int id, out int total, out Piece.PieceCategory category, out PieceTable pieceTable)
	{
		category = this.m_buildPieces.GetSelectedCategory();
		pieceTable = this.m_buildPieces;
		if (this.m_buildPieces.GetAvailablePiecesInSelectedCategory() == 0)
		{
			go = null;
			id = Vector2Int.zero;
			total = 0;
			return;
		}
		GameObject selectedPrefab = this.m_buildPieces.GetSelectedPrefab();
		go = (selectedPrefab ? selectedPrefab.GetComponent<Piece>() : null);
		id = this.m_buildPieces.GetSelectedIndex();
		total = this.m_buildPieces.GetAvailablePiecesInSelectedCategory();
	}

	// Token: 0x0600035A RID: 858 RVA: 0x0001F788 File Offset: 0x0001D988
	public List<Piece> GetBuildPieces()
	{
		if (!(this.m_buildPieces != null))
		{
			return null;
		}
		return this.m_buildPieces.GetPiecesInSelectedCategory();
	}

	// Token: 0x0600035B RID: 859 RVA: 0x0001F7A5 File Offset: 0x0001D9A5
	public int GetAvailableBuildPiecesInCategory(Piece.PieceCategory cat)
	{
		if (!(this.m_buildPieces != null))
		{
			return 0;
		}
		return this.m_buildPieces.GetAvailablePiecesInCategory(cat);
	}

	// Token: 0x0600035C RID: 860 RVA: 0x0001F7C3 File Offset: 0x0001D9C3
	private void RPC_OnDeath(long sender)
	{
		this.m_visual.SetActive(false);
	}

	// Token: 0x0600035D RID: 861 RVA: 0x0001F7D4 File Offset: 0x0001D9D4
	private void CreateDeathEffects()
	{
		GameObject[] array = this.m_deathEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		for (int i = 0; i < array.Length; i++)
		{
			Ragdoll component = array[i].GetComponent<Ragdoll>();
			if (component)
			{
				Vector3 velocity = this.m_body.velocity;
				if (this.m_pushForce.magnitude * 0.5f > velocity.magnitude)
				{
					velocity = this.m_pushForce * 0.5f;
				}
				component.Setup(velocity, 0f, 0f, 0f, null);
				this.OnRagdollCreated(component);
				this.m_ragdoll = component;
			}
		}
	}

	// Token: 0x0600035E RID: 862 RVA: 0x0001F88C File Offset: 0x0001DA8C
	public void UnequipDeathDropItems()
	{
		if (this.m_rightItem != null)
		{
			base.UnequipItem(this.m_rightItem, false);
		}
		if (this.m_leftItem != null)
		{
			base.UnequipItem(this.m_leftItem, false);
		}
		if (this.m_ammoItem != null)
		{
			base.UnequipItem(this.m_ammoItem, false);
		}
		if (this.m_utilityItem != null)
		{
			base.UnequipItem(this.m_utilityItem, false);
		}
	}

	// Token: 0x0600035F RID: 863 RVA: 0x0001F8F0 File Offset: 0x0001DAF0
	public void CreateTombStone()
	{
		if (this.m_inventory.NrOfItems() == 0)
		{
			return;
		}
		if (!ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathKeepEquip) && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathDeleteUnequipped))
		{
			base.UnequipAllItems();
		}
		if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathDeleteItems) || ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathDeleteUnequipped))
		{
			this.m_inventory.RemoveUnequipped();
		}
		if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathDeleteUnequipped) && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathKeepEquip))
		{
			base.UnequipAllItems();
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_tombstone, base.GetCenterPoint(), base.transform.rotation);
		gameObject.GetComponent<Container>().GetInventory().MoveInventoryToGrave(this.m_inventory);
		TombStone component = gameObject.GetComponent<TombStone>();
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
	}

	// Token: 0x06000360 RID: 864 RVA: 0x0001F9C9 File Offset: 0x0001DBC9
	private bool HardDeath()
	{
		return this.m_timeSinceDeath > this.m_hardDeathCooldown;
	}

	// Token: 0x06000361 RID: 865 RVA: 0x0001F9D9 File Offset: 0x0001DBD9
	public void ClearHardDeath()
	{
		this.m_timeSinceDeath = this.m_hardDeathCooldown + 1f;
	}

	// Token: 0x06000362 RID: 866 RVA: 0x0001F9F0 File Offset: 0x0001DBF0
	protected override void OnDeath()
	{
		if (!this.m_nview.IsOwner())
		{
			Debug.Log("OnDeath call but not the owner");
			return;
		}
		bool flag = this.HardDeath();
		this.m_nview.GetZDO().Set(ZDOVars.s_dead, true);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "OnDeath", Array.Empty<object>());
		Game.instance.IncrementPlayerStat(PlayerStatType.Deaths, 1f);
		switch (this.m_lastHit.m_hitType)
		{
		case HitData.HitType.Undefined:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByUndefined, 1f);
			break;
		case HitData.HitType.EnemyHit:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByEnemyHit, 1f);
			break;
		case HitData.HitType.PlayerHit:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByPlayerHit, 1f);
			break;
		case HitData.HitType.Fall:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByFall, 1f);
			break;
		case HitData.HitType.Drowning:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByDrowning, 1f);
			break;
		case HitData.HitType.Burning:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByBurning, 1f);
			break;
		case HitData.HitType.Freezing:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByFreezing, 1f);
			break;
		case HitData.HitType.Poisoned:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByPoisoned, 1f);
			break;
		case HitData.HitType.Water:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByWater, 1f);
			break;
		case HitData.HitType.Smoke:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathBySmoke, 1f);
			break;
		case HitData.HitType.EdgeOfWorld:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByEdgeOfWorld, 1f);
			break;
		case HitData.HitType.Impact:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByImpact, 1f);
			break;
		case HitData.HitType.Cart:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByCart, 1f);
			break;
		case HitData.HitType.Tree:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByTree, 1f);
			break;
		case HitData.HitType.Self:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathBySelf, 1f);
			break;
		case HitData.HitType.Structural:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByStructural, 1f);
			break;
		case HitData.HitType.Turret:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByTurret, 1f);
			break;
		case HitData.HitType.Boat:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByBoat, 1f);
			break;
		case HitData.HitType.Stalagtite:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByStalagtite, 1f);
			break;
		default:
			ZLog.LogWarning("Not implemented death type " + this.m_lastHit.m_hitType.ToString());
			break;
		}
		Game.instance.GetPlayerProfile().SetDeathPoint(base.transform.position);
		this.CreateDeathEffects();
		this.CreateTombStone();
		this.m_foods.Clear();
		if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathSkillsReset))
		{
			this.m_skills.Clear();
		}
		else if (flag)
		{
			this.m_skills.OnDeath();
		}
		this.m_seman.RemoveAllStatusEffects(false);
		Game.instance.RequestRespawn(10f, true);
		this.m_timeSinceDeath = 0f;
		if (!flag)
		{
			this.Message(MessageHud.MessageType.TopLeft, "$msg_softdeath", 0, null);
		}
		this.Message(MessageHud.MessageType.Center, "$msg_youdied", 0, null);
		this.ShowTutorial("death", false);
		Minimap.instance.AddPin(base.transform.position, Minimap.PinType.Death, string.Format("$hud_mapday {0}", EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds())), true, false, 0L, default(PlatformUserID));
		if (this.m_onDeath != null)
		{
			this.m_onDeath();
		}
		string eventLabel = "biome:" + this.GetCurrentBiome().ToString();
		Gogan.LogEvent("Game", "Death", eventLabel, 0L);
	}

	// Token: 0x06000363 RID: 867 RVA: 0x0001FDAF File Offset: 0x0001DFAF
	public void OnRespawn()
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_dead, false);
		base.SetHealth(base.GetMaxHealth());
	}

	// Token: 0x06000364 RID: 868 RVA: 0x0001FDD4 File Offset: 0x0001DFD4
	private void SetupPlacementGhost()
	{
		if (this.m_placementGhost)
		{
			UnityEngine.Object.Destroy(this.m_placementGhost);
			this.m_placementGhost = null;
		}
		if (this.m_buildPieces == null || this.IsDead())
		{
			return;
		}
		GameObject selectedPrefab = this.m_buildPieces.GetSelectedPrefab();
		if (selectedPrefab == null)
		{
			return;
		}
		Piece component = selectedPrefab.GetComponent<Piece>();
		if (component.m_repairPiece || component.m_removePiece)
		{
			return;
		}
		bool enabled = false;
		TerrainModifier componentInChildren = selectedPrefab.GetComponentInChildren<TerrainModifier>();
		if (componentInChildren)
		{
			enabled = componentInChildren.enabled;
			componentInChildren.enabled = false;
		}
		TerrainOp.m_forceDisableTerrainOps = true;
		ZNetView.m_forceDisableInit = true;
		GameObject placementGhost = this.m_placementGhost;
		this.m_placementGhost = UnityEngine.Object.Instantiate<GameObject>(selectedPrefab);
		Piece component2 = this.m_placementGhost.GetComponent<Piece>();
		if (component2 != null && component2.m_randomInitBuildRotation)
		{
			this.m_placeRotation = UnityEngine.Random.Range(0, 16);
		}
		ItemDrop component3 = this.m_placementGhost.gameObject.GetComponent<ItemDrop>();
		if (component3 != null)
		{
			component3.MakePiece(false);
		}
		ZNetView.m_forceDisableInit = false;
		TerrainOp.m_forceDisableTerrainOps = false;
		this.m_placementGhost.name = selectedPrefab.name;
		if (this.m_placementGhostLast != this.m_placementGhost.name)
		{
			this.m_manualSnapPoint = -1;
		}
		this.m_placementGhostLast = this.m_placementGhost.name;
		if (componentInChildren)
		{
			componentInChildren.enabled = enabled;
		}
		Joint[] componentsInChildren = this.m_placementGhost.GetComponentsInChildren<Joint>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren[i]);
		}
		Rigidbody[] componentsInChildren2 = this.m_placementGhost.GetComponentsInChildren<Rigidbody>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren2[i]);
		}
		ParticleSystemForceField[] componentsInChildren3 = this.m_placementGhost.GetComponentsInChildren<ParticleSystemForceField>();
		for (int i = 0; i < componentsInChildren3.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren3[i]);
		}
		Demister[] componentsInChildren4 = this.m_placementGhost.GetComponentsInChildren<Demister>();
		for (int i = 0; i < componentsInChildren4.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren4[i]);
		}
		foreach (Collider collider in this.m_placementGhost.GetComponentsInChildren<Collider>())
		{
			if ((1 << collider.gameObject.layer & this.m_placeRayMask) == 0)
			{
				ZLog.Log("Disabling " + collider.gameObject.name + "  " + LayerMask.LayerToName(collider.gameObject.layer));
				collider.enabled = false;
			}
		}
		Transform[] componentsInChildren6 = this.m_placementGhost.GetComponentsInChildren<Transform>();
		int layer = LayerMask.NameToLayer("ghost");
		Transform[] array = componentsInChildren6;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.layer = layer;
		}
		TerrainModifier[] componentsInChildren7 = this.m_placementGhost.GetComponentsInChildren<TerrainModifier>();
		for (int i = 0; i < componentsInChildren7.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren7[i]);
		}
		GuidePoint[] componentsInChildren8 = this.m_placementGhost.GetComponentsInChildren<GuidePoint>();
		for (int i = 0; i < componentsInChildren8.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren8[i]);
		}
		LightLod[] componentsInChildren9 = this.m_placementGhost.GetComponentsInChildren<LightLod>();
		for (int i = 0; i < componentsInChildren9.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren9[i]);
		}
		LightFlicker[] componentsInChildren10 = this.m_placementGhost.GetComponentsInChildren<LightFlicker>();
		for (int i = 0; i < componentsInChildren10.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren10[i]);
		}
		Light[] componentsInChildren11 = this.m_placementGhost.GetComponentsInChildren<Light>();
		for (int i = 0; i < componentsInChildren11.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren11[i]);
		}
		AudioSource[] componentsInChildren12 = this.m_placementGhost.GetComponentsInChildren<AudioSource>();
		for (int i = 0; i < componentsInChildren12.Length; i++)
		{
			componentsInChildren12[i].enabled = false;
		}
		ZSFX[] componentsInChildren13 = this.m_placementGhost.GetComponentsInChildren<ZSFX>();
		for (int i = 0; i < componentsInChildren13.Length; i++)
		{
			componentsInChildren13[i].enabled = false;
		}
		WispSpawner componentInChildren2 = this.m_placementGhost.GetComponentInChildren<WispSpawner>();
		if (componentInChildren2)
		{
			UnityEngine.Object.Destroy(componentInChildren2);
		}
		Windmill componentInChildren3 = this.m_placementGhost.GetComponentInChildren<Windmill>();
		if (componentInChildren3)
		{
			componentInChildren3.enabled = false;
		}
		ParticleSystem[] componentsInChildren14 = this.m_placementGhost.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren14.Length; i++)
		{
			componentsInChildren14[i].gameObject.SetActive(false);
		}
		Transform transform = this.m_placementGhost.transform.Find("_GhostOnly");
		if (transform)
		{
			transform.gameObject.SetActive(true);
		}
		this.m_placementGhost.transform.position = base.transform.position;
		this.m_placementGhost.transform.localScale = selectedPrefab.transform.localScale;
		this.m_ghostRippleDistance.Clear();
		this.CleanupGhostMaterials<MeshRenderer>(this.m_placementGhost);
		this.CleanupGhostMaterials<SkinnedMeshRenderer>(this.m_placementGhost);
	}

	// Token: 0x06000365 RID: 869 RVA: 0x0002029D File Offset: 0x0001E49D
	public static bool IsPlacementGhost(GameObject obj)
	{
		return Player.m_localPlayer && obj == Player.m_localPlayer.m_placementGhost;
	}

	// Token: 0x06000366 RID: 870 RVA: 0x000202BC File Offset: 0x0001E4BC
	private void CleanupGhostMaterials<T>(GameObject ghost) where T : Renderer
	{
		foreach (T t in this.m_placementGhost.GetComponentsInChildren<T>())
		{
			if (!(t.sharedMaterial == null))
			{
				Material[] sharedMaterials = t.sharedMaterials;
				for (int j = 0; j < sharedMaterials.Length; j++)
				{
					Material material = new Material(sharedMaterials[j]);
					if (material.HasProperty("_RippleDistance"))
					{
						this.m_ghostRippleDistance[material] = material.GetFloat("_RippleDistance");
					}
					material.SetFloat("_ValueNoise", 0f);
					material.SetFloat("_TriplanarLocalPos", 1f);
					sharedMaterials[j] = material;
				}
				t.sharedMaterials = sharedMaterials;
				t.shadowCastingMode = ShadowCastingMode.Off;
			}
		}
	}

	// Token: 0x06000367 RID: 871 RVA: 0x00020397 File Offset: 0x0001E597
	private void SetPlacementGhostValid(bool valid)
	{
		this.m_placementGhost.GetComponent<Piece>().SetInvalidPlacementHeightlight(!valid);
	}

	// Token: 0x06000368 RID: 872 RVA: 0x000203AD File Offset: 0x0001E5AD
	protected override void SetPlaceMode(PieceTable buildPieces)
	{
		base.SetPlaceMode(buildPieces);
		this.m_buildPieces = buildPieces;
		this.UpdateAvailablePiecesList();
	}

	// Token: 0x06000369 RID: 873 RVA: 0x000203C3 File Offset: 0x0001E5C3
	public void SetBuildCategory(int index)
	{
		if (this.m_buildPieces != null)
		{
			this.m_buildPieces.SetCategory(index);
			this.UpdateAvailablePiecesList();
		}
	}

	// Token: 0x0600036A RID: 874 RVA: 0x000203E5 File Offset: 0x0001E5E5
	public override bool InPlaceMode()
	{
		return this.m_buildPieces != null;
	}

	// Token: 0x0600036B RID: 875 RVA: 0x000203F4 File Offset: 0x0001E5F4
	public bool InRepairMode()
	{
		if (this.InPlaceMode())
		{
			Piece selectedPiece = this.m_buildPieces.GetSelectedPiece();
			if (selectedPiece != null)
			{
				return selectedPiece.m_repairPiece || selectedPiece.m_removePiece;
			}
		}
		return false;
	}

	// Token: 0x0600036C RID: 876 RVA: 0x0002042A File Offset: 0x0001E62A
	public Player.PlacementStatus GetPlacementStatus()
	{
		return this.m_placementStatus;
	}

	// Token: 0x0600036D RID: 877 RVA: 0x00020434 File Offset: 0x0001E634
	public bool CanRotatePiece()
	{
		if (this.InPlaceMode())
		{
			Piece selectedPiece = this.m_buildPieces.GetSelectedPiece();
			if (selectedPiece != null)
			{
				return selectedPiece.m_canRotate;
			}
		}
		return false;
	}

	// Token: 0x0600036E RID: 878 RVA: 0x00020460 File Offset: 0x0001E660
	private void Repair(ItemDrop.ItemData toolItem, Piece repairPiece)
	{
		if (!this.InPlaceMode())
		{
			return;
		}
		Piece hoveringPiece = this.GetHoveringPiece();
		if (hoveringPiece)
		{
			if (!this.CheckCanRemovePiece(hoveringPiece))
			{
				return;
			}
			if (!PrivateArea.CheckAccess(hoveringPiece.transform.position, 0f, true, false))
			{
				return;
			}
			bool flag = false;
			WearNTear component = hoveringPiece.GetComponent<WearNTear>();
			if (component && component.Repair())
			{
				flag = true;
			}
			if (flag)
			{
				this.FaceLookDirection();
				this.m_zanim.SetTrigger(toolItem.m_shared.m_attack.m_attackAnimation);
				hoveringPiece.m_placeEffect.Create(hoveringPiece.transform.position, hoveringPiece.transform.rotation, null, 1f, -1);
				this.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_repaired", new string[]
				{
					hoveringPiece.m_name
				}), 0, null);
				this.UseStamina(this.GetBuildStamina());
				this.UseEitr(toolItem.m_shared.m_attack.m_attackEitr);
				if (toolItem.m_shared.m_useDurability)
				{
					toolItem.m_durability -= toolItem.m_shared.m_useDurabilityDrain;
					return;
				}
			}
			else
			{
				this.Message(MessageHud.MessageType.TopLeft, hoveringPiece.m_name + " $msg_doesnotneedrepair", 0, null);
			}
		}
	}

	// Token: 0x0600036F RID: 879 RVA: 0x000205A0 File Offset: 0x0001E7A0
	private void UpdateWearNTearHover()
	{
		if (!this.InPlaceMode())
		{
			this.m_hoveringPiece = null;
			return;
		}
		this.m_hoveringPiece = null;
		RaycastHit raycastHit;
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, this.m_removeRayMask) && Vector3.Distance(this.m_eye.position, raycastHit.point) < this.m_maxPlaceDistance)
		{
			Piece componentInParent = raycastHit.collider.GetComponentInParent<Piece>();
			this.m_hoveringPiece = componentInParent;
			if (componentInParent)
			{
				WearNTear component = componentInParent.GetComponent<WearNTear>();
				if (component)
				{
					component.Highlight();
				}
			}
		}
	}

	// Token: 0x06000370 RID: 880 RVA: 0x00020646 File Offset: 0x0001E846
	public Piece GetHoveringPiece()
	{
		if (!this.InPlaceMode())
		{
			return null;
		}
		return this.m_hoveringPiece;
	}

	// Token: 0x06000371 RID: 881 RVA: 0x00020658 File Offset: 0x0001E858
	private void UpdatePlacementGhost(bool flashGuardStone)
	{
		if (this.m_placementGhost == null)
		{
			if (this.m_placementMarkerInstance)
			{
				this.m_placementMarkerInstance.SetActive(false);
			}
			return;
		}
		bool flag = (ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive()) ? this.m_altPlace : (ZInput.GetButton("AltPlace") || (ZInput.GetButton("JoyAltPlace") && !ZInput.GetButton("JoyRotate")));
		Piece component = this.m_placementGhost.GetComponent<Piece>();
		bool water = component.m_waterPiece || component.m_noInWater;
		Vector3 vector;
		Vector3 up;
		Piece piece;
		Heightmap heightmap;
		Collider x;
		if (this.PieceRayTest(out vector, out up, out piece, out heightmap, out x, water))
		{
			this.m_placementStatus = Player.PlacementStatus.Valid;
			Quaternion rotation = Quaternion.Euler(0f, this.m_placeRotationDegrees * (float)this.m_placeRotation, 0f);
			if (this.m_placementMarkerInstance == null)
			{
				this.m_placementMarkerInstance = UnityEngine.Object.Instantiate<GameObject>(this.m_placeMarker, vector, Quaternion.identity);
			}
			this.m_placementMarkerInstance.SetActive(true);
			this.m_placementMarkerInstance.transform.position = vector;
			this.m_placementMarkerInstance.transform.rotation = Quaternion.LookRotation(up, rotation * Vector3.forward);
			if (component.m_groundOnly || component.m_groundPiece || component.m_cultivatedGroundOnly)
			{
				this.m_placementMarkerInstance.SetActive(false);
			}
			WearNTear wearNTear = (piece != null) ? piece.GetComponent<WearNTear>() : null;
			StationExtension component2 = component.GetComponent<StationExtension>();
			if (component2 != null)
			{
				CraftingStation craftingStation = component2.FindClosestStationInRange(vector);
				if (craftingStation)
				{
					component2.StartConnectionEffect(craftingStation, 1f);
				}
				else
				{
					component2.StopConnectionEffect();
					this.m_placementStatus = Player.PlacementStatus.ExtensionMissingStation;
				}
				if (component2.OtherExtensionInRange(component.m_spaceRequirement))
				{
					this.m_placementStatus = Player.PlacementStatus.MoreSpace;
				}
			}
			if (component.m_blockRadius > 0f && component.m_blockingPieces.Count > 0)
			{
				Collider[] array = Physics.OverlapSphere(vector, component.m_blockRadius, LayerMask.GetMask(new string[]
				{
					"piece"
				}));
				for (int i = 0; i < array.Length; i++)
				{
					Piece componentInParent = array[i].gameObject.GetComponentInParent<Piece>();
					if (componentInParent != null && componentInParent != component)
					{
						using (List<Piece>.Enumerator enumerator = component.m_blockingPieces.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								if (enumerator.Current.m_name == componentInParent.m_name)
								{
									this.m_placementStatus = Player.PlacementStatus.MoreSpace;
									break;
								}
							}
						}
					}
				}
			}
			if (component.m_mustConnectTo != null)
			{
				ZNetView exists = null;
				Collider[] array = Physics.OverlapSphere(component.transform.position, component.m_connectRadius);
				for (int i = 0; i < array.Length; i++)
				{
					ZNetView componentInParent2 = array[i].GetComponentInParent<ZNetView>();
					if (componentInParent2 != null && componentInParent2 != this.m_nview && componentInParent2.name.Contains(component.m_mustConnectTo.name))
					{
						if (component.m_mustBeAboveConnected)
						{
							RaycastHit raycastHit;
							Physics.Raycast(component.transform.position, Vector3.down, out raycastHit);
							if (raycastHit.transform.GetComponentInParent<ZNetView>() != componentInParent2)
							{
								goto IL_31F;
							}
						}
						exists = componentInParent2;
						break;
					}
					IL_31F:;
				}
				if (!exists)
				{
					this.m_placementStatus = Player.PlacementStatus.Invalid;
				}
			}
			if (wearNTear && !wearNTear.m_supports)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_waterPiece && x == null && !flag)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_noInWater && x != null)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_groundPiece && heightmap == null)
			{
				this.m_placementGhost.SetActive(false);
				this.m_placementStatus = Player.PlacementStatus.Invalid;
				return;
			}
			if (component.m_groundOnly && heightmap == null)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_cultivatedGroundOnly && (heightmap == null || !heightmap.IsCultivated(vector)))
			{
				this.m_placementStatus = Player.PlacementStatus.NeedCultivated;
			}
			if (component.m_vegetationGroundOnly)
			{
				bool flag2 = heightmap == null;
				if (!flag2)
				{
					int biome = (int)heightmap.GetBiome(vector, 0.02f, false);
					float vegetationMask = heightmap.GetVegetationMask(vector);
					flag2 = ((biome == 32) ? (vegetationMask > 0.1f) : (vegetationMask < 0.25f));
				}
				if (flag2)
				{
					this.m_placementStatus = Player.PlacementStatus.NeedDirt;
				}
			}
			if (component.m_notOnWood && piece && wearNTear && (wearNTear.m_materialType == WearNTear.MaterialType.Wood || wearNTear.m_materialType == WearNTear.MaterialType.HardWood))
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_notOnTiltingSurface && up.y < 0.8f)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_inCeilingOnly && up.y > -0.5f)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_notOnFloor && up.y > 0.1f)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_onlyInTeleportArea && !EffectArea.IsPointInsideArea(vector, EffectArea.Type.Teleport, 0f))
			{
				this.m_placementStatus = Player.PlacementStatus.NoTeleportArea;
			}
			if (!component.m_allowedInDungeons && base.InInterior() && !EnvMan.instance.CheckInteriorBuildingOverride() && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.DungeonBuild))
			{
				this.m_placementStatus = Player.PlacementStatus.NotInDungeon;
			}
			if (heightmap)
			{
				up = Vector3.up;
			}
			this.m_placementGhost.SetActive(true);
			int manualSnapPoint = this.m_manualSnapPoint;
			if (!ZInput.GetButton("JoyAltKeys") && !Hud.IsPieceSelectionVisible() && Minimap.instance.m_mode != Minimap.MapMode.Large && !global::Console.IsVisible() && !Chat.instance.HasFocus())
			{
				if (ZInput.GetButtonDown("TabLeft") || (ZInput.GetButtonUp("JoyPrevSnap") && ZInput.GetButtonLastPressedTimer("JoyPrevSnap") < 0.33f))
				{
					this.m_manualSnapPoint--;
				}
				if (ZInput.GetButtonDown("TabRight") || (ZInput.GetButtonUp("JoyNextSnap") && ZInput.GetButtonLastPressedTimer("JoyNextSnap") < 0.33f))
				{
					this.m_manualSnapPoint++;
				}
			}
			this.m_tempSnapPoints1.Clear();
			this.m_placementGhost.GetComponent<Piece>().GetSnapPoints(this.m_tempSnapPoints1);
			if (this.m_manualSnapPoint < -1)
			{
				this.m_manualSnapPoint = this.m_tempSnapPoints1.Count - 1;
			}
			if (this.m_manualSnapPoint >= this.m_tempSnapPoints1.Count)
			{
				this.m_manualSnapPoint = -1;
			}
			if (((component.m_groundPiece || component.m_clipGround) && heightmap) || component.m_clipEverything)
			{
				GameObject selectedPrefab = this.m_buildPieces.GetSelectedPrefab();
				TerrainModifier component3 = selectedPrefab.GetComponent<TerrainModifier>();
				TerrainOp component4 = selectedPrefab.GetComponent<TerrainOp>();
				if ((component3 || component4) && component.m_allowAltGroundPlacement && ((ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive()) ? (component.m_groundPiece && !this.m_altPlace) : (component.m_groundPiece && !ZInput.GetButton("AltPlace") && !ZInput.GetButton("JoyAltPlace"))))
				{
					float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
					vector.y = groundHeight;
				}
				this.m_placementGhost.transform.position = vector + ((this.m_manualSnapPoint < 0) ? Vector3.zero : (rotation * -this.m_tempSnapPoints1[this.m_manualSnapPoint].localPosition));
				this.m_placementGhost.transform.rotation = rotation;
			}
			else
			{
				Collider[] componentsInChildren = this.m_placementGhost.GetComponentsInChildren<Collider>();
				if (componentsInChildren.Length != 0)
				{
					this.m_placementGhost.transform.position = vector + up * 50f;
					this.m_placementGhost.transform.rotation = rotation;
					Vector3 b = Vector3.zero;
					float num = 999999f;
					foreach (Collider collider in componentsInChildren)
					{
						if (!collider.isTrigger && collider.enabled)
						{
							MeshCollider meshCollider = collider as MeshCollider;
							if (!(meshCollider != null) || meshCollider.convex)
							{
								Vector3 vector2 = collider.ClosestPoint(vector);
								float num2 = Vector3.Distance(vector2, vector);
								if (num2 < num)
								{
									b = vector2;
									num = num2;
								}
							}
						}
					}
					Vector3 vector3 = this.m_placementGhost.transform.position - b;
					if (component.m_waterPiece)
					{
						vector3.y = 3f;
					}
					this.m_placementGhost.transform.position = vector + ((this.m_manualSnapPoint < 0) ? vector3 : (rotation * -this.m_tempSnapPoints1[this.m_manualSnapPoint].localPosition));
					this.m_placementGhost.transform.rotation = rotation;
				}
			}
			if (manualSnapPoint != this.m_manualSnapPoint)
			{
				this.Message(MessageHud.MessageType.Center, "$msg_snapping " + ((this.m_manualSnapPoint == -1) ? "$msg_snapping_auto" : this.m_tempSnapPoints1[this.m_manualSnapPoint].name), 0, null);
			}
			if (!flag)
			{
				this.m_tempPieces.Clear();
				Transform transform;
				Transform transform2;
				if (this.FindClosestSnapPoints(this.m_placementGhost.transform, 0.5f, out transform, out transform2, this.m_tempPieces))
				{
					Vector3 position = transform2.parent.position;
					Vector3 vector4 = transform2.position - (transform.position - this.m_placementGhost.transform.position);
					if (!this.IsOverlappingOtherPiece(vector4, this.m_placementGhost.transform.rotation, this.m_placementGhost.name, this.m_tempPieces, component.m_allowRotatedOverlap))
					{
						this.m_placementGhost.transform.position = vector4;
					}
				}
			}
			if (Location.IsInsideNoBuildLocation(this.m_placementGhost.transform.position))
			{
				this.m_placementStatus = Player.PlacementStatus.NoBuildZone;
			}
			PrivateArea component5 = component.GetComponent<PrivateArea>();
			float radius = component5 ? component5.m_radius : 0f;
			bool wardCheck = component5 != null;
			if (!PrivateArea.CheckAccess(this.m_placementGhost.transform.position, radius, flashGuardStone, wardCheck))
			{
				this.m_placementStatus = Player.PlacementStatus.PrivateZone;
			}
			if (this.CheckPlacementGhostVSPlayers())
			{
				this.m_placementStatus = Player.PlacementStatus.BlockedbyPlayer;
			}
			if (component.m_onlyInBiome != Heightmap.Biome.None && (Heightmap.FindBiome(this.m_placementGhost.transform.position) & component.m_onlyInBiome) == Heightmap.Biome.None)
			{
				this.m_placementStatus = Player.PlacementStatus.WrongBiome;
			}
			if (component.m_noClipping && this.TestGhostClipping(this.m_placementGhost, 0.2f))
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
		}
		else
		{
			if (this.m_placementMarkerInstance)
			{
				this.m_placementMarkerInstance.SetActive(false);
			}
			this.m_placementGhost.SetActive(false);
			this.m_placementStatus = Player.PlacementStatus.NoRayHits;
		}
		this.SetPlacementGhostValid(this.m_placementStatus == Player.PlacementStatus.Valid);
	}

	// Token: 0x06000372 RID: 882 RVA: 0x00021124 File Offset: 0x0001F324
	private bool IsOverlappingOtherPiece(Vector3 p, Quaternion rotation, string pieceName, List<Piece> pieces, bool allowRotatedOverlap)
	{
		foreach (Piece piece in this.m_tempPieces)
		{
			if (Vector3.Distance(p, piece.transform.position) < 0.05f && (!allowRotatedOverlap || Quaternion.Angle(piece.transform.rotation, rotation) <= 10f) && piece.gameObject.name.CustomStartsWith(pieceName))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06000373 RID: 883 RVA: 0x000211C0 File Offset: 0x0001F3C0
	private bool FindClosestSnapPoints(Transform ghost, float maxSnapDistance, out Transform a, out Transform b, List<Piece> pieces)
	{
		this.m_tempSnapPoints1.Clear();
		ghost.GetComponent<Piece>().GetSnapPoints(this.m_tempSnapPoints1);
		this.m_tempSnapPoints2.Clear();
		this.m_tempPieces.Clear();
		Piece.GetSnapPoints(ghost.transform.position, 10f, this.m_tempSnapPoints2, this.m_tempPieces);
		float num = 9999999f;
		a = null;
		b = null;
		if (this.m_manualSnapPoint < 0)
		{
			foreach (Transform transform in this.m_tempSnapPoints1)
			{
				Transform transform2;
				float num2;
				if (this.FindClosestSnappoint(transform.position, this.m_tempSnapPoints2, maxSnapDistance, out transform2, out num2) && num2 < num)
				{
					num = num2;
					a = transform;
					b = transform2;
				}
			}
			return a != null;
		}
		Transform transform3;
		float num3;
		if (this.FindClosestSnappoint(this.m_tempSnapPoints1[this.m_manualSnapPoint].position, this.m_tempSnapPoints2, maxSnapDistance, out transform3, out num3))
		{
			a = this.m_tempSnapPoints1[this.m_manualSnapPoint];
			b = transform3;
			return true;
		}
		return false;
	}

	// Token: 0x06000374 RID: 884 RVA: 0x000212F0 File Offset: 0x0001F4F0
	private bool FindClosestSnappoint(Vector3 p, List<Transform> snapPoints, float maxDistance, out Transform closest, out float distance)
	{
		closest = null;
		distance = 999999f;
		foreach (Transform transform in snapPoints)
		{
			float num = Vector3.Distance(transform.position, p);
			if (num <= maxDistance && num < distance)
			{
				closest = transform;
				distance = num;
			}
		}
		return closest != null;
	}

	// Token: 0x06000375 RID: 885 RVA: 0x0002136C File Offset: 0x0001F56C
	private bool TestGhostClipping(GameObject ghost, float maxPenetration)
	{
		Collider[] componentsInChildren = ghost.GetComponentsInChildren<Collider>();
		Collider[] array = Physics.OverlapSphere(ghost.transform.position, 10f, this.m_placeRayMask);
		foreach (Collider collider in componentsInChildren)
		{
			foreach (Collider collider2 in array)
			{
				Vector3 vector;
				float num;
				if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, collider2, collider2.transform.position, collider2.transform.rotation, out vector, out num) && num > maxPenetration)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x06000376 RID: 886 RVA: 0x00021410 File Offset: 0x0001F610
	private bool CheckPlacementGhostVSPlayers()
	{
		if (this.m_placementGhost == null)
		{
			return false;
		}
		List<Character> list = new List<Character>();
		Character.GetCharactersInRange(base.transform.position, 30f, list);
		foreach (Collider collider in this.m_placementGhost.GetComponentsInChildren<Collider>())
		{
			if (!collider.isTrigger && collider.enabled)
			{
				MeshCollider meshCollider = collider as MeshCollider;
				if (!(meshCollider != null) || meshCollider.convex)
				{
					foreach (Character character in list)
					{
						CapsuleCollider collider2 = character.GetCollider();
						Vector3 vector;
						float num;
						if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, collider2, collider2.transform.position, collider2.transform.rotation, out vector, out num))
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	// Token: 0x06000377 RID: 887 RVA: 0x00021524 File Offset: 0x0001F724
	private bool PieceRayTest(out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
	{
		int layerMask = this.m_placeRayMask;
		if (water)
		{
			layerMask = this.m_placeWaterRayMask;
		}
		RaycastHit raycastHit;
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, layerMask))
		{
			float num = this.m_maxPlaceDistance;
			if (this.m_placementGhost)
			{
				Piece component = this.m_placementGhost.GetComponent<Piece>();
				if (component != null)
				{
					num += (float)component.m_extraPlacementDistance;
				}
			}
			if (raycastHit.collider && !raycastHit.collider.attachedRigidbody && Vector3.Distance(this.m_eye.position, raycastHit.point) < num)
			{
				point = raycastHit.point;
				normal = raycastHit.normal;
				piece = raycastHit.collider.GetComponentInParent<Piece>();
				heightmap = raycastHit.collider.GetComponent<Heightmap>();
				if (raycastHit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
				{
					waterSurface = raycastHit.collider;
				}
				else
				{
					waterSurface = null;
				}
				return true;
			}
		}
		point = Vector3.zero;
		normal = Vector3.zero;
		piece = null;
		heightmap = null;
		waterSurface = null;
		return false;
	}

	// Token: 0x06000378 RID: 888 RVA: 0x0002166C File Offset: 0x0001F86C
	private void FindHoverObject(out GameObject hover, out Character hoverCreature)
	{
		hover = null;
		hoverCreature = null;
		int num = Physics.RaycastNonAlloc(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, this.m_raycastHoverHits, 50f, this.m_interactMask);
		Array.Sort<RaycastHit>(this.m_raycastHoverHits, 0, num, Player.RaycastHitComparer.Instance);
		int i = 0;
		while (i < num)
		{
			RaycastHit raycastHit = this.m_raycastHoverHits[i];
			if (!raycastHit.collider.attachedRigidbody || !(raycastHit.collider.attachedRigidbody.gameObject == base.gameObject))
			{
				if (hoverCreature == null)
				{
					Character character = raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>();
					if (character != null && (!character.GetBaseAI() || !character.GetBaseAI().IsSleeping()) && !ParticleMist.IsMistBlocked(base.GetCenterPoint(), character.GetCenterPoint()))
					{
						hoverCreature = character;
					}
				}
				if (Vector3.Distance(this.m_eye.position, raycastHit.point) >= this.m_maxInteractDistance)
				{
					break;
				}
				if (raycastHit.collider.GetComponent<Hoverable>() != null)
				{
					hover = raycastHit.collider.gameObject;
					return;
				}
				if (raycastHit.collider.attachedRigidbody)
				{
					hover = raycastHit.collider.attachedRigidbody.gameObject;
					return;
				}
				hover = raycastHit.collider.gameObject;
				return;
			}
			else
			{
				i++;
			}
		}
	}

	// Token: 0x06000379 RID: 889 RVA: 0x00021804 File Offset: 0x0001FA04
	private void Interact(GameObject go, bool hold, bool alt)
	{
		if (this.InAttack() || this.InDodge())
		{
			return;
		}
		if (hold && Time.time - this.m_lastHoverInteractTime < 0.2f)
		{
			return;
		}
		Interactable componentInParent = go.GetComponentInParent<Interactable>();
		if (componentInParent != null)
		{
			this.m_lastHoverInteractTime = Time.time;
			if (componentInParent.Interact(this, hold, alt))
			{
				base.DoInteractAnimation((componentInParent as MonoBehaviour).gameObject);
			}
		}
	}

	// Token: 0x0600037A RID: 890 RVA: 0x0002186C File Offset: 0x0001FA6C
	private void UpdateStations(float dt)
	{
		this.m_stationDiscoverTimer += dt;
		if (this.m_stationDiscoverTimer > 1f)
		{
			this.m_stationDiscoverTimer = 0f;
			CraftingStation.UpdateKnownStationsInRange(this);
		}
		if (!(this.m_currentStation != null))
		{
			if (this.m_inCraftingStation)
			{
				this.m_zanim.SetInt("crafting", 0);
				this.m_inCraftingStation = false;
				if (InventoryGui.IsVisible())
				{
					InventoryGui.instance.Hide();
				}
			}
			return;
		}
		if (!this.m_currentStation.InUseDistance(this))
		{
			InventoryGui.instance.Hide();
			this.SetCraftingStation(null);
			return;
		}
		if (!InventoryGui.IsVisible())
		{
			this.SetCraftingStation(null);
			return;
		}
		this.m_currentStation.PokeInUse();
		if (!this.AlwaysRotateCamera())
		{
			Vector3 normalized = (this.m_currentStation.transform.position - base.transform.position).normalized;
			normalized.y = 0f;
			normalized.Normalize();
			Quaternion to = Quaternion.LookRotation(normalized);
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, this.m_turnSpeed * dt);
		}
		this.m_zanim.SetInt("crafting", this.m_currentStation.m_useAnimation);
		this.m_inCraftingStation = true;
	}

	// Token: 0x0600037B RID: 891 RVA: 0x000219B3 File Offset: 0x0001FBB3
	public void SetCraftingStation(CraftingStation station)
	{
		if (this.m_currentStation == station)
		{
			return;
		}
		if (station)
		{
			this.AddKnownStation(station);
			station.PokeInUse();
			base.HideHandItems(false, true);
		}
		this.m_currentStation = station;
	}

	// Token: 0x0600037C RID: 892 RVA: 0x000219E9 File Offset: 0x0001FBE9
	public CraftingStation GetCurrentCraftingStation()
	{
		return this.m_currentStation;
	}

	// Token: 0x0600037D RID: 893 RVA: 0x000219F4 File Offset: 0x0001FBF4
	private void UpdateCover(float dt)
	{
		this.m_updateCoverTimer += dt;
		if (this.m_updateCoverTimer > 1f)
		{
			this.m_updateCoverTimer = 0f;
			Cover.GetCoverForPoint(base.GetCenterPoint(), out this.m_coverPercentage, out this.m_underRoof, 0.5f);
		}
	}

	// Token: 0x0600037E RID: 894 RVA: 0x00021A43 File Offset: 0x0001FC43
	public Character GetHoverCreature()
	{
		return this.m_hoveringCreature;
	}

	// Token: 0x0600037F RID: 895 RVA: 0x00021A4B File Offset: 0x0001FC4B
	public override GameObject GetHoverObject()
	{
		return this.m_hovering;
	}

	// Token: 0x06000380 RID: 896 RVA: 0x00021A53 File Offset: 0x0001FC53
	public override void OnNearFire(Vector3 point)
	{
		this.m_nearFireTimer = 0f;
	}

	// Token: 0x06000381 RID: 897 RVA: 0x00021A60 File Offset: 0x0001FC60
	public bool InShelter()
	{
		return this.m_coverPercentage >= 0.8f && this.m_underRoof;
	}

	// Token: 0x06000382 RID: 898 RVA: 0x00021A77 File Offset: 0x0001FC77
	public float GetStamina()
	{
		return this.m_stamina;
	}

	// Token: 0x06000383 RID: 899 RVA: 0x00021A7F File Offset: 0x0001FC7F
	public override float GetMaxStamina()
	{
		return this.m_maxStamina;
	}

	// Token: 0x06000384 RID: 900 RVA: 0x00021A87 File Offset: 0x0001FC87
	public float GetEitr()
	{
		return this.m_eitr;
	}

	// Token: 0x06000385 RID: 901 RVA: 0x00021A8F File Offset: 0x0001FC8F
	public override float GetMaxEitr()
	{
		return this.m_maxEitr;
	}

	// Token: 0x06000386 RID: 902 RVA: 0x00021A97 File Offset: 0x0001FC97
	public override float GetEitrPercentage()
	{
		return this.m_eitr / this.m_maxEitr;
	}

	// Token: 0x06000387 RID: 903 RVA: 0x00021AA6 File Offset: 0x0001FCA6
	public override float GetStaminaPercentage()
	{
		return this.m_stamina / this.m_maxStamina;
	}

	// Token: 0x06000388 RID: 904 RVA: 0x00021AB5 File Offset: 0x0001FCB5
	public void SetGodMode(bool godMode)
	{
		this.m_godMode = godMode;
	}

	// Token: 0x06000389 RID: 905 RVA: 0x00021ABE File Offset: 0x0001FCBE
	public override bool InGodMode()
	{
		return this.m_godMode;
	}

	// Token: 0x0600038A RID: 906 RVA: 0x00021AC6 File Offset: 0x0001FCC6
	public void SetGhostMode(bool ghostmode)
	{
		this.m_ghostMode = ghostmode;
	}

	// Token: 0x0600038B RID: 907 RVA: 0x00021ACF File Offset: 0x0001FCCF
	public override bool InGhostMode()
	{
		return this.m_ghostMode;
	}

	// Token: 0x1700000C RID: 12
	// (get) Token: 0x0600038C RID: 908 RVA: 0x00021AD7 File Offset: 0x0001FCD7
	// (set) Token: 0x0600038D RID: 909 RVA: 0x00021ADF File Offset: 0x0001FCDF
	public bool AttackTowardsPlayerLookDir
	{
		get
		{
			return this.m_attackTowardsPlayerLookDir;
		}
		set
		{
			this.m_attackTowardsPlayerLookDir = value;
		}
	}

	// Token: 0x0600038E RID: 910 RVA: 0x00021AE8 File Offset: 0x0001FCE8
	public override bool IsDebugFlying()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return false;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_debugFly;
		}
		return this.m_nview.GetZDO().GetBool(ZDOVars.s_debugFly, false);
	}

	// Token: 0x0600038F RID: 911 RVA: 0x00021B3C File Offset: 0x0001FD3C
	public override void AddEitr(float v)
	{
		this.m_eitr += v;
		if (this.m_eitr > this.m_maxEitr)
		{
			this.m_eitr = this.m_maxEitr;
		}
	}

	// Token: 0x06000390 RID: 912 RVA: 0x00021B66 File Offset: 0x0001FD66
	public override void AddStamina(float v)
	{
		this.m_stamina += v;
		if (this.m_stamina > this.m_maxStamina)
		{
			this.m_stamina = this.m_maxStamina;
		}
	}

	// Token: 0x06000391 RID: 913 RVA: 0x00021B90 File Offset: 0x0001FD90
	public override void UseEitr(float v)
	{
		if (v == 0f)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.RPC_UseEitr(0L, v);
			return;
		}
		this.m_nview.InvokeRPC("UseEitr", new object[]
		{
			v
		});
	}

	// Token: 0x06000392 RID: 914 RVA: 0x00021BEA File Offset: 0x0001FDEA
	private void RPC_UseEitr(long sender, float v)
	{
		if (v == 0f)
		{
			return;
		}
		this.m_eitr -= v;
		if (this.m_eitr < 0f)
		{
			this.m_eitr = 0f;
		}
		this.m_eitrRegenTimer = this.m_eitrRegenDelay;
	}

	// Token: 0x06000393 RID: 915 RVA: 0x00021C28 File Offset: 0x0001FE28
	public override bool HaveEitr(float amount = 0f)
	{
		if (this.m_nview.IsValid() && !this.m_nview.IsOwner())
		{
			return this.m_nview.GetZDO().GetFloat(ZDOVars.s_eitr, this.m_maxEitr) > amount;
		}
		return this.m_eitr > amount;
	}

	// Token: 0x06000394 RID: 916 RVA: 0x00021C78 File Offset: 0x0001FE78
	public override void UseStamina(float v)
	{
		if (v == 0f || float.IsNaN(v))
		{
			return;
		}
		v *= Game.m_staminaRate;
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.RPC_UseStamina(0L, v);
			return;
		}
		this.m_nview.InvokeRPC("UseStamina", new object[]
		{
			v
		});
	}

	// Token: 0x06000395 RID: 917 RVA: 0x00021CE3 File Offset: 0x0001FEE3
	private void RPC_UseStamina(long sender, float v)
	{
		if (v == 0f)
		{
			return;
		}
		this.m_stamina -= v;
		if (this.m_stamina < 0f)
		{
			this.m_stamina = 0f;
		}
		this.m_staminaRegenTimer = this.m_staminaRegenDelay;
	}

	// Token: 0x06000396 RID: 918 RVA: 0x00021D20 File Offset: 0x0001FF20
	public override bool HaveStamina(float amount = 0f)
	{
		if (this.m_nview.IsValid() && !this.m_nview.IsOwner())
		{
			return this.m_nview.GetZDO().GetFloat(ZDOVars.s_stamina, this.m_maxStamina) > amount;
		}
		return this.m_stamina > amount;
	}

	// Token: 0x06000397 RID: 919 RVA: 0x00021D70 File Offset: 0x0001FF70
	public void Save(ZPackage pkg)
	{
		pkg.Write(29);
		pkg.Write(base.GetMaxHealth());
		pkg.Write(base.GetHealth());
		pkg.Write(this.GetMaxStamina());
		pkg.Write(this.m_timeSinceDeath);
		pkg.Write(this.m_guardianPower);
		pkg.Write(this.m_guardianPowerCooldown);
		this.m_inventory.Save(pkg);
		pkg.Write(this.m_knownRecipes.Count);
		foreach (string data in this.m_knownRecipes)
		{
			pkg.Write(data);
		}
		pkg.Write(this.m_knownStations.Count);
		foreach (KeyValuePair<string, int> keyValuePair in this.m_knownStations)
		{
			pkg.Write(keyValuePair.Key);
			pkg.Write(keyValuePair.Value);
		}
		pkg.Write(this.m_knownMaterial.Count);
		foreach (string data2 in this.m_knownMaterial)
		{
			pkg.Write(data2);
		}
		pkg.Write(this.m_shownTutorials.Count);
		foreach (string data3 in this.m_shownTutorials)
		{
			pkg.Write(data3);
		}
		pkg.Write(this.m_uniques.Count);
		foreach (string data4 in this.m_uniques)
		{
			pkg.Write(data4);
		}
		pkg.Write(this.m_trophies.Count);
		foreach (string data5 in this.m_trophies)
		{
			pkg.Write(data5);
		}
		pkg.Write(this.m_knownBiome.Count);
		foreach (Heightmap.Biome data6 in this.m_knownBiome)
		{
			pkg.Write((int)data6);
		}
		pkg.Write(this.m_knownTexts.Count);
		foreach (KeyValuePair<string, string> keyValuePair2 in this.m_knownTexts)
		{
			pkg.Write(keyValuePair2.Key.Replace("\u0016", ""));
			pkg.Write(keyValuePair2.Value.Replace("\u0016", ""));
		}
		pkg.Write(this.m_beardItem);
		pkg.Write(this.m_hairItem);
		pkg.Write(this.m_skinColor);
		pkg.Write(this.m_hairColor);
		pkg.Write(this.m_modelIndex);
		pkg.Write(this.m_foods.Count);
		foreach (Player.Food food in this.m_foods)
		{
			pkg.Write(food.m_name);
			pkg.Write(food.m_time);
		}
		this.m_skills.Save(pkg);
		pkg.Write(this.m_customData.Count);
		foreach (KeyValuePair<string, string> keyValuePair3 in this.m_customData)
		{
			pkg.Write(keyValuePair3.Key);
			pkg.Write(keyValuePair3.Value);
		}
		pkg.Write(this.GetStamina());
		pkg.Write(this.GetMaxEitr());
		pkg.Write(this.GetEitr());
	}

	// Token: 0x06000398 RID: 920 RVA: 0x00022204 File Offset: 0x00020404
	public void Load(ZPackage pkg)
	{
		this.m_isLoading = true;
		base.UnequipAllItems();
		int num = pkg.ReadInt();
		if (num >= 7)
		{
			this.SetMaxHealth(pkg.ReadSingle(), false);
		}
		float num2 = pkg.ReadSingle();
		float maxHealth = base.GetMaxHealth();
		if (num2 <= 0f || num2 > maxHealth || float.IsNaN(num2))
		{
			num2 = maxHealth;
		}
		base.SetHealth(num2);
		if (num >= 10)
		{
			float stamina = pkg.ReadSingle();
			this.SetMaxStamina(stamina, false);
			this.m_stamina = stamina;
		}
		if (num >= 8 && num < 28)
		{
			pkg.ReadBool();
		}
		if (num >= 20)
		{
			this.m_timeSinceDeath = pkg.ReadSingle();
		}
		if (num >= 23)
		{
			string guardianPower = pkg.ReadString();
			this.SetGuardianPower(guardianPower);
		}
		if (num >= 24)
		{
			this.m_guardianPowerCooldown = pkg.ReadSingle();
		}
		if (num == 2)
		{
			pkg.ReadZDOID();
		}
		this.m_inventory.Load(pkg);
		int num3 = pkg.ReadInt();
		for (int i = 0; i < num3; i++)
		{
			string item = pkg.ReadString();
			this.m_knownRecipes.Add(item);
		}
		if (num < 15)
		{
			int num4 = pkg.ReadInt();
			for (int j = 0; j < num4; j++)
			{
				pkg.ReadString();
			}
		}
		else
		{
			int num5 = pkg.ReadInt();
			for (int k = 0; k < num5; k++)
			{
				string key = pkg.ReadString();
				int value = pkg.ReadInt();
				this.m_knownStations.Add(key, value);
			}
		}
		int num6 = pkg.ReadInt();
		for (int l = 0; l < num6; l++)
		{
			string item2 = pkg.ReadString();
			this.m_knownMaterial.Add(item2);
		}
		if (num < 19 || num >= 21)
		{
			int num7 = pkg.ReadInt();
			for (int m = 0; m < num7; m++)
			{
				string item3 = pkg.ReadString();
				this.m_shownTutorials.Add(item3);
			}
		}
		if (num >= 6)
		{
			int num8 = pkg.ReadInt();
			for (int n = 0; n < num8; n++)
			{
				string item4 = pkg.ReadString();
				this.m_uniques.Add(item4);
			}
		}
		if (num >= 9)
		{
			int num9 = pkg.ReadInt();
			for (int num10 = 0; num10 < num9; num10++)
			{
				string item5 = pkg.ReadString();
				this.m_trophies.Add(item5);
			}
		}
		if (num >= 18)
		{
			int num11 = pkg.ReadInt();
			for (int num12 = 0; num12 < num11; num12++)
			{
				Heightmap.Biome item6 = (Heightmap.Biome)pkg.ReadInt();
				this.m_knownBiome.Add(item6);
			}
		}
		if (num >= 22)
		{
			int num13 = pkg.ReadInt();
			for (int num14 = 0; num14 < num13; num14++)
			{
				string key2 = pkg.ReadString();
				string value2 = pkg.ReadString();
				this.m_knownTexts[key2] = value2;
			}
		}
		if (num >= 4)
		{
			string beard = pkg.ReadString();
			string hair = pkg.ReadString();
			base.SetBeard(beard);
			base.SetHair(hair);
		}
		if (num >= 5)
		{
			Vector3 skinColor = pkg.ReadVector3();
			Vector3 hairColor = pkg.ReadVector3();
			this.SetSkinColor(skinColor);
			this.SetHairColor(hairColor);
		}
		if (num >= 11)
		{
			int playerModel = pkg.ReadInt();
			this.SetPlayerModel(playerModel);
		}
		if (num >= 12)
		{
			this.m_foods.Clear();
			int num15 = pkg.ReadInt();
			for (int num16 = 0; num16 < num15; num16++)
			{
				if (num >= 14)
				{
					Player.Food food = new Player.Food();
					food.m_name = pkg.ReadString();
					if (num >= 25)
					{
						food.m_time = pkg.ReadSingle();
					}
					else
					{
						food.m_health = pkg.ReadSingle();
						if (num >= 16)
						{
							food.m_stamina = pkg.ReadSingle();
						}
					}
					GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(food.m_name);
					if (itemPrefab == null)
					{
						ZLog.LogWarning("Failed to find food item " + food.m_name);
					}
					else
					{
						food.m_item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
						this.m_foods.Add(food);
					}
				}
				else
				{
					pkg.ReadString();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					if (num >= 13)
					{
						pkg.ReadSingle();
					}
				}
			}
		}
		if (num >= 17)
		{
			this.m_skills.Load(pkg);
		}
		if (num >= 26)
		{
			int num17 = pkg.ReadInt();
			for (int num18 = 0; num18 < num17; num18++)
			{
				string key3 = pkg.ReadString();
				string value3 = pkg.ReadString();
				this.m_customData[key3] = value3;
			}
			this.m_stamina = Mathf.Clamp(pkg.ReadSingle(), 0f, this.m_maxStamina);
			this.SetMaxEitr(pkg.ReadSingle(), false);
			this.m_eitr = Mathf.Clamp(pkg.ReadSingle(), 0f, this.m_maxEitr);
		}
		if (num < 27)
		{
			if (this.m_knownMaterial.Contains("$item_flametal"))
			{
				ZLog.DevLog("Pre ashlands character loaded, replacing flametal with ancient as known material.");
				this.m_knownMaterial.Remove("$item_flametal");
				this.m_knownMaterial.Add("$item_flametal_old");
			}
			if (this.m_knownMaterial.Contains("$item_flametalore"))
			{
				ZLog.DevLog("Pre ashlands character loaded, replacing flametal ore with ancient as known material.");
				this.m_knownMaterial.Remove("$item_flametalore");
				this.m_knownMaterial.Add("$item_flametalore_old");
			}
		}
		this.m_isLoading = false;
		this.UpdateAvailablePiecesList();
		this.EquipInventoryItems();
		this.UpdateEvents();
	}

	// Token: 0x06000399 RID: 921 RVA: 0x00022748 File Offset: 0x00020948
	private void EquipInventoryItems()
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory.GetEquippedItems())
		{
			if (!base.EquipItem(itemData, false))
			{
				itemData.m_equipped = false;
			}
		}
	}

	// Token: 0x0600039A RID: 922 RVA: 0x000227AC File Offset: 0x000209AC
	public override bool CanMove()
	{
		return !this.m_teleporting && !this.InCutscene() && (!this.IsEncumbered() || this.HaveStamina(0f)) && base.CanMove();
	}

	// Token: 0x0600039B RID: 923 RVA: 0x000227DF File Offset: 0x000209DF
	public override bool IsEncumbered()
	{
		return this.m_inventory.GetTotalWeight() > this.GetMaxCarryWeight();
	}

	// Token: 0x0600039C RID: 924 RVA: 0x000227F4 File Offset: 0x000209F4
	public float GetMaxCarryWeight()
	{
		float maxCarryWeight = this.m_maxCarryWeight;
		this.m_seman.ModifyMaxCarryWeight(maxCarryWeight, ref maxCarryWeight);
		return maxCarryWeight;
	}

	// Token: 0x0600039D RID: 925 RVA: 0x00022817 File Offset: 0x00020A17
	public override bool HaveUniqueKey(string name)
	{
		return this.m_uniques.Contains(name);
	}

	// Token: 0x0600039E RID: 926 RVA: 0x00022828 File Offset: 0x00020A28
	public bool HaveUniqueKeyValue(string key, string value)
	{
		key = key.ToLower();
		value = value.ToLower();
		foreach (string text in this.m_uniques)
		{
			string[] array = text.Split(' ', StringSplitOptions.None);
			if (array.Length >= 2 && array[0].ToLower() == key && array[1].ToLower() == value)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x0600039F RID: 927 RVA: 0x000228BC File Offset: 0x00020ABC
	public bool TryGetUniqueKeyValue(string key, out string value)
	{
		key = key.ToLower();
		foreach (string text in this.m_uniques)
		{
			string[] array = text.Split(' ', StringSplitOptions.None);
			if (array.Length >= 2 && array[0].ToLower() == key)
			{
				value = array[1];
				return true;
			}
		}
		value = null;
		return false;
	}

	// Token: 0x060003A0 RID: 928 RVA: 0x00022940 File Offset: 0x00020B40
	public bool RemoveUniqueKeyValue(string key)
	{
		key = key.ToLower();
		int count = this.m_uniques.Count;
		this.m_uniques.RemoveWhere(delegate(string x)
		{
			string[] array = x.Split(' ', StringSplitOptions.None);
			return array.Length >= 2 && array[0].ToLower() == key;
		});
		if (this.m_uniques.Count != count)
		{
			ZoneSystem instance = ZoneSystem.instance;
			if (instance != null)
			{
				instance.UpdateWorldRates();
			}
			this.UpdateEvents();
			return true;
		}
		return false;
	}

	// Token: 0x060003A1 RID: 929 RVA: 0x000229B6 File Offset: 0x00020BB6
	public void AddUniqueKeyValue(string key, string value)
	{
		this.AddUniqueKey(key + " " + value);
	}

	// Token: 0x060003A2 RID: 930 RVA: 0x000229CA File Offset: 0x00020BCA
	public override void AddUniqueKey(string name)
	{
		if (!this.m_uniques.Contains(name))
		{
			this.m_uniques.Add(name);
		}
		ZoneSystem instance = ZoneSystem.instance;
		if (instance != null)
		{
			instance.UpdateWorldRates();
		}
		this.UpdateEvents();
	}

	// Token: 0x060003A3 RID: 931 RVA: 0x000229FD File Offset: 0x00020BFD
	public override bool RemoveUniqueKey(string name)
	{
		if (this.m_uniques.Contains(name))
		{
			this.m_uniques.Remove(name);
			ZoneSystem.instance.UpdateWorldRates();
			this.UpdateEvents();
			return true;
		}
		return false;
	}

	// Token: 0x060003A4 RID: 932 RVA: 0x00022A2D File Offset: 0x00020C2D
	public List<string> GetUniqueKeys()
	{
		this.m_tempUniqueKeys.Clear();
		this.m_tempUniqueKeys.AddRange(this.m_uniques);
		return this.m_tempUniqueKeys;
	}

	// Token: 0x060003A5 RID: 933 RVA: 0x00022A51 File Offset: 0x00020C51
	public void ResetUniqueKeys()
	{
		this.m_uniques.Clear();
	}

	// Token: 0x060003A6 RID: 934 RVA: 0x00022A5E File Offset: 0x00020C5E
	public bool IsBiomeKnown(Heightmap.Biome biome)
	{
		return this.m_knownBiome.Contains(biome);
	}

	// Token: 0x060003A7 RID: 935 RVA: 0x00022A6C File Offset: 0x00020C6C
	private void AddKnownBiome(Heightmap.Biome biome)
	{
		if (!this.m_knownBiome.Contains(biome))
		{
			this.m_knownBiome.Add(biome);
			if (biome != Heightmap.Biome.Meadows && biome != Heightmap.Biome.None)
			{
				string text = "$biome_" + biome.ToString().ToLower();
				MessageHud.instance.ShowBiomeFoundMsg(text, true);
			}
			if (biome == Heightmap.Biome.BlackForest && !ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"))
			{
				this.ShowTutorial("blackforest", false);
			}
			Gogan.LogEvent("Game", "BiomeFound", biome.ToString(), 0L);
		}
		if (biome == Heightmap.Biome.BlackForest)
		{
			this.ShowTutorial("haldor", false);
		}
		if (biome == Heightmap.Biome.AshLands)
		{
			this.ShowTutorial("ashlands", false);
		}
	}

	// Token: 0x060003A8 RID: 936 RVA: 0x00022B24 File Offset: 0x00020D24
	public void AddKnownLocationName(string label)
	{
		if (!this.m_shownTutorials.Contains(label))
		{
			this.m_shownTutorials.Add(label);
			MessageHud.instance.ShowBiomeFoundMsg(label, true);
		}
	}

	// Token: 0x060003A9 RID: 937 RVA: 0x00022B4D File Offset: 0x00020D4D
	public bool IsRecipeKnown(string name)
	{
		return this.m_knownRecipes.Contains(name);
	}

	// Token: 0x060003AA RID: 938 RVA: 0x00022B5C File Offset: 0x00020D5C
	private void AddKnownRecipe(Recipe recipe)
	{
		if (!this.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name))
		{
			this.m_knownRecipes.Add(recipe.m_item.m_itemData.m_shared.m_name);
			MessageHud.instance.QueueUnlockMsg(recipe.m_item.m_itemData.GetIcon(), "$msg_newrecipe", recipe.m_item.m_itemData.m_shared.m_name);
			Gogan.LogEvent("Game", "RecipeFound", recipe.m_item.m_itemData.m_shared.m_name, 0L);
		}
	}

	// Token: 0x060003AB RID: 939 RVA: 0x00022C08 File Offset: 0x00020E08
	private void AddKnownPiece(Piece piece)
	{
		if (this.m_knownRecipes.Contains(piece.m_name))
		{
			return;
		}
		this.m_knownRecipes.Add(piece.m_name);
		string topic = (piece.m_category == Piece.PieceCategory.Feasts || piece.m_category == Piece.PieceCategory.Food || piece.m_category == Piece.PieceCategory.Meads) ? "$msg_newdish" : "$msg_newpiece";
		MessageHud.instance.QueueUnlockMsg(piece.m_icon, topic, piece.m_name);
		Gogan.LogEvent("Game", "PieceFound", piece.m_name, 0L);
	}

	// Token: 0x060003AC RID: 940 RVA: 0x00022C94 File Offset: 0x00020E94
	public void AddKnownStation(CraftingStation station)
	{
		int level = station.GetLevel(true);
		int num;
		if (this.m_knownStations.TryGetValue(station.m_name, out num))
		{
			if (num < level)
			{
				this.m_knownStations[station.m_name] = level;
				MessageHud.instance.QueueUnlockMsg(station.m_icon, "$msg_newstation_level", station.m_name + " $msg_level " + level.ToString());
				this.UpdateKnownRecipesList();
			}
			return;
		}
		this.m_knownStations.Add(station.m_name, level);
		MessageHud.instance.QueueUnlockMsg(station.m_icon, "$msg_newstation", station.m_name);
		Gogan.LogEvent("Game", "StationFound", station.m_name, 0L);
		this.UpdateKnownRecipesList();
	}

	// Token: 0x060003AD RID: 941 RVA: 0x00022D54 File Offset: 0x00020F54
	private bool KnowStationLevel(string name, int level)
	{
		int num;
		return this.m_knownStations.TryGetValue(name, out num) && num >= level;
	}

	// Token: 0x060003AE RID: 942 RVA: 0x00022D7C File Offset: 0x00020F7C
	public void AddKnownText(string label, string text)
	{
		if (label.Length == 0)
		{
			ZLog.LogWarning("Text " + text + " Is missing label");
			return;
		}
		if (!this.m_knownTexts.ContainsKey(label.Replace("\u0016", "")))
		{
			this.m_knownTexts.Add(label, text);
			this.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_newtext", new string[]
			{
				label
			}), 0, this.m_textIcon);
		}
	}

	// Token: 0x060003AF RID: 943 RVA: 0x00022DF8 File Offset: 0x00020FF8
	public List<KeyValuePair<string, string>> GetKnownTexts()
	{
		return this.m_knownTexts.ToList<KeyValuePair<string, string>>();
	}

	// Token: 0x060003B0 RID: 944 RVA: 0x00022E08 File Offset: 0x00021008
	public void AddKnownItem(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
		{
			this.AddTrophy(item);
		}
		if (!this.m_knownMaterial.Contains(item.m_shared.m_name))
		{
			this.m_knownMaterial.Add(item.m_shared.m_name);
			if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material)
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newmaterial", item.m_shared.m_name);
			}
			else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newtrophy", item.m_shared.m_name);
			}
			else
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newitem", item.m_shared.m_name);
			}
			Gogan.LogEvent("Game", "ItemFound", item.m_shared.m_name, 0L);
			this.UpdateKnownRecipesList();
			this.UpdateEvents();
		}
	}

	// Token: 0x060003B1 RID: 945 RVA: 0x00022F08 File Offset: 0x00021108
	private void AddTrophy(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
		{
			return;
		}
		if (!this.m_trophies.Contains(item.m_dropPrefab.name))
		{
			this.m_trophies.Add(item.m_dropPrefab.name);
		}
	}

	// Token: 0x060003B2 RID: 946 RVA: 0x00022F54 File Offset: 0x00021154
	public List<string> GetTrophies()
	{
		List<string> list = new List<string>();
		list.AddRange(this.m_trophies);
		return list;
	}

	// Token: 0x060003B3 RID: 947 RVA: 0x00022F68 File Offset: 0x00021168
	private void UpdateKnownRecipesList()
	{
		if (Game.instance == null)
		{
			return;
		}
		foreach (Recipe recipe in ObjectDB.instance.m_recipes)
		{
			bool flag = this.m_currentSeason != null && this.m_currentSeason.Recipes.Contains(recipe);
			if ((recipe.m_enabled || flag) && recipe.m_item && !this.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) && this.HaveRequirements(recipe, true, 0, 1))
			{
				this.AddKnownRecipe(recipe);
			}
		}
		this.m_tempOwnedPieceTables.Clear();
		this.m_inventory.GetAllPieceTables(this.m_tempOwnedPieceTables);
		bool flag2 = false;
		foreach (PieceTable pieceTable in this.m_tempOwnedPieceTables)
		{
			foreach (GameObject gameObject in pieceTable.m_pieces)
			{
				Piece component = gameObject.GetComponent<Piece>();
				bool flag3 = this.m_currentSeason != null && this.m_currentSeason.Pieces.Contains(gameObject);
				if ((component.m_enabled || flag3) && !this.m_knownRecipes.Contains(component.m_name) && this.HaveRequirements(component, Player.RequirementMode.IsKnown))
				{
					this.AddKnownPiece(component);
					flag2 = true;
				}
			}
		}
		if (flag2)
		{
			this.UpdateAvailablePiecesList();
		}
	}

	// Token: 0x060003B4 RID: 948 RVA: 0x00023144 File Offset: 0x00021344
	private void UpdateAvailablePiecesList()
	{
		if (this.m_buildPieces != null)
		{
			this.m_buildPieces.UpdateAvailable(this.m_knownRecipes, this, false, this.m_noPlacementCost || ZoneSystem.instance.GetGlobalKey(GlobalKeys.AllPiecesUnlocked));
		}
		this.SetupPlacementGhost();
	}

	// Token: 0x060003B5 RID: 949 RVA: 0x00023184 File Offset: 0x00021384
	private void UpdateCurrentSeason()
	{
		this.m_currentSeason = null;
		foreach (SeasonalItemGroup seasonalItemGroup in this.m_seasonalItemGroups)
		{
			if (seasonalItemGroup.IsInSeason())
			{
				this.m_currentSeason = seasonalItemGroup;
				break;
			}
		}
	}

	// Token: 0x060003B6 RID: 950 RVA: 0x000231E8 File Offset: 0x000213E8
	public override void Message(MessageHud.MessageType type, string msg, int amount = 0, Sprite icon = null)
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			if (MessageHud.instance)
			{
				MessageHud.instance.ShowMessage(type, msg, amount, icon, false);
				return;
			}
		}
		else
		{
			this.m_nview.InvokeRPC("Message", new object[]
			{
				(int)type,
				msg,
				amount
			});
		}
	}

	// Token: 0x060003B7 RID: 951 RVA: 0x00023267 File Offset: 0x00021467
	private void RPC_Message(long sender, int type, string msg, int amount)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (MessageHud.instance)
		{
			MessageHud.instance.ShowMessage((MessageHud.MessageType)type, msg, amount, null, false);
		}
	}

	// Token: 0x060003B8 RID: 952 RVA: 0x00023294 File Offset: 0x00021494
	public static Player GetPlayer(long playerID)
	{
		foreach (Player player in Player.s_players)
		{
			if (player.GetPlayerID() == playerID)
			{
				return player;
			}
		}
		return null;
	}

	// Token: 0x060003B9 RID: 953 RVA: 0x000232F0 File Offset: 0x000214F0
	public static Player GetClosestPlayer(Vector3 point, float maxRange)
	{
		Player result = null;
		float num = 999999f;
		foreach (Player player in Player.s_players)
		{
			float num2 = Vector3.Distance(player.transform.position, point);
			if (num2 < num && num2 < maxRange)
			{
				num = num2;
				result = player;
			}
		}
		return result;
	}

	// Token: 0x060003BA RID: 954 RVA: 0x00023368 File Offset: 0x00021568
	public static bool IsPlayerInRange(Vector3 point, float range, long playerID)
	{
		foreach (Player player in Player.s_players)
		{
			if (player.GetPlayerID() == playerID)
			{
				return global::Utils.DistanceXZ(player.transform.position, point) < range;
			}
		}
		return false;
	}

	// Token: 0x060003BB RID: 955 RVA: 0x000233D8 File Offset: 0x000215D8
	public static void MessageAllInRange(Vector3 point, float range, MessageHud.MessageType type, string msg, Sprite icon = null)
	{
		foreach (Player player in Player.s_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				player.Message(type, msg, 0, icon);
			}
		}
	}

	// Token: 0x060003BC RID: 956 RVA: 0x00023444 File Offset: 0x00021644
	public static int GetPlayersInRangeXZ(Vector3 point, float range)
	{
		int num = 0;
		using (List<Player>.Enumerator enumerator = Player.s_players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (global::Utils.DistanceXZ(enumerator.Current.transform.position, point) < range)
				{
					num++;
				}
			}
		}
		return num;
	}

	// Token: 0x060003BD RID: 957 RVA: 0x000234A8 File Offset: 0x000216A8
	public static void GetPlayersInRange(Vector3 point, float range, List<Player> players)
	{
		foreach (Player player in Player.s_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				players.Add(player);
			}
		}
	}

	// Token: 0x060003BE RID: 958 RVA: 0x00023510 File Offset: 0x00021710
	public static bool IsPlayerInRange(Vector3 point, float range)
	{
		using (List<Player>.Enumerator enumerator = Player.s_players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Vector3.Distance(enumerator.Current.transform.position, point) < range)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x060003BF RID: 959 RVA: 0x00023574 File Offset: 0x00021774
	public static bool IsPlayerInRange(Vector3 point, float range, float minNoise)
	{
		foreach (Player player in Player.s_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				float noiseRange = player.GetNoiseRange();
				if (range <= noiseRange && noiseRange >= minNoise)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x060003C0 RID: 960 RVA: 0x000235EC File Offset: 0x000217EC
	public static Player GetPlayerNoiseRange(Vector3 point, float maxNoiseRange = 100f)
	{
		foreach (Player player in Player.s_players)
		{
			float num = Vector3.Distance(player.transform.position, point);
			float num2 = Mathf.Min(player.GetNoiseRange(), maxNoiseRange);
			if (num < num2)
			{
				return player;
			}
		}
		return null;
	}

	// Token: 0x060003C1 RID: 961 RVA: 0x00023664 File Offset: 0x00021864
	public static List<Player> GetAllPlayers()
	{
		return Player.s_players;
	}

	// Token: 0x060003C2 RID: 962 RVA: 0x0002366B File Offset: 0x0002186B
	public static Player GetRandomPlayer()
	{
		if (Player.s_players.Count == 0)
		{
			return null;
		}
		return Player.s_players[UnityEngine.Random.Range(0, Player.s_players.Count)];
	}

	// Token: 0x060003C3 RID: 963 RVA: 0x00023698 File Offset: 0x00021898
	public void GetAvailableRecipes(ref List<Recipe> available)
	{
		available.Clear();
		foreach (Recipe recipe in ObjectDB.instance.m_recipes)
		{
			bool flag = this.m_currentSeason != null && this.m_currentSeason.Recipes.Contains(recipe);
			if ((recipe.m_enabled || flag) && recipe.m_item)
			{
				if (Player.s_FilterCraft.Count > 0)
				{
					bool flag2 = false;
					int num = 0;
					while (num < Player.s_FilterCraft.Count && (Player.s_FilterCraft[num].Length <= 0 || (!recipe.m_item.name.ToLower().Contains(Player.s_FilterCraft[num].ToLower()) && !recipe.m_item.m_itemData.m_shared.m_name.ToLower().Contains(Player.s_FilterCraft[num].ToLower()) && !Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name).ToLower().Contains(Player.s_FilterCraft[num].ToLower()))))
					{
						if (num + 1 == Player.s_FilterCraft.Count)
						{
							flag2 = true;
						}
						num++;
					}
					if (flag2)
					{
						continue;
					}
				}
				if ((recipe.m_item.m_itemData.m_shared.m_dlc.Length <= 0 || DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc)) && (this.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) || this.m_noPlacementCost || ZoneSystem.instance.GetGlobalKey(GlobalKeys.AllRecipesUnlocked)) && (this.RequiredCraftingStation(recipe, 1, false) || this.m_noPlacementCost))
				{
					available.Add(recipe);
				}
			}
		}
	}

	// Token: 0x060003C4 RID: 964 RVA: 0x000238CC File Offset: 0x00021ACC
	private void OnInventoryChanged()
	{
		if (this.m_isLoading)
		{
			return;
		}
		foreach (ItemDrop.ItemData itemData in this.m_inventory.GetAllItems())
		{
			this.AddKnownItem(itemData);
			if (!itemData.m_pickedUp)
			{
				itemData.m_pickedUp = true;
				PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
				playerProfile.IncrementStat(PlayerStatType.ItemsPickedUp, 1f);
				playerProfile.m_itemPickupStats.IncrementOrSet(itemData.m_shared.m_name, (float)itemData.m_stack);
			}
			if (itemData.m_shared.m_name == "$item_hammer")
			{
				this.ShowTutorial("hammer", false);
			}
			else if (itemData.m_shared.m_name == "$item_hoe")
			{
				this.ShowTutorial("hoe", false);
			}
			else if (itemData.m_shared.m_name == "$item_bellfragment")
			{
				this.ShowTutorial("bellfragment", false);
			}
			else if (itemData.m_shared.m_name == "$item_pickaxe_antler")
			{
				this.ShowTutorial("pickaxe", false);
			}
			else if (itemData.m_shared.m_name.CustomStartsWith("$item_shield"))
			{
				this.ShowTutorial("shield", false);
			}
			if (itemData.m_shared.m_name == "$item_trophy_eikthyr")
			{
				this.ShowTutorial("boss_trophy", false);
			}
			if (itemData.m_shared.m_name == "$item_wishbone")
			{
				this.ShowTutorial("wishbone", false);
			}
			else if (itemData.m_shared.m_name == "$item_copperore" || itemData.m_shared.m_name == "$item_tinore")
			{
				this.ShowTutorial("ore", false);
			}
			else if (itemData.m_shared.m_food > 0f || itemData.m_shared.m_foodStamina > 0f)
			{
				this.ShowTutorial("food", false);
			}
		}
		this.UpdateKnownRecipesList();
		this.UpdateAvailablePiecesList();
	}

	// Token: 0x060003C5 RID: 965 RVA: 0x00023AFC File Offset: 0x00021CFC
	public bool InDebugFlyMode()
	{
		return this.m_debugFly;
	}

	// Token: 0x060003C6 RID: 966 RVA: 0x00023B04 File Offset: 0x00021D04
	public void ShowTutorial(string name, bool force = false)
	{
		if (this.HaveSeenTutorial(name))
		{
			return;
		}
		Tutorial.instance.ShowText(name, force);
	}

	// Token: 0x060003C7 RID: 967 RVA: 0x00023B1C File Offset: 0x00021D1C
	public void SetSeenTutorial(string name)
	{
		if (name.Length == 0)
		{
			return;
		}
		if (this.m_shownTutorials.Contains(name))
		{
			return;
		}
		this.m_shownTutorials.Add(name);
	}

	// Token: 0x060003C8 RID: 968 RVA: 0x00023B43 File Offset: 0x00021D43
	public bool HaveSeenTutorial(string name)
	{
		return name.Length != 0 && this.m_shownTutorials.Contains(name);
	}

	// Token: 0x060003C9 RID: 969 RVA: 0x00023B5B File Offset: 0x00021D5B
	public static bool IsSeenTutorialsCleared()
	{
		return !Player.m_localPlayer || Player.m_localPlayer.m_shownTutorials.Count == 0;
	}

	// Token: 0x060003CA RID: 970 RVA: 0x00023B7D File Offset: 0x00021D7D
	public static void ResetSeenTutorials()
	{
		if (Player.m_localPlayer)
		{
			Player.m_localPlayer.m_shownTutorials.Clear();
		}
	}

	// Token: 0x060003CB RID: 971 RVA: 0x00023B9C File Offset: 0x00021D9C
	public void SetMouseLook(Vector2 mouseLook)
	{
		Quaternion quaternion = this.m_lookYaw * Quaternion.Euler(0f, mouseLook.x, 0f);
		if (PlayerCustomizaton.IsBarberGuiVisible())
		{
			if (Vector3.Dot(base.transform.rotation * Vector3.forward, this.m_lookYaw * Vector3.forward) > 0f)
			{
				this.SetMouseLookBackward(true);
			}
			if (Vector3.Dot(base.transform.rotation * Vector3.forward, quaternion * Vector3.forward) < 0f)
			{
				this.m_lookYaw = quaternion;
			}
		}
		else
		{
			this.m_lookYaw = quaternion;
		}
		this.m_lookPitch = Mathf.Clamp(this.m_lookPitch - mouseLook.y, -89f, 89f);
		this.UpdateEyeRotation();
		this.m_lookDir = this.m_eye.forward;
		if (this.m_lookTransitionTime > 0f && mouseLook != Vector2.zero)
		{
			this.m_lookTransitionTime = 0f;
		}
	}

	// Token: 0x060003CC RID: 972 RVA: 0x00023CA4 File Offset: 0x00021EA4
	public void SetMouseLookForward(bool includePitch = true)
	{
		this.m_lookYaw = Quaternion.Euler(0f, base.transform.rotation.eulerAngles.y, 0f);
		if (includePitch)
		{
			this.m_lookPitch = 0f;
		}
	}

	// Token: 0x060003CD RID: 973 RVA: 0x00023CEC File Offset: 0x00021EEC
	public void SetMouseLookBackward(bool includePitch = true)
	{
		this.m_lookYaw = Quaternion.Euler(0f, base.transform.rotation.eulerAngles.y + 180f, 0f);
		if (includePitch)
		{
			this.m_lookPitch = 0f;
		}
	}

	// Token: 0x060003CE RID: 974 RVA: 0x00023D3A File Offset: 0x00021F3A
	protected override void UpdateEyeRotation()
	{
		this.m_eye.rotation = this.m_lookYaw * Quaternion.Euler(this.m_lookPitch, 0f, 0f);
	}

	// Token: 0x060003CF RID: 975 RVA: 0x00023D67 File Offset: 0x00021F67
	public Ragdoll GetRagdoll()
	{
		return this.m_ragdoll;
	}

	// Token: 0x060003D0 RID: 976 RVA: 0x00023D6F File Offset: 0x00021F6F
	public void OnDodgeMortal()
	{
		this.m_dodgeInvincible = false;
	}

	// Token: 0x060003D1 RID: 977 RVA: 0x00023D78 File Offset: 0x00021F78
	private void UpdateDodge(float dt)
	{
		this.m_queuedDodgeTimer -= dt;
		if (this.m_queuedDodgeTimer > 0f && base.IsOnGround() && !this.IsDead() && !this.InAttack() && !this.IsEncumbered() && !this.InDodge() && !base.IsStaggering())
		{
			float num = this.m_dodgeStaminaUsage - this.m_dodgeStaminaUsage * this.GetEquipmentMovementModifier() + this.m_dodgeStaminaUsage * this.GetEquipmentDodgeStaminaModifier();
			this.m_seman.ModifyDodgeStaminaUsage(num, ref num, true);
			if (this.HaveStamina(num))
			{
				this.ClearActionQueue();
				this.m_queuedDodgeTimer = 0f;
				this.m_dodgeInvincible = true;
				base.transform.rotation = Quaternion.LookRotation(this.m_queuedDodgeDir);
				this.m_body.rotation = base.transform.rotation;
				this.m_zanim.SetTrigger("dodge");
				base.AddNoise(5f);
				this.UseStamina(num);
				this.m_dodgeEffects.Create(base.transform.position, Quaternion.identity, base.transform, 1f, -1);
			}
			else
			{
				Hud.instance.StaminaBarEmptyFlash();
			}
		}
		bool flag = this.m_animator.GetBool(Player.s_animatorTagDodge) || base.GetNextOrCurrentAnimHash() == Player.s_animatorTagDodge;
		bool flag2 = flag && this.m_dodgeInvincible;
		if (this.m_dodgeInvincibleCached != flag2)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_dodgeinv, flag2);
		}
		this.m_dodgeInvincibleCached = flag2;
		this.m_inDodge = flag;
	}

	// Token: 0x060003D2 RID: 978 RVA: 0x00023F1A File Offset: 0x0002211A
	public override bool IsDodgeInvincible()
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_dodgeInvincibleCached;
		}
		return this.m_nview.GetZDO().GetBool(ZDOVars.s_dodgeinv, false);
	}

	// Token: 0x060003D3 RID: 979 RVA: 0x00023F55 File Offset: 0x00022155
	public override bool InDodge()
	{
		return this.m_nview.IsValid() && this.m_nview.IsOwner() && this.m_inDodge;
	}

	// Token: 0x060003D4 RID: 980 RVA: 0x00023F7C File Offset: 0x0002217C
	public override bool IsDead()
	{
		ZDO zdo = this.m_nview.GetZDO();
		return zdo != null && zdo.GetBool(ZDOVars.s_dead, false);
	}

	// Token: 0x060003D5 RID: 981 RVA: 0x00023FA6 File Offset: 0x000221A6
	private void Dodge(Vector3 dodgeDir)
	{
		this.m_queuedDodgeTimer = 0.5f;
		this.m_queuedDodgeDir = dodgeDir;
	}

	// Token: 0x060003D6 RID: 982 RVA: 0x00023FBC File Offset: 0x000221BC
	protected override bool AlwaysRotateCamera()
	{
		ItemDrop.ItemData currentWeapon = base.GetCurrentWeapon();
		if ((currentWeapon != null && this.m_currentAttack != null && this.m_lastCombatTimer < 1f && this.m_currentAttack.m_attackType != Attack.AttackType.None && !this.m_attackTowardsPlayerLookDir) || this.IsDrawingBow() || this.m_blocking)
		{
			return true;
		}
		if (currentWeapon != null && currentWeapon.m_shared.m_alwaysRotate && this.m_moveDir.magnitude < 0.01f)
		{
			return true;
		}
		if (this.m_currentAttack != null && this.m_currentAttack.m_loopingAttack && this.InAttack())
		{
			return true;
		}
		if (this.InPlaceMode())
		{
			Vector3 from = base.GetLookYaw() * Vector3.forward;
			Vector3 forward = base.transform.forward;
			if (Vector3.Angle(from, forward) > 95f)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060003D7 RID: 983 RVA: 0x00024088 File Offset: 0x00022288
	public override bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		if (!this.m_nview.IsOwner())
		{
			this.m_nview.InvokeRPC("RPC_TeleportTo", new object[]
			{
				pos,
				rot,
				distantTeleport
			});
			return false;
		}
		if (this.IsTeleporting())
		{
			return false;
		}
		if (this.m_teleportCooldown < 2f)
		{
			return false;
		}
		this.m_teleporting = true;
		this.m_distantTeleport = distantTeleport;
		this.m_teleportTimer = 0f;
		this.m_teleportCooldown = 0f;
		base.InvalidateCachedLiquidDepth();
		this.m_teleportFromPos = base.transform.position;
		this.m_teleportFromRot = base.transform.rotation;
		this.m_teleportTargetPos = pos;
		this.m_teleportTargetRot = rot;
		return true;
	}

	// Token: 0x060003D8 RID: 984 RVA: 0x0002414C File Offset: 0x0002234C
	private void UpdateTeleport(float dt)
	{
		if (!this.m_teleporting)
		{
			this.m_teleportCooldown += dt;
			return;
		}
		this.m_teleportCooldown = 0f;
		this.m_teleportTimer += dt;
		if (this.m_teleportTimer > 2f)
		{
			Vector3 dir = this.m_teleportTargetRot * Vector3.forward;
			base.transform.position = this.m_teleportTargetPos;
			base.transform.rotation = this.m_teleportTargetRot;
			this.m_body.velocity = Vector3.zero;
			this.m_maxAirAltitude = base.transform.position.y;
			base.SetLookDir(dir, 0f);
			if ((this.m_teleportTimer > 8f || !this.m_distantTeleport) && ZNetScene.instance.IsAreaReady(this.m_teleportTargetPos))
			{
				float num = 0f;
				if (ZoneSystem.instance.FindFloor(this.m_teleportTargetPos, out num))
				{
					this.m_teleportTimer = 0f;
					this.m_teleporting = false;
					base.ResetCloth();
					return;
				}
				if (this.m_teleportTimer > 15f || !this.m_distantTeleport)
				{
					if (this.m_distantTeleport)
					{
						Vector3 position = base.transform.position;
						position.y = ZoneSystem.instance.GetSolidHeight(this.m_teleportTargetPos) + 0.5f;
						base.transform.position = position;
					}
					else
					{
						base.transform.rotation = this.m_teleportFromRot;
						base.transform.position = this.m_teleportFromPos;
						this.m_maxAirAltitude = base.transform.position.y;
						this.Message(MessageHud.MessageType.Center, "$msg_portal_blocked", 0, null);
					}
					this.m_teleportTimer = 0f;
					this.m_teleporting = false;
					base.ResetCloth();
				}
			}
		}
	}

	// Token: 0x060003D9 RID: 985 RVA: 0x00024313 File Offset: 0x00022513
	public override bool IsTeleporting()
	{
		return this.m_teleporting;
	}

	// Token: 0x060003DA RID: 986 RVA: 0x0002431B File Offset: 0x0002251B
	public bool ShowTeleportAnimation()
	{
		return this.m_teleporting && this.m_distantTeleport;
	}

	// Token: 0x060003DB RID: 987 RVA: 0x0002432D File Offset: 0x0002252D
	public void SetPlayerModel(int index)
	{
		if (this.m_modelIndex == index)
		{
			return;
		}
		this.m_modelIndex = index;
		this.m_visEquipment.SetModel(index);
	}

	// Token: 0x060003DC RID: 988 RVA: 0x0002434C File Offset: 0x0002254C
	public int GetPlayerModel()
	{
		return this.m_modelIndex;
	}

	// Token: 0x060003DD RID: 989 RVA: 0x00024354 File Offset: 0x00022554
	public void SetSkinColor(Vector3 color)
	{
		if (color == this.m_skinColor)
		{
			return;
		}
		this.m_skinColor = color;
		this.m_visEquipment.SetSkinColor(this.m_skinColor);
	}

	// Token: 0x060003DE RID: 990 RVA: 0x0002437D File Offset: 0x0002257D
	public void SetHairColor(Vector3 color)
	{
		if (this.m_hairColor == color)
		{
			return;
		}
		this.m_hairColor = color;
		this.m_visEquipment.SetHairColor(this.m_hairColor);
	}

	// Token: 0x060003DF RID: 991 RVA: 0x000243A6 File Offset: 0x000225A6
	public Vector3 GetHairColor()
	{
		return this.m_hairColor;
	}

	// Token: 0x060003E0 RID: 992 RVA: 0x000243AE File Offset: 0x000225AE
	protected override void SetupVisEquipment(VisEquipment visEq, bool isRagdoll)
	{
		base.SetupVisEquipment(visEq, isRagdoll);
		visEq.SetModel(this.m_modelIndex);
		visEq.SetSkinColor(this.m_skinColor);
		visEq.SetHairColor(this.m_hairColor);
	}

	// Token: 0x060003E1 RID: 993 RVA: 0x000243DC File Offset: 0x000225DC
	public override bool CanConsumeItem(ItemDrop.ItemData item, bool checkWorldLevel = false)
	{
		if (!base.CanConsumeItem(item, checkWorldLevel))
		{
			return false;
		}
		if (item.m_shared.m_food > 0f && !this.CanEat(item, true))
		{
			return false;
		}
		if (item.m_shared.m_consumeStatusEffect)
		{
			StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
			if (this.m_seman.HaveStatusEffect(item.m_shared.m_consumeStatusEffect.NameHash()) || this.m_seman.HaveStatusEffectCategory(consumeStatusEffect.m_category))
			{
				this.Message(MessageHud.MessageType.Center, "$msg_cantconsume", 0, null);
				return false;
			}
		}
		return true;
	}

	// Token: 0x060003E2 RID: 994 RVA: 0x00024474 File Offset: 0x00022674
	public override bool ConsumeItem(Inventory inventory, ItemDrop.ItemData item, bool checkWorldLevel = false)
	{
		if (!this.CanConsumeItem(item, checkWorldLevel))
		{
			return false;
		}
		if (item.m_shared.m_consumeStatusEffect)
		{
			StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
			this.m_seman.AddStatusEffect(item.m_shared.m_consumeStatusEffect, true, 0, 0f);
		}
		if (item.m_shared.m_food > 0f)
		{
			this.EatFood(item);
		}
		inventory.RemoveOneItem(item);
		return true;
	}

	// Token: 0x060003E3 RID: 995 RVA: 0x000244EC File Offset: 0x000226EC
	public void SetIntro(bool intro)
	{
		if (this.m_intro == intro)
		{
			return;
		}
		this.m_intro = intro;
		this.m_zanim.SetBool("intro", intro);
	}

	// Token: 0x060003E4 RID: 996 RVA: 0x00024510 File Offset: 0x00022710
	public override bool InIntro()
	{
		return this.m_intro;
	}

	// Token: 0x060003E5 RID: 997 RVA: 0x00024518 File Offset: 0x00022718
	public override bool InCutscene()
	{
		return base.GetCurrentAnimHash() == Player.s_animatorTagCutscene || this.InIntro() || this.m_sleeping || base.InCutscene();
	}

	// Token: 0x060003E6 RID: 998 RVA: 0x00024544 File Offset: 0x00022744
	public void SetMaxStamina(float stamina, bool flashBar)
	{
		if (flashBar && Hud.instance != null && stamina > this.m_maxStamina)
		{
			Hud.instance.StaminaBarUppgradeFlash();
		}
		this.m_maxStamina = stamina;
		this.m_stamina = Mathf.Clamp(this.m_stamina, 0f, this.m_maxStamina);
	}

	// Token: 0x060003E7 RID: 999 RVA: 0x00024598 File Offset: 0x00022798
	private void SetMaxEitr(float eitr, bool flashBar)
	{
		if (flashBar && Hud.instance != null && eitr > this.m_maxEitr)
		{
			Hud.instance.EitrBarUppgradeFlash();
		}
		this.m_maxEitr = eitr;
		this.m_eitr = Mathf.Clamp(this.m_eitr, 0f, this.m_maxEitr);
	}

	// Token: 0x060003E8 RID: 1000 RVA: 0x000245EB File Offset: 0x000227EB
	public void SetMaxHealth(float health, bool flashBar)
	{
		if (flashBar && Hud.instance != null && health > base.GetMaxHealth())
		{
			Hud.instance.FlashHealthBar();
		}
		base.SetMaxHealth(health);
	}

	// Token: 0x060003E9 RID: 1001 RVA: 0x00024617 File Offset: 0x00022817
	public override bool IsPVPEnabled()
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_pvp;
		}
		return this.m_nview.GetZDO().GetBool(ZDOVars.s_pvp, false);
	}

	// Token: 0x060003EA RID: 1002 RVA: 0x00024654 File Offset: 0x00022854
	public void SetPVP(bool enabled)
	{
		if (this.m_pvp == enabled)
		{
			return;
		}
		this.m_pvp = enabled;
		this.m_nview.GetZDO().Set(ZDOVars.s_pvp, this.m_pvp);
		if (this.m_pvp)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_pvpon", 0, null);
			return;
		}
		this.Message(MessageHud.MessageType.Center, "$msg_pvpoff", 0, null);
	}

	// Token: 0x060003EB RID: 1003 RVA: 0x000246B2 File Offset: 0x000228B2
	public bool CanSwitchPVP()
	{
		return this.m_lastCombatTimer > 10f;
	}

	// Token: 0x060003EC RID: 1004 RVA: 0x000246C1 File Offset: 0x000228C1
	public bool NoCostCheat()
	{
		return this.m_noPlacementCost;
	}

	// Token: 0x060003ED RID: 1005 RVA: 0x000246CC File Offset: 0x000228CC
	public bool StartEmote(string emote, bool oneshot = true)
	{
		if (!this.CanMove() || this.InAttack() || this.IsDrawingBow() || this.IsAttached() || this.IsAttachedToShip())
		{
			return false;
		}
		this.SetCrouch(false);
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_emoteID, 0);
		this.m_nview.GetZDO().Set(ZDOVars.s_emoteID, @int + 1, false);
		this.m_nview.GetZDO().Set(ZDOVars.s_emote, emote);
		this.m_nview.GetZDO().Set(ZDOVars.s_emoteOneshot, oneshot);
		Player.LastEmote = emote;
		Player.LastEmoteTime = DateTime.Now;
		return true;
	}

	// Token: 0x060003EE RID: 1006 RVA: 0x00024778 File Offset: 0x00022978
	protected override void StopEmote()
	{
		if (this.m_nview.GetZDO().GetString(ZDOVars.s_emote, "") != "")
		{
			int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_emoteID, 0);
			this.m_nview.GetZDO().Set(ZDOVars.s_emoteID, @int + 1, false);
			this.m_nview.GetZDO().Set(ZDOVars.s_emote, "");
		}
	}

	// Token: 0x060003EF RID: 1007 RVA: 0x000247F8 File Offset: 0x000229F8
	private void UpdateEmote()
	{
		if (this.m_nview.IsOwner() && this.InEmote() && this.m_moveDir != Vector3.zero)
		{
			this.StopEmote();
		}
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_emoteID, 0);
		if (@int != this.m_emoteID)
		{
			this.m_emoteID = @int;
			if (!string.IsNullOrEmpty(this.m_emoteState))
			{
				this.m_animator.SetBool("emote_" + this.m_emoteState, false);
			}
			this.m_emoteState = "";
			this.m_animator.SetTrigger("emote_stop");
			string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_emote, "");
			if (!string.IsNullOrEmpty(@string))
			{
				bool @bool = this.m_nview.GetZDO().GetBool(ZDOVars.s_emoteOneshot, false);
				this.m_animator.ResetTrigger("emote_stop");
				if (@bool)
				{
					this.m_animator.SetTrigger("emote_" + @string);
					return;
				}
				this.m_emoteState = @string;
				this.m_animator.SetBool("emote_" + @string, true);
			}
		}
	}

	// Token: 0x060003F0 RID: 1008 RVA: 0x00024920 File Offset: 0x00022B20
	public override bool InEmote()
	{
		return !string.IsNullOrEmpty(this.m_emoteState) || base.GetCurrentAnimHash() == Player.s_animatorTagEmote;
	}

	// Token: 0x060003F1 RID: 1009 RVA: 0x0002493E File Offset: 0x00022B3E
	public override bool IsCrouching()
	{
		return base.GetCurrentAnimHash() == Player.s_animatorTagCrouch;
	}

	// Token: 0x060003F2 RID: 1010 RVA: 0x00024950 File Offset: 0x00022B50
	private void UpdateCrouch(float dt)
	{
		if (this.m_crouchToggled)
		{
			if (!this.HaveStamina(0f) || base.IsSwimming() || this.InBed() || this.InPlaceMode() || this.m_run || this.IsBlocking() || base.IsFlying())
			{
				this.SetCrouch(false);
			}
			bool flag = this.InAttack() || this.IsDrawingBow();
			this.m_zanim.SetBool(Player.s_crouching, this.m_crouchToggled && !flag);
			return;
		}
		this.m_zanim.SetBool(Player.s_crouching, false);
	}

	// Token: 0x060003F3 RID: 1011 RVA: 0x000249EC File Offset: 0x00022BEC
	protected override void SetCrouch(bool crouch)
	{
		this.m_crouchToggled = crouch;
	}

	// Token: 0x060003F4 RID: 1012 RVA: 0x000249F8 File Offset: 0x00022BF8
	public void SetGuardianPower(string name)
	{
		this.m_guardianPower = name;
		this.m_guardianPowerHash = (string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
		this.m_guardianSE = ObjectDB.instance.GetStatusEffect(this.m_guardianPowerHash);
		if (ZoneSystem.instance)
		{
			this.AddUniqueKey(name);
			Game.instance.IncrementPlayerStat(PlayerStatType.SetGuardianPower, 1f);
			uint num = <PrivateImplementationDetails>.ComputeStringHash(name);
			if (num <= 2474185561U)
			{
				if (num <= 2006288425U)
				{
					if (num != 1427920915U)
					{
						if (num == 2006288425U)
						{
							if (name == "GP_Bonemass")
							{
								Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerBonemass, 1f);
								return;
							}
						}
					}
					else if (name == "GP_Queen")
					{
						Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerQueen, 1f);
						return;
					}
				}
				else if (num != 2142994390U)
				{
					if (num == 2474185561U)
					{
						if (name == "GP_Yagluth")
						{
							Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerYagluth, 1f);
							return;
						}
					}
				}
				else if (name == "GP_Moder")
				{
					Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerModer, 1f);
					return;
				}
			}
			else if (num <= 3121473449U)
			{
				if (num != 2548002664U)
				{
					if (num == 3121473449U)
					{
						if (name == "GP_Ashlands")
						{
							Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerAshlands, 1f);
							return;
						}
					}
				}
				else if (name == "GP_DeepNorth")
				{
					Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerDeepNorth, 1f);
					return;
				}
			}
			else if (num != 3360619182U)
			{
				if (num == 3839426325U)
				{
					if (name == "GP_Eikthyr")
					{
						Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerEikthyr, 1f);
						return;
					}
				}
			}
			else if (name == "GP_TheElder")
			{
				Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerElder, 1f);
				return;
			}
			ZLog.LogWarning("Missing stat for guardian power: " + name);
		}
	}

	// Token: 0x060003F5 RID: 1013 RVA: 0x00024C2B File Offset: 0x00022E2B
	public string GetGuardianPowerName()
	{
		return this.m_guardianPower;
	}

	// Token: 0x060003F6 RID: 1014 RVA: 0x00024C33 File Offset: 0x00022E33
	public void GetGuardianPowerHUD(out StatusEffect se, out float cooldown)
	{
		se = this.m_guardianSE;
		cooldown = this.m_guardianPowerCooldown;
	}

	// Token: 0x060003F7 RID: 1015 RVA: 0x00024C48 File Offset: 0x00022E48
	public bool StartGuardianPower()
	{
		if (this.m_guardianSE == null)
		{
			return false;
		}
		if ((this.InAttack() && !this.HaveQueuedChain()) || this.InDodge() || !this.CanMove() || base.IsKnockedBack() || base.IsStaggering() || this.InMinorAction())
		{
			return false;
		}
		if (this.m_guardianPowerCooldown > 0f)
		{
			this.Message(MessageHud.MessageType.Center, "$hud_powernotready", 0, null);
			return false;
		}
		this.m_zanim.SetTrigger("gpower");
		Game.instance.IncrementPlayerStat(PlayerStatType.UseGuardianPower, 1f);
		string prefabName = global::Utils.GetPrefabName(this.m_guardianSE.name);
		uint num = <PrivateImplementationDetails>.ComputeStringHash(prefabName);
		if (num <= 2474185561U)
		{
			if (num <= 2006288425U)
			{
				if (num != 1427920915U)
				{
					if (num == 2006288425U)
					{
						if (prefabName == "GP_Bonemass")
						{
							Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerBonemass, 1f);
							return true;
						}
					}
				}
				else if (prefabName == "GP_Queen")
				{
					Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerQueen, 1f);
					return true;
				}
			}
			else if (num != 2142994390U)
			{
				if (num == 2474185561U)
				{
					if (prefabName == "GP_Yagluth")
					{
						Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerYagluth, 1f);
						return true;
					}
				}
			}
			else if (prefabName == "GP_Moder")
			{
				Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerModer, 1f);
				return true;
			}
		}
		else if (num <= 3121473449U)
		{
			if (num != 2548002664U)
			{
				if (num == 3121473449U)
				{
					if (prefabName == "GP_Ashlands")
					{
						Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerAshlands, 1f);
						return true;
					}
				}
			}
			else if (prefabName == "GP_DeepNorth")
			{
				Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerDeepNorth, 1f);
				return true;
			}
		}
		else if (num != 3360619182U)
		{
			if (num == 3839426325U)
			{
				if (prefabName == "GP_Eikthyr")
				{
					Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerEikthyr, 1f);
					return true;
				}
			}
		}
		else if (prefabName == "GP_TheElder")
		{
			Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerElder, 1f);
			return true;
		}
		ZLog.LogWarning("Missing stat for guardian power: " + prefabName);
		return true;
	}

	// Token: 0x060003F8 RID: 1016 RVA: 0x00024EC8 File Offset: 0x000230C8
	public bool ActivateGuardianPower()
	{
		if (this.m_guardianPowerCooldown > 0f)
		{
			return false;
		}
		if (this.m_guardianSE == null)
		{
			return false;
		}
		List<Player> list = new List<Player>();
		Player.GetPlayersInRange(base.transform.position, 10f, list);
		foreach (Player player in list)
		{
			player.GetSEMan().AddStatusEffect(this.m_guardianSE.NameHash(), true, 0, 0f);
		}
		this.m_guardianPowerCooldown = this.m_guardianSE.m_cooldown;
		return false;
	}

	// Token: 0x060003F9 RID: 1017 RVA: 0x00024F78 File Offset: 0x00023178
	private void UpdateGuardianPower(float dt)
	{
		this.m_guardianPowerCooldown -= dt;
		if (this.m_guardianPowerCooldown < 0f)
		{
			this.m_guardianPowerCooldown = 0f;
		}
	}

	// Token: 0x060003FA RID: 1018 RVA: 0x00024FA0 File Offset: 0x000231A0
	public override void AttachStart(Transform attachPoint, GameObject colliderRoot, bool hideWeapons, bool isBed, bool onShip, string attachAnimation, Vector3 detachOffset, Transform cameraPos = null)
	{
		if (this.m_attached)
		{
			return;
		}
		this.m_attached = true;
		this.m_attachedToShip = onShip;
		this.m_attachPoint = attachPoint;
		this.m_detachOffset = detachOffset;
		this.m_attachAnimation = attachAnimation;
		this.m_attachPointCamera = cameraPos;
		this.m_zanim.SetBool(attachAnimation, true);
		this.m_nview.GetZDO().Set(ZDOVars.s_inBed, isBed);
		if (colliderRoot != null)
		{
			this.m_attachColliders = colliderRoot.GetComponentsInChildren<Collider>();
			ZLog.Log("Ignoring " + this.m_attachColliders.Length.ToString() + " colliders");
			foreach (Collider collider in this.m_attachColliders)
			{
				Physics.IgnoreCollision(this.m_collider, collider, true);
			}
		}
		if (hideWeapons)
		{
			base.HideHandItems(false, true);
		}
		this.UpdateAttach();
		base.ResetCloth();
	}

	// Token: 0x060003FB RID: 1019 RVA: 0x00025080 File Offset: 0x00023280
	private void UpdateAttach()
	{
		if (this.m_attached)
		{
			if (this.m_attachPoint != null)
			{
				base.transform.position = this.m_attachPoint.position;
				base.transform.rotation = this.m_attachPoint.rotation;
				Rigidbody componentInParent = this.m_attachPoint.GetComponentInParent<Rigidbody>();
				this.m_body.useGravity = false;
				this.m_body.velocity = (componentInParent ? componentInParent.GetPointVelocity(base.transform.position) : Vector3.zero);
				this.m_body.angularVelocity = Vector3.zero;
				this.m_maxAirAltitude = base.transform.position.y;
				return;
			}
			this.AttachStop();
		}
	}

	// Token: 0x060003FC RID: 1020 RVA: 0x00025145 File Offset: 0x00023345
	public override bool IsAttached()
	{
		return this.m_attached || base.IsAttached();
	}

	// Token: 0x060003FD RID: 1021 RVA: 0x00025157 File Offset: 0x00023357
	public Transform GetAttachPoint()
	{
		return this.m_attachPoint;
	}

	// Token: 0x060003FE RID: 1022 RVA: 0x0002515F File Offset: 0x0002335F
	public Transform GetAttachCameraPoint()
	{
		return this.m_attachPointCamera;
	}

	// Token: 0x060003FF RID: 1023 RVA: 0x00025167 File Offset: 0x00023367
	public void ResetAttachCameraPoint()
	{
		this.m_attachPointCamera = null;
	}

	// Token: 0x06000400 RID: 1024 RVA: 0x00025170 File Offset: 0x00023370
	public override bool IsAttachedToShip()
	{
		return this.m_attached && this.m_attachedToShip;
	}

	// Token: 0x06000401 RID: 1025 RVA: 0x00025182 File Offset: 0x00023382
	public override bool IsRiding()
	{
		return this.m_doodadController != null && this.m_doodadController.IsValid() && this.m_doodadController is Sadle;
	}

	// Token: 0x06000402 RID: 1026 RVA: 0x000251A9 File Offset: 0x000233A9
	public override bool InBed()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool(ZDOVars.s_inBed, false);
	}

	// Token: 0x06000403 RID: 1027 RVA: 0x000251D0 File Offset: 0x000233D0
	public override void AttachStop()
	{
		if (this.m_sleeping)
		{
			return;
		}
		if (this.m_attached)
		{
			if (this.m_attachPoint != null)
			{
				base.transform.position = this.m_attachPoint.TransformPoint(this.m_detachOffset);
			}
			if (this.m_attachColliders != null)
			{
				foreach (Collider collider in this.m_attachColliders)
				{
					if (collider)
					{
						Physics.IgnoreCollision(this.m_collider, collider, false);
					}
				}
				this.m_attachColliders = null;
			}
			this.m_body.useGravity = true;
			this.m_attached = false;
			this.m_attachPoint = null;
			this.m_attachPointCamera = null;
			this.m_zanim.SetBool(this.m_attachAnimation, false);
			this.m_nview.GetZDO().Set(ZDOVars.s_inBed, false);
			base.ResetCloth();
		}
	}

	// Token: 0x06000404 RID: 1028 RVA: 0x000252A7 File Offset: 0x000234A7
	public void StartDoodadControl(IDoodadController shipControl)
	{
		this.m_doodadController = shipControl;
		ZLog.Log("Doodad controlls set " + shipControl.GetControlledComponent().gameObject.name);
	}

	// Token: 0x06000405 RID: 1029 RVA: 0x000252CF File Offset: 0x000234CF
	public void StopDoodadControl()
	{
		if (this.m_doodadController != null)
		{
			if (this.m_doodadController.IsValid())
			{
				this.m_doodadController.OnUseStop(this);
			}
			ZLog.Log("Stop doodad controlls");
			this.m_doodadController = null;
		}
	}

	// Token: 0x06000406 RID: 1030 RVA: 0x00025303 File Offset: 0x00023503
	private void SetDoodadControlls(ref Vector3 moveDir, ref Vector3 lookDir, ref bool run, ref bool autoRun, bool block)
	{
		if (this.m_doodadController.IsValid())
		{
			this.m_doodadController.ApplyControlls(moveDir, lookDir, run, autoRun, block);
		}
		moveDir = Vector3.zero;
		autoRun = false;
		run = false;
	}

	// Token: 0x06000407 RID: 1031 RVA: 0x00025342 File Offset: 0x00023542
	public Ship GetControlledShip()
	{
		if (this.m_doodadController != null && this.m_doodadController.IsValid())
		{
			return this.m_doodadController.GetControlledComponent() as Ship;
		}
		return null;
	}

	// Token: 0x06000408 RID: 1032 RVA: 0x0002536B File Offset: 0x0002356B
	public IDoodadController GetDoodadController()
	{
		return this.m_doodadController;
	}

	// Token: 0x06000409 RID: 1033 RVA: 0x00025374 File Offset: 0x00023574
	private void UpdateDoodadControls(float dt)
	{
		if (this.m_doodadController == null)
		{
			return;
		}
		if (!this.m_doodadController.IsValid())
		{
			this.StopDoodadControl();
			return;
		}
		Vector3 forward = this.m_doodadController.GetControlledComponent().transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Quaternion to = Quaternion.LookRotation(forward);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, 100f * dt);
		if (Vector3.Distance(this.m_doodadController.GetPosition(), base.transform.position) > this.m_maxInteractDistance)
		{
			this.StopDoodadControl();
		}
	}

	// Token: 0x0600040A RID: 1034 RVA: 0x0002541A File Offset: 0x0002361A
	public bool IsSleeping()
	{
		return this.m_sleeping;
	}

	// Token: 0x0600040B RID: 1035 RVA: 0x00025424 File Offset: 0x00023624
	public void SetSleeping(bool sleep)
	{
		if (this.m_sleeping == sleep)
		{
			return;
		}
		this.m_sleeping = sleep;
		if (!sleep)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_goodmorning", 0, null);
			this.m_seman.AddStatusEffect(SEMan.s_statusEffectRested, true, 0, 0f);
			this.m_wakeupTime = ZNet.instance.GetTimeSeconds();
			Game.instance.IncrementPlayerStat(PlayerStatType.Sleep, 1f);
		}
	}

	// Token: 0x0600040C RID: 1036 RVA: 0x0002548C File Offset: 0x0002368C
	public void SetControls(Vector3 movedir, bool attack, bool attackHold, bool secondaryAttack, bool secondaryAttackHold, bool block, bool blockHold, bool jump, bool crouch, bool run, bool autoRun, bool dodge = false)
	{
		if ((this.IsAttached() || this.InEmote()) && (movedir != Vector3.zero || attack || secondaryAttack || block || blockHold || jump || crouch) && this.GetDoodadController() == null)
		{
			attack = false;
			attackHold = false;
			secondaryAttack = false;
			secondaryAttackHold = false;
			this.StopEmote();
			this.AttachStop();
		}
		if (this.m_doodadController != null)
		{
			this.SetDoodadControlls(ref movedir, ref this.m_lookDir, ref run, ref autoRun, blockHold);
			if (jump || attack || secondaryAttack || dodge)
			{
				attack = false;
				attackHold = false;
				secondaryAttack = false;
				secondaryAttackHold = false;
				this.StopDoodadControl();
			}
		}
		if (run)
		{
			this.m_walk = false;
		}
		if (!this.m_autoRun)
		{
			Vector3 lookDir = this.m_lookDir;
			lookDir.y = 0f;
			lookDir.Normalize();
			this.m_moveDir = movedir.z * lookDir + movedir.x * Vector3.Cross(Vector3.up, lookDir);
		}
		if (!this.m_autoRun && autoRun && !this.InPlaceMode())
		{
			this.m_autoRun = true;
			this.SetCrouch(false);
			this.m_moveDir = this.m_lookDir;
			this.m_moveDir.y = 0f;
			this.m_moveDir.Normalize();
		}
		else if (this.m_autoRun)
		{
			if (attack || jump || dodge || crouch || movedir != Vector3.zero || this.InPlaceMode() || attackHold || secondaryAttackHold)
			{
				this.m_autoRun = false;
			}
			else if (autoRun || blockHold)
			{
				this.m_moveDir = this.m_lookDir;
				this.m_moveDir.y = 0f;
				this.m_moveDir.Normalize();
				blockHold = false;
				block = false;
			}
		}
		this.m_attack = attack;
		this.m_attackHold = attackHold;
		this.m_secondaryAttack = secondaryAttack;
		this.m_secondaryAttackHold = secondaryAttackHold;
		this.m_blocking = blockHold;
		this.m_run = run;
		if (crouch)
		{
			this.SetCrouch(!this.m_crouchToggled);
		}
		if (ZInput.InputLayout == InputLayout.Default || !ZInput.IsGamepadActive())
		{
			if (jump)
			{
				if (this.m_blocking)
				{
					Vector3 dodgeDir = this.m_moveDir;
					if (dodgeDir.magnitude < 0.1f)
					{
						dodgeDir = -this.m_lookDir;
						dodgeDir.y = 0f;
						dodgeDir.Normalize();
					}
					this.Dodge(dodgeDir);
					return;
				}
				if (this.IsCrouching() || this.m_crouchToggled)
				{
					Vector3 dodgeDir2 = this.m_moveDir;
					if (dodgeDir2.magnitude < 0.1f)
					{
						dodgeDir2 = this.m_lookDir;
						dodgeDir2.y = 0f;
						dodgeDir2.Normalize();
					}
					this.Dodge(dodgeDir2);
					return;
				}
				base.Jump(false);
				return;
			}
		}
		else if (ZInput.IsNonClassicFunctionality())
		{
			if (dodge)
			{
				if (this.m_blocking)
				{
					Vector3 dodgeDir3 = this.m_moveDir;
					if (dodgeDir3.magnitude < 0.1f)
					{
						dodgeDir3 = -this.m_lookDir;
						dodgeDir3.y = 0f;
						dodgeDir3.Normalize();
					}
					this.Dodge(dodgeDir3);
				}
				else if (this.IsCrouching() || this.m_crouchToggled)
				{
					Vector3 dodgeDir4 = this.m_moveDir;
					if (dodgeDir4.magnitude < 0.1f)
					{
						dodgeDir4 = this.m_lookDir;
						dodgeDir4.y = 0f;
						dodgeDir4.Normalize();
					}
					this.Dodge(dodgeDir4);
				}
			}
			if (jump)
			{
				base.Jump(false);
			}
		}
	}

	// Token: 0x0600040D RID: 1037 RVA: 0x000257CC File Offset: 0x000239CC
	private void UpdateTargeted(float dt)
	{
		this.m_timeSinceTargeted += dt;
		this.m_timeSinceSensed += dt;
	}

	// Token: 0x0600040E RID: 1038 RVA: 0x000257EC File Offset: 0x000239EC
	public override void OnTargeted(bool sensed, bool alerted)
	{
		if (sensed)
		{
			if (this.m_timeSinceSensed > 0.5f)
			{
				this.m_timeSinceSensed = 0f;
				this.m_nview.InvokeRPC("OnTargeted", new object[]
				{
					sensed,
					alerted
				});
				return;
			}
		}
		else if (this.m_timeSinceTargeted > 0.5f)
		{
			this.m_timeSinceTargeted = 0f;
			this.m_nview.InvokeRPC("OnTargeted", new object[]
			{
				sensed,
				alerted
			});
		}
	}

	// Token: 0x0600040F RID: 1039 RVA: 0x0002587D File Offset: 0x00023A7D
	private void RPC_OnTargeted(long sender, bool sensed, bool alerted)
	{
		this.m_timeSinceTargeted = 0f;
		if (sensed)
		{
			this.m_timeSinceSensed = 0f;
		}
		if (alerted)
		{
			MusicMan.instance.ResetCombatTimer();
		}
	}

	// Token: 0x06000410 RID: 1040 RVA: 0x000258A5 File Offset: 0x00023AA5
	protected override void OnDamaged(HitData hit)
	{
		base.OnDamaged(hit);
		if (hit.GetTotalDamage() > base.GetMaxHealth() / 10f)
		{
			Hud.instance.DamageFlash();
		}
	}

	// Token: 0x06000411 RID: 1041 RVA: 0x000258CC File Offset: 0x00023ACC
	public bool IsTargeted()
	{
		return this.m_timeSinceTargeted < 1f;
	}

	// Token: 0x06000412 RID: 1042 RVA: 0x000258DB File Offset: 0x00023ADB
	public bool IsSensed()
	{
		return this.m_timeSinceSensed < 1f;
	}

	// Token: 0x06000413 RID: 1043 RVA: 0x000258EC File Offset: 0x00023AEC
	protected override void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
	{
		if (this.m_chestItem != null)
		{
			mods.Apply(this.m_chestItem.m_shared.m_damageModifiers);
		}
		if (this.m_legItem != null)
		{
			mods.Apply(this.m_legItem.m_shared.m_damageModifiers);
		}
		if (this.m_helmetItem != null)
		{
			mods.Apply(this.m_helmetItem.m_shared.m_damageModifiers);
		}
		if (this.m_shoulderItem != null)
		{
			mods.Apply(this.m_shoulderItem.m_shared.m_damageModifiers);
		}
	}

	// Token: 0x06000414 RID: 1044 RVA: 0x00025974 File Offset: 0x00023B74
	public override float GetBodyArmor()
	{
		float num = 0f;
		if (this.m_chestItem != null)
		{
			num += this.m_chestItem.GetArmor();
		}
		if (this.m_legItem != null)
		{
			num += this.m_legItem.GetArmor();
		}
		if (this.m_helmetItem != null)
		{
			num += this.m_helmetItem.GetArmor();
		}
		if (this.m_shoulderItem != null)
		{
			num += this.m_shoulderItem.GetArmor();
		}
		return num;
	}

	// Token: 0x06000415 RID: 1045 RVA: 0x000259E0 File Offset: 0x00023BE0
	public bool TryGetArmorDifference(ItemDrop.ItemData item, out float difference)
	{
		ItemDrop.ItemData.ItemType itemType = item.m_shared.m_itemType;
		if (itemType <= ItemDrop.ItemData.ItemType.Chest)
		{
			if (itemType == ItemDrop.ItemData.ItemType.Helmet)
			{
				if (this.m_helmetItem == null)
				{
					difference = item.m_shared.m_armor;
				}
				else if (item == this.m_helmetItem)
				{
					difference = 0f - item.m_shared.m_armor;
				}
				else
				{
					difference = this.GetBodyArmor() - this.m_helmetItem.m_shared.m_armor + item.m_shared.m_armor - this.GetBodyArmor();
				}
				return true;
			}
			if (itemType == ItemDrop.ItemData.ItemType.Chest)
			{
				if (this.m_chestItem == null)
				{
					difference = item.m_shared.m_armor;
				}
				else if (item == this.m_chestItem)
				{
					difference = 0f - item.m_shared.m_armor;
				}
				else
				{
					difference = this.GetBodyArmor() - this.m_chestItem.m_shared.m_armor + item.m_shared.m_armor - this.GetBodyArmor();
				}
				return true;
			}
		}
		else
		{
			if (itemType == ItemDrop.ItemData.ItemType.Legs)
			{
				if (this.m_legItem == null)
				{
					difference = item.m_shared.m_armor;
				}
				else if (item == this.m_legItem)
				{
					difference = 0f - item.m_shared.m_armor;
				}
				else
				{
					difference = this.GetBodyArmor() - this.m_legItem.m_shared.m_armor + item.m_shared.m_armor - this.GetBodyArmor();
				}
				return true;
			}
			if (itemType == ItemDrop.ItemData.ItemType.Shoulder)
			{
				if (this.m_shoulderItem == null)
				{
					difference = item.m_shared.m_armor;
				}
				else if (item == this.m_shoulderItem)
				{
					difference = 0f - item.m_shared.m_armor;
				}
				else
				{
					difference = this.GetBodyArmor() - this.m_shoulderItem.m_shared.m_armor + item.m_shared.m_armor - this.GetBodyArmor();
				}
				return true;
			}
		}
		difference = 0f;
		return false;
	}

	// Token: 0x06000416 RID: 1046 RVA: 0x00025BB4 File Offset: 0x00023DB4
	protected override void OnSneaking(float dt)
	{
		float t = Mathf.Pow(this.m_skills.GetSkillFactor(Skills.SkillType.Sneak), 0.5f);
		float num = Mathf.Lerp(1f, 0.25f, t);
		float num2 = dt * this.m_sneakStaminaDrain * num;
		num2 += num2 * this.GetEquipmentSneakStaminaModifier();
		this.m_seman.ModifySneakStaminaUsage(num2, ref num2, true);
		this.UseStamina(num2);
		if (!this.HaveStamina(0f))
		{
			Hud.instance.StaminaBarEmptyFlash();
		}
		this.m_sneakSkillImproveTimer += dt;
		if (this.m_sneakSkillImproveTimer > 1f)
		{
			this.m_sneakSkillImproveTimer = 0f;
			if (BaseAI.InStealthRange(this))
			{
				this.RaiseSkill(Skills.SkillType.Sneak, 1f);
				return;
			}
			this.RaiseSkill(Skills.SkillType.Sneak, 0.1f);
		}
	}

	// Token: 0x06000417 RID: 1047 RVA: 0x00025C78 File Offset: 0x00023E78
	private void UpdateStealth(float dt)
	{
		this.m_stealthFactorUpdateTimer += dt;
		if (this.m_stealthFactorUpdateTimer > 0.5f)
		{
			this.m_stealthFactorUpdateTimer = 0f;
			this.m_stealthFactorTarget = 0f;
			if (this.IsCrouching())
			{
				this.m_lastStealthPosition = base.transform.position;
				float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Sneak);
				float lightFactor = StealthSystem.instance.GetLightFactor(base.GetCenterPoint());
				this.m_stealthFactorTarget = Mathf.Lerp(0.5f + lightFactor * 0.5f, 0.2f + lightFactor * 0.4f, skillFactor);
				this.m_stealthFactorTarget = Mathf.Clamp01(this.m_stealthFactorTarget);
				this.m_seman.ModifyStealth(this.m_stealthFactorTarget, ref this.m_stealthFactorTarget);
				this.m_stealthFactorTarget = Mathf.Clamp01(this.m_stealthFactorTarget);
			}
			else
			{
				this.m_stealthFactorTarget = 1f;
			}
		}
		float num = Mathf.MoveTowards(this.m_stealthFactor, this.m_stealthFactorTarget, dt / 4f);
		if (!this.m_stealthFactor.Equals(num))
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_stealth, num);
		}
		this.m_stealthFactor = num;
	}

	// Token: 0x06000418 RID: 1048 RVA: 0x00025DA4 File Offset: 0x00023FA4
	public override float GetStealthFactor()
	{
		if (!this.m_nview.IsValid())
		{
			return 0f;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_stealthFactor;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_stealth, 0f);
	}

	// Token: 0x06000419 RID: 1049 RVA: 0x00025DF4 File Offset: 0x00023FF4
	public override bool InAttack()
	{
		if (MonoUpdaters.UpdateCount == this.m_cachedFrame)
		{
			return this.m_cachedAttack;
		}
		this.m_cachedFrame = MonoUpdaters.UpdateCount;
		if (base.GetNextOrCurrentAnimHash() == Humanoid.s_animatorTagAttack)
		{
			this.m_cachedAttack = true;
			return true;
		}
		for (int i = 1; i < this.m_animator.layerCount; i++)
		{
			if ((this.m_animator.IsInTransition(i) ? this.m_animator.GetNextAnimatorStateInfo(i).tagHash : this.m_animator.GetCurrentAnimatorStateInfo(i).tagHash) == Humanoid.s_animatorTagAttack)
			{
				this.m_cachedAttack = true;
				return true;
			}
		}
		this.m_cachedAttack = false;
		return false;
	}

	// Token: 0x0600041A RID: 1050 RVA: 0x00025E9C File Offset: 0x0002409C
	private float GetEquipmentModifier(int index)
	{
		if (this.m_equipmentModifierValues != null)
		{
			return this.m_equipmentModifierValues[index];
		}
		return 0f;
	}

	// Token: 0x0600041B RID: 1051 RVA: 0x00025EB4 File Offset: 0x000240B4
	public override float GetEquipmentMovementModifier()
	{
		return this.GetEquipmentModifier(0);
	}

	// Token: 0x0600041C RID: 1052 RVA: 0x00025EBD File Offset: 0x000240BD
	public override float GetEquipmentHomeItemModifier()
	{
		return this.GetEquipmentModifier(1);
	}

	// Token: 0x0600041D RID: 1053 RVA: 0x00025EC6 File Offset: 0x000240C6
	public override float GetEquipmentHeatResistanceModifier()
	{
		return this.GetEquipmentModifier(2);
	}

	// Token: 0x0600041E RID: 1054 RVA: 0x00025ECF File Offset: 0x000240CF
	public override float GetEquipmentJumpStaminaModifier()
	{
		return this.GetEquipmentModifier(3);
	}

	// Token: 0x0600041F RID: 1055 RVA: 0x00025ED8 File Offset: 0x000240D8
	public override float GetEquipmentAttackStaminaModifier()
	{
		return this.GetEquipmentModifier(4);
	}

	// Token: 0x06000420 RID: 1056 RVA: 0x00025EE1 File Offset: 0x000240E1
	public override float GetEquipmentBlockStaminaModifier()
	{
		return this.GetEquipmentModifier(5);
	}

	// Token: 0x06000421 RID: 1057 RVA: 0x00025EEA File Offset: 0x000240EA
	public override float GetEquipmentDodgeStaminaModifier()
	{
		return this.GetEquipmentModifier(6);
	}

	// Token: 0x06000422 RID: 1058 RVA: 0x00025EF3 File Offset: 0x000240F3
	public override float GetEquipmentSwimStaminaModifier()
	{
		return this.GetEquipmentModifier(7);
	}

	// Token: 0x06000423 RID: 1059 RVA: 0x00025EFC File Offset: 0x000240FC
	public override float GetEquipmentSneakStaminaModifier()
	{
		return this.GetEquipmentModifier(8);
	}

	// Token: 0x06000424 RID: 1060 RVA: 0x00025F05 File Offset: 0x00024105
	public override float GetEquipmentRunStaminaModifier()
	{
		return this.GetEquipmentModifier(9);
	}

	// Token: 0x06000425 RID: 1061 RVA: 0x00025F10 File Offset: 0x00024110
	private float GetEquipmentModifierPlusSE(int index)
	{
		float result = this.m_equipmentModifierValues[index];
		switch (index)
		{
		case 3:
			this.m_seman.ModifyJumpStaminaUsage(1f, ref result, false);
			break;
		case 4:
			this.m_seman.ModifyAttackStaminaUsage(1f, ref result, false);
			break;
		case 5:
			this.m_seman.ModifyBlockStaminaUsage(1f, ref result, false);
			break;
		case 6:
			this.m_seman.ModifyDodgeStaminaUsage(1f, ref result, false);
			break;
		case 7:
			this.m_seman.ModifySwimStaminaUsage(1f, ref result, false);
			break;
		case 8:
			this.m_seman.ModifySneakStaminaUsage(1f, ref result, false);
			break;
		case 9:
			this.m_seman.ModifyRunStaminaDrain(1f, ref result, Vector3.zero, false);
			break;
		}
		return result;
	}

	// Token: 0x06000426 RID: 1062 RVA: 0x00025FE9 File Offset: 0x000241E9
	protected override float GetJogSpeedFactor()
	{
		return 1f + this.GetEquipmentMovementModifier();
	}

	// Token: 0x06000427 RID: 1063 RVA: 0x00025FF8 File Offset: 0x000241F8
	protected override float GetRunSpeedFactor()
	{
		float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Run);
		return (1f + skillFactor * 0.25f) * (1f + this.GetEquipmentMovementModifier() * 1.5f);
	}

	// Token: 0x06000428 RID: 1064 RVA: 0x00026034 File Offset: 0x00024234
	public override bool InMinorAction()
	{
		int tagHash = this.m_animator.GetCurrentAnimatorStateInfo(1).tagHash;
		if (tagHash == Player.s_animatorTagMinorAction || tagHash == Player.s_animatorTagMinorActionFast)
		{
			return true;
		}
		if (this.m_animator.IsInTransition(1))
		{
			int tagHash2 = this.m_animator.GetNextAnimatorStateInfo(1).tagHash;
			return tagHash2 == Player.s_animatorTagMinorAction || tagHash2 == Player.s_animatorTagMinorActionFast;
		}
		return false;
	}

	// Token: 0x06000429 RID: 1065 RVA: 0x000260A0 File Offset: 0x000242A0
	public override bool InMinorActionSlowdown()
	{
		return this.m_animator.GetCurrentAnimatorStateInfo(1).tagHash == Player.s_animatorTagMinorAction || (this.m_animator.IsInTransition(1) && this.m_animator.GetNextAnimatorStateInfo(1).tagHash == Player.s_animatorTagMinorAction);
	}

	// Token: 0x0600042A RID: 1066 RVA: 0x000260F8 File Offset: 0x000242F8
	public override bool GetRelativePosition(out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Quaternion relativeRot, out Vector3 relativeVel)
	{
		if (this.m_attached && this.m_attachPoint)
		{
			ZNetView componentInParent = this.m_attachPoint.GetComponentInParent<ZNetView>();
			if (componentInParent && componentInParent.IsValid())
			{
				parent = componentInParent.GetZDO().m_uid;
				if (componentInParent.GetComponent<Character>() != null)
				{
					attachJoint = this.m_attachPoint.name;
					relativePos = Vector3.zero;
					relativeRot = Quaternion.identity;
				}
				else
				{
					attachJoint = "";
					relativePos = componentInParent.transform.InverseTransformPoint(base.transform.position);
					relativeRot = Quaternion.Inverse(componentInParent.transform.rotation) * base.transform.rotation;
				}
				relativeVel = Vector3.zero;
				return true;
			}
		}
		return base.GetRelativePosition(out parent, out attachJoint, out relativePos, out relativeRot, out relativeVel);
	}

	// Token: 0x0600042B RID: 1067 RVA: 0x000261EC File Offset: 0x000243EC
	public override Skills GetSkills()
	{
		return this.m_skills;
	}

	// Token: 0x0600042C RID: 1068 RVA: 0x000261F4 File Offset: 0x000243F4
	public override float GetRandomSkillFactor(Skills.SkillType skill)
	{
		return this.m_skills.GetRandomSkillFactor(skill);
	}

	// Token: 0x0600042D RID: 1069 RVA: 0x00026202 File Offset: 0x00024402
	public override float GetSkillFactor(Skills.SkillType skill)
	{
		return this.m_skills.GetSkillFactor(skill);
	}

	// Token: 0x0600042E RID: 1070 RVA: 0x00026210 File Offset: 0x00024410
	protected override void DoDamageCameraShake(HitData hit)
	{
		float totalStaggerDamage = hit.m_damage.GetTotalStaggerDamage();
		if (GameCamera.instance && totalStaggerDamage > 0f)
		{
			float num = Mathf.Clamp01(totalStaggerDamage / base.GetMaxHealth());
			GameCamera.instance.AddShake(base.transform.position, 50f, this.m_baseCameraShake * num, false);
		}
	}

	// Token: 0x0600042F RID: 1071 RVA: 0x00026270 File Offset: 0x00024470
	protected override void DamageArmorDurability(HitData hit)
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
		if (this.m_chestItem != null)
		{
			list.Add(this.m_chestItem);
		}
		if (this.m_legItem != null)
		{
			list.Add(this.m_legItem);
		}
		if (this.m_helmetItem != null)
		{
			list.Add(this.m_helmetItem);
		}
		if (this.m_shoulderItem != null)
		{
			list.Add(this.m_shoulderItem);
		}
		if (list.Count == 0)
		{
			return;
		}
		float num = hit.GetTotalPhysicalDamage() + hit.GetTotalElementalDamage();
		if (num <= 0f)
		{
			return;
		}
		int index = UnityEngine.Random.Range(0, list.Count);
		ItemDrop.ItemData itemData = list[index];
		itemData.m_durability = Mathf.Max(0f, itemData.m_durability - num);
	}

	// Token: 0x06000430 RID: 1072 RVA: 0x00026320 File Offset: 0x00024520
	protected override bool ToggleEquipped(ItemDrop.ItemData item)
	{
		if (!item.IsEquipable())
		{
			return false;
		}
		if (this.InAttack())
		{
			return true;
		}
		if (item.m_shared.m_equipDuration <= 0f)
		{
			if (base.IsItemEquiped(item))
			{
				base.UnequipItem(item, true);
			}
			else
			{
				base.EquipItem(item, true);
			}
		}
		else if (base.IsItemEquiped(item))
		{
			this.QueueUnequipAction(item);
		}
		else
		{
			this.QueueEquipAction(item);
		}
		return true;
	}

	// Token: 0x06000431 RID: 1073 RVA: 0x0002638C File Offset: 0x0002458C
	public void GetActionProgress(out string name, out float progress, out Player.MinorActionData data)
	{
		Player.MinorActionData minorActionData;
		float num;
		if (this.TryGetFirstElementProgress(out minorActionData, out num))
		{
			data = minorActionData;
			name = minorActionData.m_progressText;
			progress = num;
			return;
		}
		data = null;
		name = null;
		progress = 0f;
	}

	// Token: 0x06000432 RID: 1074 RVA: 0x000263C4 File Offset: 0x000245C4
	public void GetActionProgress(out string name, out float progress)
	{
		Player.MinorActionData minorActionData;
		this.GetActionProgress(out name, out progress, out minorActionData);
	}

	// Token: 0x06000433 RID: 1075 RVA: 0x000263DC File Offset: 0x000245DC
	private bool TryGetFirstElementProgress(out Player.MinorActionData firstElement, out float progress)
	{
		firstElement = null;
		progress = 0f;
		if (this.m_actionQueue.Count > 0)
		{
			firstElement = this.m_actionQueue[0];
			if (firstElement.m_duration > 0f)
			{
				progress = Mathf.Clamp01(firstElement.m_time / firstElement.m_duration);
			}
			return true;
		}
		return false;
	}

	// Token: 0x06000434 RID: 1076 RVA: 0x00026436 File Offset: 0x00024636
	public int GetActionQueueCount()
	{
		return this.m_actionQueue.Count;
	}

	// Token: 0x06000435 RID: 1077 RVA: 0x00026444 File Offset: 0x00024644
	private void UpdateActionQueue(float dt)
	{
		if (this.m_actionQueuePause > 0f)
		{
			this.m_actionQueuePause -= dt;
			if (this.m_actionAnimation != null)
			{
				this.m_zanim.SetBool(this.m_actionAnimation, false);
				this.m_actionAnimation = null;
			}
			return;
		}
		if (this.InAttack())
		{
			if (this.m_actionAnimation != null)
			{
				this.m_zanim.SetBool(this.m_actionAnimation, false);
				this.m_actionAnimation = null;
			}
			return;
		}
		if (this.m_actionQueue.Count == 0)
		{
			if (this.m_actionAnimation != null)
			{
				this.m_zanim.SetBool(this.m_actionAnimation, false);
				this.m_actionAnimation = null;
			}
			return;
		}
		Player.MinorActionData minorActionData = this.m_actionQueue[0];
		if (this.m_actionAnimation != null && this.m_actionAnimation != minorActionData.m_animation)
		{
			this.m_zanim.SetBool(this.m_actionAnimation, false);
			this.m_actionAnimation = null;
		}
		this.m_zanim.SetBool(minorActionData.m_animation, true);
		this.m_actionAnimation = minorActionData.m_animation;
		if (minorActionData.m_time == 0f && minorActionData.m_startEffect != null)
		{
			minorActionData.m_startEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		}
		if (minorActionData.m_staminaDrain > 0f)
		{
			this.UseStamina(minorActionData.m_staminaDrain * dt);
		}
		if (minorActionData.m_eitrDrain > 0f)
		{
			this.UseEitr(minorActionData.m_eitrDrain * dt);
		}
		minorActionData.m_time += dt;
		if (minorActionData.m_time > minorActionData.m_duration)
		{
			this.m_actionQueue.RemoveAt(0);
			this.m_zanim.SetBool(this.m_actionAnimation, false);
			this.m_actionAnimation = null;
			if (!string.IsNullOrEmpty(minorActionData.m_doneAnimation))
			{
				this.m_zanim.SetTrigger(minorActionData.m_doneAnimation);
			}
			switch (minorActionData.m_type)
			{
			case Player.MinorActionData.ActionType.Equip:
				base.EquipItem(minorActionData.m_item, true);
				break;
			case Player.MinorActionData.ActionType.Unequip:
				base.UnequipItem(minorActionData.m_item, true);
				break;
			case Player.MinorActionData.ActionType.Reload:
				this.SetWeaponLoaded(minorActionData.m_item);
				break;
			}
			this.m_actionQueuePause = 0.3f;
		}
	}

	// Token: 0x06000436 RID: 1078 RVA: 0x00026668 File Offset: 0x00024868
	private void QueueEquipAction(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return;
		}
		if (this.IsEquipActionQueued(item))
		{
			this.RemoveEquipAction(item);
			return;
		}
		this.CancelReloadAction();
		Player.MinorActionData minorActionData = new Player.MinorActionData();
		minorActionData.m_item = item;
		minorActionData.m_type = Player.MinorActionData.ActionType.Equip;
		minorActionData.m_duration = item.m_shared.m_equipDuration;
		minorActionData.m_progressText = "$hud_equipping " + item.m_shared.m_name;
		minorActionData.m_animation = "equipping";
		if (minorActionData.m_duration >= 1f)
		{
			minorActionData.m_startEffect = this.m_equipStartEffects;
		}
		this.m_actionQueue.Add(minorActionData);
	}

	// Token: 0x06000437 RID: 1079 RVA: 0x00026700 File Offset: 0x00024900
	private void QueueUnequipAction(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return;
		}
		if (this.IsEquipActionQueued(item))
		{
			this.RemoveEquipAction(item);
			return;
		}
		this.CancelReloadAction();
		Player.MinorActionData minorActionData = new Player.MinorActionData();
		minorActionData.m_item = item;
		minorActionData.m_type = Player.MinorActionData.ActionType.Unequip;
		minorActionData.m_duration = item.m_shared.m_equipDuration;
		minorActionData.m_progressText = "$hud_unequipping " + item.m_shared.m_name;
		minorActionData.m_animation = "equipping";
		this.m_actionQueue.Add(minorActionData);
	}

	// Token: 0x06000438 RID: 1080 RVA: 0x00026780 File Offset: 0x00024980
	private void QueueReloadAction()
	{
		if (this.IsReloadActionQueued())
		{
			return;
		}
		ItemDrop.ItemData currentWeapon = base.GetCurrentWeapon();
		if (currentWeapon == null || !currentWeapon.m_shared.m_attack.m_requiresReload)
		{
			return;
		}
		Player.MinorActionData minorActionData = new Player.MinorActionData();
		minorActionData.m_item = currentWeapon;
		minorActionData.m_type = Player.MinorActionData.ActionType.Reload;
		minorActionData.m_duration = currentWeapon.GetWeaponLoadingTime();
		minorActionData.m_progressText = "$hud_reloading " + currentWeapon.m_shared.m_name;
		minorActionData.m_animation = currentWeapon.m_shared.m_attack.m_reloadAnimation;
		minorActionData.m_doneAnimation = currentWeapon.m_shared.m_attack.m_reloadAnimation + "_done";
		minorActionData.m_staminaDrain = currentWeapon.m_shared.m_attack.m_reloadStaminaDrain;
		minorActionData.m_eitrDrain = currentWeapon.m_shared.m_attack.m_reloadEitrDrain;
		this.m_actionQueue.Add(minorActionData);
	}

	// Token: 0x06000439 RID: 1081 RVA: 0x0002685C File Offset: 0x00024A5C
	protected override void ClearActionQueue()
	{
		this.m_actionQueue.Clear();
	}

	// Token: 0x0600043A RID: 1082 RVA: 0x0002686C File Offset: 0x00024A6C
	public override void RemoveEquipAction(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return;
		}
		foreach (Player.MinorActionData minorActionData in this.m_actionQueue)
		{
			if (minorActionData.m_item == item)
			{
				this.m_actionQueue.Remove(minorActionData);
				break;
			}
		}
	}

	// Token: 0x0600043B RID: 1083 RVA: 0x000268D4 File Offset: 0x00024AD4
	public bool IsEquipActionQueued(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return false;
		}
		foreach (Player.MinorActionData minorActionData in this.m_actionQueue)
		{
			if ((minorActionData.m_type == Player.MinorActionData.ActionType.Equip || minorActionData.m_type == Player.MinorActionData.ActionType.Unequip) && minorActionData.m_item == item)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x0600043C RID: 1084 RVA: 0x00026948 File Offset: 0x00024B48
	private bool IsReloadActionQueued()
	{
		using (List<Player.MinorActionData>.Enumerator enumerator = this.m_actionQueue.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_type == Player.MinorActionData.ActionType.Reload)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x0600043D RID: 1085 RVA: 0x000269A4 File Offset: 0x00024BA4
	public void ResetCharacter()
	{
		this.m_guardianPowerCooldown = 0f;
		Player.ResetSeenTutorials();
		this.m_knownRecipes.Clear();
		this.m_knownStations.Clear();
		this.m_knownMaterial.Clear();
		this.m_uniques.Clear();
		this.m_trophies.Clear();
		this.m_skills.Clear();
		this.m_knownBiome.Clear();
		this.m_knownTexts.Clear();
	}

	// Token: 0x0600043E RID: 1086 RVA: 0x00026A19 File Offset: 0x00024C19
	public void ResetCharacterKnownItems()
	{
		this.m_knownRecipes.Clear();
		this.m_knownStations.Clear();
		this.m_knownMaterial.Clear();
		this.m_trophies.Clear();
	}

	// Token: 0x0600043F RID: 1087 RVA: 0x00026A48 File Offset: 0x00024C48
	public bool ToggleDebugFly()
	{
		this.m_debugFly = !this.m_debugFly;
		this.m_nview.GetZDO().Set(ZDOVars.s_debugFly, this.m_debugFly);
		this.Message(MessageHud.MessageType.TopLeft, "Debug fly:" + this.m_debugFly.ToString(), 0, null);
		return this.m_debugFly;
	}

	// Token: 0x06000440 RID: 1088 RVA: 0x00026AA3 File Offset: 0x00024CA3
	public void SetNoPlacementCost(bool value)
	{
		if (value != this.m_noPlacementCost)
		{
			this.ToggleNoPlacementCost();
		}
	}

	// Token: 0x06000441 RID: 1089 RVA: 0x00026AB5 File Offset: 0x00024CB5
	public bool ToggleNoPlacementCost()
	{
		this.m_noPlacementCost = !this.m_noPlacementCost;
		this.Message(MessageHud.MessageType.TopLeft, "No placement cost:" + this.m_noPlacementCost.ToString(), 0, null);
		this.UpdateAvailablePiecesList();
		return this.m_noPlacementCost;
	}

	// Token: 0x06000442 RID: 1090 RVA: 0x00026AF0 File Offset: 0x00024CF0
	public bool IsKnownMaterial(string name)
	{
		return this.m_knownMaterial.Contains(name);
	}

	// Token: 0x1700000D RID: 13
	// (get) Token: 0x06000443 RID: 1091 RVA: 0x00026AFE File Offset: 0x00024CFE
	public bool AlternativePlacementActive
	{
		get
		{
			return this.m_altPlace;
		}
	}

	// Token: 0x1700000E RID: 14
	// (get) Token: 0x06000444 RID: 1092 RVA: 0x00026B06 File Offset: 0x00024D06
	public SeasonalItemGroup CurrentSeason
	{
		get
		{
			return this.m_currentSeason;
		}
	}

	// Token: 0x04000402 RID: 1026
	private Vector3 m_lastDistCheck;

	// Token: 0x04000403 RID: 1027
	private float m_statCheck;

	// Token: 0x04000404 RID: 1028
	[Header("Effects")]
	public EffectList m_buttonEffects = new EffectList();

	// Token: 0x04000405 RID: 1029
	private List<string> m_readyEvents = new List<string>();

	// Token: 0x04000406 RID: 1030
	private static List<IPlaced> m_placed = new List<IPlaced>();

	// Token: 0x04000407 RID: 1031
	public static string LastEmote;

	// Token: 0x04000408 RID: 1032
	public static DateTime LastEmoteTime;

	// Token: 0x04000409 RID: 1033
	private float[] m_equipmentModifierValues;

	// Token: 0x0400040A RID: 1034
	private static FieldInfo[] s_equipmentModifierSourceFields;

	// Token: 0x0400040B RID: 1035
	private static readonly string[] s_equipmentModifierSources = new string[]
	{
		"m_movementModifier",
		"m_homeItemsStaminaModifier",
		"m_heatResistanceModifier",
		"m_jumpStaminaModifier",
		"m_attackStaminaModifier",
		"m_blockStaminaModifier",
		"m_dodgeStaminaModifier",
		"m_swimStaminaModifier",
		"m_sneakStaminaModifier",
		"m_runStaminaModifier"
	};

	// Token: 0x0400040C RID: 1036
	private static readonly string[] s_equipmentModifierTooltips = new string[]
	{
		"$item_movement_modifier",
		"$base_item_modifier",
		"$item_heat_modifier",
		"$se_jumpstamina",
		"$se_attackstamina",
		"$se_blockstamina",
		"$se_dodgestamina",
		"$se_swimstamina",
		"$se_sneakstamina",
		"$se_runstamina"
	};

	// Token: 0x0400040D RID: 1037
	private float m_baseValueUpdateTimer;

	// Token: 0x0400040E RID: 1038
	private float m_rotatePieceTimer;

	// Token: 0x0400040F RID: 1039
	private float m_rotatePieceTimeSince;

	// Token: 0x04000410 RID: 1040
	private float m_scrollCurrAmount;

	// Token: 0x04000411 RID: 1041
	private bool m_altPlace;

	// Token: 0x04000412 RID: 1042
	public static Player m_localPlayer = null;

	// Token: 0x04000413 RID: 1043
	private static readonly List<Player> s_players = new List<Player>();

	// Token: 0x04000414 RID: 1044
	public static List<string> m_addUniqueKeyQueue = new List<string>();

	// Token: 0x04000415 RID: 1045
	public static List<string> s_FilterCraft = new List<string>();

	// Token: 0x04000416 RID: 1046
	public static bool m_debugMode = false;

	// Token: 0x04000417 RID: 1047
	[Header("Player")]
	public float m_maxPlaceDistance = 5f;

	// Token: 0x04000418 RID: 1048
	public float m_maxInteractDistance = 5f;

	// Token: 0x04000419 RID: 1049
	public float m_scrollSens = 4f;

	// Token: 0x0400041A RID: 1050
	public float m_staminaRegen = 5f;

	// Token: 0x0400041B RID: 1051
	public float m_staminaRegenTimeMultiplier = 1f;

	// Token: 0x0400041C RID: 1052
	public float m_staminaRegenDelay = 1f;

	// Token: 0x0400041D RID: 1053
	public float m_runStaminaDrain = 10f;

	// Token: 0x0400041E RID: 1054
	public float m_sneakStaminaDrain = 5f;

	// Token: 0x0400041F RID: 1055
	public float m_swimStaminaDrainMinSkill = 5f;

	// Token: 0x04000420 RID: 1056
	public float m_swimStaminaDrainMaxSkill = 2f;

	// Token: 0x04000421 RID: 1057
	public float m_dodgeStaminaUsage = 10f;

	// Token: 0x04000422 RID: 1058
	public float m_weightStaminaFactor = 0.1f;

	// Token: 0x04000423 RID: 1059
	public float m_eiterRegen = 5f;

	// Token: 0x04000424 RID: 1060
	public float m_eitrRegenDelay = 1f;

	// Token: 0x04000425 RID: 1061
	public float m_autoPickupRange = 2f;

	// Token: 0x04000426 RID: 1062
	public float m_maxCarryWeight = 300f;

	// Token: 0x04000427 RID: 1063
	public float m_encumberedStaminaDrain = 10f;

	// Token: 0x04000428 RID: 1064
	public float m_hardDeathCooldown = 10f;

	// Token: 0x04000429 RID: 1065
	public float m_baseCameraShake = 4f;

	// Token: 0x0400042A RID: 1066
	public float m_placeDelay = 0.4f;

	// Token: 0x0400042B RID: 1067
	public float m_removeDelay = 0.25f;

	// Token: 0x0400042C RID: 1068
	public EffectList m_drownEffects = new EffectList();

	// Token: 0x0400042D RID: 1069
	public EffectList m_spawnEffects = new EffectList();

	// Token: 0x0400042E RID: 1070
	public EffectList m_removeEffects = new EffectList();

	// Token: 0x0400042F RID: 1071
	public EffectList m_dodgeEffects = new EffectList();

	// Token: 0x04000430 RID: 1072
	public EffectList m_autopickupEffects = new EffectList();

	// Token: 0x04000431 RID: 1073
	public EffectList m_skillLevelupEffects = new EffectList();

	// Token: 0x04000432 RID: 1074
	public EffectList m_equipStartEffects = new EffectList();

	// Token: 0x04000433 RID: 1075
	public GameObject m_placeMarker;

	// Token: 0x04000434 RID: 1076
	public GameObject m_tombstone;

	// Token: 0x04000435 RID: 1077
	public SoftReference<GameObject> m_valkyrie;

	// Token: 0x04000436 RID: 1078
	public Sprite m_textIcon;

	// Token: 0x04000437 RID: 1079
	public float m_baseHP = 25f;

	// Token: 0x04000438 RID: 1080
	public float m_baseStamina = 75f;

	// Token: 0x04000439 RID: 1081
	public double m_wakeupTime;

	// Token: 0x0400043A RID: 1082
	private Skills m_skills;

	// Token: 0x0400043B RID: 1083
	private PieceTable m_buildPieces;

	// Token: 0x0400043C RID: 1084
	private bool m_noPlacementCost;

	// Token: 0x0400043D RID: 1085
	private const bool m_hideUnavailable = false;

	// Token: 0x0400043E RID: 1086
	private static bool m_enableAutoPickup = true;

	// Token: 0x0400043F RID: 1087
	private readonly HashSet<string> m_knownRecipes = new HashSet<string>();

	// Token: 0x04000440 RID: 1088
	private readonly Dictionary<string, int> m_knownStations = new Dictionary<string, int>();

	// Token: 0x04000441 RID: 1089
	private readonly HashSet<string> m_knownMaterial = new HashSet<string>();

	// Token: 0x04000442 RID: 1090
	private readonly HashSet<string> m_shownTutorials = new HashSet<string>();

	// Token: 0x04000443 RID: 1091
	private readonly HashSet<string> m_uniques = new HashSet<string>();

	// Token: 0x04000444 RID: 1092
	private readonly HashSet<string> m_trophies = new HashSet<string>();

	// Token: 0x04000445 RID: 1093
	private readonly HashSet<Heightmap.Biome> m_knownBiome = new HashSet<Heightmap.Biome>();

	// Token: 0x04000446 RID: 1094
	private readonly Dictionary<string, string> m_knownTexts = new Dictionary<string, string>();

	// Token: 0x04000447 RID: 1095
	private float m_stationDiscoverTimer;

	// Token: 0x04000448 RID: 1096
	private bool m_debugFly;

	// Token: 0x04000449 RID: 1097
	private bool m_godMode;

	// Token: 0x0400044A RID: 1098
	private bool m_ghostMode;

	// Token: 0x0400044B RID: 1099
	private float m_lookPitch;

	// Token: 0x0400044C RID: 1100
	private const int m_maxFoods = 3;

	// Token: 0x0400044D RID: 1101
	private const float m_foodDrainPerSec = 0.1f;

	// Token: 0x0400044E RID: 1102
	private float m_foodUpdateTimer;

	// Token: 0x0400044F RID: 1103
	private float m_foodRegenTimer;

	// Token: 0x04000450 RID: 1104
	private readonly List<Player.Food> m_foods = new List<Player.Food>();

	// Token: 0x04000451 RID: 1105
	private float m_stamina = 100f;

	// Token: 0x04000452 RID: 1106
	private float m_maxStamina = 100f;

	// Token: 0x04000453 RID: 1107
	private float m_staminaRegenTimer;

	// Token: 0x04000454 RID: 1108
	private float m_eitr;

	// Token: 0x04000455 RID: 1109
	private float m_maxEitr;

	// Token: 0x04000456 RID: 1110
	private float m_eitrRegenTimer;

	// Token: 0x04000457 RID: 1111
	private string m_guardianPower = "";

	// Token: 0x04000458 RID: 1112
	private int m_guardianPowerHash;

	// Token: 0x04000459 RID: 1113
	public float m_guardianPowerCooldown;

	// Token: 0x0400045A RID: 1114
	private StatusEffect m_guardianSE;

	// Token: 0x0400045B RID: 1115
	private float m_placePressedTime = -1000f;

	// Token: 0x0400045C RID: 1116
	private float m_removePressedTime = -1000f;

	// Token: 0x0400045D RID: 1117
	private bool m_blockRemove;

	// Token: 0x0400045E RID: 1118
	private float m_lastToolUseTime;

	// Token: 0x0400045F RID: 1119
	private GameObject m_placementMarkerInstance;

	// Token: 0x04000460 RID: 1120
	private GameObject m_placementGhost;

	// Token: 0x04000461 RID: 1121
	private string m_placementGhostLast;

	// Token: 0x04000462 RID: 1122
	private Player.PlacementStatus m_placementStatus = Player.PlacementStatus.Invalid;

	// Token: 0x04000463 RID: 1123
	private float m_placeRotationDegrees = 22.5f;

	// Token: 0x04000464 RID: 1124
	private int m_placeRotation;

	// Token: 0x04000465 RID: 1125
	public float m_scrollAmountThreshold = 0.1f;

	// Token: 0x04000466 RID: 1126
	private int m_buildRemoveDebt;

	// Token: 0x04000467 RID: 1127
	private int m_placeRayMask;

	// Token: 0x04000468 RID: 1128
	private int m_placeGroundRayMask;

	// Token: 0x04000469 RID: 1129
	private int m_placeWaterRayMask;

	// Token: 0x0400046A RID: 1130
	private int m_removeRayMask;

	// Token: 0x0400046B RID: 1131
	private int m_interactMask;

	// Token: 0x0400046C RID: 1132
	private int m_autoPickupMask;

	// Token: 0x0400046D RID: 1133
	private readonly List<Player.MinorActionData> m_actionQueue = new List<Player.MinorActionData>();

	// Token: 0x0400046E RID: 1134
	private float m_actionQueuePause;

	// Token: 0x0400046F RID: 1135
	private string m_actionAnimation;

	// Token: 0x04000470 RID: 1136
	private GameObject m_hovering;

	// Token: 0x04000471 RID: 1137
	private Character m_hoveringCreature;

	// Token: 0x04000472 RID: 1138
	private float m_lastHoverInteractTime;

	// Token: 0x04000473 RID: 1139
	private bool m_pvp;

	// Token: 0x04000474 RID: 1140
	private float m_updateCoverTimer;

	// Token: 0x04000475 RID: 1141
	private float m_coverPercentage;

	// Token: 0x04000476 RID: 1142
	private bool m_underRoof = true;

	// Token: 0x04000477 RID: 1143
	private float m_nearFireTimer;

	// Token: 0x04000478 RID: 1144
	private bool m_isLoading;

	// Token: 0x04000479 RID: 1145
	private ItemDrop.ItemData m_weaponLoaded;

	// Token: 0x0400047A RID: 1146
	private float m_queuedAttackTimer;

	// Token: 0x0400047B RID: 1147
	private float m_queuedSecondAttackTimer;

	// Token: 0x0400047C RID: 1148
	private float m_queuedDodgeTimer;

	// Token: 0x0400047D RID: 1149
	private Vector3 m_queuedDodgeDir = Vector3.zero;

	// Token: 0x0400047E RID: 1150
	private bool m_inDodge;

	// Token: 0x0400047F RID: 1151
	private bool m_dodgeInvincible;

	// Token: 0x04000480 RID: 1152
	private CraftingStation m_currentStation;

	// Token: 0x04000481 RID: 1153
	private bool m_inCraftingStation;

	// Token: 0x04000482 RID: 1154
	private Ragdoll m_ragdoll;

	// Token: 0x04000483 RID: 1155
	private Piece m_hoveringPiece;

	// Token: 0x04000484 RID: 1156
	private Dictionary<Material, float> m_ghostRippleDistance = new Dictionary<Material, float>();

	// Token: 0x04000485 RID: 1157
	private bool m_attackTowardsPlayerLookDir;

	// Token: 0x04000486 RID: 1158
	private string m_emoteState = "";

	// Token: 0x04000487 RID: 1159
	private int m_emoteID;

	// Token: 0x04000488 RID: 1160
	private bool m_intro;

	// Token: 0x04000489 RID: 1161
	private bool m_crouchToggled;

	// Token: 0x0400048A RID: 1162
	public bool m_autoRun;

	// Token: 0x0400048B RID: 1163
	private bool m_safeInHome;

	// Token: 0x0400048C RID: 1164
	private IDoodadController m_doodadController;

	// Token: 0x0400048D RID: 1165
	private bool m_attached;

	// Token: 0x0400048E RID: 1166
	private string m_attachAnimation = "";

	// Token: 0x0400048F RID: 1167
	private bool m_sleeping;

	// Token: 0x04000490 RID: 1168
	private bool m_attachedToShip;

	// Token: 0x04000491 RID: 1169
	private Transform m_attachPoint;

	// Token: 0x04000492 RID: 1170
	private Vector3 m_detachOffset = Vector3.zero;

	// Token: 0x04000493 RID: 1171
	private Transform m_attachPointCamera;

	// Token: 0x04000494 RID: 1172
	private Collider[] m_attachColliders;

	// Token: 0x04000495 RID: 1173
	private int m_modelIndex;

	// Token: 0x04000496 RID: 1174
	private Vector3 m_skinColor = Vector3.one;

	// Token: 0x04000497 RID: 1175
	private Vector3 m_hairColor = Vector3.one;

	// Token: 0x04000498 RID: 1176
	private bool m_teleporting;

	// Token: 0x04000499 RID: 1177
	private bool m_distantTeleport;

	// Token: 0x0400049A RID: 1178
	private float m_teleportTimer;

	// Token: 0x0400049B RID: 1179
	private float m_teleportCooldown;

	// Token: 0x0400049C RID: 1180
	private Vector3 m_teleportFromPos;

	// Token: 0x0400049D RID: 1181
	private Quaternion m_teleportFromRot;

	// Token: 0x0400049E RID: 1182
	private Vector3 m_teleportTargetPos;

	// Token: 0x0400049F RID: 1183
	private Quaternion m_teleportTargetRot;

	// Token: 0x040004A0 RID: 1184
	private Heightmap.Biome m_currentBiome;

	// Token: 0x040004A1 RID: 1185
	private float m_biomeTimer;

	// Token: 0x040004A2 RID: 1186
	private List<string> m_tempUniqueKeys = new List<string>();

	// Token: 0x040004A3 RID: 1187
	private int m_baseValue;

	// Token: 0x040004A4 RID: 1188
	private int m_baseValueOld = -1;

	// Token: 0x040004A5 RID: 1189
	private int m_comfortLevel;

	// Token: 0x040004A6 RID: 1190
	private float m_drownDamageTimer;

	// Token: 0x040004A7 RID: 1191
	private float m_timeSinceTargeted;

	// Token: 0x040004A8 RID: 1192
	private float m_timeSinceSensed;

	// Token: 0x040004A9 RID: 1193
	private float m_stealthFactorUpdateTimer;

	// Token: 0x040004AA RID: 1194
	private float m_stealthFactor;

	// Token: 0x040004AB RID: 1195
	private float m_stealthFactorTarget;

	// Token: 0x040004AC RID: 1196
	private Vector3 m_lastStealthPosition = Vector3.zero;

	// Token: 0x040004AD RID: 1197
	private float m_lastVelocity;

	// Token: 0x040004AE RID: 1198
	private float m_wakeupTimer = -1f;

	// Token: 0x040004AF RID: 1199
	private float m_timeSinceDeath = 999999f;

	// Token: 0x040004B0 RID: 1200
	private float m_runSkillImproveTimer;

	// Token: 0x040004B1 RID: 1201
	private float m_swimSkillImproveTimer;

	// Token: 0x040004B2 RID: 1202
	private float m_sneakSkillImproveTimer;

	// Token: 0x040004B3 RID: 1203
	private int m_manualSnapPoint = -1;

	// Token: 0x040004B4 RID: 1204
	private readonly List<PieceTable> m_tempOwnedPieceTables = new List<PieceTable>();

	// Token: 0x040004B5 RID: 1205
	private readonly List<Transform> m_tempSnapPoints1 = new List<Transform>();

	// Token: 0x040004B6 RID: 1206
	private readonly List<Transform> m_tempSnapPoints2 = new List<Transform>();

	// Token: 0x040004B7 RID: 1207
	private readonly List<Piece> m_tempPieces = new List<Piece>();

	// Token: 0x040004B8 RID: 1208
	[HideInInspector]
	public Dictionary<string, string> m_customData = new Dictionary<string, string>();

	// Token: 0x040004B9 RID: 1209
	private static int s_attackMask = 0;

	// Token: 0x040004BA RID: 1210
	private static readonly int s_crouching = ZSyncAnimation.GetHash("crouching");

	// Token: 0x040004BB RID: 1211
	private static readonly int s_animatorTagDodge = ZSyncAnimation.GetHash("dodge");

	// Token: 0x040004BC RID: 1212
	private static readonly int s_animatorTagCutscene = ZSyncAnimation.GetHash("cutscene");

	// Token: 0x040004BD RID: 1213
	private static readonly int s_animatorTagCrouch = ZSyncAnimation.GetHash("crouch");

	// Token: 0x040004BE RID: 1214
	private static readonly int s_animatorTagMinorAction = ZSyncAnimation.GetHash("minoraction");

	// Token: 0x040004BF RID: 1215
	private static readonly int s_animatorTagMinorActionFast = ZSyncAnimation.GetHash("minoraction_fast");

	// Token: 0x040004C0 RID: 1216
	private static readonly int s_animatorTagEmote = ZSyncAnimation.GetHash("emote");

	// Token: 0x040004C1 RID: 1217
	public const string BaseValueKey = "baseValue";

	// Token: 0x040004C2 RID: 1218
	private int m_cachedFrame;

	// Token: 0x040004C3 RID: 1219
	private bool m_cachedAttack;

	// Token: 0x040004C4 RID: 1220
	private bool m_dodgeInvincibleCached;

	// Token: 0x040004C5 RID: 1221
	[Header("Seasonal Items")]
	[SerializeField]
	private List<SeasonalItemGroup> m_seasonalItemGroups = new List<SeasonalItemGroup>();

	// Token: 0x040004C6 RID: 1222
	private SeasonalItemGroup m_currentSeason;

	// Token: 0x040004C7 RID: 1223
	private readonly RaycastHit[] m_raycastHoverHits = new RaycastHit[64];

	// Token: 0x02000239 RID: 569
	public enum RequirementMode
	{
		// Token: 0x04001F98 RID: 8088
		CanBuild,
		// Token: 0x04001F99 RID: 8089
		IsKnown,
		// Token: 0x04001F9A RID: 8090
		CanAlmostBuild
	}

	// Token: 0x0200023A RID: 570
	public class Food
	{
		// Token: 0x06001EDE RID: 7902 RVA: 0x000E19A6 File Offset: 0x000DFBA6
		public bool CanEatAgain()
		{
			return this.m_time < this.m_item.m_shared.m_foodBurnTime / 2f;
		}

		// Token: 0x04001F9B RID: 8091
		public string m_name = "";

		// Token: 0x04001F9C RID: 8092
		public ItemDrop.ItemData m_item;

		// Token: 0x04001F9D RID: 8093
		public float m_time;

		// Token: 0x04001F9E RID: 8094
		public float m_health;

		// Token: 0x04001F9F RID: 8095
		public float m_stamina;

		// Token: 0x04001FA0 RID: 8096
		public float m_eitr;
	}

	// Token: 0x0200023B RID: 571
	public class MinorActionData
	{
		// Token: 0x04001FA1 RID: 8097
		public Player.MinorActionData.ActionType m_type;

		// Token: 0x04001FA2 RID: 8098
		public ItemDrop.ItemData m_item;

		// Token: 0x04001FA3 RID: 8099
		public string m_progressText = "";

		// Token: 0x04001FA4 RID: 8100
		public float m_time;

		// Token: 0x04001FA5 RID: 8101
		public float m_duration;

		// Token: 0x04001FA6 RID: 8102
		public string m_animation = "";

		// Token: 0x04001FA7 RID: 8103
		public string m_doneAnimation = "";

		// Token: 0x04001FA8 RID: 8104
		public float m_staminaDrain;

		// Token: 0x04001FA9 RID: 8105
		public float m_eitrDrain;

		// Token: 0x04001FAA RID: 8106
		public EffectList m_startEffect;

		// Token: 0x020003D9 RID: 985
		public enum ActionType
		{
			// Token: 0x0400276F RID: 10095
			Equip,
			// Token: 0x04002770 RID: 10096
			Unequip,
			// Token: 0x04002771 RID: 10097
			Reload
		}
	}

	// Token: 0x0200023C RID: 572
	public enum PlacementStatus
	{
		// Token: 0x04001FAC RID: 8108
		Valid,
		// Token: 0x04001FAD RID: 8109
		Invalid,
		// Token: 0x04001FAE RID: 8110
		BlockedbyPlayer,
		// Token: 0x04001FAF RID: 8111
		NoBuildZone,
		// Token: 0x04001FB0 RID: 8112
		PrivateZone,
		// Token: 0x04001FB1 RID: 8113
		MoreSpace,
		// Token: 0x04001FB2 RID: 8114
		NoTeleportArea,
		// Token: 0x04001FB3 RID: 8115
		ExtensionMissingStation,
		// Token: 0x04001FB4 RID: 8116
		WrongBiome,
		// Token: 0x04001FB5 RID: 8117
		NeedCultivated,
		// Token: 0x04001FB6 RID: 8118
		NeedDirt,
		// Token: 0x04001FB7 RID: 8119
		NotInDungeon,
		// Token: 0x04001FB8 RID: 8120
		NoRayHits
	}

	// Token: 0x0200023D RID: 573
	private class RaycastHitComparer : IComparer<RaycastHit>
	{
		// Token: 0x06001EE1 RID: 7905 RVA: 0x000E1A04 File Offset: 0x000DFC04
		public int Compare(RaycastHit x, RaycastHit y)
		{
			return x.distance.CompareTo(y.distance);
		}

		// Token: 0x04001FB9 RID: 8121
		public static Player.RaycastHitComparer Instance = new Player.RaycastHitComparer();
	}
}
