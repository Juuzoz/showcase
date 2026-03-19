using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
// This script handles the core battle system
namespace TripleJ.System
{
public class BattleSystem : MonoBehaviour
{
    [Header("Core")]
    public Enemy enemy;
    public PlayerHealth playerHealth;
    public CombatController combatcontroller;
    public GameObject player;
    public PlayerInputActions inputActions;

    [Header("Battle Settings")]
    public int damageAmount = 20;
    public float minigameDuration = 5f;
    [SerializeField] private float spawnerRestartDelay = 2.0f;
    [SerializeField] private int amountToActivate = 3; //how many projectile spawners spawn
    private float minigameTimer;
    private bool isMinigameActive = false;
    public bool IsMinigameActive => isMinigameActive;

    [Header("UI")]
    public GameObject battleUI;
    public GameObject OptionsUI;
    public TextMeshProUGUI timerText;

    [Header("Combat Systems")]
    public QuickTimeEvent quickTimeEvent;
    public RandomActivator randomActivator; //picks which spawners to activate
    public Transform projectileParent;
    public Transform spawnersParent;

    private void Start()
    {
        playerHealth.SetInvincible(true);
        StartTurnBasedBattle();
        inputActions = new PlayerInputActions(); 
    }

    public void SetEnemy(EnemyType enemyType)
    {
        if (enemy == null)
        {
            enemy = GetComponentInChildren<Enemy>();
        }

        if (enemy != null)
        {
            enemy.enemyType = enemyType;
            enemy.ResetEnemy();
        }

        // Configure systems based on enemy type
        if (enemyType != null)
        {
            minigameDuration = enemyType.minigameDuration;

            if (randomActivator != null)
            {
                randomActivator.SetProjectileConfig(enemyType.projectileConfig);
            }

            // Initializing QTE based on settings
            if (quickTimeEvent != null)
            {
                quickTimeEvent.Initialize(enemyType.qteConfig);
            }
        
            // Play the appropriate combat music
            if (CombatAudioManager.Instance != null)
            {
                CombatAudioManager.Instance.PlayCombatMusic(enemyType);
            }
        }
    }

    public void OnAttackButtonPressed()
    {
        if (enemy != null)
        {
            enemy.TakeDamage(damageAmount);
            EndTurnBasedBattle();
        }
    }

    public void OnInstaWinButtonPressed()
{
    // Apply massive damage to instantly defeat the enemy
    if (enemy != null)
    {
        // 500 damage should be enough to defeat any enemy
        enemy.TakeDamage(500);
    }
}

    private void StartTurnBasedBattle()
    {
        battleUI.SetActive(true); 
    }

    private void EndTurnBasedBattle()
    {
        battleUI.SetActive(false);

        // Restart all projectile spawners
        ProjectileSpawner[] spawners = FindObjectsOfType<ProjectileSpawner>();
        foreach (ProjectileSpawner spawner in spawners)
        {
            spawner.RestartProjectileSpawner(spawnerRestartDelay);
        }
        randomActivator.RefreshActiveChildren(amountToActivate);

        StartMinigame();
    }

    private void StartMinigame()
    {
        if (player != null)
        {
            player.transform.position = Vector3.zero; // Resets player position at the start of minigame
        }
        playerHealth.SetInvincible(false);
        combatcontroller.ToggleMovement(true);
        minigameTimer = minigameDuration;
        isMinigameActive = true;
    }

    private void Update()
    {
        if (isMinigameActive) // The dodging phase length
        {
            minigameTimer -= Time.deltaTime;
            if (timerText != null)
            {
                timerText.text = "Survive: " + Mathf.Ceil(minigameTimer).ToString();
            }

            if (minigameTimer <= 0)
            {
                EndMinigame();
            }
        }
    }

    private void EndMinigame()
    {
        isMinigameActive = false;
        combatcontroller.ToggleMovement(false);
        playerHealth.SetInvincible(true);
        OptionsPhase();

    }

    private void OptionsPhase()
    {
        battleUI.SetActive(true); // Choosing whether or not to use items
        OptionsUI.SetActive(true);
    }

    public void OnBattleButtonPressed()
    {
        playerHealth.SetInvincible(true);
        quickTimeEvent.OnEnable();
        OptionsUI.SetActive(false);
        StartTurnBasedBattle();
    }
    public void RestartGame()
    {
        GameManager.Instance.EndCombat();
        playerHealth.ResetHealth();
    }
}
}
