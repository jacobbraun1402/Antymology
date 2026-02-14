using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Antymology.Terrain;
using UnityEngine;

public class Ant : AbstractAnt
{
    public float HealthGivenToQueen;

    private float HealAmount = 10;

    public int HealthFromMulch = 20;

    public DecisionModel Model;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MaxHealth = 100;

        Health = 100;

        HealthGivenToQueen = 0;
    }

    public override void UpdateAnt()
    {
        if (Health <= 0)
        {
            if (DebugMessages) Debug.Log($"Ant {id} died");
            gameObject.SetActive(false);
        }

        // Collect information about objects near the ant so that they can decide what to do next
        double[] modelInputs = GetModelInputs();
        // Debug.Log(string.Join(",", modelInputs.Select(x => x.ToString())));
        
        //
        // Model.Layers[0].Weights();

        // Debug.Log()
        double[] outputs = Model.MakeDecision(modelInputs);


        // Debug.Log(string.Join(",", outputs.Select(x => x.ToString())));
        
        // each element in outputs the probability of taking a certain action, and the sum of these probabilities is 1. 
        // These actions are: 
        // 0: Move towards Pheromones
        // 1: move randomly
        // 2: heal other ants
        // 3: dig

        double DecisionRoll = RNG.NextDouble();
        
        if (DecisionRoll <= outputs[0])
        { // Move towards pheromones
            Move();
        }

        else if (DecisionRoll <= (outputs[0] + outputs[1]))
        { // move randomly
            RandomMove();
        }

        else if (DecisionRoll <= (outputs[0] + outputs[1] + outputs[2]))
        {   // heal other ants if they have enough of their own health
            if (Health >= HealAmount) HealOthers();
            else RandomMove();
        }
        else
        {
            Dig();
        }

        float DecayMultiplier = (GetBlock(block_x, block_y, block_z) is AcidicBlock) ? 2 : 1;
        Health = Mathf.Max(Health - (HealthDecayRate * DecayMultiplier), 0);
    }

    // Pick a random block nearby and move to it
    void RandomMove()
    {
        List<int[]> ValidDestinations = GetValidDestinations();
        if (ValidDestinations.Count > 0)
        {
            int[] Destination = ValidDestinations[UnityEngine.Random.Range(0, ValidDestinations.Count)];

            block_x = Destination[0]; 
            block_y = Destination[1]; 
            block_z = Destination[2];

            // UpdatePosition(block_x, block_y, block_z);
            Angle = UnityEngine.Random.Range(0f, 360f);
            UpdateNeeded = true;
        }
    }

    // Collect information on the ant's surroundings and reformat it into something that can
    // be understood by the model
    double[] GetModelInputs()
    {
        double[] inputs = new double[10];
        Array.Fill(inputs, 0);

        // what's stored at each index:
        // 0: standing on acidic block? (1 if yes, 0 if no)
        // 1: standing on container block?
        // 2: standing on grass block?
        // 3: standing on mulch block?
        // 4: standing on nest block?
        // 5: standing on stone block?
        // 6: Other ants on same block?
        // 7: Queen on same block?
        // 8: Average Pheromone concentration of blocks surrounding ant
        // 9: current health

        AbstractBlock CurrentBlock = GetBlock(block_x, block_y, block_z);
        
        if (CurrentBlock is AcidicBlock)
        {
            inputs[0] = 1;
        }
        else if (CurrentBlock is ContainerBlock)
        {
            inputs[1] = 1;
        }
        else if (CurrentBlock is GrassBlock)
        {
            inputs[2] = 1;
        }
        else if (CurrentBlock is MulchBlock)
        {
            inputs[3] = 1;
        }
        else if (CurrentBlock is NestBlock)
        {
            inputs[4] = 1;
        }
        else if (CurrentBlock is StoneBlock)
        {
            inputs[5] = 1;
        }
        
        List<AbstractAnt> OtherAnts = WorldManager.Instance.OtherAntsAt(id, block_x, block_y, block_z);
        inputs[6] = OtherAnts.Count > 0 ? 1 : 0;

        double QueenHere = 0;
        foreach (AbstractAnt ant in OtherAnts)
        {
            if (ant is Queen Q)
            {
                QueenHere = 1;
                break;
            }
        }

        inputs[7] = QueenHere;

        Dictionary<int[],double> DestinationPheromones = GetDestinationPheromones(GetValidDestinations());
        if (DestinationPheromones.Count > 0)
        {
            inputs[8] = GetDestinationPheromones(GetValidDestinations()).Values.Average();
        }
        else inputs[8] = 0;
        
        inputs[9] = Health / MaxHealth;

        return inputs;
    }

    void HealOthers()
    {
        // // Check to see if there are other ants at same location
        List<AbstractAnt> OtherAnts = WorldManager.Instance.OtherAntsAt(id, block_x, block_y, block_z);
        // // If Ant is on same spot as the queen, then they must heal only her
        bool QueenHere = false;
        foreach (AbstractAnt A in OtherAnts)
        {
            if (A is Queen Q)
            {
                // Debug.Log($"Ant {id} healed the queen");
                HealthGivenToQueen += HealAmount;
                Q.Health = Mathf.Min(MaxHealth, Q.Health + HealAmount);
                Health = Mathf.Max(0, Health-HealAmount);
                QueenHere = true;
            }
        }
        // check if other ants are at same location as this one. If yes, try and heal those ants
        if ((OtherAnts.Count > 0) && !QueenHere)
        {
            float HealthGiven = HealAmount / OtherAnts.Count;
            foreach (AbstractAnt A in OtherAnts)
            {
                A.Health = Mathf.Min(MaxHealth, A.Health + HealthGiven);
            }
            Health = Mathf.Max(0, Health - HealAmount);
        }
    }

    // Ant will randomly select a block to move to, but is more likely to move to 
    // a destination that has higher pheromone concentrations
    public override void Move()
    {
        List<int[]> ValidDestinations = GetValidDestinations();
        if (ValidDestinations.Count > 0)
        {
            Dictionary<int[], double> DestinationPheromones = GetDestinationPheromones(ValidDestinations);

            int[] Destination = PickDestination(DestinationPheromones);
            if (DebugMessages) Debug.Log($"Ant {id} moved from ({block_x}, {block_y}, {block_z}) to ({Destination[0]}, {Destination[1]} {Destination[2]})");

            block_x = Destination[0]; 
            block_y = Destination[1]; 
            block_z = Destination[2];

            Angle = UnityEngine.Random.Range(0f, 360f);
            UpdateNeeded = true;
        }
    }

    // Find the pheromone concentrations of the air block that is above a potential destination block
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

    // Pick a random destination to move to. Blocks with a higher pheromone concentration have a higher
    // chance of being selected.
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
        }
        return destination;
    }

    // remove a block from the world, and gain health if the ant dug a mulch block
    void Dig()
    {
        AbstractBlock CurrentBlock = GetBlock(block_x, block_y, block_z);

        // Check if ant is allowed to dig up the block they are standing on
        if (ValidBlock(CurrentBlock))
        {
            if (DebugMessages) Debug.Log($"Ant {id} dug {CurrentBlock.GetType()} at ({block_x}, {block_y}, {block_z})");

            if (CurrentBlock is MulchBlock)
            {
                Health = Mathf.Min(Health + HealthFromMulch, MaxHealth);
            }
            
            // remove the block and move ant down to next solid block below the one they just removed
            WorldManager.Instance.SetBlock(block_x, block_y, block_z, new AirBlock(block_x, block_y, block_z));
            block_y = WorldManager.Instance.FindFirstSolidBlock(block_x, block_z);

            Angle = UnityEngine.Random.Range(0f, 360f);
            UpdateNeeded = true;
        }
    }

    // Checks to make sure that block ant wants to dig up is not occupied by other ants and is a valid material
    bool ValidBlock(AbstractBlock CurrentBlock)
    {
        List<AbstractAnt> OtherAnts = WorldManager.Instance.OtherAntsAt(id, block_x, block_y, block_z);
        bool ValidMaterial = !((CurrentBlock is ContainerBlock) || (CurrentBlock is AirBlock) || (CurrentBlock is NestBlock));
        return ValidMaterial && (OtherAnts.Count == 0);
    }
}
