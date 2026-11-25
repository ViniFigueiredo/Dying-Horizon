using UnityEngine;

namespace DyingHorizon
{
    [System.Serializable]
    public class WeaponSpec
    {
        public string name;
        public Sprite sprite;
        public float damage = 25f;
        public float fireRate = 4f;
        public float projectileSpeed = 16f;
        public int pelletsPerShot = 1;
        public float spreadAngle = 2f;
        public int magSize = 12;
        public int currentAmmo = 12;
        public int reserveAmmo = 48;
        public bool automatic = false;
        public float holdRadius = 0.42f;
        public float spriteScale = 0.75f;
    }

    /// <summary>
    /// Controla as armas equipadas pelo sobrevivente:
    /// - Troca pelas teclas [1], [2], [3], Scroll do mouse ou [0] para guardar.
    /// - Segue continuamente o ponteiro do mouse em 360 graus.
    /// - Dispara projéteis 8-bits na direção do cursor ao clicar com o botão esquerdo.
    /// </summary>
    public class WeaponController2D : MonoBehaviour
    {
        public WeaponSpec[] weapons = new WeaponSpec[3];
        public int currentWeaponIndex = 0;

        [Header("Efeitos de Disparo 8-Bits")]
        public Sprite bulletSprite;
        public Sprite muzzleFlashSprite;

        private Transform weaponHolder;
        private SpriteRenderer weaponRenderer;
        private SpriteRenderer muzzleFlashRenderer;
        private SpriteRenderer playerRenderer;

        private float nextFireTime;
        private float muzzleFlashTimer;
        private float recoilOffset;

        public WeaponSpec CurrentWeapon =>
            (weapons != null && currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Length)
                ? weapons[currentWeaponIndex]
                : null;

        private void Awake()
        {
            playerRenderer = GetComponent<SpriteRenderer>();
            EnsureWeaponHierarchy();
        }

        private void Start()
        {
            EnsureWeaponHierarchy();
            EquipWeapon(currentWeaponIndex);
        }

        private void EnsureWeaponHierarchy()
        {
            if (weaponHolder == null)
            {
                var existing = transform.Find("WeaponHolder");
                if (existing != null)
                {
                    weaponHolder = existing;
                }
                else
                {
                    var holderGO = new GameObject("WeaponHolder");
                    holderGO.transform.SetParent(transform, false);
                    weaponHolder = holderGO.transform;
                }
            }

            weaponRenderer = weaponHolder.GetComponent<SpriteRenderer>();
            if (weaponRenderer == null) weaponRenderer = weaponHolder.gameObject.AddComponent<SpriteRenderer>();
            weaponRenderer.sortingOrder = 11;

            if (muzzleFlashRenderer == null)
            {
                var existingFlash = weaponHolder.Find("MuzzleFlash");
                GameObject flashGO;
                if (existingFlash != null)
                {
                    flashGO = existingFlash.gameObject;
                }
                else
                {
                    flashGO = new GameObject("MuzzleFlash");
                    flashGO.transform.SetParent(weaponHolder, false);
                }
                flashGO.transform.localPosition = new Vector3(0.45f, 0f, 0f);
                flashGO.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
                muzzleFlashRenderer = flashGO.GetComponent<SpriteRenderer>();
                if (muzzleFlashRenderer == null) muzzleFlashRenderer = flashGO.AddComponent<SpriteRenderer>();
                muzzleFlashRenderer.sortingOrder = 12;
                muzzleFlashRenderer.enabled = false;
            }

            if (muzzleFlashSprite != null && muzzleFlashRenderer != null)
            {
                muzzleFlashRenderer.sprite = muzzleFlashSprite;
            }
        }

        public void EquipWeapon(int index)
        {
            EnsureWeaponHierarchy();

            if (weapons == null || index < 0 || index >= weapons.Length || weapons[index] == null)
            {
                currentWeaponIndex = -1;
                if (weaponRenderer != null) weaponRenderer.enabled = false;
                if (muzzleFlashRenderer != null) muzzleFlashRenderer.enabled = false;
                return;
            }

            currentWeaponIndex = index;
            var w = weapons[index];
            if (weaponRenderer != null)
            {
                weaponRenderer.sprite = w.sprite;
                weaponRenderer.enabled = w.sprite != null;
            }
            if (muzzleFlashRenderer != null && muzzleFlashSprite != null)
            {
                muzzleFlashRenderer.sprite = muzzleFlashSprite;
            }
        }

        public void UnlockAndEquipWeapon(int index)
        {
            if (weapons != null && index >= 0 && index < weapons.Length && weapons[index] != null)
            {
                weapons[index].currentAmmo = weapons[index].magSize;
                if (weapons[index].reserveAmmo < weapons[index].magSize * 2)
                {
                    weapons[index].reserveAmmo = weapons[index].magSize * 4;
                }
            }
            EquipWeapon(index);
        }

        private void Update()
        {
            if (GameLoop.Instance != null && GameLoop.Instance.State != RunState.Running)
            {
                if (muzzleFlashRenderer != null) muzzleFlashRenderer.enabled = false;
                return;
            }

            HandleWeaponSwitchInput();

            var w = CurrentWeapon;
            if (w == null || weaponHolder == null || weaponRenderer == null)
            {
                if (weaponRenderer != null) weaponRenderer.enabled = false;
                if (muzzleFlashRenderer != null) muzzleFlashRenderer.enabled = false;
                return;
            }

            if (muzzleFlashTimer > 0f)
            {
                muzzleFlashTimer -= Time.deltaTime;
                if (muzzleFlashTimer <= 0f && muzzleFlashRenderer != null)
                {
                    muzzleFlashRenderer.enabled = false;
                }
            }

            recoilOffset = Mathf.MoveTowards(recoilOffset, 0f, Time.deltaTime * 0.9f);

            Vector2 aimDir = GetMouseAimDirection();
            UpdateWeaponTransform(w, aimDir);
            HandleFireInput(w, aimDir);
        }

