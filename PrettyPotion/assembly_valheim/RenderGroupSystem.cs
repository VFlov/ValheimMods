using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000134 RID: 308
public class RenderGroupSystem : MonoBehaviour
{
	// Token: 0x0600137C RID: 4988 RVA: 0x00090894 File Offset: 0x0008EA94
	private void Awake()
	{
		if (RenderGroupSystem.s_instance != null)
		{
			ZLog.LogError("Instance already set!");
			return;
		}
		RenderGroupSystem.s_instance = this;
		foreach (object obj in Enum.GetValues(typeof(RenderGroup)))
		{
			RenderGroup key = (RenderGroup)obj;
			this.m_renderGroups.Add(key, new RenderGroupSystem.RenderGroupState());
		}
	}

	// Token: 0x0600137D RID: 4989 RVA: 0x00090920 File Offset: 0x0008EB20
	private void OnDestroy()
	{
		if (RenderGroupSystem.s_instance == this)
		{
			RenderGroupSystem.s_instance = null;
		}
	}

	// Token: 0x0600137E RID: 4990 RVA: 0x00090938 File Offset: 0x0008EB38
	private void LateUpdate()
	{
		bool flag = Player.m_localPlayer != null && Player.m_localPlayer.InInterior();
		this.m_renderGroups[RenderGroup.Always].Active = true;
		this.m_renderGroups[RenderGroup.Overworld].Active = !flag;
		this.m_renderGroups[RenderGroup.Interior].Active = flag;
	}

	// Token: 0x0600137F RID: 4991 RVA: 0x0009099C File Offset: 0x0008EB9C
	public static bool Register(RenderGroup group, RenderGroupSystem.GroupChangedHandler subscriber)
	{
		if (!RenderGroupSystem.s_instance)
		{
			return false;
		}
		RenderGroupSystem.RenderGroupState renderGroupState = RenderGroupSystem.s_instance.m_renderGroups[group];
		renderGroupState.GroupChanged += subscriber;
		subscriber(renderGroupState.Active);
		return true;
	}

	// Token: 0x06001380 RID: 4992 RVA: 0x000909DC File Offset: 0x0008EBDC
	public static bool Unregister(RenderGroup group, RenderGroupSystem.GroupChangedHandler subscriber)
	{
		if (!RenderGroupSystem.s_instance)
		{
			return false;
		}
		RenderGroupSystem.s_instance.m_renderGroups[group].GroupChanged -= subscriber;
		return true;
	}

	// Token: 0x06001381 RID: 4993 RVA: 0x00090A03 File Offset: 0x0008EC03
	public static bool IsGroupActive(RenderGroup group)
	{
		return RenderGroupSystem.s_instance == null || RenderGroupSystem.s_instance.m_renderGroups[group].Active;
	}

	// Token: 0x04001379 RID: 4985
	private static RenderGroupSystem s_instance;

	// Token: 0x0400137A RID: 4986
	private Dictionary<RenderGroup, RenderGroupSystem.RenderGroupState> m_renderGroups = new Dictionary<RenderGroup, RenderGroupSystem.RenderGroupState>();

	// Token: 0x0200032A RID: 810
	// (Invoke) Token: 0x0600223E RID: 8766
	public delegate void GroupChangedHandler(bool shouldRender);

	// Token: 0x0200032B RID: 811
	private class RenderGroupState
	{
		// Token: 0x170001BE RID: 446
		// (get) Token: 0x06002241 RID: 8769 RVA: 0x000ECFE0 File Offset: 0x000EB1E0
		// (set) Token: 0x06002242 RID: 8770 RVA: 0x000ECFE8 File Offset: 0x000EB1E8
		public bool Active
		{
			get
			{
				return this.active;
			}
			set
			{
				if (this.active == value)
				{
					return;
				}
				this.active = value;
				RenderGroupSystem.GroupChangedHandler groupChanged = this.GroupChanged;
				if (groupChanged == null)
				{
					return;
				}
				groupChanged(this.active);
			}
		}

		// Token: 0x14000010 RID: 16
		// (add) Token: 0x06002243 RID: 8771 RVA: 0x000ED014 File Offset: 0x000EB214
		// (remove) Token: 0x06002244 RID: 8772 RVA: 0x000ED04C File Offset: 0x000EB24C
		public event RenderGroupSystem.GroupChangedHandler GroupChanged;

		// Token: 0x0400242D RID: 9261
		private bool active;
	}
}
