using UnityEngine;

namespace DyingHorizon
{
    /// <summary>
    /// Gerencia a profundidade 2D (sortingOrder) dinâmica baseada na posição Y da base do objeto.
    /// Permite que o jogador fique atrás/debaixo da copa de árvores e estruturas ao caminhar por trás delas,
    /// e à frente do tronco ao caminhar pela frente.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class YSort2D : MonoBehaviour
    {
        public float pivotOffsetY = 0f;
        public int sortingOrderOffset = 0;
        public bool isStatic = false;

        private SpriteRenderer sr;
        private SpriteRenderer shadowSr;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            FindShadow();
            UpdateSortOrder();
        }

        private void Start()
        {
            FindShadow();
            UpdateSortOrder();
        }

        private void FindShadow()
        {
            if (shadowSr == null)
            {
                var shadowTr = transform.Find("Shadow");
                if (shadowTr != null)
                {
                    shadowSr = shadowTr.GetComponent<SpriteRenderer>();
                }
            }
        }

        private void LateUpdate()
        {
            if (!isStatic || !Application.isPlaying)
            {
                UpdateSortOrder();
            }
        }

        public void UpdateSortOrder()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float baseY = transform.position.y + pivotOffsetY;
                // Quanto menor o Y (mais ao sul), maior o sortingOrder (desenhado na frente)
                sr.sortingOrder = 1000 - Mathf.RoundToInt(baseY * 32f) + sortingOrderOffset;

                if (shadowSr != null)
                {
                    shadowSr.sortingOrder = sr.sortingOrder - 2;
                }
            }
        }
    }
}
