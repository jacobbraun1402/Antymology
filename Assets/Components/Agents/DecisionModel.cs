using System;
using System.Linq;
using System.Collections.Generic;
// using System.Diagnostics;
using UnityEngine;

// Neural network-type model that the ant uses to decide on their next action
// In the final model I used in the simulation, there was 1 hidden layer
// The Softmax function is applied to the outputs so that the actions an 
// ant could take are represented as a probability distribution

// Features:
// Current Block they are standing on
// If other ants are on the same spot
// If queen on same spot
// amount of pheromones on block with highest pheromone concentration
// Current health


// Possible actions:
// Move towards Pheromones
// Move away from pheromones
// Heal other ants
// Dig
public class Layer
{
    // number of neurons in this layer
    public int Units;

    // the number of input variables
    public int InputSize;

    // the values of the input variables
    public double[] Values;

    // The weight or "slope" parameters for each variable and neuron.
    public double[,] Weights; // Rows: Units in layer, Columns: Input values

    // double[,] Output;

    // Activation function applied to output of layer, the function I used (ReLU) can help with learning more complex relationships
    // between input variables
    public Func<double, double> Activation;

    public Layer(int Units, int InputSize, Func<double, double> Activation)
    {   
        // Initialize all weights to 1
        Weights = new double[Units, InputSize];
        for (int i = 0; i < Weights.GetLength(0); i++)
        {
            for (int j = 0; j < Weights.GetLength(1); j++)
            {
                Weights[i,j] = 1;
            }
        }

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

    // For each neuron, sum up the variable values multiplied by its associated weight, then apply the activation function
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
}

// Helper class that contains the activation functions
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
        double denom = Logits.ToList().Sum(x => Math.Exp(x));
        return Logits.ToList().Select(x => Math.Exp(x)/denom).ToArray();
    }
}

// Collects layers and combines them into the final model the ant uses
public class DecisionModel
{
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

// Add the layer that contains the input values to the model
    public void AddFirstLayer(int Units, Func<double, double> Activation, double[,] weights)
    {
        Layer l = new(Units, InputSize, Activation);
        l.SetWeights(weights);
        Layers.Add(l);
    }

    // Feed in input values, and return the decision probabilities
    public double[] MakeDecision(double[] Inputs)
    {
        double[] Outputs = Inputs.ToArray();

        foreach (Layer l in Layers)
        {
            double[] LayerInputs = Outputs.ToArray();
            l.SetValues(LayerInputs);
            Outputs = l.Evaluate();
            // foreach (double x in Outputs) Debug.Log(x);
        }

        double[] probs = Activations.SoftMax(Outputs);
        // foreach (double x in probs) Debug.Log(x);
        return probs;
    }
}