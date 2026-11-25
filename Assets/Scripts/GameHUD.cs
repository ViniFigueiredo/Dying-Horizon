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
        public TextMeshProUGUI controlsText;

        [Header("Panels")]
        public GameObject winPanel;
        public GameObject losePanel;

        private PlayerController2D playerController;
        private WeaponController2D weaponController;

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController2D>();
                weaponController = player.GetComponent<WeaponController2D>();
            }

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
                string weaponInfo = "Desarmado ([1][2][3] Equipar)";
                if (weaponController != null && weaponController.CurrentWeapon != null)
                {
                    var w = weaponController.CurrentWeapon;
                    weaponInfo = $"Arma: <b>{w.name}</b> [{w.currentAmmo}/{w.reserveAmmo}]";
                }
                statusText.text = $"Fome: {GameLoop.Instance.hunger:0}% | Sede: {GameLoop.Instance.thirst:0}% | Radiação: {GameLoop.Instance.radiation:0}%\n{weaponInfo}";
            }

            if (controlsText != null && playerController != null)
            {
                string demoTag = playerController.IsAutoDemo ? " | <color=#FFDD55>[Tab] Auto-Demo ATIVADO</color>" : "";
                controlsText.text = $"<b>[WASD]</b> Mover | <b>[Mouse + Clique]</b> Mirar e Atirar | <b>[1]</b> Pistola <b>[2]</b> Escopeta <b>[3]</b> Fuzil <b>[0]</b> Guardar | <b>[R]</b> Reiniciar{demoTag}";
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
