// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime;
using Prowl.Vector;
using Prowl.Runtime.Rendering;
using Prowl.Runtime.Resources;
using Silk.NET.Input;

namespace ShapeCastDemo;

internal class Program
{
    static void Main(string[] args)
    {
        new ShapeCastDemoGame().Run("Shape Cast Demo - Character Controller", 1280, 720);
    }
}

public sealed class ShapeCastDemoGame : Game
{
    private GameObject cameraGO;
    private Scene scene;
    private Material standardMaterial;
    private GameObject playerGO;
    private PlayerController playerController;

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
        cam.Transform.position = new(0, 5, -10);
        cam.Transform.localEulerAngles = new Double3(20, 0, 0);
        var camera = cam.AddComponent<Camera>();
        camera.Depth = -1;
        camera.HDR = true;
        cameraGO = cam;

        camera.Effects = new List<ImageEffect>()
        {
            new ScreenSpaceReflectionEffect(),
            new FXAAEffect(),
            new KawaseBloomEffect(),
            new TonemapperEffect(),
        };

        scene.Add(cam);

        // Create single shared material
        standardMaterial = new Material(Shader.LoadDefault(DefaultShader.Standard));

        // Create floor (static)
        CreateFloor();

        // Create stairs
        CreateStairs();

        // Create slopes
        CreateSlope(new Double3(-8, 0, 5), 2, 10, -30);
        CreateSlope(new Double3(-2, 0, 5), 2, 10, -45);

        CreateSlope(new Double3(8, 0, 5), 2, 10, -60);

        // Create some obstacles
        CreateObstacles();

        // Create player character
        CreatePlayer();
    }

    private void CreateFloor()
    {
        GameObject floor = new GameObject("Floor");
        var floorRenderer = floor.AddComponent<MeshRenderer>();
        floorRenderer.Mesh = Mesh.CreateCube(new Double3(40, 1, 40));
        floorRenderer.Material = standardMaterial;
        floorRenderer.MainColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
        floor.Transform.position = new Double3(0, -0.5f, 0);

        var floorRigidbody = floor.AddComponent<Rigidbody3D>();
        floorRigidbody.IsStatic = true;
        var floorCollider = floor.AddComponent<BoxCollider>();
        floorCollider.Size = new Double3(40, 1, 40);

        scene.Add(floor);
    }

    private void CreateStairs()
    {
        int stepCount = 10;
        double stepWidth = 2.0;
        double stepHeight = 0.3;
        double stepDepth = 0.5;

        for (int i = 0; i < stepCount; i++)
        {
            GameObject step = new GameObject($"Step {i}");
            var stepRenderer = step.AddComponent<MeshRenderer>();
            stepRenderer.Mesh = Mesh.CreateCube(new Double3(stepWidth, stepHeight, stepDepth));
            stepRenderer.Material = standardMaterial;
            stepRenderer.MainColor = new Color(0.6f, 0.6f, 0.8f, 1.0f);

            step.Transform.position = new Double3(
                -10,
                i * stepHeight,
                i * stepDepth
            );

            var stepRb = step.AddComponent<Rigidbody3D>();
            stepRb.IsStatic = true;
            var stepCollider = step.AddComponent<BoxCollider>();
            stepCollider.Size = new Double3(stepWidth, stepHeight, stepDepth);

            scene.Add(step);
        }
    }

    private void CreateSlope(Double3 position, double width, double length, double angleDegrees)
    {
        GameObject slope = new GameObject("Slope");
        var slopeRenderer = slope.AddComponent<MeshRenderer>();
        slopeRenderer.Mesh = Mesh.CreateCube(new Double3(width, 0.5, length));
        slopeRenderer.Material = standardMaterial;
        slopeRenderer.MainColor = new Color(0.8f, 0.6f, 0.6f, 1.0f);

        slope.Transform.position = position;
        slope.Transform.localEulerAngles = new Double3(angleDegrees, 0, 0);

        var slopeRb = slope.AddComponent<Rigidbody3D>();
        slopeRb.IsStatic = true;
        var slopeCollider = slope.AddComponent<BoxCollider>();
        slopeCollider.Size = new Double3(width, 0.5, length);

        scene.Add(slope);
    }

    private void CreateObstacles()
    {
        // Create some boxes as obstacles
        for (int i = 0; i < 5; i++)
        {
            GameObject box = new GameObject($"Obstacle {i}");
            var boxRenderer = box.AddComponent<MeshRenderer>();
            double height = 0.5 + i * 0.5;
            boxRenderer.Mesh = Mesh.CreateCube(new Double3(1, height, 1));
            boxRenderer.Material = standardMaterial;
            boxRenderer.MainColor = new Color(0.9f, 0.5f, 0.3f, 1.0f);

            box.Transform.position = new Double3(
                i * 3 - 6,
                height * 0.5,
                -5
            );

            var boxRb = box.AddComponent<Rigidbody3D>();
            boxRb.IsStatic = true;
            var boxCollider = box.AddComponent<BoxCollider>();
            boxCollider.Size = new Double3(1, height, 1);

            scene.Add(box);
        }
    }

    private void CreatePlayer()
    {
        playerGO = new GameObject("Player");
        playerGO.Transform.position = new Double3(0, 2, 0);

        //var playerRenderer = playerGO.AddComponent<MeshRenderer>();
        //playerRenderer.Mesh = Mesh.CreateCapsule(0.5f, 1.8f, 16, 8);
        //playerRenderer.Material = standardMaterial;

        playerGO.AddComponent<CharacterController>();
        playerController = playerGO.AddComponent<PlayerController>();

        scene.Add(playerGO);
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
        scene.DrawGizmos();

        // Camera follows player from behind
        if (playerGO != null)
        {
            Double3 targetPos = playerGO.Transform.position + new Double3(0, 3, -8);
            cameraGO.Transform.position = Maths.Lerp(cameraGO.Transform.position, targetPos, 1.0 * Time.deltaTime);

            // Look at player
            Double3 lookDir = playerGO.Transform.position - cameraGO.Transform.position;
            if (Double3.Length(lookDir) > 0.01)
            {
                double pitch = System.Math.Atan2(lookDir.Y, System.Math.Sqrt(lookDir.X * lookDir.X + lookDir.Z * lookDir.Z));
                double yaw = System.Math.Atan2(lookDir.X, lookDir.Z);
                cameraGO.Transform.localEulerAngles = new Double3(
                    -pitch * 180.0 / System.Math.PI,
                    yaw * 180.0 / System.Math.PI,
                    0
                );
            }
        }

        // Reset with R
        if (Input.GetKeyDown(KeyCode.R))
        {
            scene.Clear();
            Initialize();
        }
    }
}

