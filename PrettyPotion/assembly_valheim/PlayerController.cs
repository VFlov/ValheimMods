using System;
using UnityEngine;
using Valheim.SettingsGui;

// Token: 0x02000029 RID: 41
public class PlayerController : MonoBehaviour
{
	// Token: 0x06000449 RID: 1097 RVA: 0x00026F94 File Offset: 0x00025194
	private void Awake()
	{
		this.m_character = base.GetComponent<Player>();
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		if (!PlayerPrefs.HasKey("MouseSensitivity"))
		{
			KeyboardMouseSettings.SetPlatformSpecificFirstTimeSettings();
		}
		PlayerController.m_mouseSens = PlayerPrefs.GetFloat("MouseSensitivity", PlayerController.m_mouseSens);
		PlayerController.m_gamepadSens = PlayerPrefs.GetFloat("GamepadSensitivity", PlayerController.m_gamepadSens);
		PlayerController.m_invertMouse = (PlayerPrefs.GetInt("InvertMouse", 0) == 1);
		PlayerController.m_invertCameraY = (PlayerPrefs.GetInt("InvertCameraY", PlayerController.m_invertMouse ? 1 : 0) == 1);
		PlayerController.m_invertCameraX = (PlayerPrefs.GetInt("InvertCameraX", 0) == 1);
	}

