using System;
using System.Collections.Generic;
using Splatform;
using UnityEngine;

// Token: 0x02000044 RID: 68
public class Tameable : MonoBehaviour, Interactable, TextReceiver
{
	// Token: 0x06000564 RID: 1380 RVA: 0x0002DFA0 File Offset: 0x0002C1A0
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_character = base.GetComponent<Character>();
		this.m_monsterAI = base.GetComponent<MonsterAI>();
		this.m_piece = base.GetComponent<Piece>();
		if (this.m_character)
		{
			Character character = this.m_character;
			character.m_onDeath = (Action)Delegate.Combine(character.m_onDeath, new Action(this.OnDeath));
		}
		if (this.m_monsterAI)
		{
			MonsterAI monsterAI = this.m_monsterAI;
			monsterAI.m_onConsumedItem = (Action<ItemDrop>)Delegate.Combine(monsterAI.m_onConsumedItem, new Action<ItemDrop>(this.OnConsumedItem));
		}
		if (this.m_nview.IsValid())
		{
			this.m_nview.Register<ZDOID, bool>("Command", new Action<long, ZDOID, bool>(this.RPC_Command));
			this.m_nview.Register<string, string>("SetName", new Action<long, string, string>(this.RPC_SetName));
			this.m_nview.Register("RPC_UnSummon", new Action<long>(this.RPC_UnSummon));
			if (this.m_saddle != null)
			{
				this.m_nview.Register("AddSaddle", new Action<long>(this.RPC_AddSaddle));
				this.m_nview.Register<bool>("SetSaddle", new Action<long, bool>(this.RPC_SetSaddle));
				this.SetSaddle(this.HaveSaddle());
			}
			base.InvokeRepeating("TamingUpdate", 3f, 3f);
		}
		if (this.m_startsTamed && this.m_character)
		{
			this.m_character.SetTamed(true);
		}
		if (this.m_randomStartingName.Count > 0 && this.m_nview.IsValid() && this.m_nview.GetZDO().GetString(ZDOVars.s_tamedName, "").Length == 0)
		{
			this.SetText(Localization.instance.Localize(this.m_randomStartingName[UnityEngine.Random.Range(0, this.m_randomStartingName.Count)]));
		}
	}

	// Token: 0x06000565 RID: 1381 RVA: 0x0002E199 File Offset: 0x0002C399
	public void Update()
	{
		this.UpdateSummon();
		this.UpdateSavedFollowTarget();
	}

	// Token: 0x06000566 RID: 1382 RVA: 0x0002E1A8 File Offset: 0x0002C3A8
	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		string text = this.GetName();
		if (this.IsTamed())
		{
			if (this.m_character)
			{
				text += Localization.instance.Localize(" ( $hud_tame, " + this.GetStatusString() + " )");
			}
			text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] $hud_pet");
			if (ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive())
			{
				text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltKeys + $KEY_Use</b></color>] $hud_rename");
			}
			else
			{
				text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $hud_rename");
			}
			return text;
		}
		int tameness = this.GetTameness();
		if (tameness <= 0)
		{
			text += Localization.instance.Localize(" ( $hud_wild, " + this.GetStatusString() + " )");
		}
		else
		{
			text += Localization.instance.Localize(string.Concat(new string[]
			{
				" ( $hud_tameness  ",
				tameness.ToString(),
				"%, ",
				this.GetStatusString(),
				" )"
			}));
		}
		return text;
	}

	// Token: 0x06000567 RID: 1383 RVA: 0x0002E2DC File Offset: 0x0002C4DC
	public string GetStatusString()
	{
		if (this.m_monsterAI && this.m_monsterAI.IsAlerted())
		{
			return "$hud_tamefrightened";
		}
		if (this.IsHungry())
		{
			return "$hud_tamehungry";
		}
		if (!this.m_character || this.m_character.IsTamed())
		{
			return "$hud_tamehappy";
		}
		return "$hud_tameinprogress";
	}

	// Token: 0x06000568 RID: 1384 RVA: 0x0002E33C File Offset: 0x0002C53C
	public bool IsTamed()
	{
		return (this.m_character && this.m_character.IsTamed()) || this.m_startsTamed;
	}

	// Token: 0x06000569 RID: 1385 RVA: 0x0002E360 File Offset: 0x0002C560
	public string GetName()
	{
		return Localization.instance.Localize(this.m_character ? this.m_character.m_name : ((this.m_nview && this.m_nview.IsValid()) ? this.m_nview.GetZDO().GetString(ZDOVars.s_tamedName, this.m_piece.m_name) : this.m_piece.m_name));
	}

	// Token: 0x0600056A RID: 1386 RVA: 0x0002E3D8 File Offset: 0x0002C5D8
	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (hold)
		{
			return false;
		}
		if (alt)
		{
			this.SetName();
			return true;
		}
		string hoverName = this.GetHoverName();
		if (!this.IsTamed())
		{
			return false;
		}
		if (Time.time - this.m_lastPetTime > 1f)
		{
			this.m_lastPetTime = Time.time;
			this.m_petEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			if (this.m_commandable)
			{
				this.Command(user, true);
			}
			else
			{
				MessageHud.MessageType type = MessageHud.MessageType.Center;
				string msg;
				if (this.m_tameTextGetter != null)
				{
					string text = this.m_tameTextGetter();
					if (text != null && text.Length > 0)
					{
						msg = text;
						goto IL_D3;
					}
				}
				msg = (this.m_nameBeforeText ? (hoverName + " " + this.m_tameText) : this.m_tameText);
				IL_D3:
				user.Message(type, msg, 0, null);
			}
			return true;
		}
		return false;
	}

	// Token: 0x0600056B RID: 1387 RVA: 0x0002E4C4 File Offset: 0x0002C6C4
	public string GetHoverName()
	{
		if (!this.IsTamed())
		{
			return this.GetName();
		}
		string text = this.GetText().RemoveRichTextTags();
		if (text.Length > 0)
		{
			return text;
		}
		return this.GetName();
	}

	// Token: 0x0600056C RID: 1388 RVA: 0x0002E500 File Offset: 0x0002C700
	private void SetName()
	{
		if (!this.IsTamed())
		{
			return;
		}
		PrivilegeResult privilegeResult = PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.ViewUserGeneratedContent);
		if (!privilegeResult.IsGranted())
		{
			if (PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege != null)
			{
				PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.Open(Privilege.ViewUserGeneratedContent);
				if (!PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.IsOpen)
				{
					ZLog.LogError(string.Format("{0} can't resolve the {1} privilege on this platform, which was denied with result {2}. Tameable rename was blocked without meaningful feedback to the user!", "ResolvePrivilegeUI", Privilege.ViewUserGeneratedContent, privilegeResult));
					return;
				}
			}
			else
			{
				ZLog.LogError(string.Format("{0} is not available on this platform to resolve the {1} privilege, which was denied with result {2}. Tameable rename was blocked without meaningful feedback to the user!", "ResolvePrivilegeUI", Privilege.ViewUserGeneratedContent, privilegeResult));
			}
			return;
		}
		TextInput.instance.RequestText(this, "$hud_rename", 10);
	}

	// Token: 0x0600056D RID: 1389 RVA: 0x0002E5C0 File Offset: 0x0002C7C0
	public string GetText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		return CensorShittyWords.FilterUGC(this.m_nview.GetZDO().GetString(ZDOVars.s_tamedName, ""), UGCType.Text, new PlatformUserID(this.m_nview.GetZDO().GetString(ZDOVars.s_tamedNameAuthor, "")), 0L);
	}

	// Token: 0x0600056E RID: 1390 RVA: 0x0002E624 File Offset: 0x0002C824
	public void SetText(string text)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("SetName", new object[]
		{
			text,
			PlatformManager.DistributionPlatform.LocalUser.PlatformUserID.ToString()
		});
	}

	// Token: 0x0600056F RID: 1391 RVA: 0x0002E67C File Offset: 0x0002C87C
	private void RPC_SetName(long sender, string name, string authorId)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.IsTamed())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_tamedName, name);
		this.m_nview.GetZDO().Set(ZDOVars.s_tamedNameAuthor, authorId);
	}

	// Token: 0x06000570 RID: 1392 RVA: 0x0002E6DC File Offset: 0x0002C8DC
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!(this.m_saddleItem != null) || !this.IsTamed() || !(item.m_shared.m_name == this.m_saddleItem.m_itemData.m_shared.m_name))
		{
			return false;
		}
		if (this.HaveSaddle())
		{
			user.Message(MessageHud.MessageType.Center, this.GetHoverName() + " $hud_saddle_already", 0, null);
			return true;
		}
		this.m_nview.InvokeRPC("AddSaddle", Array.Empty<object>());
		user.GetInventory().RemoveOneItem(item);
		user.Message(MessageHud.MessageType.Center, this.GetHoverName() + " $hud_saddle_ready", 0, null);
		return true;
	}

	// Token: 0x06000571 RID: 1393 RVA: 0x0002E79C File Offset: 0x0002C99C
	private void RPC_AddSaddle(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.HaveSaddle())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_haveSaddleHash, true);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetSaddle", new object[]
		{
			true
		});
	}

	// Token: 0x06000572 RID: 1394 RVA: 0x0002E7FC File Offset: 0x0002C9FC
	public bool DropSaddle(Vector3 userPoint)
	{
		if (!this.HaveSaddle())
		{
			return false;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_haveSaddleHash, false);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetSaddle", new object[]
		{
			false
		});
		Vector3 flyDirection = userPoint - base.transform.position;
		this.SpawnSaddle(flyDirection);
		return true;
	}

	// Token: 0x06000573 RID: 1395 RVA: 0x0002E868 File Offset: 0x0002CA68
	private void SpawnSaddle(Vector3 flyDirection)
	{
		Rigidbody component = UnityEngine.Object.Instantiate<GameObject>(this.m_saddleItem.gameObject, base.transform.TransformPoint(this.m_dropSaddleOffset), Quaternion.identity).GetComponent<Rigidbody>();
		if (component)
		{
			Vector3 a = Vector3.up;
			if (flyDirection.magnitude > 0.1f)
			{
				flyDirection.y = 0f;
				flyDirection.Normalize();
				a += flyDirection;
			}
			component.AddForce(a * this.m_dropItemVel, ForceMode.VelocityChange);
		}
	}

	// Token: 0x06000574 RID: 1396 RVA: 0x0002E8EB File Offset: 0x0002CAEB
	private bool HaveSaddle()
	{
		return !(this.m_saddle == null) && this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool(ZDOVars.s_haveSaddleHash, false);
	}

	// Token: 0x06000575 RID: 1397 RVA: 0x0002E922 File Offset: 0x0002CB22
	private void RPC_SetSaddle(long sender, bool enabled)
	{
		this.SetSaddle(enabled);
	}

	// Token: 0x06000576 RID: 1398 RVA: 0x0002E92B File Offset: 0x0002CB2B
	private void SetSaddle(bool enabled)
	{
		ZLog.Log("Setting saddle:" + enabled.ToString());
		if (this.m_saddle != null)
		{
			this.m_saddle.gameObject.SetActive(enabled);
		}
	}

	// Token: 0x06000577 RID: 1399 RVA: 0x0002E964 File Offset: 0x0002CB64
	private void TamingUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.IsTamed())
		{
			return;
		}
		if (this.IsHungry())
		{
			return;
		}
		if (!this.m_monsterAI || this.m_monsterAI.IsAlerted())
		{
			return;
		}
		this.m_monsterAI.SetDespawnInDay(false);
		this.m_monsterAI.SetEventCreature(false);
		this.DecreaseRemainingTime(3f);
		if (this.GetRemainingTime() <= 0f)
		{
			this.Tame();
			return;
		}
		this.m_sootheEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
	}

	// Token: 0x06000578 RID: 1400 RVA: 0x0002EA1C File Offset: 0x0002CC1C
	private void Tame()
	{
		Game.instance.IncrementPlayerStat(PlayerStatType.CreatureTamed, 1f);
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner() || !this.m_monsterAI || !this.m_character)
		{
			return;
		}
		if (this.IsTamed())
		{
			return;
		}
		this.m_monsterAI.MakeTame();
		this.m_tamedEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 30f);
		if (closestPlayer)
		{
			closestPlayer.Message(MessageHud.MessageType.Center, this.m_character.m_name + " $hud_tamedone", 0, null);
		}
	}

	// Token: 0x06000579 RID: 1401 RVA: 0x0002EAE8 File Offset: 0x0002CCE8
	public static void TameAllInArea(Vector3 point, float radius)
	{
		foreach (Character character in Character.GetAllCharacters())
		{
			if (!character.IsPlayer())
			{
				Tameable component = character.GetComponent<Tameable>();
				if (component)
				{
					component.Tame();
				}
			}
		}
	}

	// Token: 0x0600057A RID: 1402 RVA: 0x0002EB50 File Offset: 0x0002CD50
	public void Command(Humanoid user, bool message = true)
	{
		this.m_nview.InvokeRPC("Command", new object[]
		{
			user.GetZDOID(),
			message
		});
	}

	// Token: 0x0600057B RID: 1403 RVA: 0x0002EB80 File Offset: 0x0002CD80
	private Player GetPlayer(ZDOID characterID)
	{
		GameObject gameObject = ZNetScene.instance.FindInstance(characterID);
		if (gameObject)
		{
			return gameObject.GetComponent<Player>();
		}
		return null;
	}

	// Token: 0x0600057C RID: 1404 RVA: 0x0002EBAC File Offset: 0x0002CDAC
	private void RPC_Command(long sender, ZDOID characterID, bool message)
	{
		Player player = this.GetPlayer(characterID);
		if (player == null || !this.m_monsterAI)
		{
			return;
		}
		if (this.m_monsterAI.GetFollowTarget())
		{
			this.m_monsterAI.SetFollowTarget(null);
			this.m_monsterAI.SetPatrolPoint();
			if (this.m_nview.IsOwner())
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_follow, "");
			}
			if (message)
			{
				player.Message(MessageHud.MessageType.Center, this.GetHoverName() + " $hud_tamestay", 0, null);
			}
		}
		else
		{
			this.m_monsterAI.ResetPatrolPoint();
			this.m_monsterAI.SetFollowTarget(player.gameObject);
			if (this.m_nview.IsOwner())
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_follow, player.GetPlayerName());
			}
			if (message)
			{
				player.Message(MessageHud.MessageType.Center, this.GetHoverName() + " $hud_tamefollow", 0, null);
			}
			int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_maxInstances, 0);
			if (@int > 0)
			{
				this.UnsummonMaxInstances(@int);
			}
		}
		this.m_unsummonTime = 0f;
	}

	// Token: 0x0600057D RID: 1405 RVA: 0x0002ECDC File Offset: 0x0002CEDC
	private void UpdateSavedFollowTarget()
	{
		if (!this.m_monsterAI || this.m_monsterAI.GetFollowTarget() != null || !this.m_nview.IsOwner())
		{
			return;
		}
		string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_follow, "");
		if (string.IsNullOrEmpty(@string))
		{
			return;
		}
		foreach (Player player in Player.GetAllPlayers())
		{
			if (player.GetPlayerName() == @string)
			{
				this.Command(player, false);
				return;
			}
		}
		if (this.m_unsummonOnOwnerLogoutSeconds > 0f)
		{
			this.m_unsummonTime += Time.fixedDeltaTime;
			if (this.m_unsummonTime > this.m_unsummonOnOwnerLogoutSeconds)
			{
				this.UnSummon();
			}
		}
	}

	// Token: 0x0600057E RID: 1406 RVA: 0x0002EDC4 File Offset: 0x0002CFC4
	public bool IsHungry()
	{
		if (!this.m_character)
		{
			return false;
		}
		if (this.m_nview == null)
		{
			return false;
		}
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo == null)
		{
			return false;
		}
		DateTime d = new DateTime(zdo.GetLong(ZDOVars.s_tameLastFeeding, 0L));
		return (ZNet.instance.GetTime() - d).TotalSeconds > (double)this.m_fedDuration;
	}

	// Token: 0x0600057F RID: 1407 RVA: 0x0002EE38 File Offset: 0x0002D038
	private void ResetFeedingTimer()
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_tameLastFeeding, ZNet.instance.GetTime().Ticks);
	}

	// Token: 0x06000580 RID: 1408 RVA: 0x0002EE6C File Offset: 0x0002D06C
	private void OnDeath()
	{
		ZLog.Log("Valid " + this.m_nview.IsValid().ToString());
		ZLog.Log("On death " + this.HaveSaddle().ToString());
		if (this.HaveSaddle() && this.m_dropSaddleOnDeath)
		{
			ZLog.Log("Spawning saddle ");
			this.SpawnSaddle(Vector3.zero);
		}
	}

	// Token: 0x06000581 RID: 1409 RVA: 0x0002EEE0 File Offset: 0x0002D0E0
	private int GetTameness()
	{
		float remainingTime = this.GetRemainingTime();
		return (int)((1f - Mathf.Clamp01(remainingTime / this.m_tamingTime)) * 100f);
	}

	// Token: 0x06000582 RID: 1410 RVA: 0x0002EF10 File Offset: 0x0002D110
	private void OnConsumedItem(ItemDrop item)
	{
		if (this.IsHungry())
		{
			this.m_sootheEffect.Create(this.m_character ? this.m_character.GetCenterPoint() : base.transform.position, Quaternion.identity, null, 1f, -1);
		}
		this.ResetFeedingTimer();
	}

	// Token: 0x06000583 RID: 1411 RVA: 0x0002EF68 File Offset: 0x0002D168
	private void DecreaseRemainingTime(float time)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		float num = this.GetRemainingTime();
		Tameable.s_nearbyPlayers.Clear();
		Player.GetPlayersInRange(base.transform.position, this.m_tamingSpeedMultiplierRange, Tameable.s_nearbyPlayers);
		using (List<Player>.Enumerator enumerator = Tameable.s_nearbyPlayers.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.TamingBoost))
				{
					time *= this.m_tamingBoostMultiplier;
				}
			}
		}
		num -= time;
		if (num < 0f)
		{
			num = 0f;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_tameTimeLeft, num);
	}

	// Token: 0x06000584 RID: 1412 RVA: 0x0002F02C File Offset: 0x0002D22C
	private float GetRemainingTime()
	{
		if (!this.m_nview.IsValid())
		{
			return 0f;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_tameTimeLeft, this.m_tamingTime);
	}

	// Token: 0x06000585 RID: 1413 RVA: 0x0002F05C File Offset: 0x0002D25C
	public bool HaveRider()
	{
		return this.m_saddle && this.m_saddle.HaveValidUser();
	}

	// Token: 0x06000586 RID: 1414 RVA: 0x0002F078 File Offset: 0x0002D278
	public float GetRiderSkill()
	{
		if (this.m_saddle)
		{
			return this.m_saddle.GetRiderSkill();
		}
		return 0f;
	}

	// Token: 0x06000587 RID: 1415 RVA: 0x0002F098 File Offset: 0x0002D298
	private void UpdateSummon()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_unsummonDistance > 0f && this.m_monsterAI)
		{
			GameObject followTarget = this.m_monsterAI.GetFollowTarget();
			if (followTarget && Vector3.Distance(followTarget.transform.position, base.gameObject.transform.position) > this.m_unsummonDistance)
			{
				this.UnSummon();
			}
		}
	}

	// Token: 0x06000588 RID: 1416 RVA: 0x0002F11C File Offset: 0x0002D31C
	private void UnsummonMaxInstances(int maxInstances)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner() || !this.m_character)
		{
			return;
		}
		GameObject followTarget = this.m_monsterAI.GetFollowTarget();
		string text;
		if (followTarget != null)
		{
			Player component = followTarget.GetComponent<Player>();
			if (component != null)
			{
				text = component.GetPlayerName();
				goto IL_4A;
			}
		}
		text = null;
		IL_4A:
		string text2 = text;
		if (text2 == null)
		{
			return;
		}
		List<Character> allCharacters = Character.GetAllCharacters();
		List<BaseAI> list = new List<BaseAI>();
		foreach (Character character in allCharacters)
		{
			if (character.m_name == this.m_character.m_name)
			{
				ZNetView component2 = character.GetComponent<ZNetView>();
				if (component2 == null)
				{
					goto IL_9F;
				}
				ZDO zdo = component2.GetZDO();
				if (zdo == null)
				{
					goto IL_9F;
				}
				string a2 = zdo.GetString(ZDOVars.s_follow, "");
				IL_B7:
				if (!(a2 == text2))
				{
					continue;
				}
				MonsterAI component3 = character.GetComponent<MonsterAI>();
				if (component3 != null)
				{
					list.Add(component3);
					continue;
				}
				continue;
				IL_9F:
				a2 = "";
				goto IL_B7;
			}
		}
		list.Sort((BaseAI a, BaseAI b) => b.GetTimeSinceSpawned().CompareTo(a.GetTimeSinceSpawned()));
		int num = list.Count - maxInstances;
		for (int i = 0; i < num; i++)
		{
			Tameable component4 = list[i].GetComponent<Tameable>();
			if (component4 != null)
			{
				component4.UnSummon();
			}
		}
		if (num > 0 && Player.m_localPlayer)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$hud_maxsummonsreached", 0, null);
		}
	}

	// Token: 0x06000589 RID: 1417 RVA: 0x0002F2A4 File Offset: 0x0002D4A4
	private void UnSummon()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_UnSummon", Array.Empty<object>());
	}

	// Token: 0x0600058A RID: 1418 RVA: 0x0002F2D0 File Offset: 0x0002D4D0
	private void RPC_UnSummon(long sender)
	{
		this.m_unSummonEffect.Create(base.gameObject.transform.position, base.gameObject.transform.rotation, null, 1f, -1);
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		ZNetScene.instance.Destroy(base.gameObject);
	}

	// Token: 0x0400060F RID: 1551
	private const float m_playerMaxDistance = 15f;

	// Token: 0x04000610 RID: 1552
	private const float m_tameDeltaTime = 3f;

	// Token: 0x04000611 RID: 1553
	private static List<Player> s_nearbyPlayers = new List<Player>();

	// Token: 0x04000612 RID: 1554
	public float m_fedDuration = 30f;

	// Token: 0x04000613 RID: 1555
	public float m_tamingTime = 1800f;

	// Token: 0x04000614 RID: 1556
	public bool m_startsTamed;

	// Token: 0x04000615 RID: 1557
	public EffectList m_tamedEffect = new EffectList();

	// Token: 0x04000616 RID: 1558
	public EffectList m_sootheEffect = new EffectList();

	// Token: 0x04000617 RID: 1559
	public EffectList m_petEffect = new EffectList();

	// Token: 0x04000618 RID: 1560
	public bool m_commandable;

	// Token: 0x04000619 RID: 1561
	public float m_unsummonDistance;

	// Token: 0x0400061A RID: 1562
	public float m_unsummonOnOwnerLogoutSeconds;

	// Token: 0x0400061B RID: 1563
	public EffectList m_unSummonEffect = new EffectList();

	// Token: 0x0400061C RID: 1564
	public Skills.SkillType m_levelUpOwnerSkill;

	// Token: 0x0400061D RID: 1565
	public float m_levelUpFactor = 1f;

	// Token: 0x0400061E RID: 1566
	public ItemDrop m_saddleItem;

	// Token: 0x0400061F RID: 1567
	public Sadle m_saddle;

	// Token: 0x04000620 RID: 1568
	public bool m_dropSaddleOnDeath = true;

	// Token: 0x04000621 RID: 1569
	public Vector3 m_dropSaddleOffset = new Vector3(0f, 1f, 0f);

	// Token: 0x04000622 RID: 1570
	public float m_dropItemVel = 5f;

	// Token: 0x04000623 RID: 1571
	public List<string> m_randomStartingName = new List<string>();

	// Token: 0x04000624 RID: 1572
	public float m_tamingSpeedMultiplierRange = 60f;

	// Token: 0x04000625 RID: 1573
	public float m_tamingBoostMultiplier = 2f;

	// Token: 0x04000626 RID: 1574
	public bool m_nameBeforeText = true;

	// Token: 0x04000627 RID: 1575
	public string m_tameText = "$hud_tamelove";

	// Token: 0x04000628 RID: 1576
	public Tameable.TextGetter m_tameTextGetter;

	// Token: 0x04000629 RID: 1577
	private Character m_character;

	// Token: 0x0400062A RID: 1578
	private MonsterAI m_monsterAI;

	// Token: 0x0400062B RID: 1579
	private Piece m_piece;

	// Token: 0x0400062C RID: 1580
	private ZNetView m_nview;

	// Token: 0x0400062D RID: 1581
	private float m_lastPetTime;

	// Token: 0x0400062E RID: 1582
	private float m_unsummonTime;

	// Token: 0x0200024A RID: 586
	// (Invoke) Token: 0x06001EF4 RID: 7924
	public delegate string TextGetter();
}
