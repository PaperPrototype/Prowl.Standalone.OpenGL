// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime;
using Prowl.Vector;
using Prowl.Runtime.Rendering;
using Prowl.Runtime.Resources;

using Silk.NET.Input;

namespace PhysicsCubes;

internal class Program
{
    static void Main(string[] args)
    {
        new PhysicsDemo().Run("Physics Demo", 1280, 720);
    }
}

public sealed class PhysicsDemo : Game
{
    private GameObject cameraGO;
    private Scene scene;
    private double selectedCubeMass = 1.0;
    private Material standardMaterial;

    public override void Initialize()
    {
        scene = new Scene();

        // Create directional light
        GameObject lightGO = new("Directional Light");
        var light = lightGO.AddComponent<DirectionalLight>();
        light.shadowQuality = ShadowQuality.Soft;
        light.shadowBias = 0.5f;
        lightGO.Transform.localEulerAngles = new Double3(-45, 45, 0);
        scene.Add(lightGO);

        // Create camera
        GameObject cam = new("Main Camera");
        cam.tag = "Main Camera";
        cam.Transform.position = new(0, 5, -15);
        cam.Transform.localEulerAngles = new Double3(15, 0, 0);
        var camera = cam.AddComponent<Camera>();
        camera.Depth = -1;
        camera.HDR = true;
        cameraGO = cam;

        camera.Effects = new List<ImageEffect>()
        {
            new ScreenSpaceReflectionEffect(),
            new FXAAEffect(),
            new KawaseBloomEffect(),
            new BokehDepthOfFieldEffect(),
            new TonemapperEffect(),
        };

        scene.Add(cam);

        // Create single shared material
        standardMaterial = new Material(Shader.LoadDefault(DefaultShader.Standard));

        // Create floor (static)
        GameObject floor = new GameObject("Floor");
        var floorRenderer = floor.AddComponent<MeshRenderer>();
        floorRenderer.Mesh = Mesh.CreateCube(new Double3(20, 1, 20));
        floorRenderer.Material = standardMaterial;
        floorRenderer.MainColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
        floor.Transform.position = new Double3(0, -0.5f, 0);

        // Add static rigidbody for floor
        var floorRigidbody = floor.AddComponent<Rigidbody3D>();
        floorRigidbody.IsStatic = true;
        var floorCollider = floor.AddComponent<BoxCollider>();
        floorCollider.Size = new Double3(20, 1, 20);

        scene.Add(floor);

        // Demo 1: Chain of connected cubes (BallSocket + DistanceLimit)
        CreateChainDemo(scene, new Double3(-8, 10, 0), new Color(1.0f, 0.7f, 0.2f, 1.0f));

        // Demo 2: Hinged door (HingeJoint)
        CreateHingedDoorDemo(scene, new Double3(0, 2, 0), new Color(0.2f, 1.0f, 0.5f, 1.0f));

        // Demo 3: Prismatic slider (PrismaticJoint)
        CreateSliderDemo(scene, new Double3(8, 3, 0), new Color(1.0f, 0.3f, 0.7f, 1.0f));

        // Demo 4: Ragdoll-style cone limits
        CreateRagdollDemo(scene, new Double3(-4, 8, -5), new Color(0.2f, 0.5f, 1.0f, 1.0f));

        // Demo 5: Powered motor demo
        CreateMotorDemo(scene, new Double3(4, 3, -5), new Color(1.0f, 0.7f, 0.2f, 1.0f));
    }

