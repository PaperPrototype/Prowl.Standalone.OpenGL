> [!NOTE]
> This is a fork of Prowl. I forked it right before the lead developer (Wulferis) was about to do a rewrite of the engine to use BGFX instead of OpenGL. It will probably be better once that is done (maybe that will be done within a few days) but I wanted my own fork of Prowl that was confirmed working on Mac and Windows. Be sure to check out the original Prowl repository for the latest changes.

<img src="https://github.com/Kuvrot/Prowl/assets/23508114/5eef8da7-fb84-42f3-9d18-54b4f2d06551" width="100%" alt="Prowl logo image">

![Github top languages](https://img.shields.io/github/languages/top/michaelsakharov/prowl)
[![GitHub version](https://img.shields.io/github/v/release/michaelsakharov/prowl?include_prereleases&style=flat-square)](https://github.com/michaelsakharov/prowl/releases)
[![GitHub license](https://img.shields.io/github/license/michaelsakharov/prowl?style=flat-square)](https://github.com/michaelsakharov/prowl/blob/main/LICENSE.txt)
[![GitHub issues](https://img.shields.io/github/issues/michaelsakharov/prowl?style=flat-square)](https://github.com/michaelsakharov/prowl/issues)
[![GitHub stars](https://img.shields.io/github/stars/michaelsakharov/prowl?style=flat-square)](https://github.com/michaelsakharov/prowl/stargazers)
[![Discord](https://img.shields.io/discord/1151582593519722668?logo=discord
)](https://discord.gg/BqnJ9Rn4sn)

1. [About The Project](#about-the-project)
2. [Getting Started](#getting-started)
2. [Contributors](#contributors)
3. [Dependencies](#dependencies)
2. [Editor](#editor)
4. [License](#license)
5. [Join the Prowl Discord (link)](https://discord.gg/BqnJ9Rn4sn)
[![Discord](https://img.shields.io/discord/1151582593519722668?logo=discord
)](https://discord.gg/BqnJ9Rn4sn)


## About The Project

Prowl is an open-source, **[MIT-licensed](#span-aligncenter-license-span)** game engine developed in **pure C# in latest .NET**. Be sure to check out the [original repository](https://github.com/ProwlEngine/Prowl)!

It aims to provide a seamless transition for developers familiar with _Unity_ by maintaining a similar API. You can use GameObjects and Scenes much like in Unity.

```cs
using Prowl.Runtime;
using Prowl.Vector;

class Program
{
    static void Main(string[] args)
    {
        new MyGame().Run("LineRenderer Demo", 1280, 720);
    }
}

class MyGame : Game
{
    private GameObject cameraGO;
    private Scene scene;

    public override void Initialize()
    {
        scene = new Scene();

        // Light
        var lightGO = new GameObject("Directional Light");
        lightGO.AddComponent<DirectionalLight>();
        lightGO.Transform.LocalEulerAngles = new Double3(-80, 5, 0);
        scene.Add(lightGO);

        // Create ground plane
        // var groundGO = new GameObject("Ground");
        // var mr = groundGO.AddComponent<MeshRenderer>();
        // mr.Mesh = Mesh.CreateCube(Double3.One);
        // mr.Material = new Material(Shader.LoadDefault(DefaultShader.Standard));
        // groundGO.Transform.Position = new(0, -3, 0);
        // groundGO.Transform.LocalScale = new(20, 1, 20);
        // scene.Add(groundGO);

        // MODIFIED from original prowl (personal taste)
        var groundGO = GameObject.Cube("Ground", new Double3(0, -3, 0), new Double3(20, 1, 20))
        scene.Add(groundGO);

        // Camera
        cameraGO = new("Main Camera");
        cameraGO.tag = "Main Camera";
        cameraGO.Transform.Position = new(0, 2, -8);
        var camera = cameraGO.AddComponent<Camera>();
        scene.Add(cameraGO);

        // Runs the Update/FixedUpdate methods
        scene.Activate();

        // You can also deactivate a scene
        // scene.Deactivate();
    }
}
```

Please keep in mind that this is a fork of Prowl that was incredibly new and unstable and the main repository of Prowl should be used instead.

## Getting Started

The Samples folder should contain fully working projects that you can run. 

**VS Code:**

If you are VS Code then you can select the `Program.cs` file and click the tiny little play button at the top of the editor.

**Visual Studio:**

Select a solution that is in the Samples folder and run it.

**Terminal and Command Prompt (VS Code):**

On windows open a new Command Prompt (cmd) terminal in VS Codes integrated terminal. On Mac it will default to the correct one when you open the itegrated terminal. 

Change into a folder in the Samples directory.

> ```bat
> cd Samples/PhysicsCubes
> ```

Assuming you have [dotnet](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed you should be able to then run the following command in the same folder as `Program.cs`.

> ```bat
> dotnet run
> ```

The same commands work on MacOS (I am primarily a Mac user).

## Contributors

- [Michael (Wulferis)](https://twitter.com/Wulferis)
- [Abdiel Lopez (PaperPrototype)](https://github.com/PaperPrototype)
- [Josh Davis](https://github.com/10xJosh)
- [ReCore67](https://github.com/recore67)
- [Isaac Marovitz](https://github.com/IsaacMarovitz)
- [Kuvrot](https://github.com/Kuvrot)
- [JaggerJo](https://github.com/JaggerJo)
- [Jihad Khawaja](https://github.com/jihadkhawaja)
- [Jasper Honkasalo](https://github.com/japsuu)
- [Kai Angulo (k0t)](https://github.com/sinnwrig)
- [Bruno Massa](https://github.com/brmassa)
- [Mark Saba (ZeppelinGames)](https://github.com/ZeppelinGames)
- [EJTP (Unified)](https://github.com/EJTP)

## Dependencies

- [Silk.NET](https://github.com/dotnet/Silk.NET)
- [Jitter Physics 2](https://github.com/notgiven688/jitterphysics2)

## Editor
The standalone branch of Prowl that this fork was based on does not have an editor, and instead is a fully code first engine. Eventually the editor will be added back again. But yeah, if you are reading this please check out the Prowl repository for the latest updates.

## License

Distributed under the MIT License. See [LICENSE](//LICENSE) for more information.
