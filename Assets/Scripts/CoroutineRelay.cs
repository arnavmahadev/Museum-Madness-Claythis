using UnityEngine;
using System.Collections;

public class CoroutineRelay : MonoBehaviour
{
    private static CoroutineRelay _instance;
    public ForceWeaponFollow weaponFollow;

    public static CoroutineRelay Instance => _instance;

    private void Awake()
    {
        if (_instance == null) _instance = this;
    }

    public void RunCoroutine(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }

    public IEnumerator TranslateWeaponLocalPos(ForceWeaponFollow weaponFollow, Vector3 from, Vector3 to, float speed, string weaponType)
    {
        weaponFollow.overridePosition = true;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            weaponFollow.localPos = Vector3.Lerp(from, to, t);
            yield return null;
        }

        if (weaponType == "Pistol")
        {
            weaponFollow.localPos = weaponFollow.pistolLocalPos;
        }
        else if (weaponType == "Rifle")
        {
            weaponFollow.localPos = weaponFollow.rifleLocalPos;
        }

        weaponFollow.overridePosition = false;
    }


        public IEnumerator TranslatePistolDown(ForceWeaponFollow weaponFollow, float speed)
        {
            Vector3 start = weaponFollow.pistolLocalPos;
            Vector3 target = start + new Vector3(0f, -0.5f, -0.25f);
            yield return TranslateWeaponLocalPos(weaponFollow, start, target, speed, "Pistol");
        }

        public IEnumerator TranslateRifleDown(ForceWeaponFollow weaponFollow, float speed)
        {
            Vector3 start = weaponFollow.rifleLocalPos;
            Vector3 target = start + new Vector3(0f, -0.5f, -0.25f);
            yield return TranslateWeaponLocalPos(weaponFollow, start, target, speed, "Rifle");
        }

        public IEnumerator TranslatePistolUp(ForceWeaponFollow weaponFollow, float speed)
        {
            Vector3 start = weaponFollow.pistolLocalPos + new Vector3(0f, -0.5f, -0.25f);
            Vector3 target = weaponFollow.pistolLocalPos;
            yield return TranslateWeaponLocalPos(weaponFollow, start, target, speed, "Pistol");
        }

        public IEnumerator TranslateRifleUp(ForceWeaponFollow weaponFollow, float speed)
        {
            Vector3 start = weaponFollow.rifleLocalPos + new Vector3(0f, -0.5f, -0.25f);
            Vector3 target = weaponFollow.rifleLocalPos;
            yield return TranslateWeaponLocalPos(weaponFollow, start, target, speed, "Rifle");
        }

}