/// <summary>
/// Handles player movement, gravity, jumping, crouching, and input.
/// Uses the CharacterController for collision detection and movement.
/// </summary>
public class PlayerController : MonoBehaviour
{
    public double MoveSpeed = 5.0;
    public double CrouchMoveSpeed = 2.5;
    public double JumpForce = 8.0;
    public double Gravity = 20.0;
    public double StandingHeight = 1.8;
    public double CrouchHeight = 0.9;

    private CharacterController characterController;
    private Double3 velocity = Double3.Zero;
    private Double3 moveInput = Double3.Zero;
    private bool jumpInput = false;
    private bool crouchInput = false;
    private bool isCrouching = false;

    public override void OnEnable()
    {
        characterController = GameObject.GetComponent<CharacterController>();
    }

    public override void Update()
    {
        if (characterController == null) return;

        moveInput = Double3.Zero;
        if (Input.GetKey(KeyCode.W)) moveInput += new Double3(0, 0, 1);
        if (Input.GetKey(KeyCode.S)) moveInput -= new Double3(0, 0, 1);
        if (Input.GetKey(KeyCode.A)) moveInput -= new Double3(1, 0, 0);
        if (Input.GetKey(KeyCode.D)) moveInput += new Double3(1, 0, 0);
        moveInput = Double3.Normalize(moveInput);

        jumpInput = Input.GetKeyDown(KeyCode.Space);
        crouchInput = Input.GetKey(KeyCode.ControlLeft);

        // Handle crouching
        HandleCrouch();

        // Update horizontal velocity based on input
        double currentSpeed = isCrouching ? CrouchMoveSpeed : MoveSpeed;
        Double3 horizontalVelocity = moveInput * currentSpeed;
        velocity.X = horizontalVelocity.X;
        velocity.Z = horizontalVelocity.Z;

        HandleGravityAndJump();

        // Calculate total movement for this frame
        Double3 movement = velocity * Time.deltaTime;

        // Move the character using the CharacterController (this also updates IsGrounded)
        characterController.Move(movement);
    }

    private void HandleCrouch()
    {
        if (crouchInput && !isCrouching)
        {
            // Try to crouch
            if (characterController.TrySetHeight(CrouchHeight))
            {
                isCrouching = true;
            }
        }
        else if (!crouchInput && isCrouching)
        {
            // Try to stand up (only if there's clearance above)
            if (characterController.TrySetHeight(StandingHeight))
            {
                isCrouching = false;
            }
            // If TrySetHeight fails, player remains crouched (not enough clearance)
        }
    }

    private void HandleGravityAndJump()
    {
        if (!characterController.IsGrounded)
        {
            velocity.Y -= Gravity * Time.deltaTime;
        }
        else
        {
            if(velocity.Y < 0)
                velocity.Y = 0;

            // Handle jump when grounded (can't jump while crouching)
            if (jumpInput && !isCrouching)
            {
                velocity.Y = JumpForce;
            }
        }
    }

    public override void DrawGizmos()
    {
        // Draw velocity
        if (Double3.Length(velocity) > 0.1)
        {
            Double3 position = GameObject.Transform.position;
            Debug.DrawArrow(position, velocity * 0.5, new Color(255, 255, 0, 255));
        }
    }
}
