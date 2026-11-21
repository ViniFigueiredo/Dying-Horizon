using UnityEngine;

namespace DyingHorizon
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        private Rigidbody2D rb;
        private Vector2 moveInput;
        private SpriteRenderer spriteRenderer;

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
                return;
            }

            moveInput = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                float h = 0f;
                float v = 0f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;

                moveInput = new Vector2(h, v).normalized;

                if (h != 0 && spriteRenderer != null)
                {
                    spriteRenderer.flipX = h < 0;
                }
            }
#else
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            moveInput = new Vector2(h, v).normalized;

            if (h != 0 && spriteRenderer != null)
            {
                spriteRenderer.flipX = h < 0;
            }
#endif
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
