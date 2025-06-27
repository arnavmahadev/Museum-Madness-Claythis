using UnityEngine;
using System.Collections;

public class GunController : MonoBehaviour
{
    [Header("Gun Settings")]
    [SerializeField] private Animator gunAnimator;
    [SerializeField] private float fireRate = 0.25f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float range = 100f;
    [SerializeField] private float reloadDuration = 1.5f;

    [Header("Effects")]
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private GameObject bulletImpactPrefab;
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float muzzleFlashDuration = 0.05f;

    [Header("Reload Rotation")]
    [SerializeField] private Transform armsTransform; // 🔁 add the gun's root transform here
    [SerializeField] private Vector3 reloadEulerRotation = new Vector3(-30f, 0f, 0f);
    [SerializeField] private float reloadRotationSpeed = 5f;

    private Quaternion originalRotation;
    private Quaternion reloadRotation;

    private float lastFireTime;
    private bool isReloading = false;
    [SerializeField] private int magSize = 20;
    private int currentAmmo;

    public bool IsShooting { get; private set; } = false;
    public bool IsReloading => isReloading;

    private void Start()
    {
        if (armsTransform != null)
        {
            originalRotation = armsTransform.localRotation;
            reloadRotation = Quaternion.Euler(reloadEulerRotation);
        }

        currentAmmo = magSize;
    }

    private void Update()
    {
        if (armsTransform == null) return;

        if (isReloading)
        {
            armsTransform.localRotation = Quaternion.Lerp(armsTransform.localRotation, reloadRotation, Time.deltaTime * reloadRotationSpeed);
        }
        else
        {
            armsTransform.localRotation = Quaternion.Lerp(armsTransform.localRotation, originalRotation, Time.deltaTime * reloadRotationSpeed);
        }
    }

    public void OnShoot()
    {
        if (isReloading) return;
        if (Time.time - lastFireTime < fireRate) return;
        if (currentAmmo <= 0)
        {
            OnReload();
            return;
        }

        lastFireTime = Time.time;
        currentAmmo--;

        IsShooting = true;
        gunAnimator.SetBool("IsShooting", true);

        StopCoroutine(nameof(ResetShootFlag));
        StartCoroutine(ResetShootFlag());

        GameObject flashInstance = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
        Destroy(flashInstance, muzzleFlashDuration);

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask))
        {
            Instantiate(bulletImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }

        if (currentAmmo <= 0)
        {
            OnReload();
        }
    }


    private IEnumerator ResetShootFlag()
    {
        yield return new WaitForSeconds(0.25f);

        while (gunAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Shoot"))
        {
            yield return null;
        }

        IsShooting = false;
        gunAnimator.SetBool("IsShooting", false);
    }

    public void OnReload()
    {
        if (isReloading) return;

        isReloading = true;
        gunAnimator.SetBool("IsReloading", true);
        StartCoroutine(ResetReloadFlag());
    }

    private IEnumerator ResetReloadFlag()
    {
        yield return new WaitForSeconds(reloadDuration);
        isReloading = false;
        currentAmmo = magSize;
        gunAnimator.SetBool("IsReloading", false);
    }
}
