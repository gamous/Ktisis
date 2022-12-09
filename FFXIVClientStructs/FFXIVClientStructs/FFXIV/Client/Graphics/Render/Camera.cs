﻿namespace FFXIVClientStructs.FFXIV.Client.Graphics.Render; 

[StructLayout(LayoutKind.Explicit, Size = 0x130)]
public struct Camera
{
    [FieldOffset(0x00)] public ReferencedClassBase ReferencedClassBase;

    [FieldOffset(0x50)] public Matrix44 ProjectionMatrix;
    [FieldOffset(0xA4)] public float FoV;
    [FieldOffset(0xA8)] public float AspectRatio;
    [FieldOffset(0xAC)] public float NearPlane;
    [FieldOffset(0xB0)] public float FarPlane;
}