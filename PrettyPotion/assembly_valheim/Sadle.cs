using System;
using UnityEngine;

// Token: 0x0200002F RID: 47
public class Sadle : MonoBehaviour, Interactable, Hoverable, IDoodadController
{
	// Token: 0x06000472 RID: 1138 RVA: 0x000289AC File Offset: 0x00026BAC
	private void Awake()
	{
		this.m_character = base.gameObject.GetComponentInParent<Character>();
		this.m_nview = this.m_character.GetComponent<ZNetView>();
		this.m_tambable = this.m_character.GetComponent<Tameable>();
		this.m_monsterAI = this.m_character.GetComponent<MonsterAI>();
		this.m_nview.Register<long>("RequestControl", new Action<long, long>(this.RPC_RequestControl));
		this.m_nview.Register<long>("ReleaseControl", new Action<long, long>(this.RPC_ReleaseControl));
		this.m_nview.Register<bool>("RequestRespons", new Action<long, bool>(this.RPC_RequestRespons));
		this.m_nview.Register<Vector3>("RemoveSaddle", new Action<long, Vector3>(this.RPC_RemoveSaddle));
		this.m_nview.Register<Vector3, int, float>("Controls", new Action<long, Vector3, int, float>(this.RPC_Controls));
	}

	// Token: 0x06000473 RID: 1139 RVA: 0x00028A89 File Offset: 0x00026C89
	public bool IsValid()
	{
		return this;
	}

