using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// Token: 0x020001B3 RID: 435
public class ShieldGenerator : MonoBehaviour, Hoverable, Interactable, IHasHoverMenu
{
	// Token: 0x06001954 RID: 6484 RVA: 0x000BCF58 File Offset: 0x000BB158
	private void Start()
	{
		if (Player.IsPlacementGhost(base.gameObject))
		{
			base.enabled = false;
			this.m_isPlacementGhost = true;
			return;
		}
		ShieldGenerator.m_instances.Add(this);
		ShieldGenerator.m_instanceChangeID++;
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview == null)
		{
			this.m_nview = base.GetComponentInParent<ZNetView>();
		}
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.m_addFuelSwitch)
		{
			Switch addFuelSwitch = this.m_addFuelSwitch;
			addFuelSwitch.m_onUse = (Switch.Callback)Delegate.Combine(addFuelSwitch.m_onUse, new Switch.Callback(this.OnAddFuel));
			this.m_addFuelSwitch.m_onHover = new Switch.TooltipCallback(this.OnHoverAddFuel);
		}
		this.m_nview.Register("RPC_AddFuel", new Action<long>(this.RPC_AddFuel));
		this.m_nview.Register("RPC_Attack", new Action<long>(this.RPC_Attack));
		this.m_nview.Register("RPC_HitNow", new Action<long>(this.RPC_HitNow));
		this.m_projectileMask = LayerMask.GetMask(Array.Empty<string>());
		if (!ShieldGenerator.m_shieldDomeEffect)
		{
			ShieldGenerator.m_shieldDomeEffect = UnityEngine.Object.FindFirstObjectByType<ShieldDomeImageEffect>();
		}
		if (!this.m_enableAttack && this.m_fuelItems.Count == 0)
		{
			this.m_addFuelSwitch.gameObject.SetActive(false);
		}
		this.m_particleFlareGradient = new Gradient();
		this.m_particleFlareGradient.colorKeys = new GradientColorKey[]
		{
			new GradientColorKey(Color.white, 0f)
		};
		this.m_particleFlareGradient.alphaKeys = new GradientAlphaKey[]
		{
			new GradientAlphaKey(0f, 0f)
		};
		this.m_propertyBlock = new MaterialPropertyBlock();
		this.m_meshRenderers = this.m_enabledObject.GetComponentsInChildren<MeshRenderer>();
		base.InvokeRepeating("UpdateShield", 0f, 0.22f);
	}

	// Token: 0x06001955 RID: 6485 RVA: 0x000BD150 File Offset: 0x000BB350
	public string GetHoverText()
	{
		if (!this.m_enableAttack)
		{
			return "";
		}
		if (this.m_attackCharge <= 0f)
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_shieldgenerator_waiting");
		}
		if (this.m_attackCharge >= 1f)
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_shieldgenerator_ready \n[<color=yellow><b>$KEY_Use</b></color>] $piece_shieldgenerator_use");
		}
		return Localization.instance.Localize(this.m_name + "\n$piece_shieldgenerator_charging " + (Terminal.m_showTests ? this.m_attackCharge.ToString("0.00") : ""));
	}

	// Token: 0x06001956 RID: 6486 RVA: 0x000BD1F3 File Offset: 0x000BB3F3
	public string GetHoverName()
	{
		return "";
	}

	// Token: 0x06001957 RID: 6487 RVA: 0x000BD1FC File Offset: 0x000BB3FC
	public bool Interact(Humanoid user, bool repeat, bool alt)
	{
		if (!this.m_enableAttack)
		{
			return false;
		}
		if (repeat)
		{
			return false;
		}
		if (user == Player.m_localPlayer && this.m_attackCharge >= 1f)
		{
			this.m_nview.InvokeRPC("RPC_Attack", Array.Empty<object>());
		}
		return false;
	}

	// Token: 0x06001958 RID: 6488 RVA: 0x000BD248 File Offset: 0x000BB448
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06001959 RID: 6489 RVA: 0x000BD24C File Offset: 0x000BB44C
	private void RPC_Attack(long sender)
	{
		this.m_attackCharge = 0f;
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.SetFuel(0f);
		this.m_nview.GetZDO().Set(ZDOVars.s_startTime, 0L);
		this.UpdateAttackCharge();
		if (this.m_attackObject)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_attackObject, base.transform.position, base.transform.rotation);
			if (!this.m_damagePlayers)
			{
				Aoe componentInChildren = gameObject.GetComponentInChildren<Aoe>();
				if (componentInChildren != null)
				{
					componentInChildren.Setup(Player.m_localPlayer, Vector3.zero, 1f, null, null, null);
				}
			}
		}
		this.m_attackEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
	}

	// Token: 0x0600195A RID: 6490 RVA: 0x000BD34A File Offset: 0x000BB54A
	private void RPC_HitNow(long sender)
	{
		this.m_lastHitTime = Time.time;
	}

	// Token: 0x0600195B RID: 6491 RVA: 0x000BD357 File Offset: 0x000BB557
	private void UpdateAttackCharge()
	{
		this.m_attackCharge = this.GetAttackCharge();
	}

	// Token: 0x0600195C RID: 6492 RVA: 0x000BD368 File Offset: 0x000BB568
	private float GetAttackCharge()
	{
		long @long = this.m_nview.GetZDO().GetLong(ZDOVars.s_startTime, 0L);
		if (@long <= 0L)
		{
			return 0f;
		}
		DateTime time = ZNet.instance.GetTime();
		DateTime d = new DateTime(@long);
		return (float)((time - d).TotalSeconds / (double)this.m_attackChargeTime);
	}

	// Token: 0x0600195D RID: 6493 RVA: 0x000BD3C4 File Offset: 0x000BB5C4
	private void OnDestroy()
	{
		if (this.m_isPlacementGhost)
		{
			return;
		}
		ShieldGenerator.m_shieldDomeEffect.RemoveShield(this);
		ShieldGenerator.m_instances.Remove(this);
		ShieldGenerator.m_instanceChangeID++;
		Character.SetupContinuousEffect(base.transform, base.transform.position, false, this.m_shieldLowLoop, ref this.m_lowLoopInstances);
	}

	// Token: 0x0600195E RID: 6494 RVA: 0x000BD420 File Offset: 0x000BB620
	private float GetFuel()
	{
		if (!this.m_nview.IsValid())
		{
			return 0f;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, (float)this.m_defaultFuel);
	}

	// Token: 0x0600195F RID: 6495 RVA: 0x000BD451 File Offset: 0x000BB651
	private void SetFuel(float fuel)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_fuel, Mathf.Max(fuel, 0f));
	}

	// Token: 0x06001960 RID: 6496 RVA: 0x000BD490 File Offset: 0x000BB690
	private bool OnAddFuel(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (this.GetFuel() > (float)(this.m_maxFuel - 1))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null);
			return false;
		}
		if (item != null)
		{
			bool flag = false;
			foreach (ItemDrop itemDrop in this.m_fuelItems)
			{
				if (item.m_shared.m_name == itemDrop.m_itemData.m_shared.m_name)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				user.Message(MessageHud.MessageType.Center, "$msg_wrongitem", 0, null);
				return false;
			}
		}
		else
		{
			bool flag2 = false;
			foreach (ItemDrop itemDrop2 in this.m_fuelItems)
			{
				if (user.GetInventory().HaveItem(itemDrop2.m_itemData.m_shared.m_name, true))
				{
					item = itemDrop2.m_itemData;
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				user.Message(MessageHud.MessageType.Center, "$msg_donthaveany $piece_shieldgenerator_fuelname", 0, null);
				return false;
			}
		}
		user.Message(MessageHud.MessageType.Center, "$msg_added " + item.m_shared.m_name, 0, null);
		user.GetInventory().RemoveItem(item.m_shared.m_name, 1, -1, true);
		this.m_nview.InvokeRPC("RPC_AddFuel", Array.Empty<object>());
		return true;
	}

	// Token: 0x06001961 RID: 6497 RVA: 0x000BD608 File Offset: 0x000BB808
	private void RPC_AddFuel(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		float fuel = this.GetFuel();
		this.SetFuel(fuel + 1f);
		this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
	}

	// Token: 0x06001962 RID: 6498 RVA: 0x000BD668 File Offset: 0x000BB868
	private void Update()
	{
		if (this.m_shieldDome)
		{
			float num = this.m_shieldDome.transform.localScale.x + (this.m_radius - this.m_shieldDome.transform.localScale.x) * this.m_decreaseInertia;
			this.m_shieldDome.transform.localScale = new Vector3(num, num, num);
		}
		if (this.m_radiusTarget != this.m_radius)
		{
			if (!this.m_firstCheck)
			{
				this.m_firstCheck = true;
				this.m_radius = this.m_radiusTarget;
			}
			float num2 = this.m_radiusTarget - this.m_radius;
			this.m_radius += Mathf.Min(this.m_startStopSpeed * Time.deltaTime, Mathf.Abs(num2)) * (float)((num2 > 0f) ? 1 : -1);
		}
		if (this.m_lastFuel != this.m_lastFuelSent || this.m_radius != this.m_radiusSent || this.m_lastHitTime != this.m_lastHitTimeSent)
		{
			ShieldGenerator.m_shieldDomeEffect.SetShieldData(this, this.m_shieldDome.transform.position, this.m_radius, this.m_lastFuel, this.m_lastHitTime);
			this.m_lastFuelSent = this.m_lastFuel;
			this.m_radiusSent = this.m_radius;
			this.m_lastHitTimeSent = this.m_lastHitTime;
		}
	}

	// Token: 0x06001963 RID: 6499 RVA: 0x000BD7B8 File Offset: 0x000BB9B8
	private void UpdateShield()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		float fuel = this.GetFuel();
		GameObject enabledObject = this.m_enabledObject;
		if (enabledObject != null)
		{
			enabledObject.SetActive(fuel > 0f);
		}
		GameObject disabledObject = this.m_disabledObject;
		if (disabledObject != null)
		{
			disabledObject.SetActive(fuel <= 0f);
		}
		float radius = this.m_radius;
		this.m_lastFuel = fuel / (float)this.m_maxFuel;
		float radiusTarget = this.m_radiusTarget;
		this.m_radiusTarget = this.m_minShieldRadius + this.m_lastFuel * (this.m_maxShieldRadius - this.m_minShieldRadius);
		Color domeColor = ShieldDomeImageEffect.GetDomeColor(this.m_lastFuel);
		foreach (ParticleSystem particleSystem in this.m_energyParticles)
		{
			particleSystem.main.startColor = domeColor;
			particleSystem.emission.rateOverTime = this.m_lastFuel * 5f;
		}
		this.m_energyParticlesFlare.customData.SetColor(ParticleSystemCustomData.Custom1, domeColor * Mathf.Pow(this.m_lastFuel, 0.5f));
		Light[] coloredLights = this.m_coloredLights;
		for (int i = 0; i < coloredLights.Length; i++)
		{
			coloredLights[i].color = domeColor;
		}
		this.m_propertyBlock.SetColor(ShieldGenerator.s_emissiveProperty, domeColor * 2f);
		MeshRenderer[] meshRenderers = this.m_meshRenderers;
		for (int i = 0; i < meshRenderers.Length; i++)
		{
			meshRenderers[i].SetPropertyBlock(this.m_propertyBlock);
		}
		if (this.m_offWhenNoFuel)
		{
			if (fuel <= 0f)
			{
				this.m_radiusTarget = 0f;
				if (radiusTarget > 0f && this.m_nview.IsOwner())
				{
					this.m_shieldStop.Create(this.m_shieldDome.transform.position, this.m_shieldDome.transform.rotation, null, 1f, -1);
				}
			}
			if (fuel > 0f && radiusTarget <= 0f && this.m_nview.IsOwner())
			{
				this.m_shieldStart.Create(this.m_shieldDome.transform.position, this.m_shieldDome.transform.rotation, null, 1f, -1);
			}
		}
		if (this.m_shieldLowLoopFuelStart > 0f && this.m_nview.IsOwner())
		{
			Character.SetupContinuousEffect(base.transform, base.transform.position, this.m_lastFuel > 0f && this.m_lastFuel < this.m_shieldLowLoopFuelStart, this.m_shieldLowLoop, ref this.m_lowLoopInstances);
		}
		if (this.m_nview.IsOwner() && fuel >= (float)this.m_maxFuel && this.m_nview.GetZDO().GetLong(ZDOVars.s_startTime, 0L) <= 0L)
		{
			DateTime time = ZNet.instance.GetTime();
			this.m_nview.GetZDO().Set(ZDOVars.s_startTime, time.Ticks);
		}
		this.UpdateAttackCharge();
	}

	// Token: 0x06001964 RID: 6500 RVA: 0x000BDAB8 File Offset: 0x000BBCB8
	public static void CheckProjectile(Projectile projectile)
	{
		if (!projectile.HasBeenOutsideShields)
		{
			int num = ShieldGenerator.m_instances.Count;
			foreach (ShieldGenerator shieldGenerator in ShieldGenerator.m_instances)
			{
				if (Vector3.Distance(shieldGenerator.m_shieldDome.transform.position, projectile.transform.position) < shieldGenerator.m_radius || !ShieldGenerator.CheckShield(shieldGenerator))
				{
					num--;
				}
			}
			if (num == 0)
			{
				projectile.TriggerShieldsLeftFlag();
			}
		}
		else
		{
			foreach (ShieldGenerator shieldGenerator2 in ShieldGenerator.m_instances)
			{
				if (ShieldGenerator.CheckShield(shieldGenerator2) && Vector3.Distance(shieldGenerator2.m_shieldDome.transform.position, projectile.m_startPoint) > shieldGenerator2.m_radius && Vector3.Distance(shieldGenerator2.m_shieldDome.transform.position, projectile.transform.position) < shieldGenerator2.m_radius)
				{
					shieldGenerator2.OnProjectileHit(projectile.gameObject);
				}
			}
		}
		ShieldGenerator.ShieldCleanup();
	}

	// Token: 0x06001965 RID: 6501 RVA: 0x000BDBFC File Offset: 0x000BBDFC
	public float GetFuelRatio()
	{
		return Mathf.Clamp01(this.m_lastFuel);
	}

	// Token: 0x06001966 RID: 6502 RVA: 0x000BDC0C File Offset: 0x000BBE0C
	public static void CheckObjectInsideShield(Cinder zinder)
	{
		foreach (ShieldGenerator shieldGenerator in ShieldGenerator.m_instances)
		{
			if (ShieldGenerator.CheckShield(shieldGenerator) && Vector3.Distance(shieldGenerator.m_shieldDome.transform.position, zinder.transform.position) < shieldGenerator.m_radius)
			{
				shieldGenerator.OnProjectileHit(zinder.gameObject);
			}
		}
		ShieldGenerator.ShieldCleanup();
	}

	// Token: 0x06001967 RID: 6503 RVA: 0x000BDC98 File Offset: 0x000BBE98
	public static bool IsInsideShield(Vector3 point)
	{
		foreach (ShieldGenerator shieldGenerator in ShieldGenerator.m_instances)
		{
			if (ShieldGenerator.CheckShield(shieldGenerator) && shieldGenerator && shieldGenerator.m_shieldDome && Vector3.Distance(shieldGenerator.m_shieldDome.transform.position, point) < shieldGenerator.m_radius)
			{
				return true;
			}
		}
		ShieldGenerator.ShieldCleanup();
		return false;
	}

	// Token: 0x06001968 RID: 6504 RVA: 0x000BDD2C File Offset: 0x000BBF2C
	public static bool IsInsideMaxShield(Vector3 point)
	{
		foreach (ShieldGenerator shieldGenerator in ShieldGenerator.m_instances)
		{
			if (ShieldGenerator.CheckShield(shieldGenerator) && shieldGenerator && shieldGenerator.m_shieldDome && Vector3.Distance(shieldGenerator.m_shieldDome.transform.position, point) < shieldGenerator.m_maxShieldRadius)
			{
				return true;
			}
		}
		ShieldGenerator.ShieldCleanup();
		return false;
	}

	// Token: 0x06001969 RID: 6505 RVA: 0x000BDDC0 File Offset: 0x000BBFC0
	public static bool IsInsideShieldCached(Vector3 point, ref int changeID)
	{
		if (Mathf.Abs(changeID) <= ShieldGenerator.m_instanceChangeID)
		{
			if (!ShieldGenerator.IsInsideMaxShield(point))
			{
				changeID = -ShieldGenerator.m_instanceChangeID;
				return false;
			}
			changeID = ShieldGenerator.m_instanceChangeID;
			if (ShieldGenerator.IsInsideShield(point))
			{
				return true;
			}
		}
		return changeID > 0 && ShieldGenerator.IsInsideShield(point);
	}

	// Token: 0x0600196A RID: 6506 RVA: 0x000BDE0E File Offset: 0x000BC00E
	private static bool CheckShield(ShieldGenerator shield)
	{
		if (!shield || !shield.m_shieldDome)
		{
			ShieldGenerator.s_cleanShields = true;
			return false;
		}
		return true;
	}

	// Token: 0x0600196B RID: 6507 RVA: 0x000BDE30 File Offset: 0x000BC030
	private static void ShieldCleanup()
	{
		if (ShieldGenerator.s_cleanShields)
		{
			int num = ShieldGenerator.m_instances.RemoveAll((ShieldGenerator x) => x == null || x.m_shieldDome == null);
			if (num > 0)
			{
				ZLog.LogWarning(string.Format("Removed {0} invalid shield instances. Some shields may be broken?", num));
			}
			ShieldGenerator.s_cleanShields = false;
		}
	}

	// Token: 0x0600196C RID: 6508 RVA: 0x000BDE90 File Offset: 0x000BC090
	public void OnProjectileHit(GameObject obj)
	{
		Vector3 position = obj.transform.position;
		Projectile component = obj.GetComponent<Projectile>();
		if (component != null)
		{
			component.OnHit(null, position, false, -obj.transform.forward);
		}
		ZNetScene.instance.Destroy(obj.gameObject);
		if (this.m_fuelPerDamage > 0f)
		{
			float num = this.m_fuelPerDamage * (component ? component.m_damage.GetTotalDamage() : 10f);
			this.SetFuel(this.GetFuel() - num);
		}
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_HitNow", Array.Empty<object>());
		this.m_shieldHitEffects.Create(position, Quaternion.LookRotation(base.transform.position.DirTo(position)), null, 1f, -1);
		this.UpdateShield();
	}

	// Token: 0x0600196D RID: 6509 RVA: 0x000BDF64 File Offset: 0x000BC164
	private string OnHoverAddFuel()
	{
		float fuel = this.GetFuel();
		return Localization.instance.Localize(string.Format("{0} ({1}/{2})\n[<color=yellow><b>$KEY_Use</b></color>] {3}", new object[]
		{
			this.m_name,
			Mathf.Ceil(fuel),
			this.m_maxFuel,
			this.m_add
		}));
	}

	// Token: 0x0600196E RID: 6510 RVA: 0x000BDFC0 File Offset: 0x000BC1C0
	public static bool HasShields()
	{
		if (ShieldGenerator.m_instances == null)
		{
			return false;
		}
		using (List<ShieldGenerator>.Enumerator enumerator = ShieldGenerator.m_instances.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_lastFuel > 0f)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x0600196F RID: 6511 RVA: 0x000BE028 File Offset: 0x000BC228
	private static float SDFSmoothMin(float a, float b, float k)
	{
		k *= 6f;
		float num = Mathf.Max(k - Mathf.Abs(a - b), 0f) / k;
		return Mathf.Min(a, b) - num * num * num * k * 0.16666667f;
	}

	// Token: 0x06001970 RID: 6512 RVA: 0x000BE06C File Offset: 0x000BC26C
	public static Vector3 DirectionToShieldWall(Vector3 pos)
	{
		float num = 0.001f;
		return -Vector3.Normalize(new Vector3(ShieldGenerator.DistanceToShieldWall(pos + new Vector3(0f, num, 0f)), ShieldGenerator.DistanceToShieldWall(pos + new Vector3(0f, 0f, num)), ShieldGenerator.DistanceToShieldWall(pos + new Vector3(num, 0f, 0f))));
	}

	// Token: 0x06001971 RID: 6513 RVA: 0x000BE0E0 File Offset: 0x000BC2E0
	public static float DistanceToShieldWall(Vector3 pos)
	{
		float num = float.PositiveInfinity;
		foreach (ShieldGenerator shieldGenerator in ShieldGenerator.m_instances)
		{
			if (shieldGenerator.m_lastFuel != 0f)
			{
				float b = Vector3.Distance(shieldGenerator.transform.position, pos) - shieldGenerator.m_radius;
				num = ShieldGenerator.SDFSmoothMin(num, b, ShieldDomeImageEffect.Smoothing);
			}
		}
		return num;
	}

	// Token: 0x06001972 RID: 6514 RVA: 0x000BE168 File Offset: 0x000BC368
	public static Vector3 GetClosestShieldPoint(Vector3 pos)
	{
		float d = ShieldGenerator.DistanceToShieldWall(pos);
		Vector3 a = ShieldGenerator.DirectionToShieldWall(pos);
		return pos + a * d;
	}

	// Token: 0x06001973 RID: 6515 RVA: 0x000BE190 File Offset: 0x000BC390
	public static ShieldGenerator GetClosestShieldGenerator(Vector3 pos, bool ignoreRadius = false)
	{
		ShieldGenerator result = null;
		float num = float.PositiveInfinity;
		foreach (ShieldGenerator shieldGenerator in ShieldGenerator.m_instances)
		{
			float num2 = ignoreRadius ? 0f : shieldGenerator.m_radius;
			float num3 = Mathf.Abs(Vector3.Distance(shieldGenerator.transform.position, pos) - num2);
			if (num3 < num)
			{
				num = num3;
				result = shieldGenerator;
			}
		}
		return result;
	}

	// Token: 0x06001974 RID: 6516 RVA: 0x000BE21C File Offset: 0x000BC41C
	public bool TryGetItems(Player player, out List<string> items)
	{
		items = new List<string>();
		if (!this.CanUseItems(player, true))
		{
			return true;
		}
		items = (from item in this.m_fuelItems
		select item.m_itemData.m_shared.m_name).ToList<string>();
		return true;
	}

	// Token: 0x06001975 RID: 6517 RVA: 0x000BE270 File Offset: 0x000BC470
	public bool CanUseItems(Player player, bool sendErrorMessage = true)
	{
		if (this.GetFuel() > (float)(this.m_maxFuel - 1))
		{
			player.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null);
			return false;
		}
		if (this.m_fuelItems.Any((ItemDrop obj) => player.GetInventory().HaveItem(obj.m_itemData.m_shared.m_name, true)))
		{
			return true;
		}
		player.Message(MessageHud.MessageType.Center, "$msg_donthaveany $piece_shieldgenerator_fuelname", 0, null);
		return false;
	}

	// Token: 0x040019C3 RID: 6595
	private static bool s_cleanShields = false;

	// Token: 0x040019C4 RID: 6596
	private static List<ShieldGenerator> m_instances = new List<ShieldGenerator>();

	// Token: 0x040019C5 RID: 6597
	private static int m_instanceChangeID = 0;

	// Token: 0x040019C6 RID: 6598
	private static ShieldDomeImageEffect m_shieldDomeEffect;

	// Token: 0x040019C7 RID: 6599
	public string m_name = "$piece_shieldgenerator";

	// Token: 0x040019C8 RID: 6600
	public string m_add = "$piece_shieldgenerator_add";

	// Token: 0x040019C9 RID: 6601
	public Switch m_addFuelSwitch;

	// Token: 0x040019CA RID: 6602
	public GameObject m_enabledObject;

	// Token: 0x040019CB RID: 6603
	public GameObject m_disabledObject;

	// Token: 0x040019CC RID: 6604
	[Header("Fuel")]
	public List<ItemDrop> m_fuelItems = new List<ItemDrop>();

	// Token: 0x040019CD RID: 6605
	public int m_maxFuel = 10;

	// Token: 0x040019CE RID: 6606
	public int m_defaultFuel;

	// Token: 0x040019CF RID: 6607
	public float m_fuelPerDamage = 0.01f;

	// Token: 0x040019D0 RID: 6608
	public EffectList m_fuelAddedEffects = new EffectList();

	// Token: 0x040019D1 RID: 6609
	[Header("Shield")]
	public GameObject m_shieldDome;

	// Token: 0x040019D2 RID: 6610
	public float m_minShieldRadius = 10f;

	// Token: 0x040019D3 RID: 6611
	public float m_maxShieldRadius = 30f;

	// Token: 0x040019D4 RID: 6612
	public float m_decreaseInertia = 0.98f;

	// Token: 0x040019D5 RID: 6613
	public float m_startStopSpeed = 0.5f;

	// Token: 0x040019D6 RID: 6614
	public bool m_offWhenNoFuel = true;

	// Token: 0x040019D7 RID: 6615
	[Header("Attack")]
	public bool m_enableAttack = true;

	// Token: 0x040019D8 RID: 6616
	public float m_attackChargeTime = 900f;

	// Token: 0x040019D9 RID: 6617
	public bool m_damagePlayers = true;

	// Token: 0x040019DA RID: 6618
	public GameObject m_attackObject;

	// Token: 0x040019DB RID: 6619
	public EffectList m_attackEffects = new EffectList();

	// Token: 0x040019DC RID: 6620
	[Header("Effects")]
	public EffectList m_shieldHitEffects = new EffectList();

	// Token: 0x040019DD RID: 6621
	public EffectList m_shieldStart = new EffectList();

	// Token: 0x040019DE RID: 6622
	public EffectList m_shieldStop = new EffectList();

	// Token: 0x040019DF RID: 6623
	public EffectList m_shieldLowLoop = new EffectList();

	// Token: 0x040019E0 RID: 6624
	public float m_shieldLowLoopFuelStart;

	// Token: 0x040019E1 RID: 6625
	public ParticleSystem[] m_energyParticles;

	// Token: 0x040019E2 RID: 6626
	public ParticleSystem m_energyParticlesFlare;

	// Token: 0x040019E3 RID: 6627
	public Light[] m_coloredLights;

	// Token: 0x040019E4 RID: 6628
	private static readonly int s_emissiveProperty = Shader.PropertyToID("_EmissionColor");

	// Token: 0x040019E5 RID: 6629
	private ZNetView m_nview;

	// Token: 0x040019E6 RID: 6630
	private StringBuilder m_sb = new StringBuilder();

	// Token: 0x040019E7 RID: 6631
	private bool m_firstCheck;

	// Token: 0x040019E8 RID: 6632
	private int m_projectileMask;

	// Token: 0x040019E9 RID: 6633
	private float m_radius;

	// Token: 0x040019EA RID: 6634
	private float m_radiusTarget;

	// Token: 0x040019EB RID: 6635
	private float m_radiusSent;

	// Token: 0x040019EC RID: 6636
	private float m_lastFuel;

	// Token: 0x040019ED RID: 6637
	private float m_lastFuelSent;

	// Token: 0x040019EE RID: 6638
	private float m_lastHitTime;

	// Token: 0x040019EF RID: 6639
	private float m_lastHitTimeSent;

	// Token: 0x040019F0 RID: 6640
	private float m_attackCharge;

	// Token: 0x040019F1 RID: 6641
	private bool m_isPlacementGhost;

	// Token: 0x040019F2 RID: 6642
	private GameObject[] m_lowLoopInstances;

	// Token: 0x040019F3 RID: 6643
	private Gradient m_particleFlareGradient;

	// Token: 0x040019F4 RID: 6644
	private MeshRenderer[] m_meshRenderers;

	// Token: 0x040019F5 RID: 6645
	private MaterialPropertyBlock m_propertyBlock;
}
