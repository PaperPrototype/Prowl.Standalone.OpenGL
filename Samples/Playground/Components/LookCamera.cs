using Prowl.Runtime;
using Prowl.Vector;

[RequireComponent(typeof(Camera))]
public class LookCamera : MonoBehaviour
{
    private const double METERS_PER_SECOND = 10;

    private bool _cursorVisible = true;

    public bool CursorVisible
    {
        get
        {
            return _cursorVisible;
        }
        set
        {
            _cursorVisible = value;
            Input.SetCursorVisible(_cursorVisible);
        }
    }

    // Input Actions
    private InputActionMap inputMap = null!;
    private InputAction moveAction = null!;
    private InputAction lookAction = null!;
    private InputAction lookEnableAction = null!;
    private InputAction flyUpAction = null!;
    private InputAction flyDownAction = null!;
    private InputAction sprintAction = null!;

    public override void Start()
    {
        inputMap = new InputActionMap("Playground Game");

        // Movement (WASD + Gamepad)
        moveAction = inputMap.AddAction("Move", InputActionType.Value);
        moveAction.ExpectedValueType = typeof(Double2);

        // WASD
        moveAction.AddBinding(new Vector2CompositeBinding(
            InputBinding.CreateKeyBinding(KeyCode.S),
            InputBinding.CreateKeyBinding(KeyCode.W),
            InputBinding.CreateKeyBinding(KeyCode.A),
            InputBinding.CreateKeyBinding(KeyCode.D),
            true
        ));

        // JOYSTICK
        var leftStick = InputBinding.CreateGamepadAxisBinding(0, deviceIndex: 0);
        leftStick.Processors.Add(new DeadzoneProcessor(0.15f));
        leftStick.Processors.Add(new NormalizeProcessor());
        moveAction.AddBinding(leftStick);

        Input.RegisterActionMap(inputMap);
        inputMap.Enable();
    }

    public override void Update()
    {
        Double2 axis = moveAction.ReadValue<Double2>();

        // Camera movement
        Double3 movement = Double3.Zero;
        // if (Input.GetKey(KeyCode.W)) movement += Transform.Forward * Time.DeltaTime * METERS_PER_SECOND;
        // if (Input.GetKey(KeyCode.S)) movement -= Transform.Forward * Time.DeltaTime * METERS_PER_SECOND;
        // if (Input.GetKey(KeyCode.D)) movement += Transform.Right * Time.DeltaTime * METERS_PER_SECOND;
        // if (Input.GetKey(KeyCode.A)) movement -= Transform.Right * Time.DeltaTime * METERS_PER_SECOND;
        movement += Transform.Forward * -axis.Y * Time.DeltaTime * METERS_PER_SECOND;
        movement += Transform.Right * axis.X * Time.DeltaTime * METERS_PER_SECOND;
        if (Input.GetKey(KeyCode.E)) movement += Transform.Up * Time.DeltaTime * METERS_PER_SECOND;
        if (Input.GetKey(KeyCode.Q)) movement -= Transform.Up * Time.DeltaTime * METERS_PER_SECOND;

        // Apply movement
        Transform.Position += movement;

        // Mouse look
        if (!_cursorVisible)
            Transform.LocalEulerAngles += new Double3(Input.MouseDelta.Y, Input.MouseDelta.X, 0) * Time.DeltaTime * METERS_PER_SECOND;

        // Enable or disable the mouse
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _cursorVisible = !_cursorVisible;
            Input.SetCursorVisible(_cursorVisible);
        }
    }
}