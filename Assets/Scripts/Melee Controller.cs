using System.Collections;
using UnityEngine;

public class MeleeController : MonoBehaviour
{
    [SerializeField] private Animator meleeAnimator;
    [SerializeField] private float punchDuration = 0.483f;

    private bool punchLeftNext = true;
    public bool IsPunching { get; private set; }

    public void OnShoot()
    {
        if (IsPunching) return;

        IsPunching = true;

        if (punchLeftNext)
        {
            meleeAnimator.SetTrigger("PunchLeft");
        }
        else
        {
            meleeAnimator.SetTrigger("PunchRight");
        }

        punchLeftNext = !punchLeftNext;

        StartCoroutine(ResetPunching());
    }

    private IEnumerator ResetPunching()
    {
        yield return new WaitForSeconds(punchDuration);
        IsPunching = false;
    }
}
