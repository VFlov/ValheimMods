using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.UI;

// Token: 0x02000078 RID: 120
public class Hud : MonoBehaviour
{
	// Token: 0x0600078B RID: 1931 RVA: 0x00040C54 File Offset: 0x0003EE54
	private void OnDestroy()
	{
		Hud.m_instance = null;
		PlayerProfile.SavingStarted = (Action)Delegate.Remove(PlayerProfile.SavingStarted, new Action(this.ProfileSaveStarted));
		PlayerProfile.SavingFinished = (Action)Delegate.Remove(PlayerProfile.SavingFinished, new Action(this.ProfileSaveFinished));
		ZNet.WorldSaveStarted = (Action)Delegate.Remove(ZNet.WorldSaveStarted, new Action(this.WorldSaveStarted));
		ZNet.WorldSaveFinished = (Action)Delegate.Remove(ZNet.WorldSaveFinished, new Action(this.WorldSaveFinished));
	}

	// Token: 0x1700002A RID: 42
	// (get) Token: 0x0600078C RID: 1932 RVA: 0x00040CE7 File Offset: 0x0003EEE7
	public static Hud instance
	{
		get
		{
			return Hud.m_instance;
		}
	}

	// Token: 0x0600078D RID: 1933 RVA: 0x00040CF0 File Offset: 0x0003EEF0
	private void Awake()
	{
		Hud.m_instance = this;
		this.m_pieceSelectionWindow.SetActive(false);
		this.m_loadingScreen.gameObject.SetActive(false);
		this.m_statusEffectTemplate.gameObject.SetActive(false);
		this.m_eventBar.SetActive(false);
		this.m_gpRoot.gameObject.SetActive(false);
		this.m_betaText.SetActive(false);
		UIInputHandler closePieceSelectionButton = this.m_closePieceSelectionButton;
		closePieceSelectionButton.m_onLeftClick = (Action<UIInputHandler>)Delegate.Combine(closePieceSelectionButton.m_onLeftClick, new Action<UIInputHandler>(this.OnClosePieceSelection));
		UIInputHandler closePieceSelectionButton2 = this.m_closePieceSelectionButton;
		closePieceSelectionButton2.m_onRightClick = (Action<UIInputHandler>)Delegate.Combine(closePieceSelectionButton2.m_onRightClick, new Action<UIInputHandler>(this.OnClosePieceSelection));
		if (SteamManager.APP_ID == 1223920U)
		{
			this.m_betaText.SetActive(true);
		}
		GameObject[] pieceCategoryTabs = this.m_pieceCategoryTabs;
		for (int i = 0; i < pieceCategoryTabs.Length; i++)
		{
			UIInputHandler component = pieceCategoryTabs[i].GetComponent<UIInputHandler>();
			component.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftDown, new Action<UIInputHandler>(this.OnLeftClickCategory));
		}
		PlayerProfile.SavingStarted = (Action)Delegate.Combine(PlayerProfile.SavingStarted, new Action(this.ProfileSaveStarted));
		PlayerProfile.SavingFinished = (Action)Delegate.Combine(PlayerProfile.SavingFinished, new Action(this.ProfileSaveFinished));
		ZNet.WorldSaveStarted = (Action)Delegate.Combine(ZNet.WorldSaveStarted, new Action(this.WorldSaveStarted));
		ZNet.WorldSaveFinished = (Action)Delegate.Combine(ZNet.WorldSaveFinished, new Action(this.WorldSaveFinished));
	}

	// Token: 0x0600078E RID: 1934 RVA: 0x00040E7E File Offset: 0x0003F07E
	private void ProfileSaveStarted()
	{
		this.m_profileSaving = true;
		this.m_fullyOpaqueSaveIcon = true;
		this.m_saveIconTimer = 3f;
	}

	// Token: 0x0600078F RID: 1935 RVA: 0x00040E99 File Offset: 0x0003F099
	private void ProfileSaveFinished()
	{
		this.m_profileSaving = false;
	}

	// Token: 0x06000790 RID: 1936 RVA: 0x00040EA2 File Offset: 0x0003F0A2
	private void WorldSaveStarted()
	{
		this.m_worldSaving = true;
		this.m_fullyOpaqueSaveIcon = true;
		this.m_saveIconTimer = 3f;
	}

	// Token: 0x06000791 RID: 1937 RVA: 0x00040EBD File Offset: 0x0003F0BD
	private void WorldSaveFinished()
	{
		this.m_worldSaving = false;
	}

	// Token: 0x06000792 RID: 1938 RVA: 0x00040EC8 File Offset: 0x0003F0C8
	private void SetVisible(bool visible)
	{
		if (visible == this.IsVisible())
		{
			return;
		}
		if (visible)
		{
			this.m_rootObject.transform.localPosition = Vector3.zero;
		}
		else
		{
			this.m_rootObject.transform.localPosition = Hud.s_notVisiblePosition;
			if (ZInput.IsGamepadActive() && !Player.m_localPlayer.InCutscene())
			{
				string text = "$hud_hidden_messagehud_notification_gamepad " + ZInput.instance.GetBoundKeyString("JoyAltKeys", false) + " + " + ZInput.instance.GetBoundKeyString("JoyToggleHUD", false);
				MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text, 0, null, true);
			}
		}
		if (Menu.instance && (visible || (Player.m_localPlayer && !Player.m_localPlayer.InCutscene())))
		{
			Menu.instance.m_root.transform.localPosition = this.m_rootObject.transform.localPosition;
		}
	}

	// Token: 0x06000793 RID: 1939 RVA: 0x00040FAB File Offset: 0x0003F1AB
	public bool IsVisible()
	{
		return this.m_rootObject.transform.localPosition.x < 1000f;
	}

	// Token: 0x06000794 RID: 1940 RVA: 0x00040FCC File Offset: 0x0003F1CC
	private void Update()
	{
		float deltaTime = Time.deltaTime;
		if (this.m_worldSaving || this.m_profileSaving || this.m_saveIconTimer > 0f)
		{
			this.m_saveIcon.SetActive(true);
			if ((double)Time.unscaledDeltaTime < 0.5)
			{
				this.m_saveIconTimer -= Time.unscaledDeltaTime;
			}
			Color color = this.m_saveIconImage.color;
			float a;
			if (this.m_fullyOpaqueSaveIcon)
			{
				a = 1f;
				this.m_fullyOpaqueSaveIcon = false;
			}
			else
			{
				a = 0.3f + Mathf.PingPong(this.m_saveIconTimer * 2f, 0.7f);
			}
			this.m_saveIconImage.color = new Color(color.r, color.g, color.b, a);
			this.m_badConnectionIcon.SetActive(false);
		}
		else
		{
			this.m_saveIcon.SetActive(false);
			this.m_badConnectionIcon.SetActive(ZNet.instance != null && ZNet.instance.HasBadConnection() && Mathf.Sin(Time.time * 10f) > 0f);
		}
		Player localPlayer = Player.m_localPlayer;
		this.UpdateDamageFlash(deltaTime);
		if (localPlayer)
		{
			bool flag = (ZInput.InputLayout == InputLayout.Alternative1 && ZInput.GetButtonDown("JoyToggleHUD") && ZInput.GetButton("JoyAltKeys") && !ZInput.GetButton("JoyLTrigger")) || (ZInput.InputLayout == InputLayout.Default && ZInput.GetButtonDown("JoyToggleHUD") && ZInput.GetButton("JoyAltKeys") && !ZInput.GetButton("JoyLBumper")) || (ZInput.InputLayout == InputLayout.Alternative2 && ZInput.GetButtonDown("JoyToggleHUD") && ZInput.GetButton("JoyAltKeys") && !ZInput.GetButton("JoyLBumper"));
			if ((ZInput.GetKeyDown(KeyCode.F3, true) && ZInput.GetKey(KeyCode.LeftControl, true)) || flag)
			{
				this.m_userHidden = !this.m_userHidden;
				this.m_hudPressed = 0f;
			}
			if (ZInput.GetButtonDown("JoyToggleHUD") && !ZInput.GetButton("JoyLTrigger"))
			{
				this.m_hudPressed += 1f;
			}
			if (this.m_hudPressed > 0f)
			{
				this.m_hudPressed -= Time.deltaTime;
			}
			if (this.m_hudPressed > 3f && this.m_userHidden)
			{
				this.m_userHidden = false;
				this.m_hudPressed = 0f;
			}
			this.SetVisible(!this.m_userHidden && !localPlayer.InCutscene());
			this.UpdateBuild(localPlayer, false);
			this.m_tempStatusEffects.Clear();
			localPlayer.GetSEMan().GetHUDStatusEffects(this.m_tempStatusEffects);
			this.UpdateStatusEffects(this.m_tempStatusEffects);
			this.UpdateGuardianPower(localPlayer);
			float attackDrawPercentage = localPlayer.GetAttackDrawPercentage();
			this.UpdateFood(localPlayer);
			this.UpdateHealth(localPlayer);
			this.UpdateStamina(localPlayer, deltaTime);
			this.UpdateEitr(localPlayer, deltaTime);
			this.UpdateStealth(localPlayer, attackDrawPercentage);
			this.UpdateCrosshair(localPlayer, attackDrawPercentage);
			this.UpdateEvent(localPlayer);
			this.UpdateActionProgress(localPlayer);
			this.UpdateStagger(localPlayer, deltaTime);
			this.UpdateMount(localPlayer, deltaTime);
		}
	}

	// Token: 0x06000795 RID: 1941 RVA: 0x000412E0 File Offset: 0x0003F4E0
	private void LateUpdate()
	{
		this.UpdateBlackScreen(Player.m_localPlayer, Time.deltaTime);
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			this.UpdateShipHud(localPlayer, Time.deltaTime);
		}
	}

	// Token: 0x06000796 RID: 1942 RVA: 0x00041317 File Offset: 0x0003F517
	private float GetFadeDuration(Player player)
	{
		if (player != null)
		{
			if (player.IsDead())
			{
				return Game.instance.m_fadeTimeDeath;
			}
			if (player.IsSleeping())
			{
				return Game.instance.m_fadeTimeSleep;
			}
		}
		return 1f;
	}

	// Token: 0x06000797 RID: 1943 RVA: 0x00041350 File Offset: 0x0003F550
	private void UpdateBlackScreen(Player player, float dt)
	{
		if (!(player == null) && !player.IsDead() && !player.IsTeleporting() && !Game.instance.IsShuttingDown() && !player.IsSleeping())
		{
			this.m_haveSetupLoadScreen = false;
			float fadeDuration = this.GetFadeDuration(player);
			float num = this.m_loadingScreen.alpha;
			num = Mathf.MoveTowards(num, 0f, dt / fadeDuration);
			this.m_loadingScreen.alpha = num;
			if (this.m_loadingScreen.alpha <= 0f)
			{
				this.m_loadingScreen.gameObject.SetActive(false);
			}
			return;
		}
		this.m_loadingScreen.gameObject.SetActive(true);
		float num2 = this.m_loadingScreen.alpha;
		float fadeDuration2 = this.GetFadeDuration(player);
		num2 = Mathf.MoveTowards(num2, 1f, dt / fadeDuration2);
		if (Game.instance.IsShuttingDown())
		{
			num2 = 1f;
		}
		this.m_loadingScreen.alpha = num2;
		if (player != null && player.IsSleeping())
		{
			this.m_sleepingProgress.SetActive(true);
			this.m_loadingProgress.SetActive(false);
			this.m_teleportingProgress.SetActive(false);
			return;
		}
		if (player != null && player.ShowTeleportAnimation())
		{
			this.m_loadingProgress.SetActive(false);
			this.m_sleepingProgress.SetActive(false);
			this.m_teleportingProgress.SetActive(true);
			return;
		}
		if (Game.instance && Game.instance.WaitingForRespawn())
		{
			bool flag = false;
			if (!this.m_haveSetupLoadScreen)
			{
				this.m_haveSetupLoadScreen = true;
				this.m_loadingTips.Shuffle<string>();
				this.m_currentLoadingTipIndex = 0;
				flag = true;
			}
			int num3 = this.m_currentLoadingTipIndex;
			if (ZInput.GetButtonDown("JoyButtonA") || ZInput.GetKeyDown(KeyCode.Space, true) || ZInput.GetMouseButtonDown(0) || ZInput.GetButtonDown("JoyDPadRight") || ZInput.GetKeyDown(KeyCode.RightArrow, true))
			{
				num3++;
			}
			if (ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetKeyDown(KeyCode.LeftArrow, true))
			{
				num3--;
			}
			if (num3 >= this.m_loadingTips.Count)
			{
				num3 = 0;
			}
			else if (num3 < 0)
			{
				num3 = this.m_loadingTips.Count - 1;
			}
			if (num3 != this.m_currentLoadingTipIndex)
			{
				this.m_currentLoadingTipIndex = num3;
				flag = true;
			}
			if (flag)
			{
				string text = this.m_loadingTips[this.m_currentLoadingTipIndex];
				ZLog.Log("tip:" + text);
				this.m_loadingTip.text = Localization.instance.Localize(text);
			}
			this.m_loadingProgress.SetActive(true);
			this.m_sleepingProgress.SetActive(false);
			this.m_teleportingProgress.SetActive(false);
			return;
		}
		this.m_loadingProgress.SetActive(false);
		this.m_sleepingProgress.SetActive(false);
		this.m_teleportingProgress.SetActive(false);
	}

	// Token: 0x06000798 RID: 1944 RVA: 0x0004160C File Offset: 0x0003F80C
	private void UpdateShipHud(Player player, float dt)
	{
		Ship controlledShip = player.GetControlledShip();
		if (controlledShip == null)
		{
			this.m_shipHudRoot.gameObject.SetActive(false);
			return;
		}
		if (!this.IsVisible())
		{
			return;
		}
		Ship.Speed speedSetting = controlledShip.GetSpeedSetting();
		float rudder = controlledShip.GetRudder();
		float rudderValue = controlledShip.GetRudderValue();
		this.m_shipHudRoot.SetActive(true);
		this.m_rudderSlow.SetActive(speedSetting == Ship.Speed.Slow);
		this.m_rudderForward.SetActive(speedSetting == Ship.Speed.Half);
		this.m_rudderFastForward.SetActive(speedSetting == Ship.Speed.Full);
		this.m_rudderBackward.SetActive(speedSetting == Ship.Speed.Back);
		this.m_rudderLeft.SetActive(false);
		this.m_rudderRight.SetActive(false);
		this.m_fullSail.SetActive(speedSetting == Ship.Speed.Full);
		this.m_halfSail.SetActive(speedSetting == Ship.Speed.Half);
		this.m_rudder.SetActive(speedSetting == Ship.Speed.Slow || speedSetting == Ship.Speed.Back || (speedSetting == Ship.Speed.Stop && Mathf.Abs(rudderValue) > 0.2f));
		if ((rudder > 0f && rudderValue < 1f) || (rudder < 0f && rudderValue > -1f))
		{
			this.m_shipRudderIcon.transform.Rotate(new Vector3(0f, 0f, 200f * -rudder * dt));
		}
		if (Mathf.Abs(rudderValue) < 0.02f)
		{
			this.m_shipRudderIndicator.gameObject.SetActive(false);
		}
		else
		{
			this.m_shipRudderIndicator.gameObject.SetActive(true);
			if (rudderValue > 0f)
			{
				this.m_shipRudderIndicator.fillClockwise = true;
				this.m_shipRudderIndicator.fillAmount = rudderValue * 0.25f;
			}
			else
			{
				this.m_shipRudderIndicator.fillClockwise = false;
				this.m_shipRudderIndicator.fillAmount = -rudderValue * 0.25f;
			}
		}
		float shipYawAngle = controlledShip.GetShipYawAngle();
		this.m_shipWindIndicatorRoot.localRotation = Quaternion.Euler(0f, 0f, shipYawAngle);
		float windAngle = controlledShip.GetWindAngle();
		this.m_shipWindIconRoot.localRotation = Quaternion.Euler(0f, 0f, windAngle);
		float windAngleFactor = controlledShip.GetWindAngleFactor();
		this.m_shipWindIcon.color = Color.Lerp(Hud.s_shipWindIconColor, Color.white, windAngleFactor);
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		this.m_shipControlsRoot.transform.position = mainCamera.WorldToScreenPointScaled(controlledShip.m_controlGuiPos.position);
	}

	// Token: 0x06000799 RID: 1945 RVA: 0x00041864 File Offset: 0x0003FA64
	private void UpdateStagger(Player player, float dt)
	{
		float staggerPercentage = player.GetStaggerPercentage();
		this.m_staggerProgress.SetValue(staggerPercentage);
		if (staggerPercentage > 0f)
		{
			this.m_staggerHideTimer = 0f;
		}
		else
		{
			this.m_staggerHideTimer += dt;
		}
		this.m_staggerAnimator.SetBool("Visible", this.m_staggerHideTimer < 1f);
	}

	// Token: 0x0600079A RID: 1946 RVA: 0x000418C4 File Offset: 0x0003FAC4
	public void StaggerBarFlash()
	{
		this.m_staggerAnimator.SetTrigger("Flash");
	}

	// Token: 0x0600079B RID: 1947 RVA: 0x000418D8 File Offset: 0x0003FAD8
	private void UpdateActionProgress(Player player)
	{
		string text;
		float value;
		Player.MinorActionData minorActionData;
		player.GetActionProgress(out text, out value, out minorActionData);
		if (!string.IsNullOrEmpty(text) && minorActionData.m_duration > 0.5f)
		{
			this.m_actionBarRoot.SetActive(true);
			this.m_actionProgress.SetValue(value);
			this.m_actionName.text = Localization.instance.Localize(text);
			return;
		}
		this.m_actionBarRoot.SetActive(false);
	}

	// Token: 0x0600079C RID: 1948 RVA: 0x00041944 File Offset: 0x0003FB44
	private void UpdateCrosshair(Player player, float bowDrawPercentage)
	{
		if (player.IsAttached() && player.GetAttachCameraPoint() != null)
		{
			this.m_crosshair.gameObject.SetActive(false);
		}
		else if (!this.m_crosshair.gameObject.activeSelf)
		{
			this.m_crosshair.gameObject.SetActive(true);
		}
		GameObject hoverObject = player.GetHoverObject();
		Hoverable hoverable = hoverObject ? hoverObject.GetComponentInParent<Hoverable>() : null;
		if (hoverable != null && !TextViewer.instance.IsVisible())
		{
			string text = hoverable.GetHoverText();
			if (ZInput.IsGamepadActive())
			{
				text = text.Replace("[<color=yellow><b><sprite=", "<sprite=");
				text = text.Replace("\"></b></color>]", "\">");
			}
			this.m_hoverName.text = text;
			this.m_crosshair.color = ((this.m_hoverName.text.Length > 0) ? Color.yellow : Hud.s_whiteHalfAlpha);
		}
		else
		{
			this.m_crosshair.color = Hud.s_whiteHalfAlpha;
			this.m_hoverName.text = "";
		}
		Piece hoveringPiece = player.GetHoveringPiece();
		if (hoveringPiece)
		{
			WearNTear component = hoveringPiece.GetComponent<WearNTear>();
			if (component)
			{
				this.m_pieceHealthRoot.gameObject.SetActive(true);
				this.m_pieceHealthBar.SetValue(component.GetHealthPercentage());
			}
			else
			{
				this.m_pieceHealthRoot.gameObject.SetActive(false);
			}
		}
		else
		{
			this.m_pieceHealthRoot.gameObject.SetActive(false);
		}
		if (bowDrawPercentage > 0f)
		{
			float num = Mathf.Lerp(1f, 0.15f, bowDrawPercentage);
			this.m_crosshairBow.gameObject.SetActive(true);
			this.m_crosshairBow.transform.localScale = new Vector3(num, num, num);
			this.m_crosshairBow.color = Color.Lerp(new Color(1f, 1f, 1f, 0f), Color.yellow, bowDrawPercentage);
			return;
		}
		this.m_crosshairBow.gameObject.SetActive(false);
	}

	// Token: 0x0600079D RID: 1949 RVA: 0x00041B40 File Offset: 0x0003FD40
	private void UpdateStealth(Player player, float bowDrawPercentage)
	{
		float stealthFactor = player.GetStealthFactor();
		if ((player.IsCrouching() || stealthFactor < 1f) && bowDrawPercentage == 0f)
		{
			if (player.IsSensed())
			{
				this.m_targetedAlert.SetActive(true);
				this.m_targeted.SetActive(false);
				this.m_hidden.SetActive(false);
			}
			else if (player.IsTargeted())
			{
				this.m_targetedAlert.SetActive(false);
				this.m_targeted.SetActive(true);
				this.m_hidden.SetActive(false);
			}
			else
			{
				this.m_targetedAlert.SetActive(false);
				this.m_targeted.SetActive(false);
				this.m_hidden.SetActive(true);
			}
			this.m_stealthBar.gameObject.SetActive(true);
			this.m_stealthBar.SetValue(stealthFactor);
			return;
		}
		this.m_targetedAlert.SetActive(false);
		this.m_hidden.SetActive(false);
		this.m_targeted.SetActive(false);
		this.m_stealthBar.gameObject.SetActive(false);
	}

	// Token: 0x0600079E RID: 1950 RVA: 0x00041C48 File Offset: 0x0003FE48
	private void SetHealthBarSize(float size)
	{
		size = Mathf.Ceil(size);
		Mathf.Max(size + 56f, 138f);
		this.m_healthBarRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
		this.m_healthBarSlow.SetWidth(size);
		this.m_healthBarFast.SetWidth(size);
	}

	// Token: 0x0600079F RID: 1951 RVA: 0x00041C94 File Offset: 0x0003FE94
	private void SetStaminaBarSize(float size)
	{
		this.m_staminaBar2Root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size + this.m_staminaBarBorderBuffer);
		this.m_staminaBar2Slow.SetWidth(size);
		this.m_staminaBar2Fast.SetWidth(size);
	}

	// Token: 0x060007A0 RID: 1952 RVA: 0x00041CC2 File Offset: 0x0003FEC2
	private void SetEitrBarSize(float size)
	{
		this.m_eitrBarRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size + this.m_staminaBarBorderBuffer);
		this.m_eitrBarSlow.SetWidth(size);
		this.m_eitrBarFast.SetWidth(size);
	}

	// Token: 0x060007A1 RID: 1953 RVA: 0x00041CF0 File Offset: 0x0003FEF0
	private void UpdateFood(Player player)
	{
		List<Player.Food> foods = player.GetFoods();
		float size = player.GetBaseFoodHP() / 25f * 32f;
		this.m_foodBaseBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
		for (int i = 0; i < this.m_foodBars.Length; i++)
		{
			Image image = this.m_foodBars[i];
			Image image2 = this.m_foodIcons[i];
			TMP_Text tmp_Text = this.m_foodTime[i];
			if (i < foods.Count)
			{
				image.gameObject.SetActive(true);
				Player.Food food = foods[i];
				image2.gameObject.SetActive(true);
				image2.sprite = food.m_item.GetIcon();
				if (food.CanEatAgain())
				{
					image2.color = new Color(1f, 1f, 1f, 0.7f + Mathf.Sin(Time.time * 5f) * 0.3f);
				}
				else
				{
					image2.color = Color.white;
				}
				tmp_Text.gameObject.SetActive(true);
				if (food.m_time >= 60f)
				{
					tmp_Text.text = Mathf.CeilToInt(food.m_time / 60f).ToString() + "m";
					tmp_Text.color = Color.white;
				}
				else
				{
					tmp_Text.text = Mathf.FloorToInt(food.m_time).ToString() + "s";
					tmp_Text.color = new Color(1f, 1f, 1f, 0.4f + Mathf.Sin(Time.time * 10f) * 0.6f);
				}
			}
			else
			{
				image.gameObject.SetActive(false);
				image2.gameObject.SetActive(false);
				tmp_Text.gameObject.SetActive(false);
			}
		}
		float size2 = Mathf.Ceil(player.GetMaxHealth() / 25f * 32f);
		this.m_foodBarRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size2);
	}

	// Token: 0x060007A2 RID: 1954 RVA: 0x00041EF0 File Offset: 0x000400F0
	private void UpdateMount(Player player, float dt)
	{
		Sadle sadle = player.GetDoodadController() as Sadle;
		if (sadle == null)
		{
			this.m_mountPanel.SetActive(false);
			return;
		}
		Character character = sadle.GetCharacter();
		this.m_mountPanel.SetActive(true);
		this.m_mountIcon.overrideSprite = sadle.m_mountIcon;
		this.m_mountHealthBarSlow.SetValue(character.GetHealthPercentage());
		this.m_mountHealthBarFast.SetValue(character.GetHealthPercentage());
		this.m_mountHealthText.text = Mathf.CeilToInt(character.GetHealth()).ToFastString();
		float stamina = sadle.GetStamina();
		float maxStamina = sadle.GetMaxStamina();
		this.m_mountStaminaBar.SetValue(stamina / maxStamina);
		this.m_mountStaminaText.text = Mathf.CeilToInt(stamina).ToFastString();
		this.m_mountNameText.text = character.GetHoverName() + " (" + Localization.instance.Localize(sadle.GetTameable().GetStatusString()) + " )";
	}

	// Token: 0x060007A3 RID: 1955 RVA: 0x00041FE8 File Offset: 0x000401E8
	private void UpdateHealth(Player player)
	{
		float maxHealth = player.GetMaxHealth();
		this.SetHealthBarSize(maxHealth / 25f * 32f);
		float health = player.GetHealth();
		this.m_healthBarFast.SetMaxValue(maxHealth);
		this.m_healthBarFast.SetValue(health);
		this.m_healthBarSlow.SetMaxValue(maxHealth);
		this.m_healthBarSlow.SetValue(health);
		string text = Mathf.CeilToInt(player.GetHealth()).ToFastString();
		this.m_healthText.text = text.ToString();
	}

	// Token: 0x060007A4 RID: 1956 RVA: 0x00042068 File Offset: 0x00040268
	private void UpdateStamina(Player player, float dt)
	{
		float stamina = player.GetStamina();
		float maxStamina = player.GetMaxStamina();
		if (stamina < maxStamina)
		{
			this.m_staminaHideTimer = 0f;
		}
		else
		{
			this.m_staminaHideTimer += dt;
		}
		this.m_staminaAnimator.SetBool("Visible", this.m_staminaHideTimer < 1f);
		this.m_staminaText.text = Mathf.CeilToInt(stamina).ToFastString();
		this.SetStaminaBarSize(maxStamina / 25f * 32f);
		RectTransform rectTransform = this.m_staminaBar2Root.transform as RectTransform;
		if (this.m_buildHud.activeSelf || this.m_shipHudRoot.activeSelf)
		{
			rectTransform.anchoredPosition = new Vector2(0f, 320f);
		}
		else
		{
			rectTransform.anchoredPosition = new Vector2(0f, 130f);
		}
		this.m_staminaBar2Slow.SetValue(stamina / maxStamina);
		this.m_staminaBar2Fast.SetValue(stamina / maxStamina);
	}

	// Token: 0x060007A5 RID: 1957 RVA: 0x0004215C File Offset: 0x0004035C
	private void UpdateEitr(Player player, float dt)
	{
		float eitr = player.GetEitr();
		float maxEitr = player.GetMaxEitr();
		if (eitr < maxEitr)
		{
			this.m_eitrHideTimer = 0f;
		}
		else
		{
			this.m_eitrHideTimer += dt;
		}
		this.m_eitrAnimator.SetBool("Visible", this.m_eitrHideTimer < 1f);
		this.m_eitrText.text = Mathf.CeilToInt(eitr).ToFastString();
		this.SetEitrBarSize(maxEitr / 25f * 32f);
		RectTransform rectTransform = this.m_eitrBarRoot.transform as RectTransform;
		if (this.m_buildHud.activeSelf || this.m_shipHudRoot.activeSelf)
		{
			rectTransform.anchoredPosition = new Vector2(0f, 285f);
		}
		else
		{
			rectTransform.anchoredPosition = new Vector2(0f, 130f);
		}
		this.m_eitrBarSlow.SetValue(eitr / maxEitr);
		this.m_eitrBarFast.SetValue(eitr / maxEitr);
	}

	// Token: 0x060007A6 RID: 1958 RVA: 0x00042250 File Offset: 0x00040450
	public void DamageFlash()
	{
		Color color = this.m_damageScreen.color;
		color.a = 1f;
		this.m_damageScreen.color = color;
		this.m_damageScreen.gameObject.SetActive(true);
	}

	// Token: 0x060007A7 RID: 1959 RVA: 0x00042294 File Offset: 0x00040494
	private void UpdateDamageFlash(float dt)
	{
		Color color = this.m_damageScreen.color;
		color.a = Mathf.MoveTowards(color.a, 0f, dt * 4f);
		this.m_damageScreen.color = color;
		if (color.a <= 0f)
		{
			this.m_damageScreen.gameObject.SetActive(false);
		}
	}

	// Token: 0x060007A8 RID: 1960 RVA: 0x000422F8 File Offset: 0x000404F8
	private void UpdatePieceList(Player player, Vector2Int selectedNr, Piece.PieceCategory category, bool updateAllBuildStatuses)
	{
		List<Piece> buildPieces = player.GetBuildPieces();
		int num = 15;
		int num2 = 6;
		if (buildPieces.Count <= 1)
		{
			num = 1;
			num2 = 1;
		}
		if (this.m_pieceIcons.Count != num * num2)
		{
			foreach (Hud.PieceIconData pieceIconData in this.m_pieceIcons)
			{
				UnityEngine.Object.Destroy(pieceIconData.m_go);
			}
			this.m_pieceIcons.Clear();
			for (int i = 0; i < num2; i++)
			{
				for (int j = 0; j < num; j++)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_pieceIconPrefab, this.m_pieceListRoot);
					(gameObject.transform as RectTransform).anchoredPosition = new Vector2((float)j * this.m_pieceIconSpacing, (float)(-(float)i) * this.m_pieceIconSpacing);
					Hud.PieceIconData pieceIconData2 = new Hud.PieceIconData();
					pieceIconData2.m_go = gameObject;
					pieceIconData2.m_tooltip = gameObject.GetComponent<UITooltip>();
					pieceIconData2.m_icon = gameObject.transform.Find("icon").GetComponent<Image>();
					pieceIconData2.m_marker = gameObject.transform.Find("selected").gameObject;
					pieceIconData2.m_upgrade = gameObject.transform.Find("upgrade").gameObject;
					pieceIconData2.m_icon.color = Hud.s_colorRedBlueZeroAlpha;
					UIInputHandler component = gameObject.GetComponent<UIInputHandler>();
					component.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftDown, new Action<UIInputHandler>(this.OnLeftClickPiece));
					component.m_onRightDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onRightDown, new Action<UIInputHandler>(this.OnRightClickPiece));
					component.m_onPointerEnter = (Action<UIInputHandler>)Delegate.Combine(component.m_onPointerEnter, new Action<UIInputHandler>(this.OnHoverPiece));
					component.m_onPointerExit = (Action<UIInputHandler>)Delegate.Combine(component.m_onPointerExit, new Action<UIInputHandler>(this.OnHoverPieceExit));
					this.m_pieceIcons.Add(pieceIconData2);
				}
			}
		}
		for (int k = 0; k < num2; k++)
		{
			for (int l = 0; l < num; l++)
			{
				int num3 = k * num + l;
				Hud.PieceIconData pieceIconData3 = this.m_pieceIcons[num3];
				pieceIconData3.m_marker.SetActive(new Vector2Int(l, k) == selectedNr);
				if (num3 < buildPieces.Count)
				{
					Piece piece = buildPieces[num3];
					pieceIconData3.m_icon.sprite = piece.m_icon;
					pieceIconData3.m_icon.enabled = true;
					pieceIconData3.m_tooltip.m_text = piece.m_name;
					pieceIconData3.m_upgrade.SetActive(piece.m_isUpgrade);
				}
				else
				{
					pieceIconData3.m_icon.enabled = false;
					pieceIconData3.m_tooltip.m_text = "";
					pieceIconData3.m_upgrade.SetActive(false);
				}
			}
		}
		this.UpdatePieceBuildStatus(buildPieces, player);
		if (updateAllBuildStatuses)
		{
			this.UpdatePieceBuildStatusAll(buildPieces, player);
		}
		if (this.m_lastPieceCategory != category)
		{
			this.m_lastPieceCategory = category;
			this.UpdatePieceBuildStatusAll(buildPieces, player);
		}
	}

	// Token: 0x060007A9 RID: 1961 RVA: 0x00042624 File Offset: 0x00040824
	private void OnLeftClickCategory(UIInputHandler ih)
	{
		for (int i = 0; i < this.m_pieceCategoryTabs.Length; i++)
		{
			if (this.m_pieceCategoryTabs[i] == ih.gameObject)
			{
				Player.m_localPlayer.SetBuildCategory(i);
				return;
			}
		}
	}

	// Token: 0x060007AA RID: 1962 RVA: 0x00042665 File Offset: 0x00040865
	private void OnLeftClickPiece(UIInputHandler ih)
	{
		this.SelectPiece(ih);
		Hud.HidePieceSelection();
	}

	// Token: 0x060007AB RID: 1963 RVA: 0x00042673 File Offset: 0x00040873
	private void OnRightClickPiece(UIInputHandler ih)
	{
		if (this.IsQuickPieceSelectEnabled())
		{
			this.SelectPiece(ih);
			Hud.HidePieceSelection();
		}
	}

	// Token: 0x060007AC RID: 1964 RVA: 0x0004268C File Offset: 0x0004088C
	private void OnHoverPiece(UIInputHandler ih)
	{
		Vector2Int selectedGrid = this.GetSelectedGrid(ih);
		if (selectedGrid.x != -1)
		{
			this.m_hoveredPiece = Player.m_localPlayer.GetPiece(selectedGrid);
		}
	}

	// Token: 0x060007AD RID: 1965 RVA: 0x000426BC File Offset: 0x000408BC
	private void OnHoverPieceExit(UIInputHandler ih)
	{
		this.m_hoveredPiece = null;
	}

	// Token: 0x060007AE RID: 1966 RVA: 0x000426C5 File Offset: 0x000408C5
	public bool IsQuickPieceSelectEnabled()
	{
		return PlayerPrefs.GetInt("QuickPieceSelect", 0) == 1;
	}

	// Token: 0x060007AF RID: 1967 RVA: 0x000426D8 File Offset: 0x000408D8
	private Vector2Int GetSelectedGrid(UIInputHandler ih)
	{
		int num = 15;
		int num2 = 6;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				int index = i * num + j;
				if (this.m_pieceIcons[index].m_go == ih.gameObject)
				{
					return new Vector2Int(j, i);
				}
			}
		}
		return new Vector2Int(-1, -1);
	}

	// Token: 0x060007B0 RID: 1968 RVA: 0x00042738 File Offset: 0x00040938
	private void SelectPiece(UIInputHandler ih)
	{
		Vector2Int selectedGrid = this.GetSelectedGrid(ih);
		if (selectedGrid.x != -1)
		{
			Player.m_localPlayer.SetSelectedPiece(selectedGrid);
			this.m_selectItemEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		}
	}

	// Token: 0x060007B1 RID: 1969 RVA: 0x00042788 File Offset: 0x00040988
	private void UpdatePieceBuildStatus(List<Piece> pieces, Player player)
	{
		if (this.m_pieceIcons.Count == 0)
		{
			return;
		}
		if (this.m_pieceIconUpdateIndex >= this.m_pieceIcons.Count)
		{
			this.m_pieceIconUpdateIndex = 0;
		}
		Hud.PieceIconData pieceIconData = this.m_pieceIcons[this.m_pieceIconUpdateIndex];
		if (this.m_pieceIconUpdateIndex < pieces.Count)
		{
			Piece piece = pieces[this.m_pieceIconUpdateIndex];
			bool flag = player.HaveRequirements(piece, Player.RequirementMode.CanBuild);
			pieceIconData.m_icon.color = (flag ? Color.white : Hud.s_colorRedBlueZeroAlpha);
		}
		this.m_pieceIconUpdateIndex++;
	}

	// Token: 0x060007B2 RID: 1970 RVA: 0x0004281C File Offset: 0x00040A1C
	private void UpdatePieceBuildStatusAll(List<Piece> pieces, Player player)
	{
		for (int i = 0; i < this.m_pieceIcons.Count; i++)
		{
			Hud.PieceIconData pieceIconData = this.m_pieceIcons[i];
			if (i < pieces.Count)
			{
				Piece piece = pieces[i];
				bool flag = player.HaveRequirements(piece, Player.RequirementMode.CanBuild);
				pieceIconData.m_icon.color = (flag ? Color.white : Hud.s_colorRedBlueZeroAlpha);
			}
			else
			{
				pieceIconData.m_icon.color = Color.white;
			}
		}
		this.m_pieceIconUpdateIndex = 0;
	}

	// Token: 0x060007B3 RID: 1971 RVA: 0x0004289C File Offset: 0x00040A9C
	public void TogglePieceSelection()
	{
		this.m_hoveredPiece = null;
		if (this.m_pieceSelectionWindow.activeSelf)
		{
			PlayerController.SetTakeInputDelay(0.2f);
			this.m_pieceSelectionWindow.SetActive(false);
			return;
		}
		this.m_pieceSelectionWindow.SetActive(true);
		this.UpdateBuild(Player.m_localPlayer, true);
	}

	// Token: 0x060007B4 RID: 1972 RVA: 0x000428EC File Offset: 0x00040AEC
	private void OnClosePieceSelection(UIInputHandler ih)
	{
		Hud.HidePieceSelection();
	}

	// Token: 0x060007B5 RID: 1973 RVA: 0x000428F3 File Offset: 0x00040AF3
	public static void HidePieceSelection()
	{
		if (Hud.m_instance == null)
		{
			return;
		}
		Hud.m_instance.m_closePieceSelection = 2;
	}

	// Token: 0x060007B6 RID: 1974 RVA: 0x0004290E File Offset: 0x00040B0E
	public static bool IsPieceSelectionVisible()
	{
		return !(Hud.m_instance == null) && Hud.m_instance.m_buildHud.activeSelf && Hud.m_instance.m_pieceSelectionWindow.activeSelf;
	}

	// Token: 0x060007B7 RID: 1975 RVA: 0x00042944 File Offset: 0x00040B44
	private void UpdateBuild(Player player, bool forceUpdateAllBuildStatuses)
	{
		if (!player.InPlaceMode())
		{
			this.m_hoveredPiece = null;
			this.m_buildHud.SetActive(false);
			this.m_pieceSelectionWindow.SetActive(false);
			return;
		}
		if (this.m_closePieceSelection > 0)
		{
			this.m_closePieceSelection--;
			if (this.m_closePieceSelection <= 0 && this.m_pieceSelectionWindow.activeSelf)
			{
				this.m_hoveredPiece = null;
				this.m_pieceSelectionWindow.SetActive(false);
				Character.SetTakeInputDelay(0.2f);
				PlayerController.SetTakeInputDelay(0.2f);
			}
		}
		Piece piece;
		Vector2Int selectedNr;
		int num;
		Piece.PieceCategory pieceCategory;
		PieceTable pieceTable;
		player.GetBuildSelection(out piece, out selectedNr, out num, out pieceCategory, out pieceTable);
		this.m_buildHud.SetActive(!this.m_radialMenu.Active);
		if (this.m_pieceSelectionWindow.activeSelf)
		{
			this.UpdatePieceList(player, selectedNr, pieceCategory, forceUpdateAllBuildStatuses);
			this.m_pieceCategoryRoot.SetActive(pieceTable.m_categories.Count > 0);
			if (pieceTable.m_categories.Count > 0)
			{
				for (int i = 0; i < this.m_pieceCategoryTabs.Length; i++)
				{
					GameObject gameObject = this.m_pieceCategoryTabs[i];
					Transform transform = gameObject.transform.Find("Selected");
					bool flag = i < pieceTable.m_categories.Count;
					gameObject.SetActive(flag);
					if (flag)
					{
						string text = string.Format("{0} [<color=yellow>{1}</color>]", pieceTable.m_categoryLabels[i], player.GetAvailableBuildPiecesInCategory(pieceTable.m_categories[i]));
						if (pieceTable.m_categories[i] == pieceCategory)
						{
							transform.gameObject.SetActive(true);
							transform.GetComponentInChildren<TMP_Text>().text = text;
						}
						else
						{
							transform.gameObject.SetActive(false);
							gameObject.GetComponentInChildren<TMP_Text>().text = text;
						}
					}
				}
			}
			Localization.instance.Localize(this.m_buildHud.transform);
		}
		if (this.m_hoveredPiece && (ZInput.IsGamepadActive() || !player.IsPieceAvailable(this.m_hoveredPiece)))
		{
			this.m_hoveredPiece = null;
		}
		if (this.m_hoveredPiece)
		{
			this.SetupPieceInfo(this.m_hoveredPiece);
			return;
		}
		this.SetupPieceInfo(piece);
	}

	// Token: 0x060007B8 RID: 1976 RVA: 0x00042B74 File Offset: 0x00040D74
	private void SetupPieceInfo(Piece piece)
	{
		if (piece == null)
		{
			this.m_buildSelection.text = Localization.instance.Localize("$hud_nothingtobuild");
			this.m_pieceDescription.text = "";
			this.m_buildIcon.enabled = false;
			this.m_snappingIcon.enabled = false;
			for (int i = 0; i < this.m_requirementItems.Length; i++)
			{
				this.m_requirementItems[i].SetActive(false);
			}
			return;
		}
		Player localPlayer = Player.m_localPlayer;
		this.m_buildSelection.text = Localization.instance.Localize(piece.m_name);
		this.m_pieceDescription.text = Localization.instance.Localize(piece.m_description);
		this.m_buildIcon.enabled = true;
		this.m_buildIcon.sprite = piece.m_icon;
		Sprite snappingIconForPiece = this.GetSnappingIconForPiece(piece);
		this.m_snappingIcon.sprite = snappingIconForPiece;
		this.m_snappingIcon.enabled = (snappingIconForPiece != null && (piece.m_category == Piece.PieceCategory.BuildingWorkbench || piece.m_groundPiece || piece.m_waterPiece));
		for (int j = 0; j < this.m_requirementItems.Length; j++)
		{
			if (j < piece.m_resources.Length)
			{
				Piece.Requirement req = piece.m_resources[j];
				this.m_requirementItems[j].SetActive(true);
				InventoryGui.SetupRequirement(this.m_requirementItems[j].transform, req, localPlayer, piece.FreeBuildKey() == GlobalKeys.NoCraftCost, 0, 1);
			}
			else
			{
				this.m_requirementItems[j].SetActive(false);
			}
		}
		if (piece.m_craftingStation)
		{
			CraftingStation craftingStation = CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, localPlayer.transform.position);
			GameObject gameObject = this.m_requirementItems[piece.m_resources.Length];
			gameObject.SetActive(true);
			Image component = gameObject.transform.Find("res_icon").GetComponent<Image>();
			TMP_Text component2 = gameObject.transform.Find("res_name").GetComponent<TMP_Text>();
			TMP_Text component3 = gameObject.transform.Find("res_amount").GetComponent<TMP_Text>();
			UITooltip component4 = gameObject.GetComponent<UITooltip>();
			component.sprite = piece.m_craftingStation.m_icon;
			component2.text = Localization.instance.Localize(piece.m_craftingStation.m_name);
			component4.m_text = piece.m_craftingStation.m_name;
			if (craftingStation != null)
			{
				craftingStation.ShowAreaMarker();
				component.color = Color.white;
				component3.text = "";
				component3.color = Color.white;
				return;
			}
			component.color = Color.gray;
			component3.text = Localization.instance.Localize("$menu_none");
			component3.color = ((Mathf.Sin(Time.time * 10f) > 0f && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost)) ? Color.red : Color.white);
		}
	}

	// Token: 0x060007B9 RID: 1977 RVA: 0x00042E50 File Offset: 0x00041050
	private Sprite GetSnappingIconForPiece(Piece piece)
	{
		if (piece.m_groundPiece)
		{
			if (!Player.m_localPlayer.AlternativePlacementActive)
			{
				return null;
			}
			return this.m_hoeSnappingIcon;
		}
		else if (piece.m_waterPiece)
		{
			if (!Player.m_localPlayer.AlternativePlacementActive)
			{
				return null;
			}
			return this.m_shipSnappingIcon;
		}
		else
		{
			if (!Player.m_localPlayer.AlternativePlacementActive)
			{
				return null;
			}
			return this.m_buildSnappingIcon;
		}
	}

	// Token: 0x060007BA RID: 1978 RVA: 0x00042EAC File Offset: 0x000410AC
	private void UpdateGuardianPower(Player player)
	{
		StatusEffect statusEffect;
		float num;
		player.GetGuardianPowerHUD(out statusEffect, out num);
		if (!statusEffect)
		{
			if (this.m_gpRoot.gameObject.activeSelf)
			{
				this.m_gpRoot.gameObject.SetActive(false);
			}
			return;
		}
		if (!this.m_gpRoot.gameObject.activeSelf)
		{
			this.m_gpRoot.gameObject.SetActive(true);
		}
		this.m_gpIcon.sprite = statusEffect.m_icon;
		this.m_gpIcon.color = ((num <= 0f) ? Color.white : Hud.s_colorRedBlueZeroAlpha);
		this.m_gpName.text = Localization.instance.Localize(statusEffect.m_name);
		if (num > 0f)
		{
			this.m_gpCooldown.text = StatusEffect.GetTimeString(num, false, false);
			return;
		}
		this.m_gpCooldown.text = Localization.instance.Localize("$hud_ready");
	}

	// Token: 0x060007BB RID: 1979 RVA: 0x00042F98 File Offset: 0x00041198
	private void UpdateStatusEffects(List<StatusEffect> statusEffects)
	{
		if (this.m_statusEffects.Count != statusEffects.Count)
		{
			foreach (RectTransform rectTransform in this.m_statusEffects)
			{
				UnityEngine.Object.Destroy(rectTransform.gameObject);
			}
			this.m_statusEffects.Clear();
			for (int i = 0; i < statusEffects.Count; i++)
			{
				int num = Mathf.FloorToInt((float)(i / this.m_effectsPerRow));
				int num2 = i - num * this.m_effectsPerRow;
				RectTransform rectTransform2 = UnityEngine.Object.Instantiate<RectTransform>(this.m_statusEffectTemplate, this.m_statusEffectListRoot);
				rectTransform2.gameObject.SetActive(true);
				rectTransform2.anchoredPosition = new Vector3(-4f - (float)num2 * this.m_statusEffectSpacing, (float)(-(float)num) * this.m_statusEffectSpacing, 0f);
				this.m_statusEffects.Add(rectTransform2);
			}
		}
		for (int j = 0; j < statusEffects.Count; j++)
		{
			StatusEffect statusEffect = statusEffects[j];
			RectTransform rectTransform3 = this.m_statusEffects[j];
			Image component = rectTransform3.Find("Icon").GetComponent<Image>();
			component.sprite = statusEffect.m_icon;
			if (statusEffect.m_flashIcon)
			{
				component.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Hud.s_colorRedish : Color.white);
			}
			else
			{
				component.color = Color.white;
			}
			rectTransform3.Find("Cooldown").gameObject.SetActive(statusEffect.m_cooldownIcon);
			rectTransform3.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize(statusEffect.m_name);
			TMP_Text component2 = rectTransform3.Find("TimeText").GetComponent<TMP_Text>();
			string iconText = statusEffect.GetIconText();
			if (!string.IsNullOrEmpty(iconText))
			{
				component2.gameObject.SetActive(true);
				component2.text = iconText;
			}
			else
			{
				component2.gameObject.SetActive(false);
			}
			if (statusEffect.m_isNew)
			{
				statusEffect.m_isNew = false;
				rectTransform3.GetComponentInChildren<Animator>().SetTrigger("flash");
			}
		}
	}

	// Token: 0x060007BC RID: 1980 RVA: 0x000431D8 File Offset: 0x000413D8
	private void UpdateEvent(Player player)
	{
		RandomEvent activeEvent = RandEventSystem.instance.GetActiveEvent();
		if (activeEvent != null && !EnemyHud.instance.ShowingBossHud() && activeEvent.GetTime() > 3f)
		{
			this.m_eventBar.SetActive(true);
			this.m_eventName.text = Localization.instance.Localize(activeEvent.GetHudText());
			return;
		}
		this.m_eventBar.SetActive(false);
	}

	// Token: 0x060007BD RID: 1981 RVA: 0x00043240 File Offset: 0x00041440
	public void ToggleBetaTextVisible()
	{
		this.m_betaText.SetActive(!this.m_betaText.activeSelf);
	}

	// Token: 0x060007BE RID: 1982 RVA: 0x0004325B File Offset: 0x0004145B
	public void FlashHealthBar()
	{
		this.m_healthAnimator.SetTrigger("Flash");
	}

	// Token: 0x060007BF RID: 1983 RVA: 0x0004326D File Offset: 0x0004146D
	public void StaminaBarUppgradeFlash()
	{
		this.m_staminaAnimator.SetTrigger("Flash");
	}

	// Token: 0x060007C0 RID: 1984 RVA: 0x00043280 File Offset: 0x00041480
	public void StaminaBarEmptyFlash()
	{
		this.m_staminaHideTimer = 0f;
		if (this.m_staminaAnimator.GetCurrentAnimatorStateInfo(0).IsTag("nostamina"))
		{
			return;
		}
		this.m_staminaAnimator.SetTrigger("NoStamina");
	}

	// Token: 0x060007C1 RID: 1985 RVA: 0x000432C4 File Offset: 0x000414C4
	public void EitrBarEmptyFlash()
	{
		this.m_eitrHideTimer = 0f;
		if (this.m_eitrAnimator.GetCurrentAnimatorStateInfo(0).IsTag("nostamina"))
		{
			return;
		}
		this.m_eitrAnimator.SetTrigger("NoStamina");
	}

	// Token: 0x060007C2 RID: 1986 RVA: 0x00043308 File Offset: 0x00041508
	public void EitrBarUppgradeFlash()
	{
		this.m_eitrAnimator.SetTrigger("Flash");
	}

	// Token: 0x060007C3 RID: 1987 RVA: 0x0004331A File Offset: 0x0004151A
	public static bool IsUserHidden()
	{
		return Hud.m_instance && Hud.m_instance.m_userHidden;
	}

	// Token: 0x060007C4 RID: 1988 RVA: 0x00043334 File Offset: 0x00041534
	public static bool InRadial()
	{
		return Hud.m_instance && Hud.instance.m_radialMenu && Hud.instance.m_radialMenu.gameObject.activeSelf;
	}

	// Token: 0x040008C6 RID: 2246
	private static Hud m_instance;

	// Token: 0x040008C7 RID: 2247
	public GameObject m_rootObject;

	// Token: 0x040008C8 RID: 2248
	public TMP_Text m_buildSelection;

	// Token: 0x040008C9 RID: 2249
	public TMP_Text m_pieceDescription;

	// Token: 0x040008CA RID: 2250
	public Image m_buildIcon;

	// Token: 0x040008CB RID: 2251
	[SerializeField]
	private Image m_snappingIcon;

	// Token: 0x040008CC RID: 2252
	[SerializeField]
	private Sprite m_buildSnappingIcon;

	// Token: 0x040008CD RID: 2253
	[SerializeField]
	private Sprite m_shipSnappingIcon;

	// Token: 0x040008CE RID: 2254
	[SerializeField]
	private Sprite m_hoeSnappingIcon;

	// Token: 0x040008CF RID: 2255
	public GameObject m_buildHud;

	// Token: 0x040008D0 RID: 2256
	public GameObject m_saveIcon;

	// Token: 0x040008D1 RID: 2257
	public Image m_saveIconImage;

	// Token: 0x040008D2 RID: 2258
	public GameObject m_badConnectionIcon;

	// Token: 0x040008D3 RID: 2259
	public GameObject m_betaText;

	// Token: 0x040008D4 RID: 2260
	[Header("Piece")]
	public GameObject[] m_requirementItems = new GameObject[0];

	// Token: 0x040008D5 RID: 2261
	public GameObject[] m_pieceCategoryTabs = new GameObject[0];

	// Token: 0x040008D6 RID: 2262
	public GameObject m_pieceSelectionWindow;

	// Token: 0x040008D7 RID: 2263
	public GameObject m_pieceCategoryRoot;

	// Token: 0x040008D8 RID: 2264
	public RectTransform m_pieceListRoot;

	// Token: 0x040008D9 RID: 2265
	public RectTransform m_pieceListMask;

	// Token: 0x040008DA RID: 2266
	public GameObject m_pieceIconPrefab;

	// Token: 0x040008DB RID: 2267
	public UIInputHandler m_closePieceSelectionButton;

	// Token: 0x040008DC RID: 2268
	public EffectList m_selectItemEffect = new EffectList();

	// Token: 0x040008DD RID: 2269
	public float m_pieceIconSpacing = 64f;

	// Token: 0x040008DE RID: 2270
	private float m_pieceBarTargetPosX;

	// Token: 0x040008DF RID: 2271
	private Piece.PieceCategory m_lastPieceCategory = Piece.PieceCategory.Max;

	// Token: 0x040008E0 RID: 2272
	[Header("Health")]
	public RectTransform m_healthBarRoot;

	// Token: 0x040008E1 RID: 2273
	public RectTransform m_healthPanel;

	// Token: 0x040008E2 RID: 2274
	private const float m_healthPanelBuffer = 56f;

	// Token: 0x040008E3 RID: 2275
	private const float m_healthPanelMinSize = 138f;

	// Token: 0x040008E4 RID: 2276
	public Animator m_healthAnimator;

	// Token: 0x040008E5 RID: 2277
	public GuiBar m_healthBarFast;

	// Token: 0x040008E6 RID: 2278
	public GuiBar m_healthBarSlow;

	// Token: 0x040008E7 RID: 2279
	public TMP_Text m_healthText;

	// Token: 0x040008E8 RID: 2280
	[Header("Food")]
	public Image[] m_foodBars;

	// Token: 0x040008E9 RID: 2281
	public Image[] m_foodIcons;

	// Token: 0x040008EA RID: 2282
	public TMP_Text[] m_foodTime;

	// Token: 0x040008EB RID: 2283
	public RectTransform m_foodBarRoot;

	// Token: 0x040008EC RID: 2284
	public RectTransform m_foodBaseBar;

	// Token: 0x040008ED RID: 2285
	public Image m_foodIcon;

	// Token: 0x040008EE RID: 2286
	public Color m_foodColorHungry = Color.white;

	// Token: 0x040008EF RID: 2287
	public Color m_foodColorFull = Color.white;

	// Token: 0x040008F0 RID: 2288
	public TMP_Text m_foodText;

	// Token: 0x040008F1 RID: 2289
	[Header("Action bar")]
	public GameObject m_actionBarRoot;

	// Token: 0x040008F2 RID: 2290
	public GuiBar m_actionProgress;

	// Token: 0x040008F3 RID: 2291
	public TMP_Text m_actionName;

	// Token: 0x040008F4 RID: 2292
	[Header("Stagger bar")]
	public Animator m_staggerAnimator;

	// Token: 0x040008F5 RID: 2293
	public GuiBar m_staggerProgress;

	// Token: 0x040008F6 RID: 2294
	[Header("Guardian power")]
	public RectTransform m_gpRoot;

	// Token: 0x040008F7 RID: 2295
	public TMP_Text m_gpName;

	// Token: 0x040008F8 RID: 2296
	public TMP_Text m_gpCooldown;

	// Token: 0x040008F9 RID: 2297
	public Image m_gpIcon;

	// Token: 0x040008FA RID: 2298
	[Header("Stamina")]
	public RectTransform m_staminaBar2Root;

	// Token: 0x040008FB RID: 2299
	public Animator m_staminaAnimator;

	// Token: 0x040008FC RID: 2300
	public GuiBar m_staminaBar2Fast;

	// Token: 0x040008FD RID: 2301
	public GuiBar m_staminaBar2Slow;

	// Token: 0x040008FE RID: 2302
	public TMP_Text m_staminaText;

	// Token: 0x040008FF RID: 2303
	private float m_staminaBarBorderBuffer = 16f;

	// Token: 0x04000900 RID: 2304
	[Header("Eitr")]
	public RectTransform m_eitrBarRoot;

	// Token: 0x04000901 RID: 2305
	public Animator m_eitrAnimator;

	// Token: 0x04000902 RID: 2306
	public GuiBar m_eitrBarFast;

	// Token: 0x04000903 RID: 2307
	public GuiBar m_eitrBarSlow;

	// Token: 0x04000904 RID: 2308
	public TMP_Text m_eitrText;

	// Token: 0x04000905 RID: 2309
	[Header("Mount")]
	public GameObject m_mountPanel;

	// Token: 0x04000906 RID: 2310
	public Image m_mountIcon;

	// Token: 0x04000907 RID: 2311
	public GuiBar m_mountHealthBarFast;

	// Token: 0x04000908 RID: 2312
	public GuiBar m_mountHealthBarSlow;

	// Token: 0x04000909 RID: 2313
	public TextMeshProUGUI m_mountHealthText;

	// Token: 0x0400090A RID: 2314
	public GuiBar m_mountStaminaBar;

	// Token: 0x0400090B RID: 2315
	public TextMeshProUGUI m_mountStaminaText;

	// Token: 0x0400090C RID: 2316
	public TextMeshProUGUI m_mountNameText;

	// Token: 0x0400090D RID: 2317
	[Header("Loading")]
	public CanvasGroup m_loadingScreen;

	// Token: 0x0400090E RID: 2318
	public GameObject m_loadingProgress;

	// Token: 0x0400090F RID: 2319
	public GameObject m_sleepingProgress;

	// Token: 0x04000910 RID: 2320
	public GameObject m_teleportingProgress;

	// Token: 0x04000911 RID: 2321
	public Image m_loadingImage;

	// Token: 0x04000912 RID: 2322
	public TMP_Text m_loadingTip;

	// Token: 0x04000913 RID: 2323
	public List<string> m_loadingTips = new List<string>();

	// Token: 0x04000914 RID: 2324
	private int m_currentLoadingTipIndex;

	// Token: 0x04000915 RID: 2325
	[Header("Crosshair")]
	public Image m_crosshair;

	// Token: 0x04000916 RID: 2326
	public Image m_crosshairBow;

	// Token: 0x04000917 RID: 2327
	public TextMeshProUGUI m_hoverName;

	// Token: 0x04000918 RID: 2328
	public RectTransform m_pieceHealthRoot;

	// Token: 0x04000919 RID: 2329
	public GuiBar m_pieceHealthBar;

	// Token: 0x0400091A RID: 2330
	public Image m_damageScreen;

	// Token: 0x0400091B RID: 2331
	public Image m_lavaWarningScreen;

	// Token: 0x0400091C RID: 2332
	[Header("Radial Menus")]
	public RadialBase m_radialMenu;

	// Token: 0x0400091D RID: 2333
	public OpenRadialConfig m_config;

	// Token: 0x0400091E RID: 2334
	[Header("Target")]
	public GameObject m_targetedAlert;

	// Token: 0x0400091F RID: 2335
	public GameObject m_targeted;

	// Token: 0x04000920 RID: 2336
	public GameObject m_hidden;

	// Token: 0x04000921 RID: 2337
	public GuiBar m_stealthBar;

	// Token: 0x04000922 RID: 2338
	[Header("Status effect")]
	public RectTransform m_statusEffectListRoot;

	// Token: 0x04000923 RID: 2339
	public RectTransform m_statusEffectTemplate;

	// Token: 0x04000924 RID: 2340
	public float m_statusEffectSpacing = 55f;

	// Token: 0x04000925 RID: 2341
	public int m_effectsPerRow = 7;

	// Token: 0x04000926 RID: 2342
	private List<RectTransform> m_statusEffects = new List<RectTransform>();

	// Token: 0x04000927 RID: 2343
	[Header("Ship hud")]
	public GameObject m_shipHudRoot;

	// Token: 0x04000928 RID: 2344
	public GameObject m_shipControlsRoot;

	// Token: 0x04000929 RID: 2345
	public GameObject m_rudderLeft;

	// Token: 0x0400092A RID: 2346
	public GameObject m_rudderRight;

	// Token: 0x0400092B RID: 2347
	public GameObject m_rudderSlow;

	// Token: 0x0400092C RID: 2348
	public GameObject m_rudderForward;

	// Token: 0x0400092D RID: 2349
	public GameObject m_rudderFastForward;

	// Token: 0x0400092E RID: 2350
	public GameObject m_rudderBackward;

	// Token: 0x0400092F RID: 2351
	public GameObject m_halfSail;

	// Token: 0x04000930 RID: 2352
	public GameObject m_fullSail;

	// Token: 0x04000931 RID: 2353
	public GameObject m_rudder;

	// Token: 0x04000932 RID: 2354
	public RectTransform m_shipWindIndicatorRoot;

	// Token: 0x04000933 RID: 2355
	public Image m_shipWindIcon;

	// Token: 0x04000934 RID: 2356
	public RectTransform m_shipWindIconRoot;

	// Token: 0x04000935 RID: 2357
	public Image m_shipRudderIndicator;

	// Token: 0x04000936 RID: 2358
	public Image m_shipRudderIcon;

	// Token: 0x04000937 RID: 2359
	[Header("Event")]
	public GameObject m_eventBar;

	// Token: 0x04000938 RID: 2360
	public TMP_Text m_eventName;

	// Token: 0x04000939 RID: 2361
	[NonSerialized]
	public bool m_userHidden;

	// Token: 0x0400093A RID: 2362
	private float m_hudPressed;

	// Token: 0x0400093B RID: 2363
	private CraftingStation m_currentCraftingStation;

	// Token: 0x0400093C RID: 2364
	private List<StatusEffect> m_tempStatusEffects = new List<StatusEffect>();

	// Token: 0x0400093D RID: 2365
	private List<Hud.PieceIconData> m_pieceIcons = new List<Hud.PieceIconData>();

	// Token: 0x0400093E RID: 2366
	private int m_pieceIconUpdateIndex;

	// Token: 0x0400093F RID: 2367
	private bool m_haveSetupLoadScreen;

	// Token: 0x04000940 RID: 2368
	private float m_staggerHideTimer = 99999f;

	// Token: 0x04000941 RID: 2369
	private float m_staminaHideTimer = 99999f;

	// Token: 0x04000942 RID: 2370
	private float m_eitrHideTimer = 99999f;

	// Token: 0x04000943 RID: 2371
	private int m_closePieceSelection;

	// Token: 0x04000944 RID: 2372
	private Piece m_hoveredPiece;

	// Token: 0x04000945 RID: 2373
	private const float minimumSaveIconDisplayTime = 3f;

	// Token: 0x04000946 RID: 2374
	private float m_saveIconTimer;

	// Token: 0x04000947 RID: 2375
	private bool m_worldSaving;

	// Token: 0x04000948 RID: 2376
	private bool m_fullyOpaqueSaveIcon;

	// Token: 0x04000949 RID: 2377
	private bool m_profileSaving;

	// Token: 0x0400094A RID: 2378
	private static Vector3 s_notVisiblePosition = new Vector3(10000f, 0f, 0f);

	// Token: 0x0400094B RID: 2379
	private static Color s_colorRedBlueZeroAlpha = new Color(1f, 0f, 1f, 0f);

	// Token: 0x0400094C RID: 2380
	private static Color s_colorRedish = new Color(1f, 0.5f, 0.5f, 1f);

	// Token: 0x0400094D RID: 2381
	private static Color s_shipWindIconColor = new Color(0.2f, 0.2f, 0.2f, 1f);

	// Token: 0x0400094E RID: 2382
	private static Color s_whiteHalfAlpha = new Color(1f, 1f, 1f, 0.5f);

	// Token: 0x0200027D RID: 637
	private class PieceIconData
	{
		// Token: 0x040021AD RID: 8621
		public GameObject m_go;

		// Token: 0x040021AE RID: 8622
		public Image m_icon;

		// Token: 0x040021AF RID: 8623
		public GameObject m_marker;

		// Token: 0x040021B0 RID: 8624
		public GameObject m_upgrade;

		// Token: 0x040021B1 RID: 8625
		public UITooltip m_tooltip;
	}
}
