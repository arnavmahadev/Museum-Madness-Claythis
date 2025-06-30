using UnityEngine;

public class IKTargetFollower : MonoBehaviour
{
    [SerializeField] public Transform target;
    public float distance = 1.2f;

    void LateUpdate()
    {
        target.position = transform.position + transform.forward * distance;
        target.rotation = transform.rotation;
    }
}