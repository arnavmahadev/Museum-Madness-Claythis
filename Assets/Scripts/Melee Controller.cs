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

        StartCoroutine(ResetPunching());
    }

    private IEnumerator ResetPunching()
    {
        yield return new WaitForSeconds(0.4f);
        IsPunching = false;
    }
}