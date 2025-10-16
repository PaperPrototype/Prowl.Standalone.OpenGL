// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Diagnostics.CodeAnalysis;

using Echo.Logging;

using Prowl.PaperUI;
using Prowl.Runtime.GUI;
using Prowl.Vector;

namespace Prowl.Runtime;

public class EchoLogger : IEchoLogger
{
    public void Debug(string message) => Prowl.Runtime.Debug.Log(message);

    public void Error(string message, Exception? exception = null) => Prowl.Runtime.Debug.LogError(message);

    public void Info(string message) => Prowl.Runtime.Debug.Log(message);

    public void Warning(string message) => Prowl.Runtime.Debug.LogWarning(message);
}

public abstract class Game
{
    private TimeData time = new();
    private double fixedTimeAccumulator = 0.0;

    private PaperRenderer _paperRenderer;
    private Paper _paper;

    public Paper PaperInstance => _paper;

    public void Run(string title, int width, int height)
    {

        Window.InitWindow(title, width, height, Silk.NET.Windowing.WindowState.Normal, false);

        Window.Load += () =>
        {
            Graphics.Initialize();

            _paperRenderer = new PaperRenderer();
            _paperRenderer.Initialize(width, height);
            _paper = new Paper(_paperRenderer, width, height, new Prowl.Quill.FontAtlasSettings());
            //Paper.Initialize(_paperRenderer, width, height);

            Initialize();

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        };

        Window.Update += (delta) =>
        {
            UpdatePaperInput();

            time.Update();
            Time.TimeStack.Clear();
            Time.TimeStack.Push(time);

            Input.UpdateActions(delta);

            // Fixed update loop
            fixedTimeAccumulator += delta;
            int count = 0;
            while (fixedTimeAccumulator >= Time.fixedDeltaTime && count++ < 10)
            {
                FixedUpdate();

                fixedTimeAccumulator -= Time.fixedDeltaTime;
            }

            Update();

            Console.Title = $"{title} - {Window.InternalWindow.FramebufferSize.X}x{Window.InternalWindow.FramebufferSize.Y} - FPS: {1.0 / Time.deltaTime}";
        };

        Window.Render += (delta) =>
        {

            Graphics.StartFrame();

            Render();

            PostRender();

            Graphics.Device.UnbindFramebuffer();
            Graphics.Device.Viewport(0, 0, (uint)Window.InternalWindow.FramebufferSize.X, (uint)Window.InternalWindow.FramebufferSize.Y);

            _paper.BeginFrame(delta);

            OnGUI(_paper);

            PostGUI(_paper);

            _paper.EndFrame();

            Graphics.EndFrame();

            Debug.ClearGizmos();
        };

        Window.Resize += (size) =>
        {
            _paper.SetResolution(size.X, size.Y);
            _paperRenderer.UpdateProjection(size.X, size.Y);
            Resize(size.X, size.Y);
        };

        Window.Closing += () =>
        {
            Closing();

            Graphics.Dispose();

            Debug.Log("Is terminating...");
        };

        Debug.LogSuccess("Initialization complete");
        Window.Start();

    }

    public virtual void Initialize() { }
    public virtual void FixedUpdate() { }
    public virtual void Update() { }
    public virtual void PostUpdate() { }
    public virtual void Render() { }
    public virtual void PostRender() { }
    public virtual void OnGUI(Paper paper) { }
    public virtual void PostGUI(Paper paper) { }
    public virtual void Resize(int width, int height) { }
    public virtual void Closing() { }