    private void CreateChainDemo(Scene scene, Double3 startPos, Color color)
    {
        GameObject anchor = new GameObject("Chain Anchor");
        anchor.Transform.position = startPos;
        var anchorRb = anchor.AddComponent<Rigidbody3D>();
        anchorRb.IsStatic = true;
        var anchorCollider = anchor.AddComponent<SphereCollider>();
        anchorCollider.Radius = 0.2f;
        var anchorRenderer = anchor.AddComponent<MeshRenderer>();
        anchorRenderer.Mesh = Mesh.CreateSphere(0.2f, 8, 8);
        anchorRenderer.Material = standardMaterial;
        anchorRenderer.MainColor = color;
        scene.Add(anchor);

        GameObject previousLink = anchor;
        for (int i = 0; i < 5; i++)
        {
            GameObject link = new GameObject($"Chain Link {i}");
            link.Transform.position = startPos + new Double3(0, -(i + 1) * 1.5, 0);
            var linkRenderer = link.AddComponent<MeshRenderer>();
            linkRenderer.Mesh = Mesh.CreateCube(new Double3(0.5, 1, 0.5));
            linkRenderer.Material = standardMaterial;
            linkRenderer.MainColor = color;

            var linkRb = link.AddComponent<Rigidbody3D>();
            linkRb.Mass = 1.0;

            var linkCollider = link.AddComponent<BoxCollider>();
            linkCollider.Size = new Double3(0.5, 1, 0.5);

            // Connect with BallSocket at top
            var ballSocket = link.AddComponent<BallSocketConstraint>();
            ballSocket.ConnectedBody = previousLink.GetComponent<Rigidbody3D>();
            ballSocket.Anchor = new Double3(0, 0.5, 0);

            scene.Add(link);
            previousLink = link;
        }
    }

    private void CreateHingedDoorDemo(Scene scene, Double3 position, Color color)
    {
        // Door frame (static)
        GameObject frame = new GameObject("Door Frame");
        frame.Transform.position = position;
        var frameRb = frame.AddComponent<Rigidbody3D>();
        frameRb.IsStatic = true;
        var frameCollider = frame.AddComponent<BoxCollider>();
        frameCollider.Size = new Double3(0.2, 3, 0.2);
        var frameRenderer = frame.AddComponent<MeshRenderer>();
        frameRenderer.Mesh = Mesh.CreateCube(new Double3(0.2, 3, 0.2));
        frameRenderer.Material = standardMaterial;
        frameRenderer.MainColor = color;
        scene.Add(frame);

        // Door (dynamic)
        GameObject door = new GameObject("Door");
        door.Transform.position = position + new Double3(1.5, 0, 0);
        var doorRenderer = door.AddComponent<MeshRenderer>();
        doorRenderer.Mesh = Mesh.CreateCube(new Double3(3, 2.8, 0.1));
        doorRenderer.Material = standardMaterial;
        doorRenderer.MainColor = color;

        var doorRb = door.AddComponent<Rigidbody3D>();
        doorRb.Mass = 2.0;

        var doorCollider = door.AddComponent<BoxCollider>();
        doorCollider.Size = new Double3(3, 2.8, 0.1);

        // Hinge joint
        var hinge = door.AddComponent<HingeJoint>();
        hinge.ConnectedBody = frameRb;
        hinge.Anchor = new Double3(-1.5, 0, 0);
        hinge.Axis = new Double3(0, 1, 0);
        hinge.MinAngleDegrees = -90;
        hinge.MaxAngleDegrees = 90;

        scene.Add(door);
    }

    private void CreateSliderDemo(Scene scene, Double3 position, Color color)
    {
        // Rail (static)
        GameObject rail = new GameObject("Slider Rail");
        rail.Transform.position = position;
        var railRb = rail.AddComponent<Rigidbody3D>();
        railRb.IsStatic = true;
        var railCollider = rail.AddComponent<BoxCollider>();
        railCollider.Size = new Double3(0.1, 4, 0.1);
        var railRenderer = rail.AddComponent<MeshRenderer>();
        railRenderer.Mesh = Mesh.CreateCube(new Double3(0.1, 4, 0.1));
        railRenderer.Material = standardMaterial;
        railRenderer.MainColor = color;
        scene.Add(rail);

        // Slider (dynamic)
        GameObject slider = new GameObject("Slider");
        slider.Transform.position = position + new Double3(0, 1, 0);
        var sliderRenderer = slider.AddComponent<MeshRenderer>();
        sliderRenderer.Mesh = Mesh.CreateCube(new Double3(1, 0.5, 1));
        sliderRenderer.Material = standardMaterial;
        sliderRenderer.MainColor = color;

        var sliderRb = slider.AddComponent<Rigidbody3D>();
        sliderRb.Mass = 1.5;

        var sliderCollider = slider.AddComponent<BoxCollider>();
        sliderCollider.Size = new Double3(1, 0.5, 1);

        // Prismatic joint (slider)
        var prismatic = slider.AddComponent<PrismaticJoint>();
        prismatic.ConnectedBody = railRb;
        prismatic.Anchor = Double3.Zero;
        prismatic.Axis = new Double3(0, 1, 0);
        prismatic.MinDistance = -1.5;
        prismatic.MaxDistance = 1.5;
        prismatic.Pinned = true;

        scene.Add(slider);
    }

