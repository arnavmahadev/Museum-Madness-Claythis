using UnityEngine;

public class ForceWeaponFollow : MonoBehaviour
{
    [SerializeField] public Transform cameraTransform;

    [Header("Rifle Offsets")]
    public Vector3 rifleLocalPos = new Vector3(0.1f, -0.19f, 0.07f);
    public Vector3 rifleLocalEuler = new Vector3(0f, 90f, 0f);

    [Header("Pistol Offsets")]
    public Vector3 pistolLocalPos = new Vector3(0.07f, -0.12f, 0.2f);
    public Vector3 pistolLocalEuler = new Vector3(0f, 90f, 0f);

    [HideInInspector] public Vector3 localPos;
    [HideInInspector] public Vector3 localEuler;

    public bool overridePosition = false;

    private Transform rifleObject;
    private Transform pistolObject;

    void Start()
    {
        rifleObject = transform.Find("Weapon Recoils/M4_Carbine");
        pistolObject = transform.Find("Weapon Recoils/Pistol");
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        if (!overridePosition)
        {
            if (rifleObject != null && rifleObject.gameObject.activeSelf)
            {
                localPos = rifleLocalPos;
                localEuler = rifleLocalEuler;
            }
            else if (pistolObject != null && pistolObject.gameObject.activeSelf)
            {
                localPos = pistolLocalPos;
                localEuler = pistolLocalEuler;
            }
        }

        transform.position = cameraTransform.TransformPoint(localPos);
        transform.rotation = cameraTransform.rotation * Quaternion.Euler(localEuler);
    }
}
