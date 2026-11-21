using UnityEngine;

namespace DyingHorizon
{
    [RequireComponent(typeof(Collider2D))]
    public class ResourceItem2D : MonoBehaviour
    {
        public string resourceName = "Madeira e Peças";
        public int amount = 1;
        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.position;
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void Update()
        {
            transform.position = startPos + new Vector3(0, Mathf.Sin(Time.time * 3f + startPos.x) * 0.1f, 0);
        }

        public void Collect()
        {
            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.CollectResource(amount);
            }
            Destroy(gameObject);
        }
    }
}
