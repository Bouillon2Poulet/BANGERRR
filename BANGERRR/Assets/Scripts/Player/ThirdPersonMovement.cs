using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    [Header("Imports")]
    public Transform orientation;
    public Transform playerGraphics;
    public Transform cam;
    private Rigidbody rb;
    private GravityBody gravityBody;

    [Header("Player Variables")]
    public float rotationSpeed; /// default: 7
    public float moveSpeed; /// default: 7

    [Header("Drag")]
    public float playerHeight; /// mine: 1.25
    public LayerMask groundMask;
    public float raycastMargin = 0.2f; /// default: .2
    bool grounded;
    public float groundDrag; /// default: 5

    [Header("Jump")]
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpForce; /// default: 12
    public float jumpCooldown; /// default: .25
    public float airMultiplier; /// default 0.4
    bool readyToJump;

    /// other variables
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;

    [Header("Debug")]
    //public Transform gravityAreaTransform; // this debug feature allows me to retrieve the current gravityArea the player is in
    private Vector3 GAPreviousPosition;
    private bool GAFirstEntering;
    private int GAPreviousID;

    void Start()
    {
        LockAndHideCursor();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; /// Mandatory so that the player doesn't fall over
        gravityBody = GetComponent<GravityBody>();
        ResetJump();

        GAPreviousPosition = Vector3.zero;
        GAFirstEntering = true;
        GAPreviousID = -1;
    }

    void Update()
    {
        /// GROUND CHECK
        /// Checks what's under the player using a raycast to see if the player is on the ground.
        grounded = Physics.Raycast(transform.position, gravityBody.GravityDirection, playerHeight * 0.5f + raycastMargin, groundMask);
        //Debug.Log("grounded: " + grounded); // To know when player is grounded

        /// ORIENTATION
        /// Gets the inputs of the joystick or keys, horizontally and vertically separately, and put them in floats.
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        /// Calculates the direction in which the player if facing
        Vector3 gravityDirection = gravityBody.GravityDirection;
        Vector3 forwardDirection = cam.forward;
        Quaternion viewDirection = Quaternion.LookRotation(Vector3.ProjectOnPlane(forwardDirection, gravityDirection), -gravityDirection);


        /// Determines the rotation of the child object (called "orientation") of the player responsible for the orientation
        orientation.rotation = viewDirection;

        /// Determines the movement direction according to inputs and the orientation calculated above
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        /// If there are inputs, we change the rotation of the player's graphics
        if (moveDirection != Vector3.zero)
        {
            Quaternion correctRotation = Quaternion.LookRotation(moveDirection, -gravityDirection);
            playerGraphics.rotation = Quaternion.Lerp(playerGraphics.rotation, correctRotation, rotationSpeed * Time.deltaTime);
        }

        /// DRAG
        /// Drag is applied on the rigidbody of the player only if they're an the ground
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        /// SPEED CAP
        /// This function limits the velocity of the player
        SpeedControl();

        /// JUMP
        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    void FixedUpdate()
    {
        FollowGravityArea();
        MovePlayer();
    }

    private void FollowGravityArea()
    {
        if (gravityBody.GravityTransform != null)
        {
            int currentID = gravityBody.GravityTransform.GetInstanceID();

            if (currentID != GAPreviousID || GAFirstEntering)
            {
                GAPreviousID = currentID;
                GAPreviousPosition = gravityBody.GravityTransform.position;
                GAFirstEntering = false;
            }
            else
            {
                Vector3 GAPositionDelta = GAPreviousPosition - gravityBody.GravityTransform.position;
                rb.MovePosition(rb.position - GAPositionDelta);
                GAPreviousPosition = gravityBody.GravityTransform.position;
            }
        }
        else
        {
            GAFirstEntering = true;
        }
    }

    private void MovePlayer()
    {
        /// Project the moveDirection vector onto the plane defined by the current gravity
        Vector3 moveDirectionOnGravityPlane = Vector3.ProjectOnPlane(moveDirection, gravityBody.GravityDirection).normalized;

        /// Rotate the rigidBody as wanted
        Quaternion rightDirection = Quaternion.Euler(0f, moveDirectionOnGravityPlane.x * (rotationSpeed * Time.fixedDeltaTime), 0f);
        Quaternion newRotation = Quaternion.Slerp(rb.rotation, rb.rotation * rightDirection, Time.fixedDeltaTime * 3f);
        rb.MoveRotation(newRotation);

        /// Apply the movement forces to the player's rigidbody
        if (grounded)
        {
            rb.AddForce(moveDirectionOnGravityPlane * moveSpeed * 10f, ForceMode.Force);
        }
        else if (!grounded)
        {
            rb.AddForce(moveDirectionOnGravityPlane * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        /// Calculation of the lateral velocity of the player on the plan defined by the gravity
        Vector3 velocityOnHorizontalPlane = rb.velocity - Vector3.Dot(rb.velocity, gravityBody.GravityDirection) * gravityBody.GravityDirection;

        /// If the velocity is too big, we clamp it
        if (velocityOnHorizontalPlane.magnitude > moveSpeed)
        {
            Vector3 clampedHorizontalVelocity = Vector3.ClampMagnitude(velocityOnHorizontalPlane, moveSpeed);
            rb.velocity = clampedHorizontalVelocity + Vector3.Dot(rb.velocity, gravityBody.GravityDirection) * gravityBody.GravityDirection;
        }
    }

    private void Jump()
    {
        /// Giving only the lateral velocity to the rb to cancel up and down velocity
        Vector3 velocityOnGravityPlane = Vector3.ProjectOnPlane(rb.velocity, gravityBody.GravityDirection);
        rb.velocity = velocityOnGravityPlane;

        rb.AddForce(-gravityBody.GravityDirection * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void LockAndHideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
