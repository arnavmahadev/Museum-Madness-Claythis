using UnityEngine;
using TMPro;
using XtremeFPS.InputHandling;
using XtremeFPS.PoolingSystem;
using XtremeFPS.FPSController;
using System.Collections;

namespace XtremeFPS.WeaponSystem
{
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Spoiled Unknown/XtremeFPS/Weapon System")]
    public class UniversalWeaponSystem : MonoBehaviour
    {
        #region Variables
        //Reference
        public FirstPersonController fpsController;
        public Transform shootPoint;
        public ParticleSystem muzzleFlash;
        public GameObject bulletPrefab;
        public TextMeshProUGUI bulletCount;
        public Animator animator;
        public GameObject aimUIImage;
        public Transform cameraTransform;

        private FPSInputManager inputManager;

        //Bullet Physics
        public float bulletSpeed;
        public float bulletDamage;
        public float bulletLifeTime;
        public float bulletGravitationalForce;

        //Bullet Shell
        public Transform ShellPosition;
        public GameObject Shell;
        public GameObject particlesPrefab;

        //Gun stats
        public int BulletsLeft { get; private set; }
        public bool isGunAuto;
        public bool isAimHold;
        public float timeBetweenEachShots;
        public float timeBetweenShooting;
        public int magazineSize;
        public int totalBullets;
        public int bulletsPerTap;
        public float reloadTime;
        public bool aiming;
        public bool hardMode;

        private int bulletsShot;
        private bool readyToShoot;
        private bool shooting;
        private bool reloading;

        //Aiming
        public bool canAim = true;
        public Transform weaponHolder;
        public Vector3 aimingLocalPosition = new Vector3(0f, -0.12f, 0.2336001f);
        public float aimSmoothing = 3f; // Reduced for slower, smoother transition

        //Camera Recoil 
        public bool haveCameraRecoil = true;
        public Transform cameraRecoilHolder;
        public float recoilRotationSpeed = 6f;
        public float recoilReturnSpeed = 25f;
        public Vector3 hipFireRecoil = new Vector3(4f, 4f, 4f);
        public Vector3 adsFireRecoil = new Vector3(2f, 2f, 2f);
        public float hRecoil = 0.215f;
        public float vRecoil = 0.221f;

        private Vector3 currentRotation;
        private Vector3 Rot;

        //Weapon Recoil 
        public bool haveWeaponRecoil = true;
        public Transform gunPositionHolder;
        public float gunRecoilPositionSpeed = 8f;
        public float gunPositionReturnSpeed = 10f;
        public Vector3 recoilKickBackHip = new Vector3(0.015f, 0f, 0.05f);
        public Vector3 recoilKickBackAds = new Vector3(-0.08f, 0.01f, 0.009f);
        public float gunRecoilRotationSpeed = 8f;
        public float gunRotationReturnSpeed = 38f;
        public Vector3 recoilRotationHip = new Vector3(10f, 5f, 7f);
        public Vector3 recoilRotationAds = new Vector3(10f, 4f, 6f);

        private Vector3 rotationRecoil;
        private Vector3 positionRecoil;
        private Vector3 rot;

        //Weapon Rotational Sway
        public bool haveRotationalSway = true;
        public float rotaionSwayIntensity = 10f;
        public float rotationSwaySmoothness = 2f;

        private Quaternion originRotation;
        private float mouseX;
        private float mouseY;

        //Jump Sway
        public bool haveJumpSway = true;
        public float jumpIntensity = 5f;
        public float weaponMaxClamp = 20f;
        public float weaponMinClamp = 20f;
        public float jumpSmooth = 15f;
        public float landingIntensity = 5f;
        public float landingSmooth = 15f;
        public float recoverySpeed = 50f;

        private float impactForce = 0;

        //Weapon Move Bobbing
        public bool haveBobbing = true;
        public float magnitude = 0.009f;
        public float idleSpeed = 2f;
        public float walkSpeedMultiplier = 4f;
        public float walkSpeedMax = 6f;
        public float aimReduction = 4f;

