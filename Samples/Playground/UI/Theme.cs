using Prowl.Runtime;
using Prowl.Vector;


namespace Playground;

public static class Themes
{
    public static Color base100;
    public static Color base200;
    public static Color base250;
    public static Color base300;
    public static Color base400;
    public static Color baseContent; // text
    public static Color primary;
    public static Color primaryContent; // text
    public static Color[] colorPalette;

    public static void Initialize()
    {
        //Dark
        base100 = new Color(255, 31, 31, 36);
        base200 = new Color(255, 42, 42, 46);
        base250 = new Color(255, 54, 55, 59);
        base300 = new Color(255, 64, 64, 68);
        base400 = new Color(255, 70, 71, 76);
        baseContent = new Color(255, 230, 230, 230);
        primary = new Color(255, 69, 135, 235);
        primaryContent = new Color(255, 226, 232, 240);
        colorPalette = [
            new Color(255, 94, 234, 212),   // Cyan
            new Color(255, 162, 155, 254),  // Purple  
            new Color(255, 249, 115, 22),   // Orange
            new Color(255, 248, 113, 113),  // Red
            new Color(255, 250, 204, 21)    // Yellow
        ];

// //Dark
//         base100 = new Color(1/255, 1/31, 1/31, 1/36);
//         base200 = new Color(1/255, 1/42, 1/42, 1/46);
//         base250 = new Color(1/255, 1/54, 1/55, 1/59);
//         base300 = new Color(1/255, 1/64, 1/64, 1/68);
//         base400 = new Color(1/255, 1/70, 1/71, 1/76);
//         baseContent = new Color(1/255, 1/230, 1/230, 1/230);
//         primary = new Color(1/255, 1/69, 1/135, 1/235);
//         primaryContent = new Color(1/255, 1/226, 1/232, 1/240);
//         colorPalette = [
//             new Color(1/255, 1/94, 1/234, 1/212),   // Cyan
//             new Color(1/255, 1/162, 1/155, 1/254),  // Purple  
//             new Color(1/255, 1/249, 1/115, 1/22),   // Orange
//             new Color(1/255, 1/248, 1/113, 1/113),  // Red
//             new Color(1/255, 1/250, 1/204, 1/21)    // Yellow
//         ];
    }
}