    private void CreateRagdollDemo(Scene scene, Double3 position, Color color)
    {
        // Torso (parent body)
        GameObject torso = new GameObject("Torso");
        torso.Transform.position = position;
        var torsoRenderer = torso.AddComponent<MeshRenderer>();
        torsoRenderer.Mesh = Mesh.CreateCube(new Double3(1, 1.5, 0.5));
        torsoRenderer.Material = standardMaterial;
        torsoRenderer.MainColor = color;

        var torsoRb = torso.AddComponent<Rigidbody3D>();
        torsoRb.Mass = 2.0;

        var torsoCollider = torso.AddComponent<BoxCollider>();
        torsoCollider.Size = new Double3(1, 1.5, 0.5);

        scene.Add(torso);

        // Left arm with cone limit
        GameObject leftArm = new GameObject("Left Arm");
        leftArm.Transform.position = position + new Double3(-0.75, 0.5, 0);
        var armRenderer = leftArm.AddComponent<MeshRenderer>();
        armRenderer.Mesh = Mesh.CreateCube(new Double3(1, 0.3, 0.3));
        armRenderer.Material = standardMaterial;
        armRenderer.MainColor = color;

        var armRb = leftArm.AddComponent<Rigidbody3D>();
        armRb.Mass = 0.5;

        var armCollider = leftArm.AddComponent<BoxCollider>();
        armCollider.Size = new Double3(1, 0.3, 0.3);

        // Ball socket for shoulder
        var shoulderBall = leftArm.AddComponent<BallSocketConstraint>();
        shoulderBall.ConnectedBody = torsoRb;
        shoulderBall.Anchor = new Double3(0.5, 0, 0);

        // Cone limit to restrict arm movement
        var shoulderCone = leftArm.AddComponent<ConeLimitConstraint>();
        shoulderCone.ConnectedBody = torsoRb;
        shoulderCone.Axis = new Double3(1, 0, 0);
        shoulderCone.MinAngle = 0;
        shoulderCone.MaxAngle = 45;

        scene.Add(leftArm);
    }

    private void CreateMotorDemo(Scene scene, Double3 position, Color color)
    {
        // Base (static)
        GameObject motorBase = new GameObject("Motor Base");
        motorBase.Transform.position = position;
        var baseRb = motorBase.AddComponent<Rigidbody3D>();
        baseRb.IsStatic = true;
        var baseCollider = motorBase.AddComponent<BoxCollider>();
        baseCollider.Size = new Double3(0.5, 0.5, 0.5);
        var baseRenderer = motorBase.AddComponent<MeshRenderer>();
        baseRenderer.Mesh = Mesh.CreateCube(new Double3(0.5, 0.5, 0.5));
        baseRenderer.Material = standardMaterial;
        baseRenderer.MainColor = color;
        scene.Add(motorBase);

        // Spinning platform
        GameObject platform = new GameObject("Spinning Platform");
        platform.Transform.position = position + new Double3(0, 0.5, 0);
        var platformRenderer = platform.AddComponent<MeshRenderer>();
        platformRenderer.Mesh = Mesh.CreateCube(new Double3(2, 0.2, 2));
        platformRenderer.Material = standardMaterial;
        platformRenderer.MainColor = color;

        var platformRb = platform.AddComponent<Rigidbody3D>();
        platformRb.Mass = 1.0;

        var platformCollider = platform.AddComponent<BoxCollider>();
        platformCollider.Size = new Double3(2, 0.2, 2);

        // Hinge joint with motor
        var motorHinge = platform.AddComponent<HingeJoint>();
        motorHinge.ConnectedBody = baseRb;
        motorHinge.Anchor = new Double3(0, -0.3, 0);
        motorHinge.Axis = new Double3(0, 1, 0);
        motorHinge.HasMotor = true;
        motorHinge.MotorTargetVelocity = 2.0; // Radians per second
        motorHinge.MotorMaxForce = 10.0;

        scene.Add(platform);
    }


