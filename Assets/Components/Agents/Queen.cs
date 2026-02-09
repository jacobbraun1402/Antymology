using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Antymology.Terrain;



public class Queen : Ant
{
    private float BuildCost;

    void Start()
    {
        MaxHealth = 600;
        Health = 600;
        BuildCost = MaxHealth / 3;
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
                Move();
            }

            float DecayMultiplier = (GetBlock(block_x, block_y, block_z) is AcidicBlock) ? 2 : 1;
            Health -= HealthDecayRate * DecayMultiplier;
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


        //NewAnt.transform.position = new Vector3(XSpawn, YSpawn-3.9f, ZSpawn+1);
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

    }
}
