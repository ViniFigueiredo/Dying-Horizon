using UnityEngine;

namespace DyingHorizon
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class MutantEnemy2D : MonoBehaviour
    {
        public string mutantName = "Kraby Mutante";
        public float maxHealth = 60f;
        public float currentHealth = 60f;
        public float damage = 20f;
        public float moveSpeed = 2f;
        public float detectionRadius = 8f;
        public Transform targetPlayer;

        [Header("8-Bit Walk Animation")]
        public Sprite[] walkSprites;
        [SerializeField] private float animationFps = 8f;

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Vector2 originPosition;
        private float lastAttackTime;
        private float hitFlashTimer;
        private float animTimer;
        private int animIndex;

        // Patrol & Navigation
        private Vector2 patrolTarget;
        private float patrolWaitTimer;
        private float stuckTimer;
        private Vector2 lastPosition;
        private Vector2 avoidTangent;
        private float avoidTimer;

        public void SetOrigin(Vector2 pos)
        {
            originPosition = pos;
            patrolTarget = pos;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            sr = GetComponent<SpriteRenderer>();
            originPosition = transform.position;
            patrolTarget = transform.position;
            currentHealth = maxHealth;
            lastPosition = transform.position;
        }

        private void Start()
        {
            if (targetPlayer == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) targetPlayer = player.transform;
            }
        }

        private void Update()
        {
            if (hitFlashTimer > 0f)
            {
                hitFlashTimer -= Time.deltaTime;
                if (sr != null)
                {
                    sr.color = hitFlashTimer > 0f ? new Color(1f, 0.35f, 0.35f) : Color.white;
                }
            }

            // Animação 8-bits e flip horizontal
            UpdateAnimationAndFacing();
        }

        private void UpdateAnimationAndFacing()
        {
            if (sr == null) return;

            Vector2 vel = rb != null ? rb.linearVelocity : Vector2.zero;
            bool isMoving = vel.sqrMagnitude > 0.04f;

            // Flip horizontal (sprites base virados para a direita por padrão)
            if (vel.x < -0.05f)
            {
                sr.flipX = true;
            }
            else if (vel.x > 0.05f)
            {
                sr.flipX = false;
            }

            // Ciclo de frames da caminhada
            if (walkSprites != null && walkSprites.Length > 0)
            {
                if (isMoving)
                {
                    animTimer += Time.deltaTime * animationFps;
                    if (animTimer >= 1f)
                    {
                        animTimer -= 1f;
                        animIndex = (animIndex + 1) % walkSprites.Length;
                        sr.sprite = walkSprites[animIndex];
                    }
                }
                else
                {
                    animTimer = 0f;
                    animIndex = 0;
                    sr.sprite = walkSprites[0];
                }
            }
        }

        private void FixedUpdate()
        {
            if (GameLoop.Instance != null && GameLoop.Instance.State != RunState.Running)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // Verificação de perseguição do jogador
            if (targetPlayer != null)
            {
                float dist = Vector2.Distance(transform.position, targetPlayer.position);
                if (dist < detectionRadius)
                {
                    Vector2 toPlayer = ((Vector2)targetPlayer.position - (Vector2)transform.position).normalized;
                    Vector2 moveDir = CalculateMoveDirection(toPlayer);
                    rb.linearVelocity = moveDir * moveSpeed;
                    return;
                }
            }

            // Estado de patrulha / ronda orgânica na área
            UpdatePatrol();
        }

        private Vector2 CalculateMoveDirection(Vector2 desiredDir)
        {
            // Detector de colisão à frente para desvio suave de árvores/rochas
            if (avoidTimer > 0f)
            {
                avoidTimer -= Time.fixedDeltaTime;
                return (desiredDir + avoidTangent * 1.2f).normalized;
            }

            RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.35f, desiredDir, 0.75f);
            if (hit.collider != null && !hit.collider.isTrigger && hit.transform != targetPlayer && hit.transform != transform)
            {
                // Escolhe tangente de desvio baseada na normal da superfície
                avoidTangent = Vector2.Perpendicular(hit.normal);
                avoidTimer = 0.45f;
                return (desiredDir + avoidTangent * 1.2f).normalized;
            }

            // Detector anti-travamento caso colida e não consiga avançar
            float movedDist = Vector2.Distance(transform.position, lastPosition);
            lastPosition = transform.position;
            if (movedDist < 0.015f)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer > 0.35f)
                {
                    avoidTangent = Random.value > 0.5f ? Vector2.Perpendicular(desiredDir) : -Vector2.Perpendicular(desiredDir);
                    avoidTimer = 0.5f;
                    stuckTimer = 0f;
                    return (desiredDir + avoidTangent * 1.5f).normalized;
                }
            }
            else
            {
                stuckTimer = 0f;
            }

            return desiredDir;
        }

        private void UpdatePatrol()
        {
            float distToTarget = Vector2.Distance(transform.position, patrolTarget);
            if (distToTarget > 0.35f)
            {
                Vector2 dir = (patrolTarget - (Vector2)transform.position).normalized;
                Vector2 moveDir = CalculateMoveDirection(dir);
                rb.linearVelocity = moveDir * (moveSpeed * 0.55f);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                patrolWaitTimer -= Time.fixedDeltaTime;
                if (patrolWaitTimer <= 0f)
                {
                    patrolWaitTimer = Random.Range(2.0f, 4.5f);
                    Vector2 randOffset = Random.insideUnitCircle * Random.Range(1.2f, 3.5f);
                    patrolTarget = originPosition + randOffset;
                }
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

        public void TakeDamage(float amount, Vector2 hitDirection)
        {
            currentHealth -= amount;
            hitFlashTimer = 0.12f;
            if (sr != null) sr.color = new Color(1f, 0.35f, 0.35f);

            if (hitDirection.sqrMagnitude > 0.001f)
            {
                transform.position += (Vector3)(hitDirection.normalized * 0.18f);
            }

            if (currentHealth <= 0f)
            {
                // Respawn mutante afastado para manter o loop de combate ativo
                Vector2 offset = Random.insideUnitCircle.normalized * 6f;
                transform.position = (Vector3)(originPosition + offset);
                patrolTarget = transform.position;
                currentHealth = maxHealth;
                if (sr != null) sr.color = Color.white;
                hitFlashTimer = 0f;
            }
        }
    }
}
