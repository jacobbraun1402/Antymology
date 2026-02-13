using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Antymology.Terrain;
using UnityEngine;

// [RequireComponent(typeof(BoxCollider))]
public class Ant : AbstractAnt
{

    // private bool showDebug = false;

    // public int block_x, block_y, block_z;
    // public float Health = 50;

    // public int id;

    // private bool DebugMessages = false;

    // public const float MaxHealth = 100;

    // private float ProbMoving;
    // private float ProbDigging;

    // Chance of an ant healing another ant if its standing on the same spot
    private float ProbHealing;

    public float HealthGivenToQueen;

    private float HealAmount = 10;

    // private System.Random RNG;

    // public float TimeBetweenTicks;

    // public int HealthDecayRate = 2;

    public int HealthFromMulch = 10;

    public DecisionModel Model;

    // public float Angle;

    // private bool UpdateNeeded;

    // public AirBlock air = new AirBlock();

    // float TimeSinceLastUpdate;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ProbMoving = .9f;
        // ProbDigging = .5f;
        ProbHealing = 0.1f;

        MaxHealth = 100;

        Health = 100;

        HealthGivenToQueen = 0;



        // RNG = new System.Random();

        // TimeBetweenTicks = WorldManager.Instance.timeBetweenTicks;
        // UpdateNeeded = false;
    }

    // Update is called once per frame
    // void Update()
    // {
    //     TimeSinceLastUpdate += Time.deltaTime;
    //     if (TimeSinceLastUpdate >= TimeBetweenTicks)
    //     {
    //         TimeSinceLastUpdate -= TimeBetweenTicks;
    //         UpdateAnt();
    //     }
    // }

    public override void UpdateAnt()
    {
        HealOthers();

        if (Health <= 0)
        {
            if (DebugMessages) Debug.Log($"Ant {id} died");
            Destroy(gameObject);
        }

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

        Health = Mathf.Max(Health - (HealthDecayRate * DecayMultiplier), 0);

        // SetYRotation(UnityEngine.Random.Range(0f, 360f));
    }

    void HealOthers()
    {
        // // Check to see if there are other ants at same location
        List<AbstractAnt> OtherAnts = WorldManager.Instance.OtherAntsAt(id, block_x, block_y, block_z);
        // // If Ant is on same spot as the queen, then they must heal
        // if (OtherAnts.Count(A => A is Queen) >= 1)
        // {
        //     Queen queen = OtherAnts.Where(A => A is Queen).Cast<Queen>().ToArray()[0];
        //     queen.Health = Mathf.Min(queen.MaxHealth, queen.Health + HealAmount);
        //     Health -= HealAmount;
        //     Debug.Log($"Ant {id} healed the queen");
        // }
        // check if other ants are at same location as this one. If yes, try and heal those ants
        if (OtherAnts.Count > 0)
        {
            double HealRoll = RNG.NextDouble();
            if (HealRoll <= ProbHealing)
            {
                float HealthGiven = HealAmount / OtherAnts.Count;
                // if (DebugMessages)
                // {
                    string Message = $"Ant {id} gave {HealthGiven} to ants " + String.Join(", ", OtherAnts.Select(A => A.id));
                    Debug.Log(Message);
                // }

                foreach (Ant A in OtherAnts.Cast<Ant>())
                {
                    A.Health = Mathf.Min(MaxHealth, A.Health + HealAmount);
                }

                Health = Mathf.Max(0, Health - HealAmount);
            }
        }
    }

    public override void Move()
    {
        List<int[]> ValidDestinations = GetValidDestinations();
        if (ValidDestinations.Count > 0)
        {
            Dictionary<int[], double> DestinationPheromones = GetDestinationPheromones(ValidDestinations);

            int[] Destination = PickDestination(DestinationPheromones);


            // int[] Destination = ValidDestinations[RNG.Next(0, ValidDestinations.Count)];
            if (DebugMessages) Debug.Log($"Ant {id} moved from ({block_x}, {block_y}, {block_z}) to ({Destination[0]}, {Destination[1]} {Destination[2]})");

            block_x = Destination[0]; 
            block_y = Destination[1]; 
            block_z = Destination[2];

            // UpdatePosition(block_x, block_y, block_z);
            Angle = UnityEngine.Random.Range(0f, 360f);
            UpdateNeeded = true;

            // Worker ants deposit a bit of pheromones whenever they move into the airblock above where they're standing
            // AirBlock B = (AirBlock) GetBlock(block_x, block_y+1, block_z);
            // B.AddPheromones('F', 1);
        }


        //NewAnt.transform.position = new Vector3(XSpawn, YSpawn-3.9f, ZSpawn+1);
    }

    public Dictionary<int[], double> GetDestinationPheromones(List<int[]> Destinations)
    {
        Dictionary<int[], double> Result = new();
        int x, y, z;
        foreach (int[] coord in Destinations)
        {
            x = coord[0];
            y = coord[1] + 1;
            z = coord[2];

            AbstractBlock block = GetBlock(x, y, z);
            if (block is AirBlock air)
            {
                Result[coord] = air.GetPheromones();
            }
        }
        return Result;
    }

    public int[] PickDestination(Dictionary<int[], double> DestinationPheromones)
    {
        List<int[]> keys = DestinationPheromones.Keys.ToList();
        int len = keys.Count;
        int[] destination = new int[3];

        if (DestinationPheromones.Values.Count(ph => ph == 0) == len)
        {
            return keys[RNG.Next(0, len)];
        }
        else
        {
            // Dictionary<int[], double> probabilities = new();
            double total = DestinationPheromones.Values.Sum();
            double roll = UnityEngine.Random.Range(0f, 1f);
            double sum = 0;
            foreach (int[] key in keys)
            {
                double probability = DestinationPheromones[key] / total;
                if (roll < probability + sum)
                {
                    destination = key;
                    break;
                }
                sum += probability;
            }

            
            // foreach (int[] key in keys)
            // {
            //     if (roll <= probabilities[key] + sum)
            //     {
            //         destination = key;
            //         break;
            //     }
            //     sum += probabilities[key];
            // }
        }
        return destination;
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

    // List<int[]> GetValidDestinations()
    // {
    //     // AbstractBlock[,,] neighbours = new AbstractBlock[3,5,3];
    //     List<int[]> Result = new();
    //     for (int i = -1; i <= 1; i++)
    //     {
    //         for (int j = -2; j <= 2; j++)
    //         {
    //             for (int k = -1; k <= 1; k++)
    //             {
    //                 // Set all blocks directly above, below, and diagonal to ant as invalid
    //                 // if ((i == 0 && k == 0) || (Math.Abs(i) == 1 && Math.Abs(k) == 1))
    //                 // {
    //                 //     continue;
    //                 // }
    //                 // Only check if Ant can move forwards, backwards, left or right
    //                 // When I had it so that ants could move diagonally, their movement patterns were kind of wonky
    //                 if ((i == 0) ^ (k == 0))
    //                 {
    //                     // world coordinates of block to query
    //                     int xcord = block_x + i;
    //                     int ycord = block_y + j;
    //                     int zcord = block_z + k;
    //                     AbstractBlock Block = GetBlock(xcord, ycord, zcord);
    //                     // neighbours[1+i, 2+j, 1+k] = Block;

    //                     // If block is solid and block above it is air, then ant could move to it
    //                     if ((Block is not AirBlock) && (GetBlock(xcord, ycord + 1, zcord) is AirBlock))
    //                     {
    //                         Result.Add(new int[] {xcord, ycord, zcord});
    //                     }
    //                 }
    //             }
    //         }
    //     }
    //     return Result;
    // }

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
        // double PheromoneAmount = 1;
        // bool ValidBlock = !((CurrentBlock is ContainerBlock) || (CurrentBlock is AirBlock) || (CurrentBlock is NestBlock));
        if (ValidBlock(CurrentBlock))
        {
            if (DebugMessages) Debug.Log($"Ant {id} dug {CurrentBlock.GetType()} at ({block_x}, {block_y}, {block_z})");

            if (CurrentBlock is MulchBlock)
            {
                Health = Mathf.Min(Health + 10, MaxHealth);
                // Ants deposit extra pheromone after they find food
                // PheromoneAmount = 10;
            }
            
            WorldManager.Instance.SetBlock(block_x, block_y, block_z, new AirBlock(block_x, block_y, block_z));
            block_y = WorldManager.Instance.FindFirstSolidBlock(block_x, block_z);

            Angle = UnityEngine.Random.Range(0f, 360f);
            UpdateNeeded = true;


            // AirBlock B = (AirBlock) GetBlock(block_x, block_y+1, block_z);
            // B.AddPheromones('Q', PheromoneAmount);
        }
    }

    bool ValidBlock(AbstractBlock CurrentBlock)
    {
        List<AbstractAnt> OtherAnts = WorldManager.Instance.OtherAntsAt(id, block_x, block_y, block_z);
        bool ValidMaterial = !((CurrentBlock is ContainerBlock) || (CurrentBlock is AirBlock) || (CurrentBlock is NestBlock));
        return ValidMaterial && (OtherAnts.Count == 0);
    }

    // // Have this just because I'm too lazy to type out WorldManager.Instance everytime I want to get a block lol
    // AbstractBlock GetBlock(int x, int y, int z)
    // {
    //     return WorldManager.Instance.GetBlock(x, y, z);
    // }

    // void UpdatePosition(int x,int y,int z)
    // {
    //     // transform.position = new Vector3(x, y - 3.9f, z + 1);
    //     transform.position = new Vector3(x, y-1.2f, z);
    // }

    // void LateUpdate()
    // {
    //     if (UpdateNeeded)
    //     {
    //         UpdatePosition(block_x, block_y, block_z);
    //         SetYRotation(Angle);
    //         UpdateNeeded = false;
    //     }

    // }

    // void SetYRotation(float angle)
    // {
    //     BoxCollider collider = GetComponent<BoxCollider>();
    //     Vector3 center = transform.TransformPoint(collider.center);
    //     transform.RotateAround(center, Vector3.up, angle);
    // }
}