	// Token: 0x06000474 RID: 1140 RVA: 0x00028A91 File Offset: 0x00026C91
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06000475 RID: 1141 RVA: 0x00028A94 File Offset: 0x00026C94
	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.CalculateHaveValidUser();
		if (!this.m_character.IsTamed())
		{
			return;
		}
		if (this.IsLocalUser())
		{
			this.UpdateRidingSkill(Time.fixedDeltaTime);
		}
		if (this.m_nview.IsOwner())
		{
			float fixedDeltaTime = Time.fixedDeltaTime;
			this.UpdateStamina(fixedDeltaTime);
			this.UpdateDrown(fixedDeltaTime);
		}
	}

	// Token: 0x06000476 RID: 1142 RVA: 0x00028AF8 File Offset: 0x00026CF8
	private void UpdateDrown(float dt)
	{
		if (this.m_character.IsSwimming() && !this.m_character.IsOnGround() && !this.HaveStamina(0f))
		{
			this.m_drownDamageTimer += dt;
			if (this.m_drownDamageTimer > 1f)
			{
				this.m_drownDamageTimer = 0f;
				float damage = Mathf.Ceil(this.m_character.GetMaxHealth() / 20f);
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = damage;
				hitData.m_point = this.m_character.GetCenterPoint();
				hitData.m_dir = Vector3.down;
				hitData.m_pushForce = 10f;
				hitData.m_hitType = HitData.HitType.Drowning;
				this.m_character.Damage(hitData);
				Vector3 position = base.transform.position;
				position.y = this.m_character.GetLiquidLevel();
				this.m_drownEffects.Create(position, base.transform.rotation, null, 1f, -1);
			}
		}
	}

	// Token: 0x06000477 RID: 1143 RVA: 0x00028C00 File Offset: 0x00026E00
	public bool UpdateRiding(float dt)
	{
		if (!base.isActiveAndEnabled)
		{
			return false;
		}
		if (!this.m_character.IsTamed())
		{
			return false;
		}
		if (!this.HaveValidUser())
		{
			return false;
		}
		if (this.m_speed == Sadle.Speed.Stop || this.m_controlDir.magnitude == 0f)
		{
			return false;
		}
		if (this.m_speed == Sadle.Speed.Walk || this.m_speed == Sadle.Speed.Run)
		{
			if (this.m_speed == Sadle.Speed.Run && !this.HaveStamina(0f))
			{
				this.m_speed = Sadle.Speed.Walk;
			}
			this.m_monsterAI.MoveTowards(this.m_controlDir, this.m_speed == Sadle.Speed.Run);
			float riderSkill = this.GetRiderSkill();
			float num = Mathf.Lerp(1f, 0.5f, riderSkill);
			if (this.m_character.IsSwimming())
			{
				this.UseStamina(this.m_swimStaminaDrain * num * dt);
			}
			else if (this.m_speed == Sadle.Speed.Run)
			{
				this.UseStamina(this.m_runStaminaDrain * num * dt);
			}
		}
		else if (this.m_speed == Sadle.Speed.Turn)
		{
			this.m_monsterAI.StopMoving();
			this.m_character.SetRun(false);
			this.m_monsterAI.LookTowards(this.m_controlDir);
		}
		this.m_monsterAI.ResetRandomMovement();
		return true;
	}

	// Token: 0x06000478 RID: 1144 RVA: 0x00028D28 File Offset: 0x00026F28
	public string GetHoverText()
	{
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
		}
		string text = Localization.instance.Localize(this.m_hoverText);
		text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
		if (ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive())
		{
			text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltKeys + $KEY_Use</b></color>] $hud_saddle_remove");
		}
		else
		{
			text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $hud_saddle_remove");
		}
		return text;
	}

	// Token: 0x06000479 RID: 1145 RVA: 0x00028DB6 File Offset: 0x00026FB6
	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_hoverText);
	}

	// Token: 0x0600047A RID: 1146 RVA: 0x00028DC8 File Offset: 0x00026FC8
	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.InUseDistance(character))
		{
			return false;
		}
		if (!this.m_character.IsTamed())
		{
			return false;
		}
		Player player = character as Player;
		if (player == null)
		{
			return false;
		}
		if (alt)
		{
			this.m_nview.InvokeRPC("RemoveSaddle", new object[]
			{
				character.transform.position
			});
			return true;
		}
		this.m_nview.InvokeRPC("RequestControl", new object[]
		{
			player.GetZDOID().UserID
		});
		return false;
	}

	// Token: 0x0600047B RID: 1147 RVA: 0x00028E70 File Offset: 0x00027070
	public Character GetCharacter()
	{
		return this.m_character;
	}

	// Token: 0x0600047C RID: 1148 RVA: 0x00028E78 File Offset: 0x00027078
	public Tameable GetTameable()
	{
		return this.m_tambable;
	}

	// Token: 0x0600047D RID: 1149 RVA: 0x00028E80 File Offset: 0x00027080
	public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		float skillFactor = Player.m_localPlayer.GetSkills().GetSkillFactor(Skills.SkillType.Ride);
		Sadle.Speed speed = Sadle.Speed.NoChange;
		Vector3 vector = Vector3.zero;
		if (block || (double)moveDir.z > 0.5 || run)
		{
			Vector3 vector2 = lookDir;
			vector2.y = 0f;
			vector2.Normalize();
			vector = vector2;
		}
		if (run)
		{
			speed = Sadle.Speed.Run;
		}
		else if ((double)moveDir.z > 0.5)
		{
			speed = Sadle.Speed.Walk;
		}
		else if ((double)moveDir.z < -0.5)
		{
			speed = Sadle.Speed.Stop;
		}
		else if (block)
		{
			speed = Sadle.Speed.Turn;
		}
		this.m_nview.InvokeRPC("Controls", new object[]
		{
			vector,
			(int)speed,
			skillFactor
		});
	}

	// Token: 0x0600047E RID: 1150 RVA: 0x00028F54 File Offset: 0x00027154
	private void RPC_Controls(long sender, Vector3 rideDir, int rideSpeed, float skill)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.m_rideSkill = skill;
		if (rideDir != Vector3.zero)
		{
			this.m_controlDir = rideDir;
		}
		if (rideSpeed == 4)
		{
			if (this.m_speed == Sadle.Speed.Turn)
			{
				this.m_speed = Sadle.Speed.Stop;
			}
			return;
		}
		if (rideSpeed == 3 && (this.m_speed == Sadle.Speed.Walk || this.m_speed == Sadle.Speed.Run))
		{
			return;
		}
		this.m_speed = (Sadle.Speed)rideSpeed;
	}

	// Token: 0x0600047F RID: 1151 RVA: 0x00028FC0 File Offset: 0x000271C0
	private void UpdateRidingSkill(float dt)
	{
		this.m_raiseSkillTimer += dt;
		if (this.m_raiseSkillTimer > 1f)
		{
			this.m_raiseSkillTimer = 0f;
			if (this.m_speed == Sadle.Speed.Run)
			{
				Player.m_localPlayer.RaiseSkill(Skills.SkillType.Ride, 1f);
			}
		}
	}

	// Token: 0x06000480 RID: 1152 RVA: 0x0002900D File Offset: 0x0002720D
	private void ResetControlls()
	{
		this.m_controlDir = Vector3.zero;
		this.m_speed = Sadle.Speed.Stop;
		this.m_rideSkill = 0f;
	}

	// Token: 0x06000481 RID: 1153 RVA: 0x0002902C File Offset: 0x0002722C
	public Component GetControlledComponent()
	{
		return this.m_character;
	}

	// Token: 0x06000482 RID: 1154 RVA: 0x00029034 File Offset: 0x00027234
	public Vector3 GetPosition()
	{
		return base.transform.position;
	}

	// Token: 0x06000483 RID: 1155 RVA: 0x00029041 File Offset: 0x00027241
	private void RPC_RemoveSaddle(long sender, Vector3 userPoint)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.HaveValidUser())
		{
			return;
		}
		this.m_tambable.DropSaddle(userPoint);
	}

	// Token: 0x06000484 RID: 1156 RVA: 0x00029068 File Offset: 0x00027268
	private void RPC_RequestControl(long sender, long playerID)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.CalculateHaveValidUser();
		if (this.GetUser() == playerID || !this.HaveValidUser())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_user, playerID);
			this.ResetControlls();
			this.m_nview.InvokeRPC(sender, "RequestRespons", new object[]
			{
				true
			});
			this.m_nview.GetZDO().SetOwner(sender);
			return;
		}
		this.m_nview.InvokeRPC(sender, "RequestRespons", new object[]
		{
			false
		});
	}

	// Token: 0x06000485 RID: 1157 RVA: 0x00029108 File Offset: 0x00027308
	public bool HaveValidUser()
	{
		return this.m_haveValidUser;
	}

	// Token: 0x06000486 RID: 1158 RVA: 0x00029110 File Offset: 0x00027310
	private void CalculateHaveValidUser()
	{
		this.m_haveValidUser = false;
		long user = this.GetUser();
		if (user == 0L)
		{
			return;
		}
		foreach (ZDO zdo in ZNet.instance.GetAllCharacterZDOS())
		{
			if (zdo.m_uid.UserID == user)
			{
				this.m_haveValidUser = (Vector3.Distance(zdo.GetPosition(), base.transform.position) < this.m_maxUseRange);
				break;
			}
		}
	}

	// Token: 0x06000487 RID: 1159 RVA: 0x000291A8 File Offset: 0x000273A8
	private void RPC_ReleaseControl(long sender, long playerID)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetUser() == playerID)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_user, 0L);
			this.ResetControlls();
		}
	}

	// Token: 0x06000488 RID: 1160 RVA: 0x000291E0 File Offset: 0x000273E0
	private void RPC_RequestRespons(long sender, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			Player.m_localPlayer.StartDoodadControl(this);
			if (this.m_attachPoint != null)
			{
				Player.m_localPlayer.AttachStart(this.m_attachPoint, this.m_character.gameObject, false, false, false, this.m_attachAnimation, this.m_detachOffset, null);
				return;
			}
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null);
		}
	}

	// Token: 0x06000489 RID: 1161 RVA: 0x00029254 File Offset: 0x00027454
	public void OnUseStop(Player player)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("ReleaseControl", new object[]
		{
			player.GetZDOID().UserID
		});
		if (this.m_attachPoint != null)
		{
			player.AttachStop();
		}
	}

	// Token: 0x0600048A RID: 1162 RVA: 0x000292B0 File Offset: 0x000274B0
	private bool IsLocalUser()
	{
		if (!Player.m_localPlayer)
		{
			return false;
		}
		long user = this.GetUser();
		return user != 0L && user == Player.m_localPlayer.GetZDOID().UserID;
	}

	// Token: 0x0600048B RID: 1163 RVA: 0x000292EC File Offset: 0x000274EC
	private long GetUser()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return 0L;
		}
		return this.m_nview.GetZDO().GetLong(ZDOVars.s_user, 0L);
	}

	// Token: 0x0600048C RID: 1164 RVA: 0x00029323 File Offset: 0x00027523
	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, this.m_attachPoint.position) < this.m_maxUseRange;
	}

	// Token: 0x0600048D RID: 1165 RVA: 0x00029348 File Offset: 0x00027548
	private void UseStamina(float v)
	{
		if (v == 0f)
		{
			return;
		}
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		float num = this.GetStamina();
		num -= v;
		if (num < 0f)
		{
			num = 0f;
		}
		this.SetStamina(num);
		this.m_staminaRegenTimer = 1f;
	}

	// Token: 0x0600048E RID: 1166 RVA: 0x000293A4 File Offset: 0x000275A4
	private bool HaveStamina(float amount = 0f)
	{
		return this.m_nview.IsValid() && this.GetStamina() > amount;
	}

	// Token: 0x0600048F RID: 1167 RVA: 0x000293C0 File Offset: 0x000275C0
	public float GetStamina()
	{
		if (this.m_nview == null)
		{
			return 0f;
		}
		if (this.m_nview.GetZDO() == null)
		{
			return 0f;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_stamina, this.GetMaxStamina());
	}

	// Token: 0x06000490 RID: 1168 RVA: 0x0002940F File Offset: 0x0002760F
	private void SetStamina(float stamina)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_stamina, stamina);
	}

	// Token: 0x06000491 RID: 1169 RVA: 0x00029427 File Offset: 0x00027627
	public float GetMaxStamina()
	{
		return this.m_maxStamina;
	}

	// Token: 0x06000492 RID: 1170 RVA: 0x00029430 File Offset: 0x00027630
	private void UpdateStamina(float dt)
	{
		this.m_staminaRegenTimer -= dt;
		if (this.m_staminaRegenTimer > 0f)
		{
			return;
		}
		if (this.m_character.InAttack() || this.m_character.IsSwimming())
		{
			return;
		}
		float num = this.GetStamina();
		float maxStamina = this.GetMaxStamina();
		if (num < maxStamina || num > maxStamina)
		{
			float num2 = this.m_tambable.IsHungry() ? this.m_staminaRegenHungry : this.m_staminaRegen;
			float num3 = num2 + (1f - num / maxStamina) * num2;
			num += num3 * dt;
			if (num > maxStamina)
			{
				num = maxStamina;
			}
			this.SetStamina(num);
		}
	}

	// Token: 0x06000493 RID: 1171 RVA: 0x000294C7 File Offset: 0x000276C7
	public float GetRiderSkill()
	{
		return this.m_rideSkill;
	}

	// Token: 0x04000515 RID: 1301
	public string m_hoverText = "";

	// Token: 0x04000516 RID: 1302
	public float m_maxUseRange = 10f;

	// Token: 0x04000517 RID: 1303
	public Transform m_attachPoint;

	// Token: 0x04000518 RID: 1304
	public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

	// Token: 0x04000519 RID: 1305
	public string m_attachAnimation = "attach_chair";

	// Token: 0x0400051A RID: 1306
	public float m_maxStamina = 100f;

	// Token: 0x0400051B RID: 1307
	public float m_runStaminaDrain = 10f;

	// Token: 0x0400051C RID: 1308
	public float m_swimStaminaDrain = 10f;

	// Token: 0x0400051D RID: 1309
	public float m_staminaRegen = 10f;

	// Token: 0x0400051E RID: 1310
	public float m_staminaRegenHungry = 10f;

	// Token: 0x0400051F RID: 1311
	public EffectList m_drownEffects = new EffectList();

	// Token: 0x04000520 RID: 1312
	public Sprite m_mountIcon;

	// Token: 0x04000521 RID: 1313
	private const float m_staminaRegenDelay = 1f;

	// Token: 0x04000522 RID: 1314
	private Vector3 m_controlDir;

	// Token: 0x04000523 RID: 1315
	private Sadle.Speed m_speed;

	// Token: 0x04000524 RID: 1316
	private float m_rideSkill;

	// Token: 0x04000525 RID: 1317
	private float m_staminaRegenTimer;

	// Token: 0x04000526 RID: 1318
	private float m_drownDamageTimer;

	// Token: 0x04000527 RID: 1319
	private float m_raiseSkillTimer;

	// Token: 0x04000528 RID: 1320
	private Character m_character;

	// Token: 0x04000529 RID: 1321
	private ZNetView m_nview;

	// Token: 0x0400052A RID: 1322
	private Tameable m_tambable;

	// Token: 0x0400052B RID: 1323
	private MonsterAI m_monsterAI;

	// Token: 0x0400052C RID: 1324
	private bool m_haveValidUser;

	// Token: 0x02000242 RID: 578
	private enum Speed
	{
		// Token: 0x04001FC8 RID: 8136
		Stop,
		// Token: 0x04001FC9 RID: 8137
		Walk,
		// Token: 0x04001FCA RID: 8138
		Run,
		// Token: 0x04001FCB RID: 8139
		Turn,
		// Token: 0x04001FCC RID: 8140
		NoChange
	}
}
