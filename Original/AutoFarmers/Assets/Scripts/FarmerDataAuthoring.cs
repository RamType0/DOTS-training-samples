﻿using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;


public struct WorkerTeleport:IComponentData {
    public float2 position;
}

public struct FarmerRuntimeData:IComponentData
{
    public Entity plantAttached;
    public Entity targetEntity;
}