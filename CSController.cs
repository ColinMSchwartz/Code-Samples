using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class CSController : MonoBehaviour
{
    // attached gameObjects and components
    public GameObject FPCam;
    public GameObject TPCam;
    public GameObject currentCam;
    private CharacterController charControl;
    public Animator bodyAnim;

    // player parameters
    public float movementSpeed = 10;
    public float groundMoveSharpness = 15f;
    public float jumpForce = 10f;
    public float gravityForce = 9.8f;
    public float wallRunThreshold = -2.0f;
    float groundCheckDistance = 0.05f;
    float airGroundCheckDistance = 0.2f;
    Vector3 groundNormal;
    float jumpPreventionTime = 0.5f;
    float shootPreventionTime = 0.5f;
    LayerMask groundCheckLayers = -1;
    
    // inputs
    public PlayerInputActions movement;
    public Vector2 move;
    public Vector2 look;
    public Vector2 gameLook;
    public bool canMove;

    // player stats
    public int team;
    float timeLastJumped = Mathf.NegativeInfinity;
    bool hasJumpedThisFrame;
    public bool isGrounded;
    public bool isWallRunning;
    public Vector3 wallNormal;
    float timeLastWallJumped = Mathf.NegativeInfinity;
    public Vector3 characterVelocity;
    float cameraVertical;
    public bool hasDoubleJumped;
    public float sprintMod;

    // weapon stats
    public int maxAmmo;
    public int currentAmmo;
    float timeLastShot = Mathf.NegativeInfinity;
    public bool hasWeapon;
    public PhotonShotgun currentWeapon;
    public AudioSource gun;
    public AudioClip shootSound;

    // networking
    public PhotonView PV;
    public bool isOnline;

    public GameObject menu;
    public bool menuStatus = false;

    public float mouseSensMultiply;
    public float gamepadSensMultiply;

    void Awake()
    {
        movement = new PlayerInputActions();
        movement.MenuControls.Disable();
        movement.PlayerControls.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
        movement.PlayerControls.Look.performed += ctx => look = ctx.ReadValue<Vector2>();
        movement.PlayerControls.GameLook.performed += ctx => gameLook = ctx.ReadValue<Vector2>();
        movement.PlayerControls.Jump.started += ctx => Jump();
        movement.PlayerControls.Fire.started += ctx => Shoot();
        movement.PlayerControls.Interact.started += ctx => DoInteract();
        movement.PlayerControls.ToggleCamera.started += ctx => SwitchCamera();
        movement.PlayerControls.Menu.started += ctx => OpenMenu();
        movement.PlayerControls.MegaLaunch.performed += ctx => MegaLaunch();
        movement.PlayerControls.Reload.started += ctx => Reload();
        movement.PlayerControls.Sprint.performed += ctx => sprintMod = ctx.ReadValue<float>();


        mouseSensMultiply = PlayerPrefs.GetFloat("mouseSens", 1f);
        gamepadSensMultiply = PlayerPrefs.GetFloat("gamepadSens", 1f);
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        charControl = GetComponent<CharacterController>();
        currentAmmo = maxAmmo;

        if (!isOnline || (isOnline && PV.IsMine))
        {
            FPCam.GetComponent<Camera>().enabled = true;
            currentCam = FPCam;
            FPCam.GetComponent<AudioListener>().enabled = true;
            canMove = true;
            FPCam.GetComponent<Camera>().cullingMask |= 1 << LayerMask.NameToLayer("FPWeapon");
        }
        else if (isOnline && PV.IsMine)
        {
            FPCam.GetComponent<Camera>().enabled = true;
            FPCam.GetComponent<AudioListener>().enabled = true;
        }
        if (isOnline)
        {
            DontDestroyOnLoad(this.gameObject);
        }

    }

    void FixedUpdate()
    {
        UpdateSensitivity();

    }

    // Update is called once per frame
    void Update()
    {
        if (!isOnline)
        {
            if (canMove)
            {
                hasJumpedThisFrame = false;
                GroundCheck();
                GetMove();
                JumpAnim();
            }
        }

       else
       {
            if (PV.IsMine && canMove)
            {
                hasJumpedThisFrame = false;
                GroundCheck();
                GetMove();
                JumpAnim();

            }
       }
    }

    void JumpAnim(){
        if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(charControl.height), charControl.radius, Vector3.down, out RaycastHit hit, 1f, groundCheckLayers, QueryTriggerInteraction.Ignore))
        {
            if(hit.distance < 0.2f){
                bodyAnim.SetBool("Air", false);
            }
            else{
                bodyAnim.SetBool("Air", true);
            }
        }

        if(sprintMod > 0.001f){
            bodyAnim.SetBool("Sprint", true);
        }
        else{
            bodyAnim.SetBool("Sprint", false);
        }
    }

    void Reload(){
        if(hasWeapon){
            currentAmmo = maxAmmo;
        }
    }

    void WallRun()
    {
        if (Time.time > timeLastWallJumped + .08f && !isGrounded && sprintMod > 0)  //check if proper wallrunning conditions are met
        {
            Vector3 wallMovement = new Vector3(characterVelocity.x, 0f, characterVelocity.z);
            RaycastHit hit1;
            
            if (Physics.SphereCast(GetCapsuleBottomHemisphere(), .5f, (-charControl.transform.right), out RaycastHit hitInfo1, 0.05f, -1, QueryTriggerInteraction.Ignore))  //check lower right side, mid-air collision
            {
                if (Physics.SphereCast(GetCapsuleBottomHemisphere() + Vector3.up, 0.5f, (-charControl.transform.right), out RaycastHit hitInfo2, 0.05f, -1, QueryTriggerInteraction.Ignore))  //check upper right side, mid-air collision
                {
                    if ((hitInfo1.normal - hitInfo2.normal).magnitude < 0.001f)  //check if surface is smooth enough
                    {
                        hit1 = hitInfo1;

                        if (Vector3.Dot(wallMovement, hit1.normal) > wallRunThreshold &&  //check character trajectory
                           wallMovement.magnitude > 3f)
                        {
                            if (hit1.distance > 0.02f)  //make velocity parallel with wall
                            {
                                charControl.transform.position += -hit1.normal * hit1.distance;
                            }

                            if (!isWallRunning && Vector3.Dot(wallMovement, hit1.normal) <= -2f)  //keep velocity parallel with wall under certain conditions
                            {
                                wallMovement = wallMovement - hit1.normal * Vector3.Dot(wallMovement, hit1.normal);
                                characterVelocity = wallMovement + Vector3.up * 8f;
                                wallNormal = hit1.normal;
                                isWallRunning = true;
                            }

                            Vector3 targetVelocity = move.y * movementSpeed * (sprintMod + 1f) * wallMovement.normalized;
                            characterVelocity = Vector3.Lerp(wallMovement, targetVelocity, groundMoveSharpness * Time.deltaTime) + Vector3.up * characterVelocity.y;
                            characterVelocity += Vector3.up * 15f * Time.deltaTime;
                            hasDoubleJumped = false;
                        }

                        else if (Vector3.Dot(wallMovement, hit1.normal) <= 0f)
                        {
                            characterVelocity = characterVelocity - hit1.normal * Vector3.Dot(characterVelocity, hit1.normal);
                            isWallRunning = false;
                        }

                        else
                        {
                            isWallRunning = false;
                        }
                    }
                }
                else
                    {
                        isWallRunning = false;
                    }
            }

            else if (Physics.SphereCast(GetCapsuleBottomHemisphere(), .5f, (charControl.transform.right), out RaycastHit hitInfo3, 0.05f, -1, QueryTriggerInteraction.Ignore))
            {
                if (Physics.SphereCast(GetCapsuleBottomHemisphere() + Vector3.up, 0.5f, (charControl.transform.right), out RaycastHit hitInfo4, 0.05f, -1, QueryTriggerInteraction.Ignore))
                {
                    if ((hitInfo3.normal - hitInfo4.normal).magnitude < 0.001f)
                    {
                        hit1 = hitInfo3;

                        if (Vector3.Dot(wallMovement, hit1.normal) > wallRunThreshold &&
                           wallMovement.magnitude > 3f)
                        {
                            if (hit1.distance > 0.02f)
                            {
                                charControl.transform.position += -hit1.normal * hit1.distance;
                            }

                            if (!isWallRunning && Vector3.Dot(wallMovement, hit1.normal) <= -2f)
                            {
                                wallMovement = wallMovement - hit1.normal * Vector3.Dot(wallMovement, hit1.normal);
                                characterVelocity = wallMovement + Vector3.up * 8f;
                                wallNormal = hit1.normal;
                                isWallRunning = true;
                            }

                            Vector3 targetVelocity = move.y * movementSpeed * (sprintMod + 1f) * wallMovement.normalized;
                            characterVelocity = Vector3.Lerp(wallMovement, targetVelocity, groundMoveSharpness * Time.deltaTime) + Vector3.up * characterVelocity.y;
                            characterVelocity += Vector3.up * 15f * Time.deltaTime;
                            hasDoubleJumped = false;
                        }

                        else if (Vector3.Dot(wallMovement, hit1.normal) <= 0f)
                        {
                            characterVelocity = characterVelocity - hit1.normal * Vector3.Dot(characterVelocity, hit1.normal);
                            isWallRunning = false;
                        }

                        else
                        {
                            isWallRunning = false;
                        }
                    }
                }
                else
                {
                    isWallRunning = false;
                }
            }
            else
            {
                isWallRunning = false;
            }
        }
        else{
            isWallRunning = false;
        }

    }

    void MegaLaunch()
    {
        if (!isOnline && currentAmmo > 0 && Time.time - timeLastShot > shootPreventionTime)
        {
            isGrounded = false;
            timeLastShot = Time.time;
            characterVelocity += FPCam.transform.forward.normalized * -200f;
            gun.PlayOneShot(shootSound);
            currentAmmo--;
        }
        else if (isOnline)
        {
            if (PV.IsMine)
            {
                if (currentAmmo > 0 && Time.time - timeLastShot > shootPreventionTime)
                {
                    isGrounded = false;
                    timeLastShot = Time.time;
                    characterVelocity += FPCam.transform.forward.normalized * -200f;
                    PV.RPC("NetShoot", RpcTarget.All);
                    currentAmmo--;
                }
            }
        }
    }

    void OpenMenu()
    {
        if (!isOnline && menu != null)
        {
            if (!menuStatus)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                menu.SetActive(true);
                movement.PlayerControls.Disable();
                look = new Vector2(0f, 0f);
                move = new Vector2(0f, 0f);
                gameLook = new Vector2(0f, 0f);
            }

            else if (menuStatus)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                menu.SetActive(false);
                movement.PlayerControls.Enable();
            }
            menuStatus = !menuStatus;
        }

        else if(isOnline && PV.IsMine && menu != null)
        {
            if (!menuStatus)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                menu.SetActive(true);
                movement.PlayerControls.Disable();
                look = new Vector2(0f, 0f);
            }

            else if (menuStatus)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                menu.SetActive(false);
                movement.PlayerControls.Enable();
            }
            menuStatus = !menuStatus;
        }
        


        
        
    }

    void Jump()
    {
        if (!isOnline)
        {
            if(isGrounded && Time.time > timeLastJumped + jumpPreventionTime)
            {
                isGrounded = false;
                timeLastJumped = Time.time;
                characterVelocity += Vector3.up * jumpForce;
            }

            else if (isWallRunning && !isGrounded)
            {
                timeLastWallJumped = Time.time;
                characterVelocity += (Vector3.up + wallNormal) * jumpForce;
                isWallRunning = false;
            }

            else if(!isGrounded && !hasDoubleJumped)
            {
                Vector3 horizontalInput = (charControl.transform.right * move.x + charControl.transform.forward * move.y).normalized;
                Vector3 horizontalVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);
                characterVelocity = horizontalVelocity.magnitude * horizontalInput;
                characterVelocity += Vector3.up * jumpForce;
                hasDoubleJumped = true;
                isWallRunning = false;
            }
        }
        else if(isOnline)
        {
            if (PV.IsMine)
            {
                if(isGrounded && Time.time - timeLastJumped > jumpPreventionTime)
                {
                isGrounded = false;
                timeLastJumped = Time.time;
                characterVelocity += Vector3.up * jumpForce;
                }

                else if (isWallRunning && !isGrounded)
                {
                    timeLastWallJumped = Time.time;
                    characterVelocity += (Vector3.up + wallNormal) * jumpForce;
                    isWallRunning = false;
                }

                else if(!isGrounded && !hasDoubleJumped)
                {
                    Vector3 horizontalInput = (charControl.transform.right * move.x + charControl.transform.forward * move.y).normalized;
                    Vector3 horizontalVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);
                    characterVelocity = horizontalVelocity.magnitude * horizontalInput;
                    characterVelocity += Vector3.up * jumpForce;
                    hasDoubleJumped = true;
                    isWallRunning = false;
                }
            }
        }
    }

    public void GroundCheck()
    {
        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        float chosenGroundCheckDistance = isGrounded ? (charControl.skinWidth + groundCheckDistance) : airGroundCheckDistance;

        // reset values before the ground check
        isGrounded = false;
        groundNormal = Vector3.up;

        // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
        if (Time.time > timeLastJumped + jumpPreventionTime)
        {
            // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
            if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(charControl.height), charControl.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, groundCheckLayers, QueryTriggerInteraction.Ignore))
            {
                // storing the upward direction for the surface found
                groundNormal = hit.normal;

                // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                // and if the slope angle is lower than the character controller's limit
                if (Vector3.Dot(hit.normal, transform.up) >= 0f &&
                    IsNormalUnderSlopeLimit(groundNormal))
                {
                    isGrounded = true;
                    hasDoubleJumped = false;

                    // handle snapping to the ground
                    if (hit.distance > charControl.skinWidth)
                    {
                        charControl.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }
    }

    void GetMove()
    {
        float speedMultiplier = sprintMod + 1f;
        float customMultiplier = 0f;
        if (gameLook.magnitude < 0.05f)
        {
            customMultiplier = mouseSensMultiply * 0.1f;
            transform.Rotate(new Vector3(0f, look.x * customMultiplier, 0f), Space.Self);
            cameraVertical += look.y * customMultiplier;
        }

        else if (look.magnitude == 0)
        {
            customMultiplier = gamepadSensMultiply;
            transform.Rotate(new Vector3(0f, gameLook.x * customMultiplier, 0f), Space.Self);
            cameraVertical += gameLook.y * customMultiplier;
        }

        cameraVertical = Mathf.Clamp(cameraVertical, -89f, 89f);
        FPCam.transform.localEulerAngles = new Vector3(-cameraVertical, 0, 0);

        if (isGrounded && !isWallRunning)
        {
            bodyAnim.SetFloat("Walk", move.y);
            Vector3 worldSpaceMove = transform.TransformVector(move.x, 0f, move.y);
            Vector3 targetVelocity = worldSpaceMove * movementSpeed * speedMultiplier;
            targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, groundNormal) * targetVelocity.magnitude;
            characterVelocity = Vector3.Lerp(characterVelocity, targetVelocity, groundMoveSharpness * Time.deltaTime);
        }

        else
        {
            WallRun();
            characterVelocity += Vector3.down * gravityForce * Time.deltaTime;
            if (!isWallRunning)
            {
                //Vector3 horizontalVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);
                //Vector3 horizontalTarget = charControl.transform.right * move.x;
                //characterVelocity = (horizontalVelocity + horizontalTarget * strafeRatio * Time.deltaTime).normalized * horizontalVelocity.magnitude + charControl.transform.up * characterVelocity.y;
            }
        }

        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(charControl.height);
        charControl.Move(characterVelocity * Time.deltaTime);


    }

    void Shoot()
    {
        if (!isOnline && currentAmmo > 0 && Time.time - timeLastShot > shootPreventionTime)
        {
            timeLastShot = Time.time;
            currentAmmo--;

        }
        else if(isOnline)
        {
            if (PV.IsMine)
            {
                if(currentAmmo > 0 && Time.time - timeLastShot > shootPreventionTime)
                {
                    timeLastShot = Time.time;
                    currentAmmo--;

                }
            }
        }
        
    }

    void UpdateSensitivity()
    {
        mouseSensMultiply = PlayerPrefs.GetFloat("mouseSens", 1f);
        gamepadSensMultiply = PlayerPrefs.GetFloat("gamepadSens", 1f);
    }

    public void SetTookTurn()
    {
        movement.PlayerControls.Move.Disable();
        move = new Vector2(0f, 0f);
        movement.PlayerControls.Jump.Disable();
    }

    public void OnRespawn()
    {
        movement.PlayerControls.Move.Enable();
        movement.PlayerControls.Jump.Enable();
        characterVelocity = new Vector3(0f, 0f, 0f);
    }

    [PunRPC]
    void NetShoot()
    {

    }

    void SwitchCamera()
    {
        if (!isOnline)
        {
            if (FPCam.GetComponent<Camera>().enabled == true)
            {
                FPCam.GetComponent<Camera>().enabled = false;
                TPCam.GetComponent<Camera>().enabled = true;
                currentCam = TPCam;
            }
            else
            {
                TPCam.GetComponent<Camera>().enabled = false;
                FPCam.GetComponent<Camera>().enabled = true;
                currentCam = FPCam;
            }
        }
        else if(isOnline)
        {
            if (PV.IsMine)
            {
                if (FPCam.GetComponent<Camera>().enabled == true)
                {
                    FPCam.GetComponent<Camera>().enabled = false;
                    TPCam.GetComponent<Camera>().enabled = true;
                    currentCam = FPCam;
                }
                else
                {
                    TPCam.GetComponent<Camera>().enabled = false;
                    FPCam.GetComponent<Camera>().enabled = true;
                    currentCam = FPCam;
                }

            }
        }
        
    }

    void OnEnable()
    {
        movement.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        movement.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void DoInteract()
    {
        
        if (Physics.SphereCast(FPCam.transform.position, 0.25f, FPCam.transform.forward, out RaycastHit hit, 5f))
        {
            Interactable newObject = hit.collider.GetComponent<Interactable>();
            if(newObject != null)
            {
                newObject.Interact(this.gameObject);

            }
        }
    }

    bool IsNormalUnderSlopeLimit(Vector3 normal)
    {
        return Vector3.Angle(transform.up, normal) <= charControl.slopeLimit;
    }

    // Gets the center point of the bottom hemisphere of the character controller capsule    
    Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * charControl.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule    
    Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        return transform.position + (transform.up * (atHeight - charControl.radius));
    }

    // Gets a reoriented direction that is tangent to a given slope
    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
    {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    [PunRPC]
    public void StartGame()
    {
        movement.Enable();
        menu.SetActive(false);
        menuStatus = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

}
