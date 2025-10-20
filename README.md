> [!NOTE]
> This is a fork of Prowl. It contains a Unity like API with GameObjects and Scenes. I forked it right before the lead developer (Wulferis) was about to do a rewrite of the engine to use BGFX instead of OpenGL. It will probably be better once that is done (maybe that will be done within a few days) but I wanted my own fork of Prowl that was confirmed working on Mac and Windows. Be sure to check out the original Prowl repository for the latest changes.

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

## About The Project

Prowl is an open-source, **[MIT-licensed](#span-aligncenter-license-span)** game engine developed in **pure C# in latest .NET**. Be sure to check out the [original repository](https://github.com/ProwlEngine/Prowl)!

It aims to provide a seamless transition for developers familiar with _Unity_ by maintaining a similar API while also following KISS and staying as small and customizable as possible. Ideally, _Unity_ projects can port over with as little resistance as possible.

Please keep in mind that this is a fork of Prowl that was incredibly new and unstable and the main repository of Prowl should be used instead.

[Join the Prowl Discord](https://discord.gg/BqnJ9Rn4sn)

## Getting Started

The Samples folder should contain fully working projects that you can run. 

**VS Code:**

If you are VS Code then you can select the `Program.cs` file and click the tiny little play button at the top of the editor.

**Visual Studio:**

Select a solution that is in the Samples folder and run it.

**Terminal and Command Prompt (VS Code):**

On windows open a new Command Prompt (cmd) terminal in VS Codes integrated terminal. Change into a folder in the samples directory.

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

---

[Join the Prowl Discord](https://discord.gg/BqnJ9Rn4sn)
[![Discord](https://img.shields.io/discord/1151582593519722668?logo=discord
)](https://discord.gg/BqnJ9Rn4sn)

