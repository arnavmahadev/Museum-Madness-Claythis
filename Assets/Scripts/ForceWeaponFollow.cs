using UnityEngine;

public class ForceWeaponFollow : MonoBehaviour
{
    [SerializeField] public Transform cameraTransform;
    public Vector3 localPos = new Vector3(0f, -0.1f, 0.3f);
    public Vector3 localEuler = Vector3.zero;

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        transform.position = cameraTransform.TransformPoint(localPos);
        transform.rotation = cameraTransform.rotation * Quaternion.Euler(localEuler);
    }
}
