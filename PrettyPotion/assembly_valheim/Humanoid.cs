using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Token: 0x0200001E RID: 30
public class Humanoid : Character
{
	// Token: 0x0600028B RID: 651 RVA: 0x00016A78 File Offset: 0x00014C78
	protected override void Awake()
	{
		base.Awake();
		this.m_visEquipment = base.GetComponent<VisEquipment>();
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_seed = this.m_nview.GetZDO().GetInt(ZDOVars.s_seed, 0);
		if (this.m_seed == 0)
		{
			this.m_seed = this.m_nview.GetZDO().m_uid.GetHashCode();
			this.m_nview.GetZDO().Set(ZDOVars.s_seed, this.m_seed, true);
		}
	}

	// Token: 0x0600028C RID: 652 RVA: 0x00016B06 File Offset: 0x00014D06
	protected override void Start()
	{
		if (!this.IsPlayer())
		{
			this.GiveDefaultItems();
		}
	}

	// Token: 0x0600028D RID: 653 RVA: 0x00016B16 File Offset: 0x00014D16
	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	// Token: 0x0600028E RID: 654 RVA: 0x00016B20 File Offset: 0x00014D20
	public void GiveDefaultItems()
	{
		foreach (GameObject prefab in this.m_defaultItems)
		{
			this.GiveDefaultItem(prefab);
		}
		if (this.m_randomWeapon.Length != 0 || this.m_randomArmor.Length != 0 || this.m_randomShield.Length != 0 || this.m_randomSets.Length != 0 || this.m_randomItems.Length != 0)
		{
			UnityEngine.Random.State state = UnityEngine.Random.state;
			UnityEngine.Random.InitState(this.m_seed);
			if (this.m_randomShield.Length != 0)
			{
				GameObject gameObject = this.m_randomShield[UnityEngine.Random.Range(0, this.m_randomShield.Length)];
				if (gameObject)
				{
					this.GiveDefaultItem(gameObject);
				}
			}
			if (this.m_randomWeapon.Length != 0)
			{
				GameObject gameObject2 = this.m_randomWeapon[UnityEngine.Random.Range(0, this.m_randomWeapon.Length)];
				if (gameObject2)
				{
					this.GiveDefaultItem(gameObject2);
				}
			}
			if (this.m_randomArmor.Length != 0)
			{
				GameObject gameObject3 = this.m_randomArmor[UnityEngine.Random.Range(0, this.m_randomArmor.Length)];
				if (gameObject3)
				{
					this.GiveDefaultItem(gameObject3);
				}
			}
			if (this.m_randomSets.Length != 0)
			{
				foreach (GameObject prefab2 in this.m_randomSets[UnityEngine.Random.Range(0, this.m_randomSets.Length)].m_items)
				{
					this.GiveDefaultItem(prefab2);
				}
			}
			if (this.m_randomItems.Length != 0)
			{
				int num = (int)Enum.GetValues(typeof(ItemDrop.ItemData.ItemType)).Cast<ItemDrop.ItemData.ItemType>().Max<ItemDrop.ItemData.ItemType>();
				this.m_randomItemSlotFilled = new bool[num];
				foreach (Humanoid.RandomItem randomItem in this.m_randomItems)
				{
					if (randomItem.m_prefab && UnityEngine.Random.value > randomItem.m_chance)
					{
						int itemType = (int)randomItem.m_prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType;
						if (!this.m_randomItemSlotFilled[itemType])
						{
							this.m_randomItemSlotFilled[itemType] = true;
							this.GiveDefaultItem(randomItem.m_prefab);
						}
					}
				}
			}
			UnityEngine.Random.state = state;
		}
	}

	// Token: 0x0600028F RID: 655 RVA: 0x00016D14 File Offset: 0x00014F14
	private void GiveDefaultItem(GameObject prefab)
	{
		ItemDrop.ItemData itemData = this.PickupPrefab(prefab, 0, false);
		if (itemData != null && !itemData.IsWeapon())
		{
			this.EquipItem(itemData, false);
		}
	}

