using System.Reflection;
using CFO.Tests;
using HarmonyLib;
using UnityEngine;

public class HarmonyTest : MonoBehaviour
{
    public bool overrideGet = true,
                overrideSet = true;

    // Start is called before the first frame update
    private void Awake()
    {
        var Harmony = new Harmony("net.z3nth10n.cfo");

        if (overrideGet)
            Harmony.Patch(
                typeof(Transform).GetProperty("position")?.GetGetMethod(false),
                new HarmonyMethod(typeof(TransformInjection).GetMethod("get", BindingFlags.NonPublic | BindingFlags.Static)));

        if (overrideSet)
            Harmony.Patch(typeof(Transform).GetProperty("position")?.GetSetMethod(false),
                new HarmonyMethod(typeof(TransformInjection).GetMethod("set", BindingFlags.NonPublic | BindingFlags.Static)));

        #region "Old code"

        //Harmony.Patch(
        //    typeof(Transform).GetProperty("position")?.GetGetMethod(false),
        //        new HarmonyMethod(typeof(TransformInjection).GetProperty("position", BindingFlags.NonPublic | BindingFlags.Static)?.GetGetMethod(false)));
        //Harmony.Patch(typeof(Transform).GetProperty("position")?.GetSetMethod(false),
        //    new HarmonyMethod(typeof(TransformInjection).GetProperty("position", BindingFlags.NonPublic | BindingFlags.Static)
        //        ?.GetSetMethod(false)));

        #endregion "Old code"
    }
}