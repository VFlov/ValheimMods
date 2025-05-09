using System;
using SoftReferenceableAssets.SceneManagement;
using UnityEngine;

// Token: 0x02000114 RID: 276
public class EntryPointSceneLoader : MonoBehaviour
{
	// Token: 0x06001146 RID: 4422 RVA: 0x00080A50 File Offset: 0x0007EC50
	private void Start()
	{
		ZLog.Log("Loading first scene");
		SceneManager.LoadScene(this.m_scene, LoadSceneMode.Single);
	}

	// Token: 0x0400106C RID: 4204
	[SerializeField]
	private SceneReference m_scene;
}
