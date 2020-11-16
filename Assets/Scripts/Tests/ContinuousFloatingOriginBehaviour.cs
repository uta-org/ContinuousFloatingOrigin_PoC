using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using CFO.Attributes;
using CFO.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Internal;

namespace CFO.Tests
{
    public class ContinuousFloatingOriginBehaviour : MonoBehaviour
    // : Transform
    {
        // TODO: https://gitlab.com/cosmochristo/harmony/-/blob/master/PlayerMove.cs (use something from here?)
        // https://github.com/qkmaxware/Spaceworks

        public static ContinuousFloatingOriginBehaviour Instance { get; private set; }

        public static Vector3 StoredPosition { get; set; } // TODO: Private set?

        //public Vector3 rootTransformPosition;
        //public Vector3 storedPosition;

        public bool preciseColliders = true;
        public bool useGravity = false;

        private Vector3 transformPosition;
        private Vector3 lastTransformPosition;
        private Vector3 deltaTransformPosition;

        public ObservableCollection<Transform> Transforms { get; } = new ObservableCollection<Transform>();

        //public ObservableCollection<PropertyInfo> Properties { get; } = new ObservableCollection<PropertyInfo>();
        //public ObservableCollection<FieldInfo> Fields { get; } = new ObservableCollection<FieldInfo>();
        public ObservableCollection<MemberInfo> Members { get; } = new ObservableCollection<MemberInfo>();

        public Transform RootTransform { get; private set; }

        //private OriginTransform myTransform;
        private Harmony Harmony { get; set; }

        //private bool firstUpdate = true;
        private bool isRigidBody;

        private Rigidbody body;

        private void Awake()
        {
            if (Instance != null) throw new Exception("Singleton rules!");
            Instance = this;

            body = GetComponent<Rigidbody>();
            isRigidBody = body != null;

            // This doesn't work: https://www.codeproject.com/Articles/37549/CLR-Injection-Runtime-Method-Replacer
            // Thanks to: https://stackoverflow.com/a/42043003/3286975

            Harmony = new Harmony("net.z3nth10n.cfo");
            TransformInjection.Harmony = Harmony;
            TransformInjection.Patch();

            //Harmony.PatchAll(Assembly.GetExecutingAssembly());

            RootTransform = new GameObject("Root").transform;

            LookupTransforms();
            LookupTypes();
        }

        private void Update()
        {
        }

        private void FixedUpdate()
        {
            //transformPosition = StoredPosition;
            //if (firstUpdate) transformPosition.y = 0;
            //firstUpdate = false;

            //deltaTransformPosition = lastTransformPosition - transformPosition;
            //lastTransformPosition = transformPosition;

            if (isRigidBody && body.useGravity && useGravity)
                StoredPosition += Physics.gravity * Time.fixedDeltaTime;
        }

        private void LateUpdate()
        {
            transformPosition = StoredPosition;
            //if (firstUpdate) transformPosition.y = 0;
            //firstUpdate = false;

            deltaTransformPosition = lastTransformPosition - transformPosition;
            lastTransformPosition = transformPosition;

            // Update all the Vectors marked with OriginVector attribute
            foreach (var member in Members)
            {
                SetPosition(member, StoredPosition);
            }

            // Only for debugging in inspector
            //rootTransformPosition += deltaTransformPosition;
            //storedPosition = StoredPosition;

            // Only update X, Z values, @TODO: do this also for Y values
            if (!useGravity)
                deltaTransformPosition.y = 0;

            //Debug.Log(deltaTransformPosition);

            if (!preciseColliders) RootTransform.gameObject.SetActive(false);
            //RootTransform.position = StoredPosition;
            RootTransform.position += deltaTransformPosition;
            if (!preciseColliders) RootTransform.gameObject.SetActive(true);

            // Force all freezes
            if (isRigidBody && useGravity)
                body.constraints = RigidbodyConstraints.FreezeAll;
        }

        // TODO: Call this every x secs in order to find new Transforms
        private void LookupTransforms()
        {
            Transforms.AddRange(FindObjectsOfType<Transform>().Where(t => t.gameObject.GetComponent<ContinuousFloatingOriginBehaviour>() == null && t.parent == null));

            foreach (var t in Transforms) t.parent = RootTransform;

            Debug.Log($"Lookup {Transforms.Count} transforms without parent and without the {nameof(ContinuousFloatingOriginBehaviour)} component!");
        }

        private void LookupTypes()
        {
            bool CheckValidType(MemberInfo member)
            {
                //var memberType = member.GetMemberUnderlyingType();

                var originAttr = member.GetAttribute<OriginVectorAttribute>();
                if (originAttr == null)
                {
                    Debug.LogWarning("Invalid class found!");
                    return false;
                }
                if (!originAttr.IsAllowedType(member))
                {
                    Debug.LogError(
                        $"'{member.Name}' with type {member.GetMemberUnderlyingType().FullName} isn't allowed to be used with {nameof(OriginVectorAttribute)}. See usage on code.");
                    return false;
                }

                return true;
            }

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in a.GetTypes())
                {
                    foreach (var prop in t.GetProperties()
                        .Where(prop => prop.IsDefined(typeof(OriginVectorAttribute), false)))
                    {
                        Debug.Log($"Found property '{prop.Name}' in type '{t.FullName}' with defined {nameof(OriginVectorAttribute)} attribute.");
                        if (!CheckValidType(prop)) continue;
                        Members.Add(prop);
                    }

                    foreach (var field in t.GetFields()
                        .Where(field => field.IsDefined(typeof(OriginVectorAttribute), false)))
                    {
                        Debug.Log($"Found field '{field.Name}' in type '{t.FullName}' with defined {nameof(OriginVectorAttribute)} attribute.");
                        if (!CheckValidType(field)) continue;
                        Members.Add(field);
                    }
                }
            }

