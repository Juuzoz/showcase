using UnityEngine;
using System.Collections;
// a game manager that handles the transition between exploration and combat

namespace TripleJ.System
{
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState { Exploration, Combat }
    public GameState CurrentState { get; private set; }
    
    [Header("State Control")]
    [SerializeField] private bool _isTransitioning;
    [SerializeField] private EnemyEncounter currentEncounter;

    [Header("Player Components")]
    public PlayerMovement playerMovement;
    public CombatController combatController;

    [Header("Systems")]
    public GameObject explorationSystem;
    public GameObject combatSystem;
    public BattleSystem battleSystem;

    [Header("UI")]
    public GameObject explorationUI;
    public GameObject combatUI;

    [Header("Camera")]
    public Camera startingRoomCamera;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SwitchToExplorationState();
    }

    public void ResetToStartingRoom()
    {
        if (startingRoomCamera != null)
        {
            Camera[] allCameras = FindObjectsOfType<Camera>();
            foreach (var camera in allCameras)
            {
                camera.enabled = (camera == startingRoomCamera);
            }
        }
    }

    public void StartCombat(EnemyType enemyType)
    {
        if (_isTransitioning) return;
        battleSystem.SetEnemy(enemyType);
        StartCoroutine(TransitionToCombat());
    }

    public void EndCombat()
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionToExploration());
    }

    private void SwitchToExplorationState()
    {
        CurrentState = GameState.Exploration;
        explorationSystem.SetActive(true);
        //turning off combatsystem
        combatSystem.SetActive(false);
        explorationUI.SetActive(true);
        combatUI.SetActive(false);
        playerMovement.enabled = true;
        combatController.enabled = false;
        //initializing inputs
        FindObjectOfType<UIManager>().InitializeInputs();
        FindObjectOfType<PlayerInventory>().InitializeInputs();
        DialogueUI dialogueUI = FindObjectOfType<DialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.InitializeInputs();
        }
        //checking opened chests
        ChestScript[] chests = FindObjectsOfType<ChestScript>();
        foreach (var chest in chests)
        {
            chest.InitializeInputs();
        }
    }

    private void SwitchToCombatState()
    {
        CurrentState = GameState.Combat;
        explorationSystem.SetActive(false);
        combatSystem.SetActive(true);
        explorationUI.SetActive(false);
        combatUI.SetActive(true);
        playerMovement.enabled = false;
        combatController.enabled = true;
    }

    private IEnumerator TransitionToCombat()
    {
        _isTransitioning = true;

        // Short delay to ensure state consistency
        yield return new WaitForEndOfFrame();

        // Switch to combat state
        SwitchToCombatState();

        _isTransitioning = false;
    }

    private IEnumerator TransitionToExploration()
    {
        _isTransitioning = true;

        ProjectileSpawner[] spawners = FindObjectsOfType<ProjectileSpawner>();
        foreach (ProjectileSpawner spawner in spawners)
        {
            spawner.ForceClearProjectiles();
        }

        // Short delay to prevent edge cases
        yield return new WaitForEndOfFrame();

        SwitchToExplorationState();

        _isTransitioning = false;
    }
}
}