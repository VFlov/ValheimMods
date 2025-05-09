using System;
using UnityEngine;

// Token: 0x02000132 RID: 306
public class RenderGroupSubscriber : MonoBehaviour
{
	// Token: 0x170000A7 RID: 167
	// (get) Token: 0x06001373 RID: 4979 RVA: 0x000907C0 File Offset: 0x0008E9C0
	// (set) Token: 0x06001374 RID: 4980 RVA: 0x000907C8 File Offset: 0x0008E9C8
	public RenderGroup Group
	{
		get
		{
			return this.m_group;
		}
		set
		{
			this.Unregister();
			this.m_group = value;
			this.Register();
		}
	}

	// Token: 0x06001375 RID: 4981 RVA: 0x000907DD File Offset: 0x0008E9DD
	private void Start()
	{
		this.Register();
	}

	// Token: 0x06001376 RID: 4982 RVA: 0x000907E5 File Offset: 0x0008E9E5
	private void OnEnable()
	{
		this.Register();
	}

	// Token: 0x06001377 RID: 4983 RVA: 0x000907ED File Offset: 0x0008E9ED
	private void OnDisable()
	{
		this.Unregister();
	}

	// Token: 0x06001378 RID: 4984 RVA: 0x000907F8 File Offset: 0x0008E9F8
	private void Register()
	{
		if (this.m_isRegistered)
		{
			return;
		}
		if (this.m_renderer == null)
		{
			this.m_renderer = base.GetComponent<MeshRenderer>();
			if (this.m_renderer == null)
			{
				return;
			}
		}
		this.m_isRegistered = RenderGroupSystem.Register(this.m_group, new RenderGroupSystem.GroupChangedHandler(this.OnGroupChanged));
	}

	// Token: 0x06001379 RID: 4985 RVA: 0x00090854 File Offset: 0x0008EA54
	private void Unregister()
	{
		if (!this.m_isRegistered)
		{
			return;
		}
		RenderGroupSystem.Unregister(this.m_group, new RenderGroupSystem.GroupChangedHandler(this.OnGroupChanged));
		this.m_isRegistered = false;
	}

	// Token: 0x0600137A RID: 4986 RVA: 0x0009087E File Offset: 0x0008EA7E
	private void OnGroupChanged(bool shouldRender)
	{
		this.m_renderer.enabled = shouldRender;
	}

	// Token: 0x04001372 RID: 4978
	private MeshRenderer m_renderer;

	// Token: 0x04001373 RID: 4979
	[SerializeField]
	private RenderGroup m_group;

	// Token: 0x04001374 RID: 4980
	private bool m_isRegistered;
}
