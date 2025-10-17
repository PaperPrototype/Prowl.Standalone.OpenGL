// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

//
// 3D Audio Demo
//
// This demo demonstrates the 3D audio system with:
// - Multiple audio sources positioned in 3D space
// - Doppler effect on moving sources
// - Distance attenuation and reference distance
// - Reverb effects in different zones
// - Audio filters (lowpass, highpass, bandpass)
// - Flying camera to navigate the scene
//
// Controls:
//   Movement:  WASD / Arrow Keys / Gamepad Left Stick
//   Look:      Mouse Move (when RMB held) / Gamepad Right Stick
//   Fly Up:    E / Gamepad A Button
//   Fly Down:  Q / Gamepad B Button
//   Sprint:    Left Shift / Gamepad Left Stick Click
//   Toggle Reverb: R
//   Cycle Filter: F
//

using Prowl.Runtime;
using Prowl.Runtime.Audio;
using Prowl.Runtime.Rendering;
using Prowl.Runtime.Resources;
using Prowl.Vector;

using MouseButton = Prowl.Runtime.MouseButton;

namespace AudioDemo;

internal class Program
{
    static void Main(string[] args)
    {
        new AudioDemoGame().Run("3D Audio Demo", 1280, 720);
    }
}

public sealed class AudioDemoGame : Game
{
    private GameObject? cameraGO;
    private Scene? scene;

    // Input Actions
    private InputActionMap inputMap = null!;
    private InputAction moveAction = null!;
    private InputAction lookAction = null!;
    private InputAction lookEnableAction = null!;
    private InputAction flyUpAction = null!;
    private InputAction flyDownAction = null!;
    private InputAction sprintAction = null!;

    // Audio sources
    private List<AudioSource> audioSources = new();
    private float time = 0;

    // Audio clips
    private AudioClip? ambientClip;
    private AudioClip? engineClip;
    private AudioClip? musicClip;

    public override void Initialize()
    {
        DrawGizmos = true;

        scene = new Scene();
        SetupInputActions();

        // Load audio files from executable directory
        LoadAudioFiles();

        // Set global audio properties
        AudioEngine.DopplerScale = 1.0f;
        AudioEngine.SpeedOfSound = 343.5f;
        AudioEngine.DistanceScale = 1.0f;


        // Create directional light
        GameObject lightGO = new("Directional Light");
        lightGO.AddComponent<DirectionalLight>();
        lightGO.Transform.LocalEulerAngles = new Double3(-45, -30, 0);
        scene.Add(lightGO);

        // Create camera with audio listener
        cameraGO = new("Main Camera");
        cameraGO.Tag = "Main Camera";
        cameraGO.Transform.Position = new(0, 5, -15);
        Camera camera = cameraGO.AddComponent<Camera>();
        camera.Depth = -1;
        camera.HDR = true;
        camera.Effects =
        [
            new FXAAEffect(),
            new KawaseBloomEffect(),
            new TonemapperEffect(),
        ];
        cameraGO.AddComponent<AudioListener>();
        scene.Add(cameraGO);

        // Create ground plane
        GameObject groundGO = new("Ground");
        MeshRenderer groundMr = groundGO.AddComponent<MeshRenderer>();
        groundMr.Mesh = Mesh.CreateCube(Double3.One);
        groundMr.Material = new Material(Shader.LoadDefault(DefaultShader.Standard));
        groundMr.Material.SetColor("_Color", new Color(0.3f, 0.3f, 0.35f, 1));
        groundGO.Transform.Position = new(0, -1, 0);
        groundGO.Transform.LocalScale = new(40, 0.5, 40);
        scene.Add(groundGO);

        // Create audio sources in the scene
        CreateAudioSources();

        // Create visual markers for audio sources
        CreateVisualMarkers();

        Input.SetCursorVisible(false);
        scene.Activate();
    }

