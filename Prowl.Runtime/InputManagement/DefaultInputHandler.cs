// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;

using Prowl.Vector;

using Silk.NET.Input;

namespace Prowl.Runtime;

public class DefaultInputHandler : IInputHandler, IDisposable
{
    public IInputContext Context { get; internal set; }

    public IReadOnlyList<IKeyboard> Keyboards => Context.Keyboards;
    public IReadOnlyList<IMouse> Mice => Context.Mice;
    public IReadOnlyList<IJoystick> Joysticks => Context.Joysticks;

    public string Clipboard
    {
        get => Context.Keyboards[0].ClipboardText;
        set
        {
            Context.Keyboards[0].ClipboardText = value;
        }
    }


    private Int2 _currentMousePos;
    private Int2 _prevMousePos;

    public Int2 PrevMousePosition => _prevMousePos;
    public Int2 MousePosition
    {
        get => _currentMousePos;
        set
        {
            _prevMousePos = value;
            _currentMousePos = value;
            Mice[0].Position = (Float2)value;
        }
    }
    public Double2 MouseDelta
    {
        get
        {
            var delta = _currentMousePos - _prevMousePos;
            return new Double2(delta.X, delta.Y); // Invert Y to match gamepad (up = positive)
        }
    }
    public double MouseWheelDelta => Mice[0].ScrollWheels[0].Y;

    private Dictionary<KeyCode, bool> wasKeyPressed = new Dictionary<KeyCode, bool>();
    private Dictionary<KeyCode, bool> isKeyPressed = new Dictionary<KeyCode, bool>();
    private Dictionary<MouseButton, bool> wasMousePressed = new Dictionary<MouseButton, bool>();
    private Dictionary<MouseButton, bool> isMousePressed = new Dictionary<MouseButton, bool>();

    // Gamepad state tracking (per device)
    private Dictionary<int, Dictionary<GamepadButton, bool>> wasGamepadButtonPressed = new();
    private Dictionary<int, Dictionary<GamepadButton, bool>> isGamepadButtonPressed = new();

    private Queue<char> pressedChars { get; set; } = new();

    public event Action<KeyCode, bool> OnKeyEvent;
    public event Action<MouseButton, double, double, bool, bool> OnMouseEvent;

    public bool IsAnyKeyDown => isKeyPressed.ContainsValue(true);

    public DefaultInputHandler(IInputContext context)
    {
        Context = context;
        _prevMousePos = (Int2)(Float2)Mice[0].Position;
        _currentMousePos = (Int2)(Float2)Mice[0].Position;

        // initialize key states
        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (key != KeyCode.Unknown)
            {
                wasKeyPressed[key] = false;
                isKeyPressed[key] = false;
            }
        }

        foreach (MouseButton button in Enum.GetValues(typeof(MouseButton)))
        {
            if (button != MouseButton.Unknown)
            {
                wasMousePressed[button] = false;
                isMousePressed[button] = false;
            }
        }

        // Initialize gamepad state for all connected gamepads
        for (int i = 0; i < Context.Gamepads.Count; i++)
        {
            InitializeGamepadState(i);
        }

        foreach (var keyboard in Keyboards)
            keyboard.KeyChar += (keyboard, c) => pressedChars.Enqueue(c);

