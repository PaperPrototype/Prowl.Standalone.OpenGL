using Prowl.Runtime.Resources;
using Prowl.Runtime;
using Prowl.Vector;
using Prowl.PaperUI;
using Prowl.Echo;

namespace Playground;


internal class Program
{
    static void Main(string[] args)
    {
        new PlaygroundGame().Run("Playground Game", 1280, 720);
    }
}

public class PlaygroundGame : Game
{
    private Scene scene;
    private GameObject cameraGO;
    private LookCamera lookCamera;
    private GameObject indicator;
    private List<Double3> placedCubes = new List<Double3>();

    public override void Initialize()
    {
        scene = new Scene();

        // Light
        var lightGO = new GameObject("Directional Light");
        lightGO.AddComponent<DirectionalLight>();
        lightGO.Transform.LocalEulerAngles = new Double3(-80, 5, 0);
        scene.Add(lightGO);

        // MODIFIED from original prowl (personal taste)
        var groundGO = GameObject.PhysicsCube("Ground", new Double3(0, -3, 0), new Double3(200, 1, 200), true);
        scene.Add(groundGO);

        // Camera
        cameraGO = new("Main Camera");
        cameraGO.Tag = "Main Camera";
        cameraGO.Transform.Position = new(0, 2, -8);
        cameraGO.AddComponent<Camera>();
        lookCamera = cameraGO.AddComponent<LookCamera>();
        scene.Add(cameraGO);

        indicator = GameObject.Cube("Indicator");
        scene.Add(indicator);

        // Runs the Update/FixedUpdate methods
        scene.Activate();

        DrawGizmos = true;

        LoadCubes();

        // You can also deactivate a scene
        // scene.Deactivate();
    }

    public override void Closing()
    {
        SaveCubes();
    }

    Prowl.Scribe.FontFile fontFile = null;

    public override void EndGui(Paper gui)
    {
        fontFile ??= new Prowl.Scribe.FontFile("/System/Library/Fonts/Monaco.ttf");

        if (!lookCamera.CursorVisible) return;
                            
        using (gui.Box("Main").Enter())
        {
            gui.Box("spacer");
            using (gui.Box("Instructions").Enter())
            {
                using (gui.Row("Sapcers").Enter())
                {
                    gui.Box("spacer");

                    using (gui.Column("Padding")
                        .Rounded(5)
                        .BackgroundColor(new Color(0.0, 0.0, 0.0, 0.8))
                        .Enter())
                    {
                        gui.Box("Info")
                        .Margin(5)
                        .Wrap(Prowl.Scribe.TextWrapMode.Wrap)
                        .Text($"""
                        FPS {(int)(1 / Time.SmoothDeltaTime)}
                        WELCOME TO THE PLAYGROUND

                        Instructions:
                        Click the escape key to enable mouse
                        Right click the mouse to place things
                        """, fontFile);

                        using (gui.Row("Buttons").Height(40).Margin(5).Enter())
                        {
                            gui.Box("spacer");

                            gui.Box("Start button")
                            .Rounded(5)
                            .Alignment(TextAlignment.MiddleCenter)
                            .Text("Okay", fontFile)
                            .TextColor(Color.Black)
                            .Height(30).Width(90)
                            .BackgroundColor(Color.Wheat)
                            .OnClick((_) =>
                            {
                                lookCamera.CursorVisible = false;
                            }); 
                        }
                    }

                    gui.Box("spacer");
                }
            }
            gui.Box("spacer");
        }
    }

    public override void EndUpdate()
    {
        RaycastHit hit;
        if (scene.Physics.Raycast(cameraGO.Transform.Position, cameraGO.Transform.Forward, out hit))
        {
            indicator.Enabled = true;
            indicator.Transform.Position = hit.Point;
            if (Input.GetMouseButtonDown(0))
            {
                PlaceCube(hit.Point);
            }
        }
        else
        {
            indicator.Enabled = false;
        }
    }

    private void SaveCubes()
    {
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;

        // Try to load ambient_loop.wav
        string cubesPath = Path.Combine(exeDir, "cubes.json");
        try
        {
            var echoObject = Serializer.Serialize(placedCubes);
            var jsonText = echoObject.WriteToString();
            File.WriteAllText(cubesPath, jsonText);
            Debug.Log($"Saved: {cubesPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save {cubesPath}: {ex.Message}");
        }
    }

    private void LoadCubes()
    {
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;

        // Try to load ambient_loop.wav
        string cubesPath = Path.Combine(exeDir, "cubes.json");
        if (File.Exists(cubesPath))
        {
            try
            {
                var jsonText = File.ReadAllText(cubesPath);
                var echoObject = EchoObject.ReadFromString(jsonText);

                Debug.Log($"echoObject.Count {echoObject.Count}");
                
                var deserialized = Serializer.Deserialize<List<Double3>>(echoObject);

                if (deserialized != null)
                {
                    foreach (var pos in deserialized)
                    {
                        PlaceCube(pos);
                    }
                    Debug.Log($"Loaded and deserialized: {cubesPath}");
                }
                else
                {
                    Debug.LogWarning($"Failed to deserialize: {cubesPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load {cubesPath}: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Cubes file not found: {cubesPath}");
        }
    }

    private void PlaceCube(Double3 position)
    {
        placedCubes.Add(position);
        scene.Add(GameObject.PhysicsCube(position, true));
    }
}