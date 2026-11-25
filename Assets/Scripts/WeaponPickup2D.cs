using System.Collections;
using UnityEngine;

namespace DyingHorizon
{
    /// <summary>
    /// Pickup coletável no chão para equipar uma das armas do jogo.
    /// </summary>
    public class WeaponPickup2D : MonoBehaviour
    {
        public int weaponIndex;
        public string weaponLabel;

        private Vector3 basePos;
        private SpriteRenderer sr;
        private Collider2D col;
        private bool collected;

        private void Awake()
        {
            basePos = transform.position;
            sr = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();
        }

        public void SetBasePosition(Vector3 pos)
        {
            basePos = pos;
            transform.position = pos;
        }

        private void Update()
        {
            if (collected) return;
            transform.position = basePos + new Vector3(0f, Mathf.Sin(Time.time * 3.5f) * 0.06f, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;
            if (other.TryGetComponent<WeaponController2D>(out var wc))
            {
                wc.UnlockAndEquipWeapon(weaponIndex);
                StartCoroutine(RespawnRoutine());
            }
        }

        private IEnumerator RespawnRoutine()
        {
            collected = true;
            if (sr != null) sr.enabled = false;
            if (col != null) col.enabled = false;
            yield return new WaitForSeconds(6f);
            collected = false;
            if (sr != null) sr.enabled = true;
            if (col != null) col.enabled = true;
        }
    }
}
