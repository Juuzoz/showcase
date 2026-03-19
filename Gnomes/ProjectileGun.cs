using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Gnome.Gun
{

public class ProjectileGun : MonoBehaviour
{
    //bullet
    public GameObject bullet;

    //bullet force
    public float shootForce, upwardForce;

    //Gun stats
    public float timeBetweenShooting, spread, reloadTime, timeBetweenShots;
    public int magazineSize, bulletsPerTap;
    public bool allowButtonHold;

    int bulletsLeft, bulletsShot;

    //bools
    public bool shooting, readyToShoot, reloading;

    public bool isSucking = false;
    public bool isShooting = false;

    //Reference
    public Camera fpsCam;
    public Transform attackPoint;
    private Animator animator;
    private PlayerInput playerInput;
    private PlayerManager _playerManager;

    //Input System
    private InputAction fireAction;
    private InputAction suckAction;

    //Graphics
    public GameObject muzzleFlash;
    public TextMeshProUGUI ammunitionDisplay;

    //Audio
    [Header("Audio Settings")]
    [SerializeField] private AudioSource gunAudioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private float shootVolume = 0.7f;
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float pitchMin = 0.9f;
    [SerializeField] private float pitchMax = 1.1f;

    //debug
    public bool allowInvoke = true;

    // Sucking mechanics
    [Header("Sucking Settings")]
    [SerializeField] private float suckingRange = 3f;
    [SerializeField] private float suckingRate = 5f; // Potatoes per second
    private float suckingTimer = 0f;
    private GrowthArea currentTargetGrowthArea;
    [SerializeField] private Transform _detectionTransform;
    [SerializeField] private float _suckRadius = 3f;
    [SerializeField] private LayerMask _suckLayer;
    [SerializeField] private float _suckForce = 10f;
    [SerializeField] private GameObject _suctionVFX;

    [Header("Shooting Animation Names")]
    public string _shootPotato = "GunShoot_Potato";
    public string _shootPumpkin = "GunShoot_Pumpkin";
    public string _shootCarrot = "GunShoot_Carrot";

    private void Awake()
    {
        //magazine full at start
        bulletsLeft = magazineSize;
        readyToShoot = true;
        animator = GetComponent<Animator>();
        _playerManager = GetComponentInParent<PlayerManager>();
        
        // Get or add AudioSource component
        gunAudioSource = GetComponent<AudioSource>();
        if (gunAudioSource == null)
        {
            gunAudioSource = gameObject.AddComponent<AudioSource>();
            gunAudioSource.playOnAwake = false;
            gunAudioSource.spatialBlend = 0.5f; // Partial 3D sound
        }
        
        // Get PlayerInput component
        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("PlayerInput component not found! Please add it to the player GameObject.");
            return;
        }
        
        // Get input actions
        fireAction = playerInput.actions["Fire"];
        suckAction = playerInput.actions["Suck"];
    }

    private void Start()
    {
        ChangePlantType(PlantType.Potato);
    }

    private void Update()
    {
        MyInput();

        //Set ammo display if it exists
        if (ammunitionDisplay != null)
            ammunitionDisplay.SetText(bulletsLeft / bulletsPerTap + " / " + magazineSize / bulletsPerTap);
    }

    private void FixedUpdate()
    {
        if(isSucking)
            CheckForSuckableObjectsAndSuck();
    }

    private void MyInput()
    {
        // Check if fire action is pressed
        if (allowButtonHold)
        {
            shooting = fireAction.IsPressed();
        }
        else
        {
            shooting = fireAction.WasPressedThisFrame();
        }

        // Handle sucking/reloading
        if (suckAction.IsPressed())
        {
            // Check what kind of state the plant is in
            // If needs care check what care and provide
            // If grown plant, vaccuum
            CheckForGrowthArea();
            if(currentTargetGrowthArea != null)
            {
                if(currentTargetGrowthArea._ammoReady)
                {
                    currentTargetGrowthArea.SpawnAmmoOnTheGround();
                }
                else if(currentTargetGrowthArea._needCare)
                {
                    switch(currentTargetGrowthArea._careType)
                    {
                        case CareType.Dehydrated:
                            Debug.Log("Care type: Dehydrated");
                            // Water Effect
                            break;
                        case CareType.HasWeeds:
                            Debug.Log("Care type: Weeds");
                            // Vaccuum weeds
                            break;
                        case CareType.HasBugs:
                            Debug.Log("Care type: Bugs");
                            // Spray bug repellant
                            break;
                    }
                }
            }
            // Need to double check that this is in right place
            StartSucking();
        }
        else if (suckAction.WasReleasedThisFrame() && isSucking)
        {
            StopSucking();
        }

        //Shooting
        if (readyToShoot && shooting && !reloading && !isSucking && _playerManager._inventoryManager.GetCurrentlySelectedAmmoAmount() > 0 && _playerManager._canShoot)
        {
            //Set bullets shot to 0
            bulletsShot = 0;
            switch (_playerManager._inventoryManager._currentlySelectedPlant)
            {
                case PlantType.Potato:
                    _playerManager._animatorManager.PlayTargetActionAnimation(_shootPotato, true, true);
                    break;
                case PlantType.Pumpkin:
                    _playerManager._animatorManager.PlayTargetActionAnimation(_shootPumpkin, true, true); 
                    break;
                case PlantType.Carrot:
                    _playerManager._animatorManager.PlayTargetActionAnimation(_shootCarrot, true, true);
                    break;
            }
            
        }
    }

    private void CheckForGrownPlant()
    {
        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, suckingRange))
        {
            if (hit.collider.CompareTag("Crop"))
            {
                GrowthArea growthArea = hit.collider.GetComponentInParent<GrowthArea>();
                if (growthArea != null && growthArea._ammoReady)
                {
                    currentTargetGrowthArea = growthArea;
                    return;
                }
            }
        }
        
        currentTargetGrowthArea = null;
    }

    private void CheckForGrowthArea()
    {
        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, suckingRange))
        {
            if (hit.collider.CompareTag("Crop"))
            {
                GrowthArea growthArea = hit.collider.GetComponentInParent<GrowthArea>();
                if (growthArea != null)
                {
                    currentTargetGrowthArea = growthArea;
                    return;
                }
            }
        }

        currentTargetGrowthArea = null;
    }

    private void StartSucking()
    {
        if (!isSucking)
        {
            isSucking = true;
            reloading = true;
            animator.SetBool("isSucking", true);
            suckingTimer = 0f;
            _suctionVFX.SetActive(true);
        }
        /*else
        {
            // Continue sucking
            suckingTimer += Time.deltaTime;
            CheckForSuckableObjectsAndSuck();
            // Calculate how many potatoes to suck this frame
            float potatoesToSuck = suckingRate * Time.deltaTime;
            int wholePotatoes = Mathf.FloorToInt(suckingTimer * suckingRate) - Mathf.FloorToInt((suckingTimer - Time.deltaTime) * suckingRate);
            
            if (wholePotatoes > 0)
            {
                // Get potatoes from the growth area
                int potatoesReceived = currentTargetGrowthArea.SuckAmmo(wholePotatoes);
                
                // Add to our ammo
                bulletsLeft = Mathf.Min(bulletsLeft + potatoesReceived, magazineSize);
                
                // If no more potatoes available or magazine full, stop sucking
                if (potatoesReceived == 0 || bulletsLeft >= magazineSize)
                {
                    StopSucking();
                }
            }
        }*/
    }

    private void StopSucking()
    {
        isSucking = false;
        reloading = false;
        animator.SetBool("isSucking", false);
        suckingTimer = 0f;
        currentTargetGrowthArea = null;
        _suctionVFX.SetActive(false);
    }
    
    public void Shoot()
    {
        readyToShoot = false;

        // Play shoot sound
        PlayShootSound();

        //Find hit position
        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //ray through the middle of the screen
        RaycastHit hit;

        //check if ray hits
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(75); //a point away from the player

        //Calc direction from attackPoint to targetPoint
        Vector3 directionWithoutSpread = targetPoint - attackPoint.position;

        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);
        float z = Random.Range(-spread, spread);

        //Calc direction with spread
        Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, z);

        //Instantiate bullet/projectile
        GameObject currentBullet = Instantiate(_playerManager._inventoryManager.GetCurrentlySelectedAmmo(), attackPoint.position, Quaternion.identity);

        //Rotate bullet to shot direction
        currentBullet.transform.forward = directionWithSpread.normalized;

        //Add forces to bullet
        float projectileSpeed = currentBullet.GetComponent<Projectile>()._projectileSpeed;
        currentBullet.GetComponent<Rigidbody>().AddForce(directionWithSpread.normalized * projectileSpeed, ForceMode.Impulse);
        currentBullet.GetComponent<Rigidbody>().AddForce(fpsCam.transform.up * upwardForce, ForceMode.Impulse);

        if (muzzleFlash != null)
            Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);

        bulletsLeft--;
        _playerManager._inventoryManager.RemoveAmmo();
        bulletsShot++;

        //Invoke resetShot function with your timeBetweenShooting
        /*if (allowInvoke)
        {
            Invoke("ResetShot", timeBetweenShooting);
            allowInvoke = false;
        }*/

        //if more than one bulletsPerTap make sure to repeat shoot function
        /*if (bulletsShot < bulletsPerTap && _playerManager._inventoryManager.GetCurrentlySelectedAmmoAmount() > 0)
            Invoke("Shoot", timeBetweenShots);*/
    }

    private void PlayShootSound()
    {
        if (shootSound != null && gunAudioSource != null)
        {
            // Randomize pitch for variety
            if (randomizePitch)
            {
                gunAudioSource.pitch = Random.Range(pitchMin, pitchMax);
            }
            else
            {
                gunAudioSource.pitch = 1f;
            }

            // Play the sound
            gunAudioSource.PlayOneShot(shootSound, shootVolume);
        }
    }
    
    private void ResetShot()
    {
        //Allow shooting and invoking again
        readyToShoot = true;
        allowInvoke = true;
        animator.SetBool("isShooting", false);
    }

    // Old reload method - no longer used but kept for compatibility
    private void Reload()
    {
        reloading = true;
        Invoke("ReloadFinished", reloadTime);
        animator.SetBool("isSucking", true);
    }

    private void ReloadFinished()
    {
        bulletsLeft = magazineSize;
        reloading = false;
        animator.SetBool("isSucking", false);
    }
    
    private void OnEnable()
    {
        if (fireAction != null)
            fireAction.Enable();
        if (suckAction != null)
            suckAction.Enable();
    }
    
    private void OnDisable()
    {
        if (fireAction != null)
            fireAction.Disable();
        if (suckAction != null)
            suckAction.Disable();
    }

    public void ChangePlantType(PlantType newPlantType)
    {
        _playerManager._inventoryManager._currentlySelectedPlant = newPlantType;
        bulletsLeft = _playerManager._inventoryManager.GetCurrentlySelectedAmmoAmount();
    }

    public void CheckForSuckableObjectsAndSuck()
    {
        Collider[] colliders = Physics.OverlapSphere(_detectionTransform.position, _suckRadius, _suckLayer);
        foreach(Collider collider in colliders)
        {
            if(collider.GetComponent<AmmoObject>() == null)
                continue;

            Rigidbody rb = collider.GetComponent<Rigidbody>();

            if(rb == null)
                continue;

            Vector3 direction = (transform.position - rb.position).normalized;

            rb.AddForce(direction * _suckForce, ForceMode.Acceleration);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_detectionTransform.position, _suckRadius);
    }
}
}
