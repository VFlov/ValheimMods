using System;
using UnityEngine;

// Token: 0x02000068 RID: 104
public class StateController : StateMachineBehaviour
{
	// Token: 0x060006C4 RID: 1732 RVA: 0x00038690 File Offset: 0x00036890
	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (this.m_enterEffect.HasEffects())
		{
			this.m_enterEffect.Create(this.GetEffectPos(animator), animator.transform.rotation, null, 1f, -1);
		}
		if (this.m_enterDisableChildren)
		{
			for (int i = 0; i < animator.transform.childCount; i++)
			{
				animator.transform.GetChild(i).gameObject.SetActive(false);
			}
		}
		if (this.m_enterEnableChildren)
		{
			for (int j = 0; j < animator.transform.childCount; j++)
			{
				animator.transform.GetChild(j).gameObject.SetActive(true);
			}
		}
	}

	// Token: 0x060006C5 RID: 1733 RVA: 0x0003873C File Offset: 0x0003693C
	private Vector3 GetEffectPos(Animator animator)
	{
		if (this.m_effectJoint.Length == 0)
		{
			return animator.transform.position;
		}
		if (this.m_effectJoinT == null)
		{
			this.m_effectJoinT = Utils.FindChild(animator.transform, this.m_effectJoint, Utils.IterativeSearchType.DepthFirst);
		}
		return this.m_effectJoinT.position;
	}

	// Token: 0x040007D7 RID: 2007
	public string m_effectJoint = "";

	// Token: 0x040007D8 RID: 2008
	public EffectList m_enterEffect = new EffectList();

	// Token: 0x040007D9 RID: 2009
	public bool m_enterDisableChildren;

	// Token: 0x040007DA RID: 2010
	public bool m_enterEnableChildren;

	// Token: 0x040007DB RID: 2011
	public GameObject[] m_enterDisable = new GameObject[0];

	// Token: 0x040007DC RID: 2012
	public GameObject[] m_enterEnable = new GameObject[0];

	// Token: 0x040007DD RID: 2013
	private Transform m_effectJoinT;
}
