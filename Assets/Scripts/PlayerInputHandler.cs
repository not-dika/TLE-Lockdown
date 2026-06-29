using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset playerControls;

    [Header("Action Map Name Reference")]
    [SerializeField] private string actionMapName = "Player";

    [Header("Action Name References")]
    [SerializeField] private string movement = "Movement";
    [SerializeField] private string rotation = "Rotation";
    [SerializeField] private string jump = "Jump";
    [SerializeField] private string sprint = "Sprint";
    [SerializeField] private string interact = "Interact";

    [Header("Touchscreen Settings")]
    [SerializeField] private bool forceTouchscreen = false;
    [SerializeField] private Joystick joystick;
    [SerializeField] private float touchSensitivity = 1.0f;

    private InputAction movementAction;
    private InputAction rotationAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction interactAction;

    private Vector2 movementInput;
    private Vector2 rotationInput;

    private int activeLookTouchId = -1;
    private Vector2 touchRotationInput = Vector2.zero;
    private bool lastTouchscreenState;
    private Vector2 lastMousePosition;
    private bool isSimulatingTouch = false;
    private bool joystickForcedHidden = false;

    public Vector2 MovementInput
    {
        get
        {
            if (IsTouchscreenModeActive() && joystick != null)
            {
                return joystick.Direction;
            }
            return movementInput;
        }
    }

    public Vector2 RotationInput
    {
        get
        {
            if (IsTouchscreenModeActive())
            {
                return touchRotationInput;
            }
            return rotationInput;
        }
    }

    public bool JumpTriggered { get; private set; }
    public bool SprintTriggered { get; private set; }
    public bool InteractTriggered { get; private set; }
    public bool RotateObjectTriggered { get; private set; }

    public bool IsTouchscreenModeActive()
    {
        return forceTouchscreen || (Application.isMobilePlatform && !Application.isEditor);
    }

    private void Awake()
    {
        InputActionMap mapReference = playerControls.FindActionMap(actionMapName);

        movementAction = mapReference.FindAction(movement);
        rotationAction = mapReference.FindAction(rotation);
        jumpAction = mapReference.FindAction(jump);
        sprintAction = mapReference.FindAction(sprint);
        interactAction = mapReference.FindAction(interact);

        SubscribeActionValuesToInputEvents();
    }

    public void SetCursorLock(bool locked)
    {
        if (IsTouchscreenModeActive())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }

    private void Start()
    {
        lastTouchscreenState = IsTouchscreenModeActive();
        UpdateJoystickVisibility();
        SetCursorLock(true);
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;
        if (IsTouchscreenModeActive() != lastTouchscreenState)
        {
            lastTouchscreenState = IsTouchscreenModeActive();
            UpdateJoystickVisibility();
            SetCursorLock(true);
        }

        if (IsTouchscreenModeActive())
        {
            HandleTouchLook();
        }
    }

    public void SetJoystickActive(bool active)
    {
        joystickForcedHidden = !active;
        UpdateJoystickVisibility();
    }

    private void UpdateJoystickVisibility()
    {
        if (joystick != null)
        {
            joystick.gameObject.SetActive(IsTouchscreenModeActive() && !joystickForcedHidden);
        }
    }

    private void HandleTouchLook()
    {
        touchRotationInput = Vector2.zero;

        // Simulate touch in Editor or on Standalone platforms (e.g. for testing forceTouchscreen) using New Input System Mouse API
        if (!Application.isMobilePlatform || Application.isEditor)
        {
            if (Mouse.current != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (mousePosition.x > Screen.width / 2f)
                    {
                        isSimulatingTouch = true;
                        lastMousePosition = mousePosition;
                    }
                }
                else if (Mouse.current.leftButton.isPressed)
                {
                    if (isSimulatingTouch)
                    {
                        touchRotationInput = (mousePosition - lastMousePosition) * touchSensitivity;
                        lastMousePosition = mousePosition;
                    }
                }
                else
                {
                    isSimulatingTouch = false;
                }

                if (isSimulatingTouch)
                {
                    return;
                }
            }
        }

        // Try New Input System Touchscreen
        if (Touchscreen.current != null)
        {
            var touches = Touchscreen.current.touches;
            bool foundActiveTouch = false;

            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (!touch.isInProgress) continue;

                int touchId = touch.touchId.ReadValue();
                Vector2 position = touch.position.ReadValue();
                UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                Vector2 delta = touch.delta.ReadValue();

                if (activeLookTouchId == -1)
                {
                    if (phase == UnityEngine.InputSystem.TouchPhase.Began && position.x > Screen.width / 2f)
                    {
                        activeLookTouchId = touchId;
                        foundActiveTouch = true;
                    }
                }
                else if (touchId == activeLookTouchId)
                {
                    if (phase == UnityEngine.InputSystem.TouchPhase.Moved)
                    {
                        touchRotationInput = delta * touchSensitivity;
                    }
                    else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        activeLookTouchId = -1;
                    }
                    foundActiveTouch = true;
                    break;
                }
            }

            if (!foundActiveTouch)
            {
                activeLookTouchId = -1;
            }
        }
    }

    private void SubscribeActionValuesToInputEvents()
    {
        movementAction.performed += inputInfo => movementInput = inputInfo.ReadValue<Vector2>();
        movementAction.canceled += inputInfo => movementInput = Vector2.zero;

        rotationAction.performed += inputInfo => rotationInput = inputInfo.ReadValue<Vector2>();
        rotationAction.canceled += inputInfo => rotationInput = Vector2.zero;

        jumpAction.performed += inputInfo => JumpTriggered = true;
        jumpAction.canceled += inputInfo => JumpTriggered = false;

        sprintAction.performed += inputInfo => SprintTriggered = true;
        sprintAction.canceled += inputInfo => SprintTriggered = false;

        interactAction.performed += inputInfo => InteractTriggered = true;
        interactAction.canceled += inputInfo => InteractTriggered = false;
    }

    private void OnEnable()
    {
        playerControls.FindActionMap(actionMapName).Enable();
    }

    private void OnDisable()
    {
        playerControls.FindActionMap(actionMapName).Disable();
    }
}
