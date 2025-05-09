using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200001C RID: 28
public class FootStep : MonoBehaviour, IMonoUpdater
{
	// Token: 0x06000270 RID: 624 RVA: 0x00015F90 File Offset: 0x00014190
	private void Start()
	{
		this.m_animator = base.GetComponentInChildren<Animator>();
		this.m_character = base.GetComponent<Character>();
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_footstep = this.m_animator.GetFloat(FootStep.s_footstepID);
		if (this.m_pieceLayer == 0)
		{
			this.m_pieceLayer = LayerMask.NameToLayer("piece");
		}
		Character character = this.m_character;
		character.m_onLand = (Action<Vector3>)Delegate.Combine(character.m_onLand, new Action<Vector3>(this.OnLand));
		this.m_lastPosition = this.m_character.transform.position;
		if (this.m_nview.IsValid())
		{
			this.m_nview.Register<int, Vector3>("Step", new Action<long, int, Vector3>(this.RPC_Step));
		}
	}

	// Token: 0x06000271 RID: 625 RVA: 0x00016055 File Offset: 0x00014255
	private void OnEnable()
	{
		FootStep.Instances.Add(this);
	}

	// Token: 0x06000272 RID: 626 RVA: 0x00016062 File Offset: 0x00014262
	private void OnDisable()
	{
		FootStep.Instances.Remove(this);
	}

	// Token: 0x06000273 RID: 627 RVA: 0x00016070 File Offset: 0x00014270
	public void CustomUpdate(float dt, float time)
	{
		if (this.m_nview == null || !this.m_nview.IsOwner())
		{
			return;
		}
		this.UpdateFootstep(dt);
		this.UpdateFootlessFootstep(dt);
	}

	// Token: 0x06000274 RID: 628 RVA: 0x0001609C File Offset: 0x0001429C
	private void UpdateFootstep(float dt)
	{
		if (this.m_feet.Length == 0)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		if (Vector3.Distance(base.transform.position, mainCamera.transform.position) > this.m_footstepCullDistance)
		{
			return;
		}
		this.UpdateFootstepCurveTrigger(dt);
	}

	// Token: 0x06000275 RID: 629 RVA: 0x000160F0 File Offset: 0x000142F0
	private void UpdateFootlessFootstep(float dt)
	{
		if (this.m_feet.Length != 0 || !this.m_footlessFootsteps)
		{
			return;
		}
		Vector3 position = base.transform.position;
		if (!this.m_character.IsOnGround())
		{
			this.m_distanceAccumulator = 0f;
		}
		else
		{
			this.m_distanceAccumulator += Vector3.Distance(position, this.m_lastPosition);
		}
		this.m_lastPosition = position;
		if (this.m_distanceAccumulator > this.m_footlessTriggerDistance)
		{
			this.m_distanceAccumulator -= this.m_footlessTriggerDistance;
			this.OnFoot(base.transform);
		}
	}

	// Token: 0x06000276 RID: 630 RVA: 0x00016184 File Offset: 0x00014384
	private void UpdateFootstepCurveTrigger(float dt)
	{
		this.m_footstepTimer += dt;
		float @float = this.m_animator.GetFloat(FootStep.s_footstepID);
		if (Utils.SignDiffers(@float, this.m_footstep) && Mathf.Max(Mathf.Abs(this.m_animator.GetFloat(FootStep.s_forwardSpeedID)), Mathf.Abs(this.m_animator.GetFloat(FootStep.s_sidewaySpeedID))) > 0.2f && this.m_footstepTimer > 0.2f)
		{
			this.m_footstepTimer = 0f;
			this.OnFoot();
		}
		this.m_footstep = @float;
	}

	// Token: 0x06000277 RID: 631 RVA: 0x0001621C File Offset: 0x0001441C
	private Transform FindActiveFoot()
	{
		Transform transform = null;
		float num = 9999f;
		Vector3 forward = base.transform.forward;
		foreach (Transform transform2 in this.m_feet)
		{
			if (!(transform2 == null))
			{
				Vector3 rhs = transform2.position - base.transform.position;
				float num2 = Vector3.Dot(forward, rhs);
				if (num2 > num || transform == null)
				{
					transform = transform2;
					num = num2;
				}
			}
		}
		return transform;
	}

