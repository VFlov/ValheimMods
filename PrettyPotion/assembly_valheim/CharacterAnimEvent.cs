using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000012 RID: 18
public class CharacterAnimEvent : MonoBehaviour, IMonoUpdater
{
	// Token: 0x06000220 RID: 544 RVA: 0x000138E8 File Offset: 0x00011AE8
	private void Awake()
	{
		this.m_character = base.GetComponentInParent<Character>();
		this.m_nview = this.m_character.GetComponent<ZNetView>();
		this.m_animator = base.GetComponent<Animator>();
		this.m_monsterAI = this.m_character.GetComponent<MonsterAI>();
		this.m_visEquipment = this.m_character.GetComponent<VisEquipment>();
		this.m_footStep = this.m_character.GetComponent<FootStep>();
		this.m_head = Utils.GetBoneTransform(this.m_animator, HumanBodyBones.Head);
		this.m_headLookDir = this.m_character.transform.forward;
		if (CharacterAnimEvent.s_ikGroundMask == 0)
		{
			CharacterAnimEvent.s_ikGroundMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"terrain",
				"vehicle"
			});
		}
	}

	// Token: 0x06000221 RID: 545 RVA: 0x000139C1 File Offset: 0x00011BC1
	private void OnEnable()
	{
		CharacterAnimEvent.Instances.Add(this);
	}

	// Token: 0x06000222 RID: 546 RVA: 0x000139CE File Offset: 0x00011BCE
	private void OnDisable()
	{
		CharacterAnimEvent.Instances.Remove(this);
	}

	// Token: 0x06000223 RID: 547 RVA: 0x000139DC File Offset: 0x00011BDC
	private void OnAnimatorMove()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_character.AddRootMotion(this.m_animator.deltaPosition);
	}

	// Token: 0x06000224 RID: 548 RVA: 0x00013A10 File Offset: 0x00011C10
	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		if (this.m_character == null)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (!this.m_character.InAttack() && !this.m_character.InMinorAction() && !this.m_character.InEmote() && this.m_character.CanMove())
		{
			this.m_animator.speed = 1f;
		}
		this.UpdateFreezeFrame(fixedDeltaTime);
	}

	// Token: 0x06000225 RID: 549 RVA: 0x00013A85 File Offset: 0x00011C85
	public bool CanChain()
	{
		return this.m_chain;
	}

	// Token: 0x06000226 RID: 550 RVA: 0x00013A90 File Offset: 0x00011C90
	public void FreezeFrame(float delay)
	{
		if (delay <= 0f)
		{
			return;
		}
		if (this.m_pauseTimer > 0f)
		{
			this.m_pauseTimer = delay;
			return;
		}
		this.m_pauseTimer = delay;
		this.m_pauseSpeed = this.m_animator.speed;
		this.m_animator.speed = 0.0001f;
		if (this.m_pauseSpeed <= 0.01f)
		{
			this.m_pauseSpeed = 1f;
		}
	}

	// Token: 0x06000227 RID: 551 RVA: 0x00013AFC File Offset: 0x00011CFC
	private void UpdateFreezeFrame(float dt)
	{
		if (this.m_pauseTimer > 0f)
		{
			this.m_pauseTimer -= dt;
			if (this.m_pauseTimer <= 0f)
			{
				this.m_animator.speed = this.m_pauseSpeed;
			}
		}
		if (this.m_animator.speed < 0.01f && this.m_pauseTimer <= 0f)
		{
			this.m_animator.speed = 1f;
		}
	}

	// Token: 0x06000228 RID: 552 RVA: 0x00013B71 File Offset: 0x00011D71
	public void Speed(float speedScale)
	{
		this.m_animator.speed = speedScale;
	}

	// Token: 0x06000229 RID: 553 RVA: 0x00013B7F File Offset: 0x00011D7F
	public void Chain()
	{
		this.m_chain = true;
	}

	// Token: 0x0600022A RID: 554 RVA: 0x00013B88 File Offset: 0x00011D88
	public void ResetChain()
	{
		this.m_chain = false;
	}

	// Token: 0x0600022B RID: 555 RVA: 0x00013B94 File Offset: 0x00011D94
	public void FootStep(AnimationEvent e)
	{
		if ((double)e.animatorClipInfo.weight < 0.33)
		{
			return;
		}
		if (this.m_footStep)
		{
			if (e.stringParameter.Length > 0)
			{
				this.m_footStep.OnFoot(e.stringParameter);
				return;
			}
			this.m_footStep.OnFoot();
		}
	}

	// Token: 0x0600022C RID: 556 RVA: 0x00013BF4 File Offset: 0x00011DF4
	public void Hit()
	{
		this.m_character.OnAttackTrigger();
	}

	// Token: 0x0600022D RID: 557 RVA: 0x00013C01 File Offset: 0x00011E01
	public void OnAttackTrigger()
	{
		this.m_character.OnAttackTrigger();
	}

	// Token: 0x0600022E RID: 558 RVA: 0x00013C0E File Offset: 0x00011E0E
	public void Jump()
	{
		this.m_character.Jump(true);
	}

	// Token: 0x0600022F RID: 559 RVA: 0x00013C1C File Offset: 0x00011E1C
	public void Land()
	{
		if (this.m_character.IsFlying())
		{
			this.m_character.Land();
		}
	}

	// Token: 0x06000230 RID: 560 RVA: 0x00013C36 File Offset: 0x00011E36
	public void TakeOff()
	{
		if (!this.m_character.IsFlying())
		{
			this.m_character.TakeOff();
		}
	}

	// Token: 0x06000231 RID: 561 RVA: 0x00013C50 File Offset: 0x00011E50
	public void Stop(AnimationEvent e)
	{
		this.m_character.OnStopMoving();
	}

	// Token: 0x06000232 RID: 562 RVA: 0x00013C60 File Offset: 0x00011E60
	public void DodgeMortal()
	{
		Player player = this.m_character as Player;
		if (player)
		{
			player.OnDodgeMortal();
		}
	}

	// Token: 0x06000233 RID: 563 RVA: 0x00013C87 File Offset: 0x00011E87
	public void TrailOn()
	{
		if (this.m_visEquipment)
		{
			this.m_visEquipment.SetWeaponTrails(true);
		}
		this.m_character.OnWeaponTrailStart();
	}

	// Token: 0x06000234 RID: 564 RVA: 0x00013CAD File Offset: 0x00011EAD
	public void TrailOff()
	{
		if (this.m_visEquipment)
		{
			this.m_visEquipment.SetWeaponTrails(false);
		}
	}

	// Token: 0x06000235 RID: 565 RVA: 0x00013CC8 File Offset: 0x00011EC8
	public void GPower()
	{
		Player player = this.m_character as Player;
		if (player)
		{
			player.ActivateGuardianPower();
		}
	}

	// Token: 0x06000236 RID: 566 RVA: 0x00013CF0 File Offset: 0x00011EF0
	private void OnAnimatorIK(int layerIndex)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateLookat();
		this.UpdateFootIK();
	}

	// Token: 0x06000237 RID: 567 RVA: 0x00013D0C File Offset: 0x00011F0C
	public void CustomLateUpdate(float deltaTime)
	{
		this.UpdateHeadRotation(deltaTime);
		if (this.m_femaleHack)
		{
			Character character = this.m_character;
			float num = (this.m_visEquipment.GetModelIndex() == 1) ? this.m_femaleOffset : this.m_maleOffset;
			Vector3 localPosition = this.m_leftShoulder.localPosition;
			localPosition.x = -num;
			this.m_leftShoulder.localPosition = localPosition;
			Vector3 localPosition2 = this.m_rightShoulder.localPosition;
			localPosition2.x = num;
			this.m_rightShoulder.localPosition = localPosition2;
		}
	}

	// Token: 0x06000238 RID: 568 RVA: 0x00013D90 File Offset: 0x00011F90
	private void UpdateLookat()
	{
		if (this.m_headRotation && this.m_head)
		{
			float target = this.m_lookWeight;
			if (this.m_headLookDir != Vector3.zero)
			{
				this.m_animator.SetLookAtPosition(this.m_head.position + this.m_headLookDir * 10f);
			}
			if (this.m_character.InAttack() || (!this.m_character.IsPlayer() && !this.m_character.CanMove()))
			{
				target = 0f;
			}
			this.m_lookAtWeight = Mathf.MoveTowards(this.m_lookAtWeight, target, Time.deltaTime);
			float bodyWeight = this.m_character.IsAttached() ? 0f : this.m_bodyLookWeight;
			this.m_animator.SetLookAtWeight(this.m_lookAtWeight, bodyWeight, this.m_headLookWeight, this.m_eyeLookWeight, this.m_lookClamp);
		}
	}

	// Token: 0x06000239 RID: 569 RVA: 0x00013E80 File Offset: 0x00012080
	private void UpdateFootIK()
	{
		if (!this.m_footIK)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		if (Vector3.Distance(base.transform.position, mainCamera.transform.position) > 64f)
		{
			return;
		}
		if ((this.m_character.IsFlying() && !this.m_character.IsOnGround()) || (this.m_character.IsSwimming() && !this.m_character.IsOnGround()) || this.m_character.IsSitting())
		{
			for (int i = 0; i < this.m_feets.Length; i++)
			{
				CharacterAnimEvent.Foot foot = this.m_feets[i];
				this.m_animator.SetIKPositionWeight(foot.m_ikHandle, 0f);
				this.m_animator.SetIKRotationWeight(foot.m_ikHandle, 0f);
			}
			return;
		}
		bool flag = this.m_character.IsSitting();
		float deltaTime = Time.deltaTime;
		for (int j = 0; j < this.m_feets.Length; j++)
		{
			CharacterAnimEvent.Foot foot2 = this.m_feets[j];
			Vector3 position = foot2.m_transform.position;
			AvatarIKGoal ikHandle = foot2.m_ikHandle;
			float num = this.m_useFeetValues ? foot2.m_footDownMax : this.m_footDownMax;
			float d = this.m_useFeetValues ? foot2.m_footOffset : this.m_footOffset;
			float num2 = this.m_useFeetValues ? foot2.m_footStepHeight : this.m_footStepHeight;
			float num3 = this.m_useFeetValues ? foot2.m_stabalizeDistance : this.m_stabalizeDistance;
			if (flag)
			{
				num2 /= 4f;
			}
			Vector3 vector = base.transform.InverseTransformPoint(position - base.transform.up * d);
			float target = 1f - Mathf.Clamp01(vector.y / num);
			foot2.m_ikWeight = Mathf.MoveTowards(foot2.m_ikWeight, target, deltaTime * 10f);
			this.m_animator.SetIKPositionWeight(ikHandle, foot2.m_ikWeight);
			this.m_animator.SetIKRotationWeight(ikHandle, foot2.m_ikWeight * 0.5f);
			if (foot2.m_ikWeight > 0f)
			{
				RaycastHit raycastHit;
				if (Physics.Raycast(position + Vector3.up * num2, Vector3.down, out raycastHit, num2 * 4f, CharacterAnimEvent.s_ikGroundMask))
				{
					Vector3 vector2 = raycastHit.point + Vector3.up * d;
					Vector3 plantNormal = raycastHit.normal;
					if (num3 > 0f)
					{
						if (foot2.m_ikWeight >= 1f)
						{
							if (!foot2.m_isPlanted)
							{
								foot2.m_plantPosition = vector2;
								foot2.m_plantNormal = plantNormal;
								foot2.m_isPlanted = true;
							}
							else if (Vector3.Distance(foot2.m_plantPosition, vector2) > num3)
							{
								foot2.m_isPlanted = false;
							}
							else
							{
								vector2 = foot2.m_plantPosition;
								plantNormal = foot2.m_plantNormal;
							}
						}
						else
						{
							foot2.m_isPlanted = false;
						}
					}
					this.m_animator.SetIKPosition(ikHandle, vector2);
					Quaternion goalRotation = Quaternion.LookRotation(Vector3.Cross(this.m_animator.GetIKRotation(ikHandle) * Vector3.right, raycastHit.normal), raycastHit.normal);
					this.m_animator.SetIKRotation(ikHandle, goalRotation);
				}
				else
				{
					foot2.m_ikWeight = Mathf.MoveTowards(foot2.m_ikWeight, 0f, deltaTime * 4f);
					this.m_animator.SetIKPositionWeight(ikHandle, foot2.m_ikWeight);
					this.m_animator.SetIKRotationWeight(ikHandle, foot2.m_ikWeight * 0.5f);
				}
			}
		}
	}

	// Token: 0x0600023A RID: 570 RVA: 0x00014218 File Offset: 0x00012418
	private void UpdateHeadRotation(float dt)
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_headRotation && this.m_head)
		{
			Vector3 lookFromPos = this.GetLookFromPos();
			Vector3 vector = Vector3.zero;
			if (this.m_nview.IsOwner())
			{
				if (this.m_monsterAI != null)
				{
					Character targetCreature = this.m_monsterAI.GetTargetCreature();
					if (targetCreature != null)
					{
						vector = targetCreature.GetEyePoint();
					}
				}
				else
				{
					vector = lookFromPos + this.m_character.GetLookDir() * 100f;
				}
				if (this.m_lookAt != null)
				{
					vector = this.m_lookAt.position;
				}
				this.m_sendTimer += Time.deltaTime;
				if (this.m_sendTimer > 0.2f)
				{
					this.m_sendTimer = 0f;
					if (!vector.Equals(this.m_lookTargetCached))
					{
						this.m_nview.GetZDO().Set(ZDOVars.s_lookTarget, vector);
					}
					this.m_lookTargetCached = vector;
				}
			}
			else
			{
				vector = this.m_nview.GetZDO().GetVec3(ZDOVars.s_lookTarget, Vector3.zero);
			}
			if (vector != Vector3.zero)
			{
				Vector3 b = Vector3.Normalize(vector - lookFromPos);
				this.m_headLookDir = Vector3.Lerp(this.m_headLookDir, b, 0.1f);
				return;
			}
			this.m_headLookDir = this.m_character.transform.forward;
		}
	}

	// Token: 0x0600023B RID: 571 RVA: 0x00014398 File Offset: 0x00012598
	private Vector3 GetLookFromPos()
	{
		if (this.m_eyes != null && this.m_eyes.Length != 0)
		{
			Vector3 a = Vector3.zero;
			foreach (Transform transform in this.m_eyes)
			{
				a += transform.position;
			}
			return a / (float)this.m_eyes.Length;
		}
		return this.m_head.position;
	}

	// Token: 0x0600023C RID: 572 RVA: 0x00014400 File Offset: 0x00012600
	public void FindJoints()
	{
		ZLog.Log("Finding joints");
		List<Transform> list = new List<Transform>();
		Transform transform = Utils.FindChild(base.transform, "LeftEye", Utils.IterativeSearchType.DepthFirst);
		Transform transform2 = Utils.FindChild(base.transform, "RightEye", Utils.IterativeSearchType.DepthFirst);
		if (transform)
		{
			list.Add(transform);
		}
		if (transform2)
		{
			list.Add(transform2);
		}
		this.m_eyes = list.ToArray();
		Transform transform3 = Utils.FindChild(base.transform, "LeftFootFront", Utils.IterativeSearchType.DepthFirst);
		Transform transform4 = Utils.FindChild(base.transform, "RightFootFront", Utils.IterativeSearchType.DepthFirst);
		Transform transform5 = Utils.FindChild(base.transform, "LeftFoot", Utils.IterativeSearchType.DepthFirst);
		if (transform5 == null)
		{
			transform5 = Utils.FindChild(base.transform, "LeftFootBack", Utils.IterativeSearchType.DepthFirst);
		}
		if (transform5 == null)
		{
			transform5 = Utils.FindChild(base.transform, "l_foot", Utils.IterativeSearchType.DepthFirst);
		}
		if (transform5 == null)
		{
			transform5 = Utils.FindChild(base.transform, "Foot.l", Utils.IterativeSearchType.DepthFirst);
		}
		if (transform5 == null)
		{
			transform5 = Utils.FindChild(base.transform, "foot.l", Utils.IterativeSearchType.DepthFirst);
		}
		Transform transform6 = Utils.FindChild(base.transform, "RightFoot", Utils.IterativeSearchType.DepthFirst);
		if (transform6 == null)
		{
			transform6 = Utils.FindChild(base.transform, "RightFootBack", Utils.IterativeSearchType.DepthFirst);
		}
		if (transform6 == null)
		{
			transform6 = Utils.FindChild(base.transform, "r_foot", Utils.IterativeSearchType.DepthFirst);
		}
		if (transform6 == null)
		{
			transform6 = Utils.FindChild(base.transform, "Foot.r", Utils.IterativeSearchType.DepthFirst);
		}
		if (transform6 == null)
		{
			transform6 = Utils.FindChild(base.transform, "foot.r", Utils.IterativeSearchType.DepthFirst);
		}
		List<CharacterAnimEvent.Foot> list2 = new List<CharacterAnimEvent.Foot>();
		if (transform3)
		{
			list2.Add(new CharacterAnimEvent.Foot(transform3, AvatarIKGoal.LeftHand));
		}
		if (transform4)
		{
			list2.Add(new CharacterAnimEvent.Foot(transform4, AvatarIKGoal.RightHand));
		}
		if (transform5)
		{
			list2.Add(new CharacterAnimEvent.Foot(transform5, AvatarIKGoal.LeftFoot));
		}
		if (transform6)
		{
			list2.Add(new CharacterAnimEvent.Foot(transform6, AvatarIKGoal.RightFoot));
		}
		this.m_feets = list2.ToArray();
	}

	// Token: 0x0600023D RID: 573 RVA: 0x00014604 File Offset: 0x00012804
	private void OnDrawGizmosSelected()
	{
		if (this.m_footIK)
		{
			foreach (CharacterAnimEvent.Foot foot in this.m_feets)
			{
				float d = this.m_useFeetValues ? foot.m_footDownMax : this.m_footDownMax;
				float d2 = this.m_useFeetValues ? foot.m_footOffset : this.m_footOffset;
				float d3 = this.m_useFeetValues ? foot.m_footStepHeight : this.m_footStepHeight;
				float num = this.m_useFeetValues ? foot.m_stabalizeDistance : this.m_stabalizeDistance;
				Vector3 vector = foot.m_transform.position - base.transform.up * d2;
				Gizmos.color = ((vector.y > base.transform.position.y) ? Color.red : Color.white);
				Gizmos.DrawWireSphere(vector, 0.1f);
				Gizmos.color = Color.yellow;
				Gizmos.DrawWireCube(new Vector3(vector.x, base.transform.position.y, vector.z) + Vector3.up * d, new Vector3(1f, 0.01f, 1f));
				Gizmos.color = Color.red;
				Gizmos.DrawLine(vector, vector + Vector3.up * d3);
				if (num > 0f)
				{
					Gizmos.color = Color.green;
					Gizmos.DrawWireSphere(vector, num);
					Gizmos.matrix = Matrix4x4.identity;
				}
				if (foot.m_isPlanted)
				{
					Gizmos.color = Color.yellow;
					Gizmos.DrawWireCube(vector, new Vector3(0.4f, 0.3f, 0.4f));
				}
			}
		}
	}

	// Token: 0x17000008 RID: 8
	// (get) Token: 0x0600023E RID: 574 RVA: 0x000147C1 File Offset: 0x000129C1
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04000302 RID: 770
	[Header("Foot IK")]
	public bool m_footIK;

	// Token: 0x04000303 RID: 771
	public float m_footDownMax = 0.4f;

	// Token: 0x04000304 RID: 772
	public float m_footOffset = 0.1f;

	// Token: 0x04000305 RID: 773
	public float m_footStepHeight = 1f;

	// Token: 0x04000306 RID: 774
	public float m_stabalizeDistance;

	// Token: 0x04000307 RID: 775
	public bool m_useFeetValues;

	// Token: 0x04000308 RID: 776
	public CharacterAnimEvent.Foot[] m_feets = Array.Empty<CharacterAnimEvent.Foot>();

	// Token: 0x04000309 RID: 777
	[Header("Head/eye rotation")]
	public bool m_headRotation = true;

	// Token: 0x0400030A RID: 778
	public Transform[] m_eyes;

	// Token: 0x0400030B RID: 779
	public float m_lookWeight = 0.5f;

	// Token: 0x0400030C RID: 780
	public float m_bodyLookWeight = 0.1f;

	// Token: 0x0400030D RID: 781
	public float m_headLookWeight = 1f;

	// Token: 0x0400030E RID: 782
	public float m_eyeLookWeight;

	// Token: 0x0400030F RID: 783
	public float m_lookClamp = 0.5f;

	// Token: 0x04000310 RID: 784
	private const float m_headRotationSmoothness = 0.1f;

	// Token: 0x04000311 RID: 785
	public Transform m_lookAt;

	// Token: 0x04000312 RID: 786
	[Header("Player Female hack")]
	public bool m_femaleHack;

	// Token: 0x04000313 RID: 787
	public Transform m_leftShoulder;

	// Token: 0x04000314 RID: 788
	public Transform m_rightShoulder;

	// Token: 0x04000315 RID: 789
	public float m_femaleOffset = 0.0004f;

	// Token: 0x04000316 RID: 790
	public float m_maleOffset = 0.0007651657f;

	// Token: 0x04000317 RID: 791
	private Character m_character;

	// Token: 0x04000318 RID: 792
	private Animator m_animator;

	// Token: 0x04000319 RID: 793
	private ZNetView m_nview;

	// Token: 0x0400031A RID: 794
	private MonsterAI m_monsterAI;

	// Token: 0x0400031B RID: 795
	private VisEquipment m_visEquipment;

	// Token: 0x0400031C RID: 796
	private FootStep m_footStep;

	// Token: 0x0400031D RID: 797
	private float m_pauseTimer;

	// Token: 0x0400031E RID: 798
	private float m_pauseSpeed = 1f;

	// Token: 0x0400031F RID: 799
	private float m_sendTimer;

	// Token: 0x04000320 RID: 800
	private Vector3 m_headLookDir;

	// Token: 0x04000321 RID: 801
	private float m_lookAtWeight;

	// Token: 0x04000322 RID: 802
	private Transform m_head;

	// Token: 0x04000323 RID: 803
	private bool m_chain;

	// Token: 0x04000324 RID: 804
	private Vector3 m_lookTargetCached = Vector3.negativeInfinity;

	// Token: 0x04000325 RID: 805
	private static int s_ikGroundMask = 0;

	// Token: 0x0200022D RID: 557
	[Serializable]
	public class Foot
	{
		// Token: 0x06001ECD RID: 7885 RVA: 0x000E158C File Offset: 0x000DF78C
		public Foot(Transform t, AvatarIKGoal handle)
		{
			this.m_transform = t;
			this.m_ikHandle = handle;
			this.m_ikWeight = 0f;
		}

		// Token: 0x04001F58 RID: 8024
		public Transform m_transform;

		// Token: 0x04001F59 RID: 8025
		public AvatarIKGoal m_ikHandle;

		// Token: 0x04001F5A RID: 8026
		public float m_footDownMax = 0.4f;

		// Token: 0x04001F5B RID: 8027
		public float m_footOffset = 0.1f;

		// Token: 0x04001F5C RID: 8028
		public float m_footStepHeight = 1f;

		// Token: 0x04001F5D RID: 8029
		public float m_stabalizeDistance;

		// Token: 0x04001F5E RID: 8030
		[NonSerialized]
		public float m_ikWeight;

		// Token: 0x04001F5F RID: 8031
		[NonSerialized]
		public Vector3 m_plantPosition = Vector3.zero;

		// Token: 0x04001F60 RID: 8032
		[NonSerialized]
		public Vector3 m_plantNormal = Vector3.up;

		// Token: 0x04001F61 RID: 8033
		[NonSerialized]
		public bool m_isPlanted;
	}
}
