using System;
using UnityEngine;

// Token: 0x0200017F RID: 383
public class FloatingTerrain : MonoBehaviour
{
	// Token: 0x06001715 RID: 5909 RVA: 0x000AB6C0 File Offset: 0x000A98C0
	private void Start()
	{
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_collider = base.GetComponentInChildren<BoxCollider>();
		base.InvokeRepeating("UpdateTerrain", UnityEngine.Random.Range(0.1f, 0.4f), 0.24f);
		this.UpdateTerrain();
	}

	// Token: 0x06001716 RID: 5910 RVA: 0x000AB700 File Offset: 0x000A9900
	private void UpdateTerrain()
	{
		if (!this.m_lastHeightmap)
		{
			this.m_targetOffset = 0f;
			return;
		}
		this.m_targetOffset = this.m_lastHeightmap.GetHeightOffset(base.transform.position) + this.m_padding;
		if (!this.m_dummy)
		{
			GameObject gameObject = new GameObject();
			if (this.m_copyLayer)
			{
				gameObject.layer = base.gameObject.layer;
			}
			this.m_dummy = gameObject.AddComponent<FloatingTerrainDummy>();
			this.m_dummy.m_parent = this;
			this.m_dummyBody = gameObject.AddComponent<Rigidbody>();
			this.m_dummyBody.mass = this.m_body.mass;
			this.m_dummyBody.drag = this.m_body.drag;
			this.m_dummyBody.angularDrag = this.m_body.angularDrag;
			this.m_dummyBody.constraints = this.m_body.constraints;
			this.m_dummyCollider = gameObject.AddComponent<BoxCollider>();
			this.m_dummyCollider.center = this.m_collider.center;
			this.m_dummyCollider.size = this.m_collider.size;
			if (this.m_collider.gameObject != this)
			{
				this.m_dummyCollider.size = Vector3.Scale(this.m_collider.size, this.m_collider.transform.localScale);
				this.m_dummyCollider.center = Vector3.Scale(this.m_collider.center, this.m_collider.transform.localScale);
				this.m_dummyCollider.center -= this.m_collider.transform.localPosition;
			}
			gameObject.transform.parent = base.transform.parent;
			gameObject.transform.position = base.transform.position;
			this.m_collider.isTrigger = true;
			UnityEngine.Object.Destroy(this.m_body);
		}
	}

	// Token: 0x06001717 RID: 5911 RVA: 0x000AB900 File Offset: 0x000A9B00
	private void FixedUpdate()
	{
		if (this.m_dummy)
		{
			float maxCorrectionSpeed = this.m_maxCorrectionSpeed;
			float value = this.m_targetOffset - this.m_currentOffset;
			this.m_currentOffset += Mathf.Clamp(value, -maxCorrectionSpeed, maxCorrectionSpeed);
			float num = this.m_currentOffset;
			if (this.m_waveFreq > 0f && num > this.m_waveMinOffset)
			{
				this.m_waveTime += Time.fixedDeltaTime;
				num += Mathf.Cos(this.m_waveTime * this.m_waveFreq) * this.m_waveAmp;
			}
			base.transform.position = this.m_dummy.transform.position + new Vector3(0f, num, 0f);
			base.transform.rotation = this.m_dummy.transform.rotation;
		}
	}

	// Token: 0x06001718 RID: 5912 RVA: 0x000AB9DE File Offset: 0x000A9BDE
	public void OnDummyCollision(Collision collision)
	{
		this.OnCollisionStay(collision);
	}

	// Token: 0x06001719 RID: 5913 RVA: 0x000AB9E8 File Offset: 0x000A9BE8
	private void OnCollisionStay(Collision collision)
	{
		Heightmap component = collision.gameObject.GetComponent<Heightmap>();
		if (component != null)
		{
			this.m_lastGroundNormal = collision.contacts[0].normal;
			this.m_lastHeightmapTime = Time.time;
			if (this.m_lastHeightmap != component)
			{
				this.m_lastHeightmap = component;
				this.UpdateTerrain();
				return;
			}
		}
		else if (this.m_lastHeightmapTime > 0.2f)
		{
			this.m_lastHeightmap = null;
		}
	}

	// Token: 0x0600171A RID: 5914 RVA: 0x000ABA58 File Offset: 0x000A9C58
	private void OnDrawGizmos()
	{
		if (this.m_dummyCollider && this.m_dummyCollider.enabled)
		{
			Gizmos.color = Color.yellow;
			Gizmos.matrix = Matrix4x4.TRS(this.m_dummyCollider.transform.position, this.m_dummyCollider.transform.rotation, this.m_dummyCollider.transform.lossyScale);
			Gizmos.DrawWireCube(this.m_dummyCollider.center, this.m_dummyCollider.size);
		}
		if (this.m_dummy != null)
		{
			Gizmos.DrawLine(base.transform.position, base.transform.position + new Vector3(0f, this.m_currentOffset, 0f));
		}
	}

	// Token: 0x0600171B RID: 5915 RVA: 0x000ABB21 File Offset: 0x000A9D21
	private void OnDestroy()
	{
		if (this.m_dummy)
		{
			UnityEngine.Object.Destroy(this.m_dummy.gameObject);
		}
	}

	// Token: 0x0600171C RID: 5916 RVA: 0x000ABB40 File Offset: 0x000A9D40
	public static Rigidbody GetBody(GameObject obj)
	{
		FloatingTerrain component = obj.GetComponent<FloatingTerrain>();
		if (component != null && component.m_dummy && component.m_dummyBody)
		{
			return component.m_dummyBody;
		}
		return null;
	}

	// Token: 0x04001706 RID: 5894
	public float m_padding;

	// Token: 0x04001707 RID: 5895
	public float m_waveMinOffset;

	// Token: 0x04001708 RID: 5896
	public float m_waveFreq;

	// Token: 0x04001709 RID: 5897
	public float m_waveAmp;

	// Token: 0x0400170A RID: 5898
	public FloatingTerrainDummy m_dummy;

	// Token: 0x0400170B RID: 5899
	public float m_maxCorrectionSpeed = 0.025f;

	// Token: 0x0400170C RID: 5900
	public bool m_copyLayer = true;

	// Token: 0x0400170D RID: 5901
	private Rigidbody m_body;

	// Token: 0x0400170E RID: 5902
	[NonSerialized]
	public Rigidbody m_dummyBody;

	// Token: 0x0400170F RID: 5903
	private BoxCollider m_collider;

	// Token: 0x04001710 RID: 5904
	private BoxCollider m_dummyCollider;

	// Token: 0x04001711 RID: 5905
	private Heightmap m_lastHeightmap;

	// Token: 0x04001712 RID: 5906
	private Vector3 m_lastGroundNormal;

	// Token: 0x04001713 RID: 5907
	private float m_targetOffset;

	// Token: 0x04001714 RID: 5908
	private float m_currentOffset;

	// Token: 0x04001715 RID: 5909
	private float m_lastHeightmapTime;

	// Token: 0x04001716 RID: 5910
	private float m_waveTime;
}