        private void HandleWeaponSwitchInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) EquipWeapon(0);
                else if (kb.digit2Key.wasPressedThisFrame) EquipWeapon(1);
                else if (kb.digit3Key.wasPressedThisFrame) EquipWeapon(2);
                else if (kb.digit0Key.wasPressedThisFrame) EquipWeapon(-1);
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && weapons != null && weapons.Length > 0)
            {
                float scrollY = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.1f)
                {
                    int dir = scrollY > 0f ? 1 : -1;
                    int next = currentWeaponIndex < 0 ? 0 : (currentWeaponIndex + dir + weapons.Length) % weapons.Length;
                    EquipWeapon(next);
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1)) EquipWeapon(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) EquipWeapon(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) EquipWeapon(2);
            else if (Input.GetKeyDown(KeyCode.Alpha0)) EquipWeapon(-1);
#endif
        }

        private Vector2 GetMouseAimDirection()
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector2.right;

            Vector2 mouseScreen;
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            mouseScreen = mouse != null ? mouse.position.ReadValue() : (Vector2)Input.mousePosition;
#else
            mouseScreen = Input.mousePosition;
#endif

            Vector3 mouseWorld = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Mathf.Abs(cam.transform.position.z)));
            Vector2 aimDir = (Vector2)mouseWorld - (Vector2)transform.position;
            return aimDir.sqrMagnitude < 0.001f ? Vector2.right : aimDir.normalized;
        }

        private void UpdateWeaponTransform(WeaponSpec w, Vector2 aimDir)
        {
            float parentScale = Mathf.Max(0.1f, Mathf.Abs(transform.localScale.x));
            float radius = Mathf.Max(0.12f, w.holdRadius - recoilOffset) / parentScale;

            weaponHolder.localPosition = new Vector3(aimDir.x * radius, aimDir.y * radius - 0.05f, 0f);
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            weaponHolder.rotation = Quaternion.Euler(0f, 0f, angle);

            float localSprScale = w.spriteScale / parentScale;
            weaponHolder.localScale = new Vector3(localSprScale, localSprScale, 1f);

            weaponRenderer.enabled = w.sprite != null;
            weaponRenderer.flipY = aimDir.x < 0f;
            int playerOrder = playerRenderer != null ? playerRenderer.sortingOrder : 1000;
            weaponRenderer.sortingOrder = aimDir.y > 0.55f ? (playerOrder - 1) : (playerOrder + 1);
            if (muzzleFlashRenderer != null)
            {
                muzzleFlashRenderer.sortingOrder = playerOrder + 2;
            }

            if (playerRenderer != null)
            {
                playerRenderer.flipX = aimDir.x < 0f;
            }
        }

        private void HandleFireInput(WeaponSpec w, Vector2 aimDir)
        {
            bool firePressed = false;
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                firePressed = w.automatic ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
            }
#else
            firePressed = w.automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
#endif

            if (firePressed && Time.time >= nextFireTime)
            {
                Fire(w, aimDir);
            }
        }

        private void Fire(WeaponSpec w, Vector2 aimDir)
        {
            if (w.currentAmmo <= 0)
            {
                if (w.reserveAmmo <= 0) w.reserveAmmo = w.magSize * 4;
                int reloadAmt = Mathf.Min(w.magSize, w.reserveAmmo);
                w.currentAmmo = reloadAmt;
                w.reserveAmmo -= reloadAmt;
            }

            nextFireTime = Time.time + (1f / Mathf.Max(0.1f, w.fireRate));
            w.currentAmmo = Mathf.Max(0, w.currentAmmo - 1);

            if (w.currentAmmo == 0)
            {
                if (w.reserveAmmo <= 0) w.reserveAmmo = w.magSize * 4;
                int reloadAmt = Mathf.Min(w.magSize, w.reserveAmmo);
                w.currentAmmo = reloadAmt;
                w.reserveAmmo -= reloadAmt;
            }

            Vector3 muzzlePos = weaponHolder.position + (Vector3)(aimDir * 0.35f);
            int pellets = Mathf.Max(1, w.pelletsPerShot);
            for (int i = 0; i < pellets; i++)
            {
                float spread = Random.Range(-w.spreadAngle * 0.5f, w.spreadAngle * 0.5f);
                Vector2 dir = (Quaternion.Euler(0f, 0f, spread) * aimDir).normalized;

                var projGO = new GameObject("Bullet_8Bit");
                projGO.transform.position = muzzlePos;
                float projAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                projGO.transform.rotation = Quaternion.Euler(0f, 0f, projAngle);
                projGO.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

                var sr = projGO.AddComponent<SpriteRenderer>();
                sr.sprite = bulletSprite;
                sr.sortingOrder = 12;

                var col = projGO.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.14f;

                var rb = projGO.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;

                var proj = projGO.AddComponent<Projectile2D>();
                proj.speed = w.projectileSpeed;
                proj.damage = w.damage;
                proj.direction = dir;
                proj.lifeTime = 1.8f;
            }

            if (muzzleFlashRenderer != null && muzzleFlashSprite != null)
            {
                muzzleFlashRenderer.sprite = muzzleFlashSprite;
                muzzleFlashRenderer.enabled = true;
                muzzleFlashTimer = 0.05f;
            }
            recoilOffset = 0.10f;
        }
    }
}