        UpdateKeyStates();
    }

    private void InitializeGamepadState(int gamepadIndex)
    {
        wasGamepadButtonPressed[gamepadIndex] = new Dictionary<GamepadButton, bool>();
        isGamepadButtonPressed[gamepadIndex] = new Dictionary<GamepadButton, bool>();

        foreach (GamepadButton button in Enum.GetValues(typeof(GamepadButton)))
        {
            if (button != GamepadButton.Unknown)
            {
                wasGamepadButtonPressed[gamepadIndex][button] = false;
                isGamepadButtonPressed[gamepadIndex][button] = false;
            }
        }
    }

    internal void LateUpdate()
    {
        _prevMousePos = _currentMousePos;
        _currentMousePos = (Int2)(Float2)Mice[0].Position;
        if (!_prevMousePos.Equals(_currentMousePos))
        {
            if (isMousePressed[MouseButton.Left])
                OnMouseEvent?.Invoke(MouseButton.Left, MousePosition.X, MousePosition.Y, false, true);
            else if (isMousePressed[MouseButton.Right])
                OnMouseEvent?.Invoke(MouseButton.Right, MousePosition.X, MousePosition.Y, false, true);
            else if (isMousePressed[MouseButton.Middle])
                OnMouseEvent?.Invoke(MouseButton.Middle, MousePosition.X, MousePosition.Y, false, true);
            else
                OnMouseEvent?.Invoke(MouseButton.Unknown, MousePosition.X, MousePosition.Y, false, true);
        }
        UpdateKeyStates();
    }

    // Update the state of each key
    private void UpdateKeyStates()
    {
        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (key != KeyCode.Unknown)
            {
                wasKeyPressed[key] = isKeyPressed[key];
                isKeyPressed[key] = false;
                foreach (var keyboard in Keyboards)
                    if (keyboard.IsKeyPressed((Silk.NET.Input.Key)key))
                    {
                        isKeyPressed[key] = true;
                        break;
                    }

                if (wasKeyPressed[key] != isKeyPressed[key])
                    OnKeyEvent?.Invoke(key, isKeyPressed[key]);
            }
        }

        foreach (MouseButton button in Enum.GetValues(typeof(MouseButton)))
        {
            if (button != MouseButton.Unknown)
            {
                wasMousePressed[button] = isMousePressed[button];
                isMousePressed[button] = false;
                foreach (var mouse in Mice)
                    if (mouse.IsButtonPressed((Silk.NET.Input.MouseButton)button))
                    {
                        isMousePressed[button] = true;
                        break;
                    }
                if (wasMousePressed[button] != isMousePressed[button])
                    OnMouseEvent?.Invoke(button, MousePosition.X, MousePosition.Y, isMousePressed[button], false);
            }
        }

        // Update gamepad button states
        for (int gamepadIndex = 0; gamepadIndex < Context.Gamepads.Count; gamepadIndex++)
        {
            if (!Context.Gamepads[gamepadIndex].IsConnected)
                continue;

            // Initialize if needed
            if (!isGamepadButtonPressed.ContainsKey(gamepadIndex))
                InitializeGamepadState(gamepadIndex);

            var gamepad = Context.Gamepads[gamepadIndex];
            foreach (GamepadButton button in Enum.GetValues(typeof(GamepadButton)))
            {
                if (button != GamepadButton.Unknown)
                {
                    wasGamepadButtonPressed[gamepadIndex][button] = isGamepadButtonPressed[gamepadIndex][button];
                    isGamepadButtonPressed[gamepadIndex][button] = false;

                    // Check if button is pressed
                    var buttonIndex = (int)button;
                    if (buttonIndex < gamepad.Buttons.Count && gamepad.Buttons[buttonIndex].Pressed)
                    {
                        isGamepadButtonPressed[gamepadIndex][button] = true;
                    }
                }
            }
        }
    }

    public char? GetPressedChar()
    {
        if (pressedChars.TryDequeue(out char c))
            return c;
        return null;
    }

    public bool GetKey(KeyCode key) => isKeyPressed[key];

    public bool GetKeyDown(KeyCode key) => isKeyPressed[key] && !wasKeyPressed[key];

    public bool GetKeyUp(KeyCode key) => !isKeyPressed[key] && wasKeyPressed[key];

    public bool GetMouseButton(int button) => isMousePressed[(MouseButton)button];

    public bool GetMouseButtonDown(int button) => isMousePressed[(MouseButton)button] && !wasMousePressed[(MouseButton)button];

    public bool GetMouseButtonUp(int button) => !isMousePressed[(MouseButton)button] && wasMousePressed[(MouseButton)button];

    public void SetCursorVisible(bool visible, int miceIndex = 0) => Mice[miceIndex].Cursor.CursorMode = visible ? CursorMode.Normal : CursorMode.Disabled;

    // Gamepad methods implementation
    public int GetGamepadCount() => Context.Gamepads.Count;

    public bool IsGamepadConnected(int gamepadIndex)
    {
        return gamepadIndex >= 0 && gamepadIndex < Context.Gamepads.Count && Context.Gamepads[gamepadIndex].IsConnected;
    }

    public bool GetGamepadButton(int gamepadIndex, GamepadButton button)
    {
        if (!IsGamepadConnected(gamepadIndex) || !isGamepadButtonPressed.ContainsKey(gamepadIndex))
            return false;
        return isGamepadButtonPressed[gamepadIndex].GetValueOrDefault(button, false);
    }

    public bool GetGamepadButtonDown(int gamepadIndex, GamepadButton button)
    {
        if (!IsGamepadConnected(gamepadIndex) || !isGamepadButtonPressed.ContainsKey(gamepadIndex))
            return false;
        return isGamepadButtonPressed[gamepadIndex].GetValueOrDefault(button, false) &&
               !wasGamepadButtonPressed[gamepadIndex].GetValueOrDefault(button, false);
    }

    public bool GetGamepadButtonUp(int gamepadIndex, GamepadButton button)
    {
        if (!IsGamepadConnected(gamepadIndex) || !isGamepadButtonPressed.ContainsKey(gamepadIndex))
            return false;
        return !isGamepadButtonPressed[gamepadIndex].GetValueOrDefault(button, false) &&
               wasGamepadButtonPressed[gamepadIndex].GetValueOrDefault(button, false);
    }

    public Double2 GetGamepadAxis(int gamepadIndex, int axisIndex)
    {
        if (!IsGamepadConnected(gamepadIndex))
            return Double2.Zero;

        var gamepad = Context.Gamepads[gamepadIndex];
        if (axisIndex < 0 || axisIndex >= gamepad.Thumbsticks.Count)
            return Double2.Zero;

        var thumbstick = gamepad.Thumbsticks[axisIndex];
        return new Double2(thumbstick.X, thumbstick.Y); // We flip y to make UP on the stick positive
    }

    public double GetGamepadTrigger(int gamepadIndex, int triggerIndex)
    {
        if (!IsGamepadConnected(gamepadIndex))
            return 0f;

        var gamepad = Context.Gamepads[gamepadIndex];
        if (triggerIndex < 0 || triggerIndex >= gamepad.Triggers.Count)
            return 0f;

        return gamepad.Triggers[triggerIndex].Position;
    }

    public void SetGamepadVibration(int gamepadIndex, double leftMotor, double rightMotor)
    {
        if (!IsGamepadConnected(gamepadIndex))
            return;

        var gamepad = Context.Gamepads[gamepadIndex];
        if (gamepad.VibrationMotors.Count >= 2)
        {
            gamepad.VibrationMotors[0].Speed = (float)leftMotor;
            gamepad.VibrationMotors[1].Speed = (float)rightMotor;
        }
    }

    public void Dispose() => Context.Dispose();
}
