﻿using System.Numerics;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using _Vector3 = System.Numerics.Vector3;

namespace FFXIVClientStructs.FFXIV.Client.Graphics.Scene; 

[StructLayout(LayoutKind.Explicit, Size = 0xF0)]
public unsafe partial struct Camera
{
    [FieldOffset(0x00)] public Object Object;
    [FieldOffset(0x80)] public Vector3 Vector_0;
    [FieldOffset(0x90)] public Vector3 Vector_1;
    [FieldOffset(0xA0)] public Matrix4x4 ViewMatrix;
    [FieldOffset(0xE0)] public Render.Camera* RenderCamera;

    [MemberFunction("E8 ?? ?? ?? ?? 4C 8B E0 48 8B EB")]
    public partial void ScreenPointToRay(Ray* ray, int x, int y);

    public Ray ScreenPointToRay(Vector2 screenPoint) {
	    return ScreenPointToRay((int)screenPoint.X, (int)screenPoint.Y);
    }

    public Ray ScreenPointToRay(int x, int y) {
        var pRay = stackalloc Ray[1];
        ScreenPointToRay(pRay, x, y);
        return *pRay;
    }

    public static _Vector3 ScreenToWorldPoint(Vector2 screenPoint) {
	    var ray = CameraManager.Instance()->CurrentCamera->ScreenPointToRay(screenPoint);
	    BGCollisionModule.Raycast(ray.Origin, ray.Direction, out var hit);
	    return hit.Point;
    }

    public static Vector2 WorldToScreenPoint(Vector3 worldPoint) {
	    var screen = stackalloc float[2];
	    WorldToScreenPoint(screen, worldPoint);
	    return *(Vector2*)screen;
    }

    [MemberFunction("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B E9 48 8B DA 48 8D 0D", IsStatic = true, IsPrivate = true)]
    private static partial float* WorldToScreenPoint(float* screenPoint, Vector3 worldPoint);
}