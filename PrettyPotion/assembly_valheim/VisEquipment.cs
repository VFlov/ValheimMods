using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Token: 0x02000047 RID: 71
public class VisEquipment : MonoBehaviour, IMonoUpdater
{
	// Token: 0x060005AB RID: 1451 RVA: 0x0002FF14 File Offset: 0x0002E114
	private void Awake()
	{
		this.m_nview = ((this.m_nViewOverride != null) ? this.m_nViewOverride : base.GetComponent<ZNetView>());
		Transform transform = base.transform.Find("Visual");
		if (transform == null)
		{
			transform = base.transform;
		}
		this.m_visual = transform.gameObject;
		this.m_lodGroup = this.m_visual.GetComponentInChildren<LODGroup>();
		if (this.m_bodyModel != null && this.m_bodyModel.material.HasProperty("_ChestTex"))
		{
			this.m_emptyBodyTexture = this.m_bodyModel.material.GetTexture("_ChestTex");
		}
		if (this.m_bodyModel != null && this.m_bodyModel.material.HasProperty("_LegsTex"))
		{
			this.m_emptyLegsTexture = this.m_bodyModel.material.GetTexture("_LegsTex");
		}
	}

	// Token: 0x060005AC RID: 1452 RVA: 0x00030001 File Offset: 0x0002E201
	private void OnEnable()
	{
		VisEquipment.Instances.Add(this);
	}

	// Token: 0x060005AD RID: 1453 RVA: 0x0003000E File Offset: 0x0002E20E
	private void OnDisable()
	{
		VisEquipment.Instances.Remove(this);
	}

	// Token: 0x060005AE RID: 1454 RVA: 0x0003001C File Offset: 0x0002E21C
	private void Start()
	{
		this.UpdateVisuals();
	}

