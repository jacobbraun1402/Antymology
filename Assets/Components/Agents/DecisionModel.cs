using System;
using System.Linq;
using System.Collections.Generic;

// Features:
// Current Block they are standing on
// If other ants are on the same spot
// If queen on same spot
// Average strength of the two blocks with the highest pheromone concentrations
// Current health


// Possible actions:
// Move towards Queen Pheromones
// Move away from pheromones
// Heal other ants
// Dig

// struct ModelInputs(double[] CurrentBlock, double NumAnts, double QueenHere, double QueenPheromone, double FoodPheromone, double Health)
// {
//     double[] CurrentBlock = CurrentBlock;
//     double NumAnts = NumAnts;
//     double QueenHere = QueenHere;

//     double QueenPheromone = QueenPheromone;
//     double FoodPheromone = FoodPheromone;
//     double Health = Health;
// }
public class Layer
{
    public int Units;

    public int InputSize;
    public double[] Values;

    public double[,] Weights; // Rows: Units in layer, Columns: Input values

    // double[,] Output;

    public Func<double, double> Activation;

    public Layer(int Units, int InputSize, Func<double, double> Activation)
    {
        Weights = new double[Units, InputSize];
        for (int i = 0; i < Weights.GetLength(0); i++)
        {
            for (int j = 0; j < Weights.GetLength(1); j++)
            {
                Weights[i,j] = 1;
            }
        }
        // foreach (double i in Weights)
        // {
        //     Console.WriteLine(i);
        // }
        Values = new double[InputSize];
        this.Units = Units;
        this.InputSize = InputSize;
        this.Activation = Activation;
    }

    public void SetWeights(double[,] vals)
    {
        Weights = vals;
    }

    public void SetValues(double[] vals)
    {
        Values = vals;
    }

    public double[] Evaluate()
    {
        double[] OutputArray = new double[Units];
        for (int i = 0; i < Units; i++)
        {
            double sum = 0;
            for (int j = 0; j < InputSize; j++)
            {
                sum += Values[j] * Weights[i,j];
            }
            OutputArray[i] = Activation.Invoke(sum);
        }
        return OutputArray;
    }

    // double ReLU(double x)
    // {
    //     return Math.Max(0, x);
    // }
}

public static class Activations
{
    public static double ReLU(double x)
    {
        return Math.Max(0, x);
    }

    public static double Linear(double x)
    {
        return x;
    }

    public static double[] SoftMax(double[] Logits)
    {
        double denom = Logits.ToList().Sum(Math.Exp);

        return Logits.ToList().Select(x => Math.Exp(x)/denom).ToArray();
    }
}

public class DecisionModel
{
    // double x = Activations.ReLU(0.5);
    // List<double[]> Weights;

    // int NumLayers;

    public int InputSize;
    public int OutputSize;
    public List<Layer> Layers;

    public DecisionModel(int InputSize, int OutputSize)
    {
        this.OutputSize = OutputSize;
        this.InputSize = InputSize;
        Layers = new List<Layer>();
    }

    public void AddLayer(int Units, Func<double, double> Activation, double[,] weights)
    {
        Layer l = new(Units, Layers.Last().Units, Activation);
        l.SetWeights(weights);
        Layers.Add(l);
    }

    public void AddFirstLayer(int Units, Func<double, double> Activation, double[,] weights)
    {
        Layer l = new(Units, InputSize, Activation);
        l.SetWeights(weights);
        Layers.Add(l);
    }

    public double[] MakeDecision(double[] Inputs)
    {
        double[] Outputs = Inputs.ToArray();

        foreach (Layer l in Layers)
        {
            double[] LayerInputs = Outputs.ToArray();
            l.SetValues(LayerInputs);
            Outputs = l.Evaluate();
            foreach (double x in Outputs) Console.WriteLine(x);
        }

        return Activations.SoftMax(Outputs);
    }
}


    // static int Main()
    // {
        
    //     double[] inputs = [1, 2, 3, 4, 5];
    //     int units = 5;

    //     double[,] weights = new double[units, inputs.Length];

    //     for (int i = 0; i < units; i++)
    //     {
    //         for (int j = 0; j < inputs.Length; j++)
    //         {
    //             weights[i, j] = new Random().NextDouble();
    //         }
    //     }

    //     Layer test = new Layer(units, inputs.Length, Activations.ReLU);
    //     test.SetValues(inputs);
    //     test.SetWeights(weights);


    //     Layer next = new Layer(units, inputs.Length, Activations.ReLU);



    //     double[] output = test.Evaluate();
    //     double[] probabilities = Activations.SoftMax(output);

    //     foreach (var x in probabilities)
    //     {
    //         Console.WriteLine($"{x} ");
    //     }

    //     Console.Write($"{probabilities.Sum()}");

        
    //     return 0;
    // }
// }


// class TestClass
// {
//     static int Main()
//     {
//         double[] inputs = new double[11];
//         Array.Fill(inputs, 1);

//         DecisionModel Model = new DecisionModel(11, 6);
//         Random rng = new Random(21);


//         double[,] FirstLayerWeights = new double[16,11];
//         for (int i = 0; i < FirstLayerWeights.GetLength(0); i++)
//         {
//             for (int j = 0; j < FirstLayerWeights.GetLength(1); j++)
//             {
//                 FirstLayerWeights[i,j] = rng.NextDouble();
//             }
//         }

//         Model.AddFirstLayer(16, Activations.ReLU, FirstLayerWeights);
        
//         double[,] secondlayerweights = new double[6,16];
//         for (int i = 0; i < secondlayerweights.GetLength(0); i++)
//         {
//             for (int j = 0; j < secondlayerweights.GetLength(1); j++)
//             {
//                 secondlayerweights[i,j] = rng.NextDouble();
//             }
//         }

//         Model.AddLayer(6, Activations.Linear, secondlayerweights);

//         double[] output = Model.MakeDecision(inputs);

//         foreach (var x in output)
//         {
//             Console.WriteLine(x);
//         }


//         return 0;
//     }
// }