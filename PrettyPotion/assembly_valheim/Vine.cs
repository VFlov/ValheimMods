using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001D9 RID: 473
public class Vine : MonoBehaviour
{
	// Token: 0x06001B1A RID: 6938 RVA: 0x000CA378 File Offset: 0x000C8578
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_pickable = base.GetComponent<Pickable>();
		Pickable pickable = this.m_pickable;
		pickable.m_spawnCheck = (Pickable.SpawnCheck)Delegate.Combine(pickable.m_spawnCheck, new Pickable.SpawnCheck(this.CanSpawnPickable));
		this.GetRandom();
		Vine.s_allVines.Add(this);
		if (Vine.s_pieceMask == 0)
		{
			Vine.s_pieceMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece"
			});
		}
		if (Vine.s_solidMask == 0)
		{
			Vine.s_solidMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"piece_nonsolid",
				"terrain",
				"character",
				"character_net",
				"character_ghost",
				"hitbox",
				"character_noenv",
				"vehicle"
			});
		}
		base.InvokeRepeating("CheckSupport", (float)(10 + UnityEngine.Random.Range(0, 7)), 7f);
		base.InvokeRepeating("UpdateGrow", (float)UnityEngine.Random.Range(5, 10 + this.m_growCheckTime), (float)this.m_growCheckTime);
		this.IsDoneGrowing = this.IsDone();
		if (this.m_nview.IsOwner())
		{
			this.m_plantTime = this.m_nview.GetZDO().GetLong(ZDOVars.s_plantTime, 0L);
			if (this.m_plantTime == 0L)
			{
				long ticks = ZNet.instance.GetTime().Ticks;
				this.m_nview.GetZDO().Set(ZDOVars.s_plantTime, this.m_plantTime = ticks);
			}
			this.m_vineType = (Vine.VineType)this.m_nview.GetZDO().GetInt(ZDOVars.s_type, 0);
		}
		this.UpdateType();
		this.CheckBerryBlocker();
		string s;
		int initialGrowItterations;
		if (Terminal.m_testList.TryGetValue("quickvine", out s) && int.TryParse(s, out initialGrowItterations))
		{
			this.m_initialGrowItterations = initialGrowItterations;
		}
	}

	// Token: 0x06001B1B RID: 6939 RVA: 0x000CA584 File Offset: 0x000C8784
	private void GetRandom()
	{
		if (this.m_rnd == null)
		{
			this.m_rnd = new System.Random((this.m_forceSeed != 0) ? (this.m_forceSeed + this.GetOriginOffset().GetHashCode()) : UnityEngine.Random.Range(int.MinValue, int.MaxValue));
		}
	}

	// Token: 0x06001B1C RID: 6940 RVA: 0x000CA5D8 File Offset: 0x000C87D8
	public void Update()
	{
		if (this.m_initialGrowItterations > 0 && this.m_nview && this.m_nview.IsOwner())
		{
			for (int i = 0; i < Mathf.Min(this.m_initialGrowItterations, 25); i++)
			{
				if (this.UpdateGrow())
				{
					this.m_initialGrowItterations--;
					break;
				}
				this.m_initialGrowItterations--;
			}
		}
		bool flag = this.m_pickable.CanBePicked();
		if (flag != this.m_lastPickable)
		{
			this.BerryBlockNeighbours();
			this.m_lastPickable = flag;
		}
	}

	// Token: 0x06001B1D RID: 6941 RVA: 0x000CA668 File Offset: 0x000C8868
	public bool UpdateGrow()
	{
		if (!this.m_nview || !this.m_nview.IsOwner())
		{
			return false;
		}
		if (!this.m_dupeCheck)
		{
			this.m_dupeCheck = true;
			foreach (Vine vine in Vine.s_allVines)
			{
				if (vine != this && vine && !this.m_pickable.CanBePicked() && Vector3.Distance(vine.transform.position, base.transform.position) < 0.01f)
				{
					ZNetScene.instance.Destroy(base.gameObject);
					return false;
				}
			}
		}
		if ((this.IsDoneGrowing || this.IsDone()) && this.m_initialGrowItterations > 0)
		{
			this.m_initialGrowItterations = 0;
		}
		if (this.m_initialGrowItterations <= 0)
		{
			float num = (float)(ZNet.instance ? ZNet.instance.GetTime().Ticks : 0L);
			long num2;
			if (this.m_nview)
			{
				ZDO zdo = this.m_nview.GetZDO();
				if (zdo != null)
				{
					long @long = zdo.GetLong(ZDOVars.s_growStart, 0L);
					num2 = @long;
					goto IL_139;
				}
			}
			num2 = 0L;
			IL_139:
			long num3 = num2;
			int num4 = (int)((float)(((long)num - ((this.m_plantTime > num3) ? this.m_plantTime : num3)) / 10000000L) / (this.m_growTime + this.m_growTimePerBranch * (float)this.GetBranches()));
			if (num4 < 1)
			{
				return false;
			}
			if (num4 >= 2 && num4 > this.m_initialGrowItterations)
			{
				this.m_initialGrowItterations = num4 - 1;
				if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("vine"))
				{
					Terminal.Log(string.Format("Vine is queuing {0} itterations to catch up!", this.m_initialGrowItterations));
				}
			}
		}
		if (this.m_nview && this.m_nview.IsOwner() && this.m_nview.IsValid())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_growStart, ZNet.instance.GetTime().Ticks);
		}
		this.m_originOffset = this.GetOriginOffset();
		int num5 = 0;
		bool flag = this.m_rnd.NextDouble() < (double)this.m_maxGrowEdgeIgnoreChance;
		if (this.m_growSides)
		{
			float num6 = this.m_maxGrowWidth + this.m_originOffset.y * this.m_extraGrowWidthPerHeight;
			if (flag || this.m_originOffset.z + 1f <= num6)
			{
				num5 += this.CheckGrowChance(new Vector3(0f, 0f, 1f), Vine.VineType.Left, Vine.VineState.ClosedLeft, Vine.VineState.BranchLeft, Vine.VineState.BranchRight);
			}
			else
			{
				this.m_vineState |= Vine.VineState.ClosedLeft;
			}
			if (flag || Mathf.Abs(this.m_originOffset.z) + 1f <= num6)
			{
				num5 += this.CheckGrowChance(new Vector3(0f, 0f, -1f), Vine.VineType.Right, Vine.VineState.ClosedRight, Vine.VineState.BranchRight, Vine.VineState.BranchLeft);
			}
			else
			{
				this.m_vineState |= Vine.VineState.ClosedRight;
			}
		}
		if (this.m_growUp)
		{
			if (flag || this.m_originOffset.y + 1f <= this.m_maxGrowUp)
			{
				num5 += this.CheckGrowChance(new Vector3(0f, 1f, 0f), Vine.VineType.Top, Vine.VineState.ClosedTop, Vine.VineState.BranchTop, Vine.VineState.BranchBottom);
			}
			else
			{
				this.m_vineState |= Vine.VineState.ClosedTop;
			}
		}
		if (this.m_growDown)
		{
			if (flag || this.m_originOffset.y - 1f >= this.m_maxGrowDown)
			{
				num5 += this.CheckGrowChance(new Vector3(0f, -1f, 0f), Vine.VineType.Bottom, Vine.VineState.ClosedBottom, Vine.VineState.BranchBottom, Vine.VineState.BranchTop);
			}
			else
			{
				this.m_vineState |= Vine.VineState.ClosedBottom;
			}
		}
		this.IsDoneGrowing = this.IsDone();
		return num5 > 0;
	}

	// Token: 0x06001B1E RID: 6942 RVA: 0x000CAA44 File Offset: 0x000C8C44
	private int CheckGrowChance(Vector3 offset, Vine.VineType type, Vine.VineState closed, Vine.VineState branch, Vine.VineState branchFrom)
	{
		if (this.m_rnd.NextDouble() < (double)(this.m_growCheckChance + this.m_growCheckChancePerBranch * (float)this.GetBranches()))
		{
			return this.CheckGrow(offset, type, closed, branch, branchFrom);
		}
		return 0;
	}

	// Token: 0x06001B1F RID: 6943 RVA: 0x000CAA78 File Offset: 0x000C8C78
	private int CheckGrow(Vector3 offset, Vine.VineType type, Vine.VineState closed, Vine.VineState branch, Vine.VineState branchFrom)
	{
		if (this.m_vineState.HasFlag(branch))
		{
			return 0;
		}
		if (this.m_vineState.HasFlag(closed) && this.m_rnd.NextDouble() > 0.4000000059604645)
		{
			return 0;
		}
		if (this.m_rnd.NextDouble() < (double)Mathf.Min(this.m_maxCloseEndChance, this.m_closeEndChance + this.m_closeEndChancePerBranch * (float)this.GetBranches() + this.m_closeEndChancePerHeight * this.GetOriginOffset().y))
		{
			if (this.GetBranches() > 1)
			{
				this.m_vineState |= closed;
			}
			return 0;
		}
		Vector3 originOffset = offset;
		if (this.m_randomOffset != 0f)
		{
			if (type - Vine.VineType.Left > 1)
			{
				if (type - Vine.VineType.Top <= 1)
				{
					offset.z += UnityEngine.Random.Range(-this.m_randomOffset, this.m_randomOffset);
				}
			}
			else
			{
				offset.y += UnityEngine.Random.Range(-this.m_randomOffset, this.m_randomOffset);
			}
		}
		this.m_sensorBlockCollider.transform.localPosition = Vector3.zero;
		this.m_sensorBlockCollider.transform.Translate(offset * this.m_size + this.m_sensorBlockCollider.center, base.transform);
		int num = Physics.OverlapBoxNonAlloc(this.m_sensorBlockCollider.transform.position, this.m_sensorBlockCollider.size / 2f, Vine.s_colliders, this.m_sensorBlockCollider.transform.rotation, Vine.s_solidMask);
		int i = 0;
		while (i < num)
		{
			Vine componentInParent = Vine.s_colliders[i].GetComponentInParent<Vine>();
			if (!(componentInParent == this))
			{
				if (componentInParent != null)
				{
					if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("vine"))
					{
						Terminal.Log("Blocked by vine, count it as a branch (green box)");
					}
					this.m_vineState |= branch;
					this.UpdateBranches();
					return 0;
				}
				if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("vine"))
				{
					Terminal.Log("Blocked by piece (red box)");
				}
				this.m_vineState |= closed;
				this.UpdateBranches();
				return 0;
			}
			else
			{
				i++;
			}
		}
		this.m_sensorGrow.transform.localPosition = Vector3.zero;
		this.m_sensorGrow.transform.Translate(offset * this.m_size + this.m_sensorGrow.center, base.transform);
		int num2 = 0;
		foreach (BoxCollider boxCollider in this.m_sensorGrowColliders)
		{
			num = Physics.OverlapBoxNonAlloc(this.m_sensorGrow.transform.position, boxCollider.size / 2f, Vine.s_colliders, this.m_sensorGrow.transform.rotation, Vine.s_pieceMask);
			if (num > 0)
			{
				num2++;
			}
		}
		if ((float)num2 >= (float)this.m_sensorGrowColliders.Count * this.m_growCollidersMinimum && this.m_rnd.NextDouble() < (double)this.m_growChance)
		{
			this.Grow(offset, originOffset, type, branch, branchFrom);
			return 1;
		}
		return 0;
	}

	// Token: 0x06001B20 RID: 6944 RVA: 0x000CADC4 File Offset: 0x000C8FC4
	private void CheckSupport()
	{
		if (!this.IsSupported())
		{
			Destructible component = base.GetComponent<Destructible>();
			if (component != null)
			{
				component.m_destroyedEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			}
			else
			{
				WearNTear component2 = base.GetComponent<WearNTear>();
				if (component2 != null)
				{
					component2.m_destroyedEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				}
			}
			ZNetScene.instance.Destroy(base.gameObject);
		}
	}

	// Token: 0x06001B21 RID: 6945 RVA: 0x000CAE54 File Offset: 0x000C9054
	private bool IsSupported()
	{
		return Physics.OverlapBoxNonAlloc(base.transform.TransformPoint(this.m_supportCollider.center), this.m_supportCollider.size / 2f, Vine.s_colliders, this.m_supportCollider.transform.rotation, Vine.s_pieceMask) > 0;
	}

	// Token: 0x06001B22 RID: 6946 RVA: 0x000CAEAE File Offset: 0x000C90AE
	private int GetBranches()
	{
		return Mathf.Max(0, this.m_branches - 1);
	}

	// Token: 0x06001B23 RID: 6947 RVA: 0x000CAEC0 File Offset: 0x000C90C0
	private void Grow(Vector3 offset, Vector3 originOffset, Vine.VineType type, Vine.VineState branch, Vine.VineState growingFrom)
	{
		if (this.m_vineType != Vine.VineType.Full)
		{
			this.SetType(Vine.VineType.Full);
		}
		Vine component = UnityEngine.Object.Instantiate<GameObject>(this.m_vinePrefab, base.transform.position, base.transform.rotation).GetComponent<Vine>();
		component.transform.Translate(offset * this.m_size, Space.Self);
		component.SetType(type);
		component.name = component.name.Substring(0, Mathf.Min(component.name.Length, 15));
		component.m_vineState |= growingFrom;
		component.m_initialGrowItterations = this.m_initialGrowItterations - 1;
		component.m_pickable.m_respawnTimeInitMin += (float)this.m_initialGrowItterations;
		component.m_pickable.m_respawnTimeInitMax += (float)this.m_initialGrowItterations;
		this.m_originOffset = this.GetOriginOffset();
		component.SetOriginOffset(this.m_originOffset + originOffset);
		if (this.m_maxScale != 1f || this.m_minScale != 1f)
		{
			float num = this.m_minScale + (float)(this.m_rnd.NextDouble() * (double)(this.m_maxScale - this.m_minScale));
			component.transform.localScale = new Vector3(num, num, num);
		}
		if (component.m_nview && component.m_nview.IsOwner() && component.m_nview.IsValid())
		{
			component.m_nview.GetZDO().Set(ZDOVars.s_plantTime, ZNet.instance.GetTime().Ticks);
		}
	}

	// Token: 0x06001B24 RID: 6948 RVA: 0x000CB054 File Offset: 0x000C9254
	private void SetType(Vine.VineType type)
	{
		this.m_vineType = type;
		if (this.m_nview && this.m_nview.IsOwner() && this.m_nview.IsValid())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_type, (int)type, false);
		}
		this.UpdateType();
	}

	// Token: 0x06001B25 RID: 6949 RVA: 0x000CB0AC File Offset: 0x000C92AC
	public void SetOriginOffset(Vector3 offset)
	{
		this.m_originOffset = offset;
		if (this.m_nview && this.m_nview.IsValid())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_offset, this.m_originOffset);
		}
	}

	// Token: 0x06001B26 RID: 6950 RVA: 0x000CB0EC File Offset: 0x000C92EC
	public Vector3 GetOriginOffset()
	{
		if (this.m_originOffset == Vector3.zero && this.m_nview && this.m_nview.IsValid())
		{
			this.m_originOffset = this.m_nview.GetZDO().GetVec3(ZDOVars.s_offset, Vector3.zero);
		}
		return this.m_originOffset;
	}

	// Token: 0x06001B27 RID: 6951 RVA: 0x000CB14C File Offset: 0x000C934C
	private void UpdateType()
	{
		this.m_vineFull.SetActive(this.m_vineType == Vine.VineType.Full);
		this.m_vineLeft.SetActive(this.m_vineType == Vine.VineType.Left);
		this.m_vineRight.SetActive(this.m_vineType == Vine.VineType.Right);
		this.m_vineTop.SetActive(this.m_vineType == Vine.VineType.Top);
		this.m_vineBottom.SetActive(this.m_vineType == Vine.VineType.Bottom);
	}

	// Token: 0x06001B28 RID: 6952 RVA: 0x000CB1C0 File Offset: 0x000C93C0
	private void UpdateBranches()
	{
		this.m_branches = 0;
		if (this.m_vineState.HasFlag(Vine.VineState.BranchLeft))
		{
			this.m_branches++;
		}
		if (this.m_vineState.HasFlag(Vine.VineState.BranchRight))
		{
			this.m_branches++;
		}
		if (this.m_vineState.HasFlag(Vine.VineState.BranchTop))
		{
			this.m_branches++;
		}
		if (this.m_vineState.HasFlag(Vine.VineState.BranchBottom))
		{
			this.m_branches++;
		}
	}

	// Token: 0x06001B29 RID: 6953 RVA: 0x000CB274 File Offset: 0x000C9474
	private void BerryBlockNeighbours()
	{
		if (!this.m_nview || !this.m_nview.IsOwner())
		{
			return;
		}
		if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("vine"))
		{
			Terminal.Log("Vine pickable changed, blocking neighbors");
		}
		int num = Physics.OverlapBoxNonAlloc(base.transform.TransformPoint(this.m_berryBlocker.center), this.m_berryBlocker.size / 2f, Vine.s_colliders, this.m_berryBlocker.transform.rotation, Vine.s_solidMask);
		Vine.s_vines.Clear();
		for (int i = 0; i < num; i++)
		{
			Vine componentInParent = Vine.s_colliders[i].GetComponentInParent<Vine>();
			if (componentInParent != null)
			{
				Vine.s_vines.Add(componentInParent);
			}
		}
		foreach (Vine vine in Vine.s_vines)
		{
			vine.CheckBerryBlocker();
		}
	}

	// Token: 0x06001B2A RID: 6954 RVA: 0x000CB380 File Offset: 0x000C9580
	private bool CanSpawnPickable(Pickable p)
	{
		return this.CheckBerryBlocker();
	}

	// Token: 0x06001B2B RID: 6955 RVA: 0x000CB388 File Offset: 0x000C9588
	private bool CheckBerryBlocker()
	{
		if (!this.m_nview || !this.m_nview.IsOwner())
		{
			return true;
		}
		if (this.m_pickable.CanBePicked())
		{
			return true;
		}
		int num = Physics.OverlapBoxNonAlloc(base.transform.TransformPoint(this.m_berryBlocker.center), this.m_berryBlocker.size / 2f, Vine.s_colliders, this.m_berryBlocker.transform.rotation, Vine.s_solidMask);
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			Vine componentInParent = Vine.s_colliders[i].GetComponentInParent<Vine>();
			if (componentInParent != null && componentInParent.m_pickable.CanBePicked())
			{
				num2++;
			}
		}
		this.UpdateBranches();
		bool flag = num2 < this.m_maxBerriesWithinBlocker && this.GetBranches() > 0;
		if (!flag)
		{
			this.m_pickable.SetPicked(true);
		}
		if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("vine"))
		{
			Terminal.Log(string.Format("Vine checking berry blockers. Berries: {0}, Pickable: {1}", num2, flag));
		}
		return flag;
	}

	// Token: 0x06001B2C RID: 6956 RVA: 0x000CB49C File Offset: 0x000C969C
	private bool IsDone()
	{
		return (this.m_vineState.HasFlag(Vine.VineState.ClosedLeft) || this.m_vineState.HasFlag(Vine.VineState.BranchLeft)) && (this.m_vineState.HasFlag(Vine.VineState.ClosedRight) || this.m_vineState.HasFlag(Vine.VineState.BranchRight)) && (this.m_vineState.HasFlag(Vine.VineState.ClosedTop) || this.m_vineState.HasFlag(Vine.VineState.BranchTop));
	}

	// Token: 0x06001B2D RID: 6957 RVA: 0x000CB53E File Offset: 0x000C973E
	private void OnDestroy()
	{
		Vine.s_allVines.Remove(this);
	}

	// Token: 0x170000CB RID: 203
	// (get) Token: 0x06001B2E RID: 6958 RVA: 0x000CB54C File Offset: 0x000C974C
	// (set) Token: 0x06001B2F RID: 6959 RVA: 0x000CB554 File Offset: 0x000C9754
	public bool IsDoneGrowing { get; private set; }

	// Token: 0x04001BBE RID: 7102
	private static int s_pieceMask;

	// Token: 0x04001BBF RID: 7103
	private static int s_solidMask;

	// Token: 0x04001BC0 RID: 7104
	[Header("Grow Settings")]
	[global::Tooltip("Chance that a grow check is run on each open branch, which can result in a chance to grow or chance to close.")]
	[Range(0f, 1f)]
	public float m_growCheckChance = 0.3f;

	// Token: 0x04001BC1 RID: 7105
	[global::Tooltip("Grow check decreases by this amount for each near branch.")]
	[Range(-1f, 0f)]
	public float m_growCheckChancePerBranch = -0.75f;

	// Token: 0x04001BC2 RID: 7106
	[global::Tooltip("Chance that a possible branch will actually grow")]
	[Range(0f, 1f)]
	public float m_growChance = 0.5f;

	// Token: 0x04001BC3 RID: 7107
	[global::Tooltip("At what interval the GrowCheck function will repeat.")]
	public int m_growCheckTime = 5;

	// Token: 0x04001BC4 RID: 7108
	[global::Tooltip("Seconds it will take between each attempt to grow.")]
	public float m_growTime = 3f;

	// Token: 0x04001BC5 RID: 7109
	[global::Tooltip("Extra seconds it will take between each attempt to grow for each branch connected to it.")]
	public float m_growTimePerBranch = 2f;

	// Token: 0x04001BC6 RID: 7110
	[global::Tooltip("Chance that a branch will close after during a grow check.")]
	[Range(0f, 1f)]
	public float m_closeEndChance;

	// Token: 0x04001BC7 RID: 7111
	[global::Tooltip("Close chance increases by this amount for each near branch.")]
	[Range(0f, 1f)]
	public float m_closeEndChancePerBranch = 0.1f;

	// Token: 0x04001BC8 RID: 7112
	[global::Tooltip("Close chance increases by this amount for each height.")]
	[Range(0f, 1f)]
	public float m_closeEndChancePerHeight = 0.2f;

	// Token: 0x04001BC9 RID: 7113
	[global::Tooltip("Close chance will never go above this. (Also will never be closed unless there is atleast one grown branch.")]
	[Range(0f, 1f)]
	public float m_maxCloseEndChance = 0.9f;

	// Token: 0x04001BCA RID: 7114
	public float m_maxGrowUp = 1000f;

	// Token: 0x04001BCB RID: 7115
	public float m_maxGrowDown = -2f;

	// Token: 0x04001BCC RID: 7116
	[global::Tooltip("Grow width limitation")]
	public float m_maxGrowWidth = 3f;

	// Token: 0x04001BCD RID: 7117
	[global::Tooltip("Extra grow width limitation per height")]
	public float m_extraGrowWidthPerHeight = 0.5f;

	// Token: 0x04001BCE RID: 7118
	[global::Tooltip("Chance to ignore width limitaion.")]
	[Range(0f, 1f)]
	public float m_maxGrowEdgeIgnoreChance = 0.2f;

	// Token: 0x04001BCF RID: 7119
	[global::Tooltip("Vine will grow this many itterations upon placement. Used for locations for instant growing. Test with console 'test quickvine 300'")]
	public int m_initialGrowItterations;

	// Token: 0x04001BD0 RID: 7120
	public int m_forceSeed;

	// Token: 0x04001BD1 RID: 7121
	public float m_size = 1.5f;

	// Token: 0x04001BD2 RID: 7122
	[global::Tooltip("At least this much % of the colliders must find support to be able to grow.")]
	[Range(0f, 1f)]
	public float m_growCollidersMinimum = 0.75f;

	// Token: 0x04001BD3 RID: 7123
	public bool m_growSides = true;

	// Token: 0x04001BD4 RID: 7124
	public bool m_growUp = true;

	// Token: 0x04001BD5 RID: 7125
	public bool m_growDown;

	// Token: 0x04001BD6 RID: 7126
	public float m_minScale = 1f;

	// Token: 0x04001BD7 RID: 7127
	public float m_maxScale = 1f;

	// Token: 0x04001BD8 RID: 7128
	public float m_randomOffset = 0.1f;

	// Token: 0x04001BD9 RID: 7129
	[Header("Berries")]
	public int m_maxBerriesWithinBlocker = 2;

	// Token: 0x04001BDA RID: 7130
	public BoxCollider m_berryBlocker;

	// Token: 0x04001BDB RID: 7131
	[Header("Prefabs")]
	public GameObject m_vinePrefab;

	// Token: 0x04001BDC RID: 7132
	public GameObject m_vineFull;

	// Token: 0x04001BDD RID: 7133
	public GameObject m_vineTop;

	// Token: 0x04001BDE RID: 7134
	public GameObject m_vineBottom;

	// Token: 0x04001BDF RID: 7135
	public GameObject m_vineLeft;

	// Token: 0x04001BE0 RID: 7136
	public GameObject m_vineRight;

	// Token: 0x04001BE1 RID: 7137
	public BoxCollider m_sensorGrow;

	// Token: 0x04001BE2 RID: 7138
	public BoxCollider m_supportCollider;

	// Token: 0x04001BE3 RID: 7139
	public List<BoxCollider> m_sensorGrowColliders;

	// Token: 0x04001BE4 RID: 7140
	public GameObject m_sensorBlock;

	// Token: 0x04001BE5 RID: 7141
	public BoxCollider m_sensorBlockCollider;

	// Token: 0x04001BE6 RID: 7142
	public BoxCollider m_placementCollider;

	// Token: 0x04001BE7 RID: 7143
	[Header("Testing")]
	public int m_testItterations = 10;

	// Token: 0x04001BE9 RID: 7145
	[NonSerialized]
	public Vine.VineState m_vineState;

	// Token: 0x04001BEA RID: 7146
	private Pickable m_pickable;

	// Token: 0x04001BEB RID: 7147
	private bool m_lastPickable;

	// Token: 0x04001BEC RID: 7148
	private long m_plantTime;

	// Token: 0x04001BED RID: 7149
	private long m_lastGrow;

	// Token: 0x04001BEE RID: 7150
	private ZNetView m_nview;

	// Token: 0x04001BEF RID: 7151
	private Vine.VineType m_vineType;

	// Token: 0x04001BF0 RID: 7152
	private int m_branches;

	// Token: 0x04001BF1 RID: 7153
	private Vector3 m_originOffset;

	// Token: 0x04001BF2 RID: 7154
	private System.Random m_rnd;

	// Token: 0x04001BF3 RID: 7155
	private bool m_dupeCheck;

	// Token: 0x04001BF4 RID: 7156
	private static Collider[] s_colliders = new Collider[20];

	// Token: 0x04001BF5 RID: 7157
	private static List<Vine> s_vines = new List<Vine>();

	// Token: 0x04001BF6 RID: 7158
	private static List<Vine> s_allVines = new List<Vine>();

	// Token: 0x02000397 RID: 919
	[Flags]
	public enum VineState
	{
		// Token: 0x040026C4 RID: 9924
		None = 0,
		// Token: 0x040026C5 RID: 9925
		ClosedLeft = 1,
		// Token: 0x040026C6 RID: 9926
		ClosedRight = 2,
		// Token: 0x040026C7 RID: 9927
		ClosedTop = 4,
		// Token: 0x040026C8 RID: 9928
		ClosedBottom = 8,
		// Token: 0x040026C9 RID: 9929
		BranchLeft = 16,
		// Token: 0x040026CA RID: 9930
		BranchRight = 32,
		// Token: 0x040026CB RID: 9931
		BranchTop = 64,
		// Token: 0x040026CC RID: 9932
		BranchBottom = 128
	}

	// Token: 0x02000398 RID: 920
	private enum VineType
	{
		// Token: 0x040026CE RID: 9934
		Full,
		// Token: 0x040026CF RID: 9935
		Left,
		// Token: 0x040026D0 RID: 9936
		Right,
		// Token: 0x040026D1 RID: 9937
		Top,
		// Token: 0x040026D2 RID: 9938
		Bottom
	}
}
