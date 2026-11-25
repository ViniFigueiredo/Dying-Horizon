using UnityEngine;

namespace DyingHorizon
{
    /// <summary>
    /// Quando o jogador passa por trás/debaixo da copa da árvore,
    /// aplica uma leve translucidez suave (alpha ~0.72) para que o jogador
    /// veja o sobrevivente sob a folhagem. Ao sair debaixo da árvore,
    /// o sprite retorna suavemente a 100% opaco.
    /// </summary>
    public class TreeCanopyFader2D : MonoBehaviour
    {
        [SerializeField] private float fadeAlpha = 0.72f;
        [SerializeField] private float fadeSpeed = 8f;

        private SpriteRenderer sr;
        private bool playerUnderCanopy = false;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (sr == null) return;
            float targetA = playerUnderCanopy ? fadeAlpha : 1f;
            Color c = sr.color;
            if (Mathf.Abs(c.a - targetA) > 0.005f)
            {
                c.a = Mathf.MoveTowards(c.a, targetA, Time.deltaTime * fadeSpeed);
                sr.color = c;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player") && !collision.isTrigger)
            {
                playerUnderCanopy = true;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player") && !collision.isTrigger)
            {
                playerUnderCanopy = false;
            }
        }
    }
}
