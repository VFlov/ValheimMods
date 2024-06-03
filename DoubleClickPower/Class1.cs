using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DoubleClickPower
{
    [BepInPlugin("vsp.DoubleClickPower", "DoubleClickPower", "1.0.0")]

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
        private static float clicktime = 0;
        private static bool Prefix()
        {
            if (clicktime == 0)
            {
                clicktime = Time.time;
                return false;
            }
            else
            {
                if (Time.time - clicktime < 0.5f)
                {

                    clicktime = 0;
                    return true;
                }
                else
                {
                    clicktime = 0;
                    return false;
                }
            }
        }
    }

}
