using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Antymology.Terrain;
using System.Linq.Expressions;



public class Queen : Ant
{
    private float BuildCost;

    void Start()
    {
        MaxHealth = 100;
        Health = 100;
        BuildCost = 33;

        // Makes Queen only move sometimes so that other ants can catch up to her
        ProbMoving = 0.25f;
    }

    public override void UpdateAnt()
    {
        if (Health > 0)
        {
            if (Health >= BuildCost)
            {
                BuildNest();
            }
            else
            {
                if (RNG.NextDouble() <= ProbMoving) Move();
            }

            float DecayMultiplier = (GetBlock(block_x, block_y, block_z) is AcidicBlock) ? 2 : 1;
            Health = Mathf.Max(Health - (HealthDecayRate * DecayMultiplier), 0);
        }
        else
        {
            DepositPheromones(10);
        }
    }

    public override void Move()
    {
        List<int[]> ValidDestinations = GetValidDestinations();

        if (ValidDestinations.Count > 0)
        {

            int[] Destination = ValidDestinations[RNG.Next(0, ValidDestinations.Count)];
            if (DebugMessages) Debug.Log($"Ant {id} moved from ({block_x}, {block_y}, {block_z}) to ({Destination[0]}, {Destination[1]} {Destination[2]})");

            block_x = Destination[0]; 
            block_y = Destination[1]; 
            block_z = Destination[2];

            // UpdatePosition(block_x, block_y, block_z);
            Angle = UnityEngine.Random.Range(0f, 360f);
            UpdateNeeded = true;
        }

        // Queen deposits her own pheromone into the airblock above block she's standing on
        DepositPheromones(50);
    }

    void BuildNest()
    {
        // Move queen up one, and place nest block below her. Also need to move
        // Any ants that may be standing on this spot
        
        WorldManager.Instance.SetBlock(block_x, block_y+1, block_z, new NestBlock());

        int new_y = WorldManager.Instance.FindFirstSolidBlock(block_x, block_z);

        block_y = new_y;
        UpdateNeeded = true;
        Angle = UnityEngine.Random.Range(0f, 360f);

        List<AbstractAnt> OtherAnts = WorldManager.Instance.OtherAntsAt(id, block_x, block_y, block_z);

        Health = MathF.Max(0, Health - BuildCost);

        foreach (AbstractAnt A in OtherAnts)
        {
            A.block_y = new_y;
            A.UpdateNeeded = true;
        }

        // Queen will deposit extra pheromones if she builds a nest block this tick
        DepositPheromones(500);
        Move();
    }

    void DepositPheromones(float amount)
    {
        AbstractBlock B = GetBlock(block_x, block_y+1, block_z);
        try
        {
            AirBlock air = (AirBlock) B;
            air.AddPheromones(amount);
        }
        catch (Exception)
        {
            block_y += 1;
            UpdateNeeded = true;
            DepositPheromones(amount);
        }
    }
}