    [RequiresDynamicCode("Calls System.Enum.GetValues(Type)")]
    private void UpdatePaperInput()
    {
        // Handle mouse position and movement
        Int2 mousePos = Input.MousePosition;
        _paper.SetPointerState(PaperMouseBtn.Unknown, mousePos.X, mousePos.Y, false, true);

        // Handle mouse buttons
        if (Input.GetMouseButtonDown(0))
            _paper.SetPointerState(PaperMouseBtn.Left, mousePos.X, mousePos.Y, true, false);
        if (Input.GetMouseButtonUp(0))
            _paper.SetPointerState(PaperMouseBtn.Left, mousePos.X, mousePos.Y, false, false);

        if (Input.GetMouseButtonDown(1))
            _paper.SetPointerState(PaperMouseBtn.Right, mousePos.X, mousePos.Y, true, false);
        if (Input.GetMouseButtonUp(1))
            _paper.SetPointerState(PaperMouseBtn.Right, mousePos.X, mousePos.Y, false, false);

        if (Input.GetMouseButtonDown(2))
            _paper.SetPointerState(PaperMouseBtn.Middle, mousePos.X, mousePos.Y, true, false);
        if (Input.GetMouseButtonUp(2))
            _paper.SetPointerState(PaperMouseBtn.Middle, mousePos.X, mousePos.Y, false, false);

        // Handle mouse wheel
        double wheelDelta = Input.MouseWheelDelta;
        if (wheelDelta != 0)
            _paper.SetPointerWheel(wheelDelta);

        // Handle keyboard input
        char? c = Input.GetPressedChar();
        while (c != null)
        {
            _paper.AddInputCharacter((c.Value).ToString());
            c = Input.GetPressedChar();
        }

        // Handle key states for keys
        // Fortunately Papers key enums have almost all the same names
        // So we only need to map a few keys manually, the rest we can use reflection
        foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
            if (k != KeyCode.Unknown)
                if (Enum.TryParse(k.ToString(), out PaperKey paperKey))
                    HandleKey(k, paperKey);

        // Handle the few keys that are not the same
        HandleKey(KeyCode.Equal, PaperKey.Equals);
        HandleKey(KeyCode.BackSlash, PaperKey.Backslash);
        HandleKey(KeyCode.GraveAccent, PaperKey.Grave);
        HandleKey(KeyCode.KeypadEqual, PaperKey.KeypadEquals);

        HandleKey(KeyCode.Number0, PaperKey.Num0);
        HandleKey(KeyCode.Number1, PaperKey.Num1);
        HandleKey(KeyCode.Number2, PaperKey.Num2);
        HandleKey(KeyCode.Number3, PaperKey.Num3);
        HandleKey(KeyCode.Number4, PaperKey.Num4);
        HandleKey(KeyCode.Number5, PaperKey.Num5);
        HandleKey(KeyCode.Number6, PaperKey.Num6);
        HandleKey(KeyCode.Number7, PaperKey.Num7);
        HandleKey(KeyCode.Number8, PaperKey.Num8);
        HandleKey(KeyCode.Number9, PaperKey.Num9);

        HandleKey(KeyCode.KeypadSubtract, PaperKey.KeypadMinus);
        HandleKey(KeyCode.KeypadAdd, PaperKey.KeypadPlus);

        HandleKey(KeyCode.LeftBracket, PaperKey.LeftBracket);
        HandleKey(KeyCode.RightBracket, PaperKey.RightBracket);
        HandleKey(KeyCode.ShiftLeft, PaperKey.LeftShift);
        HandleKey(KeyCode.ShiftRight, PaperKey.RightShift);
        HandleKey(KeyCode.AltLeft, PaperKey.LeftAlt);
        HandleKey(KeyCode.AltRight, PaperKey.RightAlt);
        HandleKey(KeyCode.ControlLeft, PaperKey.LeftControl);
        HandleKey(KeyCode.ControlRight, PaperKey.RightControl);
        HandleKey(KeyCode.SuperLeft, PaperKey.LeftSuper);
        HandleKey(KeyCode.SuperRight, PaperKey.RightSuper);
    }

    void HandleKey(KeyCode silkKey, PaperKey paperKey)
    {
        if (Input.GetKeyDown(silkKey))
            _paper.SetKeyState(paperKey, true);
        else if (Input.GetKeyUp(silkKey))
            _paper.SetKeyState(paperKey, false);
    }

    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Log the exception, display it, etc
        Console.WriteLine((e.ExceptionObject as Exception).Message);
    }

    public static void Quit()
    {
        Window.Stop();
        Debug.Log("Is terminating...");
    }
}