        private float sinY = 0f;
        private float sinX = 0f;
        private Vector3 lastPosition;

        //Audio Setup
        public AudioClip bulletSoundClip;
        public AudioClip bulletReloadClip;

        private AudioSource bulletSoundSource;
        private ForceWeaponFollow weaponFollow; // Reference to ForceWeaponFollow script

        // Weapon Selection
        private string currentWeapon = "Pistol";
        public GameObject pistolObject;
        public GameObject rifleObject;
        private Vector3 targetLocalPos;
        private Vector3 velocity = Vector3.zero;
        private bool returningFromReload = false;
        private Vector3 reloadStartPosition;


        #endregion

        #region MonoBehaviour Callbacks
        private void Start()
        {
            inputManager = FPSInputManager.Instance;
            bulletSoundSource = GetComponent<AudioSource>();
            weaponFollow = weaponHolder.GetComponent<ForceWeaponFollow>(); // Get the ForceWeaponFollow component

            BulletsLeft = magazineSize;

            lastPosition = transform.position;
            if (haveRotationalSway) originRotation = transform.localRotation;

            SetBulletCountUI();
            readyToShoot = true;
        }

        private void Update()
        {
            HandleWeaponSwitching();
            PlayerWeaponsInput();
            DetermineAim();
            HandleWeaponRecoil();
            HandleCameraRecoil();
            WeaponRotationSway();
            WeaponBobbing();
            JumpSwayEffect();

            UpdateWeaponPositionLerp(); // 👈 New centralized position lerp
        }

        /*private void LateUpdate()
        {
            if (reloading && canAim)
            {
                weaponFollow.overridePosition = true;
                Vector3 target = reloadLocalPosition;
                weaponFollow.localPos = Vector3.Lerp(weaponFollow.localPos, target, Time.deltaTime * aimSmoothing);
            }
        }
        */
        #endregion

        #region Private Methods
        private void PlayerWeaponsInput()
        {
            if (currentWeapon != "Pistol" && currentWeapon != "Rifle") return;

            if (isGunAuto) shooting = inputManager.isFiringHold;
            else shooting = inputManager.isFiringTapped;

            //handle mouse inputs
            mouseX = inputManager.mouseDirection.x;
            mouseY = inputManager.mouseDirection.y;

            if (isAimHold) aiming = inputManager.isAimingHold;
            else aiming = inputManager.isAimingTapped;

            if ((inputManager.isReloading || BulletsLeft == 0)
                && BulletsLeft < magazineSize
                && totalBullets > 0
                && !reloading) Reload();

            //Shoot
            if (readyToShoot
                && shooting
                && !reloading
                && BulletsLeft > 0)
            {
                bulletsShot = bulletsPerTap;
                Shoot();
                bulletSoundSource.PlayOneShot(bulletSoundClip);
            }
            else fpsController.AddRecoil(0f, 0f);
        }

        #region Shooting && Reloading
        private void UpdateWeaponPositionLerp()
        {
            if (!canAim) return;

            if (reloading)
            {
                // Lerp down from whatever position the weapon was in before reload
                targetLocalPos = reloadStartPosition + new Vector3(0f, -0.3f, -0.15f);
                weaponFollow.overridePosition = true;
                weaponFollow.localPos = Vector3.Lerp(weaponFollow.localPos, targetLocalPos, Time.deltaTime * aimSmoothing);
            }
            else if (returningFromReload)
            {
                // Lerp back to the saved pre-reload position
                targetLocalPos = reloadStartPosition;
                weaponFollow.overridePosition = true;
                weaponFollow.localPos = Vector3.Lerp(weaponFollow.localPos, targetLocalPos, Time.deltaTime * aimSmoothing);

                if (Vector3.Distance(weaponFollow.localPos, targetLocalPos) < 0.01f)
                {
                    weaponFollow.localPos = targetLocalPos;
                    returningFromReload = false;
                    weaponFollow.overridePosition = false;
                }
            }
        }





