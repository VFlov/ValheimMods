using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000AC RID: 172
public class CraftingStation : MonoBehaviour, Hoverable, Interactable, IMonoUpdater
{
	// Token: 0x06000AA6 RID: 2726 RVA: 0x0005B058 File Offset: 0x00059258
	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview && this.m_nview.GetZDO() == null)
		{
			return;
		}
		CraftingStation.m_allStations.Add(this);
		if (this.m_areaMarker)
		{
			this.m_areaMarker.SetActive(false);
			this.m_areaMarkerCircle = this.m_areaMarker.GetComponent<CircleProjector>();
		}
		if (this.m_craftRequireFire)
		{
			base.InvokeRepeating("CheckFire", 1f, 1f);
		}
		this.m_updateExtensionTimer = 2f;
	}

	// Token: 0x06000AA7 RID: 2727 RVA: 0x0005B0E9 File Offset: 0x000592E9
	protected virtual void OnEnable()
	{
		CraftingStation.Instances.Add(this);
	}

	// Token: 0x06000AA8 RID: 2728 RVA: 0x0005B0F6 File Offset: 0x000592F6
	protected virtual void OnDisable()
	{
		CraftingStation.Instances.Remove(this);
	}

	// Token: 0x06000AA9 RID: 2729 RVA: 0x0005B104 File Offset: 0x00059304
	private void OnDestroy()
	{
		CraftingStation.m_allStations.Remove(this);
	}

	// Token: 0x06000AAA RID: 2730 RVA: 0x0005B114 File Offset: 0x00059314
	public bool Interact(Humanoid user, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (user == Player.m_localPlayer)
		{
			if (!this.InUseDistance(user))
			{
				return false;
			}
			Player player = user as Player;
			if (this.CheckUsable(player, true))
			{
				player.SetCraftingStation(this);
				InventoryGui.instance.Show(null, 3);
				return false;
			}
		}
		return false;
	}

	// Token: 0x06000AAB RID: 2731 RVA: 0x0005B165 File Offset: 0x00059365
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06000AAC RID: 2732 RVA: 0x0005B168 File Offset: 0x00059368
	public bool CheckUsable(Player player, bool showMessage)
	{
		if (this.m_craftRequireRoof && !player.NoCostCheat())
		{
			float num;
			bool flag;
			Cover.GetCoverForPoint(this.m_roofCheckPoint.position, out num, out flag, 0.5f);
			if (!flag)
			{
				if (showMessage)
				{
					player.Message(MessageHud.MessageType.Center, "$msg_stationneedroof", 0, null);
				}
				return false;
			}
			if (num < 0.7f)
			{
				if (showMessage)
				{
					player.Message(MessageHud.MessageType.Center, "$msg_stationtooexposed", 0, null);
				}
				return false;
			}
		}
		if (this.m_craftRequireFire && !player.NoCostCheat() && !this.m_haveFire)
		{
			if (showMessage)
			{
				player.Message(MessageHud.MessageType.Center, "$msg_needfire", 0, null);
			}
			return false;
		}
		return true;
	}

	// Token: 0x06000AAD RID: 2733 RVA: 0x0005B1FB File Offset: 0x000593FB
	public string GetHoverText()
	{
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use ");
	}

	// Token: 0x06000AAE RID: 2734 RVA: 0x0005B234 File Offset: 0x00059434
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x06000AAF RID: 2735 RVA: 0x0005B23C File Offset: 0x0005943C
	public void ShowAreaMarker()
	{
		if (this.m_areaMarker)
		{
			this.m_areaMarker.SetActive(true);
			base.CancelInvoke("HideMarker");
			base.Invoke("HideMarker", 0.5f);
			this.PokeInUse();
		}
	}

	// Token: 0x06000AB0 RID: 2736 RVA: 0x0005B278 File Offset: 0x00059478
	private void HideMarker()
	{
		this.m_areaMarker.SetActive(false);
	}

	// Token: 0x06000AB1 RID: 2737 RVA: 0x0005B288 File Offset: 0x00059488
	public static void UpdateKnownStationsInRange(Player player)
	{
		Vector3 position = player.transform.position;
		foreach (CraftingStation craftingStation in CraftingStation.m_allStations)
		{
			if (Vector3.Distance(craftingStation.transform.position, position) < craftingStation.m_discoverRange)
			{
				player.AddKnownStation(craftingStation);
			}
		}
	}

	// Token: 0x06000AB2 RID: 2738 RVA: 0x0005B300 File Offset: 0x00059500
	public void CustomUpdate(float deltaTime, float time)
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		this.m_useTimer += deltaTime;
		this.m_updateExtensionTimer += deltaTime;
		if (this.m_inUseObject)
		{
			bool flag = this.m_useTimer < 1f;
			if (this.m_inUseObject.activeSelf != flag)
			{
				this.m_inUseObject.SetActive(flag);
			}
		}
	}

	// Token: 0x06000AB3 RID: 2739 RVA: 0x0005B37A File Offset: 0x0005957A
	private void CheckFire()
	{
		this.m_haveFire = EffectArea.IsPointPlus025InsideBurningArea(base.transform.position);
		if (this.m_haveFireObject)
		{
			this.m_haveFireObject.SetActive(this.m_haveFire);
		}
	}

	// Token: 0x06000AB4 RID: 2740 RVA: 0x0005B3B0 File Offset: 0x000595B0
	public void PokeInUse()
	{
		this.m_useTimer = 0f;
		this.TriggerExtensionEffects();
	}

	// Token: 0x06000AB5 RID: 2741 RVA: 0x0005B3C4 File Offset: 0x000595C4
	public static CraftingStation GetCraftingStation(Vector3 point)
	{
		if (CraftingStation.m_triggerMask == 0)
		{
			CraftingStation.m_triggerMask = LayerMask.GetMask(new string[]
			{
				"character_trigger"
			});
		}
		foreach (Collider collider in Physics.OverlapSphere(point, 0.1f, CraftingStation.m_triggerMask, QueryTriggerInteraction.Collide))
		{
			if (collider.gameObject.CompareTag("StationUseArea"))
			{
				CraftingStation componentInParent = collider.GetComponentInParent<CraftingStation>();
				if (componentInParent != null)
				{
					return componentInParent;
				}
			}
		}
		return null;
	}

	// Token: 0x06000AB6 RID: 2742 RVA: 0x0005B43C File Offset: 0x0005963C
	public static CraftingStation HaveBuildStationInRange(string name, Vector3 point)
	{
		foreach (CraftingStation craftingStation in CraftingStation.m_allStations)
		{
			if (!(craftingStation.m_name != name))
			{
				float stationBuildRange = craftingStation.GetStationBuildRange();
				point.y = craftingStation.transform.position.y;
				if (Vector3.Distance(craftingStation.transform.position, point) < stationBuildRange)
				{
					return craftingStation;
				}
			}
		}
		return null;
	}

	// Token: 0x06000AB7 RID: 2743 RVA: 0x0005B4D0 File Offset: 0x000596D0
	public static void FindStationsInRange(string name, Vector3 point, float range, List<CraftingStation> stations)
	{
		foreach (CraftingStation craftingStation in CraftingStation.m_allStations)
		{
			if (!(craftingStation.m_name != name) && Vector3.Distance(craftingStation.transform.position, point) < range)
			{
				stations.Add(craftingStation);
			}
		}
	}

	// Token: 0x06000AB8 RID: 2744 RVA: 0x0005B544 File Offset: 0x00059744
	public static CraftingStation FindClosestStationInRange(string name, Vector3 point, float range)
	{
		CraftingStation craftingStation = null;
		float num = 99999f;
		foreach (CraftingStation craftingStation2 in CraftingStation.m_allStations)
		{
			if (!(craftingStation2.m_name != name))
			{
				float num2 = Vector3.Distance(craftingStation2.transform.position, point);
				if (num2 < range && (num2 < num || craftingStation == null))
				{
					craftingStation = craftingStation2;
					num = num2;
				}
			}
		}
		return craftingStation;
	}

	// Token: 0x06000AB9 RID: 2745 RVA: 0x0005B5D4 File Offset: 0x000597D4
	private List<StationExtension> GetExtensions()
	{
		if (this.m_updateExtensionTimer >= 2f)
		{
			this.m_updateExtensionTimer = 0f;
			this.m_attachedExtensions.Clear();
			StationExtension.FindExtensions(this, base.transform.position, this.m_attachedExtensions);
			this.m_buildRange = this.m_rangeBuild + (float)this.GetExtentionCount(false) * this.m_extraRangePerLevel;
			if (this.m_areaMarker)
			{
				this.m_areaMarkerCircle.m_radius = this.m_buildRange;
			}
			if (this.m_effectAreaCollider == null)
			{
				return this.m_attachedExtensions;
			}
			Collider effectAreaCollider = this.m_effectAreaCollider;
			SphereCollider sphereCollider = effectAreaCollider as SphereCollider;
			if (sphereCollider == null)
			{
				CapsuleCollider capsuleCollider = effectAreaCollider as CapsuleCollider;
				if (capsuleCollider != null)
				{
					capsuleCollider.radius = this.m_buildRange;
				}
			}
			else
			{
				sphereCollider.radius = this.m_buildRange;
			}
		}
		return this.m_attachedExtensions;
	}

	// Token: 0x06000ABA RID: 2746 RVA: 0x0005B6AC File Offset: 0x000598AC
	private void TriggerExtensionEffects()
	{
		Vector3 connectionEffectPoint = this.GetConnectionEffectPoint();
		foreach (StationExtension stationExtension in this.GetExtensions())
		{
			if (stationExtension)
			{
				stationExtension.StartConnectionEffect(connectionEffectPoint, 1f);
			}
		}
	}

	// Token: 0x06000ABB RID: 2747 RVA: 0x0005B714 File Offset: 0x00059914
	public Vector3 GetConnectionEffectPoint()
	{
		if (this.m_connectionPoint)
		{
			return this.m_connectionPoint.position;
		}
		return base.transform.position;
	}

	// Token: 0x06000ABC RID: 2748 RVA: 0x0005B73A File Offset: 0x0005993A
	public int GetLevel(bool checkExtensions = true)
	{
		return 1 + this.GetExtentionCount(checkExtensions);
	}

	// Token: 0x06000ABD RID: 2749 RVA: 0x0005B745 File Offset: 0x00059945
	public int GetExtentionCount(bool checkExtensions = true)
	{
		if (checkExtensions)
		{
			this.GetExtensions();
		}
		return this.m_attachedExtensions.Count;
	}

	// Token: 0x06000ABE RID: 2750 RVA: 0x0005B75C File Offset: 0x0005995C
	public float GetStationBuildRange()
	{
		this.GetExtensions();
		return this.m_buildRange;
	}

	// Token: 0x06000ABF RID: 2751 RVA: 0x0005B76B File Offset: 0x0005996B
	public bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, base.transform.position) < this.m_useDistance;
	}

	// Token: 0x1700004F RID: 79
	// (get) Token: 0x06000AC0 RID: 2752 RVA: 0x0005B790 File Offset: 0x00059990
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04000C2B RID: 3115
	public string m_name = "";

	// Token: 0x04000C2C RID: 3116
	public Sprite m_icon;

	// Token: 0x04000C2D RID: 3117
	public float m_discoverRange = 4f;

	// Token: 0x04000C2E RID: 3118
	public float m_rangeBuild = 10f;

	// Token: 0x04000C2F RID: 3119
	public float m_extraRangePerLevel;

	// Token: 0x04000C30 RID: 3120
	public bool m_craftRequireRoof = true;

	// Token: 0x04000C31 RID: 3121
	public bool m_craftRequireFire = true;

	// Token: 0x04000C32 RID: 3122
	public Transform m_roofCheckPoint;

	// Token: 0x04000C33 RID: 3123
	public Transform m_connectionPoint;

	// Token: 0x04000C34 RID: 3124
	public bool m_showBasicRecipies;

	// Token: 0x04000C35 RID: 3125
	public float m_useDistance = 2f;

	// Token: 0x04000C36 RID: 3126
	public Collider m_effectAreaCollider;

	// Token: 0x04000C37 RID: 3127
	public int m_useAnimation;

	// Token: 0x04000C38 RID: 3128
	public Skills.SkillType m_craftingSkill = Skills.SkillType.Crafting;

	// Token: 0x04000C39 RID: 3129
	public GameObject m_areaMarker;

	// Token: 0x04000C3A RID: 3130
	public GameObject m_inUseObject;

	// Token: 0x04000C3B RID: 3131
	public GameObject m_haveFireObject;

	// Token: 0x04000C3C RID: 3132
	public EffectList m_craftItemEffects = new EffectList();

	// Token: 0x04000C3D RID: 3133
	public EffectList m_craftItemDoneEffects = new EffectList();

	// Token: 0x04000C3E RID: 3134
	public EffectList m_repairItemDoneEffects = new EffectList();

	// Token: 0x04000C3F RID: 3135
	private const float m_updateExtensionInterval = 2f;

	// Token: 0x04000C40 RID: 3136
	private float m_updateExtensionTimer;

	// Token: 0x04000C41 RID: 3137
	private bool m_initialized;

	// Token: 0x04000C42 RID: 3138
	private float m_useTimer = 10f;

	// Token: 0x04000C43 RID: 3139
	private bool m_haveFire;

	// Token: 0x04000C44 RID: 3140
	private float m_buildRange;

	// Token: 0x04000C45 RID: 3141
	private ZNetView m_nview;

	// Token: 0x04000C46 RID: 3142
	private List<StationExtension> m_attachedExtensions = new List<StationExtension>();

	// Token: 0x04000C47 RID: 3143
	private static List<CraftingStation> m_allStations = new List<CraftingStation>();

	// Token: 0x04000C48 RID: 3144
	private static int m_triggerMask = 0;

	// Token: 0x04000C49 RID: 3145
	private CircleProjector m_areaMarkerCircle;
}
