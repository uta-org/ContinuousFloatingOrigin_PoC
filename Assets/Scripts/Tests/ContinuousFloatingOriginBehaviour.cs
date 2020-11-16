using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using CFO.Attributes;
using CFO.Utils;
using HarmonyLib;
using UnityEngine;
using static CFO.Tests.TransformInjection;

namespace CFO.Tests
{
    // [RequireComponent(typeof(CharacterController))] // TODO: Require also RigidBody, I need to look if RigidBodies are always present with CharacterControllers.
    public class ContinuousFloatingOriginBehaviour : MonoBehaviour
    // : Transform
    {
#if DEBUG
        public const bool DEBUG_MODE = true;
#else
        public const bool DEBUG_MODE = false;
#endif

        // TODO: https://gitlab.com/cosmochristo/harmony/-/blob/master/PlayerMove.cs (use something from here?)
        // https://github.com/qkmaxware/Spaceworks

        public static ContinuousFloatingOriginBehaviour Instance { get; private set; }

        public static Vector3 StoredPosition { get; set; } // TODO: Private set?

        //public Vector3 rootTransformPosition;
        public Vector3 storedPosition;

        public bool preciseColliders = true;
        public bool simulateGravity = true;

        private bool lastUseGravity;

        public bool debugMode = DEBUG_MODE;

        private Vector3 transformPosition;
        private Vector3 lastTransformPosition;
        private Vector3 deltaTransformPosition;

        public ObservableCollection<Transform> Transforms { get; } = new ObservableCollection<Transform>();
        public ObservableCollection<MemberInfo> Members { get; } = new ObservableCollection<MemberInfo>();

        public Transform RootTransform { get; private set; }

        //private OriginTransform myTransform;
        private Harmony Harmony { get; set; }

        //private bool firstUpdate = true;
        private bool isRigidBody;

        private Rigidbody body;
        private CharacterController controller;

        #region "PlayerMove.cs"

        // Multiplier to each movement input
        private readonly float speed = 7.0f;

        //private GameObject player;
        //private GameObject SceneRoot;

        // Layer mask for ray casting
        private int layerMask;

        // Horizontal movement deltas
        private float deltaX;

        private float deltaZ;
        private float speedAdj;

        // Current reverse transform
        private Vector3 reverseMovement;

        // Rotated reverse transform
        private Vector3 rotated_transform = new Vector3(0f, 0f, 0f);

        private readonly Vector3 player_position = new Vector3(0f, 0f, 0f);
        private RaycastHit rayCollision;

        #endregion "PlayerMove.cs"

        private void Awake()
        {
            if (Instance != null) throw new Exception("Singleton rules!");
            Instance = this;

            DebugMode = debugMode;

            controller = GetComponent<CharacterController>();

            body = GetComponent<Rigidbody>();
            isRigidBody = body != null;

            // This doesn't work: https://www.codeproject.com/Articles/37549/CLR-Injection-Runtime-Method-Replacer
            // Thanks to: https://stackoverflow.com/a/42043003/3286975

            Harmony = new Harmony("net.z3nth10n.cfo");
            TransformInjection.Harmony = Harmony;
            Patch();

            RootTransform = new GameObject("Root").transform;

            LookupTransforms();
            LookupTypes();
        }

        /// <summary>
        /// Do not use FixedUpdate here because performance drops dramatically (on dual core i5 macbook pro).
        /// <seealso cref="PlayerView.cs"/>
        /// </summary>
        private void Update()
        {
            if (isRigidBody && lastUseGravity != body.useGravity)
            {
                // TODO: Check also for constraints changes
                ToggleCharacterController(body.useGravity);
                lastUseGravity = body.useGravity;
            }

            if (IsFlyMode()) return; // Don't execute if CharacterController, but this case isn't covered yet so @todo.

            // Get the horizontal movement changes from keyboard and
            // negate them so we can move scene in reverse
            deltaX = -Input.GetAxis("Horizontal");
            deltaZ = -Input.GetAxis("Vertical");

            // Two alternatives to running script flat out:
            // 1. Only process floating origin movement if there is navigation input
            //   change and it is above noise/shake threshold.
            //   Performance: don't really want a sqr root here -
            //   or even a squares comparision.
            //if ((Mathf.Abs(deltaX) + Mathf.Abs(deltaZ)) > NAV_SHAKE_THRESHOLD)
            //{
            // or
            // 2. Time based method:

            speedAdj = Time.deltaTime * speed;

            // Scene reverse transform for floating origin navigation.
            // Make movement delta proportional to time since last move and speed factor.
            // Peformance: changed this to assignment so no mem alloc and GC needed, and
            // 2 multiplies a bit faster than multiply by 3D vector.
            reverseMovement.x = deltaX * speedAdj;
            reverseMovement.z = deltaZ * speedAdj;

            /*// Uncomment to do player collision detection.
              //If player collided with close object then ...
            if (Physics.Raycast(player_position, player.transform.TransformDirection(Vector3.forward), out rayCollision, COLLISION_DISTANCE, layerMask)
                && (rayCollision.distance < COLLISION_DISTANCE))
            {
                /// ... bounce back a little from collision
                transform.Translate(-rotated_transform*COLLISION_ADJUST);
            }
            else // no collision, so move scene in reverse
            {*/
            // use player camera rotation to modify reverse movement vector so that player forward corresponds to forward movement input
            rotated_transform = Quaternion.Euler(transform.localEulerAngles) * reverseMovement;

            // SetActive logic is done on LateUpdate.

            // Use this switch to reduce performance overhead:
            // no need for unity to do collider processing when translacte whole scene
            //SceneRoot.SetActive(false);

            // Move the scene to the new position by changing scene parent object transform.
            transform.Translate(rotated_transform);

            // restore active scene
            //SceneRoot.SetActive(true);
            /*}*/
        }

        private void FixedUpdate()
        {
            if (simulateGravity && !IsFlyMode())
            {
                var delta = Physics.gravity * Time.fixedDeltaTime;

                StoredPosition -= delta;
                RootTransform.position -= delta;
            }
        }

        private void LateUpdate()
        {
            transformPosition = StoredPosition;

            deltaTransformPosition = lastTransformPosition - transformPosition;
            lastTransformPosition = transformPosition;

            // Update all the Vectors marked with OriginVector attribute
            foreach (var member in Members)
            {
                //SetPosition(member, StoredPosition, false);

                // var d = new Vector3(-deltaTransformPosition.x, deltaTransformPosition.y, -deltaTransformPosition.z);
                SetPosition(member, -deltaTransformPosition, true);
            }

            deltaTransformPosition.y = 0;

            // Only for debugging in inspector
            //rootTransformPosition += deltaTransformPosition;
            storedPosition = StoredPosition;

            // Only update X, Z values, @TODO: do this also for Y values
            // if (!simulateGravity)

            //Debug.Log(deltaTransformPosition);

            if (!preciseColliders) RootTransform.gameObject.SetActive(false);
            //RootTransform.position = StoredPosition;
            RootTransform.position += deltaTransformPosition;
            if (!preciseColliders) RootTransform.gameObject.SetActive(true);

            // Force all freezes
            if (isRigidBody)
                body.constraints = RigidbodyConstraints.FreezeAll;
        }

        private void ToggleCharacterController(bool isGravityEnabled)
        {
            if (controller == null) return;
            controller.enabled = !isGravityEnabled;
        }

        private bool IsFlyMode()
        {
            return controller != null && controller.enabled || body != null && !body.useGravity;
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

        private void SetPosition(MemberInfo member, Vector3 value, bool sum)
        {
            // TODO: This make the terrain generator to do weird things
            var instances = FindObjectsOfType(member.DeclaringType);
            if (instances?.Length == 0) return;
            // ReSharper disable once PossibleNullReferenceException
            foreach (var instance in instances)
            {
                if (sum)
                {
                    var v = member.GetValue<Vector3>(instance);
                    value += v;
                }

                member.SetValue(instance, value);
            }
        }
    }

    // https://stackoverflow.com/questions/44509636/convert-a-transform-to-a-recttransform (custom transform didn't worked)
}