using System;
using UnityEngine;

// Token: 0x020001CA RID: 458
public class TestCollision : MonoBehaviour
{
	// Token: 0x06001A85 RID: 6789 RVA: 0x000C5722 File Offset: 0x000C3922
	private void Start()
	{
	}

	// Token: 0x06001A86 RID: 6790 RVA: 0x000C5724 File Offset: 0x000C3924
	private void Update()
	{
	}

	// Token: 0x06001A87 RID: 6791 RVA: 0x000C5728 File Offset: 0x000C3928
	public void OnCollisionEnter(Collision info)
	{
		ZLog.Log("Hit by " + info.rigidbody.gameObject.name);
		ZLog.Log("rel vel " + info.relativeVelocity.ToString() + " " + info.relativeVelocity.ToString());
		ZLog.Log("Vel " + info.rigidbody.velocity.ToString() + "  " + info.rigidbody.angularVelocity.ToString());
	}
}
