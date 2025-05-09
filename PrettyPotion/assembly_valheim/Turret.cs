using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// Token: 0x020001D5 RID: 469
public class Turret : MonoBehaviour, Hoverable, Interactable, IPieceMarker, IHasHoverMenu
{
	// Token: 0x06001AD6 RID: 6870 RVA: 0x000C7840 File Offset: 0x000C5A40
	protected void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview)
		{
			this.m_nview.Register<string>("RPC_AddAmmo", new Action<long, string>(this.RPC_AddAmmo));
			this.m_nview.Register<ZDOID>("RPC_SetTarget", new Action<long, ZDOID>(this.RPC_SetTarget));
		}
		this.m_updateTargetTimer = UnityEngine.Random.Range(0f, this.m_updateTargetIntervalNear);
		this.m_baseBodyRotation = this.m_turretBody.transform.localRotation;
		this.m_baseNeckRotation = this.m_turretNeck.transform.localRotation;
		WearNTear component = base.GetComponent<WearNTear>();
		if (component != null)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
		if (this.m_marker)
		{
			this.m_marker.m_radius = this.m_viewDistance;
			this.m_marker.gameObject.SetActive(false);
		}
		foreach (Turret.AmmoType ammoType in this.m_allowedAmmo)
		{
			ammoType.m_visual.SetActive(false);
		}
		if (this.m_nview && this.m_nview.IsValid())
		{
			this.UpdateVisualBolt();
		}
		this.ReadTargets();
	}

	// Token: 0x06001AD7 RID: 6871 RVA: 0x000C79AC File Offset: 0x000C5BAC
	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		this.UpdateReloadState();
		this.UpdateMarker(fixedDeltaTime);
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateTurretRotation();
		this.UpdateVisualBolt();
		if (!this.m_nview.IsOwner())
		{
			if (this.m_nview.IsValid() && this.m_lastUpdateTargetRevision != this.m_nview.GetZDO().DataRevision)
			{
				this.m_lastUpdateTargetRevision = this.m_nview.GetZDO().DataRevision;
				this.ReadTargets();
			}
			return;
		}
		this.UpdateTarget(fixedDeltaTime);
		this.UpdateAttack(fixedDeltaTime);
	}

	// Token: 0x06001AD8 RID: 6872 RVA: 0x000C7A44 File Offset: 0x000C5C44
	private void UpdateTurretRotation()
	{
		if (this.IsCoolingDown())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		bool flag = this.m_target && this.HasAmmo();
		Vector3 forward;
		if (flag)
		{
			if (this.m_lastAmmo == null)
			{
				this.m_lastAmmo = this.GetAmmoItem();
			}
			if (this.m_lastAmmo == null)
			{
				ZLog.LogWarning("Turret had invalid ammo, resetting ammo");
				this.m_nview.GetZDO().Set(ZDOVars.s_ammo, 0, false);
				return;
			}
			float d = Vector2.Distance(this.m_target.transform.position, this.m_eye.transform.position) / this.m_lastAmmo.m_shared.m_attack.m_projectileVel;
			Vector3 b = this.m_target.GetVelocity() * d * this.m_predictionModifier;
			forward = this.m_target.transform.position + b - this.m_turretBody.transform.position;
			float y = forward.y;
			CapsuleCollider componentInChildren = this.m_target.GetComponentInChildren<CapsuleCollider>();
			forward.y = y + ((componentInChildren != null) ? (componentInChildren.height / 2f) : 1f);
		}
		else if (!this.HasAmmo())
		{
			forward = base.transform.forward + new Vector3(0f, -0.3f, 0f);
		}
		else
		{
			this.m_scan += fixedDeltaTime;
			if (this.m_scan > this.m_noTargetScanRate * 2f)
			{
				this.m_scan = 0f;
			}
			forward = Quaternion.Euler(0f, base.transform.rotation.eulerAngles.y + (float)((this.m_scan - this.m_noTargetScanRate > 0f) ? 1 : -1) * this.m_horizontalAngle, 0f) * Vector3.forward;
		}
		forward.Normalize();
		Quaternion quaternion = Quaternion.LookRotation(forward, Vector3.up);
		Vector3 eulerAngles = quaternion.eulerAngles;
		float y2 = base.transform.rotation.eulerAngles.y;
		eulerAngles.y -= y2;
		if (this.m_horizontalAngle >= 0f)
		{
			float num = eulerAngles.y;
			if (num > 180f)
			{
				num -= 360f;
			}
			else if (num < -180f)
			{
				num += 360f;
			}
			if (num > this.m_horizontalAngle)
			{
				eulerAngles = new Vector3(eulerAngles.x, this.m_horizontalAngle + y2, eulerAngles.z);
				quaternion.eulerAngles = eulerAngles;
			}
			else if (num < -this.m_horizontalAngle)
			{
				eulerAngles = new Vector3(eulerAngles.x, -this.m_horizontalAngle + y2, eulerAngles.z);
				quaternion.eulerAngles = eulerAngles;
			}
		}
		Quaternion quaternion2 = Utils.RotateTorwardsSmooth(this.m_turretBody.transform.rotation, quaternion, this.m_lastRotation, this.m_turnRate * fixedDeltaTime, this.m_lookAcceleration, this.m_lookDeacceleration, this.m_lookMinDegreesDelta);
		this.m_lastRotation = this.m_turretBody.transform.rotation;
		this.m_turretBody.transform.rotation = this.m_baseBodyRotation * quaternion2;
		this.m_turretNeck.transform.rotation = this.m_baseNeckRotation * Quaternion.Euler(0f, this.m_turretBody.transform.rotation.eulerAngles.y, this.m_turretBody.transform.rotation.eulerAngles.z);
		this.m_aimDiffToTarget = (flag ? Quaternion.Dot(quaternion2, quaternion) : -1f);
	}

	// Token: 0x06001AD9 RID: 6873 RVA: 0x000C7E08 File Offset: 0x000C6008
	private void UpdateTarget(float dt)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (!this.HasAmmo())
		{
			if (this.m_haveTarget)
			{
				this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetTarget", new object[]
				{
					ZDOID.None
				});
			}
			return;
		}
		this.m_updateTargetTimer -= dt;
		if (this.m_updateTargetTimer <= 0f)
		{
			this.m_updateTargetTimer = (Character.IsCharacterInRange(base.transform.position, 40f) ? this.m_updateTargetIntervalNear : this.m_updateTargetIntervalFar);
			Character character = BaseAI.FindClosestCreature(base.transform, this.m_eye.transform.position, 0f, this.m_viewDistance, this.m_horizontalAngle, false, false, true, this.m_targetPlayers, (this.m_targetItems.Count > 0) ? this.m_targetTamedConfig : this.m_targetTamed, this.m_targetEnemies, this.m_targetCharacters);
			if (character != this.m_target)
			{
				if (character)
				{
					this.m_newTargetEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				}
				else
				{
					this.m_lostTargetEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				}
				this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetTarget", new object[]
				{
					character ? character.GetZDOID() : ZDOID.None
				});
			}
		}
		if (this.m_haveTarget && (!this.m_target || this.m_target.IsDead()))
		{
			ZLog.Log("Target is gone");
			this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetTarget", new object[]
			{
				ZDOID.None
			});
			this.m_lostTargetEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		}
	}

	// Token: 0x06001ADA RID: 6874 RVA: 0x000C8025 File Offset: 0x000C6225
	private void UpdateAttack(float dt)
	{
		if (!this.m_target)
		{
			return;
		}
		if (this.m_aimDiffToTarget < this.m_shootWhenAimDiff)
		{
			return;
		}
		if (!this.HasAmmo())
		{
			return;
		}
		if (this.IsCoolingDown())
		{
			return;
		}
		this.ShootProjectile();
	}

	// Token: 0x06001ADB RID: 6875 RVA: 0x000C805C File Offset: 0x000C625C
	public void ShootProjectile()
	{
		Transform transform = this.m_eye.transform;
		this.m_shootEffect.Create(transform.position, transform.rotation, null, 1f, -1);
		this.m_nview.GetZDO().Set(ZDOVars.s_lastAttack, (float)ZNet.instance.GetTimeSeconds());
		this.m_lastAmmo = this.GetAmmoItem();
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_ammo, 0);
		int num = Mathf.Min(1, (this.m_maxAmmo == 0) ? this.m_lastAmmo.m_shared.m_attack.m_projectiles : Mathf.Min(@int, this.m_lastAmmo.m_shared.m_attack.m_projectiles));
		if (this.m_maxAmmo > 0)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_ammo, @int - num, false);
		}
		ZLog.Log(string.Format("Turret '{0}' is shooting {1} projectiles, ammo: {2}/{3}", new object[]
		{
			base.name,
			num,
			@int,
			this.m_maxAmmo
		}));
		for (int i = 0; i < num; i++)
		{
			Vector3 vector = transform.forward;
			Vector3 axis = Vector3.Cross(vector, Vector3.up);
			float projectileAccuracy = this.m_lastAmmo.m_shared.m_attack.m_projectileAccuracy;
			Quaternion rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(-projectileAccuracy, projectileAccuracy), Vector3.up);
			vector = Quaternion.AngleAxis(UnityEngine.Random.Range(-projectileAccuracy, projectileAccuracy), axis) * vector;
			vector = rotation * vector;
			this.m_lastProjectile = UnityEngine.Object.Instantiate<GameObject>(this.m_lastAmmo.m_shared.m_attack.m_attackProjectile, transform.position, transform.rotation);
			HitData hitData = new HitData();
			hitData.m_toolTier = (short)this.m_lastAmmo.m_shared.m_toolTier;
			hitData.m_pushForce = this.m_lastAmmo.m_shared.m_attackForce;
			hitData.m_backstabBonus = this.m_lastAmmo.m_shared.m_backstabBonus;
			hitData.m_staggerMultiplier = this.m_lastAmmo.m_shared.m_attack.m_staggerMultiplier;
			hitData.m_damage.Add(this.m_lastAmmo.GetDamage(), 1);
			hitData.m_statusEffectHash = (this.m_lastAmmo.m_shared.m_attackStatusEffect ? this.m_lastAmmo.m_shared.m_attackStatusEffect.NameHash() : 0);
			hitData.m_blockable = this.m_lastAmmo.m_shared.m_blockable;
			hitData.m_dodgeable = this.m_lastAmmo.m_shared.m_dodgeable;
			hitData.m_skill = this.m_lastAmmo.m_shared.m_skillType;
			hitData.m_itemWorldLevel = (byte)Game.m_worldLevel;
			hitData.m_hitType = HitData.HitType.Turret;
			if (this.m_lastAmmo.m_shared.m_attackStatusEffect != null)
			{
				hitData.m_statusEffectHash = this.m_lastAmmo.m_shared.m_attackStatusEffect.NameHash();
			}
			IProjectile component = this.m_lastProjectile.GetComponent<IProjectile>();
			if (component != null)
			{
				component.Setup(null, vector * this.m_lastAmmo.m_shared.m_attack.m_projectileVel, this.m_hitNoise, hitData, null, this.m_lastAmmo);
			}
		}
	}

	// Token: 0x06001ADC RID: 6876 RVA: 0x000C83A4 File Offset: 0x000C65A4
	public bool IsCoolingDown()
	{
		return this.m_nview.IsValid() && (double)(this.m_nview.GetZDO().GetFloat(ZDOVars.s_lastAttack, 0f) + this.m_attackCooldown) > ZNet.instance.GetTimeSeconds();
	}

	// Token: 0x06001ADD RID: 6877 RVA: 0x000C83E3 File Offset: 0x000C65E3
	public bool HasAmmo()
	{
		return this.m_maxAmmo == 0 || this.GetAmmo() > 0;
	}

	// Token: 0x06001ADE RID: 6878 RVA: 0x000C83F8 File Offset: 0x000C65F8
	public int GetAmmo()
	{
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_ammo, 0);
	}

	// Token: 0x06001ADF RID: 6879 RVA: 0x000C8410 File Offset: 0x000C6610
	public string GetAmmoType()
	{
		if (!this.m_defaultAmmo)
		{
			return this.m_nview.GetZDO().GetString(ZDOVars.s_ammoType, "");
		}
		return this.m_defaultAmmo.name;
	}

	// Token: 0x06001AE0 RID: 6880 RVA: 0x000C8448 File Offset: 0x000C6648
	public void UpdateReloadState()
	{
		bool flag = this.IsCoolingDown();
		if (!this.m_turretBodyArmed.activeInHierarchy && !flag)
		{
			this.m_reloadEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		}
		this.m_turretBodyArmed.SetActive(!flag);
		this.m_turretBodyUnarmed.SetActive(flag);
	}

	// Token: 0x06001AE1 RID: 6881 RVA: 0x000C84B0 File Offset: 0x000C66B0
	private ItemDrop.ItemData GetAmmoItem()
	{
		string ammoType = this.GetAmmoType();
		GameObject prefab = ZNetScene.instance.GetPrefab(ammoType);
		if (!prefab)
		{
			ZLog.LogWarning("Turret '" + base.name + "' is trying to fire but has no ammo or default ammo!");
			return null;
		}
		return prefab.GetComponent<ItemDrop>().m_itemData;
	}

	// Token: 0x06001AE2 RID: 6882 RVA: 0x000C8500 File Offset: 0x000C6700
	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		if (!this.m_targetEnemies)
		{
			return Localization.instance.Localize(this.m_name);
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		this.sb.Clear();
		this.sb.Append((!this.HasAmmo()) ? (this.m_name + " ($piece_turret_noammo)") : string.Format("{0} ({1} / {2})", this.m_name, this.GetAmmo(), this.m_maxAmmo));
		if (this.m_targetCharacters.Count == 0)
		{
			this.sb.Append(" $piece_turret_target $piece_turret_target_everything");
		}
		else
		{
			this.sb.Append(" $piece_turret_target ");
			this.sb.Append(this.m_targetsText);
		}
		this.sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_turret_addammo\n[<color=yellow><b>1-8</b></color>] $piece_turret_target_set");
		return Localization.instance.Localize(this.sb.ToString());
	}

	// Token: 0x06001AE3 RID: 6883 RVA: 0x000C862E File Offset: 0x000C682E
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x06001AE4 RID: 6884 RVA: 0x000C8636 File Offset: 0x000C6836
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			if (this.m_holdRepeatInterval <= 0f)
			{
				return false;
			}
			if (Time.time - this.m_lastUseTime < this.m_holdRepeatInterval)
			{
				return false;
			}
		}
		this.m_lastUseTime = Time.time;
		return this.UseItem(character, null);
	}

	// Token: 0x06001AE5 RID: 6885 RVA: 0x000C8674 File Offset: 0x000C6874
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (item == null)
		{
			item = this.FindAmmoItem(user.GetInventory(), true);
			if (item == null)
			{
				if (this.GetAmmo() > 0 && this.FindAmmoItem(user.GetInventory(), false) != null)
				{
					ItemDrop component = ZNetScene.instance.GetPrefab(this.GetAmmoType()).GetComponent<ItemDrop>();
					user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_turretotherammo") + Localization.instance.Localize(component.m_itemData.m_shared.m_name), 0, null);
					return false;
				}
				user.Message(MessageHud.MessageType.Center, "$msg_noturretammo", 0, null);
				return false;
			}
		}
		foreach (Turret.TrophyTarget trophyTarget in this.m_configTargets)
		{
			if (item.m_shared.m_name == trophyTarget.m_item.m_itemData.m_shared.m_name)
			{
				if (this.m_targetItems.Contains(trophyTarget.m_item))
				{
					this.m_targetItems.Remove(trophyTarget.m_item);
				}
				else
				{
					if (this.m_targetItems.Count >= this.m_maxConfigTargets)
					{
						this.m_targetItems.RemoveAt(0);
					}
					this.m_targetItems.Add(trophyTarget.m_item);
				}
				this.SetTargets();
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$piece_turret_target_set_msg " + ((this.m_targetCharacters.Count == 0) ? "$piece_turret_target_everything" : this.m_targetsText)), 0, null);
				this.m_setTargetEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				Game.instance.IncrementPlayerStat(PlayerStatType.TurretTrophySet, 1f);
				return true;
			}
		}
		if (!this.IsItemAllowed(item.m_dropPrefab.name))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_wontwork", 0, null);
			return false;
		}
		if (this.GetAmmo() > 0 && this.GetAmmoType() != item.m_dropPrefab.name)
		{
			ItemDrop component2 = ZNetScene.instance.GetPrefab(this.GetAmmoType()).GetComponent<ItemDrop>();
			user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_turretotherammo") + Localization.instance.Localize(component2.m_itemData.m_shared.m_name), 0, null);
			return false;
		}
		ZLog.Log("trying to add ammo " + item.m_shared.m_name);
		if (this.GetAmmo() >= this.m_maxAmmo)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null);
			return false;
		}
		user.Message(MessageHud.MessageType.Center, "$msg_added " + item.m_shared.m_name, 0, null);
		user.GetInventory().RemoveItem(item, 1);
		Game.instance.IncrementPlayerStat(PlayerStatType.TurretAmmoAdded, 1f);
		this.m_nview.InvokeRPC("RPC_AddAmmo", new object[]
		{
			item.m_dropPrefab.name
		});
		return true;
	}

	// Token: 0x06001AE6 RID: 6886 RVA: 0x000C8998 File Offset: 0x000C6B98
	private void RPC_AddAmmo(long sender, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.IsItemAllowed(name))
		{
			ZLog.Log("Item not allowed " + name);
			return;
		}
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_ammo, 0);
		this.m_nview.GetZDO().Set(ZDOVars.s_ammo, @int + 1, false);
		this.m_nview.GetZDO().Set(ZDOVars.s_ammoType, name);
		this.m_addAmmoEffect.Create(this.m_turretBody.transform.position, this.m_turretBody.transform.rotation, null, 1f, -1);
		this.UpdateVisualBolt();
		ZLog.Log("Added ammo " + name);
	}

	// Token: 0x06001AE7 RID: 6887 RVA: 0x000C8A5C File Offset: 0x000C6C5C
	private void RPC_SetTarget(long sender, ZDOID character)
	{
		GameObject gameObject = ZNetScene.instance.FindInstance(character);
		if (gameObject)
		{
			Character component = gameObject.GetComponent<Character>();
			if (component != null)
			{
				this.m_target = component;
				this.m_haveTarget = true;
				return;
			}
		}
		this.m_target = null;
		this.m_haveTarget = false;
		this.m_scan = 0f;
	}

	// Token: 0x06001AE8 RID: 6888 RVA: 0x000C8AB0 File Offset: 0x000C6CB0
	private void UpdateVisualBolt()
	{
		if (this.HasAmmo())
		{
			bool flag = !this.IsCoolingDown();
		}
		string ammoType = this.GetAmmoType();
		bool flag2 = this.HasAmmo() && !this.IsCoolingDown();
		foreach (Turret.AmmoType ammoType2 in this.m_allowedAmmo)
		{
			bool flag3 = ammoType2.m_ammo.name == ammoType;
			ammoType2.m_visual.SetActive(flag3 && flag2);
		}
	}

	// Token: 0x06001AE9 RID: 6889 RVA: 0x000C8B4C File Offset: 0x000C6D4C
	private bool IsItemAllowed(string itemName)
	{
		using (List<Turret.AmmoType>.Enumerator enumerator = this.m_allowedAmmo.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_ammo.name == itemName)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x06001AEA RID: 6890 RVA: 0x000C8BB0 File Offset: 0x000C6DB0
	private ItemDrop.ItemData FindAmmoItem(Inventory inventory, bool onlyCurrentlyLoadableType)
	{
		if (onlyCurrentlyLoadableType && this.HasAmmo())
		{
			return inventory.GetAmmoItem(this.m_ammoType, this.GetAmmoType());
		}
		return inventory.GetAmmoItem(this.m_ammoType, null);
	}

	// Token: 0x06001AEB RID: 6891 RVA: 0x000C8BE0 File Offset: 0x000C6DE0
	private void OnDestroyed()
	{
		if (this.m_nview.IsOwner() && this.m_returnAmmoOnDestroy)
		{
			int ammo = this.GetAmmo();
			string ammoType = this.GetAmmoType();
			GameObject prefab = ZNetScene.instance.GetPrefab(ammoType);
			for (int i = 0; i < ammo; i++)
			{
				Vector3 position = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
				Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
				UnityEngine.Object.Instantiate<GameObject>(prefab, position, rotation);
			}
		}
	}

	// Token: 0x06001AEC RID: 6892 RVA: 0x000C8C7E File Offset: 0x000C6E7E
	public void ShowHoverMarker()
	{
		this.ShowBuildMarker();
	}

	// Token: 0x06001AED RID: 6893 RVA: 0x000C8C86 File Offset: 0x000C6E86
	public void ShowBuildMarker()
	{
		if (this.m_marker)
		{
			this.m_marker.gameObject.SetActive(true);
			base.CancelInvoke("HideMarker");
			base.Invoke("HideMarker", this.m_markerHideTime);
		}
	}

	// Token: 0x06001AEE RID: 6894 RVA: 0x000C8CC4 File Offset: 0x000C6EC4
	private void UpdateMarker(float dt)
	{
		if (this.m_marker && this.m_marker.isActiveAndEnabled)
		{
			this.m_marker.m_start = base.transform.rotation.eulerAngles.y - this.m_horizontalAngle;
			this.m_marker.m_turns = this.m_horizontalAngle * 2f / 360f;
		}
	}

	// Token: 0x06001AEF RID: 6895 RVA: 0x000C8D32 File Offset: 0x000C6F32
	private void HideMarker()
	{
		if (this.m_marker)
		{
			this.m_marker.gameObject.SetActive(false);
		}
	}

	// Token: 0x06001AF0 RID: 6896 RVA: 0x000C8D54 File Offset: 0x000C6F54
	private void SetTargets()
	{
		if (!this.m_nview.IsOwner())
		{
			this.m_nview.ClaimOwnership();
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_targets, this.m_targetItems.Count, false);
		for (int i = 0; i < this.m_targetItems.Count; i++)
		{
			this.m_nview.GetZDO().Set("target" + i.ToString(), this.m_targetItems[i].m_itemData.m_shared.m_name);
		}
		this.ReadTargets();
	}

	// Token: 0x06001AF1 RID: 6897 RVA: 0x000C8DF4 File Offset: 0x000C6FF4
	private void ReadTargets()
	{
		if (!this.m_nview || !this.m_nview.IsValid())
		{
			return;
		}
		this.m_targetItems.Clear();
		this.m_targetCharacters.Clear();
		this.m_targetsText = "";
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_targets, 0);
		for (int i = 0; i < @int; i++)
		{
			string @string = this.m_nview.GetZDO().GetString("target" + i.ToString(), "");
			foreach (Turret.TrophyTarget trophyTarget in this.m_configTargets)
			{
				if (trophyTarget.m_item.m_itemData.m_shared.m_name == @string)
				{
					this.m_targetItems.Add(trophyTarget.m_item);
					this.m_targetCharacters.AddRange(trophyTarget.m_targets);
					if (this.m_targetsText.Length > 0)
					{
						this.m_targetsText += ", ";
					}
					if (!string.IsNullOrEmpty(trophyTarget.m_nameOverride))
					{
						this.m_targetsText += trophyTarget.m_nameOverride;
						break;
					}
					for (int j = 0; j < trophyTarget.m_targets.Count; j++)
					{
						this.m_targetsText += trophyTarget.m_targets[j].m_name;
						if (j + 1 < trophyTarget.m_targets.Count)
						{
							this.m_targetsText += ", ";
						}
					}
					break;
				}
			}
		}
	}

	// Token: 0x06001AF2 RID: 6898 RVA: 0x000C8FDC File Offset: 0x000C71DC
	public bool TryGetItems(Player player, out List<string> items)
	{
		items = new List<string>();
		if (!this.CanUseItems(player, true))
		{
			return true;
		}
		ItemDrop.ItemData itemData = this.FindAmmoItem(player.GetInventory(), true);
		if (this.GetAmmo() > 0)
		{
			items.Add(itemData.m_shared.m_name);
		}
		else
		{
			items.AddRange(from ammoType in this.m_allowedAmmo
			select ammoType.m_ammo.m_itemData.m_shared.m_name);
		}
		return true;
	}

	// Token: 0x06001AF3 RID: 6899 RVA: 0x000C905C File Offset: 0x000C725C
	public bool CanUseItems(Player player, bool sendErrorMessage = true)
	{
		if (this.GetAmmo() >= this.m_maxAmmo)
		{
			if (sendErrorMessage)
			{
				player.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null);
			}
			return false;
		}
		Inventory inventory = player.GetInventory();
		ItemDrop.ItemData itemData = this.FindAmmoItem(inventory, true);
		if (this.GetAmmo() > 0 && itemData == null)
		{
			ItemDrop component = ZNetScene.instance.GetPrefab(this.GetAmmoType()).GetComponent<ItemDrop>();
			if (sendErrorMessage)
			{
				player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_turretotherammo") + Localization.instance.Localize(component.m_itemData.m_shared.m_name), 0, null);
			}
			return false;
		}
		itemData = this.FindAmmoItem(inventory, false);
		if (itemData != null)
		{
			return true;
		}
		if (sendErrorMessage)
		{
			player.Message(MessageHud.MessageType.Center, "$msg_noturretammo", 0, null);
		}
		return false;
	}

	// Token: 0x04001B4B RID: 6987
	public string m_name = "Turret";

	// Token: 0x04001B4C RID: 6988
	[Header("Turret")]
	public GameObject m_turretBody;

	// Token: 0x04001B4D RID: 6989
	public GameObject m_turretBodyArmed;

	// Token: 0x04001B4E RID: 6990
	public GameObject m_turretBodyUnarmed;

	// Token: 0x04001B4F RID: 6991
	public GameObject m_turretNeck;

	// Token: 0x04001B50 RID: 6992
	public GameObject m_eye;

	// Token: 0x04001B51 RID: 6993
	[Header("Look & Scan")]
	public float m_turnRate = 10f;

	// Token: 0x04001B52 RID: 6994
	public float m_horizontalAngle = 25f;

	// Token: 0x04001B53 RID: 6995
	public float m_verticalAngle = 20f;

	// Token: 0x04001B54 RID: 6996
	public float m_viewDistance = 10f;

	// Token: 0x04001B55 RID: 6997
	public float m_noTargetScanRate = 10f;

	// Token: 0x04001B56 RID: 6998
	public float m_lookAcceleration = 1.2f;

	// Token: 0x04001B57 RID: 6999
	public float m_lookDeacceleration = 0.05f;

	// Token: 0x04001B58 RID: 7000
	public float m_lookMinDegreesDelta = 0.005f;

	// Token: 0x04001B59 RID: 7001
	[Header("Attack Settings (rest in projectile)")]
	public ItemDrop m_defaultAmmo;

	// Token: 0x04001B5A RID: 7002
	public float m_attackCooldown = 1f;

	// Token: 0x04001B5B RID: 7003
	public float m_attackWarmup = 1f;

	// Token: 0x04001B5C RID: 7004
	public float m_hitNoise = 10f;

	// Token: 0x04001B5D RID: 7005
	public float m_shootWhenAimDiff = 0.9f;

	// Token: 0x04001B5E RID: 7006
	public float m_predictionModifier = 1f;

	// Token: 0x04001B5F RID: 7007
	public float m_updateTargetIntervalNear = 1f;

	// Token: 0x04001B60 RID: 7008
	public float m_updateTargetIntervalFar = 10f;

	// Token: 0x04001B61 RID: 7009
	[Header("Ammo")]
	public int m_maxAmmo;

	// Token: 0x04001B62 RID: 7010
	public string m_ammoType = "$ammo_turretbolt";

	// Token: 0x04001B63 RID: 7011
	public List<Turret.AmmoType> m_allowedAmmo = new List<Turret.AmmoType>();

	// Token: 0x04001B64 RID: 7012
	public bool m_returnAmmoOnDestroy = true;

	// Token: 0x04001B65 RID: 7013
	public float m_holdRepeatInterval = 0.2f;

	// Token: 0x04001B66 RID: 7014
	[Header("Target mode: Everything")]
	public bool m_targetPlayers = true;

	// Token: 0x04001B67 RID: 7015
	public bool m_targetTamed = true;

	// Token: 0x04001B68 RID: 7016
	public bool m_targetEnemies = true;

	// Token: 0x04001B69 RID: 7017
	[Header("Target mode: Configured")]
	public bool m_targetTamedConfig;

	// Token: 0x04001B6A RID: 7018
	public List<Turret.TrophyTarget> m_configTargets = new List<Turret.TrophyTarget>();

	// Token: 0x04001B6B RID: 7019
	public int m_maxConfigTargets = 1;

	// Token: 0x04001B6C RID: 7020
	[Header("Effects")]
	public CircleProjector m_marker;

	// Token: 0x04001B6D RID: 7021
	public float m_markerHideTime = 0.5f;

	// Token: 0x04001B6E RID: 7022
	public EffectList m_shootEffect;

	// Token: 0x04001B6F RID: 7023
	public EffectList m_addAmmoEffect;

	// Token: 0x04001B70 RID: 7024
	public EffectList m_reloadEffect;

	// Token: 0x04001B71 RID: 7025
	public EffectList m_warmUpStartEffect;

	// Token: 0x04001B72 RID: 7026
	public EffectList m_newTargetEffect;

	// Token: 0x04001B73 RID: 7027
	public EffectList m_lostTargetEffect;

	// Token: 0x04001B74 RID: 7028
	public EffectList m_setTargetEffect;

	// Token: 0x04001B75 RID: 7029
	private ZNetView m_nview;

	// Token: 0x04001B76 RID: 7030
	private GameObject m_lastProjectile;

	// Token: 0x04001B77 RID: 7031
	private ItemDrop.ItemData m_lastAmmo;

	// Token: 0x04001B78 RID: 7032
	private Character m_target;

	// Token: 0x04001B79 RID: 7033
	private bool m_haveTarget;

	// Token: 0x04001B7A RID: 7034
	private Quaternion m_baseBodyRotation;

	// Token: 0x04001B7B RID: 7035
	private Quaternion m_baseNeckRotation;

	// Token: 0x04001B7C RID: 7036
	private Quaternion m_lastRotation;

	// Token: 0x04001B7D RID: 7037
	private float m_aimDiffToTarget;

	// Token: 0x04001B7E RID: 7038
	private float m_updateTargetTimer;

	// Token: 0x04001B7F RID: 7039
	private float m_lastUseTime;

	// Token: 0x04001B80 RID: 7040
	private float m_scan;

	// Token: 0x04001B81 RID: 7041
	private readonly List<ItemDrop> m_targetItems = new List<ItemDrop>();

	// Token: 0x04001B82 RID: 7042
	private readonly List<Character> m_targetCharacters = new List<Character>();

	// Token: 0x04001B83 RID: 7043
	private string m_targetsText;

	// Token: 0x04001B84 RID: 7044
	private readonly StringBuilder sb = new StringBuilder();

	// Token: 0x04001B85 RID: 7045
	private uint m_lastUpdateTargetRevision = uint.MaxValue;

	// Token: 0x02000392 RID: 914
	[Serializable]
	public struct AmmoType
	{
		// Token: 0x040026B5 RID: 9909
		public ItemDrop m_ammo;

		// Token: 0x040026B6 RID: 9910
		public GameObject m_visual;
	}

	// Token: 0x02000393 RID: 915
	[Serializable]
	public struct TrophyTarget
	{
		// Token: 0x040026B7 RID: 9911
		public string m_nameOverride;

		// Token: 0x040026B8 RID: 9912
		public ItemDrop m_item;

		// Token: 0x040026B9 RID: 9913
		public List<Character> m_targets;
	}
}