    Mesh cubeShootMesh = null;
    int shootCounter = 0;
    GameObject lastShot = null;
    private void ShootCube()
    {
        // Create a cube at camera position
        GameObject cube = new GameObject("Shot Cube");
        lastShot = cube;
        cube.Transform.position = cameraGO.Transform.position + cameraGO.Transform.forward * 2.0;

        var cubeRenderer = cube.AddComponent<MeshRenderer>();
        cubeShootMesh = cubeShootMesh == null ? Mesh.CreateCube(new Double3(0.5, 0.5, 0.5)) : cubeShootMesh;
        cubeRenderer.Mesh = cubeShootMesh;
        cubeRenderer.Material = standardMaterial;
        cubeRenderer.MainColor = new Color(1.0f, 0.3f, 0.3f, 1.0f);

        var cubeRb = cube.AddComponent<Rigidbody3D>();
        cubeRb.Mass = selectedCubeMass;
        cubeRb.EnableSpeculativeContacts = true;

        var cubeCollider = cube.AddComponent<BoxCollider>();
        cubeCollider.Size = new Double3(0.5, 0.5, 0.5);

        //var light = cube.AddComponent<PointLight>();
        //light.intensity = 32;
        //light.color = new Color(RNG.Shared.NextDouble(), RNG.Shared.NextDouble(), RNG.Shared.NextDouble(), 1f);

        scene.Add(cube);

        // Add velocity in the direction the camera is facing
        cubeRb.LinearVelocity = cameraGO.Transform.forward * 20.0;


        shootCounter++;
        Debug.Log($"Shot cube #{shootCounter} with mass {selectedCubeMass}");
    }

    public override void FixedUpdate()
    {
        scene.FixedUpdate();
    }

    public override void Render()
    {
        scene.RenderScene();
    }

    public override void Update()
    {
        scene.Update();
        //scene.DrawGizmos();

        // Camera movement
        Double2 movement = Double2.Zero;
        if (Input.GetKey(KeyCode.W)) movement += Double2.UnitY;
        if (Input.GetKey(KeyCode.S)) movement -= Double2.UnitY;
        if (Input.GetKey(KeyCode.A)) movement -= Double2.UnitX;
        if (Input.GetKey(KeyCode.D)) movement += Double2.UnitX;

        // forward/back
        cameraGO.Transform.position += cameraGO.Transform.forward * movement.Y * 10f * Time.deltaTime;
        // left/right
        cameraGO.Transform.position += cameraGO.Transform.right * movement.X * 10f * Time.deltaTime;

        // up/down
        float upDown = 0;
        if (Input.GetKey(KeyCode.E)) upDown += 1;
        if (Input.GetKey(KeyCode.Q)) upDown -= 1;
        cameraGO.Transform.position += Double3.UnitY * upDown * 10f * Time.deltaTime;

        // rotate with mouse
        if (Input.GetMouseButton(1))
        {
            Double2 delta = Input.MouseDelta;
            cameraGO.Transform.localEulerAngles += new Double3(delta.Y, delta.X, 0) * 0.1f;
        }

        // Reset scene with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Clear and reinitialize
            scene.Clear();
            Initialize();
        }

        if (Input.GetKeyDown(KeyCode.X) && lastShot != null)
        {
            lastShot.Destroy();
        }

        // Weight selection with number keys
        if (Input.GetKeyDown(KeyCode.Number1))
        {
            selectedCubeMass = 0.5;
            Debug.Log($"Cube weight set to: {selectedCubeMass}");
        }
        else if (Input.GetKeyDown(KeyCode.Number2))
        {
            selectedCubeMass = 1.0;
            Debug.Log($"Cube weight set to: {selectedCubeMass}");
        }
        else if (Input.GetKeyDown(KeyCode.Number3))
        {
            selectedCubeMass = 2.0;
            Debug.Log($"Cube weight set to: {selectedCubeMass}");
        }
        else if (Input.GetKeyDown(KeyCode.Number4))
        {
            selectedCubeMass = 5.0;
            Debug.Log($"Cube weight set to: {selectedCubeMass}");
        }

        // Shoot cube with left mouse button
        if (Input.GetMouseButton(0))
        {
            ShootCube();
        }
    }
}