	// Token: 0x0600044A RID: 1098 RVA: 0x0002704C File Offset: 0x0002524C
	private void FixedUpdate()
	{
		PlayerController.takeInputDelay = Mathf.Max(0f, PlayerController.takeInputDelay - Time.deltaTime);
		if (this.m_nview && !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.TakeInput(false))
		{
			this.m_character.SetControls(Vector3.zero, false, false, false, false, false, false, false, false, false, false, false);
			return;
		}
		bool flag = this.InInventoryEtc();
		bool flag2 = Hud.IsPieceSelectionVisible();
		bool flag3 = (ZInput.GetButton("SecondaryAttack") || ZInput.GetButton("JoySecondaryAttack")) && !flag && !Hud.InRadial();
		Vector3 zero = Vector3.zero;
		if (ZInput.GetButton("Forward"))
		{
			zero.z += 1f;
		}
		if (ZInput.GetButton("Backward"))
		{
			zero.z -= 1f;
		}
		if (ZInput.GetButton("Left"))
		{
			zero.x -= 1f;
		}
		if (ZInput.GetButton("Right"))
		{
			zero.x += 1f;
		}
		zero.x += ZInput.GetJoyLeftStickX(false);
		zero.z += -ZInput.GetJoyLeftStickY(true);
		if (zero.magnitude > 1f)
		{
			zero.Normalize();
		}
		bool flag4 = (ZInput.GetButton("Attack") || ZInput.GetButton("JoyAttack")) && !flag && !Hud.InRadial();
		bool attackHold = flag4;
		bool attack = flag4 && !this.m_attackWasPressed;
		this.m_attackWasPressed = flag4;
		bool secondaryAttackHold = flag3;
		bool secondaryAttack = flag3 && !this.m_secondAttackWasPressed;
		this.m_secondAttackWasPressed = flag3;
		bool flag5 = (ZInput.GetButton("Block") || ZInput.GetButton("JoyBlock")) && !flag && !Hud.InRadial();
		bool blockHold = flag5;
		bool block = flag5 && !this.m_blockWasPressed;
		this.m_blockWasPressed = flag5;
		bool button = ZInput.GetButton("Jump");
		bool jump = (button && !this.m_lastJump) || (ZInput.GetButtonDown("JoyJump") && !flag2 && !flag && !Hud.InRadial());
		this.m_lastJump = button;
		bool dodge = ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive() && ZInput.GetButtonDown("JoyDodge") && !flag && !Hud.InRadial();
		bool flag6 = InventoryGui.IsVisible();
		bool flag7 = (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch")) && !flag6 && !Hud.InRadial();
		bool crouch = flag7 && !this.m_lastCrouch;
		this.m_lastCrouch = flag7;
		bool flag8 = ZInput.GetButton("Run") || ZInput.GetButton("JoyRun");
		if (!this.m_lastRunPressed && flag8 && this.m_character.GetStamina() > 0f)
		{
			this.m_runPressedWhileStamina = true;
		}
		else if (this.m_character.GetStamina() <= 0f)
		{
			this.m_runPressedWhileStamina = false;
		}
		if (ZInput.ToggleRun)
		{
			if (!this.m_lastRunPressed && flag8)
			{
				this.m_run = !this.m_run;
			}
			if (this.m_character.GetStamina() <= 0f)
			{
				this.m_run = false;
			}
		}
		else
		{
			this.m_run = (flag8 && this.m_runPressedWhileStamina);
		}
		float magnitude = zero.magnitude;
		if (magnitude < 0.05f && this.m_lastMagnitude < 0.05f && !this.m_character.m_autoRun)
		{
			this.m_run = false;
		}
		this.m_lastRunPressed = flag8;
		this.m_lastMagnitude = magnitude;
		this.m_lastRunPressed = flag8;
		bool button2 = ZInput.GetButton("AutoRun");
		if (PlayerController.takeInputDelay > 0f)
		{
			this.m_character.SetControls(zero, false, false, false, false, false, false, false, false, this.m_run, button2, false);
			return;
		}
		this.m_character.SetControls(zero, attack, attackHold, secondaryAttack, secondaryAttackHold, block, blockHold, jump, crouch, this.m_run, button2, dodge);
	}

	// Token: 0x0600044B RID: 1099 RVA: 0x00027448 File Offset: 0x00025648
	private static bool DetectTap(bool pressed, float dt, float minPressTime, bool run, ref float pressTimer, ref float releasedTimer, ref bool tapPressed)
	{
		bool result = false;
		if (pressed)
		{
			if ((releasedTimer > 0f && releasedTimer < minPressTime) & tapPressed)
			{
				tapPressed = false;
				result = true;
			}
			pressTimer += dt;
			releasedTimer = 0f;
		}
		else
		{
			if (pressTimer > 0f)
			{
				tapPressed = (pressTimer < minPressTime);
				if (run & tapPressed)
				{
					tapPressed = false;
					result = true;
				}
			}
			releasedTimer += dt;
			pressTimer = 0f;
		}
		return result;
	}

	// Token: 0x0600044C RID: 1100 RVA: 0x000274BC File Offset: 0x000256BC
	private bool TakeInput(bool look = false)
	{
		return !GameCamera.InFreeFly() && ((!Chat.instance || !Chat.instance.HasFocus()) && !Menu.IsVisible() && !global::Console.IsVisible() && !TextInput.IsVisible() && !Minimap.InTextInput() && (!ZInput.IsGamepadActive() || !Minimap.IsOpen()) && (!ZInput.IsGamepadActive() || !InventoryGui.IsVisible()) && (!ZInput.IsGamepadActive() || !StoreGui.IsVisible()) && (!ZInput.IsGamepadActive() || !Hud.IsPieceSelectionVisible())) && (!PlayerCustomizaton.IsBarberGuiVisible() || look) && (!PlayerCustomizaton.BarberBlocksLook() || !look);
	}

	// Token: 0x0600044D RID: 1101 RVA: 0x00027556 File Offset: 0x00025756
	private bool InInventoryEtc()
	{
		return InventoryGui.IsVisible() || Minimap.IsOpen() || StoreGui.IsVisible() || Hud.IsPieceSelectionVisible();
	}

	// Token: 0x1700000F RID: 15
	// (get) Token: 0x0600044E RID: 1102 RVA: 0x00027574 File Offset: 0x00025774
	public static bool HasInputDelay
	{
		get
		{
			return PlayerController.takeInputDelay > 0f;
		}
	}

	// Token: 0x0600044F RID: 1103 RVA: 0x00027582 File Offset: 0x00025782
	public static void SetTakeInputDelay(float delayInSeconds)
	{
		PlayerController.takeInputDelay = delayInSeconds;
	}

	// Token: 0x06000450 RID: 1104 RVA: 0x0002758C File Offset: 0x0002578C
	private void LateUpdate()
	{
		if ((Hud.InRadial() && Hud.instance.m_radialMenu.IsBlockingInput) || (PlayerController.takeInputDelay > 0f && ZInput.IsGamepadActive()))
		{
			return;
		}
		if (!this.TakeInput(true) || this.InInventoryEtc())
		{
			this.m_character.SetMouseLook(Vector2.zero);
			return;
		}
		Vector2 mouseLook = Vector2.zero;
		if (ZInput.IsGamepadActive() && !Hud.InRadial())
		{
			if (!this.m_character.InPlaceMode() || !ZInput.GetButton("JoyRotate"))
			{
				if (!PlayerController.cameraDirectionLock.Equals(Vector2.zero))
				{
					Vector2 vector = new Vector2(ZInput.GetJoyRightStickX(true), -ZInput.GetJoyRightStickY(true));
					if (!vector.Equals(PlayerController.cameraDirectionLock))
					{
						PlayerController.cameraDirectionLock = Vector2.zero;
						mouseLook.x += ZInput.GetJoyRightStickX(true) * 110f * Time.deltaTime * PlayerController.m_gamepadSens;
						mouseLook.y += -ZInput.GetJoyRightStickY(true) * 110f * Time.deltaTime * PlayerController.m_gamepadSens;
					}
				}
				else
				{
					mouseLook.x += ZInput.GetJoyRightStickX(true) * 110f * Time.deltaTime * PlayerController.m_gamepadSens;
					mouseLook.y += -ZInput.GetJoyRightStickY(true) * 110f * Time.deltaTime * PlayerController.m_gamepadSens;
				}
			}
			if (PlayerController.m_invertCameraX)
			{
				mouseLook.x *= -1f;
			}
			if (PlayerController.m_invertCameraY)
			{
				mouseLook.y *= -1f;
			}
		}
		else if (!Hud.InRadial())
		{
			mouseLook = ZInput.GetMouseDelta() * PlayerController.m_mouseSens;
			if (PlayerController.m_invertMouse)
			{
				mouseLook.y *= -1f;
			}
		}
		this.m_character.SetMouseLook(mouseLook);
	}

	// Token: 0x040004C8 RID: 1224
	private bool m_run;

	// Token: 0x040004C9 RID: 1225
	private bool m_runToggled;

	// Token: 0x040004CA RID: 1226
	private bool m_lastRunPressed;

	// Token: 0x040004CB RID: 1227
	private float m_lastMagnitude;

	// Token: 0x040004CC RID: 1228
	private bool m_runPressedWhileStamina = true;

	// Token: 0x040004CD RID: 1229
	private static float takeInputDelay = 0f;

	// Token: 0x040004CE RID: 1230
	private Player m_character;

	// Token: 0x040004CF RID: 1231
	private ZNetView m_nview;

	// Token: 0x040004D0 RID: 1232
	public static Vector2 cameraDirectionLock = Vector2.zero;

	// Token: 0x040004D1 RID: 1233
	public static float m_mouseSens = 1f;

	// Token: 0x040004D2 RID: 1234
	public static float m_gamepadSens = 1f;

	// Token: 0x040004D3 RID: 1235
	public static bool m_invertMouse = false;

	// Token: 0x040004D4 RID: 1236
	public static bool m_invertCameraY = false;

	// Token: 0x040004D5 RID: 1237
	public static bool m_invertCameraX = false;

	// Token: 0x040004D6 RID: 1238
	public float m_minDodgeTime = 0.2f;

	// Token: 0x040004D7 RID: 1239
	private bool m_attackWasPressed;

	// Token: 0x040004D8 RID: 1240
	private bool m_secondAttackWasPressed;

	// Token: 0x040004D9 RID: 1241
	private bool m_blockWasPressed;

	// Token: 0x040004DA RID: 1242
	private bool m_lastJump;

	// Token: 0x040004DB RID: 1243
	private bool m_lastCrouch;
}
