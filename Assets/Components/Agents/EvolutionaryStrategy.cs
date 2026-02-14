using System;
using System.Linq;
using System.Collections.Generic;
using Antymology.Terrain;
using UnityEngine;


// Implements a (mu/2 + lambda) kind of strategy where half of the 
// population is selected to make up the next generation, as well as reproduce.
// 'Genes' will be the parameters of the model the ant uses to make decisions
public static class EvolutionaryStrategy
{
    const float MutationRate = 0.1f;
    const double Beta = 1.3f;

    static System.Random RNG = new System.Random();

    // Main function that will be called to evaluate the performance of each ant and create the next generation of decision models
    public static List<List<double[,]>> MakeNextGen(AbstractAnt[] Ants)
    {
        List<Ant> LotteryPool = GetLotteryPool(Ants);
        List<Ant> SelectedAnts = SelectAnts(LotteryPool);
        // Add in the genes from the selected ants
        List<List<double[,]>> NewModelsWeights = SelectedAnts.Select(A => A.Model.Layers.Select(L => L.Weights).ToList()).ToList();
        // Selected ants will also mate with each other
        List<Tuple<Ant,Ant>> Pairs = GetMates(SelectedAnts);
        NewModelsWeights.AddRange(BreedAllAnts(Pairs));
        
        return NewModelsWeights;
    }

    // At end of generation, Get the ants that will be used to populate the next generation. The fitness of each ant will just be
    // the total amount they healed the queen for throughout the evaluation phase.
    // Ants will be selected using a lottery kind of idea. Each ant will get to add one "ticket" to the pool, no matter how well
    // they did. For every 10 health an ant gives to the queen, they get to add that many extra tickets. So if an ant gave 50 health,
    // that ant will get 5 extra tickets.
    public static List<Ant> GetLotteryPool(AbstractAnt[] Ants)
    {
        List<Ant> Result = new();
        for (int i = 0; i < WorldManager.Instance.NumAnts; i++)
        {
            Ant A = (Ant) Ants[i];
            Result.Add(A);

            int NumToAdd = Mathf.FloorToInt(A.HealthGivenToQueen / 10);

            for (int j = 0; j < NumToAdd; j++)
            {
                Result.Add(A);
            }
        }
        return Result;
    }

    // Get the ants that will make up the next generation
    public static List<Ant> SelectAnts(List<Ant> pool)
    {
        List<Ant> SelectedAnts = new();
        int NumToSelect = WorldManager.Instance.NumAnts / 2;

        for (int i = 0; i < NumToSelect; i++)
        {
            Ant selected = pool[UnityEngine.Random.Range(0, pool.Count)];
            SelectedAnts.Add(selected);
            pool.RemoveAll(A => A.id == selected.id);
        }
        return SelectedAnts;
    }

    // Pair up each of the ants in the mating pool
    public static List<Tuple<Ant,Ant>> GetMates(List<Ant> SelectedAnts)
    {
        List<Tuple<Ant,Ant>> Pairs = new();
        while(SelectedAnts.Count > 0)
        {
            Ant Ant1 = SelectedAnts[UnityEngine.Random.Range(0, SelectedAnts.Count)];
            SelectedAnts.Remove(Ant1);
            Ant Ant2 = SelectedAnts[UnityEngine.Random.Range(0, SelectedAnts.Count)];
            SelectedAnts.Remove(Ant2);
            Pairs.Add(new Tuple<Ant, Ant>(Ant1, Ant2));
        }
        return Pairs;
    }

    // Create all of the new ants for the next generation
    public static List<List<double[,]>> BreedAllAnts(List<Tuple<Ant,Ant>> Pairs)
    {
        List<List<double[,]>> ModelWeights = new();
        foreach (Tuple<Ant,Ant> Pair in Pairs)
        {
            List<double[,]> Child1, Child2;
            Child1 = Breed(Pair);
            Child2 = Breed(Pair);
            ModelWeights.Add(Child1);
            ModelWeights.Add(Child2);
        }
        return ModelWeights;
    }

    // Recombine and mutate the model parameters of two parent ants
    public static List<double[,]> Breed(Tuple<Ant, Ant> Pair)
    {
        List<double[,]> NewWeights = new();

        Ant Ant1 = Pair.Item1;
        Ant Ant2 = Pair.Item2;

        DecisionModel Ant1Model = Ant1.Model;
        DecisionModel Ant2Model = Ant2.Model;

        double[,] Ant1Layer1 = Ant1Model.Layers[0].Weights;
        double[,] Ant2Layer1 = Ant2Model.Layers[0].Weights;

        double[,] Layer1Weights = UpdateLayerWeights(Ant1Layer1, Ant2Layer1);

        NewWeights.Add(Layer1Weights);
        
        double[,] Ant1Layer2 = Ant1Model.Layers[1].Weights;
        double[,] Ant2Layer2 = Ant2Model.Layers[1].Weights;

        double[,] Layer2Weights = UpdateLayerWeights(Ant1Layer2, Ant2Layer2);
        NewWeights.Add(Layer2Weights);

        return NewWeights;
    }

    public static double[,] UpdateLayerWeights(double[,] Ant1Layer, double[,] Ant2Layer)
    {
        double[,] NewWeights = new double[Ant1Layer.GetLength(0),Ant1Layer.GetLength(1)];

        for (int i = 0; i < NewWeights.GetLength(0); i++)
        {
            // Do discrete recombination and then mutate using Rechenburg heuristic
            for (int j = 0; j < NewWeights.GetLength(1); j++)
            {
                double SelectedGene = RNG.NextDouble() <= 0.5 ? Ant1Layer[i,j] : Ant2Layer[i,j];
                NewWeights[i,j] = Mutate(SelectedGene);
            }
        }
        return NewWeights;
    }

    public static double Mutate(double OriginalValue)
    {
        double MutationRoll = RNG.NextDouble();
        if (MutationRoll <= MutationRate)
        {
            return RNG.NextDouble() < 0.5 ? Beta * OriginalValue : 1/Beta * OriginalValue;
        }
        return OriginalValue;
    }
}