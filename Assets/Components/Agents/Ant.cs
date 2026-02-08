using System;
using System.Globalization;
using Antymology.Terrain;
using UnityEngine;

public class Ant : MonoBehaviour
{

    public int block_x, block_y, block_z;
    public float health = 100;

    private float ProbMoving;
    private float ProbDigging;

    private System.Random RNG;

    public float TimeBetweenTicks;

    public float HealthDecayRate = 1;

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
    }

    void Move()
    {
        
    }

    AbstractBlock[,,] GetNeighbours()
    {
        // Contains blocks surrounding block ant is standing on, up to two blocks above and two blocks below
        // with a radius of 1
        // block ant is standing on will be element [1,2,1] in the array
        AbstractBlock[,,] neighbours = new AbstractBlock[3,5,3];

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -2; j <= 2; j++)
            {
                for (int k = -1; k < 1; k++)
                {
                    AbstractBlock Block = WorldManager.Instance.GetBlock(block_x + i, block_y + j, block_z + k);
                    neighbours[1+i, 2+j, 1+k] = Block;
                }
            }
        }
        return neighbours;
    }

    void Dig()
    {
        
    }
}