	// Token: 0x06000290 RID: 656 RVA: 0x00016D3F File Offset: 0x00014F3F
	public override void CustomFixedUpdate(float fixedDeltaTime)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.UpdateAttack(fixedDeltaTime);
			this.UpdateEquipment(fixedDeltaTime);
			this.UpdateBlock(fixedDeltaTime);
		}
		this.UpdateUseVisual(fixedDeltaTime);
		base.CustomFixedUpdate(fixedDeltaTime);
	}

	// Token: 0x06000291 RID: 657 RVA: 0x00016D7F File Offset: 0x00014F7F
	public override bool InAttack()
	{
		return base.GetNextAnimHash() == Humanoid.s_animatorTagAttack || base.GetCurrentAnimHash() == Humanoid.s_animatorTagAttack;
	}

	// Token: 0x06000292 RID: 658 RVA: 0x00016DA0 File Offset: 0x00014FA0
	public override bool StartAttack(Character target, bool secondaryAttack)
	{
		if ((this.InAttack() && !this.HaveQueuedChain()) || this.InDodge() || !this.CanMove() || base.IsKnockedBack() || base.IsStaggering() || this.InMinorAction())
		{
			return false;
		}
		ItemDrop.ItemData currentWeapon = this.GetCurrentWeapon();
		if (currentWeapon == null)
		{
			return false;
		}
		if (secondaryAttack && !currentWeapon.HaveSecondaryAttack())
		{
			return false;
		}
		if (!secondaryAttack && !currentWeapon.HavePrimaryAttack())
		{
			return false;
		}
		if (this.m_currentAttack != null)
		{
			this.m_currentAttack.Stop();
			this.m_previousAttack = this.m_currentAttack;
			this.m_currentAttack = null;
		}
		Attack attack = secondaryAttack ? currentWeapon.m_shared.m_secondaryAttack.Clone() : currentWeapon.m_shared.m_attack.Clone();
		if (attack.Start(this, this.m_body, this.m_zanim, this.m_animEvent, this.m_visEquipment, currentWeapon, this.m_previousAttack, this.m_timeSinceLastAttack, this.GetAttackDrawPercentage()))
		{
			this.ClearActionQueue();
			this.StartAttackGroundCheck();
			this.m_currentAttack = attack;
			this.m_currentAttackIsSecondary = secondaryAttack;
			this.m_lastCombatTimer = 0f;
			return true;
		}
		return false;
	}

	// Token: 0x06000293 RID: 659 RVA: 0x00016EBC File Offset: 0x000150BC
	private void StartAttackGroundCheck()
	{
		if (!this.IsPlayer())
		{
			return;
		}
		Collider lastGroundCollider = base.GetLastGroundCollider();
		if (!lastGroundCollider)
		{
			return;
		}
		int layer = lastGroundCollider.gameObject.layer;
		int num = Character.s_groundRayMask | Character.s_characterLayerMask;
		if (num == (num | 1 << layer))
		{
			this.m_lastGroundColliderOnAttackStart = layer;
		}
	}

	// Token: 0x06000294 RID: 660 RVA: 0x00016F0A File Offset: 0x0001510A
	private IEnumerator EndAttackGroundCheck()
	{
		if (!this.IsPlayer())
		{
			yield break;
		}
		yield return new WaitForSeconds(0.03f);
		Collider lastGroundCollider = base.GetLastGroundCollider();
		if (!lastGroundCollider)
		{
			yield break;
		}
		int layer = lastGroundCollider.gameObject.layer;
		bool flag = Character.s_characterLayerMask == (Character.s_characterLayerMask | 1 << layer);
		bool flag2 = Character.s_characterLayerMask == (Character.s_characterLayerMask | 1 << this.m_lastGroundColliderOnAttackStart);
		if (this.m_lastGroundColliderOnAttackStart != layer)
		{
			if (flag && flag2)
			{
				yield break;
			}
			if (Character.s_characterLayerMask == (Character.s_characterLayerMask | 1 << layer))
			{
				base.TimeoutGroundForce(2f);
			}
		}
		yield break;
	}

	// Token: 0x06000295 RID: 661 RVA: 0x00016F19 File Offset: 0x00015119
	public override float GetTimeSinceLastAttack()
	{
		return this.m_timeSinceLastAttack;
	}

	// Token: 0x06000296 RID: 662 RVA: 0x00016F24 File Offset: 0x00015124
	public float GetAttackDrawPercentage()
	{
		ItemDrop.ItemData currentWeapon = this.GetCurrentWeapon();
		if (currentWeapon == null || !currentWeapon.m_shared.m_attack.m_bowDraw || this.m_attackDrawTime <= 0f)
		{
			return 0f;
		}
		float skillFactor = this.GetSkillFactor(currentWeapon.m_shared.m_skillType);
		float num = Mathf.Lerp(currentWeapon.m_shared.m_attack.m_drawDurationMin, currentWeapon.m_shared.m_attack.m_drawDurationMin * 0.2f, skillFactor);
		if (num <= 0f)
		{
			return 1f;
		}
		return Mathf.Clamp01(this.m_attackDrawTime / num);
	}

	// Token: 0x06000297 RID: 663 RVA: 0x00016FBC File Offset: 0x000151BC
	private void UpdateEquipment(float dt)
	{
		if (!this.IsPlayer())
		{
			return;
		}
		if (base.IsSwimming() && !base.IsOnGround())
		{
			this.HideHandItems(false, true);
		}
		if (this.m_rightItem != null && this.m_rightItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_rightItem, dt);
		}
		if (this.m_leftItem != null && this.m_leftItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_leftItem, dt);
		}
		if (this.m_chestItem != null && this.m_chestItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_chestItem, dt);
		}
		if (this.m_legItem != null && this.m_legItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_legItem, dt);
		}
		if (this.m_helmetItem != null && this.m_helmetItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_helmetItem, dt);
		}
		if (this.m_shoulderItem != null && this.m_shoulderItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_shoulderItem, dt);
		}
		if (this.m_utilityItem != null && this.m_utilityItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_utilityItem, dt);
		}
	}

	// Token: 0x06000298 RID: 664 RVA: 0x000170FC File Offset: 0x000152FC
	private void UpdateUseVisual(float dt)
	{
		if (this.m_useItemTime > 0f)
		{
			this.m_useItemTime -= dt;
			if (this.m_useItemTime <= 0f)
			{
				if (this.m_useItemVisual != null)
				{
					this.m_useItemVisual.m_equipEffect.Create(this.m_visEquipment.m_rightHand.position, this.m_visEquipment.m_rightHand.rotation, null, 1f, -1);
				}
				this.m_visEquipment.SetRightItemVisual(null);
				this.m_useItemVisual = null;
				if (this.m_hidHandsOnEat)
				{
					this.ShowHandItems(true, false);
				}
			}
		}
	}

	// Token: 0x06000299 RID: 665 RVA: 0x00017194 File Offset: 0x00015394
	private void DrainEquipedItemDurability(ItemDrop.ItemData item, float dt)
	{
		item.m_durability -= item.m_shared.m_durabilityDrain * dt;
		if (item.m_durability > 0f)
		{
			return;
		}
		this.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_broke", new string[]
		{
			item.m_shared.m_name
		}), 0, item.GetIcon());
		this.UnequipItem(item, false);
		if (item.m_shared.m_destroyBroken)
		{
			this.m_inventory.RemoveItem(item);
		}
	}

	// Token: 0x0600029A RID: 666 RVA: 0x0001721E File Offset: 0x0001541E
	protected override void OnDamaged(HitData hit)
	{
		this.SetCrouch(false);
	}

	// Token: 0x0600029B RID: 667 RVA: 0x00017228 File Offset: 0x00015428
	public ItemDrop.ItemData GetCurrentWeapon()
	{
		if (this.m_rightItem != null && this.m_rightItem.IsWeapon())
		{
			return this.m_rightItem;
		}
		if (this.m_leftItem != null && this.m_leftItem.IsWeapon() && this.m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
		{
			return this.m_leftItem;
		}
		if (this.m_unarmedWeapon)
		{
			return this.m_unarmedWeapon.m_itemData;
		}
		return null;
	}

	// Token: 0x0600029C RID: 668 RVA: 0x0001729B File Offset: 0x0001549B
	private ItemDrop.ItemData GetCurrentBlocker()
	{
		if (this.m_leftItem != null)
		{
			return this.m_leftItem;
		}
		return this.GetCurrentWeapon();
	}

	// Token: 0x0600029D RID: 669 RVA: 0x000172B4 File Offset: 0x000154B4
	private void UpdateAttack(float dt)
	{
		this.m_lastCombatTimer += dt;
		if (this.m_currentAttack != null && this.GetCurrentWeapon() != null)
		{
			this.m_currentAttack.Update(dt);
		}
		if (this.InAttack())
		{
			this.m_timeSinceLastAttack = 0f;
			return;
		}
		this.m_timeSinceLastAttack += dt;
	}

	// Token: 0x0600029E RID: 670 RVA: 0x0001730D File Offset: 0x0001550D
	protected override float GetAttackSpeedFactorMovement()
	{
		if (!this.InAttack() || this.m_currentAttack == null)
		{
			return 1f;
		}
		if (!base.IsFlying() && !base.IsOnGround())
		{
			return 1f;
		}
		return this.m_currentAttack.m_speedFactor;
	}

	// Token: 0x0600029F RID: 671 RVA: 0x00017346 File Offset: 0x00015546
	protected override float GetAttackSpeedFactorRotation()
	{
		if (this.InAttack() && this.m_currentAttack != null)
		{
			return this.m_currentAttack.m_speedFactorRotation;
		}
		return 1f;
	}

	// Token: 0x060002A0 RID: 672 RVA: 0x00017369 File Offset: 0x00015569
	protected virtual bool HaveQueuedChain()
	{
		return false;
	}

	// Token: 0x060002A1 RID: 673 RVA: 0x0001736C File Offset: 0x0001556C
	public override void OnWeaponTrailStart()
	{
		if (this.m_nview.IsValid() && this.m_nview.IsOwner() && this.m_currentAttack != null && this.GetCurrentWeapon() != null)
		{
			this.m_currentAttack.OnTrailStart();
		}
	}

	// Token: 0x060002A2 RID: 674 RVA: 0x000173A4 File Offset: 0x000155A4
	public override void OnAttackTrigger()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_currentAttack != null && this.GetCurrentWeapon() != null)
		{
			base.StartCoroutine(this.EndAttackGroundCheck());
			this.m_currentAttack.OnAttackTrigger();
		}
	}

	// Token: 0x060002A3 RID: 675 RVA: 0x000173F4 File Offset: 0x000155F4
	public override void OnStopMoving()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_currentAttack != null)
		{
			return;
		}
		if (!this.InAttack())
		{
			return;
		}
		if (this.GetCurrentWeapon() != null)
		{
			this.m_currentAttack.m_speedFactor = 0f;
			this.m_currentAttack.m_speedFactorRotation = 0f;
		}
	}

	// Token: 0x060002A4 RID: 676 RVA: 0x00017456 File Offset: 0x00015656
	public virtual Vector3 GetAimDir(Vector3 fromPoint)
	{
		return base.GetLookDir();
	}

	// Token: 0x060002A5 RID: 677 RVA: 0x00017460 File Offset: 0x00015660
	public ItemDrop.ItemData PickupPrefab(GameObject prefab, int stackSize = 0, bool autoequip = true)
	{
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab);
		ZNetView.m_forceDisableInit = false;
		if (stackSize > 0)
		{
			ItemDrop component = gameObject.GetComponent<ItemDrop>();
			component.m_itemData.m_stack = Mathf.Clamp(stackSize, 1, component.m_itemData.m_shared.m_maxStackSize);
		}
		if (this.Pickup(gameObject, autoequip, true))
		{
			return gameObject.GetComponent<ItemDrop>().m_itemData;
		}
		UnityEngine.Object.Destroy(gameObject);
		return null;
	}

	// Token: 0x060002A6 RID: 678 RVA: 0x000174CB File Offset: 0x000156CB
	public virtual bool HaveUniqueKey(string name)
	{
		return false;
	}

	// Token: 0x060002A7 RID: 679 RVA: 0x000174CE File Offset: 0x000156CE
	public virtual void AddUniqueKey(string name)
	{
	}

	// Token: 0x060002A8 RID: 680 RVA: 0x000174D0 File Offset: 0x000156D0
	public virtual bool RemoveUniqueKey(string name)
	{
		return false;
	}

	// Token: 0x060002A9 RID: 681 RVA: 0x000174D4 File Offset: 0x000156D4
	public bool Pickup(GameObject go, bool autoequip = true, bool autoPickupDelay = true)
	{
		if (this.IsTeleporting())
		{
			return false;
		}
		ItemDrop component = go.GetComponent<ItemDrop>();
		if (component == null)
		{
			return false;
		}
		component.Load();
		if (this.IsPlayer() && (component.m_itemData.m_shared.m_icons == null || component.m_itemData.m_shared.m_icons.Length == 0 || component.m_itemData.m_variant >= component.m_itemData.m_shared.m_icons.Length))
		{
			return false;
		}
		if (!component.CanPickup(autoPickupDelay))
		{
			return false;
		}
		if (this.m_inventory.ContainsItem(component.m_itemData))
		{
			return false;
		}
		if (component.m_itemData.m_shared.m_questItem && this.HaveUniqueKey(component.m_itemData.m_shared.m_name))
		{
			this.Message(MessageHud.MessageType.Center, "$msg_cantpickup", 0, null);
			return false;
		}
		int stack = component.m_itemData.m_stack;
		bool flag = this.m_inventory.AddItem(component.m_itemData);
		if (this.m_nview.GetZDO() == null)
		{
			UnityEngine.Object.Destroy(go);
			return true;
		}
		if (!flag)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_noroom", 0, null);
			return false;
		}
		if (component.m_itemData.m_shared.m_questItem)
		{
			this.AddUniqueKey(component.m_itemData.m_shared.m_name);
		}
		ZNetScene.instance.Destroy(go);
		if (autoequip && flag && this.IsPlayer() && component.m_itemData.IsWeapon() && this.m_rightItem == null && this.m_hiddenRightItem == null && (this.m_leftItem == null || !this.m_leftItem.IsTwoHanded()) && (this.m_hiddenLeftItem == null || !this.m_hiddenLeftItem.IsTwoHanded()))
		{
			this.EquipItem(component.m_itemData, true);
		}
		this.m_pickupEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		if (this.IsPlayer())
		{
			base.ShowPickupMessage(component.m_itemData, stack);
			if (Player.m_localPlayer == this as Player && Hud.instance.m_radialMenu.Active)
			{
				Hud.instance.m_radialMenu.OnAddItem(component.m_itemData);
			}
		}
		return flag;
	}

	// Token: 0x060002AA RID: 682 RVA: 0x000176F8 File Offset: 0x000158F8
	public void EquipBestWeapon(Character targetCreature, StaticTarget targetStatic, Character hurtFriend, Character friend)
	{
		List<ItemDrop.ItemData> allItems = this.m_inventory.GetAllItems();
		if (allItems.Count == 0)
		{
			return;
		}
		if (this.InAttack())
		{
			return;
		}
		float num = 0f;
		if (targetCreature)
		{
			float radius = targetCreature.GetRadius();
			num = Vector3.Distance(targetCreature.transform.position, base.transform.position) - radius;
		}
		else if (targetStatic)
		{
			num = Vector3.Distance(targetStatic.transform.position, base.transform.position);
		}
		float time = Time.time;
		base.IsFlying();
		base.IsSwimming();
		Humanoid.optimalWeapons.Clear();
		Humanoid.outofRangeWeapons.Clear();
		Humanoid.allWeapons.Clear();
		foreach (ItemDrop.ItemData itemData in allItems)
		{
			if (itemData.IsWeapon() && this.m_baseAI.CanUseAttack(itemData))
			{
				if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy)
				{
					if (num >= itemData.m_shared.m_aiAttackRangeMin)
					{
						Humanoid.allWeapons.Add(itemData);
						if ((!(targetCreature == null) || !(targetStatic == null)) && time - itemData.m_lastAttackTime >= itemData.m_shared.m_aiAttackInterval)
						{
							if (num > itemData.m_shared.m_aiAttackRange)
							{
								Humanoid.outofRangeWeapons.Add(itemData);
							}
							else
							{
								if (itemData.m_shared.m_aiPrioritized)
								{
									this.EquipItem(itemData, true);
									return;
								}
								Humanoid.optimalWeapons.Add(itemData);
							}
						}
					}
				}
				else if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt)
				{
					if (!(hurtFriend == null) && time - itemData.m_lastAttackTime >= itemData.m_shared.m_aiAttackInterval)
					{
						if (itemData.m_shared.m_aiPrioritized)
						{
							this.EquipItem(itemData, true);
							return;
						}
						Humanoid.optimalWeapons.Add(itemData);
					}
				}
				else if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Friend && !(friend == null) && time - itemData.m_lastAttackTime >= itemData.m_shared.m_aiAttackInterval)
				{
					if (itemData.m_shared.m_aiPrioritized)
					{
						this.EquipItem(itemData, true);
						return;
					}
					Humanoid.optimalWeapons.Add(itemData);
				}
			}
		}
		if (Humanoid.optimalWeapons.Count > 0)
		{
			foreach (ItemDrop.ItemData itemData2 in Humanoid.optimalWeapons)
			{
				if (itemData2.m_shared.m_aiPrioritized)
				{
					this.EquipItem(itemData2, true);
					return;
				}
			}
			this.EquipItem(Humanoid.optimalWeapons[UnityEngine.Random.Range(0, Humanoid.optimalWeapons.Count)], true);
			return;
		}
		if (Humanoid.outofRangeWeapons.Count > 0)
		{
			foreach (ItemDrop.ItemData itemData3 in Humanoid.outofRangeWeapons)
			{
				if (itemData3.m_shared.m_aiPrioritized)
				{
					this.EquipItem(itemData3, true);
					return;
				}
			}
			this.EquipItem(Humanoid.outofRangeWeapons[UnityEngine.Random.Range(0, Humanoid.outofRangeWeapons.Count)], true);
			return;
		}
		if (Humanoid.allWeapons.Count > 0)
		{
			foreach (ItemDrop.ItemData itemData4 in Humanoid.allWeapons)
			{
				if (itemData4.m_shared.m_aiPrioritized)
				{
					this.EquipItem(itemData4, true);
					return;
				}
			}
			this.EquipItem(Humanoid.allWeapons[UnityEngine.Random.Range(0, Humanoid.allWeapons.Count)], true);
			return;
		}
		ItemDrop.ItemData currentWeapon = this.GetCurrentWeapon();
		this.UnequipItem(currentWeapon, false);
	}

	// Token: 0x060002AB RID: 683 RVA: 0x00017B4C File Offset: 0x00015D4C
	public bool DropItem(Inventory inventory, ItemDrop.ItemData item, int amount)
	{
		if (inventory == null)
		{
			inventory = this.m_inventory;
		}
		if (amount == 0)
		{
			return false;
		}
		if (item.m_shared.m_questItem)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_cantdrop", 0, null);
			return false;
		}
		if (amount > item.m_stack)
		{
			amount = item.m_stack;
		}
		this.RemoveEquipAction(item);
		this.UnequipItem(item, false);
		if (this.m_hiddenLeftItem == item)
		{
			this.m_hiddenLeftItem = null;
			this.SetupVisEquipment(this.m_visEquipment, false);
		}
		if (this.m_hiddenRightItem == item)
		{
			this.m_hiddenRightItem = null;
			this.SetupVisEquipment(this.m_visEquipment, false);
		}
		if (amount == item.m_stack)
		{
			ZLog.Log("drop all " + amount.ToString() + "  " + item.m_stack.ToString());
			if (!inventory.RemoveItem(item))
			{
				ZLog.Log("Was not removed");
				return false;
			}
		}
		else
		{
			ZLog.Log("drop some " + amount.ToString() + "  " + item.m_stack.ToString());
			inventory.RemoveItem(item, amount);
		}
		ItemDrop itemDrop = ItemDrop.DropItem(item, amount, base.transform.position + base.transform.forward + base.transform.up, base.transform.rotation);
		if (this.IsPlayer())
		{
			itemDrop.OnPlayerDrop();
		}
		float d = 5f;
		if (item.GetWeight(-1) >= 300f)
		{
			d = 0.5f;
		}
		itemDrop.GetComponent<Rigidbody>().velocity = (base.transform.forward + Vector3.up) * d;
		this.m_zanim.SetTrigger("interact");
		this.m_dropEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		this.Message(MessageHud.MessageType.TopLeft, "$msg_dropped " + itemDrop.m_itemData.m_shared.m_name, itemDrop.m_itemData.m_stack, itemDrop.m_itemData.GetIcon());
		return true;
	}

	// Token: 0x060002AC RID: 684 RVA: 0x00017D4A File Offset: 0x00015F4A
	protected virtual void SetPlaceMode(PieceTable buildPieces)
	{
	}

	// Token: 0x060002AD RID: 685 RVA: 0x00017D4C File Offset: 0x00015F4C
	public Inventory GetInventory()
	{
		return this.m_inventory;
	}

	// Token: 0x060002AE RID: 686 RVA: 0x00017D54 File Offset: 0x00015F54
	public void UseIemBlockkMessage()
	{
		this.m_useItemBlockMessage = 1;
	}

	// Token: 0x060002AF RID: 687 RVA: 0x00017D60 File Offset: 0x00015F60
	public void UseItem(Inventory inventory, ItemDrop.ItemData item, bool fromInventoryGui)
	{
		if (inventory == null)
		{
			inventory = this.m_inventory;
		}
		if (!inventory.ContainsItem(item))
		{
			return;
		}
		GameObject hoverObject = this.GetHoverObject();
		Hoverable hoverable = hoverObject ? hoverObject.GetComponentInParent<Hoverable>() : null;
		if (hoverable != null && !fromInventoryGui)
		{
			Interactable componentInParent = hoverObject.GetComponentInParent<Interactable>();
			if (componentInParent != null && componentInParent.UseItem(this, item))
			{
				this.DoInteractAnimation(hoverObject);
				return;
			}
		}
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)
		{
			if (this.ConsumeItem(inventory, item, true))
			{
				this.m_consumeItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity, null, 1f, -1);
				this.m_zanim.SetTrigger("eat");
				GameObject obj;
				if (ObjectDB.instance.TryGetItemPrefab(item.m_shared, out obj))
				{
					this.SetUseHandVisual(obj, item.m_shared.m_foodEatAnimTime);
				}
			}
			return;
		}
		if (inventory == this.m_inventory && this.ToggleEquipped(item))
		{
			return;
		}
		if (!fromInventoryGui && this.m_useItemBlockMessage == 0)
		{
			if (hoverable != null)
			{
				this.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_cantuseon", new string[]
				{
					item.m_shared.m_name,
					hoverable.GetHoverName()
				}), 0, null);
			}
			else
			{
				this.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_useonwhat", new string[]
				{
					item.m_shared.m_name
				}), 0, null);
			}
		}
		this.m_useItemBlockMessage = 0;
	}

	// Token: 0x060002B0 RID: 688 RVA: 0x00017EC0 File Offset: 0x000160C0
	public void TryUseItemOnInteractable(ItemDrop.ItemData item, GameObject hoverObject, bool fromInventoryGui)
	{
		Hoverable hoverable = hoverObject ? hoverObject.GetComponentInParent<Hoverable>() : null;
		if (hoverable == null || fromInventoryGui)
		{
			if (!fromInventoryGui)
			{
				this.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_useonwhat", new string[]
				{
					item.m_shared.m_name
				}), 0, null);
			}
			return;
		}
		Interactable componentInParent = hoverObject.GetComponentInParent<Interactable>();
		if (componentInParent != null && componentInParent.UseItem(this, item))
		{
			return;
		}
		if (this.m_useItemBlockMessage == 0)
		{
			this.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_cantuseon", new string[]
			{
				item.m_shared.m_name,
				hoverable.GetHoverName()
			}), 0, null);
		}
		this.m_useItemBlockMessage = 0;
	}

	// Token: 0x060002B1 RID: 689 RVA: 0x00017F70 File Offset: 0x00016170
	protected void DoInteractAnimation(GameObject obj)
	{
		Vector3 forward = obj.transform.position - base.transform.position;
		forward.y = 0f;
		forward.Normalize();
		base.transform.rotation = Quaternion.LookRotation(forward);
		Physics.SyncTransforms();
		ItemDrop component = obj.GetComponent<ItemDrop>();
		if (component != null && component.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable && component.IsPiece())
		{
			this.SetUseHandVisual(obj, component.m_itemData.m_shared.m_foodEatAnimTime);
			this.m_zanim.SetTrigger("eat");
			return;
		}
		this.m_zanim.SetTrigger("interact");
	}

	// Token: 0x060002B2 RID: 690 RVA: 0x00018020 File Offset: 0x00016220
	protected void SetUseHandVisual(GameObject obj, float seconds = 1f)
	{
		if (!this.m_nview || !this.m_nview.IsValid() || !this.m_visEquipment)
		{
			return;
		}
		this.m_hidHandsOnEat = this.HideHandItems(true, false);
		ItemDrop component = obj.GetComponent<ItemDrop>();
		this.m_useItemVisual = ((component != null) ? component.m_itemData.m_shared : null);
		this.m_useItemTime = seconds;
		this.m_visEquipment.SetRightItemVisual(Utils.GetPrefabName(obj.name));
	}

	// Token: 0x060002B3 RID: 691 RVA: 0x0001809E File Offset: 0x0001629E
	protected virtual void ClearActionQueue()
	{
	}

	// Token: 0x060002B4 RID: 692 RVA: 0x000180A0 File Offset: 0x000162A0
	public virtual void RemoveEquipAction(ItemDrop.ItemData item)
	{
	}

	// Token: 0x060002B5 RID: 693 RVA: 0x000180A2 File Offset: 0x000162A2
	public virtual void ResetLoadedWeapon()
	{
	}

	// Token: 0x060002B6 RID: 694 RVA: 0x000180A4 File Offset: 0x000162A4
	public virtual bool IsWeaponLoaded()
	{
		return false;
	}

	// Token: 0x060002B7 RID: 695 RVA: 0x000180A7 File Offset: 0x000162A7
	protected virtual bool ToggleEquipped(ItemDrop.ItemData item)
	{
		if (!item.IsEquipable())
		{
			return false;
		}
		if (this.InAttack())
		{
			return true;
		}
		if (this.IsItemEquiped(item))
		{
			this.UnequipItem(item, true);
		}
		else
		{
			this.EquipItem(item, true);
		}
		return true;
	}

	// Token: 0x060002B8 RID: 696 RVA: 0x000180DA File Offset: 0x000162DA
	public virtual bool CanConsumeItem(ItemDrop.ItemData item, bool checkWorldLevel = false)
	{
		if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
		{
			return false;
		}
		if (checkWorldLevel && Game.m_worldLevel > 0 && item.m_worldLevel < Game.m_worldLevel)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_ng_item_too_low", 0, null);
			return false;
		}
		return true;
	}

	// Token: 0x060002B9 RID: 697 RVA: 0x00018115 File Offset: 0x00016315
	public virtual bool ConsumeItem(Inventory inventory, ItemDrop.ItemData item, bool checkWorldLevel = false)
	{
		this.CanConsumeItem(item, checkWorldLevel);
		return false;
	}

	// Token: 0x060002BA RID: 698 RVA: 0x00018124 File Offset: 0x00016324
	public bool EquipItem(ItemDrop.ItemData item, bool triggerEquipEffects = true)
	{
		if (this.IsItemEquiped(item))
		{
			return false;
		}
		if (!this.m_inventory.ContainsItem(item))
		{
			return false;
		}
		if (this.InAttack() || this.InDodge())
		{
			return false;
		}
		if (this.IsPlayer() && !this.IsDead() && base.IsSwimming() && !base.IsOnGround())
		{
			return false;
		}
		if (item.m_shared.m_useDurability && item.m_durability <= 0f)
		{
			return false;
		}
		if (item.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(item.m_shared.m_dlc))
		{
			this.Message(MessageHud.MessageType.Center, "$msg_dlcrequired", 0, null);
			return false;
		}
		if (Game.m_worldLevel > 0 && item.m_worldLevel < Game.m_worldLevel && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_ng_item_too_low", 0, null);
			return false;
		}
		if (Application.isEditor)
		{
			item.m_shared = item.m_dropPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		}
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool)
		{
			this.UnequipItem(this.m_rightItem, triggerEquipEffects);
			this.UnequipItem(this.m_leftItem, triggerEquipEffects);
			this.m_rightItem = item;
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
			if (this.m_visEquipment && this.m_visEquipment.m_isPlayer)
			{
				item.m_shared.m_equipEffect.Create(this.m_visEquipment.m_rightHand.position, this.m_visEquipment.m_rightHand.rotation, null, 1f, -1);
			}
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch)
		{
			if (this.m_rightItem != null && this.m_leftItem == null && this.m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
			{
				this.m_leftItem = item;
				if (this.m_visEquipment && this.m_visEquipment.m_isPlayer)
				{
					item.m_shared.m_equipEffect.Create(this.m_visEquipment.m_leftHand.position, this.m_visEquipment.m_leftHand.rotation, null, 1f, -1);
				}
			}
			else
			{
				this.UnequipItem(this.m_rightItem, triggerEquipEffects);
				if (this.m_leftItem != null && this.m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield)
				{
					this.UnequipItem(this.m_leftItem, triggerEquipEffects);
				}
				this.m_rightItem = item;
				if (this.m_visEquipment && this.m_visEquipment.m_isPlayer)
				{
					item.m_shared.m_equipEffect.Create(this.m_visEquipment.m_rightHand.position, this.m_visEquipment.m_rightHand.rotation, null, 1f, -1);
				}
			}
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
		{
			if (this.m_rightItem != null && this.m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch && this.m_leftItem == null)
			{
				ItemDrop.ItemData rightItem = this.m_rightItem;
				this.UnequipItem(this.m_rightItem, triggerEquipEffects);
				this.m_leftItem = rightItem;
				this.m_leftItem.m_equipped = true;
			}
			this.UnequipItem(this.m_rightItem, triggerEquipEffects);
			if (this.m_leftItem != null && this.m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield && this.m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
			{
				this.UnequipItem(this.m_leftItem, triggerEquipEffects);
			}
			this.m_rightItem = item;
			if (this.m_visEquipment && this.m_visEquipment.m_isPlayer)
			{
				item.m_shared.m_equipEffect.Create(this.m_visEquipment.m_rightHand.position, this.m_visEquipment.m_rightHand.rotation, null, 1f, -1);
			}
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
		{
			this.UnequipItem(this.m_leftItem, triggerEquipEffects);
			if (this.m_rightItem != null && this.m_rightItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.OneHandedWeapon && this.m_rightItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
			{
				this.UnequipItem(this.m_rightItem, triggerEquipEffects);
			}
			this.m_leftItem = item;
			if (this.m_visEquipment && this.m_visEquipment.m_isPlayer)
			{
				item.m_shared.m_equipEffect.Create(this.m_visEquipment.m_leftHand.position, this.m_visEquipment.m_leftHand.rotation, null, 1f, -1);
			}
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
		{
			this.UnequipItem(this.m_leftItem, triggerEquipEffects);
			this.UnequipItem(this.m_rightItem, triggerEquipEffects);
			this.m_leftItem = item;
			if (this.m_visEquipment && this.m_visEquipment.m_isPlayer)
			{
				item.m_shared.m_equipEffect.Create(this.m_visEquipment.m_leftHand.position, this.m_visEquipment.m_leftHand.rotation, null, 1f, -1);
			}
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon)
		{
			this.UnequipItem(this.m_leftItem, triggerEquipEffects);
			this.UnequipItem(this.m_rightItem, triggerEquipEffects);
			this.m_rightItem = item;
			if (this.m_visEquipment && this.m_visEquipment.m_isPlayer)
			{
				item.m_shared.m_equipEffect.Create(this.m_visEquipment.m_rightHand.position, this.m_visEquipment.m_rightHand.rotation, null, 1f, -1);
			}
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft)
		{
			this.UnequipItem(this.m_leftItem, triggerEquipEffects);
			this.UnequipItem(this.m_rightItem, triggerEquipEffects);
			this.m_leftItem = item;
			item.m_shared.m_equipEffect.Create(this.m_visEquipment.m_leftHand.position, this.m_visEquipment.m_leftHand.rotation, null, 1f, -1);
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest)
		{
			this.UnequipItem(this.m_chestItem, triggerEquipEffects);
			this.m_chestItem = item;
			if (this.m_visEquipment && this.m_visEquipment.m_isPlayer)
			{
				item.m_shared.m_equipEffect.Create(base.transform.position + Vector3.up, base.transform.rotation, null, 1f, -1);
			}
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs)
		{
			this.UnequipItem(this.m_legItem, triggerEquipEffects);
			this.m_legItem = item;
			if (this.m_visEquipment && this.m_visEquipment.m_isPlayer)
			{
				item.m_shared.m_equipEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			}
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable)
		{
			this.UnequipItem(this.m_ammoItem, triggerEquipEffects);
			this.m_ammoItem = item;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet)
		{
			this.UnequipItem(this.m_helmetItem, triggerEquipEffects);
			this.m_helmetItem = item;
			if (this.m_visEquipment && this.m_visEquipment.m_isPlayer)
			{
				item.m_shared.m_equipEffect.Create(this.m_visEquipment.m_helmet.position, this.m_visEquipment.m_helmet.rotation, null, 1f, -1);
			}
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder)
		{
			this.UnequipItem(this.m_shoulderItem, triggerEquipEffects);
			this.m_shoulderItem = item;
			if (this.m_visEquipment && this.m_visEquipment.m_isPlayer)
			{
				item.m_shared.m_equipEffect.Create(base.transform.position + Vector3.up, base.transform.rotation, null, 1f, -1);
			}
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
		{
			this.UnequipItem(this.m_utilityItem, triggerEquipEffects);
			this.m_utilityItem = item;
			if (this.m_visEquipment && this.m_visEquipment.m_isPlayer)
			{
				item.m_shared.m_equipEffect.Create(base.transform.position + Vector3.up, base.transform.rotation, null, 1f, -1);
			}
		}
		if (this.IsItemEquiped(item))
		{
			item.m_equipped = true;
		}
		this.SetupEquipment();
		if (triggerEquipEffects)
		{
			this.TriggerEquipEffect(item);
		}
		return true;
	}

	// Token: 0x060002BB RID: 699 RVA: 0x00018A44 File Offset: 0x00016C44
	public void UnequipItem(ItemDrop.ItemData item, bool triggerEquipEffects = true)
	{
		if (item == null)
		{
			return;
		}
		if (this.m_hiddenLeftItem == item)
		{
			this.m_hiddenLeftItem = null;
			this.SetupVisEquipment(this.m_visEquipment, false);
		}
		if (this.m_hiddenRightItem == item)
		{
			this.m_hiddenRightItem = null;
			this.SetupVisEquipment(this.m_visEquipment, false);
		}
		if (this.IsItemEquiped(item))
		{
			if (item.IsWeapon())
			{
				if (this.m_currentAttack != null && this.m_currentAttack.GetWeapon() == item)
				{
					this.m_currentAttack.Stop();
					this.m_previousAttack = this.m_currentAttack;
					this.m_currentAttack = null;
				}
				if (!string.IsNullOrEmpty(item.m_shared.m_attack.m_drawAnimationState))
				{
					this.m_zanim.SetBool(item.m_shared.m_attack.m_drawAnimationState, false);
				}
				this.m_attackDrawTime = 0f;
				this.ResetLoadedWeapon();
			}
			if (this.m_rightItem == item)
			{
				this.m_rightItem = null;
			}
			else if (this.m_leftItem == item)
			{
				this.m_leftItem = null;
			}
			else if (this.m_chestItem == item)
			{
				this.m_chestItem = null;
			}
			else if (this.m_legItem == item)
			{
				this.m_legItem = null;
			}
			else if (this.m_ammoItem == item)
			{
				this.m_ammoItem = null;
			}
			else if (this.m_helmetItem == item)
			{
				this.m_helmetItem = null;
			}
			else if (this.m_shoulderItem == item)
			{
				this.m_shoulderItem = null;
			}
			else if (this.m_utilityItem == item)
			{
				this.m_utilityItem = null;
			}
			item.m_equipped = false;
			this.SetupEquipment();
			item.m_shared.m_unequipEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
			if (triggerEquipEffects)
			{
				this.TriggerEquipEffect(item);
			}
		}
	}

	// Token: 0x060002BC RID: 700 RVA: 0x00018BE8 File Offset: 0x00016DE8
	private void TriggerEquipEffect(ItemDrop.ItemData item)
	{
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (MonoUpdaters.UpdateCount == this.m_lastEquipEffectFrame)
		{
			return;
		}
		this.m_lastEquipEffectFrame = MonoUpdaters.UpdateCount;
		this.m_equipEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x060002BD RID: 701 RVA: 0x00018C3F File Offset: 0x00016E3F
	public override bool IsAttached()
	{
		return (this.m_currentAttack != null && this.InAttack() && this.m_currentAttack.IsAttached() && !this.m_currentAttack.IsDone()) || base.IsAttached();
	}

	// Token: 0x060002BE RID: 702 RVA: 0x00018C74 File Offset: 0x00016E74
	public override bool GetRelativePosition(out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Quaternion relativeRot, out Vector3 relativeVel)
	{
		if (this.m_currentAttack != null && this.InAttack() && this.m_currentAttack.IsAttached() && !this.m_currentAttack.IsDone())
		{
			return this.m_currentAttack.GetAttachData(out parent, out attachJoint, out relativePos, out relativeRot, out relativeVel);
		}
		return base.GetRelativePosition(out parent, out attachJoint, out relativePos, out relativeRot, out relativeVel);
	}

	// Token: 0x060002BF RID: 703 RVA: 0x00018CCC File Offset: 0x00016ECC
	public void UnequipAllItems()
	{
		this.UnequipItem(this.m_rightItem, false);
		this.UnequipItem(this.m_leftItem, false);
		this.UnequipItem(this.m_chestItem, false);
		this.UnequipItem(this.m_legItem, false);
		this.UnequipItem(this.m_helmetItem, false);
		this.UnequipItem(this.m_ammoItem, false);
		this.UnequipItem(this.m_shoulderItem, false);
		this.UnequipItem(this.m_utilityItem, false);
	}

	// Token: 0x060002C0 RID: 704 RVA: 0x00018D44 File Offset: 0x00016F44
	protected override void OnRagdollCreated(Ragdoll ragdoll)
	{
		VisEquipment component = ragdoll.GetComponent<VisEquipment>();
		if (component)
		{
			this.SetupVisEquipment(component, true);
		}
	}

	// Token: 0x060002C1 RID: 705 RVA: 0x00018D68 File Offset: 0x00016F68
	protected virtual void SetupVisEquipment(VisEquipment visEq, bool isRagdoll)
	{
		if (!isRagdoll)
		{
			visEq.SetLeftItem((this.m_leftItem != null) ? this.m_leftItem.m_dropPrefab.name : "", (this.m_leftItem != null) ? this.m_leftItem.m_variant : 0);
			visEq.SetRightItem((this.m_rightItem != null) ? this.m_rightItem.m_dropPrefab.name : "");
			if (this.IsPlayer())
			{
				visEq.SetLeftBackItem((this.m_hiddenLeftItem != null) ? this.m_hiddenLeftItem.m_dropPrefab.name : "", (this.m_hiddenLeftItem != null) ? this.m_hiddenLeftItem.m_variant : 0);
				visEq.SetRightBackItem((this.m_hiddenRightItem != null) ? this.m_hiddenRightItem.m_dropPrefab.name : "");
			}
		}
		visEq.SetChestItem((this.m_chestItem != null) ? this.m_chestItem.m_dropPrefab.name : "");
		visEq.SetLegItem((this.m_legItem != null) ? this.m_legItem.m_dropPrefab.name : "");
		visEq.SetHelmetItem((this.m_helmetItem != null) ? this.m_helmetItem.m_dropPrefab.name : "");
		visEq.SetShoulderItem((this.m_shoulderItem != null) ? this.m_shoulderItem.m_dropPrefab.name : "", (this.m_shoulderItem != null) ? this.m_shoulderItem.m_variant : 0);
		visEq.SetUtilityItem((this.m_utilityItem != null) ? this.m_utilityItem.m_dropPrefab.name : "");
		if (this.IsPlayer())
		{
			visEq.SetBeardItem(this.m_beardItem);
			visEq.SetHairItem(this.m_hairItem);
		}
	}

	// Token: 0x060002C2 RID: 706 RVA: 0x00018F34 File Offset: 0x00017134
	private void SetupEquipment()
	{
		if (this.m_visEquipment && (this.m_nview.GetZDO() == null || this.m_nview.IsOwner()))
		{
			this.SetupVisEquipment(this.m_visEquipment, false);
		}
		if (this.m_nview.GetZDO() != null)
		{
			this.UpdateEquipmentStatusEffects();
			if (this.m_rightItem != null && this.m_rightItem.m_shared.m_buildPieces)
			{
				this.SetPlaceMode(this.m_rightItem.m_shared.m_buildPieces);
			}
			else
			{
				this.SetPlaceMode(null);
			}
			this.SetupAnimationState();
		}
	}

	// Token: 0x060002C3 RID: 707 RVA: 0x00018FCC File Offset: 0x000171CC
	private void SetupAnimationState()
	{
		if (this.m_leftItem != null)
		{
			if (this.m_leftItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch)
			{
				this.SetAnimationState(ItemDrop.ItemData.AnimationState.LeftTorch);
				return;
			}
			this.SetAnimationState(this.m_leftItem.m_shared.m_animationState);
			return;
		}
		else
		{
			if (this.m_rightItem != null)
			{
				this.SetAnimationState(this.m_rightItem.m_shared.m_animationState);
				return;
			}
			if (this.m_unarmedWeapon != null)
			{
				this.SetAnimationState(this.m_unarmedWeapon.m_itemData.m_shared.m_animationState);
			}
			return;
		}
	}

	// Token: 0x060002C4 RID: 708 RVA: 0x0001905C File Offset: 0x0001725C
	private void SetAnimationState(ItemDrop.ItemData.AnimationState state)
	{
		this.m_zanim.SetFloat(Humanoid.s_statef, (float)state);
		this.m_zanim.SetInt(Humanoid.s_statei, (int)state);
	}

	// Token: 0x060002C5 RID: 709 RVA: 0x00019081 File Offset: 0x00017281
	public override bool IsSitting()
	{
		return base.GetCurrentAnimHash() == Character.s_animatorTagSitting;
	}

	// Token: 0x060002C6 RID: 710 RVA: 0x00019090 File Offset: 0x00017290
	private void UpdateEquipmentStatusEffects()
	{
		HashSet<StatusEffect> hashSet = new HashSet<StatusEffect>();
		if (this.m_leftItem != null && this.m_leftItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_leftItem.m_shared.m_equipStatusEffect);
		}
		if (this.m_rightItem != null && this.m_rightItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_rightItem.m_shared.m_equipStatusEffect);
		}
		if (this.m_chestItem != null && this.m_chestItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_chestItem.m_shared.m_equipStatusEffect);
		}
		if (this.m_legItem != null && this.m_legItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_legItem.m_shared.m_equipStatusEffect);
		}
		if (this.m_helmetItem != null && this.m_helmetItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_helmetItem.m_shared.m_equipStatusEffect);
		}
		if (this.m_shoulderItem != null && this.m_shoulderItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_shoulderItem.m_shared.m_equipStatusEffect);
		}
		if (this.m_utilityItem != null && this.m_utilityItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_utilityItem.m_shared.m_equipStatusEffect);
		}
		if (this.HaveSetEffect(this.m_leftItem))
		{
			hashSet.Add(this.m_leftItem.m_shared.m_setStatusEffect);
		}
		if (this.HaveSetEffect(this.m_rightItem))
		{
			hashSet.Add(this.m_rightItem.m_shared.m_setStatusEffect);
		}
		if (this.HaveSetEffect(this.m_chestItem))
		{
			hashSet.Add(this.m_chestItem.m_shared.m_setStatusEffect);
		}
		if (this.HaveSetEffect(this.m_legItem))
		{
			hashSet.Add(this.m_legItem.m_shared.m_setStatusEffect);
		}
		if (this.HaveSetEffect(this.m_helmetItem))
		{
			hashSet.Add(this.m_helmetItem.m_shared.m_setStatusEffect);
		}
		if (this.HaveSetEffect(this.m_shoulderItem))
		{
			hashSet.Add(this.m_shoulderItem.m_shared.m_setStatusEffect);
		}
		if (this.HaveSetEffect(this.m_utilityItem))
		{
			hashSet.Add(this.m_utilityItem.m_shared.m_setStatusEffect);
		}
		foreach (StatusEffect statusEffect in this.m_equipmentStatusEffects)
		{
			if (!hashSet.Contains(statusEffect))
			{
				this.m_seman.RemoveStatusEffect(statusEffect.NameHash(), false);
			}
		}
		foreach (StatusEffect statusEffect2 in hashSet)
		{
			if (!this.m_equipmentStatusEffects.Contains(statusEffect2))
			{
				this.m_seman.AddStatusEffect(statusEffect2, false, 0, 0f);
			}
		}
		this.m_equipmentStatusEffects.Clear();
		this.m_equipmentStatusEffects.UnionWith(hashSet);
	}

	// Token: 0x060002C7 RID: 711 RVA: 0x000193EC File Offset: 0x000175EC
	private bool HaveSetEffect(ItemDrop.ItemData item)
	{
		return item != null && !(item.m_shared.m_setStatusEffect == null) && item.m_shared.m_setName.Length != 0 && item.m_shared.m_setSize > 1 && this.GetSetCount(item.m_shared.m_setName) >= item.m_shared.m_setSize;
	}

	// Token: 0x060002C8 RID: 712 RVA: 0x00019454 File Offset: 0x00017654
	private int GetSetCount(string setName)
	{
		int num = 0;
		if (this.m_leftItem != null && this.m_leftItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (this.m_rightItem != null && this.m_rightItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (this.m_chestItem != null && this.m_chestItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (this.m_legItem != null && this.m_legItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (this.m_helmetItem != null && this.m_helmetItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (this.m_shoulderItem != null && this.m_shoulderItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (this.m_utilityItem != null && this.m_utilityItem.m_shared.m_setName == setName)
		{
			num++;
		}
		return num;
	}

	// Token: 0x060002C9 RID: 713 RVA: 0x00019560 File Offset: 0x00017760
	public void SetBeard(string name)
	{
		this.m_beardItem = name;
		this.SetupEquipment();
	}

	// Token: 0x060002CA RID: 714 RVA: 0x0001956F File Offset: 0x0001776F
	public string GetBeard()
	{
		return this.m_beardItem;
	}

	// Token: 0x060002CB RID: 715 RVA: 0x00019577 File Offset: 0x00017777
	public void SetHair(string hair)
	{
		this.m_hairItem = hair;
		this.SetupEquipment();
	}

	// Token: 0x060002CC RID: 716 RVA: 0x00019586 File Offset: 0x00017786
	public string GetHair()
	{
		return this.m_hairItem;
	}

	// Token: 0x060002CD RID: 717 RVA: 0x00019590 File Offset: 0x00017790
	public bool IsItemEquiped(ItemDrop.ItemData item)
	{
		return this.m_rightItem == item || this.m_leftItem == item || this.m_chestItem == item || this.m_legItem == item || this.m_ammoItem == item || this.m_helmetItem == item || this.m_shoulderItem == item || this.m_utilityItem == item;
	}

	// Token: 0x060002CE RID: 718 RVA: 0x000195F6 File Offset: 0x000177F6
	protected ItemDrop.ItemData GetRightItem()
	{
		return this.m_rightItem;
	}

	// Token: 0x060002CF RID: 719 RVA: 0x000195FE File Offset: 0x000177FE
	protected ItemDrop.ItemData GetLeftItem()
	{
		return this.m_leftItem;
	}

	// Token: 0x060002D0 RID: 720 RVA: 0x00019606 File Offset: 0x00017806
	protected override bool CheckRun(Vector3 moveDir, float dt)
	{
		return !this.IsDrawingBow() && base.CheckRun(moveDir, dt) && !this.IsBlocking();
	}

	// Token: 0x060002D1 RID: 721 RVA: 0x00019628 File Offset: 0x00017828
	public override bool IsDrawingBow()
	{
		if (this.m_attackDrawTime <= 0f)
		{
			return false;
		}
		ItemDrop.ItemData currentWeapon = this.GetCurrentWeapon();
		return currentWeapon != null && currentWeapon.m_shared.m_attack.m_bowDraw;
	}

	// Token: 0x060002D2 RID: 722 RVA: 0x00019660 File Offset: 0x00017860
	protected override bool BlockAttack(HitData hit, Character attacker)
	{
		if (Vector3.Dot(hit.m_dir, base.transform.forward) > 0f)
		{
			return false;
		}
		ItemDrop.ItemData currentBlocker = this.GetCurrentBlocker();
		if (currentBlocker == null)
		{
			return false;
		}
		bool flag = currentBlocker.m_shared.m_timedBlockBonus > 1f && this.m_blockTimer != -1f && this.m_blockTimer < 0.25f;
		float skillFactor = this.GetSkillFactor(Skills.SkillType.Blocking);
		float num = currentBlocker.GetBlockPower(skillFactor);
		if (flag)
		{
			num *= currentBlocker.m_shared.m_timedBlockBonus;
		}
		if (currentBlocker.m_shared.m_damageModifiers.Count > 0)
		{
			HitData.DamageModifiers modifiers = default(HitData.DamageModifiers);
			modifiers.Apply(currentBlocker.m_shared.m_damageModifiers);
			HitData.DamageModifier damageModifier;
			hit.ApplyResistance(modifiers, out damageModifier);
		}
		HitData.DamageTypes damageTypes = hit.m_damage.Clone();
		damageTypes.ApplyArmor(num);
		float totalBlockableDamage = hit.GetTotalBlockableDamage();
		float totalBlockableDamage2 = damageTypes.GetTotalBlockableDamage();
		float num2 = totalBlockableDamage - totalBlockableDamage2;
		float num3 = Mathf.Clamp01(num2 / num);
		float num4 = flag ? this.m_blockStaminaDrain : (this.m_blockStaminaDrain * num3);
		num4 += num4 * this.GetEquipmentBlockStaminaModifier();
		this.m_seman.ModifyBlockStaminaUsage(num4, ref num4, true);
		this.UseStamina(num4);
		float totalStaggerDamage = damageTypes.GetTotalStaggerDamage();
		bool flag2 = base.AddStaggerDamage(totalStaggerDamage, hit.m_dir);
		bool flag3 = this.HaveStamina(0f);
		bool flag4 = flag3 && !flag2;
		if (flag3 && !flag2)
		{
			hit.m_statusEffectHash = 0;
			hit.BlockDamage(num2);
			DamageText.instance.ShowText(DamageText.TextType.Blocked, hit.m_point + Vector3.up * 0.5f, num2, false);
		}
		if (currentBlocker.m_shared.m_useDurability)
		{
			float num5 = currentBlocker.m_shared.m_useDurabilityDrain * (totalBlockableDamage / num);
			currentBlocker.m_durability -= num5;
		}
		this.RaiseSkill(Skills.SkillType.Blocking, flag ? 2f : 1f);
		currentBlocker.m_shared.m_blockEffect.Create(hit.m_point, Quaternion.identity, null, 1f, -1);
		if (attacker && flag && flag4)
		{
			this.m_perfectBlockEffect.Create(hit.m_point, Quaternion.identity, null, 1f, -1);
			if (attacker.m_staggerWhenBlocked)
			{
				attacker.Stagger(-hit.m_dir);
			}
			num4 = this.m_blockStaminaDrain;
			num4 -= num4 * this.GetEquipmentBlockStaminaModifier();
			this.m_seman.ModifyBlockStaminaUsage(num4, ref num4, true);
			this.UseStamina(num4);
		}
		if (flag4)
		{
			hit.m_pushForce *= num3;
			if (attacker && !hit.m_ranged)
			{
				float num6 = 1f - Mathf.Clamp01(num3 * 0.5f);
				HitData hitData = new HitData();
				hitData.m_pushForce = currentBlocker.GetDeflectionForce() * num6;
				hitData.m_dir = attacker.transform.position - base.transform.position;
				hitData.m_dir.y = 0f;
				hitData.m_dir.Normalize();
				attacker.Damage(hitData);
			}
		}
		return true;
	}

	// Token: 0x060002D3 RID: 723 RVA: 0x00019978 File Offset: 0x00017B78
	public override bool IsBlocking()
	{
		if (this.m_nview.IsValid() && !this.m_nview.IsOwner())
		{
			return this.m_nview.GetZDO().GetBool(ZDOVars.s_isBlockingHash, false);
		}
		return this.m_blocking && !this.InAttack() && !this.InDodge() && !this.InPlaceMode() && !this.IsEncumbered() && !this.InMinorAction() && !base.IsStaggering();
	}

	// Token: 0x060002D4 RID: 724 RVA: 0x000199F4 File Offset: 0x00017BF4
	private void UpdateBlock(float dt)
	{
		if (!this.IsBlocking())
		{
			if (this.m_internalBlockingState)
			{
				this.m_internalBlockingState = false;
				this.m_nview.GetZDO().Set(ZDOVars.s_isBlockingHash, false);
				this.m_zanim.SetBool(Humanoid.s_blocking, false);
			}
			this.m_blockTimer = -1f;
			return;
		}
		if (!this.m_internalBlockingState)
		{
			this.m_internalBlockingState = true;
			this.m_nview.GetZDO().Set(ZDOVars.s_isBlockingHash, true);
			this.m_zanim.SetBool(Humanoid.s_blocking, true);
		}
		if (this.m_blockTimer < 0f)
		{
			this.m_blockTimer = 0f;
			return;
		}
		this.m_blockTimer += dt;
	}

	// Token: 0x060002D5 RID: 725 RVA: 0x00019AA8 File Offset: 0x00017CA8
	public bool HideHandItems(bool onlyRightHand = false, bool animation = true)
	{
		if (this.m_leftItem == null && this.m_rightItem == null)
		{
			return false;
		}
		if (!onlyRightHand)
		{
			ItemDrop.ItemData leftItem = this.m_leftItem;
			this.UnequipItem(this.m_leftItem, true);
			this.m_hiddenLeftItem = leftItem;
		}
		else
		{
			this.m_hiddenLeftItem = null;
		}
		ItemDrop.ItemData rightItem = this.m_rightItem;
		this.UnequipItem(this.m_rightItem, true);
		this.m_hiddenRightItem = rightItem;
		this.SetupVisEquipment(this.m_visEquipment, false);
		if (animation)
		{
			this.m_zanim.SetTrigger("equip_hip");
		}
		return true;
	}

	// Token: 0x060002D6 RID: 726 RVA: 0x00019B2C File Offset: 0x00017D2C
	protected void ShowHandItems(bool onlyRightHand = false, bool animation = true)
	{
		ItemDrop.ItemData hiddenLeftItem = this.m_hiddenLeftItem;
		ItemDrop.ItemData hiddenRightItem = this.m_hiddenRightItem;
		if (hiddenLeftItem == null && hiddenRightItem == null)
		{
			return;
		}
		if (!onlyRightHand)
		{
			this.m_hiddenLeftItem = null;
			if (hiddenLeftItem != null)
			{
				this.EquipItem(hiddenLeftItem, true);
			}
		}
		this.m_hiddenRightItem = null;
		if (hiddenRightItem != null)
		{
			this.EquipItem(hiddenRightItem, true);
		}
		if (animation)
		{
			this.m_zanim.SetTrigger("equip_hip");
		}
	}

	// Token: 0x060002D7 RID: 727 RVA: 0x00019B8A File Offset: 0x00017D8A
	public ItemDrop.ItemData GetAmmoItem()
	{
		return this.m_ammoItem;
	}

	// Token: 0x060002D8 RID: 728 RVA: 0x00019B92 File Offset: 0x00017D92
	public virtual GameObject GetHoverObject()
	{
		return null;
	}

	// Token: 0x060002D9 RID: 729 RVA: 0x00019B95 File Offset: 0x00017D95
	public bool IsTeleportable()
	{
		return this.m_inventory.IsTeleportable();
	}

	// Token: 0x060002DA RID: 730 RVA: 0x00019BA4 File Offset: 0x00017DA4
	public override bool UseMeleeCamera()
	{
		ItemDrop.ItemData currentWeapon = this.GetCurrentWeapon();
		return currentWeapon != null && currentWeapon.m_shared.m_centerCamera;
	}

	// Token: 0x060002DB RID: 731 RVA: 0x00019BC8 File Offset: 0x00017DC8
	public float GetEquipmentWeight()
	{
		float num = 0f;
		if (this.m_rightItem != null)
		{
			num += this.m_rightItem.m_shared.m_weight;
		}
		if (this.m_leftItem != null)
		{
			num += this.m_leftItem.m_shared.m_weight;
		}
		if (this.m_chestItem != null)
		{
			num += this.m_chestItem.m_shared.m_weight;
		}
		if (this.m_legItem != null)
		{
			num += this.m_legItem.m_shared.m_weight;
		}
		if (this.m_helmetItem != null)
		{
			num += this.m_helmetItem.m_shared.m_weight;
		}
		if (this.m_shoulderItem != null)
		{
			num += this.m_shoulderItem.m_shared.m_weight;
		}
		if (this.m_utilityItem != null)
		{
			num += this.m_utilityItem.m_shared.m_weight;
		}
		return num;
	}

	// Token: 0x1700000A RID: 10
	// (get) Token: 0x060002DC RID: 732 RVA: 0x00019C99 File Offset: 0x00017E99
	public ItemDrop.ItemData RightItem
	{
		get
		{
			return this.m_rightItem;
		}
	}

	// Token: 0x1700000B RID: 11
	// (get) Token: 0x060002DD RID: 733 RVA: 0x00019CA1 File Offset: 0x00017EA1
	public ItemDrop.ItemData LeftItem
	{
		get
		{
			return this.m_leftItem;
		}
	}

	// Token: 0x040003AB RID: 939
	private static List<ItemDrop.ItemData> optimalWeapons = new List<ItemDrop.ItemData>();

	// Token: 0x040003AC RID: 940
	private static List<ItemDrop.ItemData> outofRangeWeapons = new List<ItemDrop.ItemData>();

	// Token: 0x040003AD RID: 941
	private static List<ItemDrop.ItemData> allWeapons = new List<ItemDrop.ItemData>();

	// Token: 0x040003AE RID: 942
	[Header("Humanoid")]
	public float m_equipStaminaDrain = 10f;

	// Token: 0x040003AF RID: 943
	public float m_blockStaminaDrain = 25f;

	// Token: 0x040003B0 RID: 944
	[Header("Default items")]
	public GameObject[] m_defaultItems;

	// Token: 0x040003B1 RID: 945
	public GameObject[] m_randomWeapon;

	// Token: 0x040003B2 RID: 946
	public GameObject[] m_randomArmor;

	// Token: 0x040003B3 RID: 947
	public GameObject[] m_randomShield;

	// Token: 0x040003B4 RID: 948
	public Humanoid.ItemSet[] m_randomSets;

	// Token: 0x040003B5 RID: 949
	public Humanoid.RandomItem[] m_randomItems;

	// Token: 0x040003B6 RID: 950
	public ItemDrop m_unarmedWeapon;

	// Token: 0x040003B7 RID: 951
	private bool[] m_randomItemSlotFilled;

	// Token: 0x040003B8 RID: 952
	[Header("Effects")]
	public EffectList m_pickupEffects = new EffectList();

	// Token: 0x040003B9 RID: 953
	public EffectList m_dropEffects = new EffectList();

	// Token: 0x040003BA RID: 954
	public EffectList m_consumeItemEffects = new EffectList();

	// Token: 0x040003BB RID: 955
	public EffectList m_equipEffects = new EffectList();

	// Token: 0x040003BC RID: 956
	public EffectList m_perfectBlockEffect = new EffectList();

	// Token: 0x040003BD RID: 957
	protected readonly Inventory m_inventory = new Inventory("Inventory", null, 8, 4);

	// Token: 0x040003BE RID: 958
	protected ItemDrop.ItemData m_rightItem;

	// Token: 0x040003BF RID: 959
	protected ItemDrop.ItemData m_leftItem;

	// Token: 0x040003C0 RID: 960
	protected ItemDrop.ItemData m_chestItem;

	// Token: 0x040003C1 RID: 961
	protected ItemDrop.ItemData m_legItem;

	// Token: 0x040003C2 RID: 962
	protected ItemDrop.ItemData m_ammoItem;

	// Token: 0x040003C3 RID: 963
	protected ItemDrop.ItemData m_helmetItem;

	// Token: 0x040003C4 RID: 964
	protected ItemDrop.ItemData m_shoulderItem;

	// Token: 0x040003C5 RID: 965
	protected ItemDrop.ItemData m_utilityItem;

	// Token: 0x040003C6 RID: 966
	protected string m_beardItem = "";

	// Token: 0x040003C7 RID: 967
	protected string m_hairItem = "";

	// Token: 0x040003C8 RID: 968
	protected Attack m_currentAttack;

	// Token: 0x040003C9 RID: 969
	protected bool m_currentAttackIsSecondary;

	// Token: 0x040003CA RID: 970
	protected float m_attackDrawTime;

	// Token: 0x040003CB RID: 971
	protected float m_lastCombatTimer = 999f;

	// Token: 0x040003CC RID: 972
	protected VisEquipment m_visEquipment;

	// Token: 0x040003CD RID: 973
	private Attack m_previousAttack;

	// Token: 0x040003CE RID: 974
	private ItemDrop.ItemData m_hiddenLeftItem;

	// Token: 0x040003CF RID: 975
	private ItemDrop.ItemData m_hiddenRightItem;

	// Token: 0x040003D0 RID: 976
	private int m_lastEquipEffectFrame;

	// Token: 0x040003D1 RID: 977
	private float m_timeSinceLastAttack;

	// Token: 0x040003D2 RID: 978
	private bool m_internalBlockingState;

	// Token: 0x040003D3 RID: 979
	private float m_blockTimer = 9999f;

	// Token: 0x040003D4 RID: 980
	private const float m_perfectBlockInterval = 0.25f;

	// Token: 0x040003D5 RID: 981
	private readonly HashSet<StatusEffect> m_equipmentStatusEffects = new HashSet<StatusEffect>();

	// Token: 0x040003D6 RID: 982
	private int m_seed;

	// Token: 0x040003D7 RID: 983
	private int m_useItemBlockMessage;

	// Token: 0x040003D8 RID: 984
	private int m_lastGroundColliderOnAttackStart = -1;

	// Token: 0x040003D9 RID: 985
	private float m_useItemTime;

	// Token: 0x040003DA RID: 986
	private ItemDrop.ItemData.SharedData m_useItemVisual;

	// Token: 0x040003DB RID: 987
	private bool m_hidHandsOnEat;

	// Token: 0x040003DC RID: 988
	private static readonly int s_statef = ZSyncAnimation.GetHash("statef");

	// Token: 0x040003DD RID: 989
	private static readonly int s_statei = ZSyncAnimation.GetHash("statei");

	// Token: 0x040003DE RID: 990
	private static readonly int s_blocking = ZSyncAnimation.GetHash("blocking");

	// Token: 0x040003DF RID: 991
	protected static readonly int s_animatorTagAttack = ZSyncAnimation.GetHash("attack");

	// Token: 0x02000234 RID: 564
	[Serializable]
	public class ItemSet
	{
		// Token: 0x04001F87 RID: 8071
		public string m_name = "";

		// Token: 0x04001F88 RID: 8072
		public GameObject[] m_items = Array.Empty<GameObject>();
	}

	// Token: 0x02000235 RID: 565
	[Serializable]
	public class RandomItem
	{
		// Token: 0x04001F89 RID: 8073
		public GameObject m_prefab;

		// Token: 0x04001F8A RID: 8074
		[Range(0f, 1f)]
		public float m_chance = 0.5f;
	}
}
