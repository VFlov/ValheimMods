using System;
using System.Collections.Generic;
using Dynamics;
using UnityEngine;

// Token: 0x020001B9 RID: 441
public class SiegeMachine : MonoBehaviour
{
	// Token: 0x060019D0 RID: 6608 RVA: 0x000C081C File Offset: 0x000BEA1C
	private void Awake()
	{
		foreach (SiegeMachine.SiegePart siegePart in this.m_movingParts)
		{
			siegePart.m_position = 0f;
			siegePart.m_originalPosition = siegePart.m_gameobject.transform.localPosition;
			siegePart.m_dynamicsPosition = 0f;
			siegePart.m_floatDynamics = new FloatDynamics(this.m_dynamicsParameters, siegePart.m_dynamicsPosition);
		}
		this.m_aoe.gameObject.SetActive(false);
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
		}
	}

	// Token: 0x060019D1 RID: 6609 RVA: 0x000C08DC File Offset: 0x000BEADC
	private void Update()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateSiege(Time.deltaTime);
	}

	// Token: 0x060019D2 RID: 6610 RVA: 0x000C08F7 File Offset: 0x000BEAF7
	private void UpdateAnimPhase()
	{
		this.m_currentPart = (this.m_currentPart + 1) % this.m_movingParts.Count;
		if (this.m_currentPart == 0)
		{
			this.m_animPhase = ((this.m_animPhase == SiegeMachine.AnimPhase.Charging) ? SiegeMachine.AnimPhase.Firing : SiegeMachine.AnimPhase.Charging);
		}
	}

	// Token: 0x060019D3 RID: 6611 RVA: 0x000C0930 File Offset: 0x000BEB30
	private void UpdateSiege(float dt)
	{
		bool flag = this.m_nview != null && this.m_nview.IsValid() && this.m_nview.IsOwner();
		foreach (SiegeMachine.SiegePart siegePart in this.m_movingParts)
		{
			siegePart.m_dynamicsPosition = siegePart.m_floatDynamics.Update(dt, this.m_armAnimCurve.Evaluate(siegePart.m_position / this.m_chargeTime), float.NegativeInfinity, false);
			siegePart.m_gameobject.transform.localPosition = Vector3.Lerp(siegePart.m_originalPosition, siegePart.m_originalPosition + Vector3.back * this.m_chargeOffsetDistance, siegePart.m_dynamicsPosition);
		}
		if ((this.m_engine && !this.m_engine.IsActive()) || (this.m_wagon && (this.m_enabledWhenAttached ^ this.m_wagon.IsAttached())))
		{
			this.m_aoe.gameObject.SetActive(false);
			this.m_animPhase = SiegeMachine.AnimPhase.Charging;
			this.m_currentPart = 0;
			foreach (SiegeMachine.SiegePart siegePart2 in this.m_movingParts)
			{
				siegePart2.m_position = Mathf.MoveTowards(siegePart2.m_position, 0f, dt / 0.5f);
				if ((double)siegePart2.m_position < 0.02)
				{
					siegePart2.m_position = 0f;
				}
			}
			this.m_wasDisabledLastUpdate = true;
			return;
		}
		if (this.m_wasDisabledLastUpdate)
		{
			foreach (SiegeMachine.SiegePart siegePart3 in this.m_movingParts)
			{
				siegePart3.m_position = 0f;
			}
		}
		if (flag)
		{
			if (this.m_aoeActiveTimer > 0f)
			{
				this.m_aoeActiveTimer -= dt;
			}
			this.m_aoe.gameObject.SetActive(this.m_aoeActiveTimer >= 0f);
		}
		SiegeMachine.SiegePart siegePart4 = this.m_movingParts[this.m_currentPart];
		SiegeMachine.AnimPhase animPhase = this.m_animPhase;
		if (animPhase == SiegeMachine.AnimPhase.Charging || animPhase != SiegeMachine.AnimPhase.Firing)
		{
			if (siegePart4.m_position == 0f && flag)
			{
				this.m_chargeEffect.Create(siegePart4.m_gameobject.transform.position, Quaternion.identity, null, 1f, -1);
			}
			siegePart4.m_position += dt;
			if (siegePart4.m_position >= this.m_chargeTime)
			{
				this.UpdateAnimPhase();
			}
		}
		else
		{
			this.m_firingTimer += dt;
			if (this.m_firingTimer > this.m_hitDelay)
			{
				this.m_firingTimer = 0f;
				Terminal.Log("Firing!");
				siegePart4.m_position = 0f;
				if (flag)
				{
					this.m_punchEffect.Create(siegePart4.m_effectPoint.position, siegePart4.m_effectPoint.rotation, null, 1f, -1);
					this.m_aoeActiveTimer = 0.05f;
					this.m_aoe.gameObject.SetActive(true);
				}
				this.UpdateAnimPhase();
			}
		}
		this.m_wasDisabledLastUpdate = false;
	}

	// Token: 0x04001A51 RID: 6737
	public Smelter m_engine;

	// Token: 0x04001A52 RID: 6738
	public Vagon m_wagon;

	// Token: 0x04001A53 RID: 6739
	public bool m_enabledWhenAttached = true;

	// Token: 0x04001A54 RID: 6740
	public List<SiegeMachine.SiegePart> m_movingParts = new List<SiegeMachine.SiegePart>();

	// Token: 0x04001A55 RID: 6741
	public DynamicsParameters m_dynamicsParameters;

	// Token: 0x04001A56 RID: 6742
	private ZNetView m_nview;

	// Token: 0x04001A57 RID: 6743
	private bool m_wasDisabledLastUpdate = true;

	// Token: 0x04001A58 RID: 6744
	public float m_chargeTime = 4f;

	// Token: 0x04001A59 RID: 6745
	public float m_hitDelay = 2f;

	// Token: 0x04001A5A RID: 6746
	public float m_chargeOffsetDistance = 2f;

	// Token: 0x04001A5B RID: 6747
	public AnimationCurve m_armAnimCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	// Token: 0x04001A5C RID: 6748
	public GameObject m_aoe;

	// Token: 0x04001A5D RID: 6749
	public EffectList m_punchEffect;

	// Token: 0x04001A5E RID: 6750
	public EffectList m_chargeEffect;

	// Token: 0x04001A5F RID: 6751
	private int m_currentPart;

	// Token: 0x04001A60 RID: 6752
	private float m_firingTimer;

	// Token: 0x04001A61 RID: 6753
	private float m_aoeActiveTimer;

	// Token: 0x04001A62 RID: 6754
	private SiegeMachine.AnimPhase m_animPhase;

	// Token: 0x02000382 RID: 898
	[Serializable]
	public class SiegePart
	{
		// Token: 0x04002671 RID: 9841
		public GameObject m_gameobject;

		// Token: 0x04002672 RID: 9842
		public Transform m_effectPoint;

		// Token: 0x04002673 RID: 9843
		[NonSerialized]
		public float m_position;

		// Token: 0x04002674 RID: 9844
		[NonSerialized]
		public FloatDynamics m_floatDynamics;

		// Token: 0x04002675 RID: 9845
		[NonSerialized]
		public float m_dynamicsPosition;

		// Token: 0x04002676 RID: 9846
		[NonSerialized]
		public Vector3 m_originalPosition;
	}

	// Token: 0x02000383 RID: 899
	private enum AnimPhase
	{
		// Token: 0x04002678 RID: 9848
		Charging,
		// Token: 0x04002679 RID: 9849
		Firing
	}
}
