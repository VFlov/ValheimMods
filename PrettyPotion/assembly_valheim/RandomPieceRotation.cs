using System;
using UnityEngine;

// Token: 0x020001AB RID: 427
public class RandomPieceRotation : MonoBehaviour
{
	// Token: 0x060018FF RID: 6399 RVA: 0x000BAECC File Offset: 0x000B90CC
	private void Awake()
	{
		Vector3 position = base.transform.position;
		int seed = (int)position.x * (int)(position.y * 10f) * (int)(position.z * 100f);
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		float x = this.m_rotateX ? ((float)UnityEngine.Random.Range(0, this.m_stepsX) * 360f / (float)this.m_stepsX) : 0f;
		float y = this.m_rotateY ? ((float)UnityEngine.Random.Range(0, this.m_stepsY) * 360f / (float)this.m_stepsY) : 0f;
		float z = this.m_rotateZ ? ((float)UnityEngine.Random.Range(0, this.m_stepsZ) * 360f / (float)this.m_stepsZ) : 0f;
		base.transform.localRotation = Quaternion.Euler(x, y, z);
		UnityEngine.Random.state = state;
	}

	// Token: 0x04001950 RID: 6480
	public bool m_rotateX;

	// Token: 0x04001951 RID: 6481
	public bool m_rotateY;

	// Token: 0x04001952 RID: 6482
	public bool m_rotateZ;

	// Token: 0x04001953 RID: 6483
	public int m_stepsX = 4;

	// Token: 0x04001954 RID: 6484
	public int m_stepsY = 4;

	// Token: 0x04001955 RID: 6485
	public int m_stepsZ = 4;
}
