using UnityEngine;

namespace DyingHorizon
{
    /// <summary>
    /// Projétil 2D disparado pelas armas do sobrevivente.
    /// </summary>
    public class Projectile2D : MonoBehaviour
    {
        public float speed = 15f;
        public float damage = 25f;
        public Vector2 direction = Vector2.right;
        public float lifeTime = 1.8f;

        private void Update()
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.isTrigger) return;

            if (other.TryGetComponent<MutantEnemy2D>(out var enemy))
            {
                enemy.TakeDamage(damage, direction);
                Destroy(gameObject);
                return;
            }

            // Destrói ao colidir com obstáculos sólidos (rochas, casebres, oceano)
            Destroy(gameObject);
        }
    }
}
