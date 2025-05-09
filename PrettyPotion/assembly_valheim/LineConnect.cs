using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000059 RID: 89
public class LineConnect : MonoBehaviour
{
	// Token: 0x06000632 RID: 1586 RVA: 0x00034490 File Offset: 0x00032690
	private void Awake()
	{
		this.m_lineRenderer = base.GetComponent<LineRenderer>();
		this.m_nview = base.GetComponentInParent<ZNetView>();
		this.m_linePeerID = ZDO.GetHashZDOID(this.m_netViewPrefix + "line_peer");
		this.m_slackHash = (this.m_netViewPrefix + "line_slack").GetStableHashCode();
	}

	// Token: 0x06000633 RID: 1587 RVA: 0x000344EC File Offset: 0x000326EC
	private void LateUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			this.m_lineRenderer.enabled = false;
			return;
		}
		ZDOID zdoid = this.m_nview.GetZDO().GetZDOID(this.m_linePeerID);
		GameObject gameObject = ZNetScene.instance.FindInstance(zdoid);
		if (gameObject && !string.IsNullOrEmpty(this.m_childObject))
		{
			Transform transform = Utils.FindChild(gameObject.transform, this.m_childObject, Utils.IterativeSearchType.DepthFirst);
			if (transform)
			{
				gameObject = transform.gameObject;
			}
		}
		if (gameObject != null)
		{
			Vector3 endpoint = gameObject.transform.position;
			if (this.m_centerOfCharacter)
			{
				Character component = gameObject.GetComponent<Character>();
				if (component)
				{
					endpoint = component.GetCenterPoint();
				}
			}
			this.SetEndpoint(endpoint);
			this.m_lineRenderer.enabled = true;
			return;
		}
		if (this.m_hideIfNoConnection)
		{
			this.m_lineRenderer.enabled = false;
			return;
		}
		this.m_lineRenderer.enabled = true;
		this.SetEndpoint(base.transform.position + this.m_noConnectionWorldOffset);
	}

	// Token: 0x06000634 RID: 1588 RVA: 0x000345F4 File Offset: 0x000327F4
	private void SetEndpoint(Vector3 pos)
	{
		Vector3 vector = base.transform.InverseTransformPoint(pos);
		Vector3 a = base.transform.InverseTransformDirection(Vector3.down);
		if (this.m_dynamicSlack)
		{
			float @float = this.m_nview.GetZDO().GetFloat(this.m_slackHash, this.m_slack);
			Vector3 position = this.m_lineRenderer.GetPosition(0);
			Vector3 b = vector;
			float d = Vector3.Distance(position, b) / 2f;
			for (int i = 1; i < this.m_lineRenderer.positionCount; i++)
			{
				float num = (float)i / (float)(this.m_lineRenderer.positionCount - 1);
				float num2 = Mathf.Abs(0.5f - num) * 2f;
				num2 *= num2;
				num2 = 1f - num2;
				Vector3 vector2 = Vector3.Lerp(position, b, num);
				vector2 += a * d * @float * num2;
				this.m_lineRenderer.SetPosition(i, vector2);
			}
		}
		else
		{
			this.m_lineRenderer.SetPosition(1, vector);
		}
		if (this.m_dynamicThickness)
		{
			float v = Vector3.Distance(base.transform.position, pos);
			float num3 = Utils.LerpStep(this.m_minDistance, this.m_maxDistance, v);
			num3 = Mathf.Pow(num3, this.m_thicknessPower);
			this.m_lineRenderer.widthMultiplier = Mathf.Lerp(this.m_maxThickness, this.m_minThickness, num3);
		}
	}

	// Token: 0x06000635 RID: 1589 RVA: 0x00034763 File Offset: 0x00032963
	public void SetPeer(ZNetView other)
	{
		if (other)
		{
			this.SetPeer(other.GetZDO().m_uid);
			return;
		}
		this.SetPeer(ZDOID.None);
	}

	// Token: 0x06000636 RID: 1590 RVA: 0x0003478A File Offset: 0x0003298A
	public void SetPeer(ZDOID zdoid)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().Set(this.m_linePeerID, zdoid);
	}

	// Token: 0x06000637 RID: 1591 RVA: 0x000347BE File Offset: 0x000329BE
	public void SetSlack(float slack)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().Set(this.m_slackHash, slack);
	}

	// Token: 0x0400072F RID: 1839
	public bool m_centerOfCharacter;

	// Token: 0x04000730 RID: 1840
	public string m_childObject = "";

	// Token: 0x04000731 RID: 1841
	public bool m_hideIfNoConnection = true;

	// Token: 0x04000732 RID: 1842
	public Vector3 m_noConnectionWorldOffset = new Vector3(0f, -1f, 0f);

	// Token: 0x04000733 RID: 1843
	[Header("Dynamic slack")]
	public bool m_dynamicSlack;

	// Token: 0x04000734 RID: 1844
	public float m_slack = 0.5f;

	// Token: 0x04000735 RID: 1845
	[Header("Thickness")]
	public bool m_dynamicThickness = true;

	// Token: 0x04000736 RID: 1846
	public float m_minDistance = 6f;

	// Token: 0x04000737 RID: 1847
	public float m_maxDistance = 30f;

	// Token: 0x04000738 RID: 1848
	public float m_minThickness = 0.2f;

	// Token: 0x04000739 RID: 1849
	public float m_maxThickness = 0.8f;

	// Token: 0x0400073A RID: 1850
	public float m_thicknessPower = 0.2f;

	// Token: 0x0400073B RID: 1851
	public string m_netViewPrefix = "";

	// Token: 0x0400073C RID: 1852
	private LineRenderer m_lineRenderer;

	// Token: 0x0400073D RID: 1853
	private ZNetView m_nview;

	// Token: 0x0400073E RID: 1854
	private KeyValuePair<int, int> m_linePeerID;

	// Token: 0x0400073F RID: 1855
	private int m_slackHash;
}
