using System;
using System.IO;
using UnityEngine;

// Token: 0x02000111 RID: 273
public class CustomLogger : MonoBehaviour
{
	// Token: 0x06001138 RID: 4408 RVA: 0x0008064C File Offset: 0x0007E84C
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Startup()
	{
		if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
		{
			string text = Application.persistentDataPath + "/Player.log";
			string text2 = Application.persistentDataPath + "/Player-prev.log";
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
			if (File.Exists(text))
			{
				File.Copy(text, text2);
			}
			CustomLogger.logWriter = new StreamWriter(text, false);
			string str = "~/Library/Logs/IronGate/Valheim/Player.log";
			CustomLogger.logWriter.WriteLine("This log file has Unity's logs redirected from the default location, for the original please see " + str);
			Application.logMessageReceived += CustomLogger.HandleLog;
		}
	}

	// Token: 0x06001139 RID: 4409 RVA: 0x000806DD File Offset: 0x0007E8DD
	private static void HandleLog(string logString, string stackTrace, LogType type)
	{
		if (!logString.EndsWith("\n"))
		{
			logString += "\n";
		}
		CustomLogger.logWriter.Write(logString);
		CustomLogger.logWriter.Flush();
	}

	// Token: 0x04001063 RID: 4195
	private static StreamWriter logWriter;

	// Token: 0x04001064 RID: 4196
	private string logFilePath = "";
}
