using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

// Token: 0x02000121 RID: 289
public class GameCamera : MonoBehaviour
{
	// Token: 0x1700009F RID: 159
	// (get) Token: 0x0600126C RID: 4716 RVA: 0x0008930C File Offset: 0x0008750C
	public static GameCamera instance
	{
		get
		{
			return GameCamera.m_instance;
		}
	}

	// Token: 0x0600126D RID: 4717 RVA: 0x00089314 File Offset: 0x00087514
	private void Awake()
	{
		GameCamera.m_instance = this;
		this.m_camera = base.GetComponent<Camera>();
		this.m_listner = base.GetComponentInChildren<AudioListener>();
		this.m_heatDistortImageEffect = base.GetComponent<HeatDistortImageEffect>();
		this.m_camera.depthTextureMode = DepthTextureMode.DepthNormals;
		this.ApplySettings();
		if (!Application.isEditor)
		{
			this.m_mouseCapture = true;
		}
	}

	// Token: 0x0600126E RID: 4718 RVA: 0x0008936B File Offset: 0x0008756B
	private void OnDestroy()
	{
		if (GameCamera.m_instance == this)
		{
			GameCamera.m_instance = null;
		}
	}

	// Token: 0x0600126F RID: 4719 RVA: 0x00089380 File Offset: 0x00087580
	public void ApplySettings()
	{
		this.m_cameraShakeEnabled = (PlayerPrefs.GetInt("CameraShake", 1) == 1);
		this.m_shipCameraTilt = (PlayerPrefs.GetInt("ShipCameraTilt", 1) == 1);
	}

	// Token: 0x06001270 RID: 4720 RVA: 0x000893B4 File Offset: 0x000875B4
	private void LateUpdate()
	{
		float deltaTime = Time.deltaTime;
		if (ZInput.GetKeyDown(KeyCode.F11, true) || (this.m_freeFly && ZInput.GetKeyDown(KeyCode.Mouse1, true)))
		{
			GameCamera.ScreenShot();
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			this.UpdateBaseOffset(localPlayer, deltaTime);
		}
		this.UpdateMouseCapture();
		this.UpdateCamera(Time.unscaledDeltaTime);
		this.UpdateListner();
	}

