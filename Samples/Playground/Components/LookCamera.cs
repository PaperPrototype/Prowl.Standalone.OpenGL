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

    public override void Update()
    {
        // Camera movement
        Double3 movement = Double3.Zero;
        if (Input.GetKey(KeyCode.W)) movement += Transform.Forward * Time.DeltaTime * METERS_PER_SECOND;
        if (Input.GetKey(KeyCode.S)) movement -= Transform.Forward * Time.DeltaTime * METERS_PER_SECOND;
        if (Input.GetKey(KeyCode.D)) movement += Transform.Right * Time.DeltaTime * METERS_PER_SECOND;
        if (Input.GetKey(KeyCode.A)) movement -= Transform.Right * Time.DeltaTime * METERS_PER_SECOND;
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