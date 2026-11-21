using UnityEngine;

namespace DyingHorizon
{
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        [SerializeField] private float smoothSpeed = 6f;

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
                else return;
            }

            Vector3 desired = new Vector3(target.position.x, target.position.y, -10f);
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smoothSpeed);
        }
    }
}
