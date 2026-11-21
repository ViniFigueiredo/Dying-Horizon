using TMPro;
using UnityEngine;

namespace DyingHorizon
{
    public class GameHUD : MonoBehaviour
    {
        [Header("HUD References")]
        public TextMeshProUGUI goalText;
        public TextMeshProUGUI healthText;
        public TextMeshProUGUI statusText;

        [Header("Panels")]
        public GameObject winPanel;
        public GameObject losePanel;

        private void Start()
        {
            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.OnStateChanged += HandleStateChanged;
                GameLoop.Instance.OnResourceChanged += HandleResourceChanged;
                GameLoop.Instance.OnHealthChanged += HandleHealthChanged;

                HandleResourceChanged(GameLoop.Instance.currentResources, GameLoop.Instance.targetResources);
                HandleHealthChanged(GameLoop.Instance.currentHealth, GameLoop.Instance.maxHealth);
                HandleStateChanged(GameLoop.Instance.State);
            }
        }

        private void OnDestroy()
        {
            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.OnStateChanged -= HandleStateChanged;
                GameLoop.Instance.OnResourceChanged -= HandleResourceChanged;
                GameLoop.Instance.OnHealthChanged -= HandleHealthChanged;
            }
        }

        private void Update()
        {
            if (GameLoop.Instance != null && statusText != null)
            {
                statusText.text = $"Fome: {GameLoop.Instance.hunger:0}% | Sede: {GameLoop.Instance.thirst:0}% | Radiação: {GameLoop.Instance.radiation:0}%";
            }
        }

        private void HandleResourceChanged(int current, int target)
        {
            if (goalText != null)
            {
                goalText.text = $"Zona Litoral — Recursos para o Abrigo: {current} / {target}";
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthText != null)
            {
                healthText.text = $"Vida: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void HandleStateChanged(RunState state)
        {
            if (winPanel != null) winPanel.SetActive(state == RunState.Won);
            if (losePanel != null) losePanel.SetActive(state == RunState.Lost);
        }
    }
}
