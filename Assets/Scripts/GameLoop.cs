using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DyingHorizon
{
    public enum RunState
    {
        Ready,
        Running,
        Won,
        Lost
    }

    public class GameLoop : MonoBehaviour
    {
        public static GameLoop Instance { get; private set; }

        [Header("State")]
        [SerializeField] private RunState state = RunState.Ready;
        public RunState State => state;

        [Header("Goals")]
        public int targetResources = 5;
        public int currentResources = 0;

        [Header("Survivor Vitals")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float hunger = 100f;
        public float thirst = 100f;
        public float radiation = 0f;

        public event Action<RunState> OnStateChanged;
        public event Action<int, int> OnResourceChanged;
        public event Action<float, float> OnHealthChanged;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            StartRun();
        }

        private void Update()
        {
            if (state == RunState.Running)
            {
                hunger = Mathf.Max(0, hunger - Time.deltaTime * 0.5f);
                thirst = Mathf.Max(0, thirst - Time.deltaTime * 0.8f);

                if (hunger <= 0 || thirst <= 0)
                {
                    TakeDamage(Time.deltaTime * 2f);
                }

                if (radiation >= 100f)
                {
                    Lose();
                }
            }
            else if (state == RunState.Won || state == RunState.Lost)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
                {
                    Restart();
                }
#else
                if (Input.GetKeyDown(KeyCode.R))
                {
                    Restart();
                }
#endif
            }
        }

        public void StartRun()
        {
            currentResources = 0;
            currentHealth = maxHealth;
            hunger = 100f;
            thirst = 100f;
            radiation = 0f;
            SetState(RunState.Running);
            OnResourceChanged?.Invoke(currentResources, targetResources);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void CollectResource(int amount = 1)
        {
            if (state != RunState.Running) return;

            currentResources += amount;
            OnResourceChanged?.Invoke(currentResources, targetResources);

            if (currentResources >= targetResources)
            {
                Win();
            }
        }

        public void TakeDamage(float amount)
        {
            if (state != RunState.Running) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Lose();
            }
        }

        public void AddRadiation(float amount)
        {
            if (state != RunState.Running) return;
            radiation = Mathf.Clamp(radiation + amount, 0, 100f);
        }

        public void Win()
        {
            if (state == RunState.Running)
            {
                SetState(RunState.Won);
            }
        }

        public void Lose()
        {
            if (state == RunState.Running)
            {
                SetState(RunState.Lost);
            }
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void SetState(RunState newState)
        {
            state = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
