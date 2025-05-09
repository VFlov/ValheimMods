using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DoubleClickPower
{
    [BepInPlugin("vaffle.DoubleClickPower", "DoubleClickPower", "1.0.0")]

    public class Class1 : BaseUnityPlugin
    {
        void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
    }
    [HarmonyPatch(typeof(Player), "StartGuardianPower")]
    public class InputPatch
    {
        private static float SingleClickedTime = 0;
        private static bool Prefix()
        {
            if (SingleClickedTime == 0)
            {
                SingleClickedTime = Time.time;
                return false;
            }
            else
            {
                if (Time.time - SingleClickedTime < 0.5f)
                {

                    SingleClickedTime = 0;
                    return true;
                }
                else
                {
                    SingleClickedTime = 0;
                    return false;
                }
            }
        }
    }

}
