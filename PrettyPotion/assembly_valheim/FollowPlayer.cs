using System;
using UnityEngine;

// Token: 0x02000052 RID: 82
public class FollowPlayer : MonoBehaviour
{
	// Token: 0x0600060D RID: 1549 RVA: 0x0003315C File Offset: 0x0003135C
	private void LateUpdate()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (Player.m_localPlayer == null || mainCamera == null)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		if (this.m_follow == FollowPlayer.Type.Camera || GameCamera.InFreeFly())
		{
			vector = mainCamera.transform.position;
		}
		else if (this.m_follow == FollowPlayer.Type.Average)
		{
			if (GameCamera.InFreeFly())
			{
				vector = mainCamera.transform.position;
			}
			else
			{
				vector = (mainCamera.transform.position + Player.m_localPlayer.transform.position) * 0.5f;
			}
		}
		else
		{
			vector = Player.m_localPlayer.transform.position;
		}
		if (this.m_lockYPos)
		{
			vector.y = base.transform.position.y;
		}
		if (vector.y > this.m_maxYPos)
		{
			vector.y = this.m_maxYPos;
		}
		base.transform.position = vector;
	}

	// Token: 0x040006D6 RID: 1750
	public FollowPlayer.Type m_follow = FollowPlayer.Type.Camera;

	// Token: 0x040006D7 RID: 1751
	public bool m_lockYPos;

	// Token: 0x040006D8 RID: 1752
	public float m_maxYPos = 1000000f;

	// Token: 0x02000250 RID: 592
	public enum Type
	{
		// Token: 0x04002015 RID: 8213
		Player,
		// Token: 0x04002016 RID: 8214
		Camera,
		// Token: 0x04002017 RID: 8215
		Average
	}
}
