using System;
using System.Reflection;
using CFO.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Internal;
using static CFO.Tests.ContinuousFloatingOriginBehaviour;

namespace CFO.Tests
{
    public static class TransformInjection
    {
        public static Harmony Harmony { get; set; }

        private static MethodInfo getMethod;

        public static bool GetRealPosition { get; set; } = true;

        private static bool obtainedMethod = false;

        public static bool DebugMode { get; set; }

        // This can't be done: https://stackoverflow.com/a/2957428/3286975
        //public static MethodInfo GetMethod
        //{
        //    get
        //    {
        //        if (getMethod == null && ContinuousFloatingOriginBehaviour.Instance != null && !obtainedMethod)
        //        {
        //            var transform = ContinuousFloatingOriginBehaviour.Instance.transform;
        //            Debug.Log(transform.GetType().FullName);
        //            getMethod = transform.GetType().GetMethod("get_position_Injected", BindingFlags.NonPublic);
        //            if (getMethod == null) Debug.LogWarning("Couldn't find get_position_Injected method on Transform.");
        //            obtainedMethod = true;
        //        }
        //        return getMethod;
        //    }
        //}

        private static bool get(ref Vector3 __result)
        {
            // Keep this, but we won't override the get method by the moment.

            if (GetRealPosition) return true;
            //if (ContinuousFloatingOriginBehaviour.Instance != null) GetMethod?.InvokeWithOutParam(ContinuousFloatingOriginBehaviour.Instance.transform, out __result);
            __result = StoredPosition;
            GetRealPosition = true;
            return false;
        }

        private static bool set(Transform __instance, Vector3 value)
        {
            try
            {
                if (IsTransform(__instance)) return true;
                if (DebugMode) Debug.Log("set: " + value);
                StoredPosition = value;
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex); // TODO: fix MissingReferenceException
                /*
                    MissingReferenceException: The object of type 'ContinuousFloatingOriginBehaviour' has been destroyed but you are still trying to access it.
                    Your script should either check if it is null or you should not destroy the object.
                    CFO.Tests.TransformInjection.set (UnityEngine.Transform __instance, UnityEngine.Vector3 value) (at Assets/Scripts/Tests/ContinuousFloatingOriginBehaviour.cs:211)
                    UnityEngine.Debug:LogException(Exception)
                    CFO.Tests.TransformInjection:set(Transform, Vector3) (at Assets/Scripts/Tests/ContinuousFloatingOriginBehaviour.cs:219)
                    UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)
                 */
                return true;
            }
        }

        private static bool IsTransform(Transform __instance)
        {
            var transform = Instance?.transform;
            if (transform == null || __instance != transform) return true;
            return false;
        }

        private static bool Translate(Transform __instance, Vector3 translation, [DefaultValue("Space.Self")] Space relativeTo)
        {
            if (IsTransform(__instance)) return true;

            if (relativeTo == Space.World)
                StoredPosition += translation;
            else
                StoredPosition += __instance.TransformDirection(translation);

            if (DebugMode) Debug.Log($"Translation: {translation}\n" +
                                     $"Relative To: {relativeTo}\n" +
                                     $"Stored Position: {StoredPosition}\n" +
                                     $"Instance Position: {__instance.position}");

            return false;
        }

        public static void Patch(bool patchGet = true, bool patchSet = true)
        {
            if (patchGet)
                Harmony.Patch(
                    typeof(Transform).GetProperty("position")?.GetGetMethod(false),
                    new HarmonyMethod(typeof(TransformInjection).GetMethod("get", BindingFlags.NonPublic | BindingFlags.Static)));

            if (patchSet)
                Harmony.Patch(typeof(Transform).GetProperty("position")?.GetSetMethod(false),
                    new HarmonyMethod(typeof(TransformInjection).GetMethod("set", BindingFlags.NonPublic | BindingFlags.Static)));

            Harmony.Patch(typeof(Transform).GetMethods()
                    .FindMethod("Translate", typeof(Vector3), typeof(Space)),
                new HarmonyMethod(typeof(TransformInjection).GetMethod("Translate", BindingFlags.NonPublic | BindingFlags.Static)));
        }
    }
}