        private void Shoot()
        {
            if (currentWeapon != "Pistol" && currentWeapon != "Rifle") return;
            readyToShoot = false;

            // Calculate shoot direction from camera (center of screen)
            Vector3 shootDirection = cameraTransform.forward;

            // Add spread for realism
            float spread = 0.01f;
            shootDirection += cameraTransform.right * Random.Range(-spread, spread);
            shootDirection += cameraTransform.up * Random.Range(-spread, spread);
            shootDirection.Normalize();

            // Raycast from camera
            if (Physics.Raycast(cameraTransform.position, shootDirection, out RaycastHit hit, 100f))
            {
                if (particlesPrefab != null)
                {
                    GameObject impact = PoolManager.Instance.SpawnObject(
                        particlesPrefab,
                        hit.point,
                        Quaternion.LookRotation(hit.normal)
                    );
                    StartCoroutine(DespawnAfter(impact, 1.5f));
                }
            }

            // Visuals
            if (muzzleFlash != null) muzzleFlash.Play();
            PoolManager.Instance.SpawnObject(Shell, ShellPosition.position, ShellPosition.rotation);

            float hRecoil = Random.Range(-this.hRecoil, this.hRecoil);

            if (aiming)
            {
                currentRotation += new Vector3(-adsFireRecoil.x, Random.Range(-adsFireRecoil.y, adsFireRecoil.y), Random.Range(-adsFireRecoil.z, adsFireRecoil.z));
                rotationRecoil += new Vector3(-recoilRotationAds.x, Random.Range(-recoilRotationAds.y, recoilRotationAds.y), Random.Range(-recoilRotationAds.z, recoilRotationAds.z));
                positionRecoil += new Vector3(Random.Range(-recoilKickBackAds.x, recoilKickBackAds.y), Random.Range(-recoilKickBackAds.y, recoilKickBackAds.y), recoilKickBackAds.z);

                fpsController.AddRecoil(hRecoil * 0.5f, vRecoil * 0.5f);
            }
            else
            {
                currentRotation += new Vector3(-hipFireRecoil.x, Random.Range(-hipFireRecoil.y, hipFireRecoil.y), Random.Range(-hipFireRecoil.z, hipFireRecoil.z));
                rotationRecoil += new Vector3(-recoilRotationHip.x, Random.Range(-recoilRotationHip.y, recoilRotationHip.y), Random.Range(-recoilRotationHip.z, recoilRotationHip.z));
                positionRecoil += new Vector3(Random.Range(-recoilKickBackHip.x, recoilKickBackHip.y), Random.Range(-recoilKickBackHip.y, recoilKickBackHip.y), recoilKickBackHip.z);

                fpsController.AddRecoil(hRecoil, vRecoil);
            }

            BulletsLeft--;
            bulletsShot--;

            SetBulletCountUI();

            Invoke(nameof(ResetShot), timeBetweenShooting);
            if (bulletsShot > 0 && BulletsLeft > 0) Invoke(nameof(Shoot), timeBetweenEachShots);
        }

