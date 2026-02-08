using System;
using System.Collections.Generic;
using System.Globalization;
using Antymology.Terrain;
using UnityEngine;

public class Ant : MonoBehaviour
{

    // private bool showDebug = false;

    public int block_x, block_y, block_z;
    public int Health = 50;

    public int id;

    public const int MaxHealth = 100;

    private float ProbMoving;
    private float ProbDigging;

    private System.Random RNG;

    public float TimeBetweenTicks;

    public int HealthDecayRate = 2;

    public int HealthFromMulch = 10;

    public AirBlock air = new AirBlock();

    float TimeSinceLastUpdate;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ProbMoving = .5f;
        ProbDigging = .5f;

        RNG = new System.Random();

        TimeBetweenTicks = WorldManager.Instance.timeBetweenTicks;
    }

    // Update is called once per frame
    void Update()
    {
        TimeSinceLastUpdate += Time.deltaTime;
        if (TimeSinceLastUpdate >= TimeBetweenTicks)
        {
            TimeSinceLastUpdate -= TimeBetweenTicks;
            UpdateAnt();
        }
    }

    void UpdateAnt()
    {
        double MoveRoll = RNG.NextDouble();
        if (MoveRoll <= ProbMoving)
        {
            Move();
        }
        else
        {
            Dig();
        }

        Health -= HealthDecayRate;

        if (Health <= 0)
        {
            Debug.Log($"Ant {id} died");
            Destroy(gameObject);
        }
    }

    void Move()
    {
        List<int[]> ValidDestinations = GetValidDestinations();

        if (ValidDestinations.Count > 0)
        {
            int[] Destination = ValidDestinations[RNG.Next(0, ValidDestinations.Count)];
            // Debug.Log($"Ant {id} moved from ({block_x}, {block_y}, {block_z}) to ({Destination[0]}, {Destination[1]} {Destination[2]})");

            block_x = Destination[0]; 
            block_y = Destination[1]; 
            block_z = Destination[2];

            UpdatePosition(block_x, block_y, block_z);
        }


        //NewAnt.transform.position = new Vector3(XSpawn, YSpawn-3.9f, ZSpawn+1);
    }

    List<int[]> GetValidDestinations()
    {
        AbstractBlock[,,] neighbours = new AbstractBlock[3,5,3];
        List<int[]> Result = new();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -2; j <= 2; j++)
            {
                for (int k = -1; k <= 1; k++)
                {
                    // don't set Blocks directly above or below ant as valid
                    if (i == 0 && k == 0)
                    {
                        continue;
                    }
                    // world coordinates of block to query
                    int xcord = block_x + i;
                    int ycord = block_y + j;
                    int zcord = block_z + k;
                    AbstractBlock Block = GetBlock(xcord, ycord, zcord);
                    neighbours[1+i, 2+j, 1+k] = Block;

                    // If block is solid and block above it is air, then ant could move to it
                    if ((Block is not AirBlock) && (GetBlock(xcord, ycord + 1, zcord) is AirBlock))
                    {
                        Result.Add(new int[] {xcord, ycord, zcord});
                    }
                }
            }
        }
        return Result;
    }

    // AbstractBlock[,,] GetNeighbours()
    // {
    //     // Contains blocks surrounding block ant is standing on, up to two blocks above and two blocks below
    //     // with a radius of 1
    //     // block ant is standing on will be element [1,2,1] in the array
    //     AbstractBlock[,,] neighbours = new AbstractBlock[3,5,3];

    //     for (int i = -1; i <= 1; i++)
    //     {
    //         for (int j = -2; j <= 2; j++)
    //         {
    //             for (int k = -1; k < 1; k++)
    //             {
    //                 AbstractBlock Block = WorldManager.Instance.GetBlock(block_x + i, block_y + j, block_z + k);
    //                 neighbours[1+i, 2+j, 1+k] = Block;
    //             }
    //         }
    //     }
    //     return neighbours;
    // }

    void Dig()
    {
        AbstractBlock CurrentBlock = GetBlock(block_x, block_y, block_z);
        if ((CurrentBlock is not ContainerBlock) && (CurrentBlock is not AirBlock) && (WorldManager.Instance.OtherAntsAt(id, block_x, block_y, block_z).Count == 0))
        {
            Debug.Log($"Ant {id} dug {CurrentBlock.GetType()} at ({block_x}, {block_y}, {block_z})");

            if (CurrentBlock is MulchBlock)
            {
                Health = Math.Max(Health + 10, MaxHealth);
            }
            
            WorldManager.Instance.SetBlock(block_x, block_y, block_z, air);
            block_y -= 1;

            UpdatePosition(block_x, block_y, block_z);
        }
    }

    // Have this just because I'm too lazy to type out WorldManager.Instance everytime I want to get a block lol
    AbstractBlock GetBlock(int x, int y, int z)
    {
        return WorldManager.Instance.GetBlock(x, y, z);
    }

    void UpdatePosition(int x,int y,int z)
    {
        transform.position = new Vector3(x, y - 3.9f, z + 1);
    }

    void LateUpdate()
    {
      UpdatePosition(block_x, block_y, block_z);  
    }
}