	// Token: 0x06000278 RID: 632 RVA: 0x000162A0 File Offset: 0x000144A0
	private Transform FindFoot(string name)
	{
		foreach (Transform transform in this.m_feet)
		{
			if (transform.gameObject.name == name)
			{
				return transform;
			}
		}
		return null;
	}

	// Token: 0x06000279 RID: 633 RVA: 0x000162DC File Offset: 0x000144DC
	public void OnFoot()
	{
		Transform foot = this.FindActiveFoot();
		this.OnFoot(foot);
	}

	// Token: 0x0600027A RID: 634 RVA: 0x000162F8 File Offset: 0x000144F8
	public void OnFoot(string name)
	{
		Transform transform = this.FindFoot(name);
		if (transform == null)
		{
			ZLog.LogWarning("FAiled to find foot:" + name);
			return;
		}
		this.OnFoot(transform);
	}

	// Token: 0x0600027B RID: 635 RVA: 0x00016330 File Offset: 0x00014530
	private void OnLand(Vector3 point)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		FootStep.GroundMaterial groundMaterial = this.GetGroundMaterial(this.m_character, point);
		int num = this.FindBestStepEffect(groundMaterial, FootStep.MotionType.Land);
		if (num != -1)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "Step", new object[]
			{
				num,
				point
			});
		}
	}

	// Token: 0x0600027C RID: 636 RVA: 0x00016394 File Offset: 0x00014594
	private void OnFoot(Transform foot)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		Vector3 vector = (foot != null) ? foot.position : base.transform.position;
		FootStep.MotionType motionType = FootStep.GetMotionType(this.m_character);
		FootStep.GroundMaterial groundMaterial = this.GetGroundMaterial(this.m_character, vector);
		int num = this.FindBestStepEffect(groundMaterial, motionType);
		if (num != -1)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "Step", new object[]
			{
				num,
				vector
			});
		}
	}

	// Token: 0x0600027D RID: 637 RVA: 0x00016420 File Offset: 0x00014620
	private static void PurgeOldEffects()
	{
		while (FootStep.s_stepInstances.Count > 30)
		{
			GameObject gameObject = FootStep.s_stepInstances.Dequeue();
			if (gameObject)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
		}
	}

	// Token: 0x0600027E RID: 638 RVA: 0x00016458 File Offset: 0x00014658
	private void DoEffect(FootStep.StepEffect effect, Vector3 point)
	{
		foreach (GameObject gameObject in effect.m_effectPrefabs)
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, point, base.transform.rotation);
			FootStep.s_stepInstances.Enqueue(gameObject2);
			if (gameObject2.GetComponent<ZNetView>() != null)
			{
				ZLog.LogWarning(string.Concat(new string[]
				{
					"Foot step effect ",
					effect.m_name,
					" prefab ",
					gameObject.name,
					" in ",
					this.m_character.gameObject.name,
					" should not contain a ZNetView component"
				}));
			}
		}
		FootStep.PurgeOldEffects();
	}

	// Token: 0x0600027F RID: 639 RVA: 0x0001650C File Offset: 0x0001470C
	private void RPC_Step(long sender, int effectIndex, Vector3 point)
	{
		FootStep.StepEffect effect = this.m_effects[effectIndex];
		this.DoEffect(effect, point);
	}

	// Token: 0x06000280 RID: 640 RVA: 0x0001652E File Offset: 0x0001472E
	private static FootStep.MotionType GetMotionType(Character character)
	{
		if (character.IsWalking())
		{
			return FootStep.MotionType.Walk;
		}
		if (character.IsSwimming())
		{
			return FootStep.MotionType.Swimming;
		}
		if (character.IsWallRunning())
		{
			return FootStep.MotionType.Climbing;
		}
		if (character.IsRunning())
		{
			return FootStep.MotionType.Run;
		}
		if (character.IsSneaking())
		{
			return FootStep.MotionType.Sneak;
		}
		return FootStep.MotionType.Jog;
	}

	// Token: 0x06000281 RID: 641 RVA: 0x00016568 File Offset: 0x00014768
	private FootStep.GroundMaterial GetGroundMaterial(Character character, Vector3 point)
	{
		if (character.InWater())
		{
			return FootStep.GroundMaterial.Water;
		}
		if (character.InLiquid())
		{
			return FootStep.GroundMaterial.Tar;
		}
		Collider lastGroundCollider = character.GetLastGroundCollider();
		if (lastGroundCollider == null)
		{
			return FootStep.GroundMaterial.Default;
		}
		Heightmap component = lastGroundCollider.GetComponent<Heightmap>();
		if (component != null)
		{
			Vector3 lastGroundNormal = character.GetLastGroundNormal();
			return component.GetGroundMaterial(lastGroundNormal, point, 0.6f);
		}
		if (lastGroundCollider.gameObject.layer != this.m_pieceLayer)
		{
			return FootStep.GroundMaterial.Default;
		}
		WearNTear componentInParent = lastGroundCollider.GetComponentInParent<WearNTear>();
		if (!componentInParent)
		{
			return FootStep.GroundMaterial.Default;
		}
		switch (componentInParent.m_materialType)
		{
		case WearNTear.MaterialType.Wood:
			return FootStep.GroundMaterial.Wood;
		case WearNTear.MaterialType.Stone:
		case WearNTear.MaterialType.Marble:
			return FootStep.GroundMaterial.Stone;
		case WearNTear.MaterialType.Iron:
			return FootStep.GroundMaterial.Metal;
		case WearNTear.MaterialType.HardWood:
			return FootStep.GroundMaterial.Wood;
		default:
			return FootStep.GroundMaterial.Default;
		}
	}

	// Token: 0x06000282 RID: 642 RVA: 0x0001661C File Offset: 0x0001481C
	public void FindJoints()
	{
		ZLog.Log("Finding joints");
		Transform transform = Utils.FindChild(base.transform, "LeftFootFront", Utils.IterativeSearchType.DepthFirst);
		Transform transform2 = Utils.FindChild(base.transform, "RightFootFront", Utils.IterativeSearchType.DepthFirst);
		Transform transform3 = Utils.FindChild(base.transform, "LeftFoot", Utils.IterativeSearchType.DepthFirst);
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "LeftFootBack", Utils.IterativeSearchType.DepthFirst);
		}
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "l_foot", Utils.IterativeSearchType.DepthFirst);
		}
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "Foot.l", Utils.IterativeSearchType.DepthFirst);
		}
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "foot.l", Utils.IterativeSearchType.DepthFirst);
		}
		Transform transform4 = Utils.FindChild(base.transform, "RightFoot", Utils.IterativeSearchType.DepthFirst);
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "RightFootBack", Utils.IterativeSearchType.DepthFirst);
		}
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "r_foot", Utils.IterativeSearchType.DepthFirst);
		}
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "Foot.r", Utils.IterativeSearchType.DepthFirst);
		}
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "foot.r", Utils.IterativeSearchType.DepthFirst);
		}
		List<Transform> list = new List<Transform>();
		if (transform)
		{
			list.Add(transform);
		}
		if (transform2)
		{
			list.Add(transform2);
		}
		if (transform3)
		{
			list.Add(transform3);
		}
		if (transform4)
		{
			list.Add(transform4);
		}
		this.m_feet = list.ToArray();
	}

	// Token: 0x06000283 RID: 643 RVA: 0x000167A8 File Offset: 0x000149A8
	private int FindBestStepEffect(FootStep.GroundMaterial material, FootStep.MotionType motion)
	{
		FootStep.StepEffect stepEffect = null;
		int result = -1;
		for (int i = 0; i < this.m_effects.Count; i++)
		{
			FootStep.StepEffect stepEffect2 = this.m_effects[i];
			if (((stepEffect2.m_material & material) != FootStep.GroundMaterial.None || (stepEffect == null && (stepEffect2.m_material & FootStep.GroundMaterial.Default) != FootStep.GroundMaterial.None)) && (stepEffect2.m_motionType & motion) != (FootStep.MotionType)0)
			{
				stepEffect = stepEffect2;
				result = i;
			}
		}
		return result;
	}

	// Token: 0x17000009 RID: 9
	// (get) Token: 0x06000284 RID: 644 RVA: 0x00016802 File Offset: 0x00014A02
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04000391 RID: 913
	[Header("Footless")]
	public bool m_footlessFootsteps;

	// Token: 0x04000392 RID: 914
	public float m_footlessTriggerDistance = 1f;

	// Token: 0x04000393 RID: 915
	[Space(16f)]
	public float m_footstepCullDistance = 20f;

	// Token: 0x04000394 RID: 916
	public List<FootStep.StepEffect> m_effects = new List<FootStep.StepEffect>();

	// Token: 0x04000395 RID: 917
	public Transform[] m_feet = Array.Empty<Transform>();

	// Token: 0x04000396 RID: 918
	private static readonly int s_footstepID = ZSyncAnimation.GetHash("footstep");

	// Token: 0x04000397 RID: 919
	private static readonly int s_forwardSpeedID = ZSyncAnimation.GetHash("forward_speed");

	// Token: 0x04000398 RID: 920
	private static readonly int s_sidewaySpeedID = ZSyncAnimation.GetHash("sideway_speed");

	// Token: 0x04000399 RID: 921
	private static readonly Queue<GameObject> s_stepInstances = new Queue<GameObject>();

	// Token: 0x0400039A RID: 922
	private float m_footstep;

	// Token: 0x0400039B RID: 923
	private float m_footstepTimer;

	// Token: 0x0400039C RID: 924
	private int m_pieceLayer;

	// Token: 0x0400039D RID: 925
	private float m_distanceAccumulator;

	// Token: 0x0400039E RID: 926
	private Vector3 m_lastPosition;

	// Token: 0x0400039F RID: 927
	private const float c_MinFootstepInterval = 0.2f;

	// Token: 0x040003A0 RID: 928
	private const int c_MaxFootstepInstances = 30;

	// Token: 0x040003A1 RID: 929
	private Animator m_animator;

	// Token: 0x040003A2 RID: 930
	private Character m_character;

	// Token: 0x040003A3 RID: 931
	private ZNetView m_nview;

	// Token: 0x02000230 RID: 560
	[Flags]
	public enum MotionType
	{
		// Token: 0x04001F6B RID: 8043
		Jog = 1,
		// Token: 0x04001F6C RID: 8044
		Run = 2,
		// Token: 0x04001F6D RID: 8045
		Sneak = 4,
		// Token: 0x04001F6E RID: 8046
		Climbing = 8,
		// Token: 0x04001F6F RID: 8047
		Swimming = 16,
		// Token: 0x04001F70 RID: 8048
		Land = 32,
		// Token: 0x04001F71 RID: 8049
		Walk = 64
	}

	// Token: 0x02000231 RID: 561
	[Flags]
	public enum GroundMaterial
	{
		// Token: 0x04001F73 RID: 8051
		None = 0,
		// Token: 0x04001F74 RID: 8052
		Default = 1,
		// Token: 0x04001F75 RID: 8053
		Water = 2,
		// Token: 0x04001F76 RID: 8054
		Stone = 4,
		// Token: 0x04001F77 RID: 8055
		Wood = 8,
		// Token: 0x04001F78 RID: 8056
		Snow = 16,
		// Token: 0x04001F79 RID: 8057
		Mud = 32,
		// Token: 0x04001F7A RID: 8058
		Grass = 64,
		// Token: 0x04001F7B RID: 8059
		GenericGround = 128,
		// Token: 0x04001F7C RID: 8060
		Metal = 256,
		// Token: 0x04001F7D RID: 8061
		Tar = 512,
		// Token: 0x04001F7E RID: 8062
		Ashlands = 1024,
		// Token: 0x04001F7F RID: 8063
		Lava = 2048,
		// Token: 0x04001F80 RID: 8064
		Everything = 4095
	}

	// Token: 0x02000232 RID: 562
	[Serializable]
	public class StepEffect
	{
		// Token: 0x04001F81 RID: 8065
		public string m_name = "";

		// Token: 0x04001F82 RID: 8066
		[BitMask(typeof(FootStep.MotionType))]
		public FootStep.MotionType m_motionType = FootStep.MotionType.Jog;

		// Token: 0x04001F83 RID: 8067
		[BitMask(typeof(FootStep.GroundMaterial))]
		public FootStep.GroundMaterial m_material = FootStep.GroundMaterial.Default;

		// Token: 0x04001F84 RID: 8068
		public GameObject[] m_effectPrefabs = Array.Empty<GameObject>();
	}
}
