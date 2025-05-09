using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000F0 RID: 240
public class ZSyncTransform : MonoBehaviour, IMonoUpdater
{
	// Token: 0x06000FBF RID: 4031 RVA: 0x00075798 File Offset: 0x00073998
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_projectile = base.GetComponent<Projectile>();
		this.m_character = base.GetComponent<Character>();
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		if (this.m_body)
		{
			this.m_isKinematicBody = this.m_body.isKinematic;
			this.m_useGravity = this.m_body.useGravity;
		}
		this.m_wasOwner = this.m_nview.GetZDO().IsOwner();
	}

	// Token: 0x06000FC0 RID: 4032 RVA: 0x0007582F File Offset: 0x00073A2F
	private void OnEnable()
	{
		ZSyncTransform.Instances.Add(this);
	}

	// Token: 0x06000FC1 RID: 4033 RVA: 0x0007583C File Offset: 0x00073A3C
	private void OnDisable()
	{
		ZSyncTransform.Instances.Remove(this);
	}

	// Token: 0x06000FC2 RID: 4034 RVA: 0x0007584A File Offset: 0x00073A4A
	private Vector3 GetVelocity()
	{
		if (this.m_body != null)
		{
			return this.m_body.velocity;
		}
		if (this.m_projectile != null)
		{
			return this.m_projectile.GetVelocity();
		}
		return Vector3.zero;
	}

	// Token: 0x06000FC3 RID: 4035 RVA: 0x00075885 File Offset: 0x00073A85
	private Vector3 GetPosition()
	{
		if (!this.m_body)
		{
			return base.transform.position;
		}
		return this.m_body.position;
	}

	// Token: 0x06000FC4 RID: 4036 RVA: 0x000758AC File Offset: 0x00073AAC
	private void OwnerSync()
	{
		ZDO zdo = this.m_nview.GetZDO();
		bool flag = zdo.IsOwner();
		bool flag2 = !this.m_wasOwner && flag;
		this.m_wasOwner = flag;
		if (!flag)
		{
			return;
		}
		if (flag2)
		{
			bool flag3 = false;
			if (this.m_syncPosition)
			{
				base.transform.position = zdo.GetPosition();
				flag3 = true;
			}
			if (this.m_syncRotation)
			{
				base.transform.rotation = zdo.GetRotation();
				flag3 = true;
			}
			if (this.m_syncBodyVelocity && this.m_body)
			{
				this.m_body.velocity = zdo.GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero);
				this.m_body.angularVelocity = zdo.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero);
			}
			if (flag3 && this.m_body)
			{
				Physics.SyncTransforms();
			}
		}
		if (base.transform.position.y < -5000f)
		{
			if (this.m_body)
			{
				this.m_body.velocity = Vector3.zero;
			}
			ZLog.Log("Object fell out of world:" + base.gameObject.name);
			float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
			Vector3 position = base.transform.position;
			position.y = groundHeight + 1f;
			base.transform.position = position;
			if (this.m_body)
			{
				Physics.SyncTransforms();
			}
			return;
		}
		if (this.m_syncPosition)
		{
			Vector3 position2 = this.GetPosition();
			if (!this.m_positionCached.Equals(position2))
			{
				zdo.SetPosition(position2);
			}
			Vector3 velocity = this.GetVelocity();
			if (!this.m_velocityCached.Equals(velocity))
			{
				zdo.Set(ZDOVars.s_velHash, velocity);
			}
			this.m_positionCached = position2;
			this.m_velocityCached = velocity;
			if (this.m_characterParentSync)
			{
				if (this.GetRelativePosition(zdo, out this.m_tempParent, out this.m_tempAttachJoint, out this.m_tempRelativePos, out this.m_tempRelativeRot, out this.m_tempRelativeVel))
				{
					if (this.m_tempParent != this.m_tempParentCached)
					{
						zdo.SetConnection(ZDOExtraData.ConnectionType.SyncTransform, this.m_tempParent);
						zdo.Set(ZDOVars.s_attachJointHash, this.m_tempAttachJoint);
					}
					if (!this.m_tempRelativePos.Equals(this.m_tempRelativePosCached))
					{
						zdo.Set(ZDOVars.s_relPosHash, this.m_tempRelativePos);
					}
					if (!this.m_tempRelativeRot.Equals(this.m_tempRelativeRotCached))
					{
						zdo.Set(ZDOVars.s_relRotHash, this.m_tempRelativeRot);
					}
					if (!this.m_tempRelativeVel.Equals(this.m_tempRelativeVelCached))
					{
						zdo.Set(ZDOVars.s_velHash, this.m_tempRelativeVel);
					}
					this.m_tempRelativePosCached = this.m_tempRelativePos;
					this.m_tempRelativeRotCached = this.m_tempRelativeRot;
					this.m_tempRelativeVelCached = this.m_tempRelativeVel;
				}
				else if (this.m_tempParent != this.m_tempParentCached)
				{
					zdo.UpdateConnection(ZDOExtraData.ConnectionType.SyncTransform, ZDOID.None);
					zdo.Set(ZDOVars.s_attachJointHash, "");
				}
				this.m_tempParentCached = this.m_tempParent;
			}
		}
		if (this.m_syncRotation && base.transform.hasChanged)
		{
			Quaternion rotation = this.m_body ? this.m_body.rotation : base.transform.rotation;
			zdo.SetRotation(rotation);
		}
		if (this.m_syncScale && base.transform.hasChanged)
		{
			if (Mathf.Approximately(base.transform.localScale.x, base.transform.localScale.y) && Mathf.Approximately(base.transform.localScale.x, base.transform.localScale.z))
			{
				zdo.RemoveVec3(ZDOVars.s_scaleHash);
				zdo.Set(ZDOVars.s_scaleScalarHash, base.transform.localScale.x);
			}
			else
			{
				zdo.RemoveFloat(ZDOVars.s_scaleScalarHash);
				zdo.Set(ZDOVars.s_scaleHash, base.transform.localScale);
			}
		}
		if (this.m_body)
		{
			if (this.m_syncBodyVelocity)
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_bodyVelHash, this.m_body.velocity);
				this.m_nview.GetZDO().Set(ZDOVars.s_bodyAVelHash, this.m_body.angularVelocity);
			}
			this.m_body.useGravity = this.m_useGravity;
		}
		base.transform.hasChanged = false;
	}

	// Token: 0x06000FC5 RID: 4037 RVA: 0x00075D20 File Offset: 0x00073F20
	private bool GetRelativePosition(ZDO zdo, out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Quaternion relativeRot, out Vector3 relativeVel)
	{
		if (this.m_character)
		{
			return this.m_character.GetRelativePosition(out parent, out attachJoint, out relativePos, out relativeRot, out relativeVel);
		}
		if (base.transform.parent)
		{
			ZNetView znetView = base.transform.parent ? base.transform.parent.GetComponent<ZNetView>() : null;
			if (znetView && znetView.IsValid())
			{
				parent = znetView.GetZDO().m_uid;
				attachJoint = "";
				relativePos = base.transform.localPosition;
				relativeRot = base.transform.localRotation;
				relativeVel = Vector3.zero;
				return true;
			}
		}
		parent = ZDOID.None;
		attachJoint = "";
		relativePos = Vector3.zero;
		relativeRot = Quaternion.identity;
		relativeVel = Vector3.zero;
		return false;
	}

	// Token: 0x06000FC6 RID: 4038 RVA: 0x00075E1C File Offset: 0x0007401C
	private void SyncPosition(ZDO zdo, float dt, out bool usedLocalRotation)
	{
		usedLocalRotation = false;
		if (this.m_characterParentSync && zdo.HasOwner())
		{
			ZDOID connectionZDOID = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.SyncTransform);
			if (!connectionZDOID.IsNone())
			{
				GameObject gameObject = ZNetScene.instance.FindInstance(connectionZDOID);
				if (gameObject)
				{
					ZSyncTransform component = gameObject.GetComponent<ZSyncTransform>();
					if (component)
					{
						component.ClientSync(dt);
					}
					string @string = zdo.GetString(ZDOVars.s_attachJointHash, "");
					Vector3 vector = zdo.GetVec3(ZDOVars.s_relPosHash, Vector3.zero);
					Quaternion quaternion = zdo.GetQuaternion(ZDOVars.s_relRotHash, Quaternion.identity);
					Vector3 vec = zdo.GetVec3(ZDOVars.s_velHash, Vector3.zero);
					bool flag = false;
					if (zdo.DataRevision != this.m_posRevision)
					{
						this.m_posRevision = zdo.DataRevision;
						this.m_targetPosTimer = 0f;
					}
					if (@string.Length > 0)
					{
						Transform transform = Utils.FindChild(gameObject.transform, @string, Utils.IterativeSearchType.DepthFirst);
						if (transform)
						{
							base.transform.position = transform.position;
							flag = true;
						}
					}
					else
					{
						this.m_targetPosTimer += dt;
						this.m_targetPosTimer = Mathf.Min(this.m_targetPosTimer, 2f);
						vector += vec * this.m_targetPosTimer;
						if (!this.m_haveTempRelPos)
						{
							this.m_haveTempRelPos = true;
							this.m_tempRelPos = vector;
						}
						if (Vector3.Distance(this.m_tempRelPos, vector) > 0.001f)
						{
							this.m_tempRelPos = Vector3.Lerp(this.m_tempRelPos, vector, 0.2f);
							vector = this.m_tempRelPos;
						}
						Vector3 vector2 = gameObject.transform.TransformPoint(vector);
						if (Vector3.Distance(base.transform.position, vector2) > 0.001f)
						{
							base.transform.position = vector2;
							flag = true;
						}
					}
					Quaternion a = Quaternion.Inverse(gameObject.transform.rotation) * base.transform.rotation;
					if (Quaternion.Angle(a, quaternion) > 0.001f)
					{
						Quaternion rhs = Quaternion.Slerp(a, quaternion, 0.5f);
						base.transform.rotation = gameObject.transform.rotation * rhs;
						flag = true;
					}
					usedLocalRotation = true;
					if (flag && this.m_body)
					{
						Physics.SyncTransforms();
					}
					return;
				}
			}
		}
		this.m_haveTempRelPos = false;
		Vector3 vector3 = zdo.GetPosition();
		if (zdo.DataRevision != this.m_posRevision)
		{
			this.m_posRevision = zdo.DataRevision;
			this.m_targetPosTimer = 0f;
		}
		if (zdo.HasOwner())
		{
			this.m_targetPosTimer += dt;
			this.m_targetPosTimer = Mathf.Min(this.m_targetPosTimer, 2f);
			Vector3 vec2 = zdo.GetVec3(ZDOVars.s_velHash, Vector3.zero);
			vector3 += vec2 * this.m_targetPosTimer;
		}
		float num = Vector3.Distance(base.transform.position, vector3);
		if (num > 0.001f)
		{
			base.transform.position = ((num < 5f) ? Vector3.Lerp(base.transform.position, vector3, 0.2f) : vector3);
			if (this.m_body)
			{
				Physics.SyncTransforms();
			}
		}
	}

	// Token: 0x06000FC7 RID: 4039 RVA: 0x00076150 File Offset: 0x00074350
	private void ClientSync(float dt)
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo.IsOwner())
		{
			return;
		}
		int frameCount = Time.frameCount;
		if (this.m_lastUpdateFrame == frameCount)
		{
			return;
		}
		this.m_lastUpdateFrame = frameCount;
		if (this.m_isKinematicBody)
		{
			if (this.m_syncPosition)
			{
				Vector3 vector = zdo.GetPosition();
				if (Vector3.Distance(this.m_body.position, vector) > 5f)
				{
					this.m_body.position = vector;
				}
				else
				{
					if (Vector3.Distance(this.m_body.position, vector) > 0.01f)
					{
						vector = Vector3.Lerp(this.m_body.position, vector, 0.2f);
					}
					this.m_body.MovePosition(vector);
				}
			}
			if (this.m_syncRotation)
			{
				Quaternion rotation = zdo.GetRotation();
				if (Quaternion.Angle(this.m_body.rotation, rotation) > 45f)
				{
					this.m_body.rotation = rotation;
				}
				else
				{
					this.m_body.MoveRotation(rotation);
				}
			}
		}
		else
		{
			bool flag = false;
			if (this.m_syncPosition)
			{
				this.SyncPosition(zdo, dt, out flag);
			}
			if (this.m_syncRotation && !flag)
			{
				Quaternion rotation2 = zdo.GetRotation();
				if (Quaternion.Angle(base.transform.rotation, rotation2) > 0.001f)
				{
					base.transform.rotation = Quaternion.Slerp(base.transform.rotation, rotation2, 0.5f);
				}
			}
			if (this.m_body)
			{
				this.m_body.useGravity = false;
				if (this.m_syncBodyVelocity && this.m_nview.HasOwner())
				{
					Vector3 vec = zdo.GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero);
					Vector3 vec2 = zdo.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero);
					if (vec.magnitude > 0.01f || vec2.magnitude > 0.01f)
					{
						this.m_body.velocity = vec;
						this.m_body.angularVelocity = vec2;
					}
					else
					{
						this.m_body.Sleep();
					}
				}
				else if (!this.m_body.IsSleeping())
				{
					this.m_body.velocity = Vector3.zero;
					this.m_body.angularVelocity = Vector3.zero;
					this.m_body.Sleep();
				}
			}
		}
		if (this.m_syncScale)
		{
			Vector3 vec3 = zdo.GetVec3(ZDOVars.s_scaleHash, Vector3.zero);
			if (vec3 != Vector3.zero)
			{
				base.transform.localScale = vec3;
				return;
			}
			float @float = zdo.GetFloat(ZDOVars.s_scaleScalarHash, base.transform.localScale.x);
			if (!base.transform.localScale.x.Equals(@float))
			{
				base.transform.localScale = new Vector3(@float, @float, @float);
			}
		}
	}

	// Token: 0x06000FC8 RID: 4040 RVA: 0x0007640C File Offset: 0x0007460C
	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.ClientSync(fixedDeltaTime);
	}

	// Token: 0x06000FC9 RID: 4041 RVA: 0x00076423 File Offset: 0x00074623
	public void CustomLateUpdate(float deltaTime)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.OwnerSync();
	}

	// Token: 0x06000FCA RID: 4042 RVA: 0x00076439 File Offset: 0x00074639
	public void SyncNow()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.OwnerSync();
	}

	// Token: 0x17000083 RID: 131
	// (get) Token: 0x06000FCB RID: 4043 RVA: 0x0007644F File Offset: 0x0007464F
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04000F21 RID: 3873
	public bool m_syncPosition = true;

	// Token: 0x04000F22 RID: 3874
	public bool m_syncRotation = true;

	// Token: 0x04000F23 RID: 3875
	public bool m_syncScale;

	// Token: 0x04000F24 RID: 3876
	public bool m_syncBodyVelocity;

	// Token: 0x04000F25 RID: 3877
	public bool m_characterParentSync;

	// Token: 0x04000F26 RID: 3878
	private const float m_smoothnessPos = 0.2f;

	// Token: 0x04000F27 RID: 3879
	private const float m_smoothnessRot = 0.5f;

	// Token: 0x04000F28 RID: 3880
	private bool m_isKinematicBody;

	// Token: 0x04000F29 RID: 3881
	private bool m_useGravity = true;

	// Token: 0x04000F2A RID: 3882
	private Vector3 m_tempRelPos;

	// Token: 0x04000F2B RID: 3883
	private bool m_haveTempRelPos;

	// Token: 0x04000F2C RID: 3884
	private float m_targetPosTimer;

	// Token: 0x04000F2D RID: 3885
	private uint m_posRevision = uint.MaxValue;

	// Token: 0x04000F2E RID: 3886
	private int m_lastUpdateFrame = -1;

	// Token: 0x04000F2F RID: 3887
	private bool m_wasOwner;

	// Token: 0x04000F30 RID: 3888
	private ZNetView m_nview;

	// Token: 0x04000F31 RID: 3889
	private Rigidbody m_body;

	// Token: 0x04000F32 RID: 3890
	private Projectile m_projectile;

	// Token: 0x04000F33 RID: 3891
	private Character m_character;

	// Token: 0x04000F34 RID: 3892
	private ZDOID m_tempParent = ZDOID.None;

	// Token: 0x04000F35 RID: 3893
	private ZDOID m_tempParentCached;

	// Token: 0x04000F36 RID: 3894
	private string m_tempAttachJoint;

	// Token: 0x04000F37 RID: 3895
	private Vector3 m_tempRelativePos;

	// Token: 0x04000F38 RID: 3896
	private Quaternion m_tempRelativeRot;

	// Token: 0x04000F39 RID: 3897
	private Vector3 m_tempRelativeVel;

	// Token: 0x04000F3A RID: 3898
	private Vector3 m_tempRelativePosCached;

	// Token: 0x04000F3B RID: 3899
	private Quaternion m_tempRelativeRotCached;

	// Token: 0x04000F3C RID: 3900
	private Vector3 m_tempRelativeVelCached;

	// Token: 0x04000F3D RID: 3901
	private Vector3 m_positionCached = Vector3.negativeInfinity;

	// Token: 0x04000F3E RID: 3902
	private Vector3 m_velocityCached = Vector3.negativeInfinity;
}
