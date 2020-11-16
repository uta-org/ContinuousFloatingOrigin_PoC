using CFO.Tests;
using UnityEngine;

namespace CFO.Utils
{
    /// <summary>
    /// A simple implementation of a flying camera.
    /// Uses Mouse Look and acceleration axes to control.
    /// Can lock to terrain below with followTerrain, it will
    /// still fly the set height (above the terrain)
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FlyCamera : MonoBehaviour
    {
        public float movementSpeed;

        public float height;

        public bool followTerrain;

        public bool autoFly;

        private float sensitivityX = 6f;

        //private Vector3 lastPosition;

        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();

            if (autoFly)
                ToggleAutoFly(true);
        }

        // Update is called once per frame
        private void Update()
        {
            if (!autoFly) return;

            // mouse look rotation
            float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;
            transform.localEulerAngles = new Vector3(0, rotationX, 0);

            // keyboard movement
            Vector3 moveVector = Input.GetAxis("Vertical") * transform.forward + Input.GetAxis("Horizontal") * transform.right;

            // autofly
            if (autoFly)
            {
                // turn off auto fly on key input.
                if (moveVector.magnitude > 0)
                {
                    ToggleAutoFly(false);
                    //autoFly = false;
                }

                moveVector = transform.forward;
            }

            transform.Translate(moveVector * movementSpeed * Time.deltaTime, Space.World);
            TransformInjection.GetRealPosition = false; // The next got position will be from the StoredPosition modified by the patched Translate method.
            Vector3 position = transform.position;
            position.y = height;

            if (followTerrain)
            {
                RaycastHit hitInfo;
                Physics.Raycast(new Vector3(transform.position.x, 100f, transform.position.z), -Vector3.up, out hitInfo, 100f);
                position.y = hitInfo.point.y + height;
            }

            transform.position = position;
            // Vector3.Lerp(position, lastPosition, .5f);

            //lastPosition = position;
        }

        private void ToggleAutoFly(bool active)
        {
            body.useGravity = !active;
            autoFly = active;
        }
    }
}