    private void LoadAudioFiles()
    {
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;

        // Try to load ambient_loop.wav
        string ambientPath = Path.Combine(exeDir, "ambient_loop.wav");
        if (File.Exists(ambientPath))
        {
            try
            {
                ambientClip = AudioClip.LoadFromFile(ambientPath, true);
                Debug.Log($"Loaded: {ambientPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load {ambientPath}: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Audio file not found: {ambientPath}");
        }

        // Try to load engine_loop.wav
        string enginePath = Path.Combine(exeDir, "engine_loop.wav");
        if (File.Exists(enginePath))
        {
            try
            {
                engineClip = AudioClip.LoadFromFile(enginePath, true);
                Debug.Log($"Loaded: {enginePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load {enginePath}: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Audio file not found: {enginePath}");
        }

        // Try to load music_loop.wav
        string musicPath = Path.Combine(exeDir, "music_loop.wav");
        if (File.Exists(musicPath))
        {
            try
            {
                musicClip = AudioClip.LoadFromFile(musicPath, true);
                Debug.Log($"Loaded: {musicPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load {musicPath}: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Audio file not found: {musicPath}");
        }

        if (ambientClip == null && engineClip == null && musicClip == null)
        {
            Debug.LogWarning("No audio files loaded. Place ambient_loop.wav, engine_loop.wav, or music_loop.wav next to the executable.");
        }
    }

    private void CreateAudioSources()
    {
        // 1. Static source with no effects (center)
        var staticSource = CreateAudioSourceAt(
            "Static Sound",
            new Double3(0, 2, 0),
            Color.White,
            looping: true,
            volume: 0.8,
            pitch: 1.0,
            maxDistance: 10
        );
        staticSource.Clip = ambientClip ?? engineClip ?? musicClip;

        // 2. Moving source with Doppler effect (orbiting far out)
        var moving = CreateAudioSourceAt(
            "Moving Doppler",
            new Double3(30, 2, 0),
            Color.Red,
            looping: true,
            volume: 0.7,
            pitch: 1.0,
            maxDistance: 12
        );
        moving.Clip = engineClip ?? ambientClip ?? musicClip;

        // 6. High pitched moving source (separate orbit area)
        var highPitch = CreateAudioSourceAt(
            "High Pitch Mover",
            new Double3(-30, 2, 30),
            Color.Green,
            looping: true,
            volume: 0.5,
            pitch: 1.5,
            maxDistance: 10
        );
        highPitch.Clip = engineClip ?? ambientClip ?? musicClip;

        // 7. Low pitched source (front left, far)
        var lowPitch = CreateAudioSourceAt(
            "Low Pitch",
            new Double3(-30, 2, -30),
            Color.Blue,
            looping: true,
            volume: 0.6,
            pitch: 0.7,
            maxDistance: 10
        );
        lowPitch.Clip = engineClip ?? ambientClip ?? musicClip;

        Debug.Log($"Created {audioSources.Count} audio sources");
    }

    private AudioSource CreateAudioSourceAt(string name, Double3 position, Color color, bool looping = false,
        double volume = 1.0, double pitch = 1.0, double maxDistance = 20)
    {
        GameObject go = new(name);
        go.Transform.Position = position;

        AudioSource audioSource = go.AddComponent<AudioSource>();
        audioSource.PlayOnStart = true;
        audioSource.Looping = looping;
        audioSource.Volume = volume;
        audioSource.Pitch = pitch;
        audioSource.MaxDistance = maxDistance;
        audioSource.ReferenceDistance = 1.0;

        audioSources.Add(audioSource);
        scene.Add(go);

        return audioSource;
    }

    private void CreateVisualMarkers()
    {
        Material markerMat = new(Shader.LoadDefault(DefaultShader.Standard));

        foreach (var audioSource in audioSources)
        {
            GameObject markerGO = new($"{audioSource.GameObject.Name} Marker");
            MeshRenderer mr = markerGO.AddComponent<MeshRenderer>();
            mr.Mesh = Mesh.CreateCube(Double3.One);
            mr.Material = markerMat;

            // Color based on audio source type
            if (audioSource.GameObject.Name.Contains("Doppler"))
                mr.Material.SetColor("_Color", new Color(1, 0.2f, 0.2f, 1));
            else if (audioSource.GameObject.Name.Contains("Filter"))
                mr.Material.SetColor("_Color", new Color(1, 1, 0.2f, 1));
            else if (audioSource.GameObject.Name.Contains("Reverb"))
                mr.Material.SetColor("_Color", new Color(0.8f, 0.2f, 1, 1));
            else if (audioSource.GameObject.Name.Contains("High Pitch"))
                mr.Material.SetColor("_Color", new Color(0.2f, 1, 0.2f, 1));
            else if (audioSource.GameObject.Name.Contains("Low Pitch"))
                mr.Material.SetColor("_Color", new Color(0.2f, 0.5f, 1, 1));
            else
                mr.Material.SetColor("_Color", new Color(0.8f, 0.8f, 0.8f, 1));

            markerGO.Transform.Position = audioSource.GameObject.Transform.Position;
            markerGO.Transform.LocalScale = new Double3(0.5, 0.5, 0.5);
            markerGO.Transform.Parent = audioSource.GameObject.Transform;
            scene.Add(markerGO);
        }
    }

    private void SetupInputActions()
    {
        inputMap = new InputActionMap("Audio Demo");

        // Movement (WASD + Gamepad)
        moveAction = inputMap.AddAction("Move", InputActionType.Value);
        moveAction.ExpectedValueType = typeof(Double2);
        moveAction.AddBinding(new Vector2CompositeBinding(
            InputBinding.CreateKeyBinding(KeyCode.W),
            InputBinding.CreateKeyBinding(KeyCode.S),
            InputBinding.CreateKeyBinding(KeyCode.A),
            InputBinding.CreateKeyBinding(KeyCode.D),
            true
        ));
        var leftStick = InputBinding.CreateGamepadAxisBinding(0, deviceIndex: 0);
        leftStick.Processors.Add(new DeadzoneProcessor(0.15f));
        leftStick.Processors.Add(new NormalizeProcessor());
        moveAction.AddBinding(leftStick);

        // Look enable (RMB)
        lookEnableAction = inputMap.AddAction("LookEnable", InputActionType.Button);
        lookEnableAction.AddBinding(MouseButton.Right);

        // Look (Mouse + Gamepad)
        lookAction = inputMap.AddAction("Look", InputActionType.Value);
        lookAction.ExpectedValueType = typeof(Double2);
        var mouse = new DualAxisCompositeBinding(
            InputBinding.CreateMouseAxisBinding(0),
            InputBinding.CreateMouseAxisBinding(1));
        mouse.Processors.Add(new ScaleProcessor(0.1f));
        lookAction.AddBinding(mouse);
        var rightStick = InputBinding.CreateGamepadAxisBinding(1, deviceIndex: 0);
        rightStick.Processors.Add(new DeadzoneProcessor(0.15f));
        rightStick.Processors.Add(new NormalizeProcessor());
        lookAction.AddBinding(rightStick);

        // Fly Up/Down
        flyUpAction = inputMap.AddAction("FlyUp", InputActionType.Button);
        flyUpAction.AddBinding(KeyCode.E);
        flyUpAction.AddBinding(GamepadButton.A);
        flyDownAction = inputMap.AddAction("FlyDown", InputActionType.Button);
        flyDownAction.AddBinding(KeyCode.Q);
        flyDownAction.AddBinding(GamepadButton.B);

        // Sprint
        sprintAction = inputMap.AddAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding(KeyCode.ShiftLeft);
        sprintAction.AddBinding(GamepadButton.LeftStick);

        Input.RegisterActionMap(inputMap);
        inputMap.Enable();
    }

    public override void BeginUpdate()
    {
        time += (float)Time.DeltaTime;

        // Animate moving audio sources
        AnimateMovingSources();

        // Camera controls
        HandleCameraInput();

        // Debug info
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("=== Audio Demo Controls ===");
            Debug.Log("WASD/Arrows: Move camera");
            Debug.Log("Mouse (hold RMB): Look around");
            Debug.Log("E/Q: Fly up/down");
            Debug.Log("Shift: Sprint");
            Debug.Log("R: Toggle reverb (NYI)");
            Debug.Log("F: Cycle filters (NYI)");
            Debug.Log("H: Show this help");
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Input.SetCursorVisible(true);
    }

    private void AnimateMovingSources()
    {
        // Find and animate specific sources
        foreach (var audioSource in audioSources)
        {
            if (audioSource.GameObject.Name.Contains("Moving Doppler"))
            {
                // Large circular orbit on right side
                float angle = time * 0.5f;
                audioSource.GameObject.Transform.Position = new Double3(
                    30 + Math.Cos(angle) * 10,
                    2,
                    Math.Sin(angle) * 10
                );
            }
            else if (audioSource.GameObject.Name.Contains("High Pitch Mover"))
            {
                // Orbit in back-left corner
                float angle = time * 4.2f;
                audioSource.GameObject.Transform.Position = new Double3(
                    -30 + Math.Cos(angle) * 8,
                    2 + Math.Sin(time * 2) * 0.5,
                    30 + Math.Sin(angle) * 8
                );
            }
        }
    }

    private void HandleCameraInput()
    {
        Double2 movement = moveAction.ReadValue<Double2>();
        float speedMultiplier = sprintAction.IsPressed() ? 2.5f : 1.0f;
        float moveSpeed = 8f * speedMultiplier * (float)Time.DeltaTime;
        
        cameraGO.Transform.Position += cameraGO.Transform.Forward * movement.Y * moveSpeed;
        cameraGO.Transform.Position += cameraGO.Transform.Right * movement.X * moveSpeed;
        
        float upDown = 0;
        if (flyUpAction.IsPressed()) upDown += 1;
        if (flyDownAction.IsPressed()) upDown -= 1;
        cameraGO.Transform.Position += Double3.UnitY * upDown * moveSpeed;

        Double2 lookInput = lookAction.ReadValue<Double2>();
        if (lookEnableAction.IsPressed() || Math.Abs(lookInput.X) > 0.01f || Math.Abs(lookInput.Y) > 0.01f)
        {
            cameraGO.Transform.LocalEulerAngles += new Double3(lookInput.Y, lookInput.X, 0);
        }
    }
}
