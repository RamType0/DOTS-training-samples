﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class MovementSystem : SystemBase
{
    const int kChanceToChangeDirection = 3;

    public NativeArray<float2> directions;
    
    protected override void OnCreate()
    {
        directions = new NativeArray<float2>(4, Allocator.Persistent);
        directions[0] = math.left().xy;
        directions[1] = math.right().xy;
        directions[2] = math.up().xy;
        directions[3] = math.down().xy;
    }

    protected override void OnDestroy()
    {
        directions.Dispose();
    }

    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        var dirs      = directions;

        Entities.ForEach((ref ZombieTag _, ref Position position, ref Direction direction, ref Speed speed, ref Random random) => 
        {
            // some percentage of the time, pick a new direction.
            if (random.Value.NextInt(0, 100) < kChanceToChangeDirection)
                direction.Value = dirs[random.Value.NextInt(4)];

            position.Value += direction.Value * speed.Value * deltaTime;
        })
        .Schedule();
    }
}