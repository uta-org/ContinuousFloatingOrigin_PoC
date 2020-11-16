using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using TerrainGenerator = uzSurfaceMapper.Utils.Terrains.TerrainGenerator;

namespace CFO.Utils
{
    [RequireComponent(typeof(FlyCamera))]
    public class PedController : MonoBehaviour
    {
        private bool isFoundGroundExecuted;

        private CharacterController characterController;
        private Coroutine m_findGroundCoroutine;

        [SerializeField] private TerrainGenerator terrainGenerator;
        //[SerializeField] private FlyCamera flyCamera;

        //public NoClipController noClip;

        private void Awake()
        {
            var flyCamera = GetComponent<FlyCamera>();
            flyCamera.OnLeaveFlyMode += OnLeaveFlyMode;

            characterController = GetComponent<CharacterController>();
            terrainGenerator.OnFinishGeneration += OnFinishGeneration;
            //TerrainGenerator.Instance.OnTerrainUpdated += OnTerrainUpdated;
        }

        private void OnLeaveFlyMode()
        {
            FindGround();
        }

        private void OnFinishGeneration()
        {
            if (isFoundGroundExecuted) return;
            FindGround();
            isFoundGroundExecuted = true;
        }

        public struct FindGroundParams
        {
            public bool tryFromAbove;
            public float raycastDistance;
            public int maxTries;

            public FindGroundParams(bool tryFromAbove = true, float raycastDistance = 1000, int maxTries = 50)
            {
                this.tryFromAbove = tryFromAbove;
                this.raycastDistance = raycastDistance;
                this.maxTries = maxTries;
            }

            //public static FindGroundParams DefaultBasedOnLoadedWorld => new FindGroundParams((null == Cell.Instance || Cell.Instance.HasExterior));
            public static FindGroundParams DefaultBasedOnLoadedWorld => new FindGroundParams();
        }

        public void Teleport(Vector3 position, Quaternion rotation, FindGroundParams parameters)
        {
            //if (!NetStatus.IsServer)
            //    return;

            //if (this.IsInVehicle)
            //    return;

            transform.position = position;
            transform.rotation = rotation;
            //this.Heading = rotation.TransformDirection(Vector3.forward);

            FindGround(parameters);
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            Teleport(position, rotation, FindGroundParams.DefaultBasedOnLoadedWorld);
        }

        public void Teleport(Vector3 position)
        {
            Teleport(position, transform.rotation);
        }

        public void FindGround()
        {
            FindGround(new FindGroundParams());
        }

        public void FindGround(FindGroundParams parameters)
        {
            if (m_findGroundCoroutine != null)
            {
                StopCoroutine(m_findGroundCoroutine);
                m_findGroundCoroutine = null;
            }

            m_findGroundCoroutine = StartCoroutine(FindGroundCoroutine(parameters));
        }

        private IEnumerator FindGroundCoroutine(FindGroundParams parameters)
        {
            // set y pos to high value, so that higher grounds can be loaded
            //	this.transform.SetY (150);

            Vector3 startingPos = transform.position;

            yield return null;

            // wait for loader to finish, in case he didn't
            //while (!Loader.HasLoaded)
            //    yield return null;

            // yield until you find ground beneath or above the player, or until timeout expires

            //float timeStarted = Time.time;
            int numAttempts = 1;

            while (true)
            {
                //if (Time.time - timeStarted > 4.0f)
                //{
                //    // timeout expired
                //    //Debug.LogWarningFormat("Failed to find ground for ped {0} - timeout expired", this.DescriptionForLogging);
                //    yield break;
                //}

                // maintain starting position
                transform.position = startingPos;
                //this.Velocity = Vector3.zero;

                float raycastDistance = parameters.raycastDistance;
                //int raycastLayerMask = ~PedManager.Instance.groundFindingIgnoredLayerMask;

                var raycastPositions = new List<Vector3> { transform.position };   //transform.position - Vector3.up * characterController.height;
                var raycastDirections = new List<Vector3> { Vector3.down };
                var customMessages = new List<string> { "from center" };

                if (parameters.tryFromAbove)
                {
                    raycastPositions.Add(transform.position + Vector3.up * raycastDistance);
                    raycastDirections.Add(Vector3.down);
                    customMessages.Add("from above");
                }

                for (int i = 0; i < raycastPositions.Count; i++)
                {
                    //if (Physics.Raycast(raycastPositions[i], raycastDirections[i], out hit, raycastDistance, raycastLayerMask))
                    if (Physics.Raycast(raycastPositions[i], raycastDirections[i], out var hit, raycastDistance))
                    {
                        // ray hit the ground
                        // we can move there

                        OnFoundGround(hit, numAttempts, customMessages[i]);
                        yield break;
                    }
                }

                if (numAttempts > parameters.maxTries)
                    yield break;

                if (characterController.isGrounded)
                    yield break;

                numAttempts++;
                yield return null;
            }
        }

        private void OnFoundGround(RaycastHit hit, int numAttempts, string customMessage)
        {
            transform.position = hit.point + Vector3.up * (characterController.height + 0.1f);
            //this.Velocity = Vector3.zero;

            //Debug.LogFormat("Found ground at {0}, distance {1}, object name {2}, num attempts {3}, {4}, ped {5}", hit.point, hit.distance,
            //    hit.transform.name, numAttempts, customMessage, this.DescriptionForLogging);
        }
    }
}