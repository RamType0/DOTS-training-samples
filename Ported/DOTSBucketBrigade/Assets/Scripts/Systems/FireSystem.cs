using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public struct BoardElement : IBufferElementData
{
	// represents the heat of the cell element.
	// 0 -> no heat, 1 -> on fire
	public float Value;

	public static implicit operator float(BoardElement e)
	{
		return e.Value;
	}

	public static implicit operator BoardElement (float e)
	{
		return new BoardElement { Value = e };
	}
}

public class FireSystem : SystemBase
{
	Entity m_BoardEntity;
	NativeArray<int2> m_NeighborOffsets;

	protected override void OnCreate()
	{
		System.Random fireRandom = new System.Random(1234);

		m_BoardEntity = EntityManager.CreateEntity();
		DynamicBuffer<BoardElement> boardCells = EntityManager.AddBuffer<BoardElement>(m_BoardEntity);
		boardCells.ResizeUninitialized(FireSimConfig.xDim * FireSimConfig.yDim);
		
		for (int i = 0; i < boardCells.Length; ++i)
		{
			boardCells[i] = 0.0f;
		}

		//temp for visualizing the fire.
		for (int i = 0; i < FireSimConfig.numFireStarters; ++i)
		{
			boardCells[fireRandom.Next(0, boardCells.Length)] = 1.0f;
		}

		m_NeighborOffsets = new NativeArray<int2>(8, Allocator.Persistent);
		NativeArray<int2>.Copy(new [] {new int2(+0, -1),
			new int2(+1, -1),
			new int2(+1, +0),
			new int2(+1, +1),
			new int2(+0, +1),
			new int2(-1, +1),
			new int2(-1, +0),
			new int2(-1, -1)}, m_NeighborOffsets);
	}
	
	protected override void OnUpdate()
	{
		var xDim = FireSimConfig.xDim;
		var yDim = FireSimConfig.yDim;

		var neighborOffsets = m_NeighborOffsets;
		var newHeat = new NativeArray<float>(xDim*yDim, Allocator.TempJob);
		var heatTransferRate = FireSimConfig.heatTransferRate;

		var currentDeltaTime = Time.DeltaTime;

		float4 groundColor = new float4(FireSimConfig.color_ground.r, FireSimConfig.color_ground.g, FireSimConfig.color_ground.b, FireSimConfig.color_ground.a);
		float4 fireLowColor = new float4(FireSimConfig.color_fire_low.r, FireSimConfig.color_fire_low.g, FireSimConfig.color_fire_low.b, FireSimConfig.color_fire_low.a);
		float4 fireHighColor = new float4(FireSimConfig.color_fire_high.r, FireSimConfig.color_fire_high.g, FireSimConfig.color_fire_high.b, FireSimConfig.color_fire_high.a);

		Entities.ForEach((in DynamicBuffer<BoardElement> board) =>
		{
			for (int i=0; i<board.Length; ++i)
			{
				float heatValue = 0; 
				int2 coord = new int2(i % xDim, i/xDim);
				for (int j=0; j<8; j++)
				{
					var neighbor = neighborOffsets[j];
					int2 neighborCoord = coord + neighbor;
					if (math.any(neighborCoord >= new int2(xDim, yDim)) ||
					    math.any(neighborCoord < int2.zero))
					{
						continue;
					}

					heatValue += board[neighborCoord.y*xDim + neighborCoord.x];
				}

				newHeat[i] = Math.Min(1.0f, board[i] + (heatTransferRate * heatValue * currentDeltaTime));
			}
		}).Schedule();
		
		Entities.WithReadOnly(newHeat).ForEach((ref Translation translation, ref URPMaterialPropertyBaseColor fireColor, in FireCell fireCell) =>
			{
				var index = fireCell.coord.y * xDim + fireCell.coord.x;
				float3 newTranslation = translation.Value;
				newTranslation.x = fireCell.coord.x; 
				newTranslation.y = newHeat[index];
				newTranslation.z = fireCell.coord.y;
				translation.Value = newTranslation;
				if (newHeat[index] < 0.01f)
				{
					fireColor.Value = groundColor;
				}
				else if (newHeat[index] < 0.5f)
				{
					fireColor.Value = fireLowColor;
				}
				else 
				{
					fireColor.Value = fireHighColor;
				}
			}
		).Schedule();

		Entities.ForEach((ref DynamicBuffer<BoardElement> board) =>
		{
			for (int i = 0; i < board.Length; ++i)
			{
				board[i] = newHeat[i];
			}
		}).Schedule();

		Dependency = newHeat.Dispose(Dependency);
	}
}