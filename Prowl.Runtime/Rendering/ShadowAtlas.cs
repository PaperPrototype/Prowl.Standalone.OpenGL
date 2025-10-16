// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;

using Prowl.Runtime.Resources;
using Prowl.Vector;

namespace Prowl.Runtime.Rendering;

public static class ShadowAtlas
{
    private static int size;
    private static int maxShadowSize;
    private static RenderTexture? atlas;

    // Skyline algorithm data structures
    private struct SkylineSegment
    {
        public int X;       // Start position
        public int Y;       // Height of this segment
        public int Width;   // Width of this segment

        public SkylineSegment(int x, int y, int width)
        {
            X = x;
            Y = y;
            Width = width;
        }
    }

    private struct PackedRect
    {
        public int X, Y, Width, Height;
        public int LightID;

        public PackedRect(int x, int y, int width, int height, int lightID)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            LightID = lightID;
        }
    }

    private static List<SkylineSegment> skyline = [];
    private static List<PackedRect> packedRects = [];

    public static void TryInitialize()
    {
        if (atlas.IsValid()) return;

        bool supports8k = Graphics.MaxTextureSize >= 8192;
        size = supports8k ? 8192 : 4096;
        maxShadowSize = 1024;

        atlas ??= new RenderTexture(size, size, true, []);

        // Initialize skyline with one segment spanning the entire width at y=0
        skyline.Clear();
        skyline.Add(new SkylineSegment(0, 0, size));
        packedRects.Clear();
    }

    public static int GetMinShadowSize() => 32; // Minimum shadow resolution
    public static int GetMaxShadowSize() => maxShadowSize;
    public static int GetSize() => size;
    public static RenderTexture? GetAtlas() => atlas;

    public static Int2? ReserveTiles(int width, int height, int lightID)
    {
        // Clamp to min/max bounds
        width = Math.Clamp(width, 32, maxShadowSize);
        height = Math.Clamp(height, 32, maxShadowSize);

        if (width > size || height > size)
            return null;

        // Find best position using bottom-left heuristic
        int bestIndex = -1;
        int bestY = int.MaxValue;
        int bestX = int.MaxValue;
        int bestWastedArea = int.MaxValue;

        for (int i = 0; i < skyline.Count; i++)
        {
            int x = skyline[i].X;
            int y = 0;
            int wastedArea = 0;

            if (!CanFit(i, width, height, ref x, ref y, ref wastedArea))
                continue;

            // Prefer lower positions (bottom-left), then leftmost, then least waste
            if (y < bestY || (y == bestY && x < bestX) || (y == bestY && x == bestX && wastedArea < bestWastedArea))
            {
                bestIndex = i;
                bestY = y;
                bestX = x;
                bestWastedArea = wastedArea;
            }
        }

        if (bestIndex == -1)
            return null;

        // Place the rectangle
        PlaceRect(bestIndex, bestX, bestY, width, height, lightID);

        return new Int2(bestX, bestY);
    }

    // Reserve tiles for point light cubemap shadows (2x3 grid layout)
    public static Int2? ReserveCubemapTiles(int faceSize, int lightID)
    {
        int cubemapWidth = faceSize * 2;  // 2 faces wide
        int cubemapHeight = faceSize * 3; // 3 faces tall
        return ReserveTiles(cubemapWidth, cubemapHeight, lightID);
    }

    private static bool CanFit(int segmentIndex, int width, int height, ref int outX, ref int outY, ref int wastedArea)
    {
        int x = skyline[segmentIndex].X;
        int y = skyline[segmentIndex].Y;

        // Check if we go beyond atlas width
        if (x + width > size)
            return false;

        int maxY = y;
        int currentX = x;
        int segmentIdx = segmentIndex;
        wastedArea = 0;

        // Check all segments we would overlap
        while (currentX < x + width && segmentIdx < skyline.Count)
        {
            SkylineSegment segment = skyline[segmentIdx];
            maxY = Math.Max(maxY, segment.Y);

            // Check if height fits
            if (maxY + height > size)
                return false;

            // Calculate wasted area (area below rect but above skyline)
            int overlapWidth = Math.Min(segment.X + segment.Width, x + width) - currentX;
            wastedArea += overlapWidth * (maxY - segment.Y);

            currentX += overlapWidth;
            segmentIdx++;
        }

        outX = x;
        outY = maxY;
        return true;
    }

    private static void PlaceRect(int segmentIndex, int x, int y, int width, int height, int lightID)
    {
        packedRects.Add(new PackedRect(x, y, width, height, lightID));

        int newY = y + height;
        int rectRight = x + width;
        int i = segmentIndex;

        // Handle segment that starts before rectangle
        if (skyline[i].X < x)
        {
            SkylineSegment segment = skyline[i];
            int leftWidth = x - segment.X;
            skyline[i] = new SkylineSegment(segment.X, segment.Y, leftWidth);
            i++;

            // Insert new segment for overlapped portion if needed
            if (segment.X + segment.Width > rectRight)
            {
                skyline.Insert(i, new SkylineSegment(rectRight, segment.Y,
                    segment.X + segment.Width - rectRight));
            }
        }

        // Remove or trim segments covered by rectangle
        while (i < skyline.Count && skyline[i].X < rectRight)
        {
            SkylineSegment segment = skyline[i];

            if (segment.X + segment.Width <= rectRight)
            {
                skyline.RemoveAt(i);
            }
            else
            {
                skyline[i] = new SkylineSegment(rectRight, segment.Y,
                    segment.X + segment.Width - rectRight);
                break;
            }
        }

        // Insert new segment for placed rectangle
        skyline.Insert(i, new SkylineSegment(x, newY, width));

        MergeSkyline();
    }

    private static void MergeSkyline()
    {
        for (int i = 0; i < skyline.Count - 1; i++)
        {
            if (skyline[i].Y == skyline[i + 1].Y)
            {
                skyline[i] = new SkylineSegment(skyline[i].X, skyline[i].Y, skyline[i].Width + skyline[i + 1].Width);
                skyline.RemoveAt(i + 1);
                i--;
            }
        }
    }

    public static void Clear()
    {
        // Reset skyline to initial state
        skyline.Clear();
        skyline.Add(new SkylineSegment(0, 0, size));
        packedRects.Clear();
    }
}
