using System;
using UnityEngine;

// Token: 0x02000053 RID: 83
public class Gibber : MonoBehaviour
{
	// Token: 0x0600060F RID: 1551 RVA: 0x00033264 File Offset: 0x00031464
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
	}

	// Token: 0x06000610 RID: 1552 RVA: 0x00033274 File Offset: 0x00031474
	private void Start()
	{
		Vector3 vector = base.transform.position;
		Vector3 vector2 = Vector3.zero;
		if (this.m_nview && this.m_nview.IsValid())
		{
			vector = this.m_nview.GetZDO().GetVec3(ZDOVars.s_hitPoint, vector);
			vector2 = this.m_nview.GetZDO().GetVec3(ZDOVars.s_hitDir, vector2);
		}
		if (this.m_delay > 0f)
		{
			base.Invoke("Explode", this.m_delay);
			return;
		}
		this.Explode(vector, vector2);
	}

	// Token: 0x06000611 RID: 1553 RVA: 0x00033304 File Offset: 0x00031504
	public void Setup(Vector3 hitPoint, Vector3 hitDir)
	{
		if (this.m_nview && this.m_nview.IsValid())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_hitPoint, hitPoint);
			this.m_nview.GetZDO().Set(ZDOVars.s_hitDir, hitDir);
		}
	}

	// Token: 0x06000612 RID: 1554 RVA: 0x00033358 File Offset: 0x00031558
	private void DestroyAll()
	{
		if (this.m_nview)
		{
			if (!this.m_nview.GetZDO().HasOwner())
			{
				this.m_nview.ClaimOwnership();
			}
			if (this.m_nview.IsOwner())
			{
				ZNetScene.instance.Destroy(base.gameObject);
				return;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	// Token: 0x06000613 RID: 1555 RVA: 0x000333B8 File Offset: 0x000315B8
	private void CreateBodies()
	{
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			GameObject gameObject = componentsInChildren[i].gameObject;
			if (this.m_chanceToRemoveGib > 0f && UnityEngine.Random.value < this.m_chanceToRemoveGib)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
			else if (!gameObject.GetComponent<Rigidbody>())
			{
				gameObject.AddComponent<BoxCollider>();
				gameObject.AddComponent<Rigidbody>().maxDepenetrationVelocity = 2f;
				TimedDestruction timedDestruction = gameObject.AddComponent<TimedDestruction>();
				timedDestruction.m_timeout = UnityEngine.Random.Range(this.m_timeout / 2f, this.m_timeout);
				timedDestruction.Trigger();
			}
		}
	}

	// Token: 0x06000614 RID: 1556 RVA: 0x00033459 File Offset: 0x00031659
	private void Explode()
	{
		this.Explode(Vector3.zero, Vector3.zero);
	}

	// Token: 0x06000615 RID: 1557 RVA: 0x0003346C File Offset: 0x0003166C
	private void Explode(Vector3 hitPoint, Vector3 hitDir)
	{
		base.InvokeRepeating("DestroyAll", this.m_timeout, 1f);
		float t = ((double)hitDir.magnitude > 0.01) ? this.m_impactDirectionMix : 0f;
		this.CreateBodies();
		Rigidbody[] componentsInChildren = base.gameObject.GetComponentsInChildren<Rigidbody>();
		if (componentsInChildren.Length == 0)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		int num = 0;
		foreach (Rigidbody rigidbody in componentsInChildren)
		{
			vector += rigidbody.worldCenterOfMass;
			num++;
		}
		vector /= (float)num;
		foreach (Rigidbody rigidbody2 in componentsInChildren)
		{
			float d = UnityEngine.Random.Range(this.m_minVel, this.m_maxVel);
			Vector3 a = Vector3.Lerp(Vector3.Normalize(rigidbody2.worldCenterOfMass - vector), hitDir, t);
			rigidbody2.velocity = a * d;
			rigidbody2.angularVelocity = new Vector3(UnityEngine.Random.Range(-this.m_maxRotVel, this.m_maxRotVel), UnityEngine.Random.Range(-this.m_maxRotVel, this.m_maxRotVel), UnityEngine.Random.Range(-this.m_maxRotVel, this.m_maxRotVel));
		}
		foreach (Gibber.GibbData gibbData in this.m_gibbs)
		{
			if (gibbData.m_object && gibbData.m_chanceToSpawn < 1f && UnityEngine.Random.value > gibbData.m_chanceToSpawn)
			{
				UnityEngine.Object.Destroy(gibbData.m_object);
			}
		}
		if ((double)hitDir.magnitude > 0.01)
		{
			Quaternion baseRot = Quaternion.LookRotation(hitDir);
			this.m_punchEffector.Create(hitPoint, baseRot, null, 1f, -1);
		}
	}

	// Token: 0x040006D9 RID: 1753
	public EffectList m_punchEffector = new EffectList();

	// Token: 0x040006DA RID: 1754
	public GameObject m_gibHitEffect;

	// Token: 0x040006DB RID: 1755
	public GameObject m_gibDestroyEffect;

	// Token: 0x040006DC RID: 1756
	public float m_gibHitDestroyChance;

	// Token: 0x040006DD RID: 1757
	public Gibber.GibbData[] m_gibbs = new Gibber.GibbData[0];

	// Token: 0x040006DE RID: 1758
	public float m_minVel = 10f;

	// Token: 0x040006DF RID: 1759
	public float m_maxVel = 20f;

	// Token: 0x040006E0 RID: 1760
	public float m_maxRotVel = 20f;

	// Token: 0x040006E1 RID: 1761
	public float m_impactDirectionMix = 0.5f;

	// Token: 0x040006E2 RID: 1762
	public float m_timeout = 5f;

	// Token: 0x040006E3 RID: 1763
	public float m_delay;

	// Token: 0x040006E4 RID: 1764
	[Range(0f, 1f)]
	public float m_chanceToRemoveGib;

	// Token: 0x040006E5 RID: 1765
	private ZNetView m_nview;

	// Token: 0x02000251 RID: 593
	[Serializable]
	public class GibbData
	{
		// Token: 0x04002018 RID: 8216
		public GameObject m_object;

		// Token: 0x04002019 RID: 8217
		public float m_chanceToSpawn = 1f;
	}
}
