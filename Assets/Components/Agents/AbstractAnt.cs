using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Antymology.Terrain;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public abstract class AbstractAnt : MonoBehaviour
{
    public int block_x, block_y, block_z;

    public float Health;

    public int id;

    protected bool DebugMessages;

    protected float MaxHealth;

    protected float ProbMoving;

    protected System.Random RNG;

    public int HealthDecayRate = 2;

    public bool UpdateNeeded;

    protected float TimeBetweenTicks;
    protected float TimeSinceLastUpdate;

    protected float Angle;

    public abstract void UpdateAnt();
    public abstract void Move();


    void Awake()
    {
        RNG = new System.Random();

        TimeBetweenTicks = WorldManager.Instance.timeBetweenTicks;
        TimeSinceLastUpdate = 0;

        UpdateNeeded = false;
    }

    void Update()
    {
        TimeSinceLastUpdate += Time.deltaTime;
        if (TimeSinceLastUpdate >= TimeBetweenTicks)
        {
            TimeSinceLastUpdate -= TimeBetweenTicks;
            UpdateAnt();
        } 
    }

    // Look for valid destinations that an ant can move to
    protected List<int[]> GetValidDestinations()
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

    // Have this just because I'm too lazy to type out WorldManager.Instance everytime I want to get a block lol
    protected AbstractBlock GetBlock(int x, int y, int z)
    {
        return WorldManager.Instance.GetBlock(x, y, z);
    }

    protected void UpdatePosition(int x,int y,int z)
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

    protected void SetYRotation(float angle)
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        Vector3 center = transform.TransformPoint(collider.center);
        transform.RotateAround(center, Vector3.up, angle);
    }
}