	// Token: 0x06001271 RID: 4721 RVA: 0x0008941C File Offset: 0x0008761C
	private void UpdateMouseCapture()
	{
		if (ZInput.GetKey(KeyCode.LeftControl, true) && ZInput.GetKeyDown(KeyCode.F1, true))
		{
			this.m_mouseCapture = !this.m_mouseCapture;
		}
		if (this.m_mouseCapture && !Hud.InRadial() && !InventoryGui.IsVisible() && !TextInput.IsVisible() && !Menu.IsVisible() && !Minimap.IsOpen() && !StoreGui.IsVisible() && !Hud.IsPieceSelectionVisible() && !PlayerCustomizaton.BarberBlocksLook() && !UnifiedPopup.IsVisible() && !ZNet.IsPasswordDialogShowing())
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			return;
		}
		if (Hud.InRadial())
		{
			Cursor.lockState = (ZInput.IsMouseActive() ? CursorLockMode.None : CursorLockMode.Locked);
			Cursor.visible = ZInput.IsMouseActive();
			return;
		}
		if (!Menu.IsVisible() || UnifiedPopup.IsVisible())
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = ZInput.IsMouseActive();
		}
	}

	// Token: 0x06001272 RID: 4722 RVA: 0x000894F0 File Offset: 0x000876F0
	public static void ScreenShot()
	{
		DateTime now = DateTime.Now;
		Directory.CreateDirectory(Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/screenshots");
		string text = now.Hour.ToString("00") + now.Minute.ToString("00") + now.Second.ToString("00");
		string text2 = now.ToString("yyyy-MM-dd");
		string text3 = string.Concat(new string[]
		{
			Utils.GetSaveDataPath(FileHelpers.FileSource.Local),
			"/screenshots/screenshot_",
			text2,
			"_",
			text,
			".png"
		});
		if (File.Exists(text3))
		{
			return;
		}
		ScreenCapture.CaptureScreenshot(text3);
		ZLog.Log("Screenshot saved:" + text3);
	}

	// Token: 0x06001273 RID: 4723 RVA: 0x000895C0 File Offset: 0x000877C0
	private void UpdateListner()
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer && !this.m_freeFly)
		{
			this.m_listner.transform.position = localPlayer.m_eye.position;
			return;
		}
		this.m_listner.transform.localPosition = Vector3.zero;
	}

	// Token: 0x06001274 RID: 4724 RVA: 0x00089614 File Offset: 0x00087814
	private void UpdateCamera(float dt)
	{
		if (this.m_freeFly)
		{
			this.UpdateFreeFly(dt);
			this.UpdateCameraShake(dt);
			this.<UpdateCamera>g__debugCamera|9_0(ZInput.GetMouseScrollWheel());
			return;
		}
		this.m_camera.fieldOfView = this.m_fov;
		this.m_skyCamera.fieldOfView = this.m_fov;
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			if ((!Chat.instance || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !InventoryGui.IsVisible() && !StoreGui.IsVisible() && !Menu.IsVisible() && !Minimap.IsOpen() && !Hud.IsPieceSelectionVisible() && !Hud.InRadial() && !localPlayer.InCutscene() && (!localPlayer.InPlaceMode() || localPlayer.InRepairMode() || !localPlayer.CanRotatePiece() || localPlayer.GetPlacementStatus() == Player.PlacementStatus.NoRayHits || ZInput.IsGamepadActive()))
			{
				float minDistance = this.m_minDistance;
				float num = ZInput.GetMouseScrollWheel();
				num = Mathf.Clamp(num, -0.05f, 0.05f);
				if (Player.m_debugMode)
				{
					num = this.<UpdateCamera>g__debugCamera|9_0(num);
				}
				this.m_distance -= num * this.m_zoomSens;
				if (ZInput.GetButton("JoyAltKeys") && !Hud.InRadial())
				{
					if (ZInput.GetButton("JoyCamZoomIn"))
					{
						this.m_distance -= this.m_zoomSens * dt;
					}
					else if (ZInput.GetButton("JoyCamZoomOut"))
					{
						this.m_distance += this.m_zoomSens * dt;
					}
				}
				float max = (localPlayer.GetControlledShip() != null) ? this.m_maxDistanceBoat : this.m_maxDistance;
				this.m_distance = Mathf.Clamp(this.m_distance, minDistance, max);
			}
			if (localPlayer.IsDead() && localPlayer.GetRagdoll())
			{
				Vector3 averageBodyPosition = localPlayer.GetRagdoll().GetAverageBodyPosition();
				base.transform.LookAt(averageBodyPosition);
			}
			else if (localPlayer.IsAttached() && localPlayer.GetAttachCameraPoint() != null)
			{
				Transform attachCameraPoint = localPlayer.GetAttachCameraPoint();
				base.transform.position = attachCameraPoint.position;
				base.transform.rotation = attachCameraPoint.rotation;
			}
			else
			{
				Vector3 position;
				Quaternion rotation;
				this.GetCameraPosition(dt, out position, out rotation);
				base.transform.position = position;
				base.transform.rotation = rotation;
			}
			this.UpdateCameraShake(dt);
		}
	}

	// Token: 0x06001275 RID: 4725 RVA: 0x00089864 File Offset: 0x00087A64
	private void GetCameraPosition(float dt, out Vector3 pos, out Quaternion rot)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			pos = base.transform.position;
			rot = base.transform.rotation;
			return;
		}
		Vector3 vector = this.GetOffsetedEyePos();
		float num = this.m_distance;
		if (localPlayer.InIntro())
		{
			vector = localPlayer.transform.position;
			num = this.m_flyingDistance;
		}
		Vector3 vector2 = -localPlayer.m_eye.transform.forward;
		if (this.m_smoothYTilt && !localPlayer.InIntro())
		{
			num = Mathf.Lerp(num, 1.5f, Utils.SmoothStep(0f, -0.5f, vector2.y));
		}
		Vector3 vector3 = vector + vector2 * num;
		this.CollideRay2(localPlayer.m_eye.position, vector, ref vector3);
		this.UpdateNearClipping(vector, vector3, dt);
		float liquidLevel = Floating.GetLiquidLevel(vector3, 1f, LiquidType.All);
		if (vector3.y < liquidLevel + this.m_minWaterDistance)
		{
			vector3.y = liquidLevel + this.m_minWaterDistance;
			this.m_waterClipping = true;
		}
		else
		{
			this.m_waterClipping = false;
		}
		pos = vector3;
		rot = localPlayer.m_eye.transform.rotation;
		if (this.m_shipCameraTilt)
		{
			this.ApplyCameraTilt(localPlayer, dt, ref rot);
		}
	}

	// Token: 0x06001276 RID: 4726 RVA: 0x000899B4 File Offset: 0x00087BB4
	private void ApplyCameraTilt(Player player, float dt, ref Quaternion rot)
	{
		if (player.InIntro())
		{
			return;
		}
		Ship standingOnShip = player.GetStandingOnShip();
		float num = Mathf.Clamp01((this.m_distance - this.m_minDistance) / (this.m_maxDistanceBoat - this.m_minDistance));
		num = Mathf.Pow(num, 2f);
		float smoothTime = Mathf.Lerp(this.m_tiltSmoothnessShipMin, this.m_tiltSmoothnessShipMax, num);
		Vector3 up = Vector3.up;
		if (standingOnShip != null && standingOnShip.transform.up.y > 0f)
		{
			up = standingOnShip.transform.up;
		}
		else if (player.IsAttached())
		{
			up = player.GetVisual().transform.up;
		}
		Vector3 forward = player.m_eye.transform.forward;
		Vector3 target = Vector3.Lerp(up, Vector3.up, num * 0.5f);
		this.m_smoothedCameraUp = Vector3.SmoothDamp(this.m_smoothedCameraUp, target, ref this.m_smoothedCameraUpVel, smoothTime, 99f, dt);
		rot = Quaternion.LookRotation(forward, this.m_smoothedCameraUp);
	}

	// Token: 0x06001277 RID: 4727 RVA: 0x00089AB8 File Offset: 0x00087CB8
	private void UpdateNearClipping(Vector3 eyePos, Vector3 camPos, float dt)
	{
		float num = this.m_nearClipPlaneMax;
		Vector3 normalized = (camPos - eyePos).normalized;
		if (this.m_waterClipping || Physics.CheckSphere(camPos - normalized * this.m_nearClipPlaneMax, this.m_nearClipPlaneMax, this.m_blockCameraMask))
		{
			num = this.m_nearClipPlaneMin;
		}
		if (this.m_camera.nearClipPlane != num)
		{
			this.m_camera.nearClipPlane = num;
		}
	}

	// Token: 0x06001278 RID: 4728 RVA: 0x00089B30 File Offset: 0x00087D30
	private void CollideRay2(Vector3 eyePos, Vector3 offsetedEyePos, ref Vector3 end)
	{
		float num;
		if (this.RayTestPoint(eyePos, offsetedEyePos, (end - offsetedEyePos).normalized, Vector3.Distance(eyePos, end), out num))
		{
			float t = Utils.LerpStep(0.5f, 2f, num);
			Vector3 a = eyePos + (end - eyePos).normalized * num;
			Vector3 b = offsetedEyePos + (end - offsetedEyePos).normalized * num;
			end = Vector3.Lerp(a, b, t);
		}
	}

	// Token: 0x06001279 RID: 4729 RVA: 0x00089BCC File Offset: 0x00087DCC
	private bool RayTestPoint(Vector3 point, Vector3 offsetedPoint, Vector3 dir, float maxDist, out float distance)
	{
		bool flag = false;
		distance = maxDist;
		float num = ZoneSystem.instance.GetGroundOffset(point) * 1.6f;
		offsetedPoint += new Vector3(0f, -num, 0f);
		RaycastHit raycastHit;
		if (Physics.SphereCast(offsetedPoint, this.m_raycastWidth, dir, out raycastHit, maxDist, this.m_blockCameraMask))
		{
			distance = raycastHit.distance;
			flag = true;
		}
		offsetedPoint + dir * distance;
		if (Physics.SphereCast(point, this.m_raycastWidth, dir, out raycastHit, maxDist, this.m_blockCameraMask))
		{
			if (raycastHit.distance < distance)
			{
				distance = raycastHit.distance;
			}
			flag = true;
		}
		if (Physics.Raycast(point - new Vector3(0f, num, 0f), dir, out raycastHit, maxDist, this.m_blockCameraMask))
		{
			float num2 = raycastHit.distance - this.m_nearClipPlaneMin;
			if (num2 < distance)
			{
				distance = num2;
			}
			flag = true;
		}
		if (flag)
		{
			Vector3 position = point + dir.normalized * distance;
			float num3 = Mathf.Max(ZoneSystem.instance.GetGroundOffset(position) * 1.6f, num);
			if (num3 > 0f && Physics.Raycast(point + new Vector3(0f, -num3, 0f), dir, out raycastHit, maxDist, this.m_blockCameraMask))
			{
				float num4 = raycastHit.distance - this.m_nearClipPlaneMin;
				if (num4 < distance)
				{
					distance = num4;
				}
			}
		}
		return flag;
	}

	// Token: 0x0600127A RID: 4730 RVA: 0x00089D50 File Offset: 0x00087F50
	private bool RayTestPoint(Vector3 point, Vector3 dir, float maxDist, out Vector3 hitPoint)
	{
		RaycastHit raycastHit;
		if (Physics.SphereCast(point, 0.2f, dir, out raycastHit, maxDist, this.m_blockCameraMask))
		{
			hitPoint = point + dir * raycastHit.distance;
			return true;
		}
		if (Physics.Raycast(point, dir, out raycastHit, maxDist, this.m_blockCameraMask))
		{
			hitPoint = point + dir * (raycastHit.distance - 0.05f);
			return true;
		}
		hitPoint = Vector3.zero;
		return false;
	}

	// Token: 0x0600127B RID: 4731 RVA: 0x00089DDC File Offset: 0x00087FDC
	private void UpdateFreeFly(float dt)
	{
		if (global::Console.IsVisible())
		{
			return;
		}
		Vector2 vector = Vector2.zero;
		vector = ZInput.GetMouseDelta();
		vector.x += ZInput.GetJoyRightStickX(true) * 110f * dt;
		vector.y += -ZInput.GetJoyRightStickY(true) * 110f * dt;
		this.m_freeFlyYaw += vector.x;
		this.m_freeFlyPitch -= vector.y;
		if (ZInput.GetMouseScrollWheel() < 0f)
		{
			this.m_freeFlySpeed *= 0.8f;
		}
		if (ZInput.GetMouseScrollWheel() > 0f)
		{
			this.m_freeFlySpeed *= 1.2f;
		}
		if (ZInput.GetMouseScrollWheel() > 0f)
		{
			this.m_freeFlySpeed *= 1.2f;
		}
		if (ZInput.GetButton("JoyTabLeft"))
		{
			this.m_camera.fieldOfView = Mathf.Max(this.m_freeFlyMinFov, this.m_camera.fieldOfView - dt * 20f);
		}
		if (ZInput.GetButton("JoyTabRight"))
		{
			this.m_camera.fieldOfView = Mathf.Min(this.m_freeFlyMaxFov, this.m_camera.fieldOfView + dt * 20f);
		}
		this.m_skyCamera.fieldOfView = this.m_camera.fieldOfView;
		if (ZInput.GetButton("JoyButtonY"))
		{
			this.m_freeFlySpeed += this.m_freeFlySpeed * 0.1f * dt * 10f;
		}
		if (ZInput.GetButton("JoyButtonX"))
		{
			this.m_freeFlySpeed -= this.m_freeFlySpeed * 0.1f * dt * 10f;
		}
		this.m_freeFlySpeed = Mathf.Clamp(this.m_freeFlySpeed, 1f, 1000f);
		if (ZInput.GetButtonDown("JoyLStick") || ZInput.GetButtonDown("SecondaryAttack"))
		{
			if (this.m_freeFlyLockon)
			{
				this.m_freeFlyLockon = null;
			}
			else
			{
				int mask = LayerMask.GetMask(new string[]
				{
					"Default",
					"static_solid",
					"terrain",
					"vehicle",
					"character",
					"piece",
					"character_net",
					"viewblock"
				});
				RaycastHit raycastHit;
				if (Physics.Raycast(base.transform.position, base.transform.forward, out raycastHit, 10000f, mask))
				{
					this.m_freeFlyLockon = raycastHit.collider.transform;
					this.m_freeFlyLockonOffset = this.m_freeFlyLockon.InverseTransformPoint(base.transform.position);
				}
			}
		}
		Vector3 vector2 = Vector3.zero;
		if (ZInput.GetButton("Left"))
		{
			vector2 -= Vector3.right;
		}
		if (ZInput.GetButton("Right"))
		{
			vector2 += Vector3.right;
		}
		if (ZInput.GetButton("Forward"))
		{
			vector2 += Vector3.forward;
		}
		if (ZInput.GetButton("Backward"))
		{
			vector2 -= Vector3.forward;
		}
		if (ZInput.GetButton("Jump"))
		{
			vector2 += Vector3.up;
		}
		if (ZInput.GetButton("Crouch"))
		{
			vector2 -= Vector3.up;
		}
		vector2 += Vector3.up * ZInput.GetJoyRTrigger();
		vector2 -= Vector3.up * ZInput.GetJoyLTrigger();
		vector2 += Vector3.right * ZInput.GetJoyLeftStickX(false);
		vector2 += -Vector3.forward * ZInput.GetJoyLeftStickY(true);
		if (ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("Block"))
		{
			this.m_freeFlySavedVel = vector2;
		}
		float magnitude = this.m_freeFlySavedVel.magnitude;
		if (magnitude > 0.001f)
		{
			vector2 += this.m_freeFlySavedVel;
			if (vector2.magnitude > magnitude)
			{
				vector2 = vector2.normalized * magnitude;
			}
		}
		if (vector2.magnitude > 1f)
		{
			vector2.Normalize();
		}
		vector2 = base.transform.TransformVector(vector2);
		vector2 *= this.m_freeFlySpeed;
		if (this.m_freeFlySmooth <= 0f)
		{
			this.m_freeFlyVel = vector2;
		}
		else
		{
			this.m_freeFlyVel = Vector3.SmoothDamp(this.m_freeFlyVel, vector2, ref this.m_freeFlyAcc, this.m_freeFlySmooth, 99f, dt);
		}
		if (this.m_freeFlyLockon)
		{
			this.m_freeFlyLockonOffset += this.m_freeFlyLockon.InverseTransformVector(this.m_freeFlyVel * dt);
			base.transform.position = this.m_freeFlyLockon.TransformPoint(this.m_freeFlyLockonOffset);
		}
		else
		{
			base.transform.position = base.transform.position + this.m_freeFlyVel * dt;
		}
		Quaternion quaternion = Quaternion.Euler(0f, this.m_freeFlyYaw, 0f) * Quaternion.Euler(this.m_freeFlyPitch, 0f, 0f);
		if (this.m_freeFlyLockon)
		{
			quaternion = this.m_freeFlyLockon.rotation * quaternion;
		}
		if ((ZInput.GetButtonDown("JoyRStick") && !ZInput.GetButton("JoyAltKeys")) || ZInput.GetButtonDown("Attack"))
		{
			if (this.m_freeFlyTarget)
			{
				this.m_freeFlyTarget = null;
			}
			else
			{
				int mask2 = LayerMask.GetMask(new string[]
				{
					"Default",
					"static_solid",
					"terrain",
					"vehicle",
					"character",
					"piece",
					"character_net",
					"viewblock"
				});
				RaycastHit raycastHit2;
				if (Physics.Raycast(base.transform.position, base.transform.forward, out raycastHit2, 10000f, mask2))
				{
					this.m_freeFlyTarget = raycastHit2.collider.transform;
					this.m_freeFlyTargetOffset = this.m_freeFlyTarget.InverseTransformPoint(raycastHit2.point);
				}
			}
		}
		if (this.m_freeFlyTarget)
		{
			quaternion = Quaternion.LookRotation((this.m_freeFlyTarget.TransformPoint(this.m_freeFlyTargetOffset) - base.transform.position).normalized, Vector3.up);
		}
		if (this.m_freeFlySmooth <= 0f)
		{
			base.transform.rotation = quaternion;
			return;
		}
		Quaternion rotation = Utils.SmoothDamp(base.transform.rotation, quaternion, ref this.m_freeFlyRef, this.m_freeFlySmooth, 9999f, dt);
		base.transform.rotation = rotation;
	}

	// Token: 0x0600127C RID: 4732 RVA: 0x0008A464 File Offset: 0x00088664
	private void UpdateCameraShake(float dt)
	{
		this.m_shakeIntensity -= dt;
		if (this.m_shakeIntensity <= 0f)
		{
			this.m_shakeIntensity = 0f;
			return;
		}
		float num = this.m_shakeIntensity * this.m_shakeIntensity * this.m_shakeIntensity;
		this.m_shakeTimer += dt * Mathf.Clamp01(this.m_shakeIntensity) * this.m_shakeFreq;
		Quaternion rhs = Quaternion.Euler(Mathf.Sin(this.m_shakeTimer) * num * this.m_shakeMovement, Mathf.Cos(this.m_shakeTimer * 0.9f) * num * this.m_shakeMovement, 0f);
		base.transform.rotation = base.transform.rotation * rhs;
	}

	// Token: 0x0600127D RID: 4733 RVA: 0x0008A524 File Offset: 0x00088724
	public void AddShake(Vector3 point, float range, float strength, bool continous)
	{
		if (!this.m_cameraShakeEnabled)
		{
			return;
		}
		float num = Vector3.Distance(point, base.transform.position);
		if (num > range)
		{
			return;
		}
		num = Mathf.Max(1f, num);
		float num2 = 1f - num / range;
		float num3 = strength * num2;
		if (num3 < this.m_shakeIntensity)
		{
			return;
		}
		this.m_shakeIntensity = num3;
		if (continous)
		{
			this.m_shakeTimer = Time.time * Mathf.Clamp01(strength) * this.m_shakeFreq;
			return;
		}
		this.m_shakeTimer = Time.time * Mathf.Clamp01(this.m_shakeIntensity) * this.m_shakeFreq;
	}

	// Token: 0x0600127E RID: 4734 RVA: 0x0008A5B8 File Offset: 0x000887B8
	private float RayTest(Vector3 point, Vector3 dir, float maxDist)
	{
		RaycastHit raycastHit;
		if (Physics.SphereCast(point, 0.2f, dir, out raycastHit, maxDist, this.m_blockCameraMask))
		{
			return raycastHit.distance;
		}
		return maxDist;
	}

	// Token: 0x0600127F RID: 4735 RVA: 0x0008A5EC File Offset: 0x000887EC
	private Vector3 GetCameraBaseOffset(Player player)
	{
		if (player.InBed())
		{
			return player.GetHeadPoint() - player.transform.position;
		}
		if (player.IsAttached() || player.IsSitting())
		{
			return player.GetHeadPoint() + Vector3.up * 0.3f - player.transform.position;
		}
		return player.m_eye.transform.position - player.transform.position;
	}

	// Token: 0x06001280 RID: 4736 RVA: 0x0008A674 File Offset: 0x00088874
	private void UpdateBaseOffset(Player player, float dt)
	{
		Vector3 cameraBaseOffset = this.GetCameraBaseOffset(player);
		this.m_currentBaseOffset = Vector3.SmoothDamp(this.m_currentBaseOffset, cameraBaseOffset, ref this.m_offsetBaseVel, 0.5f, 999f, dt);
		if (Vector3.Distance(this.m_playerPos, player.transform.position) > 20f)
		{
			this.m_playerPos = player.transform.position;
		}
		this.m_playerPos = Vector3.SmoothDamp(this.m_playerPos, player.transform.position, ref this.m_playerVel, this.m_smoothness, 999f, dt);
	}

	// Token: 0x06001281 RID: 4737 RVA: 0x0008A708 File Offset: 0x00088908
	private Vector3 GetOffsetedEyePos()
	{
		Player localPlayer = Player.m_localPlayer;
		if (!localPlayer)
		{
			return base.transform.position;
		}
		if (localPlayer.GetStandingOnShip() != null || localPlayer.IsAttached())
		{
			return localPlayer.transform.position + this.m_currentBaseOffset + this.GetCameraOffset(localPlayer);
		}
		return this.m_playerPos + this.m_currentBaseOffset + this.GetCameraOffset(localPlayer);
	}

	// Token: 0x06001282 RID: 4738 RVA: 0x0008A788 File Offset: 0x00088988
	private Vector3 GetCameraOffset(Player player)
	{
		if (this.m_distance <= 0f)
		{
			return player.m_eye.transform.TransformVector(this.m_fpsOffset);
		}
		if (player.InBed())
		{
			return Vector3.zero;
		}
		Vector3 vector = player.UseMeleeCamera() ? this.m_3rdCombatOffset : this.m_3rdOffset;
		return player.m_eye.transform.TransformVector(vector);
	}

	// Token: 0x06001283 RID: 4739 RVA: 0x0008A7EF File Offset: 0x000889EF
	public void ToggleFreeFly()
	{
		this.m_freeFly = !this.m_freeFly;
	}

	// Token: 0x06001284 RID: 4740 RVA: 0x0008A800 File Offset: 0x00088A00
	public void SetFreeFlySmoothness(float smooth)
	{
		this.m_freeFlySmooth = Mathf.Clamp(smooth, 0f, 1f);
	}

	// Token: 0x06001285 RID: 4741 RVA: 0x0008A818 File Offset: 0x00088A18
	public float GetFreeFlySmoothness()
	{
		return this.m_freeFlySmooth;
	}

	// Token: 0x06001286 RID: 4742 RVA: 0x0008A820 File Offset: 0x00088A20
	public static bool InFreeFly()
	{
		return GameCamera.m_instance && GameCamera.m_instance.m_freeFly;
	}

	// Token: 0x06001288 RID: 4744 RVA: 0x0008A9E0 File Offset: 0x00088BE0
	[CompilerGenerated]
	private float <UpdateCamera>g__debugCamera|9_0(float scroll)
	{
		if (ZInput.GetKey(KeyCode.LeftShift, true) && ZInput.GetKey(KeyCode.C, true) && !global::Console.IsVisible())
		{
			Vector2 mouseDelta = ZInput.GetMouseDelta();
			EnvMan.instance.m_debugTimeOfDay = true;
			EnvMan.instance.m_debugTime = (EnvMan.instance.m_debugTime + mouseDelta.y * 0.005f) % 1f;
			if (EnvMan.instance.m_debugTime < 0f)
			{
				EnvMan.instance.m_debugTime += 1f;
			}
			this.m_fov += mouseDelta.x * 1f;
			this.m_fov = Mathf.Clamp(this.m_fov, 0.5f, 165f);
			this.m_camera.fieldOfView = this.m_fov;
			this.m_skyCamera.fieldOfView = this.m_fov;
			if (Player.m_localPlayer && Player.m_localPlayer.IsDebugFlying())
			{
				if (scroll > 0f)
				{
					Character.m_debugFlySpeed = (int)Mathf.Clamp((float)Character.m_debugFlySpeed * 1.1f, (float)(Character.m_debugFlySpeed + 1), 300f);
				}
				else if (scroll < 0f && Character.m_debugFlySpeed > 1)
				{
					Character.m_debugFlySpeed = (int)Mathf.Min((float)Character.m_debugFlySpeed * 0.9f, (float)(Character.m_debugFlySpeed - 1));
				}
			}
			scroll = 0f;
		}
		return scroll;
	}

	// Token: 0x0400120D RID: 4621
	private Vector3 m_playerPos = Vector3.zero;

	// Token: 0x0400120E RID: 4622
	private Vector3 m_currentBaseOffset = Vector3.zero;

	// Token: 0x0400120F RID: 4623
	private Vector3 m_offsetBaseVel = Vector3.zero;

	// Token: 0x04001210 RID: 4624
	private Vector3 m_playerVel = Vector3.zero;

	// Token: 0x04001211 RID: 4625
	public Vector3 m_3rdOffset = Vector3.zero;

	// Token: 0x04001212 RID: 4626
	public Vector3 m_3rdCombatOffset = Vector3.zero;

	// Token: 0x04001213 RID: 4627
	public Vector3 m_fpsOffset = Vector3.zero;

	// Token: 0x04001214 RID: 4628
	public float m_flyingDistance = 15f;

	// Token: 0x04001215 RID: 4629
	public LayerMask m_blockCameraMask;

	// Token: 0x04001216 RID: 4630
	public float m_minDistance;

	// Token: 0x04001217 RID: 4631
	public float m_maxDistance = 6f;

	// Token: 0x04001218 RID: 4632
	public float m_maxDistanceBoat = 6f;

	// Token: 0x04001219 RID: 4633
	public float m_raycastWidth = 0.35f;

	// Token: 0x0400121A RID: 4634
	public bool m_smoothYTilt;

	// Token: 0x0400121B RID: 4635
	public float m_zoomSens = 10f;

	// Token: 0x0400121C RID: 4636
	public float m_inventoryOffset = 0.1f;

	// Token: 0x0400121D RID: 4637
	public float m_nearClipPlaneMin = 0.1f;

	// Token: 0x0400121E RID: 4638
	public float m_nearClipPlaneMax = 0.5f;

	// Token: 0x0400121F RID: 4639
	public float m_fov = 65f;

	// Token: 0x04001220 RID: 4640
	public float m_freeFlyMinFov = 5f;

	// Token: 0x04001221 RID: 4641
	public float m_freeFlyMaxFov = 120f;

	// Token: 0x04001222 RID: 4642
	public float m_tiltSmoothnessShipMin = 0.1f;

	// Token: 0x04001223 RID: 4643
	public float m_tiltSmoothnessShipMax = 0.5f;

	// Token: 0x04001224 RID: 4644
	public float m_shakeFreq = 10f;

	// Token: 0x04001225 RID: 4645
	public float m_shakeMovement = 1f;

	// Token: 0x04001226 RID: 4646
	public float m_smoothness = 0.1f;

	// Token: 0x04001227 RID: 4647
	public float m_minWaterDistance = 0.3f;

	// Token: 0x04001228 RID: 4648
	public Camera m_skyCamera;

	// Token: 0x04001229 RID: 4649
	private float m_distance = 4f;

	// Token: 0x0400122A RID: 4650
	private bool m_freeFly;

	// Token: 0x0400122B RID: 4651
	private float m_shakeIntensity;

	// Token: 0x0400122C RID: 4652
	private float m_shakeTimer;

	// Token: 0x0400122D RID: 4653
	private bool m_cameraShakeEnabled = true;

	// Token: 0x0400122E RID: 4654
	private bool m_mouseCapture;

	// Token: 0x0400122F RID: 4655
	private Quaternion m_freeFlyRef = Quaternion.identity;

	// Token: 0x04001230 RID: 4656
	private float m_freeFlyYaw;

	// Token: 0x04001231 RID: 4657
	private float m_freeFlyPitch;

	// Token: 0x04001232 RID: 4658
	private float m_freeFlySpeed = 20f;

	// Token: 0x04001233 RID: 4659
	private float m_freeFlySmooth;

	// Token: 0x04001234 RID: 4660
	private Vector3 m_freeFlySavedVel = Vector3.zero;

	// Token: 0x04001235 RID: 4661
	private Transform m_freeFlyTarget;

	// Token: 0x04001236 RID: 4662
	private Vector3 m_freeFlyTargetOffset = Vector3.zero;

	// Token: 0x04001237 RID: 4663
	private Transform m_freeFlyLockon;

	// Token: 0x04001238 RID: 4664
	private Vector3 m_freeFlyLockonOffset = Vector3.zero;

	// Token: 0x04001239 RID: 4665
	private Vector3 m_freeFlyVel = Vector3.zero;

	// Token: 0x0400123A RID: 4666
	private Vector3 m_freeFlyAcc = Vector3.zero;

	// Token: 0x0400123B RID: 4667
	private Vector3 m_freeFlyTurnVel = Vector3.zero;

	// Token: 0x0400123C RID: 4668
	private bool m_shipCameraTilt = true;

	// Token: 0x0400123D RID: 4669
	private Vector3 m_smoothedCameraUp = Vector3.up;

	// Token: 0x0400123E RID: 4670
	private Vector3 m_smoothedCameraUpVel = Vector3.zero;

	// Token: 0x0400123F RID: 4671
	private AudioListener m_listner;

	// Token: 0x04001240 RID: 4672
	private Camera m_camera;

	// Token: 0x04001241 RID: 4673
	private bool m_waterClipping;

	// Token: 0x04001242 RID: 4674
	private bool m_camZoomToggle;

	// Token: 0x04001243 RID: 4675
	public HeatDistortImageEffect m_heatDistortImageEffect;

	// Token: 0x04001244 RID: 4676
	private static GameCamera m_instance;
}
