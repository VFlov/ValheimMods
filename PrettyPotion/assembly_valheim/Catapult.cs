using System;
using System.Collections.Generic;
using System.Linq;
using Dynamics;
using UnityEngine;
using UnityEngine.Serialization;

// Token: 0x0200015F RID: 351
public class Catapult : MonoBehaviour
{
	// Token: 0x06001554 RID: 5460 RVA: 0x0009C558 File Offset: 0x0009A758
	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_wagon = base.GetComponent<Vagon>();
		this.m_rigidBody = base.GetComponent<Rigidbody>();
		this.m_baseMass = this.m_rigidBody.mass;
		this.m_legRotations = new Vector3[this.m_legs.Count];
		this.m_legRotationQuat = new Quaternion[this.m_legs.Count];
		for (int i = 0; i < this.m_legs.Count; i++)
		{
			Switch @switch = this.m_legs[i];
			@switch.m_onUse = (Switch.Callback)Delegate.Combine(@switch.m_onUse, new Switch.Callback(this.OnLegUse));
			Switch switch2 = this.m_legs[i];
			switch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(switch2.m_onHover, new Switch.TooltipCallback(this.OnLegHover));
			this.m_legRotations[i] = this.m_legs[i].transform.localEulerAngles;
			this.m_legRotationQuat[i] = this.m_legs[i].transform.localRotation;
		}
		Switch loadPoint = this.m_loadPoint;
		loadPoint.m_onUse = (Switch.Callback)Delegate.Combine(loadPoint.m_onUse, new Switch.Callback(this.OnLoadPointUse));
		Switch loadPoint2 = this.m_loadPoint;
		loadPoint2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(loadPoint2.m_onHover, new Switch.TooltipCallback(this.OnHoverLoadPoint));
		this.m_armRotation = this.m_arm.transform.localRotation.eulerAngles;
		this.m_armDynamics = new FloatDynamics(this.m_armDynamicsSettings, 0f);
		this.m_legDynamics = new FloatDynamics(this.m_legDynamicsSettings, 1f);
		this.m_legAnimationCurveUp = new AnimationCurve();
		for (int j = 0; j < this.m_legAnimationCurve.keys.Length; j++)
		{
			Keyframe key = this.m_legAnimationCurve.keys[j];
			key.value = 1f - key.value;
			this.m_legAnimationCurveUp.AddKey(key);
		}
		if (this.m_nview)
		{
			ZDO zdo = this.m_nview.GetZDO();
			if (zdo != null && zdo.GetBool(ZDOVars.s_locked, false))
			{
				this.m_lockedLegs = zdo.GetBool(ZDOVars.s_locked, false);
			}
		}
		this.m_nview.Register("RPC_Shoot", new Action<long>(this.RPC_Shoot));
		this.m_nview.Register<bool>("RPC_OnLegUse", new Action<long, bool>(this.RPC_OnLegUse));
		this.m_nview.Register<string>("RPC_SetLoadedVisual", new Action<long, string>(this.RPC_SetLoadedVisual));
		this.m_legAnimTimer = 1f;
		this.m_movingLegs = true;
		this.UpdateLegAnimation(Time.fixedDeltaTime);
		if (Catapult.m_characterMask == 0)
		{
			Catapult.m_characterMask = LayerMask.GetMask(new string[]
			{
				"character"
			});
		}
	}

	// Token: 0x06001555 RID: 5461 RVA: 0x0009C83C File Offset: 0x0009AA3C
	private void FixedUpdate()
	{
		this.UpdateArmAnimation(Time.fixedDeltaTime);
		this.UpdateLegAnimation(Time.fixedDeltaTime);
	}

	// Token: 0x06001556 RID: 5462 RVA: 0x0009C854 File Offset: 0x0009AA54
	private void UpdateArmAnimation(float dt)
	{
		float num = this.m_armAnimTime / this.m_armAnimationTime;
		float num2 = this.m_armDynamics.Update(dt, this.m_armAnimation.Evaluate(num), float.NegativeInfinity, false);
		this.m_arm.transform.localEulerAngles = new Vector3(this.m_armRotation.x + num2 * this.m_armAnimationDegrees, this.m_armRotation.y, this.m_armRotation.z);
		if (this.m_armAnimTime <= 0f)
		{
			return;
		}
		this.m_armAnimTime += dt;
		if (this.m_armAnimTime > this.m_armAnimationTime)
		{
			this.m_armAnimTime = 0f;
			this.m_arm.transform.localEulerAngles = this.m_armRotation;
			this.m_armReturnEffect.Create(this.m_loadPoint.transform.position, this.m_loadPoint.transform.rotation, null, 1f, -1);
			return;
		}
		if (num > this.m_releaseAnimationTime && (this.m_loadedItem != null || this.m_launchCharacters.Count > 0))
		{
			this.Release();
			return;
		}
		if (this.m_preLaunchForce > 0f)
		{
			Vector3 normalized = (this.m_forceVector.transform.position - base.transform.position).normalized;
			foreach (Character character in this.m_launchCharacters)
			{
				character.ForceJump(normalized * this.m_preLaunchForce, true);
			}
		}
	}

	// Token: 0x06001557 RID: 5463 RVA: 0x0009C9FC File Offset: 0x0009ABFC
	private void UpdateLegAnimation(float dt)
	{
		if (!this.m_nview || !this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_movingLegs)
		{
			this.m_legAnimTimer += Time.deltaTime;
			if (this.m_legAnimTimer >= this.m_legAnimationTime)
			{
				this.m_movingLegs = false;
				for (int i = 0; i < this.m_legs.Count; i++)
				{
					Vector3 position = this.m_legs[i].transform.GetChild(0).transform.position;
					if (!this.m_lockedLegs)
					{
						this.m_legUpDoneEffect.Create(this.m_legs[i].transform.position, this.m_legs[i].transform.rotation, null, 1f, -1);
					}
					else
					{
						this.m_legDownDoneEffect.Create(position, Quaternion.identity, null, 1f, -1);
						this.m_rigidBody.mass = this.m_legDownMass;
					}
				}
				return;
			}
		}
		float time = this.m_legAnimTimer / this.m_legAnimationTime;
		AnimationCurve animationCurve = this.m_lockedLegs ? this.m_legAnimationCurve : this.m_legAnimationCurveUp;
		float num = this.m_legDynamics.Update(dt, animationCurve.Evaluate(time), float.NegativeInfinity, false);
		for (int j = 0; j < this.m_legs.Count; j++)
		{
			Vector3 localEulerAngles = this.m_legRotations[j];
			localEulerAngles.z += num * this.m_legAnimationDegrees;
			this.m_legs[j].transform.localEulerAngles = localEulerAngles;
		}
	}

	// Token: 0x06001558 RID: 5464 RVA: 0x0009CBA4 File Offset: 0x0009ADA4
	private bool OnLegUse(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (this.m_movingLegs)
		{
			return false;
		}
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_OnLegUse", new object[]
		{
			!this.m_lockedLegs
		});
		if (!this.m_nview.IsOwner())
		{
			this.m_nview.InvokeRPC("RPC_RequestOwn", Array.Empty<object>());
		}
		return true;
	}

	// Token: 0x06001559 RID: 5465 RVA: 0x0009CC0C File Offset: 0x0009AE0C
	private void RPC_OnLegUse(long sender, bool value)
	{
		this.m_lockedLegs = value;
		if (this.m_nview && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_locked, this.m_lockedLegs);
		}
		this.m_legAnimTimer = 0f;
		this.m_movingLegs = true;
		if (this.m_lockedLegs)
		{
			this.m_legDownEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			return;
		}
		this.m_legUpEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		this.m_rigidBody.mass = this.m_baseMass;
	}

	// Token: 0x0600155A RID: 5466 RVA: 0x0009CCD3 File Offset: 0x0009AED3
	private string OnLegHover()
	{
		if (this.m_movingLegs)
		{
			return "";
		}
		return Localization.instance.Localize(this.m_lockedLegs ? "[<color=yellow><b>$KEY_Use</b></color>] $piece_catapult_legsup" : "[<color=yellow><b>$KEY_Use</b></color>] $piece_catapult_legsdown");
	}

	// Token: 0x0600155B RID: 5467 RVA: 0x0009CD04 File Offset: 0x0009AF04
	private bool OnLoadPointUse(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (this.m_loadedItem != null || item == null || this.m_armAnimTime != 0f)
		{
			user.UseIemBlockkMessage();
			return false;
		}
		if (!this.CanItemBeLoaded(item))
		{
			user.Message(MessageHud.MessageType.Center, "$piece_catapult_wontfit", 0, null);
			user.UseIemBlockkMessage();
			return false;
		}
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetLoadedVisual", new object[]
		{
			item.m_dropPrefab.name
		});
		this.m_loadedItem = item;
		this.m_loadStack = Mathf.Min(item.m_stack, this.m_maxLoadStack);
		base.Invoke("Shoot", this.m_shootAfterLoadDelay);
		if (item.m_equipped)
		{
			user.UnequipItem(item, true);
		}
		user.GetInventory().RemoveItem(item, this.m_loadStack);
		if (!this.m_nview.IsOwner())
		{
			this.m_nview.InvokeRPC("RPC_RequestOwn", Array.Empty<object>());
		}
		this.m_loadItemEffect.Create(this.m_loadPoint.transform.position, this.m_loadPoint.transform.rotation, null, 1f, -1);
		return true;
	}

	// Token: 0x0600155C RID: 5468 RVA: 0x0009CE24 File Offset: 0x0009B024
	private bool CanItemBeLoaded(ItemDrop.ItemData item)
	{
		return this.m_includeItemsOverride.Any((ItemDrop x) => x.m_itemData.m_shared.m_name == item.m_shared.m_name) || ((!this.m_onlyUseIncludedProjectiles || !(ItemStand.GetAttachPrefab(item.m_dropPrefab) == null)) && (!this.m_defaultIncludeAndListExclude || !this.m_includeExcludeTypesList.Contains(item.m_shared.m_itemType)) && (this.m_defaultIncludeAndListExclude || this.m_includeExcludeTypesList.Contains(item.m_shared.m_itemType)) && !this.m_excludeItemsOverride.Any((ItemDrop x) => x.m_itemData.m_shared.m_name == item.m_shared.m_name));
	}

	// Token: 0x0600155D RID: 5469 RVA: 0x0009CEE5 File Offset: 0x0009B0E5
	private void Shoot()
	{
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_Shoot", Array.Empty<object>());
	}

	// Token: 0x0600155E RID: 5470 RVA: 0x0009CF04 File Offset: 0x0009B104
	private void RPC_Shoot(long sender)
	{
		this.m_shootStartEffect.Create(this.m_loadPoint.transform.position, this.m_loadPoint.transform.rotation, null, 1f, -1);
		this.m_armAnimTime = 1E-06f;
		this.CollectLaunchCharacters();
	}

	// Token: 0x0600155F RID: 5471 RVA: 0x0009CF58 File Offset: 0x0009B158
	private void RPC_SetLoadedVisual(long sender, string name)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if (component != null)
		{
			this.m_loadedItem = component.m_itemData;
		}
		GameObject gameObject = ItemStand.GetAttachPrefab(itemPrefab);
		if (gameObject == null)
		{
			ZLog.LogError("Valid catapult ammo '" + name + "' is missing attach prefab, aborting.");
			return;
		}
		gameObject = ItemStand.GetAttachGameObject(gameObject);
		this.m_visualItem = UnityEngine.Object.Instantiate<GameObject>(gameObject, this.m_loadPoint.transform);
		this.m_visualItem.transform.localPosition = Vector3.zero;
	}

	// Token: 0x06001560 RID: 5472 RVA: 0x0009CFDE File Offset: 0x0009B1DE
	private void Release()
	{
		this.ShootProjectile();
		this.LaunchCharacters();
		this.m_loadedItem = null;
	}

	// Token: 0x06001561 RID: 5473 RVA: 0x0009CFF4 File Offset: 0x0009B1F4
	private void ShootProjectile()
	{
		Vector3 a = this.m_forceVector.transform.position - base.transform.position;
		Vector3 vector = a.normalized;
		this.m_shootReleaseEffect.Create(this.m_loadPoint.transform.position, Quaternion.LookRotation(vector), null, 1f, -1);
		Projectile projectile = this.m_projectile;
		bool flag = this.m_includeItemsOverride.Any((ItemDrop x) => x.m_itemData.m_shared.m_name == this.m_loadedItem.m_shared.m_name);
		if ((!this.m_onlyUseIncludedProjectiles || (this.m_onlyUseIncludedProjectiles && flag)) && this.m_loadedItem.m_shared.m_attack.m_attackProjectile != null)
		{
			Projectile component = this.m_loadedItem.m_shared.m_attack.m_attackProjectile.GetComponent<Projectile>();
			if (component != null)
			{
				projectile = component;
			}
		}
		this.m_lastAmmo = this.m_defaultAmmo.m_itemData;
		if (this.m_nview.IsOwner())
		{
			for (int i = 0; i < this.m_loadStack; i++)
			{
				this.m_lastProjectile = UnityEngine.Object.Instantiate<Projectile>(projectile, this.m_shootPoint.transform.position, this.m_shootPoint.transform.rotation);
				HitData hitData = new HitData();
				if (projectile == this.m_projectile)
				{
					if (this.m_lastProjectile.m_visual)
					{
						this.m_lastProjectile.m_visual.gameObject.SetActive(false);
					}
					this.m_lastProjectile.GetComponent<ZNetView>().GetZDO().Set(ZDOVars.s_visual, this.m_loadedItem.m_dropPrefab.name);
					Collider componentInChildren = this.m_lastProjectile.m_visual.GetComponentInChildren<Collider>();
					if (componentInChildren != null)
					{
						componentInChildren.enabled = false;
					}
					if (!this.m_onlyIncludedItemsDealDamage || (this.m_onlyIncludedItemsDealDamage && flag))
					{
						hitData.m_toolTier = (short)this.m_lastAmmo.m_shared.m_toolTier;
						hitData.m_pushForce = this.m_lastAmmo.m_shared.m_attackForce;
						hitData.m_backstabBonus = this.m_lastAmmo.m_shared.m_backstabBonus;
						hitData.m_staggerMultiplier = this.m_lastAmmo.m_shared.m_attack.m_staggerMultiplier;
						hitData.m_damage.Add(this.m_lastAmmo.GetDamage(), 1);
						hitData.m_statusEffectHash = (this.m_lastAmmo.m_shared.m_attackStatusEffect ? this.m_lastAmmo.m_shared.m_attackStatusEffect.NameHash() : 0);
						hitData.m_blockable = this.m_lastAmmo.m_shared.m_blockable;
						hitData.m_dodgeable = this.m_lastAmmo.m_shared.m_dodgeable;
						hitData.m_skill = this.m_lastAmmo.m_shared.m_skillType;
						if (this.m_lastAmmo.m_shared.m_attackStatusEffect != null)
						{
							hitData.m_statusEffectHash = this.m_lastAmmo.m_shared.m_attackStatusEffect.NameHash();
						}
					}
				}
				else if (!this.m_onlyIncludedItemsDealDamage || (this.m_onlyIncludedItemsDealDamage && flag))
				{
					hitData.m_toolTier = (short)this.m_loadedItem.m_shared.m_toolTier;
					hitData.m_pushForce = this.m_loadedItem.m_shared.m_attackForce;
					hitData.m_backstabBonus = this.m_loadedItem.m_shared.m_backstabBonus;
					hitData.m_damage.Add(this.m_loadedItem.GetDamage(), 1);
					hitData.m_statusEffectHash = (this.m_loadedItem.m_shared.m_attackStatusEffect ? this.m_loadedItem.m_shared.m_attackStatusEffect.NameHash() : 0);
					hitData.m_skillLevel = 1f;
					hitData.m_itemLevel = (short)this.m_loadedItem.m_quality;
					hitData.m_itemWorldLevel = (byte)this.m_loadedItem.m_worldLevel;
					hitData.m_blockable = this.m_loadedItem.m_shared.m_blockable;
					hitData.m_dodgeable = this.m_loadedItem.m_shared.m_dodgeable;
					hitData.m_skill = this.m_loadedItem.m_shared.m_skillType;
					hitData.m_hitType = HitData.HitType.Catapult;
				}
				if (this.m_lastAmmo.m_shared.m_attack.m_projectileAccuracyMin > 0f || this.m_lastAmmo.m_shared.m_attack.m_projectileAccuracy > 0f)
				{
					float num = UnityEngine.Random.Range(this.m_lastAmmo.m_shared.m_attack.m_projectileAccuracyMin, this.m_lastAmmo.m_shared.m_attack.m_projectileAccuracy);
					Vector3 axis = Vector3.Cross(vector, Vector3.up);
					Quaternion rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(-num, num), Vector3.up);
					vector = Quaternion.AngleAxis(UnityEngine.Random.Range(-num, num), axis) * vector;
					vector = rotation * vector;
				}
				Vector3 velocity = a * this.m_lastAmmo.m_shared.m_attack.m_projectileVel * UnityEngine.Random.Range(1f, 1f + this.m_shootVelocityVariation);
				projectile.m_respawnItemOnHit = !flag;
				this.m_lastProjectile.Setup(null, velocity, this.m_hitNoise, hitData, this.m_loadedItem, this.m_lastAmmo);
				this.m_lastProjectile.m_rotateVisual = UnityEngine.Random.Range(this.m_randomRotationMin, this.m_randomRotationMax);
				this.m_lastProjectile.m_rotateVisualY = UnityEngine.Random.Range(this.m_randomRotationMin, this.m_randomRotationMax);
				this.m_lastProjectile.m_rotateVisualZ = UnityEngine.Random.Range(this.m_randomRotationMin, this.m_randomRotationMax);
			}
		}
		UnityEngine.Object.Destroy(this.m_visualItem);
		this.m_visualItem = null;
	}

	// Token: 0x06001562 RID: 5474 RVA: 0x0009D59C File Offset: 0x0009B79C
	private void CollectLaunchCharacters()
	{
		this.m_launchCharacters.Clear();
		int num = Physics.OverlapSphereNonAlloc(this.m_launchCollectArea.transform.position, this.m_launchCollectArea.radius, this.m_colliders, Catapult.m_characterMask);
		for (int i = 0; i < num; i++)
		{
			Character componentInParent = this.m_colliders[i].GetComponentInParent<Character>();
			if (componentInParent != null)
			{
				ZNetView component = componentInParent.GetComponent<ZNetView>();
				if (component != null && component.IsOwner())
				{
					this.m_launchCharacters.Add(componentInParent);
					componentInParent.SetTempParent(this.m_arm.transform);
				}
			}
		}
	}

	// Token: 0x06001563 RID: 5475 RVA: 0x0009D62C File Offset: 0x0009B82C
	private void LaunchCharacters()
	{
		foreach (Character character in this.m_launchCharacters)
		{
			character.ReleaseTempParent();
			Vector3 normalized = (this.m_forceVector.transform.position - base.transform.position).normalized;
			character.ForceJump(normalized * this.m_launchForce, true);
			character.StandUpOnNextGround();
		}
		this.m_launchCharacters.Clear();
	}

	// Token: 0x06001564 RID: 5476 RVA: 0x0009D6CC File Offset: 0x0009B8CC
	private string OnHoverLoadPoint()
	{
		return Localization.instance.Localize((this.m_loadedItem == null) ? "[<color=yellow><b>1-8</b></color>] $piece_catapult_placeitem" : "");
	}

	// Token: 0x040014C6 RID: 5318
	[Header("Legs")]
	public List<Switch> m_legs = new List<Switch>();

	// Token: 0x040014C7 RID: 5319
	[FormerlySerializedAs("m_legAnimationDown")]
	public AnimationCurve m_legAnimationCurve;

	// Token: 0x040014C8 RID: 5320
	private AnimationCurve m_legAnimationCurveUp;

	// Token: 0x040014C9 RID: 5321
	public float m_legAnimationDegrees = 90f;

	// Token: 0x040014CA RID: 5322
	public float m_legAnimationUpMultiplier = 1f;

	// Token: 0x040014CB RID: 5323
	public float m_legAnimationTime = 5f;

	// Token: 0x040014CC RID: 5324
	public float m_legDownMass = 500f;

	// Token: 0x040014CD RID: 5325
	[Header("Shooting")]
	public GameObject m_forceVector;

	// Token: 0x040014CE RID: 5326
	public GameObject m_arm;

	// Token: 0x040014CF RID: 5327
	public Switch m_loadPoint;

	// Token: 0x040014D0 RID: 5328
	public Transform m_shootPoint;

	// Token: 0x040014D1 RID: 5329
	public AnimationCurve m_armAnimation;

	// Token: 0x040014D2 RID: 5330
	public float m_armAnimationDegrees = 180f;

	// Token: 0x040014D3 RID: 5331
	public float m_armAnimationTime = 2f;

	// Token: 0x040014D4 RID: 5332
	public float m_releaseAnimationTime;

	// Token: 0x040014D5 RID: 5333
	public float m_shootAfterLoadDelay = 1f;

	// Token: 0x040014D6 RID: 5334
	public Projectile m_projectile;

	// Token: 0x040014D7 RID: 5335
	public ItemDrop m_defaultAmmo;

	// Token: 0x040014D8 RID: 5336
	public int m_maxLoadStack = 1;

	// Token: 0x040014D9 RID: 5337
	public float m_hitNoise = 1f;

	// Token: 0x040014DA RID: 5338
	public float m_randomRotationMin = 2f;

	// Token: 0x040014DB RID: 5339
	public float m_randomRotationMax = 10f;

	// Token: 0x040014DC RID: 5340
	public float m_shootVelocityVariation = 0.1f;

	// Token: 0x040014DD RID: 5341
	[Header("Dynamics")]
	public DynamicsParameters m_armDynamicsSettings;

	// Token: 0x040014DE RID: 5342
	private FloatDynamics m_armDynamics;

	// Token: 0x040014DF RID: 5343
	public DynamicsParameters m_legDynamicsSettings;

	// Token: 0x040014E0 RID: 5344
	private FloatDynamics m_legDynamics;

	// Token: 0x040014E1 RID: 5345
	[Header("Ammo")]
	[global::Tooltip("If checked, will include all except listed types. If unchecked, will exclude all except listed types.")]
	public bool m_defaultIncludeAndListExclude = true;

	// Token: 0x040014E2 RID: 5346
	public bool m_onlyUseIncludedProjectiles = true;

	// Token: 0x040014E3 RID: 5347
	public bool m_onlyIncludedItemsDealDamage = true;

	// Token: 0x040014E4 RID: 5348
	public List<ItemDrop.ItemData.ItemType> m_includeExcludeTypesList = new List<ItemDrop.ItemData.ItemType>();

	// Token: 0x040014E5 RID: 5349
	public List<ItemDrop> m_includeItemsOverride = new List<ItemDrop>();

	// Token: 0x040014E6 RID: 5350
	public List<ItemDrop> m_excludeItemsOverride = new List<ItemDrop>();

	// Token: 0x040014E7 RID: 5351
	[Header("Character Launching")]
	public SphereCollider m_launchCollectArea;

	// Token: 0x040014E8 RID: 5352
	public float m_preLaunchForce = 5f;

	// Token: 0x040014E9 RID: 5353
	public float m_launchForce = 100f;

	// Token: 0x040014EA RID: 5354
	[Header("Effects")]
	public EffectList m_legDownEffect = new EffectList();

	// Token: 0x040014EB RID: 5355
	public EffectList m_legDownDoneEffect = new EffectList();

	// Token: 0x040014EC RID: 5356
	public EffectList m_legUpEffect = new EffectList();

	// Token: 0x040014ED RID: 5357
	public EffectList m_legUpDoneEffect = new EffectList();

	// Token: 0x040014EE RID: 5358
	public EffectList m_shootStartEffect = new EffectList();

	// Token: 0x040014EF RID: 5359
	public EffectList m_shootReleaseEffect = new EffectList();

	// Token: 0x040014F0 RID: 5360
	public EffectList m_armReturnEffect = new EffectList();

	// Token: 0x040014F1 RID: 5361
	public EffectList m_loadItemEffect = new EffectList();

	// Token: 0x040014F2 RID: 5362
	private static int m_characterMask;

	// Token: 0x040014F3 RID: 5363
	private ZNetView m_nview;

	// Token: 0x040014F4 RID: 5364
	private ZNetView m_wagonNview;

	// Token: 0x040014F5 RID: 5365
	private Vagon m_wagon;

	// Token: 0x040014F6 RID: 5366
	private Rigidbody m_rigidBody;

	// Token: 0x040014F7 RID: 5367
	private float m_baseMass;

	// Token: 0x040014F8 RID: 5368
	private ItemDrop.ItemData m_loadedItem;

	// Token: 0x040014F9 RID: 5369
	private int m_loadStack;

	// Token: 0x040014FA RID: 5370
	private GameObject m_visualItem;

	// Token: 0x040014FB RID: 5371
	private GameObject m_shotItem;

	// Token: 0x040014FC RID: 5372
	private bool m_lockedLegs;

	// Token: 0x040014FD RID: 5373
	private Vector3[] m_legRotations;

	// Token: 0x040014FE RID: 5374
	private Quaternion[] m_legRotationQuat;

	// Token: 0x040014FF RID: 5375
	private float m_legAnimTimer;

	// Token: 0x04001500 RID: 5376
	private bool m_movingLegs;

	// Token: 0x04001501 RID: 5377
	private Vector3 m_armRotation;

	// Token: 0x04001502 RID: 5378
	private float m_armAnimTime;

	// Token: 0x04001503 RID: 5379
	private Projectile m_lastProjectile;

	// Token: 0x04001504 RID: 5380
	private ItemDrop.ItemData m_lastAmmo;

	// Token: 0x04001505 RID: 5381
	private Collider[] m_colliders = new Collider[10];

	// Token: 0x04001506 RID: 5382
	private List<Character> m_launchCharacters = new List<Character>();
}
