using UnityEngine;
using System.Collections;

public class GunController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float shootDuration = 0.2f;
    [SerializeField] private float reloadDuration = 1.5f;

    public bool IsReloading { get; private set; }
    public bool IsShooting { get; private set; }

    public void OnShoot()
    {
        if (IsReloading || IsShooting) return;

        StartCoroutine(PlayShoot());
    }

    public void OnReload()
    {
        if (IsReloading) return;

        StartCoroutine(PlayReload());
    }

    private IEnumerator PlayShoot()
    {
        IsShooting = true;
        animator.SetBool("IsShooting", true);

        yield return new WaitForSeconds(shootDuration);

        // Wait an extra frame to prevent immediate transition
        yield return null;

        animator.SetBool("IsShooting", false);
        IsShooting = false;
    }

    private IEnumerator PlayReload()
    {
        IsReloading = true;
        animator.SetBool("IsReloading", true);
        yield return new WaitForSeconds(reloadDuration);
        animator.SetBool("IsReloading", false);
        IsReloading = false;
    }
}
