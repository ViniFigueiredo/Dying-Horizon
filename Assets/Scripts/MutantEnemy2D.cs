using UnityEngine;

namespace DyingHorizon
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class MutantEnemy2D : MonoBehaviour
    {
        public string mutantName = "Kraby Mutante";
        public float damage = 20f;
        public float moveSpeed = 2f;
        public float detectionRadius = 6f;
        public Transform targetPlayer;

        private Rigidbody2D rb;
        private Vector2 originPosition;
        private float lastAttackTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            originPosition = transform.position;
        }

        private void Start()
        {
            if (targetPlayer == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) targetPlayer = player.transform;
            }
        }

        private void FixedUpdate()
        {
            if (GameLoop.Instance != null && GameLoop.Instance.State != RunState.Running)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (targetPlayer != null)
            {
                float dist = Vector2.Distance(transform.position, targetPlayer.position);
                if (dist < detectionRadius)
                {
                    Vector2 dir = ((Vector2)targetPlayer.position - (Vector2)transform.position).normalized;
                    rb.linearVelocity = dir * moveSpeed;
                    return;
                }
            }

            if (Vector2.Distance(transform.position, originPosition) > 0.5f)
            {
                Vector2 dir = (originPosition - (Vector2)transform.position).normalized;
                rb.linearVelocity = dir * (moveSpeed * 0.5f);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        public void AttackPlayer(PlayerController2D player)
        {
            if (Time.time - lastAttackTime < 1.0f) return;
            lastAttackTime = Time.time;

            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.TakeDamage(damage);
                GameLoop.Instance.AddRadiation(10f);
            }
        }
    }
}