            Debug.Log($"Lookup {Members.Count} members!");
        }

        private void SetPosition(MemberInfo member, Vector3 value)
        {
            // TODO: Tests
            var instances = FindObjectsOfType(member.DeclaringType);
            if (instances?.Length == 0) return;
            // ReSharper disable once PossibleNullReferenceException
            foreach (var instance in instances)
            {
                member.SetValue(instance, value);
            }
        }

        //public class AssociatedMemberInfo
        //{
        //    private AssociatedMemberInfo()
        //    {
        //    }

        //    public AssociatedMemberInfo(object o, MemberInfo member)
        //    {
        //        Object = o;
        //        Member = member;
        //    }

        //    public object Object { get; }
        //    public MemberInfo Member { get; }
        //}
    }

    // https://stackoverflow.com/questions/44509636/convert-a-transform-to-a-recttransform (custom transform didn't worked)

    public static class TransformInjection
    {
        public static Harmony Harmony { get; set; }

        private static MethodInfo getMethod;

        public static bool GetRealPosition { get; set; } = true;

        private static bool obtainedMethod = false;

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
            __result = ContinuousFloatingOriginBehaviour.StoredPosition;
            GetRealPosition = true;
            return false;
        }

        private static bool set(Transform __instance, Vector3 value)
        {
            try
            {
                if (IsTransform(__instance)) return true;
                //Debug.Log("3: " + value);
                ContinuousFloatingOriginBehaviour.StoredPosition = value;
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
            var transform = ContinuousFloatingOriginBehaviour.Instance?.transform;
            if (transform == null || __instance != transform) return true;
            return false;
        }

        private static bool Translate(Transform __instance, Vector3 translation, [DefaultValue("Space.Self")] Space relativeTo)
        {
            if (IsTransform(__instance)) return true;
            //var pos = ContinuousFloatingOriginBehaviour.StoredPosition;
            // __instance.position;
            //var r = relativeTo == Space.World
            //        ? set(__instance, pos + translation)
            //        : set(__instance, pos + __instance.TransformDirection(translation));

            if (relativeTo == Space.World)
                ContinuousFloatingOriginBehaviour.StoredPosition += translation;
            else
                ContinuousFloatingOriginBehaviour.StoredPosition += __instance.TransformDirection(translation);

            //Debug.Log(relativeTo);

            //Debug.Log("1: " + translation);
            //Debug.Log("2: " + ContinuousFloatingOriginBehaviour.StoredPosition);

            // Debug.Log(pos);
            // Debug.Log(__instance.position);

            // Debug.Log(pos);
            //Debug.Log("2: " + translation);

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