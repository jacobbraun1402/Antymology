using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Antymology.Terrain;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Ant : MonoBehaviour
{

    // private bool showDebug = false;

    public int block_x, block_y, block_z;
    public float Health = 50;

    public int id;

    private bool DebugMessages = false;

    public const float MaxHealth = 100;

    private float ProbMoving;
    private float ProbDigging;

    // Chance of an ant healing another ant if its standing on the same spot
    private float ProbHealing;

    private float HealAmount = 20;

    private System.Random RNG;

    public float TimeBetweenTicks;

    public int HealthDecayRate = 2;

    public int HealthFromMulch = 10;

    public float Angle;

    private bool UpdateNeeded;

    // public AirBlock air = new AirBlock();

    float TimeSinceLastUpdate;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ProbMoving = .5f;
        ProbDigging = .5f;
        ProbHealing = 0.1f;

        RNG = new System.Random();

        TimeBetweenTicks = WorldManager.Instance.timeBetweenTicks;
        UpdateNeeded = false;
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
        HealOthers();

        double MoveRoll = RNG.NextDouble();

        if (MoveRoll <= ProbMoving)
        {
            Move();
        }
        else
        {
            Dig();
        }

        float DecayMultiplier = (GetBlock(block_x, block_y, block_z) is AcidicBlock) ? 2 : 1;

        Health -= HealthDecayRate * DecayMultiplier;

        if (Health <= 0)
        {
            if (DebugMessages) Debug.Log($"Ant {id} died");
            Destroy(gameObject);
        }

        // SetYRotation(UnityEngine.Random.Range(0f, 360f));
    }

    void HealOthers()
    {
        // check if other ants are at same location as this one. If yes, try and heal those ants
        List<Ant> OtherAnts = WorldManager.Instance.OtherAntsAt(id, block_x, block_y, block_z);
        if (OtherAnts.Count > 0)
        {
            double HealRoll = RNG.NextDouble();
            if (HealRoll <= ProbHealing)
            {
                float HealthGiven = HealAmount / OtherAnts.Count;
                if (DebugMessages)
                {
                    string Message = $"Ant {id} gave {HealthGiven} to ants " + String.Join(", ", OtherAnts.Select(A => A.id));
                    Debug.Log(Message);
                }

                foreach (Ant A in OtherAnts)
                {
                    A.Health += HealAmount;
                }

                Health -= HealAmount;
            }
        }
    }

    void Move()
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

    // If ant is surrounded by columns of blocks that are two or more blocks higher than the ant in all 4 directions,
    // Ant can't dig. This should help to prevent ants from digging themselves into holes they can't get out out
    bool CheckIfCanDig()
    {
        AbstractBlock[,,] neighbours = GetNeighbouringBlocks();

        AbstractBlock[] LeftColumns = new AbstractBlock[] {neighbours[0,3,1], neighbours[0,4,1]};
        AbstractBlock[] RightColumns = new AbstractBlock[] {neighbours[2, 3, 1], neighbours[2, 4, 1]};
        AbstractBlock[] BackwardColumns = new AbstractBlock[] {neighbours[1,3,0], neighbours[1,4,0]};
        AbstractBlock[] ForwardColumns = new AbstractBlock[] {neighbours[1,3,2], neighbours[1,4,2]};

        AbstractBlock[] AllColumnBlocks = (new AbstractBlock[] {neighbours[0, 3, 1], neighbours[0, 4, 1]})
        .Concat(new AbstractBlock[] {neighbours[2, 3, 1], neighbours[2, 4, 1]})
        .Concat(new AbstractBlock[] {neighbours[1, 3, 0], neighbours[1, 4, 0]})
        .Concat(new AbstractBlock[] {neighbours[1, 3, 2], neighbours[1, 4, 2]}).ToArray();

        // if (AllColumns.Sum(a => a is not AirBlock ? 1 : 0) == 8)
        // {
        //     return false;
        // }

        // return true;


        return AllColumnBlocks.Sum(a => a is not AirBlock ? 1 : 0) != 8;
    }

    AbstractBlock[,,] GetNeighbouringBlocks()
    {
        AbstractBlock[,,] neighbours = new AbstractBlock[3,5,3];
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -2; j <= 2; j++)
            {
                for (int k = -1; k <= 1; k++)
                {
                    if ((i == 0) ^ (k == 0))
                    {
                        // world coordinates of block to query
                        int xcord = block_x + i;
                        int ycord = block_y + j;
                        int zcord = block_z + k;
                        AbstractBlock Block = GetBlock(xcord, ycord, zcord);
                        neighbours[1+i, 2+j, 1+k] = Block;
                    }
                }
            }
        }
        return neighbours;
    }

    List<int[]> GetValidDestinations()
    {
        // AbstractBlock[,,] neighbours = new AbstractBlock[3,5,3];
        List<int[]> Result = new();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -2; j <= 2; j++)
            {
                for (int k = -1; k <= 1; k++)
                {
                    // Set all blocks directly above, below, and diagonal to ant as invalid
                    // if ((i == 0 && k == 0) || (Math.Abs(i) == 1 && Math.Abs(k) == 1))
                    // {
                    //     continue;
                    // }
                    // Only check if Ant can move forwards, backwards, left or right
                    // When I had it so that ants could move diagonally, their movement patterns were kind of wonky
                    if ((i == 0) ^ (k == 0))
                    {
                        // world coordinates of block to query
                        int xcord = block_x + i;
                        int ycord = block_y + j;
                        int zcord = block_z + k;
                        AbstractBlock Block = GetBlock(xcord, ycord, zcord);
                        // neighbours[1+i, 2+j, 1+k] = Block;

                        // If block is solid and block above it is air, then ant could move to it
                        if ((Block is not AirBlock) && (GetBlock(xcord, ycord + 1, zcord) is AirBlock))
                        {
                            Result.Add(new int[] {xcord, ycord, zcord});
                        }
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
            if (DebugMessages) Debug.Log($"Ant {id} dug {CurrentBlock.GetType()} at ({block_x}, {block_y}, {block_z})");

            if (CurrentBlock is MulchBlock)
            {
                Health = Math.Max(Health + 10, MaxHealth);
            }
            
            WorldManager.Instance.SetBlock(block_x, block_y, block_z, new AirBlock());
            block_y = WorldManager.Instance.FindFirstSolidBlock(block_x, block_z);

            Angle = UnityEngine.Random.Range(0f, 360f);
            UpdateNeeded = true;
        }
    }

    // Have this just because I'm too lazy to type out WorldManager.Instance everytime I want to get a block lol
    AbstractBlock GetBlock(int x, int y, int z)
    {
        return WorldManager.Instance.GetBlock(x, y, z);
    }

    void UpdatePosition(int x,int y,int z)
    {
        // transform.position = new Vector3(x, y - 3.9f, z + 1);
        transform.position = new Vector3(x, y-1.2f, z);
    }

    void LateUpdate()
    {
        if (UpdateNeeded)
        {
            UpdatePosition(block_x, block_y, block_z);
            SetYRotation(Angle);
            UpdateNeeded = false;
        }

    }

    void SetYRotation(float angle)
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        Vector3 center = transform.TransformPoint(collider.center);
        transform.RotateAround(center, Vector3.up, angle);
    }
}
