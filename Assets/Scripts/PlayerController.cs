using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NavKeypad;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintMultiplier = 2.0f;

    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce = 5.0f;
    [SerializeField] private float gravityMultiplier = 1.0f;

    [Header("Look Parameters")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownLookRange = 80f;

    [Header("Interaction Parameters")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer = ~0;
    [SerializeField] private float keypadStandDistance = 1.2f;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInputHandler playerInputHandler;

    private Vector3 currentMovement;
    private float verticalRotation;
    private float CurrentSpeed => walkSpeed * (playerInputHandler.SprintTriggered ? sprintMultiplier : 1);
    private bool wasInteractTriggered;
    private bool isUsingKeypad;
    private Keypad activeKeypad;
    private Vector3 savedPlayerPosition;
    private Quaternion savedPlayerRotation;
    private float savedVerticalRotation;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isUsingKeypad)
        {
            HandleKeypadMode();
            return;
        }

        HandleMovement();
        HandleRotation();
        HandleInteraction();
    }

    private Vector3 CalculateWorldDirection()
    {
        Vector3 inputDirection = new Vector3(playerInputHandler.MovementInput.x, 0f, playerInputHandler.MovementInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        return worldDirection.normalized;
    }

    private void HandleJumping()
    {
        if (characterController.isGrounded)
        {
            currentMovement.y = -0.5f;

            if (playerInputHandler.JumpTriggered)
            {
                currentMovement.y = jumpForce;
            }
        }
        else
        {
            currentMovement.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovement.x = worldDirection.x * CurrentSpeed;
        currentMovement.z = worldDirection.z * CurrentSpeed;

        HandleJumping();
        characterController.Move(currentMovement * Time.deltaTime);
    }

    private void ApplyHorizontalRotation(float rotationAmount)
    {
        transform.Rotate(0, rotationAmount, 0);
    }

    private void ApplyVerticalRotation(float rotationAmount)
    {
        verticalRotation = Mathf.Clamp(verticalRotation - rotationAmount, -upDownLookRange, upDownLookRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void HandleRotation()
    {
        float mouseXRotation = playerInputHandler.RotationInput.x * mouseSensitivity;
        float mouseYRotation = playerInputHandler.RotationInput.y * mouseSensitivity;

        ApplyHorizontalRotation(mouseXRotation);
        ApplyVerticalRotation(mouseYRotation);
    }

    private void HandleInteraction()
    {
        bool isInteractTriggered = playerInputHandler.InteractTriggered;

        // Ensure we only interact once per button press (like GetKeyDown)
        if (isInteractTriggered && !wasInteractTriggered)
        {
            if (mainCamera != null)
            {
                RaycastHit hit;
                if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, interactableLayer))
                {
                    if (hit.transform.CompareTag("door"))
                    {
                        Door door = hit.transform.GetComponentInParent<Door>();
                        if (door != null)
                        {
                            door.ActionDoor();
                        }
                    }
                    else if (hit.transform.CompareTag("keypad") || hit.transform.GetComponentInParent<Keypad>() != null)
                    {
                        Keypad keypad = hit.transform.GetComponentInParent<Keypad>();
                        if (keypad != null && !keypad.IsAccessGranted)
                        {
                            EnterKeypadMode(keypad);
                        }
                    }
                    else
                    {
                        hit.transform.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
        }

        wasInteractTriggered = isInteractTriggered;
    }

    private void EnterKeypadMode(Keypad keypad)
    {
        isUsingKeypad = true;
        activeKeypad = keypad;

        // Save player state before teleporting
        savedPlayerPosition = transform.position;
        savedPlayerRotation = transform.rotation;
        savedVerticalRotation = verticalRotation;

        // Subscribe to auto-exit on access granted
        activeKeypad.OnKeypadAccessGranted += OnKeypadSuccess;

        // Teleport player in front of the keypad
        TeleportToKeypad(keypad);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ExitKeypadMode()
    {
        if (activeKeypad != null)
        {
            activeKeypad.OnKeypadAccessGranted -= OnKeypadSuccess;
            activeKeypad = null;
        }

        // Restore player to original position and look direction
        characterController.enabled = false;
        transform.position = savedPlayerPosition;
        transform.rotation = savedPlayerRotation;
        characterController.enabled = true;

        verticalRotation = savedVerticalRotation;
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);

        isUsingKeypad = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnKeypadSuccess()
    {
        // Delay exit slightly so the player can see the "Granted" message
        StartCoroutine(DelayedExitKeypad(1.5f));
    }

    private IEnumerator DelayedExitKeypad(float delay)
    {
        yield return new WaitForSeconds(delay);
        ExitKeypadMode();
    }

    private void TeleportToKeypad(Keypad keypad)
    {
        Transform keypadTransform = keypad.transform;

        // The keypad's -forward typically faces outward (toward where the player should stand)
        // Try both directions and pick the one that makes sense based on the keypad's orientation
        Vector3 keypadOutward = -keypadTransform.forward;

        // Position the player in front of the keypad face
        Vector3 targetPosition = keypadTransform.position + keypadOutward * keypadStandDistance;

        // Keep the player at their current Y height (ground level)
        targetPosition.y = transform.position.y;

        // Disable CharacterController to teleport (it blocks transform.position changes)
        characterController.enabled = false;
        transform.position = targetPosition;
        characterController.enabled = true;

        // Face toward the keypad (opposite of outward direction)
        Vector3 lookDirection = -keypadOutward;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized);
        }

        // Tilt camera to look at the keypad center
        Vector3 toKeypad = keypadTransform.position - mainCamera.transform.position;
        float vertAngle = Mathf.Atan2(toKeypad.y, new Vector2(toKeypad.x, toKeypad.z).magnitude) * Mathf.Rad2Deg;
        verticalRotation = Mathf.Clamp(-vertAngle, -upDownLookRange, upDownLookRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void HandleKeypadMode()
    {
        bool isInteractTriggered = playerInputHandler.InteractTriggered;

        // Exit keypad mode when pressing E again or Escape
        if ((isInteractTriggered && !wasInteractTriggered) || UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitKeypadMode();
        }

        wasInteractTriggered = isInteractTriggered;
    }
}
