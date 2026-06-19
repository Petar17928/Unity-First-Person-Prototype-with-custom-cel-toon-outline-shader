using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private CollisionFlags collisionFlags;
    private float standCenterY;

    public float gravity = -9.8f;
    public float fallMultiplier = 2.5f;

    public float riseMultiplier = 1.4f;    
    public float apexThreshold = 2.5f;        

    public float jumpHeight = 1.5f;
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float slideForce = 6f;
    public float slideDuration = 0.4f;

    public float standingHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchLerpTime = 0.15f;

    public bool isGrounded { get; private set; }
    public bool isMoving { get; private set; }
    public bool isAiming { get; private set; }

    public float CurrentControllerHeight => controller.height;

    private bool sprinting;
    private bool crouching;
    private bool sliding;
    private float currentSpeed;
    private float slideTimer;
    private Vector3 slideDirection;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        controller = GetComponent<CharacterController>();

        controller.height = standingHeight;
        controller.center = new Vector3(0, standingHeight / 2f, 0);

        Vector3 pos = transform.position;
        pos.y += controller.height / 2f;
        transform.position = pos;

        currentSpeed = walkSpeed;
    }


    void Update()
    {
        isGrounded = controller.isGrounded;
        HandleCrouchLerp();
    }

    public void ProccessMove(Vector2 input)
    {
        isMoving = input.magnitude > 0.1f;

        if (sliding)
        {
            HandleSlide();
        }
        else
        {
            Vector3 move = new Vector3(input.x, 0f, input.y);
            controller.Move(transform.TransformDirection(move) * currentSpeed * Time.deltaTime);
        }

        ApplyGravity();
    }

    private void ApplyGravity()
    {
        if (isGrounded && playerVelocity.y < 0f)
            playerVelocity.y = -2f;

        if (playerVelocity.y > 0f)
        {
            if (playerVelocity.y < apexThreshold)
            {
                playerVelocity.y += gravity * riseMultiplier * Time.deltaTime;
            }
            else
            {
                playerVelocity.y += gravity * Time.deltaTime;
            }
        }
        else
        {
            playerVelocity.y += gravity * fallMultiplier * Time.deltaTime;
        }

        collisionFlags = controller.Move(playerVelocity * Time.deltaTime);

        if ((collisionFlags & CollisionFlags.Above) != 0 && playerVelocity.y > 0f)
        {
            playerVelocity.y = -1f;
        }
    }



    public void Jump()
    {
        if (!isGrounded) return;

        if (crouching)
            ExitCrouch();

        playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    public void Crouch()
    {
        if (sliding) return;

        if (sprinting && isGrounded)
        {
            StartSlide();
            return;
        }

        if (crouching)
        {
            if (HasCeilingAbove())
                return;

            ExitCrouch();
        }
        else
        {
            crouching = true;
            currentSpeed = crouchSpeed;
        }
    }


    public bool IsCrouching()
    {
        return crouching;
    }

    private bool ExitCrouch()
    {
        if (HasCeilingAbove())
            return false;

        crouching = false;
        currentSpeed = walkSpeed;
        return true;
    }

    private void HandleCrouchLerp()
    {
        float targetHeight = crouching ? crouchHeight : standingHeight;
        float previousHeight = controller.height;

        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            Time.deltaTime / crouchLerpTime
        );

        float newCenterY = controller.height / 2;
        controller.center = new Vector3(0, newCenterY, 0);
    }

    public void StartSprint()
    {
        if (!isGrounded || sliding) return;
        bool canSprint = true;
        if (crouching)
            canSprint = ExitCrouch();
        if (canSprint)
        {
            sprinting = true;
            currentSpeed = sprintSpeed;
            StopAim();
        }
        
    }


    public void StopSprint()
    {
        sprinting = false;
        currentSpeed = crouching ? crouchSpeed : walkSpeed;
    }

    public bool IsSprinting()
    {
        return sprinting;
    }
    private void StartSlide()
    {
        sliding = true;
        sprinting = false;
        crouching = true;

        slideTimer = slideDuration;
        currentSpeed = 0f;

        slideDirection = new Vector3(
            controller.velocity.x,
            0f,
            controller.velocity.z
        ).normalized;

        if (slideDirection.magnitude < 0.1f)
        {
            slideDirection = transform.forward;
        }
    }

    private void HandleSlide()
    {
        slideTimer -= Time.deltaTime;

        controller.Move(slideDirection * slideForce * Time.deltaTime);

        if (slideTimer <= 0f)
        {
            sliding = false;
            crouching = true;
            currentSpeed = crouchSpeed;
        }
    }


    private bool HasCeilingAbove()
    {
        float extraHeight = standingHeight - controller.height;
        if (extraHeight <= 0f)
            return false;

        Vector3 origin = transform.position + Vector3.up * controller.height;
        return Physics.Raycast(origin, Vector3.up, extraHeight + 0.05f);
    }

    public void StartAim()
    {
        isAiming = true;

        if (IsSprinting())
            StopSprint();
    }

    public void StopAim()
    {
        isAiming = false;
    }
}