using UnityEngine;

public class CubeSetter : MonoBehaviour
{
    public GameObject cube;

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        cube.transform.position += Vector3.forward * Time.fixedDeltaTime;
    }
}