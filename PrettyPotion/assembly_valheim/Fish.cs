using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200017A RID: 378
public class Fish : MonoBehaviour, IWaterInteractable, Hoverable, Interactable, IMonoUpdater
{
	// Token: 0x060016BA RID: 5818 RVA: 0x000A89DC File Offset: 0x000A6BDC
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_itemDrop = base.GetComponent<ItemDrop>();
		this.m_lodGroup = base.GetComponent<LODGroup>();
		if (this.m_itemDrop)
		{
			if (this.m_itemDrop.m_itemData.m_quality > 1)
			{
				this.m_itemDrop.SetQuality(this.m_itemDrop.m_itemData.m_quality);
			}
			ItemDrop itemDrop = this.m_itemDrop;
			itemDrop.m_onDrop = (Action<ItemDrop>)Delegate.Combine(itemDrop.m_onDrop, new Action<ItemDrop>(this.onDrop));
			if (this.m_pickupItem == null)
			{
				this.m_pickupItem = base.gameObject;
			}
		}
		this.m_waterWaveCount = UnityEngine.Random.Range(0, 1);
		if (this.m_lodGroup)
		{
			this.m_originalLocalRef = this.m_lodGroup.localReferencePoint;
		}
	}

	// Token: 0x060016BB RID: 5819 RVA: 0x000A8AC0 File Offset: 0x000A6CC0
	private void Start()
	{
		this.m_spawnPoint = this.m_nview.GetZDO().GetVec3(ZDOVars.s_spawnPoint, base.transform.position);
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnPoint, this.m_spawnPoint);
			this.RandomizeWaypoint(true, Time.time);
		}
		if (this.m_nview.IsValid())
		{
			this.m_nview.Register("RequestPickup", new Action<long>(this.RPC_RequestPickup));
			this.m_nview.Register("Pickup", new Action<long>(this.RPC_Pickup));
		}
		if (this.m_waterVolume != null)
		{
			this.m_waterDepth = this.m_waterVolume.Depth(base.transform.position);
			this.m_waterWave = this.m_waterVolume.CalcWave(base.transform.position, this.m_waterDepth, Fish.s_wrappedTimeSeconds, 1f);
		}
	}

	// Token: 0x060016BC RID: 5820 RVA: 0x000A8BC3 File Offset: 0x000A6DC3
	private void OnEnable()
	{
		Fish.Instances.Add(this);
	}

	// Token: 0x060016BD RID: 5821 RVA: 0x000A8BD0 File Offset: 0x000A6DD0
	private void OnDisable()
	{
		Fish.Instances.Remove(this);
	}

	// Token: 0x060016BE RID: 5822 RVA: 0x000A8BE0 File Offset: 0x000A6DE0
	public string GetHoverText()
	{
		string text = this.m_name;
		if (this.IsOutOfWater())
		{
			if (this.m_itemDrop)
			{
				return this.m_itemDrop.GetHoverText();
			}
			text += "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup";
		}
		return Localization.instance.Localize(text);
	}

	// Token: 0x060016BF RID: 5823 RVA: 0x000A8C2C File Offset: 0x000A6E2C
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x060016C0 RID: 5824 RVA: 0x000A8C34 File Offset: 0x000A6E34
	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		return !repeat && this.IsOutOfWater() && this.Pickup(character);
	}

	// Token: 0x060016C1 RID: 5825 RVA: 0x000A8C54 File Offset: 0x000A6E54
	public bool Pickup(Humanoid character)
	{
		if (this.m_itemDrop)
		{
			this.m_itemDrop.Pickup(character);
			return true;
		}
		if (this.m_pickupItem == null)
		{
			return false;
		}
		if (!character.GetInventory().CanAddItem(this.m_pickupItem, this.m_pickupItemStackSize))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_noroom", 0, null);
			return false;
		}
		this.m_nview.InvokeRPC("RequestPickup", Array.Empty<object>());
		return true;
	}

	// Token: 0x060016C2 RID: 5826 RVA: 0x000A8CCB File Offset: 0x000A6ECB
	private void RPC_RequestPickup(long uid)
	{
		if (Time.time - this.m_pickupTime > 2f)
		{
			this.m_pickupTime = Time.time;
			this.m_nview.InvokeRPC(uid, "Pickup", Array.Empty<object>());
		}
	}

	// Token: 0x060016C3 RID: 5827 RVA: 0x000A8D01 File Offset: 0x000A6F01
	private void RPC_Pickup(long uid)
	{
		if (Player.m_localPlayer && Player.m_localPlayer.PickupPrefab(this.m_pickupItem, this.m_pickupItemStackSize, true) != null)
		{
			this.m_nview.ClaimOwnership();
			this.m_nview.Destroy();
		}
	}

	// Token: 0x060016C4 RID: 5828 RVA: 0x000A8D3E File Offset: 0x000A6F3E
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x060016C5 RID: 5829 RVA: 0x000A8D44 File Offset: 0x000A6F44
	public void SetLiquidLevel(float level, LiquidType type, Component liquidObj)
	{
		if (type == LiquidType.Water)
		{
			this.m_inWater = level;
		}
		this.m_liquidSurface = null;
		this.m_waterVolume = null;
		WaterVolume waterVolume = liquidObj as WaterVolume;
		if (waterVolume != null)
		{
			this.m_waterVolume = waterVolume;
			return;
		}
		LiquidSurface liquidSurface = liquidObj as LiquidSurface;
		if (liquidSurface != null)
		{
			this.m_liquidSurface = liquidSurface;
		}
	}

	// Token: 0x060016C6 RID: 5830 RVA: 0x000A8D8C File Offset: 0x000A6F8C
	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	// Token: 0x060016C7 RID: 5831 RVA: 0x000A8D9F File Offset: 0x000A6F9F
	public bool IsOutOfWater()
	{
		return this.m_inWater < base.transform.position.y - this.m_height;
	}

	// Token: 0x060016C8 RID: 5832 RVA: 0x000A8DC0 File Offset: 0x000A6FC0
	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (Time.frameCount != Fish.s_updatedFrame)
		{
			Vector4 a;
			Vector4 b;
			float num;
			EnvMan.instance.GetWindData(out a, out b, out num);
			Fish.s_wind = a + b;
			Fish.s_wrappedTimeSeconds = ZNet.instance.GetWrappedDayTimeSeconds();
			Fish.s_deltaTime = fixedDeltaTime;
			Fish.s_time = Time.time;
			Fish.s_dawnDusk = 1f - Utils.Abs(Utils.Abs(EnvMan.instance.GetDayFraction() * 2f - 1f) - 0.5f) * 2f;
			Fish.s_updatedFrame = Time.frameCount;
		}
		if (this.m_isColliding > 0)
		{
			if (Fish.s_time > this.m_lastCollision + 0.5f)
			{
				this.onCollision();
			}
			if (this.m_isJumping)
			{
				this.m_isJumping = false;
			}
		}
		Vector3 position = base.transform.position;
		bool flag = this.IsOutOfWater();
		if (this.m_waterVolume != null)
		{
			int num2 = this.m_waterWaveCount + 1;
			this.m_waterWaveCount = num2;
			if ((num2 & 1) == 1)
			{
				this.m_waterDepth = this.m_waterVolume.Depth(position);
			}
			else
			{
				this.m_waterWave = this.m_waterVolume.CalcWave(position, this.m_waterDepth, Fish.s_wrappedTimeSeconds, 1f);
			}
		}
		this.SetVisible(this.m_nview.HasOwner());
		if (this.m_lastOwner != this.m_nview.GetZDO().GetOwner())
		{
			this.m_lastOwner = this.m_nview.GetZDO().GetOwner();
			this.m_body.WakeUp();
		}
		if (!flag && UnityEngine.Random.value > 0.975f && this.m_nview.GetZDO().GetInt(ZDOVars.s_hooked, 0) == 1 && this.m_nview.GetZDO().GetFloat(ZDOVars.s_escape, 0f) > 0f)
		{
			this.m_jumpEffects.Create(position, Quaternion.identity, base.transform, 1f, -1);
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		FishingFloat fishingFloat = FishingFloat.FindFloat(this);
		if (fishingFloat)
		{
			Utils.Pull(this.m_body, fishingFloat.transform.position, 1f, this.m_hookForce, 1f, 0.5f, false, false, 1f);
		}
		if (this.m_isColliding > 0 && flag)
		{
			this.ConsiderJump(Fish.s_time);
		}
		if (this.m_escapeTime > 0f)
		{
			this.m_body.rotation *= Quaternion.AngleAxis(MathF.Sin(this.m_escapeTime * 40f) * 12f, Vector3.up);
			this.m_escapeTime -= Fish.s_deltaTime;
			if (this.m_escapeTime <= 0f)
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_escape, 0, false);
				this.m_nextEscape = Fish.s_time + UnityEngine.Random.Range(this.m_escapeWaitMin, this.m_escapeWaitMax);
			}
		}
		else if (Fish.s_time > this.m_nextEscape && this.IsHooked())
		{
			this.Escape();
		}
		if (this.m_inWater <= -10000f || this.m_inWater < position.y + this.m_height)
		{
			this.m_body.useGravity = true;
			if (flag)
			{
				if (this.m_isJumping)
				{
					Vector3 velocity = this.m_body.velocity;
					if (!this.m_jumpedFromLand && velocity != Vector3.zero)
					{
						velocity.y *= 1.6f;
						this.m_body.rotation = Quaternion.RotateTowards(this.m_body.rotation, Quaternion.LookRotation(velocity), 5f);
					}
				}
				return;
			}
		}
		if (this.m_isJumping)
		{
			if (this.m_body.velocity.y < 0f)
			{
				this.m_jumpEffects.Create(position, Quaternion.identity, null, 1f, -1);
				this.m_isJumping = false;
				this.m_body.rotation = Quaternion.Euler(0f, this.m_body.rotation.eulerAngles.y, 0f);
				this.RandomizeWaypoint(true, Fish.s_time);
			}
		}
		else if (this.m_waterWave >= this.m_minDepth && this.m_waterWave < this.m_minDepth + this.m_maxJumpDepthOffset)
		{
			this.ConsiderJump(Fish.s_time);
		}
		this.m_JumpHeightStrength = 1f;
		this.m_body.useGravity = false;
		this.m_fast = false;
		bool flag2 = Fish.s_time > this.m_blockChange;
		Player playerNoiseRange = Player.GetPlayerNoiseRange(position, 100f);
		if (playerNoiseRange)
		{
			if (Vector3.Distance(position, playerNoiseRange.transform.position) > this.m_avoidRange / 2f && !this.IsHooked())
			{
				if (flag2 || Fish.s_time > this.m_lastCollision + this.m_collisionFleeTimeout)
				{
					Vector3 normalized = (position - playerNoiseRange.transform.position).normalized;
					this.SwimDirection(normalized, true, true, Fish.s_deltaTime);
				}
				return;
			}
			this.m_fast = true;
			if (this.m_swimTimer > 0.5f)
			{
				this.m_swimTimer = 0.5f;
			}
		}
		this.m_swimTimer -= Fish.s_deltaTime;
		if (this.m_swimTimer <= 0f && flag2)
		{
			this.RandomizeWaypoint(!this.m_fast, Fish.s_time);
		}
		if (this.m_haveWaypoint)
		{
			if (this.m_waypointFF)
			{
				this.m_waypoint = this.m_waypointFF.transform.position + Vector3.down;
			}
			if (Vector2.Distance(this.m_waypoint, position) < 0.2f || (this.m_escapeTime < 0f && this.IsHooked()))
			{
				if (!this.m_waypointFF)
				{
					this.m_haveWaypoint = false;
					return;
				}
				if (Fish.s_time - this.m_lastNibbleTime > 1f && this.m_failedBait != this.m_waypointFF)
				{
					this.m_lastNibbleTime = Fish.s_time;
					bool flag3 = this.TestBate(this.m_waypointFF);
					this.m_waypointFF.Nibble(this, flag3);
					if (!flag3)
					{
						this.m_failedBait = this.m_waypointFF;
					}
				}
			}
			Vector3 dir = Vector3.Normalize(this.m_waypoint - position);
			this.SwimDirection(dir, this.m_fast, false, Fish.s_deltaTime);
		}
		else
		{
			this.Stop(Fish.s_deltaTime);
		}
		if (!flag && this.m_waterVolume != null)
		{
			this.m_body.AddForce(new Vector3(0f, this.m_waterWave - this.m_lastWave, 0f) * 10f, ForceMode.VelocityChange);
			this.m_lastWave = this.m_waterWave;
			if (this.m_waterWave > 0f)
			{
				this.m_body.AddForce(Fish.s_wind * this.m_waveFollowDirection * this.m_waterWave);
			}
		}
	}

	// Token: 0x060016C9 RID: 5833 RVA: 0x000A94C4 File Offset: 0x000A76C4
	private void Stop(float dt)
	{
		if (this.m_inWater < base.transform.position.y + this.m_height)
		{
			return;
		}
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Quaternion to = Quaternion.LookRotation(forward, Vector3.up);
		Quaternion rot = Quaternion.RotateTowards(this.m_body.rotation, to, this.m_turnRate * dt);
		this.m_body.MoveRotation(rot);
		Vector3 force = -this.m_body.velocity * this.m_acceleration;
		this.m_body.AddForce(force, ForceMode.VelocityChange);
	}

	// Token: 0x060016CA RID: 5834 RVA: 0x000A956C File Offset: 0x000A776C
	private void SwimDirection(Vector3 dir, bool fast, bool avoidLand, float dt)
	{
		Vector3 vector = dir;
		vector.y = 0f;
		if (vector == Vector3.zero)
		{
			ZLog.LogWarning("Invalid swim direction");
			return;
		}
		vector.Normalize();
		float num = this.m_turnRate;
		if (fast)
		{
			num *= this.m_avoidSpeedScale;
		}
		Quaternion to = Quaternion.LookRotation(vector, Vector3.up);
		Quaternion rotation = Quaternion.RotateTowards(base.transform.rotation, to, num * dt);
		if (this.m_isJumping && this.m_body.velocity.y > 0f)
		{
			return;
		}
		if (!this.m_isJumping)
		{
			this.m_body.rotation = rotation;
		}
		float num2 = this.m_speed;
		if (fast)
		{
			num2 *= this.m_avoidSpeedScale;
		}
		if (avoidLand && this.GetPointDepth(base.transform.position + base.transform.forward) < this.m_minDepth)
		{
			num2 = 0f;
		}
		if (fast && Vector3.Dot(dir, base.transform.forward) < 0f)
		{
			num2 = 0f;
		}
		Vector3 forward = base.transform.forward;
		forward.y = dir.y;
		Vector3 vector2 = forward * num2 - this.m_body.velocity;
		if (this.m_inWater < base.transform.position.y + this.m_height && vector2.y > 0f)
		{
			vector2.y = 0f;
		}
		this.m_body.AddForce(vector2 * this.m_acceleration, ForceMode.VelocityChange);
	}

	// Token: 0x060016CB RID: 5835 RVA: 0x000A9704 File Offset: 0x000A7904
	private FishingFloat FindFloat()
	{
		foreach (FishingFloat fishingFloat in FishingFloat.GetAllInstances())
		{
			if (fishingFloat.IsInWater() && Vector3.Distance(base.transform.position, fishingFloat.transform.position) <= fishingFloat.m_range && !(fishingFloat.GetCatch() != null))
			{
				float baseHookChance = this.m_baseHookChance;
				if (UnityEngine.Random.value < baseHookChance)
				{
					return fishingFloat;
				}
			}
		}
		return null;
	}

	// Token: 0x060016CC RID: 5836 RVA: 0x000A97A0 File Offset: 0x000A79A0
	private bool TestBate(FishingFloat ff)
	{
		string bait = ff.GetBait();
		foreach (Fish.BaitSetting baitSetting in this.m_baits)
		{
			if (baitSetting.m_bait.name == bait && UnityEngine.Random.value < baitSetting.m_chance)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060016CD RID: 5837 RVA: 0x000A981C File Offset: 0x000A7A1C
	private bool RandomizeWaypoint(bool canHook, float timeNow)
	{
		if (this.m_isJumping)
		{
			return false;
		}
		Vector2 vector = UnityEngine.Random.insideUnitCircle * this.m_swimRange;
		this.m_waypoint = this.m_spawnPoint + new Vector3(vector.x, 0f, vector.y);
		this.m_waypointFF = null;
		if (canHook)
		{
			FishingFloat fishingFloat = this.FindFloat();
			if (fishingFloat && fishingFloat != this.m_failedBait)
			{
				this.m_waypointFF = fishingFloat;
				this.m_waypoint = fishingFloat.transform.position + Vector3.down;
			}
		}
		float pointDepth = this.GetPointDepth(this.m_waypoint);
		if (pointDepth < this.m_minDepth)
		{
			return false;
		}
		Vector3 p = (this.m_waypoint + base.transform.position) * 0.5f;
		if (this.GetPointDepth(p) < this.m_minDepth)
		{
			return false;
		}
		float maxInclusive = Mathf.Min(this.m_maxDepth, pointDepth - this.m_height);
		float waterLevel = this.GetWaterLevel(this.m_waypoint);
		this.m_waypoint.y = waterLevel - UnityEngine.Random.Range(this.m_minDepth, maxInclusive);
		this.m_haveWaypoint = true;
		this.m_swimTimer = UnityEngine.Random.Range(this.m_wpDurationMin, this.m_wpDurationMax);
		this.m_blockChange = timeNow + UnityEngine.Random.Range(this.m_blockChangeDurationMin, this.m_blockChangeDurationMax);
		return true;
	}

	// Token: 0x060016CE RID: 5838 RVA: 0x000A9978 File Offset: 0x000A7B78
	private void Escape()
	{
		this.m_escapeTime = UnityEngine.Random.Range(this.m_escapeMin, this.m_escapeMax + (float)(this.m_itemDrop ? this.m_itemDrop.m_itemData.m_quality : 1) * this.m_escapeMaxPerLevel);
		this.m_nview.GetZDO().Set(ZDOVars.s_escape, this.m_escapeTime);
	}

	// Token: 0x060016CF RID: 5839 RVA: 0x000A99E0 File Offset: 0x000A7BE0
	private float GetPointDepth(Vector3 p)
	{
		float num;
		if (ZoneSystem.instance && ZoneSystem.instance.GetSolidHeight(p, out num, (this.m_waterVolume != null) ? 0 : 1000))
		{
			return this.GetWaterLevel(p) - num;
		}
		return 0f;
	}

	// Token: 0x060016D0 RID: 5840 RVA: 0x000A9A2D File Offset: 0x000A7C2D
	private float GetWaterLevel(Vector3 point)
	{
		if (!(this.m_waterVolume != null))
		{
			return 30f;
		}
		return this.m_waterVolume.GetWaterSurface(point, 1f);
	}

	// Token: 0x060016D1 RID: 5841 RVA: 0x000A9A54 File Offset: 0x000A7C54
	private bool DangerNearby()
	{
		return Player.GetPlayerNoiseRange(base.transform.position, 100f) != null;
	}

	// Token: 0x060016D2 RID: 5842 RVA: 0x000A9A71 File Offset: 0x000A7C71
	public ZDOID GetZDOID()
	{
		return this.m_nview.GetZDO().m_uid;
	}

	// Token: 0x060016D3 RID: 5843 RVA: 0x000A9A84 File Offset: 0x000A7C84
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * this.m_height, new Vector3(1f, 0.02f, 1f));
	}

	// Token: 0x060016D4 RID: 5844 RVA: 0x000A9AD4 File Offset: 0x000A7CD4
	private void OnCollisionEnter(Collision collision)
	{
		this.m_isColliding++;
	}

	// Token: 0x060016D5 RID: 5845 RVA: 0x000A9AE4 File Offset: 0x000A7CE4
	private void OnCollisionExit(Collision collision)
	{
		this.m_isColliding--;
	}

	// Token: 0x060016D6 RID: 5846 RVA: 0x000A9AF4 File Offset: 0x000A7CF4
	private void onCollision()
	{
		this.m_lastCollision = Fish.s_time;
		if (!this.m_nview || !this.m_nview.IsOwner())
		{
			return;
		}
		int num = 0;
		while (num < 10 && !this.RandomizeWaypoint(!this.m_fast, Fish.s_time))
		{
			num++;
		}
	}

	// Token: 0x060016D7 RID: 5847 RVA: 0x000A9B4A File Offset: 0x000A7D4A
	private void onDrop(ItemDrop item)
	{
		this.m_JumpHeightStrength = 0f;
	}

	// Token: 0x060016D8 RID: 5848 RVA: 0x000A9B58 File Offset: 0x000A7D58
	private void ConsiderJump(float timeNow)
	{
		if (this.m_itemDrop && (float)this.m_itemDrop.m_itemData.m_quality > this.m_jumpMaxLevel)
		{
			return;
		}
		if (this.m_JumpHeightStrength > 0f && timeNow > this.m_lastJumpCheck + this.m_jumpFrequencySeconds)
		{
			this.m_lastJumpCheck = timeNow;
			if (this.IsOutOfWater())
			{
				if (UnityEngine.Random.Range(0f, 1f) < this.m_jumpOnLandChance * this.m_JumpHeightStrength)
				{
					this.Jump();
					return;
				}
			}
			else if (UnityEngine.Random.Range(0f, 1f) < (this.m_jumpChance + Mathf.Min(0f, this.m_lastWave) * this.m_waveJumpMultiplier) * Fish.s_dawnDusk)
			{
				this.Jump();
			}
		}
	}

	// Token: 0x060016D9 RID: 5849 RVA: 0x000A9C18 File Offset: 0x000A7E18
	private void Jump()
	{
		if (this.m_isJumping)
		{
			return;
		}
		this.m_isJumping = true;
		if (this.IsOutOfWater())
		{
			this.m_jumpedFromLand = true;
			this.m_JumpHeightStrength *= this.m_jumpOnLandDecay;
			float jumpOnLandRotation = this.m_jumpOnLandRotation;
			this.m_body.AddForce(new Vector3(0f, this.m_JumpHeightStrength * this.m_jumpHeightLand * base.transform.localScale.y, 0f), ForceMode.Impulse);
			this.m_body.AddTorque(UnityEngine.Random.Range(-jumpOnLandRotation, jumpOnLandRotation), UnityEngine.Random.Range(-jumpOnLandRotation, jumpOnLandRotation), UnityEngine.Random.Range(-jumpOnLandRotation, jumpOnLandRotation), ForceMode.Impulse);
			return;
		}
		this.m_jumpedFromLand = false;
		this.m_jumpEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		this.m_body.AddForce(new Vector3(0f, this.m_jumpHeight * base.transform.localScale.y, 0f), ForceMode.Impulse);
		this.m_body.AddForce(base.transform.forward * (this.m_jumpForwardStrength * base.transform.localScale.y), ForceMode.Impulse);
	}

	// Token: 0x060016DA RID: 5850 RVA: 0x000A9D4C File Offset: 0x000A7F4C
	public void OnHooked(FishingFloat ff)
	{
		if (this.m_nview && this.m_nview.IsValid())
		{
			this.m_nview.ClaimOwnership();
		}
		this.m_fishingFloat = ff;
		if (this.m_nview.IsValid())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_hooked, (ff != null) ? 1 : 0, false);
			this.Escape();
		}
	}

	// Token: 0x060016DB RID: 5851 RVA: 0x000A9DBB File Offset: 0x000A7FBB
	public bool IsHooked()
	{
		return this.m_fishingFloat != null;
	}

	// Token: 0x060016DC RID: 5852 RVA: 0x000A9DC9 File Offset: 0x000A7FC9
	public bool IsEscaping()
	{
		return this.m_escapeTime > 0f && this.IsHooked();
	}

	// Token: 0x060016DD RID: 5853 RVA: 0x000A9DE0 File Offset: 0x000A7FE0
	public float GetStaminaUse()
	{
		if (!this.IsEscaping())
		{
			return this.m_staminaUse;
		}
		return this.m_escapeStaminaUse;
	}

	// Token: 0x060016DE RID: 5854 RVA: 0x000A9DF8 File Offset: 0x000A7FF8
	protected void SetVisible(bool visible)
	{
		if (this.m_lodGroup == null)
		{
			return;
		}
		if (this.m_lodVisible == visible)
		{
			return;
		}
		this.m_lodVisible = visible;
		if (this.m_lodVisible)
		{
			this.m_lodGroup.localReferencePoint = this.m_originalLocalRef;
			return;
		}
		this.m_lodGroup.localReferencePoint = new Vector3(999999f, 999999f, 999999f);
	}

	// Token: 0x060016DF RID: 5855 RVA: 0x000A9E60 File Offset: 0x000A8060
	public int Increment(LiquidType type)
	{
		int[] liquids = this.m_liquids;
		int num = liquids[(int)type] + 1;
		liquids[(int)type] = num;
		return num;
	}

	// Token: 0x060016E0 RID: 5856 RVA: 0x000A9E84 File Offset: 0x000A8084
	public int Decrement(LiquidType type)
	{
		int[] liquids = this.m_liquids;
		int num = liquids[(int)type] - 1;
		liquids[(int)type] = num;
		return num;
	}

	// Token: 0x170000C1 RID: 193
	// (get) Token: 0x060016E1 RID: 5857 RVA: 0x000A9EA5 File Offset: 0x000A80A5
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x0400167F RID: 5759
	public string m_name = "Fish";

	// Token: 0x04001680 RID: 5760
	public float m_swimRange = 20f;

	// Token: 0x04001681 RID: 5761
	public float m_minDepth = 1f;

	// Token: 0x04001682 RID: 5762
	public float m_maxDepth = 4f;

	// Token: 0x04001683 RID: 5763
	public float m_speed = 10f;

	// Token: 0x04001684 RID: 5764
	public float m_acceleration = 5f;

	// Token: 0x04001685 RID: 5765
	public float m_turnRate = 10f;

	// Token: 0x04001686 RID: 5766
	public float m_wpDurationMin = 4f;

	// Token: 0x04001687 RID: 5767
	public float m_wpDurationMax = 4f;

	// Token: 0x04001688 RID: 5768
	public float m_avoidSpeedScale = 2f;

	// Token: 0x04001689 RID: 5769
	public float m_avoidRange = 5f;

	// Token: 0x0400168A RID: 5770
	public float m_height = 0.2f;

	// Token: 0x0400168B RID: 5771
	public float m_hookForce = 4f;

	// Token: 0x0400168C RID: 5772
	public float m_staminaUse = 1f;

	// Token: 0x0400168D RID: 5773
	public float m_escapeStaminaUse = 2f;

	// Token: 0x0400168E RID: 5774
	public float m_escapeMin = 0.5f;

	// Token: 0x0400168F RID: 5775
	public float m_escapeMax = 3f;

	// Token: 0x04001690 RID: 5776
	public float m_escapeWaitMin = 0.75f;

	// Token: 0x04001691 RID: 5777
	public float m_escapeWaitMax = 4f;

	// Token: 0x04001692 RID: 5778
	public float m_escapeMaxPerLevel = 1.5f;

	// Token: 0x04001693 RID: 5779
	public float m_baseHookChance = 0.5f;

	// Token: 0x04001694 RID: 5780
	public GameObject m_pickupItem;

	// Token: 0x04001695 RID: 5781
	public int m_pickupItemStackSize = 1;

	// Token: 0x04001696 RID: 5782
	[global::Tooltip("Fish aren't smart enough to change their mind too often (and makes reactions/collisions feel less artificial)")]
	public float m_blockChangeDurationMin = 0.1f;

	// Token: 0x04001697 RID: 5783
	public float m_blockChangeDurationMax = 0.6f;

	// Token: 0x04001698 RID: 5784
	public float m_collisionFleeTimeout = 1.5f;

	// Token: 0x04001699 RID: 5785
	private Vector3 m_waypoint;

	// Token: 0x0400169A RID: 5786
	private FishingFloat m_waypointFF;

	// Token: 0x0400169B RID: 5787
	private FishingFloat m_failedBait;

	// Token: 0x0400169C RID: 5788
	private bool m_haveWaypoint;

	// Token: 0x0400169D RID: 5789
	[Header("Baits")]
	public List<Fish.BaitSetting> m_baits = new List<Fish.BaitSetting>();

	// Token: 0x0400169E RID: 5790
	public DropTable m_extraDrops = new DropTable();

	// Token: 0x0400169F RID: 5791
	[Header("Jumping")]
	public float m_jumpSpeed = 3f;

	// Token: 0x040016A0 RID: 5792
	public float m_jumpHeight = 14f;

	// Token: 0x040016A1 RID: 5793
	public float m_jumpForwardStrength = 16f;

	// Token: 0x040016A2 RID: 5794
	public float m_jumpHeightLand = 3f;

	// Token: 0x040016A3 RID: 5795
	public float m_jumpChance = 0.25f;

	// Token: 0x040016A4 RID: 5796
	public float m_jumpOnLandChance = 0.5f;

	// Token: 0x040016A5 RID: 5797
	public float m_jumpOnLandDecay = 0.5f;

	// Token: 0x040016A6 RID: 5798
	public float m_maxJumpDepthOffset = 0.5f;

	// Token: 0x040016A7 RID: 5799
	public float m_jumpFrequencySeconds = 0.1f;

	// Token: 0x040016A8 RID: 5800
	public float m_jumpOnLandRotation = 2f;

	// Token: 0x040016A9 RID: 5801
	public float m_waveJumpMultiplier = 0.05f;

	// Token: 0x040016AA RID: 5802
	public float m_jumpMaxLevel = 2f;

	// Token: 0x040016AB RID: 5803
	public EffectList m_jumpEffects = new EffectList();

	// Token: 0x040016AC RID: 5804
	private float m_JumpHeightStrength;

	// Token: 0x040016AD RID: 5805
	private bool m_jumpedFromLand;

	// Token: 0x040016AE RID: 5806
	private int m_isColliding;

	// Token: 0x040016AF RID: 5807
	private bool m_isJumping;

	// Token: 0x040016B0 RID: 5808
	private float m_lastJumpCheck;

	// Token: 0x040016B1 RID: 5809
	private float m_swimTimer;

	// Token: 0x040016B2 RID: 5810
	private float m_lastNibbleTime;

	// Token: 0x040016B3 RID: 5811
	private float m_escapeTime;

	// Token: 0x040016B4 RID: 5812
	private float m_nextEscape;

	// Token: 0x040016B5 RID: 5813
	private Vector3 m_spawnPoint;

	// Token: 0x040016B6 RID: 5814
	private bool m_fast;

	// Token: 0x040016B7 RID: 5815
	private float m_lastCollision;

	// Token: 0x040016B8 RID: 5816
	private float m_blockChange;

	// Token: 0x040016B9 RID: 5817
	[Header("Waves")]
	public float m_waveFollowDirection = 7f;

	// Token: 0x040016BA RID: 5818
	private float m_lastWave;

	// Token: 0x040016BB RID: 5819
	private float m_inWater = -10000f;

	// Token: 0x040016BC RID: 5820
	private WaterVolume m_waterVolume;

	// Token: 0x040016BD RID: 5821
	private LiquidSurface m_liquidSurface;

	// Token: 0x040016BE RID: 5822
	private FishingFloat m_fishingFloat;

	// Token: 0x040016BF RID: 5823
	private float m_pickupTime;

	// Token: 0x040016C0 RID: 5824
	private long m_lastOwner = -1L;

	// Token: 0x040016C1 RID: 5825
	private Vector3 m_originalLocalRef;

	// Token: 0x040016C2 RID: 5826
	private bool m_lodVisible = true;

	// Token: 0x040016C3 RID: 5827
	private ZNetView m_nview;

	// Token: 0x040016C4 RID: 5828
	private Rigidbody m_body;

	// Token: 0x040016C5 RID: 5829
	private ItemDrop m_itemDrop;

	// Token: 0x040016C6 RID: 5830
	private LODGroup m_lodGroup;

	// Token: 0x040016C7 RID: 5831
	private static Vector4 s_wind;

	// Token: 0x040016C8 RID: 5832
	private static float s_wrappedTimeSeconds;

	// Token: 0x040016C9 RID: 5833
	private static float s_deltaTime;

	// Token: 0x040016CA RID: 5834
	private static float s_time;

	// Token: 0x040016CB RID: 5835
	private static float s_dawnDusk;

	// Token: 0x040016CC RID: 5836
	private static int s_updatedFrame;

	// Token: 0x040016CD RID: 5837
	private float m_waterDepth;

	// Token: 0x040016CE RID: 5838
	private float m_waterWave;

	// Token: 0x040016CF RID: 5839
	private int m_waterWaveCount;

	// Token: 0x040016D0 RID: 5840
	private readonly int[] m_liquids = new int[2];

	// Token: 0x0200035F RID: 863
	[Serializable]
	public class BaitSetting
	{
		// Token: 0x040025A1 RID: 9633
		public ItemDrop m_bait;

		// Token: 0x040025A2 RID: 9634
		[Range(0f, 1f)]
		public float m_chance;
	}
}