        private System.Collections.IEnumerator DespawnAfter(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            PoolManager.Instance.DespawnObject(obj);
        }
        private void ResetShot()
        {
            if (currentWeapon != "Pistol" && currentWeapon != "Rifle") return;
            readyToShoot = true;
        }
        private void Reload()
        {
            if (currentWeapon != "Pistol" && currentWeapon != "Rifle") return;
            reloadStartPosition = weaponFollow.localPos;
            StartCoroutine(ReloadRoutine());

        }
        private void HandleReloadAnimation()
        {
            if (currentWeapon != "Pistol" && currentWeapon != "Rifle") return;
            animator.SetBool("IsReloading", reloading);
        }
        private void ReloadFinished()
        {
            if (currentWeapon != "Pistol" && currentWeapon != "Rifle") return;
            reloading = false;
            HandleReloadAnimation();
            if (hardMode)
            {
                switch (totalBullets.CompareTo(magazineSize))
                {
                    case 1:  // totalBullets > magazineSize
                        BulletsLeft = magazineSize;
                        totalBullets -= magazineSize;
                        break;
                    case 0:  // totalBullets == magazineSize
                        BulletsLeft = magazineSize;
                        totalBullets -= magazineSize;
                        break;
                    case -1: // totalBullets < magazineSize
                        BulletsLeft = totalBullets;
                        totalBullets = 0;
                        break;
                    default:
                        break;
                }
            }
            else //if hardMode is false
            {
                if ((BulletsLeft + totalBullets) >= magazineSize)
                {
                    int bulletsNeededForReload = magazineSize - BulletsLeft;
                    BulletsLeft += bulletsNeededForReload;
                    totalBullets -= bulletsNeededForReload;
                }
                else
                {
                    int bulletsNeededForReload = magazineSize - BulletsLeft;
                    BulletsLeft += Mathf.Min(bulletsNeededForReload, totalBullets);
                    totalBullets -= bulletsNeededForReload;
                    totalBullets = Mathf.Max(0, totalBullets);
                }
            }

            weaponFollow.overridePosition = true;
            returningFromReload = true; // 👈 start lerping back up after reload
            SetBulletCountUI();
        }
        /*private IEnumerator ReturnToPosition()
        {
            if (canAim)
            {
                weaponFollow.overridePosition = true;
                Vector3 startPos = weaponFollow.localPos;
                Vector3 target = aiming ? aimingLocalPosition : normalLocalPosition;
                float elapsedTime = 0f;
                float returnDuration = 1f;

                while (elapsedTime < returnDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / returnDuration;
                    weaponFollow.localPos = Vector3.Lerp(startPos, target, t);
                    yield return null;
                }

                weaponFollow.localPos = target;
                yield return new WaitForSeconds(0.05f); // Wait one frame before disabling override
                weaponFollow.overridePosition = false;
            }
        }*/

        private void SetBulletCountUI()
        {
            if (bulletCount == null) return;
            bulletCount.SetText(BulletsLeft + " / " + totalBullets);
        }

        private void HandleWeaponSwitching()
        {
            if (inputManager.switchToPistol)
            {
                pistolObject.SetActive(true);
                rifleObject.SetActive(false);
                currentWeapon = "Pistol";
            }

            if (inputManager.switchToRifle)
            {
                pistolObject.SetActive(false);
                rifleObject.SetActive(true);
                currentWeapon = "Rifle";
            }
        }

        private System.Collections.IEnumerator ReloadRoutine()
        {
            reloading = true;
            HandleReloadAnimation();
            bulletSoundSource.PlayOneShot(bulletReloadClip);

            float t = 0f;
            while (t < reloadTime)
            {
                t += Time.deltaTime;
                yield return null;
            }

            ReloadFinished();
        }
        #endregion
        #region Recoil
        private void HandleWeaponRecoil()
        {
            if (currentWeapon != "Pistol" && currentWeapon != "Rifle" || !haveWeaponRecoil) return;
            rotationRecoil = Vector3.Lerp(rotationRecoil, Vector3.zero, gunRotationReturnSpeed * Time.deltaTime);
            positionRecoil = Vector3.Lerp(positionRecoil, Vector3.zero, gunPositionReturnSpeed * Time.deltaTime);

            gunPositionHolder.localPosition = Vector3.Slerp(gunPositionHolder.localPosition, positionRecoil, gunRecoilPositionSpeed * Time.deltaTime);
            rot = Vector3.Slerp(rot, rotationRecoil, gunRecoilRotationSpeed* Time.deltaTime);
            gunPositionHolder.localRotation = Quaternion.Euler(rot);
        }
        private void HandleCameraRecoil()
        {
            if (currentWeapon != "Pistol" && currentWeapon != "Rifle" || !haveCameraRecoil) return;

            currentRotation = Vector3.Lerp(currentRotation, Vector3.zero, recoilReturnSpeed * Time.deltaTime);
            Rot = Vector3.Slerp(Rot, currentRotation, recoilRotationSpeed * Time.deltaTime);
            cameraRecoilHolder.transform.localRotation = Quaternion.Euler(Rot);
        }
        #endregion
        private void DetermineAim()
        {
            if (currentWeapon != "Pistol" && currentWeapon != "Rifle" || !canAim || reloading) return;

            if (aimUIImage != null)
            {
                aimUIImage.SetActive(aiming);
                animator.gameObject.SetActive(!aiming);
                fpsController.enableZoom = aiming;
            }
        }


