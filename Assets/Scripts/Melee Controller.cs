using System.Collections;
using UnityEngine;

public class MeleeController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private bool punchLeftNext = true;
    public bool IsPunching { get; private set; }

    public void OnShoot()
    {
        if (IsPunching) return;
        IsPunching = true;

        if (punchLeftNext)
            animator.SetTrigger("PunchLeft");
        else
            animator.SetTrigger("PunchRight");

        punchLeftNext = !punchLeftNext;

        // Optional: wait ~0.4–1.2s depending on animation length to re-enable punching
        StartCoroutine(ResetPunching());
    }

    private IEnumerator ResetPunching()
    {
        yield return new WaitForSeconds(0f); // Use a middle value or sync to anim
        IsPunching = false;
    }
}