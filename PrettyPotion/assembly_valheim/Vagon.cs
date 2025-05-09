using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001D6 RID: 470
public class Vagon : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06001AF5 RID: 6901 RVA: 0x000C9260 File Offset: 0x000C7460
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		Vagon.m_instances.Add(this);
		Heightmap.ForceGenerateAll();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_bodies = base.GetComponentsInChildren<Rigidbody>();
		this.m_lineRenderer = base.GetComponent<LineRenderer>();
		Rigidbody[] bodies = this.m_bodies;
		for (int i = 0; i < bodies.Length; i++)
		{
			bodies[i].maxDepenetrationVelocity = 2f;
		}
		this.m_nview.Register("RPC_RequestOwn", new Action<long>(this.RPC_RequestOwn));
		this.m_nview.Register("RPC_RequestDenied", new Action<long>(this.RPC_RequestDenied));
		base.InvokeRepeating("UpdateMass", 0f, 5f);
		base.InvokeRepeating("UpdateLoadVisualization", 0f, 3f);
	}

	// Token: 0x06001AF6 RID: 6902 RVA: 0x000C9346 File Offset: 0x000C7546
	private void OnDestroy()
	{
		Vagon.m_instances.Remove(this);
	}

	// Token: 0x06001AF7 RID: 6903 RVA: 0x000C9354 File Offset: 0x000C7554
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x06001AF8 RID: 6904 RVA: 0x000C935C File Offset: 0x000C755C
	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
	}

	// Token: 0x06001AF9 RID: 6905 RVA: 0x000C9378 File Offset: 0x000C7578
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		this.m_useRequester = character;
		if (!this.m_nview.IsOwner())
		{
			this.m_nview.InvokeRPC("RPC_RequestOwn", Array.Empty<object>());
		}
		return false;
	}

	// Token: 0x06001AFA RID: 6906 RVA: 0x000C93AC File Offset: 0x000C75AC
	public void RPC_RequestOwn(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.InUse())
		{
			ZLog.Log("Requested use, but is already in use");
			this.m_nview.InvokeRPC(sender, "RPC_RequestDenied", Array.Empty<object>());
			return;
		}
		this.m_nview.GetZDO().SetOwner(sender);
	}

	// Token: 0x06001AFB RID: 6907 RVA: 0x000C9401 File Offset: 0x000C7601
	private void RPC_RequestDenied(long sender)
	{
		ZLog.Log("Got request denied");
		if (this.m_useRequester)
		{
			this.m_useRequester.Message(MessageHud.MessageType.Center, this.m_name + " is in use by someone else", 0, null);
			this.m_useRequester = null;
		}
	}

	// Token: 0x06001AFC RID: 6908 RVA: 0x000C9440 File Offset: 0x000C7640
	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateAudio(Time.fixedDeltaTime);
		if (this.m_nview.IsOwner())
		{
			if (this.m_useRequester)
			{
				if (this.IsAttached())
				{
					this.Detach();
				}
				else if (this.CanAttach(this.m_useRequester.gameObject))
				{
					this.AttachTo(this.m_useRequester.gameObject);
				}
				else
				{
					this.m_useRequester.Message(MessageHud.MessageType.Center, "$msg_cart_incorrectposition", 0, null);
				}
				this.m_useRequester = null;
			}
			if (this.IsAttached() && (!this.m_attachJoin || !this.CanAttach(this.m_attachJoin.connectedBody.gameObject)))
			{
				this.Detach();
				return;
			}
		}
		else if (this.IsAttached())
		{
			this.Detach();
		}
	}

	// Token: 0x06001AFD RID: 6909 RVA: 0x000C9518 File Offset: 0x000C7718
	private void LateUpdate()
	{
		if (this.IsAttached() && this.m_attachJoin != null)
		{
			this.m_lineRenderer.enabled = true;
			this.m_lineRenderer.SetPosition(0, this.m_lineAttachPoints0.position);
			this.m_lineRenderer.SetPosition(1, this.m_attachJoin.connectedBody.transform.position + this.m_lineAttachOffset);
			this.m_lineRenderer.SetPosition(2, this.m_lineAttachPoints1.position);
			return;
		}
		this.m_lineRenderer.enabled = false;
	}

	// Token: 0x06001AFE RID: 6910 RVA: 0x000C95AE File Offset: 0x000C77AE
	public bool IsAttached(Character character)
	{
		return this.m_attachJoin && this.m_attachJoin.connectedBody.gameObject == character.gameObject;
	}

	// Token: 0x06001AFF RID: 6911 RVA: 0x000C95DD File Offset: 0x000C77DD
	public bool InUse()
	{
		return (this.m_container && this.m_container.IsInUse()) || this.IsAttached();
	}

	// Token: 0x06001B00 RID: 6912 RVA: 0x000C9601 File Offset: 0x000C7801
	public bool IsAttached()
	{
		return this.m_attachJoin != null || (this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool(ZDOVars.s_attachJointHash, false));
	}

	// Token: 0x06001B01 RID: 6913 RVA: 0x000C9638 File Offset: 0x000C7838
	private bool CanAttach(GameObject go)
	{
		if (base.transform.up.y < 0.1f)
		{
			return false;
		}
		Humanoid component = go.GetComponent<Humanoid>();
		return (!component || (!component.InDodge() && !component.IsTeleporting())) && Vector3.Distance(go.transform.position + this.m_attachOffset, this.m_attachPoint.position) < this.m_detachDistance;
	}

	// Token: 0x06001B02 RID: 6914 RVA: 0x000C96B0 File Offset: 0x000C78B0
	private void AttachTo(GameObject go)
	{
		Vagon.DetachAll();
		this.m_attachJoin = base.gameObject.AddComponent<ConfigurableJoint>();
		this.m_attachJoin.autoConfigureConnectedAnchor = false;
		this.m_attachJoin.anchor = this.m_attachPoint.localPosition;
		this.m_attachJoin.connectedAnchor = this.m_attachOffset;
		this.m_attachJoin.breakForce = this.m_breakForce;
		this.m_attachJoin.xMotion = ConfigurableJointMotion.Limited;
		this.m_attachJoin.yMotion = ConfigurableJointMotion.Limited;
		this.m_attachJoin.zMotion = ConfigurableJointMotion.Limited;
		SoftJointLimit linearLimit = default(SoftJointLimit);
		linearLimit.limit = 0.001f;
		this.m_attachJoin.linearLimit = linearLimit;
		SoftJointLimitSpring linearLimitSpring = default(SoftJointLimitSpring);
		linearLimitSpring.spring = this.m_spring;
		linearLimitSpring.damper = this.m_springDamping;
		this.m_attachJoin.linearLimitSpring = linearLimitSpring;
		this.m_attachJoin.zMotion = ConfigurableJointMotion.Locked;
		this.m_attachJoin.connectedBody = go.GetComponent<Rigidbody>();
		this.m_attachedObject = go;
		if (this.m_nview.IsValid())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_attachJointHash, true);
		}
		if (this.m_playerExtraPullMass != 0f)
		{
			Character component = this.m_attachedObject.GetComponent<Character>();
			if (component != null)
			{
				component.SetExtraMass(this.m_playerExtraPullMass);
			}
		}
	}

	// Token: 0x06001B03 RID: 6915 RVA: 0x000C97F8 File Offset: 0x000C79F8
	private static void DetachAll()
	{
		foreach (Vagon vagon in Vagon.m_instances)
		{
			vagon.Detach();
		}
	}

	// Token: 0x06001B04 RID: 6916 RVA: 0x000C9848 File Offset: 0x000C7A48
	private void Detach()
	{
		if (this.m_attachJoin)
		{
			UnityEngine.Object.Destroy(this.m_attachJoin);
		}
		this.m_attachJoin = null;
		if (this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_attachJointHash, false);
		}
		this.m_body.WakeUp();
		this.m_body.AddForce(0f, 1f, 0f);
		if (this.m_playerExtraPullMass != 0f && this.m_attachedObject)
		{
			Character component = this.m_attachedObject.GetComponent<Character>();
			if (component != null)
			{
				component.SetExtraMass(0f);
			}
		}
		this.m_attachedObject = null;
	}

	// Token: 0x06001B05 RID: 6917 RVA: 0x000C9904 File Offset: 0x000C7B04
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06001B06 RID: 6918 RVA: 0x000C9908 File Offset: 0x000C7B08
	private void UpdateMass()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_container == null)
		{
			return;
		}
		float totalWeight = this.m_container.GetInventory().GetTotalWeight();
		float mass = this.m_baseMass + totalWeight * this.m_itemWeightMassFactor;
		this.SetMass(mass);
	}

	// Token: 0x06001B07 RID: 6919 RVA: 0x000C995C File Offset: 0x000C7B5C
	private void SetMass(float mass)
	{
		float mass2 = mass / (float)this.m_bodies.Length;
		Rigidbody[] bodies = this.m_bodies;
		for (int i = 0; i < bodies.Length; i++)
		{
			bodies[i].mass = mass2;
		}
	}

	// Token: 0x06001B08 RID: 6920 RVA: 0x000C9994 File Offset: 0x000C7B94
	private void UpdateLoadVisualization()
	{
		if (this.m_container == null)
		{
			return;
		}
		float num = this.m_container.GetInventory().SlotsUsedPercentage();
		foreach (Vagon.LoadData loadData in this.m_loadVis)
		{
			loadData.m_gameobject.SetActive(num >= loadData.m_minPercentage);
		}
	}

	// Token: 0x06001B09 RID: 6921 RVA: 0x000C9A18 File Offset: 0x000C7C18
	private void UpdateAudio(float dt)
	{
		float num = 0f;
		foreach (Rigidbody rigidbody in this.m_wheels)
		{
			num += rigidbody.angularVelocity.magnitude;
		}
		num /= (float)this.m_wheels.Length;
		float target = Mathf.Lerp(this.m_minPitch, this.m_maxPitch, Mathf.Clamp01(num / this.m_maxPitchVel));
		float target2 = this.m_maxVol * Mathf.Clamp01(num / this.m_maxVolVel);
		foreach (ZSFX zsfx in this.m_wheelLoops)
		{
			zsfx.SetVolumeModifier(Mathf.MoveTowards(zsfx.GetVolumeModifier(), target2, this.m_audioChangeSpeed * dt));
			zsfx.SetPitchModifier(Mathf.MoveTowards(zsfx.GetPitchModifier(), target, this.m_audioChangeSpeed * dt));
		}
	}

	// Token: 0x04001B86 RID: 7046
	private static List<Vagon> m_instances = new List<Vagon>();

	// Token: 0x04001B87 RID: 7047
	public Transform m_attachPoint;

	// Token: 0x04001B88 RID: 7048
	public string m_name = "Wagon";

	// Token: 0x04001B89 RID: 7049
	public float m_detachDistance = 2f;

	// Token: 0x04001B8A RID: 7050
	public Vector3 m_attachOffset = new Vector3(0f, 0.8f, 0f);

	// Token: 0x04001B8B RID: 7051
	public Container m_container;

	// Token: 0x04001B8C RID: 7052
	public Transform m_lineAttachPoints0;

	// Token: 0x04001B8D RID: 7053
	public Transform m_lineAttachPoints1;

	// Token: 0x04001B8E RID: 7054
	public Vector3 m_lineAttachOffset = new Vector3(0f, 1f, 0f);

	// Token: 0x04001B8F RID: 7055
	public float m_breakForce = 10000f;

	// Token: 0x04001B90 RID: 7056
	public float m_spring = 5000f;

	// Token: 0x04001B91 RID: 7057
	public float m_springDamping = 1000f;

	// Token: 0x04001B92 RID: 7058
	public float m_baseMass = 20f;

	// Token: 0x04001B93 RID: 7059
	public float m_itemWeightMassFactor = 1f;

	// Token: 0x04001B94 RID: 7060
	public float m_playerExtraPullMass;

	// Token: 0x04001B95 RID: 7061
	public ZSFX[] m_wheelLoops;

	// Token: 0x04001B96 RID: 7062
	public float m_minPitch = 1f;

	// Token: 0x04001B97 RID: 7063
	public float m_maxPitch = 1.5f;

	// Token: 0x04001B98 RID: 7064
	public float m_maxPitchVel = 10f;

	// Token: 0x04001B99 RID: 7065
	public float m_maxVol = 1f;

	// Token: 0x04001B9A RID: 7066
	public float m_maxVolVel = 10f;

	// Token: 0x04001B9B RID: 7067
	public float m_audioChangeSpeed = 2f;

	// Token: 0x04001B9C RID: 7068
	public Rigidbody[] m_wheels = new Rigidbody[0];

	// Token: 0x04001B9D RID: 7069
	public List<Vagon.LoadData> m_loadVis = new List<Vagon.LoadData>();

	// Token: 0x04001B9E RID: 7070
	private ZNetView m_nview;

	// Token: 0x04001B9F RID: 7071
	private ConfigurableJoint m_attachJoin;

	// Token: 0x04001BA0 RID: 7072
	private GameObject m_attachedObject;

	// Token: 0x04001BA1 RID: 7073
	private Rigidbody m_body;

	// Token: 0x04001BA2 RID: 7074
	private LineRenderer m_lineRenderer;

	// Token: 0x04001BA3 RID: 7075
	private Rigidbody[] m_bodies;

	// Token: 0x04001BA4 RID: 7076
	private Humanoid m_useRequester;

	// Token: 0x02000395 RID: 917
	[Serializable]
	public class LoadData
	{
		// Token: 0x040026BC RID: 9916
		public GameObject m_gameobject;

		// Token: 0x040026BD RID: 9917
		public float m_minPercentage;
	}
}
