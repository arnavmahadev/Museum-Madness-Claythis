using System.Collections;
using UnityEngine;
using XtremeFPS.PoolingSystem;
using XtremeFPS.Interfaces;

namespace XtremeFPS.WeaponSystem
{
    public class ParabolicBullet : MonoBehaviour
    {
        #region Variables
        private float speed;
        private float damage;
        private float gravity;
        private Vector3 startPosition;
        private Vector3 startForward;
        private GameObject particlesPrefab;
        private float bulletLiftime;

        private float startTime = -1;
        private Vector3 currentPoint;
        private Coroutine despawnCoroutine;
        #endregion

        #region Initialization
        public void Initialize(Transform startPoint, float speed, float damage, float gravity, float bulletLifetime, GameObject particlePrefab)
        {
            this.startPosition = startPoint.position;
            this.startForward = startPoint.forward.normalized;
            this.speed = speed;
            this.damage = damage;
            this.gravity = gravity;
            this.particlesPrefab = particlePrefab;
            this.bulletLiftime = bulletLifetime;
        }
        #endregion

        #region MonoBehaviour Callbacks
        void OnEnable()
        {
            startTime = -1f;
            if (despawnCoroutine != null) StopCoroutine(despawnCoroutine);
            despawnCoroutine = StartCoroutine(DespawnAfter(bulletLiftime));
        }

        private void FixedUpdate()
        {
            if (startTime < 0) startTime = Time.time;

            float currentTime = Time.time - startTime;
            float prevTime = currentTime - Time.fixedDeltaTime;
            float nextTime = currentTime + Time.fixedDeltaTime;

            RaycastHit hit;
            Vector3 currentPoint = FindPointOnParabola(currentTime);

            if (prevTime > 0)
            {
                Vector3 prevPoint = FindPointOnParabola(prevTime);
                if (CastRayBetweenPoints(prevPoint, currentPoint, out hit))
                {
                    HandleImpact(hit);
                    return;
                }
            }

            Vector3 nextPoint = FindPointOnParabola(nextTime);
            if (CastRayBetweenPoints(currentPoint, nextPoint, out hit))
            {
                HandleImpact(hit);
            }
        }

        private void Update()
        {
            if (startTime < 0) return;

            float currentTime = Time.time - startTime;
            currentPoint = FindPointOnParabola(currentTime);
            transform.position = currentPoint;
        }
        #endregion

        #region Impact + Despawn
        private void HandleImpact(RaycastHit hit)
        {
            if (hit.transform.TryGetComponent<IShootableObject>(out IShootableObject shootableObject))
                shootableObject.OnHit(hit, damage);

            if (particlesPrefab != null)
            {
                GameObject hitObject = PoolManager.Instance.SpawnObject(particlesPrefab, hit.point + hit.normal * 0.05f, Quaternion.LookRotation(hit.normal));
                if (hitObject != null)
                {
                    hitObject.transform.parent = hit.transform;
                    StartCoroutine(DespawnAfter(hitObject, 1.5f));
                }
                else
                {
                    Debug.LogWarning("SpawnObject returned null. Check if particlesPrefab is set up correctly in PoolManager.");
                }
            }
            else
            {
                Debug.LogWarning("particlesPrefab is null. Skipping impact effect.");
            }

            PoolManager.Instance.DespawnObject(this.gameObject);
        }

        private IEnumerator DespawnAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (this != null && gameObject != null)
                PoolManager.Instance.DespawnObject(this.gameObject);
        }

        private IEnumerator DespawnAfter(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
                PoolManager.Instance.DespawnObject(obj);
        }
        #endregion

        #region Utility
        private Vector3 FindPointOnParabola(float time)
        {
            Vector3 point = startPosition + (speed * time * startForward);
            Vector3 gravityVec = gravity * time * time * Vector3.down;
            return point + gravityVec;
        }

        private bool CastRayBetweenPoints(Vector3 startPoint, Vector3 endPoint, out RaycastHit hit)
        {
            return Physics.Raycast(startPoint, endPoint - startPoint, out hit, (endPoint - startPoint).magnitude);
        }
        #endregion
    }
}