        #region Effects
            private void WeaponRotationSway()
        {
            if (currentWeapon != "Pistol" && currentWeapon != "Rifle" || !haveRotationalSway) return;

            Quaternion newAdjustedRotationX = Quaternion.AngleAxis(rotaionSwayIntensity * mouseX * -1f, Vector3.up);
            Quaternion targetRotation = originRotation * newAdjustedRotationX;
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, rotationSwaySmoothness * Time.deltaTime);
        }
        private void WeaponBobbing()
        {
            if (currentWeapon != "Pistol" && currentWeapon != "Rifle" || !haveBobbing || fpsController.MovementState == FirstPersonController.PlayerMovementState.Sliding) return;

            if (!fpsController.IsGrounded)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, Time.deltaTime);
                return;
            }

            // Calculate delta time based on the player's movement speed.
            float delta = Time.deltaTime * idleSpeed;
            float velocity = (lastPosition - transform.position).magnitude * walkSpeedMultiplier;
            delta += Mathf.Clamp(velocity, 0, walkSpeedMax);

            // Update the sinX and sinY values to create a bobbing effect.
            sinX += delta / 2;
            sinY += delta;
            sinX %= Mathf.PI * 2;
            sinY %= Mathf.PI * 2;

            // Adjust the weapon's local position to create the bobbing effect.
            float magnitude = aiming ? this.magnitude / aimReduction : this.magnitude;
            transform.localPosition = Vector3.zero + magnitude * Mathf.Sin(sinY) * Vector3.up;
            transform.localPosition += magnitude * Mathf.Sin(sinX) * Vector3.right;

            lastPosition = transform.position;
        }
        private void JumpSwayEffect()
        {
            if (currentWeapon != "Pistol" && currentWeapon != "Rifle" || !haveJumpSway || aiming) return;

            switch (fpsController.IsGrounded)
            {
                case false:
                    // Adjust the weapon's rotation based on the player's jump velocity.
                    float yVelocity = fpsController.jumpVelocity.y;
                    yVelocity = Mathf.Clamp(yVelocity, -weaponMinClamp, weaponMaxClamp);
                    impactForce = -yVelocity * landingIntensity;

                    if (aiming)
                    {
                        yVelocity = Mathf.Max(yVelocity, 0);
                    }

                    // Update the weapon's local rotation to simulate the jump sway effect.
                    this.transform.localRotation = Quaternion.Lerp(this.transform.localRotation, Quaternion.Euler(0f, 0f, yVelocity * jumpIntensity), Time.deltaTime * jumpSmooth);
                    break;
                case true when impactForce >= 0:
                    // If the player is grounded and has impact force, adjust the weapon's rotation accordingly.
                    this.transform.localRotation = Quaternion.Lerp(this.transform.localRotation, Quaternion.Euler(0, 0, impactForce), Time.deltaTime * landingSmooth);
                    impactForce -= recoverySpeed * Time.deltaTime;
                    break;
                case true:
                    // If the player is grounded and there's no impact force, reset the weapon's rotation.
                    this.transform.localRotation = Quaternion.Lerp(this.transform.localRotation, Quaternion.identity, Time.deltaTime * landingSmooth);
                    break;
            }
        }
        #endregion
        #endregion
    }
}