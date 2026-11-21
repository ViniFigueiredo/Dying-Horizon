using UnityEngine;

namespace DyingHorizon
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private bool autoDemo = true;

        [Header("8-Bit Walk Animation")]
        public Sprite[] walkSprites;
        [SerializeField] private float animationFps = 8f;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private SpriteRenderer spriteRenderer;
        private float animTimer;
        private int animIndex;

        private readonly Vector2[] demoWaypoints = new Vector2[]
        {
            new Vector2(2.5f, 1.8f),
            new Vector2(-2.0f, 1.5f),
            new Vector2(-2.2f, -2.5f),
            new Vector2(3.0f, -2.5f),
            new Vector2(0.0f, 0.0f)
        };
        private int waypointIndex;

        public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;
        public bool IsAutoDemo => autoDemo;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (GameLoop.Instance != null && GameLoop.Instance.State != RunState.Running)
            {
                moveInput = Vector2.zero;
                UpdateAnimation(false);
                return;
            }

            Vector2 manualInput = Vector2.zero;
            bool tabPressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.tabKey.wasPressedThisFrame) tabPressed = true;
                float h = 0f;
                float v = 0f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
                manualInput = new Vector2(h, v).normalized;
            }
#else
            if (Input.GetKeyDown(KeyCode.Tab)) tabPressed = true;
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            manualInput = new Vector2(h, v).normalized;
#endif

            if (tabPressed)
            {
                autoDemo = !autoDemo;
            }

            if (manualInput.sqrMagnitude > 0.01f)
            {
                autoDemo = false;
                moveInput = manualInput;
            }
            else if (autoDemo)
            {
                Vector2 target = demoWaypoints[waypointIndex];
                Vector2 diff = target - (Vector2)transform.position;
                if (diff.magnitude < 0.25f)
                {
                    waypointIndex = (waypointIndex + 1) % demoWaypoints.Length;
                    target = demoWaypoints[waypointIndex];
                    diff = target - (Vector2)transform.position;
                }
                moveInput = diff.normalized;
            }
            else
            {
                moveInput = Vector2.zero;
            }

            if (Mathf.Abs(moveInput.x) > 0.05f && spriteRenderer != null)
            {
                spriteRenderer.flipX = moveInput.x < 0f;
            }

            UpdateAnimation(moveInput.sqrMagnitude > 0.01f);
        }

        private void UpdateAnimation(bool isMoving)
        {
            if (spriteRenderer == null || walkSprites == null || walkSprites.Length == 0) return;

            if (!isMoving)
            {
                animIndex = 0;
                animTimer = 0f;
                spriteRenderer.sprite = walkSprites[0];
                return;
            }

            animTimer += Time.deltaTime * animationFps;
            if (animTimer >= 1f)
            {
                animTimer -= 1f;
                // Cycle: 0 -> 1 -> 0 -> 2
                animIndex = (animIndex + 1) % 4;
                int frame = animIndex == 1 ? 1 : (animIndex == 3 && walkSprites.Length > 2 ? 2 : 0);
                spriteRenderer.sprite = walkSprites[Mathf.Clamp(frame, 0, walkSprites.Length - 1)];
            }
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent<ResourceItem2D>(out var resource))
            {
                resource.Collect();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.TryGetComponent<MutantEnemy2D>(out var enemy))
            {
                enemy.AttackPlayer(this);
            }
        }
    }
}
