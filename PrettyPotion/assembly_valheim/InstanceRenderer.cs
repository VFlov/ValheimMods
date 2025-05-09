using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Token: 0x02000123 RID: 291
public class InstanceRenderer : MonoBehaviour, IMonoUpdater
{
	// Token: 0x0600128C RID: 4748 RVA: 0x0008AB52 File Offset: 0x00088D52
	private void OnEnable()
	{
		InstanceRenderer.Instances.Add(this);
	}

	// Token: 0x0600128D RID: 4749 RVA: 0x0008AB5F File Offset: 0x00088D5F
	private void OnDisable()
	{
		InstanceRenderer.Instances.Remove(this);
	}

	// Token: 0x0600128E RID: 4750 RVA: 0x0008AB70 File Offset: 0x00088D70
	public void CustomUpdate(float deltaTime, float time)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (this.m_instanceCount == 0 || mainCamera == null)
		{
			return;
		}
		if (this.m_frustumCull)
		{
			if (this.m_dirtyBounds)
			{
				this.UpdateBounds();
			}
			if (!Utils.InsideMainCamera(this.m_bounds))
			{
				return;
			}
		}
		if (this.m_useLod)
		{
			float num = this.m_useXZLodDistance ? Utils.DistanceXZ(mainCamera.transform.position, base.transform.position) : Vector3.Distance(mainCamera.transform.position, base.transform.position);
			int num2 = (int)((1f - Utils.LerpStep(this.m_lodMinDistance, this.m_lodMaxDistance, num)) * (float)this.m_instanceCount);
			float maxDelta = deltaTime * (float)this.m_instanceCount;
			this.m_lodCount = Mathf.MoveTowards(this.m_lodCount, (float)num2, maxDelta);
			if (this.m_firstFrame)
			{
				if (num < this.m_lodMinDistance)
				{
					this.m_lodCount = (float)num2;
				}
				this.m_firstFrame = false;
			}
			this.m_lodCount = Mathf.Min(this.m_lodCount, (float)this.m_instanceCount);
			int num3 = (int)this.m_lodCount;
			if (num3 > 0)
			{
				Graphics.DrawMeshInstanced(this.m_mesh, 0, this.m_material, this.m_instances, num3, null, this.m_shadowCasting);
				return;
			}
		}
		else
		{
			Graphics.DrawMeshInstanced(this.m_mesh, 0, this.m_material, this.m_instances, this.m_instanceCount, null, this.m_shadowCasting);
		}
	}

	// Token: 0x0600128F RID: 4751 RVA: 0x0008ACD4 File Offset: 0x00088ED4
	private void UpdateBounds()
	{
		this.m_dirtyBounds = false;
		Vector3 vector = new Vector3(9999999f, 9999999f, 9999999f);
		Vector3 vector2 = new Vector3(-9999999f, -9999999f, -9999999f);
		float magnitude = this.m_mesh.bounds.extents.magnitude;
		for (int i = 0; i < this.m_instanceCount; i++)
		{
			Matrix4x4 matrix4x = this.m_instances[i];
			Vector3 a = new Vector3(matrix4x[0, 3], matrix4x[1, 3], matrix4x[2, 3]);
			Vector3 lossyScale = matrix4x.lossyScale;
			float num = Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z);
			Vector3 b = new Vector3(num * magnitude, num * magnitude, num * magnitude);
			vector2 = Vector3.Max(vector2, a + b);
			vector = Vector3.Min(vector, a - b);
		}
		this.m_bounds.position = (vector2 + vector) * 0.5f;
		this.m_bounds.radius = Vector3.Distance(vector2, this.m_bounds.position);
	}

	// Token: 0x06001290 RID: 4752 RVA: 0x0008AE14 File Offset: 0x00089014
	public void AddInstance(Vector3 pos, Quaternion rot, float scale)
	{
		Matrix4x4 m = Matrix4x4.TRS(pos, rot, this.m_scale * scale);
		this.AddInstance(m);
	}

	// Token: 0x06001291 RID: 4753 RVA: 0x0008AE3C File Offset: 0x0008903C
	public void AddInstance(Vector3 pos, Quaternion rot)
	{
		Matrix4x4 m = Matrix4x4.TRS(pos, rot, this.m_scale);
		this.AddInstance(m);
	}

	// Token: 0x06001292 RID: 4754 RVA: 0x0008AE5E File Offset: 0x0008905E
	public void AddInstance(Matrix4x4 m)
	{
		if (this.m_instanceCount >= 1023)
		{
			return;
		}
		this.m_instances[this.m_instanceCount] = m;
		this.m_instanceCount++;
		this.m_dirtyBounds = true;
	}

	// Token: 0x06001293 RID: 4755 RVA: 0x0008AE95 File Offset: 0x00089095
	public void Clear()
	{
		this.m_instanceCount = 0;
		this.m_dirtyBounds = true;
	}

	// Token: 0x06001294 RID: 4756 RVA: 0x0008AEA8 File Offset: 0x000890A8
	public void SetInstance(int index, Vector3 pos, Quaternion rot, float scale)
	{
		Matrix4x4 matrix4x = Matrix4x4.TRS(pos, rot, this.m_scale * scale);
		this.m_instances[index] = matrix4x;
		this.m_dirtyBounds = true;
	}

	// Token: 0x06001295 RID: 4757 RVA: 0x0008AEDE File Offset: 0x000890DE
	private void Resize(int instances)
	{
		this.m_instanceCount = instances;
		this.m_dirtyBounds = true;
	}

	// Token: 0x06001296 RID: 4758 RVA: 0x0008AEF0 File Offset: 0x000890F0
	public void SetInstances(List<Transform> transforms, bool faceCamera = false)
	{
		this.Resize(transforms.Count);
		for (int i = 0; i < transforms.Count; i++)
		{
			Transform transform = transforms[i];
			this.m_instances[i] = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
		}
		this.m_dirtyBounds = true;
	}

	// Token: 0x06001297 RID: 4759 RVA: 0x0008AF4C File Offset: 0x0008914C
	public void SetInstancesBillboard(List<Vector4> points)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 forward = -mainCamera.transform.forward;
		this.Resize(points.Count);
		for (int i = 0; i < points.Count; i++)
		{
			Vector4 vector = points[i];
			Vector3 pos = new Vector3(vector.x, vector.y, vector.z);
			float w = vector.w;
			Quaternion q = Quaternion.LookRotation(forward);
			this.m_instances[i] = Matrix4x4.TRS(pos, q, w * this.m_scale);
		}
		this.m_dirtyBounds = true;
	}

	// Token: 0x06001298 RID: 4760 RVA: 0x0008AFF1 File Offset: 0x000891F1
	private void OnDrawGizmosSelected()
	{
	}

	// Token: 0x170000A0 RID: 160
	// (get) Token: 0x06001299 RID: 4761 RVA: 0x0008AFF3 File Offset: 0x000891F3
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04001245 RID: 4677
	public Mesh m_mesh;

	// Token: 0x04001246 RID: 4678
	public Material m_material;

	// Token: 0x04001247 RID: 4679
	public Vector3 m_scale = Vector3.one;

	// Token: 0x04001248 RID: 4680
	public bool m_frustumCull = true;

	// Token: 0x04001249 RID: 4681
	public bool m_useLod;

	// Token: 0x0400124A RID: 4682
	public bool m_useXZLodDistance = true;

	// Token: 0x0400124B RID: 4683
	public float m_lodMinDistance = 5f;

	// Token: 0x0400124C RID: 4684
	public float m_lodMaxDistance = 20f;

	// Token: 0x0400124D RID: 4685
	public ShadowCastingMode m_shadowCasting;

	// Token: 0x0400124E RID: 4686
	private bool m_dirtyBounds = true;

	// Token: 0x0400124F RID: 4687
	private BoundingSphere m_bounds;

	// Token: 0x04001250 RID: 4688
	private float m_lodCount;

	// Token: 0x04001251 RID: 4689
	private Matrix4x4[] m_instances = new Matrix4x4[1024];

	// Token: 0x04001252 RID: 4690
	private int m_instanceCount;

	// Token: 0x04001253 RID: 4691
	private bool m_firstFrame = true;
}