	// Token: 0x060005AF RID: 1455 RVA: 0x00030024 File Offset: 0x0002E224
	public void SetWeaponTrails(bool enabled)
	{
		if (this.m_useAllTrails)
		{
			MeleeWeaponTrail[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeleeWeaponTrail>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Emit = enabled;
			}
			return;
		}
		if (this.m_rightItemInstance)
		{
			MeleeWeaponTrail[] componentsInChildren = this.m_rightItemInstance.GetComponentsInChildren<MeleeWeaponTrail>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Emit = enabled;
			}
		}
	}

	// Token: 0x060005B0 RID: 1456 RVA: 0x00030090 File Offset: 0x0002E290
	public void SetModel(int index)
	{
		if (this.m_modelIndex == index)
		{
			return;
		}
		if (index < 0 || index >= this.m_models.Length)
		{
			return;
		}
		ZLog.Log("Vis equip model set to " + index.ToString());
		this.m_modelIndex = index;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_modelIndex, this.m_modelIndex, false);
		}
	}

	// Token: 0x060005B1 RID: 1457 RVA: 0x0003010C File Offset: 0x0002E30C
	public void SetSkinColor(Vector3 color)
	{
		if (color == this.m_skinColor)
		{
			return;
		}
		this.m_skinColor = color;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_skinColor, this.m_skinColor);
		}
	}

	// Token: 0x060005B2 RID: 1458 RVA: 0x00030164 File Offset: 0x0002E364
	public void SetHairColor(Vector3 color)
	{
		if (this.m_hairColor == color)
		{
			return;
		}
		this.m_hairColor = color;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_hairColor, this.m_hairColor);
		}
	}

	// Token: 0x060005B3 RID: 1459 RVA: 0x000301BC File Offset: 0x0002E3BC
	public void SetItem(VisSlot slot, string name, int variant = 0)
	{
		switch (slot)
		{
		case VisSlot.HandLeft:
			this.SetLeftItem(name, variant);
			return;
		case VisSlot.HandRight:
			this.SetRightItem(name);
			return;
		case VisSlot.BackLeft:
			this.SetLeftBackItem(name, variant);
			return;
		case VisSlot.BackRight:
			this.SetRightBackItem(name);
			return;
		case VisSlot.Chest:
			this.SetChestItem(name);
			return;
		case VisSlot.Legs:
			this.SetLegItem(name);
			return;
		case VisSlot.Helmet:
			this.SetHelmetItem(name);
			return;
		case VisSlot.Shoulder:
			this.SetShoulderItem(name, variant);
			return;
		case VisSlot.Utility:
			this.SetUtilityItem(name);
			return;
		case VisSlot.Beard:
			this.SetBeardItem(name);
			return;
		case VisSlot.Hair:
			this.SetHairItem(name);
			return;
		default:
			throw new NotImplementedException("Unknown slot: " + slot.ToString());
		}
	}

	// Token: 0x060005B4 RID: 1460 RVA: 0x00030274 File Offset: 0x0002E474
	public void SetLeftItem(string name, int variant)
	{
		if (this.m_leftItem == name && this.m_leftItemVariant == variant)
		{
			return;
		}
		this.m_leftItem = name;
		this.m_leftItemVariant = variant;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_leftItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
			this.m_nview.GetZDO().Set(ZDOVars.s_leftItemVariant, variant, false);
		}
	}

	// Token: 0x060005B5 RID: 1461 RVA: 0x000302FF File Offset: 0x0002E4FF
	public void SetRightItem(string name)
	{
		if (this.m_rightItem == name)
		{
			return;
		}
		this.m_rightItem = name;
		this.SetRightItemVisual(name);
	}

	// Token: 0x060005B6 RID: 1462 RVA: 0x00030320 File Offset: 0x0002E520
	public void SetRightItemVisual(string name)
	{
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_rightItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	// Token: 0x060005B7 RID: 1463 RVA: 0x00030370 File Offset: 0x0002E570
	public void SetLeftBackItem(string name, int variant)
	{
		if (this.m_leftBackItem == name && this.m_leftBackItemVariant == variant)
		{
			return;
		}
		this.m_leftBackItem = name;
		this.m_leftBackItemVariant = variant;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_leftBackItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
			this.m_nview.GetZDO().Set(ZDOVars.s_leftBackItemVariant, variant, false);
		}
	}

	// Token: 0x060005B8 RID: 1464 RVA: 0x000303FC File Offset: 0x0002E5FC
	public void SetRightBackItem(string name)
	{
		if (this.m_rightBackItem == name)
		{
			return;
		}
		this.m_rightBackItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_rightBackItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	// Token: 0x060005B9 RID: 1465 RVA: 0x00030460 File Offset: 0x0002E660
	public void SetChestItem(string name)
	{
		if (this.m_chestItem == name)
		{
			return;
		}
		this.m_chestItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_chestItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	// Token: 0x060005BA RID: 1466 RVA: 0x000304C4 File Offset: 0x0002E6C4
	public void SetLegItem(string name)
	{
		if (this.m_legItem == name)
		{
			return;
		}
		this.m_legItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_legItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	// Token: 0x060005BB RID: 1467 RVA: 0x00030528 File Offset: 0x0002E728
	public void SetHelmetItem(string name)
	{
		if (this.m_helmetItem == name)
		{
			return;
		}
		this.m_helmetItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_helmetItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	// Token: 0x060005BC RID: 1468 RVA: 0x0003058C File Offset: 0x0002E78C
	public void SetShoulderItem(string name, int variant)
	{
		if (this.m_shoulderItem == name && this.m_shoulderItemVariant == variant)
		{
			return;
		}
		this.m_shoulderItem = name;
		this.m_shoulderItemVariant = variant;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_shoulderItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
			this.m_nview.GetZDO().Set(ZDOVars.s_shoulderItemVariant, variant, false);
		}
	}

	// Token: 0x060005BD RID: 1469 RVA: 0x00030618 File Offset: 0x0002E818
	public void SetBeardItem(string name)
	{
		if (this.m_beardItem == name)
		{
			return;
		}
		this.m_beardItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_beardItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	// Token: 0x060005BE RID: 1470 RVA: 0x0003067C File Offset: 0x0002E87C
	public void SetHairItem(string name)
	{
		if (this.m_hairItem == name)
		{
			return;
		}
		this.m_hairItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_hairItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	// Token: 0x060005BF RID: 1471 RVA: 0x000306E0 File Offset: 0x0002E8E0
	public void SetUtilityItem(string name)
	{
		if (this.m_utilityItem == name)
		{
			return;
		}
		this.m_utilityItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_utilityItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	// Token: 0x060005C0 RID: 1472 RVA: 0x00030744 File Offset: 0x0002E944
	public void CustomUpdate(float deltaTime, float time)
	{
		this.UpdateVisuals();
	}

	// Token: 0x060005C1 RID: 1473 RVA: 0x0003074C File Offset: 0x0002E94C
	private void UpdateVisuals()
	{
		this.UpdateEquipmentVisuals();
		if (this.m_isPlayer)
		{
			this.UpdateBaseModel();
			this.UpdateColors();
		}
	}

	// Token: 0x060005C2 RID: 1474 RVA: 0x00030768 File Offset: 0x0002E968
	private void UpdateColors()
	{
		Color value = Utils.Vec3ToColor(this.m_skinColor);
		Color value2 = Utils.Vec3ToColor(this.m_hairColor);
		if (this.m_nview.GetZDO() != null)
		{
			value = Utils.Vec3ToColor(this.m_nview.GetZDO().GetVec3(ZDOVars.s_skinColor, Vector3.one));
			value2 = Utils.Vec3ToColor(this.m_nview.GetZDO().GetVec3(ZDOVars.s_hairColor, Vector3.one));
		}
		this.m_bodyModel.materials[0].SetColor("_SkinColor", value);
		this.m_bodyModel.materials[1].SetColor("_SkinColor", value2);
		if (this.m_beardItemInstance)
		{
			Renderer[] componentsInChildren = this.m_beardItemInstance.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].material.SetColor("_SkinColor", value2);
			}
		}
		if (this.m_hairItemInstance)
		{
			Renderer[] componentsInChildren = this.m_hairItemInstance.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].material.SetColor("_SkinColor", value2);
			}
		}
	}

	// Token: 0x060005C3 RID: 1475 RVA: 0x00030880 File Offset: 0x0002EA80
	private void UpdateBaseModel()
	{
		if (this.m_models.Length == 0)
		{
			return;
		}
		int num = this.m_modelIndex;
		if (this.m_nview.GetZDO() != null)
		{
			num = this.m_nview.GetZDO().GetInt(ZDOVars.s_modelIndex, 0);
		}
		if (this.m_currentModelIndex != num || this.m_bodyModel.sharedMesh != this.m_models[num].m_mesh)
		{
			this.m_currentModelIndex = num;
			this.m_bodyModel.sharedMesh = this.m_models[num].m_mesh;
			this.m_bodyModel.materials[0].SetTexture("_MainTex", this.m_models[num].m_baseMaterial.GetTexture("_MainTex"));
			this.m_bodyModel.materials[0].SetTexture("_SkinBumpMap", this.m_models[num].m_baseMaterial.GetTexture("_SkinBumpMap"));
		}
	}

	// Token: 0x060005C4 RID: 1476 RVA: 0x00030968 File Offset: 0x0002EB68
	private void UpdateEquipmentVisuals()
	{
		int hash = 0;
		int rightHandEquipped = 0;
		int chestEquipped = 0;
		int legEquipped = 0;
		int hash2 = 0;
		int num = 0;
		int num2 = 0;
		int hash3 = 0;
		int utilityEquipped = 0;
		int leftItem = 0;
		int rightItem = 0;
		int variant = this.m_shoulderItemVariant;
		int variant2 = this.m_leftItemVariant;
		int leftVariant = this.m_leftBackItemVariant;
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo != null)
		{
			hash = zdo.GetInt(ZDOVars.s_leftItem, 0);
			rightHandEquipped = zdo.GetInt(ZDOVars.s_rightItem, 0);
			chestEquipped = zdo.GetInt(ZDOVars.s_chestItem, 0);
			legEquipped = zdo.GetInt(ZDOVars.s_legItem, 0);
			hash2 = zdo.GetInt(ZDOVars.s_helmetItem, 0);
			hash3 = zdo.GetInt(ZDOVars.s_shoulderItem, 0);
			utilityEquipped = zdo.GetInt(ZDOVars.s_utilityItem, 0);
			if (this.m_isPlayer)
			{
				num = zdo.GetInt(ZDOVars.s_beardItem, 0);
				num2 = zdo.GetInt(ZDOVars.s_hairItem, 0);
				leftItem = zdo.GetInt(ZDOVars.s_leftBackItem, 0);
				rightItem = zdo.GetInt(ZDOVars.s_rightBackItem, 0);
				variant = zdo.GetInt(ZDOVars.s_shoulderItemVariant, 0);
				variant2 = zdo.GetInt(ZDOVars.s_leftItemVariant, 0);
				leftVariant = zdo.GetInt(ZDOVars.s_leftBackItemVariant, 0);
			}
		}
		else
		{
			if (!string.IsNullOrEmpty(this.m_leftItem))
			{
				hash = this.m_leftItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_rightItem))
			{
				rightHandEquipped = this.m_rightItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_chestItem))
			{
				chestEquipped = this.m_chestItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_legItem))
			{
				legEquipped = this.m_legItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_helmetItem))
			{
				hash2 = this.m_helmetItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_shoulderItem))
			{
				hash3 = this.m_shoulderItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_utilityItem))
			{
				utilityEquipped = this.m_utilityItem.GetStableHashCode();
			}
			if (this.m_isPlayer)
			{
				if (!string.IsNullOrEmpty(this.m_beardItem))
				{
					num = this.m_beardItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(this.m_hairItem))
				{
					num2 = this.m_hairItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(this.m_leftBackItem))
				{
					leftItem = this.m_leftBackItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(this.m_rightBackItem))
				{
					rightItem = this.m_rightBackItem.GetStableHashCode();
				}
			}
		}
		bool flag = false;
		flag = (this.SetRightHandEquipped(rightHandEquipped) || flag);
		flag = (this.SetLeftHandEquipped(hash, variant2) || flag);
		flag = (this.SetChestEquipped(chestEquipped) || flag);
		flag = (this.SetLegEquipped(legEquipped) || flag);
		flag = (this.SetHelmetEquipped(hash2, num2) || flag);
		flag = (this.SetShoulderEquipped(hash3, variant) || flag);
		flag = (this.SetUtilityEquipped(utilityEquipped) || flag);
		if (this.m_isPlayer)
		{
			num = this.GetHairItem(this.m_helmetHideBeard, num, ItemDrop.ItemData.AccessoryType.Beard);
			flag = (this.SetBeardEquipped(num) || flag);
			flag = (this.SetBackEquipped(leftItem, rightItem, leftVariant) || flag);
			num2 = this.GetHairItem(this.m_helmetHideHair, num2, ItemDrop.ItemData.AccessoryType.Hair);
			flag = (this.SetHairEquipped(num2) || flag);
		}
		if (flag)
		{
			this.UpdateLodgroup();
		}
	}

	// Token: 0x060005C5 RID: 1477 RVA: 0x00030C80 File Offset: 0x0002EE80
	private int GetHairItem(ItemDrop.ItemData.HelmetHairType type, int itemHash, ItemDrop.ItemData.AccessoryType accessory)
	{
		if (type == ItemDrop.ItemData.HelmetHairType.Hidden)
		{
			return 0;
		}
		if (type == ItemDrop.ItemData.HelmetHairType.Default)
		{
			return itemHash;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab)
		{
			ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
			if (component != null)
			{
				List<ItemDrop.ItemData.HelmetHairSettings> source;
				if (accessory != ItemDrop.ItemData.AccessoryType.Hair)
				{
					if (accessory != ItemDrop.ItemData.AccessoryType.Beard)
					{
						throw new Exception("Acecssory type not implemented");
					}
					source = component.m_itemData.m_shared.m_helmetBeardSettings;
				}
				else
				{
					source = component.m_itemData.m_shared.m_helmetHairSettings;
				}
				ItemDrop.ItemData.HelmetHairSettings helmetHairSettings = source.FirstOrDefault((ItemDrop.ItemData.HelmetHairSettings x) => x.m_setting == type);
				if (helmetHairSettings != null)
				{
					return helmetHairSettings.m_hairPrefab.name.GetStableHashCode();
				}
			}
		}
		return 0;
	}

	// Token: 0x060005C6 RID: 1478 RVA: 0x00030D34 File Offset: 0x0002EF34
	private void UpdateLodgroup()
	{
		if (this.m_lodGroup == null)
		{
			return;
		}
		List<Renderer> list = new List<Renderer>(this.m_visual.GetComponentsInChildren<Renderer>());
		for (int i = list.Count - 1; i >= 0; i--)
		{
			Renderer renderer = list[i];
			LODGroup componentInParent = renderer.GetComponentInParent<LODGroup>();
			if (componentInParent != null && componentInParent != this.m_lodGroup)
			{
				LOD[] lods = componentInParent.GetLODs();
				for (int j = 0; j < lods.Length; j++)
				{
					if (Array.IndexOf<Renderer>(lods[j].renderers, renderer) >= 0)
					{
						list.RemoveAt(i);
						break;
					}
				}
			}
		}
		LOD[] lods2 = this.m_lodGroup.GetLODs();
		lods2[0].renderers = list.ToArray();
		this.m_lodGroup.SetLODs(lods2);
	}

	// Token: 0x060005C7 RID: 1479 RVA: 0x00030E00 File Offset: 0x0002F000
	private bool SetRightHandEquipped(int hash)
	{
		if (this.m_currentRightItemHash == hash)
		{
			return false;
		}
		if (this.m_rightItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_rightItemInstance);
			this.m_rightItemInstance = null;
		}
		this.m_currentRightItemHash = hash;
		if (hash != 0)
		{
			this.m_rightItemInstance = this.AttachItem(hash, 0, this.m_rightHand, true, false);
		}
		return true;
	}

	// Token: 0x060005C8 RID: 1480 RVA: 0x00030E58 File Offset: 0x0002F058
	private bool SetLeftHandEquipped(int hash, int variant)
	{
		if (this.m_currentLeftItemHash == hash && this.m_currentLeftItemVariant == variant)
		{
			return false;
		}
		if (this.m_leftItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_leftItemInstance);
			this.m_leftItemInstance = null;
		}
		this.m_currentLeftItemHash = hash;
		this.m_currentLeftItemVariant = variant;
		if (hash != 0)
		{
			this.m_leftItemInstance = this.AttachItem(hash, variant, this.m_leftHand, true, false);
		}
		return true;
	}

	// Token: 0x060005C9 RID: 1481 RVA: 0x00030EC0 File Offset: 0x0002F0C0
	private bool SetBackEquipped(int leftItem, int rightItem, int leftVariant)
	{
		if (this.m_currentLeftBackItemHash == leftItem && this.m_currentRightBackItemHash == rightItem && this.m_currentLeftBackItemVariant == leftVariant)
		{
			return false;
		}
		if (this.m_leftBackItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_leftBackItemInstance);
			this.m_leftBackItemInstance = null;
		}
		if (this.m_rightBackItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_rightBackItemInstance);
			this.m_rightBackItemInstance = null;
		}
		this.m_currentLeftBackItemHash = leftItem;
		this.m_currentRightBackItemHash = rightItem;
		this.m_currentLeftBackItemVariant = leftVariant;
		if (this.m_currentLeftBackItemHash != 0)
		{
			this.m_leftBackItemInstance = this.AttachBackItem(leftItem, leftVariant, false);
		}
		if (this.m_currentRightBackItemHash != 0)
		{
			this.m_rightBackItemInstance = this.AttachBackItem(rightItem, 0, true);
		}
		return true;
	}

	// Token: 0x060005CA RID: 1482 RVA: 0x00030F6C File Offset: 0x0002F16C
	private GameObject AttachBackItem(int hash, int variant, bool rightHand)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing back attach item prefab: " + hash.ToString());
			return null;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		ItemDrop.ItemData.ItemType itemType = (component.m_itemData.m_shared.m_attachOverride != ItemDrop.ItemData.ItemType.None) ? component.m_itemData.m_shared.m_attachOverride : component.m_itemData.m_shared.m_itemType;
		if (itemType == ItemDrop.ItemData.ItemType.Torch)
		{
			if (rightHand)
			{
				return this.AttachItem(hash, variant, this.m_backMelee, false, true);
			}
			return this.AttachItem(hash, variant, this.m_backTool, false, true);
		}
		else
		{
			switch (itemType)
			{
			case ItemDrop.ItemData.ItemType.OneHandedWeapon:
				return this.AttachItem(hash, variant, this.m_backMelee, false, true);
			case ItemDrop.ItemData.ItemType.Bow:
				return this.AttachItem(hash, variant, this.m_backBow, false, true);
			case ItemDrop.ItemData.ItemType.Shield:
				return this.AttachItem(hash, variant, this.m_backShield, false, true);
			default:
				if (itemType != ItemDrop.ItemData.ItemType.TwoHandedWeapon)
				{
					switch (itemType)
					{
					case ItemDrop.ItemData.ItemType.Tool:
						return this.AttachItem(hash, variant, this.m_backTool, false, true);
					case ItemDrop.ItemData.ItemType.Attach_Atgeir:
						return this.AttachItem(hash, variant, this.m_backAtgeir, false, true);
					case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
						goto IL_10B;
					}
					return null;
				}
				IL_10B:
				return this.AttachItem(hash, variant, this.m_backTwohandedMelee, false, true);
			}
		}
	}

	// Token: 0x060005CB RID: 1483 RVA: 0x000310A8 File Offset: 0x0002F2A8
	private bool SetChestEquipped(int hash)
	{
		if (this.m_currentChestItemHash == hash)
		{
			return false;
		}
		this.m_currentChestItemHash = hash;
		if (this.m_bodyModel == null)
		{
			return true;
		}
		if (this.m_chestItemInstances != null)
		{
			foreach (GameObject gameObject in this.m_chestItemInstances)
			{
				if (this.m_lodGroup)
				{
					Utils.RemoveFromLodgroup(this.m_lodGroup, gameObject);
				}
				UnityEngine.Object.Destroy(gameObject);
			}
			this.m_chestItemInstances = null;
			this.m_bodyModel.material.SetTexture("_ChestTex", this.m_emptyBodyTexture);
			this.m_bodyModel.material.SetTexture("_ChestBumpMap", null);
			this.m_bodyModel.material.SetTexture("_ChestMetal", null);
		}
		if (this.m_currentChestItemHash == 0)
		{
			return true;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing chest item " + hash.ToString());
			return true;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if (component.m_itemData.m_shared.m_armorMaterial)
		{
			this.m_bodyModel.material.SetTexture("_ChestTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestTex"));
			this.m_bodyModel.material.SetTexture("_ChestBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestBumpMap"));
			this.m_bodyModel.material.SetTexture("_ChestMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestMetal"));
		}
		this.m_chestItemInstances = this.AttachArmor(hash, -1);
		return true;
	}

	// Token: 0x060005CC RID: 1484 RVA: 0x00031284 File Offset: 0x0002F484
	private bool SetShoulderEquipped(int hash, int variant)
	{
		if (this.m_currentShoulderItemHash == hash && this.m_currentShoulderItemVariant == variant)
		{
			return false;
		}
		this.m_currentShoulderItemHash = hash;
		this.m_currentShoulderItemVariant = variant;
		if (this.m_bodyModel == null)
		{
			return true;
		}
		if (this.m_shoulderItemInstances != null)
		{
			foreach (GameObject gameObject in this.m_shoulderItemInstances)
			{
				if (this.m_lodGroup)
				{
					Utils.RemoveFromLodgroup(this.m_lodGroup, gameObject);
				}
				UnityEngine.Object.Destroy(gameObject);
			}
			this.m_shoulderItemInstances = null;
		}
		if (this.m_currentShoulderItemHash == 0)
		{
			return true;
		}
		if (ObjectDB.instance.GetItemPrefab(hash) == null)
		{
			ZLog.Log("Missing shoulder item " + hash.ToString());
			return true;
		}
		this.m_shoulderItemInstances = this.AttachArmor(hash, variant);
		return true;
	}

	// Token: 0x060005CD RID: 1485 RVA: 0x00031374 File Offset: 0x0002F574
	private bool SetLegEquipped(int hash)
	{
		if (this.m_currentLegItemHash == hash)
		{
			return false;
		}
		this.m_currentLegItemHash = hash;
		if (this.m_bodyModel == null)
		{
			return true;
		}
		if (this.m_legItemInstances != null)
		{
			foreach (GameObject obj in this.m_legItemInstances)
			{
				UnityEngine.Object.Destroy(obj);
			}
			this.m_legItemInstances = null;
			this.m_bodyModel.material.SetTexture("_LegsTex", this.m_emptyLegsTexture);
			this.m_bodyModel.material.SetTexture("_LegsBumpMap", null);
			this.m_bodyModel.material.SetTexture("_LegsMetal", null);
		}
		if (this.m_currentLegItemHash == 0)
		{
			return true;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing legs item " + hash.ToString());
			return true;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if (component.m_itemData.m_shared.m_armorMaterial)
		{
			this.m_bodyModel.material.SetTexture("_LegsTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsTex"));
			this.m_bodyModel.material.SetTexture("_LegsBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsBumpMap"));
			this.m_bodyModel.material.SetTexture("_LegsMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsMetal"));
		}
		this.m_legItemInstances = this.AttachArmor(hash, -1);
		return true;
	}

	// Token: 0x060005CE RID: 1486 RVA: 0x00031534 File Offset: 0x0002F734
	private bool SetBeardEquipped(int hash)
	{
		if (this.m_currentBeardItemHash == hash)
		{
			return false;
		}
		if (this.m_beardItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_beardItemInstance);
			this.m_beardItemInstance = null;
		}
		this.m_currentBeardItemHash = hash;
		if (hash != 0)
		{
			this.m_beardItemInstance = this.AttachItem(hash, 0, this.m_helmet, true, false);
		}
		return true;
	}

	// Token: 0x060005CF RID: 1487 RVA: 0x0003158C File Offset: 0x0002F78C
	private bool SetHairEquipped(int hash)
	{
		if (this.m_currentHairItemHash == hash)
		{
			return false;
		}
		if (this.m_hairItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_hairItemInstance);
			this.m_hairItemInstance = null;
		}
		this.m_currentHairItemHash = hash;
		if (hash != 0)
		{
			this.m_hairItemInstance = this.AttachItem(hash, 0, this.m_helmet, true, false);
		}
		return true;
	}

	// Token: 0x060005D0 RID: 1488 RVA: 0x000315E4 File Offset: 0x0002F7E4
	private bool SetHelmetEquipped(int hash, int hairHash)
	{
		if (this.m_currentHelmetItemHash == hash)
		{
			return false;
		}
		if (this.m_helmetItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_helmetItemInstance);
			this.m_helmetItemInstance = null;
		}
		this.m_currentHelmetItemHash = hash;
		VisEquipment.HelmetHides(hash, out this.m_helmetHideHair, out this.m_helmetHideBeard);
		if (hash != 0)
		{
			this.m_helmetItemInstance = this.AttachItem(hash, 0, this.m_helmet, true, false);
		}
		return true;
	}

	// Token: 0x060005D1 RID: 1489 RVA: 0x00031650 File Offset: 0x0002F850
	private bool SetUtilityEquipped(int hash)
	{
		if (this.m_currentUtilityItemHash == hash)
		{
			return false;
		}
		if (this.m_utilityItemInstances != null)
		{
			foreach (GameObject gameObject in this.m_utilityItemInstances)
			{
				if (this.m_lodGroup)
				{
					Utils.RemoveFromLodgroup(this.m_lodGroup, gameObject);
				}
				UnityEngine.Object.Destroy(gameObject);
			}
			this.m_utilityItemInstances = null;
		}
		this.m_currentUtilityItemHash = hash;
		if (hash != 0)
		{
			this.m_utilityItemInstances = this.AttachArmor(hash, -1);
		}
		return true;
	}

	// Token: 0x060005D2 RID: 1490 RVA: 0x000316F0 File Offset: 0x0002F8F0
	private static void HelmetHides(int itemHash, out ItemDrop.ItemData.HelmetHairType hideHair, out ItemDrop.ItemData.HelmetHairType hideBeard)
	{
		hideHair = ItemDrop.ItemData.HelmetHairType.Default;
		hideBeard = ItemDrop.ItemData.HelmetHairType.Default;
		if (itemHash == 0)
		{
			return;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			return;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		hideHair = component.m_itemData.m_shared.m_helmetHideHair;
		hideBeard = component.m_itemData.m_shared.m_helmetHideBeard;
	}

	// Token: 0x060005D3 RID: 1491 RVA: 0x00031748 File Offset: 0x0002F948
	private List<GameObject> AttachArmor(int itemHash, int variant = -1)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing attach item: " + itemHash.ToString() + "  ob:" + base.gameObject.name);
			return null;
		}
		List<GameObject> list = new List<GameObject>();
		int childCount = itemPrefab.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = itemPrefab.transform.GetChild(i);
			if (child.gameObject.name.CustomStartsWith("attach_"))
			{
				string text = child.gameObject.name.Substring(7);
				GameObject gameObject;
				if (text == "skin")
				{
					gameObject = UnityEngine.Object.Instantiate<GameObject>(child.gameObject, this.m_bodyModel.transform.position, this.m_bodyModel.transform.parent.rotation, this.m_bodyModel.transform.parent);
					gameObject.SetActive(true);
					foreach (SkinnedMeshRenderer skinnedMeshRenderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
					{
						skinnedMeshRenderer.rootBone = this.m_bodyModel.rootBone;
						skinnedMeshRenderer.bones = this.m_bodyModel.bones;
					}
					foreach (Cloth cloth in gameObject.GetComponentsInChildren<Cloth>())
					{
						if (this.m_clothColliders.Length != 0)
						{
							if (cloth.capsuleColliders.Length != 0)
							{
								List<CapsuleCollider> list2 = new List<CapsuleCollider>(this.m_clothColliders);
								list2.AddRange(cloth.capsuleColliders);
								cloth.capsuleColliders = list2.ToArray();
							}
							else
							{
								cloth.capsuleColliders = this.m_clothColliders;
							}
						}
					}
				}
				else
				{
					Transform transform = Utils.FindChild(this.m_visual.transform, text, Utils.IterativeSearchType.DepthFirst);
					if (transform == null)
					{
						ZLog.LogWarning("Missing joint " + text + " in item " + itemPrefab.name);
						goto IL_256;
					}
					gameObject = UnityEngine.Object.Instantiate<GameObject>(child.gameObject);
					gameObject.SetActive(true);
					gameObject.transform.SetParent(transform);
					gameObject.transform.localPosition = Vector3.zero;
					gameObject.transform.localRotation = Quaternion.identity;
				}
				if (variant >= 0)
				{
					IEquipmentVisual componentInChildren = gameObject.GetComponentInChildren<IEquipmentVisual>();
					if (componentInChildren != null)
					{
						componentInChildren.Setup(variant);
					}
				}
				VisEquipment.CleanupInstance(gameObject);
				VisEquipment.EnableEquippedEffects(gameObject);
				list.Add(gameObject);
			}
			IL_256:;
		}
		return list;
	}

	// Token: 0x060005D4 RID: 1492 RVA: 0x000319B8 File Offset: 0x0002FBB8
	private GameObject AttachItem(int itemHash, int variant, Transform joint, bool enableEquipEffects = true, bool backAttach = false)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			ZLog.Log(string.Concat(new string[]
			{
				"Missing attach item: ",
				itemHash.ToString(),
				"  ob:",
				base.gameObject.name,
				"  joint:",
				joint ? joint.name : "none"
			}));
			return null;
		}
		GameObject gameObject = null;
		Transform transform = itemPrefab.transform.Find("equipoffset");
		int childCount = itemPrefab.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = itemPrefab.transform.GetChild(i);
			if (backAttach && child.gameObject.name == "attach_back")
			{
				gameObject = child.gameObject;
				break;
			}
			if (child.gameObject.name == "attach" || (!backAttach && child.gameObject.name == "attach_skin"))
			{
				gameObject = child.gameObject;
				break;
			}
		}
		if (gameObject == null)
		{
			return null;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject);
		gameObject2.SetActive(true);
		VisEquipment.CleanupInstance(gameObject2);
		if (enableEquipEffects)
		{
			VisEquipment.EnableEquippedEffects(gameObject2);
		}
		if (gameObject.name == "attach_skin")
		{
			gameObject2.transform.SetParent(this.m_bodyModel.transform.parent);
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in gameObject2.GetComponentsInChildren<SkinnedMeshRenderer>())
			{
				skinnedMeshRenderer.rootBone = this.m_bodyModel.rootBone;
				skinnedMeshRenderer.bones = this.m_bodyModel.bones;
			}
		}
		else
		{
			gameObject2.transform.SetParent(joint);
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
		}
		if (transform != null)
		{
			gameObject2.transform.localPosition += transform.position;
			gameObject2.transform.localRotation *= transform.rotation;
		}
		IEquipmentVisual componentInChildren = gameObject2.GetComponentInChildren<IEquipmentVisual>();
		if (componentInChildren != null)
		{
			componentInChildren.Setup(variant);
		}
		return gameObject2;
	}

	// Token: 0x060005D5 RID: 1493 RVA: 0x00031C24 File Offset: 0x0002FE24
	private static void CleanupInstance(GameObject instance)
	{
		Collider[] componentsInChildren = instance.GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
	}

	// Token: 0x060005D6 RID: 1494 RVA: 0x00031C50 File Offset: 0x0002FE50
	private static void EnableEquippedEffects(GameObject instance)
	{
		Transform transform = instance.transform.Find("equiped");
		if (transform)
		{
			transform.gameObject.SetActive(true);
		}
	}

	// Token: 0x060005D7 RID: 1495 RVA: 0x00031C84 File Offset: 0x0002FE84
	public int GetModelIndex()
	{
		int result = this.m_modelIndex;
		if (this.m_nview.IsValid())
		{
			result = this.m_nview.GetZDO().GetInt(ZDOVars.s_modelIndex, 0);
		}
		return result;
	}

	// Token: 0x17000011 RID: 17
	// (get) Token: 0x060005D8 RID: 1496 RVA: 0x00031CBD File Offset: 0x0002FEBD
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x0400064B RID: 1611
	public SkinnedMeshRenderer m_bodyModel;

	// Token: 0x0400064C RID: 1612
	public ZNetView m_nViewOverride;

	// Token: 0x0400064D RID: 1613
	[Header("Attachment points")]
	public Transform m_leftHand;

	// Token: 0x0400064E RID: 1614
	public Transform m_rightHand;

	// Token: 0x0400064F RID: 1615
	public Transform m_helmet;

	// Token: 0x04000650 RID: 1616
	public Transform m_backShield;

	// Token: 0x04000651 RID: 1617
	public Transform m_backMelee;

	// Token: 0x04000652 RID: 1618
	public Transform m_backTwohandedMelee;

	// Token: 0x04000653 RID: 1619
	public Transform m_backBow;

	// Token: 0x04000654 RID: 1620
	public Transform m_backTool;

	// Token: 0x04000655 RID: 1621
	public Transform m_backAtgeir;

	// Token: 0x04000656 RID: 1622
	public CapsuleCollider[] m_clothColliders = Array.Empty<CapsuleCollider>();

	// Token: 0x04000657 RID: 1623
	public VisEquipment.PlayerModel[] m_models = Array.Empty<VisEquipment.PlayerModel>();

	// Token: 0x04000658 RID: 1624
	public bool m_isPlayer;

	// Token: 0x04000659 RID: 1625
	public bool m_useAllTrails;

	// Token: 0x0400065A RID: 1626
	private string m_leftItem = "";

	// Token: 0x0400065B RID: 1627
	private string m_rightItem = "";

	// Token: 0x0400065C RID: 1628
	private string m_chestItem = "";

	// Token: 0x0400065D RID: 1629
	private string m_legItem = "";

	// Token: 0x0400065E RID: 1630
	private string m_helmetItem = "";

	// Token: 0x0400065F RID: 1631
	private string m_shoulderItem = "";

	// Token: 0x04000660 RID: 1632
	private string m_beardItem = "";

	// Token: 0x04000661 RID: 1633
	private string m_hairItem = "";

	// Token: 0x04000662 RID: 1634
	private string m_utilityItem = "";

	// Token: 0x04000663 RID: 1635
	private string m_leftBackItem = "";

	// Token: 0x04000664 RID: 1636
	private string m_rightBackItem = "";

	// Token: 0x04000665 RID: 1637
	private int m_shoulderItemVariant;

	// Token: 0x04000666 RID: 1638
	private int m_leftItemVariant;

	// Token: 0x04000667 RID: 1639
	private int m_leftBackItemVariant;

	// Token: 0x04000668 RID: 1640
	private GameObject m_leftItemInstance;

	// Token: 0x04000669 RID: 1641
	private GameObject m_rightItemInstance;

	// Token: 0x0400066A RID: 1642
	private GameObject m_helmetItemInstance;

	// Token: 0x0400066B RID: 1643
	private List<GameObject> m_chestItemInstances;

	// Token: 0x0400066C RID: 1644
	private List<GameObject> m_legItemInstances;

	// Token: 0x0400066D RID: 1645
	private List<GameObject> m_shoulderItemInstances;

	// Token: 0x0400066E RID: 1646
	private List<GameObject> m_utilityItemInstances;

	// Token: 0x0400066F RID: 1647
	private GameObject m_beardItemInstance;

	// Token: 0x04000670 RID: 1648
	private GameObject m_hairItemInstance;

	// Token: 0x04000671 RID: 1649
	private GameObject m_leftBackItemInstance;

	// Token: 0x04000672 RID: 1650
	private GameObject m_rightBackItemInstance;

	// Token: 0x04000673 RID: 1651
	private int m_currentLeftItemHash;

	// Token: 0x04000674 RID: 1652
	private int m_currentRightItemHash;

	// Token: 0x04000675 RID: 1653
	private int m_currentChestItemHash;

	// Token: 0x04000676 RID: 1654
	private int m_currentLegItemHash;

	// Token: 0x04000677 RID: 1655
	private int m_currentHelmetItemHash;

	// Token: 0x04000678 RID: 1656
	private int m_currentShoulderItemHash;

	// Token: 0x04000679 RID: 1657
	private int m_currentBeardItemHash;

	// Token: 0x0400067A RID: 1658
	private int m_currentHairItemHash;

	// Token: 0x0400067B RID: 1659
	private int m_currentUtilityItemHash;

	// Token: 0x0400067C RID: 1660
	private int m_currentLeftBackItemHash;

	// Token: 0x0400067D RID: 1661
	private int m_currentRightBackItemHash;

	// Token: 0x0400067E RID: 1662
	private int m_currentShoulderItemVariant;

	// Token: 0x0400067F RID: 1663
	private int m_currentLeftItemVariant;

	// Token: 0x04000680 RID: 1664
	private int m_currentLeftBackItemVariant;

	// Token: 0x04000681 RID: 1665
	private ItemDrop.ItemData.HelmetHairType m_helmetHideHair;

	// Token: 0x04000682 RID: 1666
	private ItemDrop.ItemData.HelmetHairType m_helmetHideBeard;

	// Token: 0x04000683 RID: 1667
	private Texture m_emptyBodyTexture;

	// Token: 0x04000684 RID: 1668
	private Texture m_emptyLegsTexture;

	// Token: 0x04000685 RID: 1669
	private int m_modelIndex;

	// Token: 0x04000686 RID: 1670
	private Vector3 m_skinColor = Vector3.one;

	// Token: 0x04000687 RID: 1671
	private Vector3 m_hairColor = Vector3.one;

	// Token: 0x04000688 RID: 1672
	private int m_currentModelIndex;

	// Token: 0x04000689 RID: 1673
	private ZNetView m_nview;

	// Token: 0x0400068A RID: 1674
	private GameObject m_visual;

	// Token: 0x0400068B RID: 1675
	private LODGroup m_lodGroup;

	// Token: 0x0200024C RID: 588
	[Serializable]
	public class PlayerModel
	{
		// Token: 0x04002002 RID: 8194
		public Mesh m_mesh;

		// Token: 0x04002003 RID: 8195
		public Material m_baseMaterial;
	}
}
