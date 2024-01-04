using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;

public class NeuralNetwork
{
    private int[] layers;
    private string output = "";
    private int answer = 0;
    private int samples = 0;
    private readonly int batchSize = 1;
    private readonly double learnRate = 1d;
    private double[][] neurons;
    private double[][] activation_z_values;
    private double[][] biases;
    private double[][] biasDerivative;
    private double[][][] weights;
    private double[][][] weightDerivative;
    private int correct = 0;
    private int total = 0;

    #region Printing Info

    public string NeuronInfo(int i, int j)
    {
        string message = "";
        message += "Neuron " + j + ": " + neurons[i][j];
        //if (i != 0)
        //{
        //    message += "\nBias: " + biases[i][j] + "\nWeights:";
        //    for (int k = 0; k < weights[i - 1][j].Length; k++)
        //    {
        //        message += "\n" + k + " : " + weights[i - 1][j][k];
        //    }
        //}

        return message;
    }

    #endregion

    #region Init

    public NeuralNetwork(int[] layers, int batchSize, double learnRate)
    {
        this.layers = new int[layers.Length];
        layers.CopyTo(this.layers, 0);
        this.batchSize = batchSize;
        this.learnRate = learnRate;

        CreateNewNetwork();
    }

    private void CreateNewNetwork()
    {
        InitNeurons();
        InitBiases();
        InitWeights();
    }

    private void InitNeurons()
    {
        List<double[]> neuronList = new List<double[]>();
        List<double[]> activationList = new List<double[]>();
        for (int i = 0; i < layers.Length; i++)
        {
            neuronList.Add(new double[layers[i]]);
            activationList.Add(new double[layers[i]]);
        }

        neurons = neuronList.ToArray();
        activation_z_values = activationList.ToArray();
    }

    private void InitBiases()
    {
        List<double[]> biasList = new List<double[]>();
        List<double[]> biasDerivativeList = new List<double[]>();
        for (int i = 0; i < layers.Length; i++)
        {
            double[] bias = new double[layers[i]];
            for (int j = 0; j < layers[i]; j++)
            {
                bias[j] = Random.Range(-.5f, .5f);
            }

            biasList.Add(bias);
            biasDerivativeList.Add(new double[layers[i]]);
        }

        biases = biasList.ToArray();
        biasDerivative = biasDerivativeList.ToArray();
    }

    private void InitWeights()
    {
        List<double[][]> weightList = new List<double[][]>();
        List<double[][]> weightDerivativeList = new List<double[][]>();
        for (int i = 1; i < layers.Length; i++)
        {
            List<double[]> layerWeightList = new List<double[]>();
            List<double[]> layerWeightDerivativeList = new List<double[]>();
            int neuronsInPreviousLayer = layers[i - 1];
            for (int j = 0; j < neurons[i].Length; j++)
            {
                double[] neuronWeights = new double[neuronsInPreviousLayer];
                for (int k = 0; k < neuronsInPreviousLayer; k++)
                {
                    neuronWeights[k] = Random.Range(-.5f, .5f);
                }

                layerWeightList.Add(neuronWeights);
                layerWeightDerivativeList.Add(new double[neuronsInPreviousLayer]);
            }

            weightList.Add(layerWeightList.ToArray());
            weightDerivativeList.Add(layerWeightDerivativeList.ToArray());
        }

        weights = weightList.ToArray();
        weightDerivative = weightDerivativeList.ToArray();
    }

    #endregion

    #region Useful Functions

    public bool SaveNetwork()
    {
        try
        {
            using StreamWriter sw = new StreamWriter(Application.persistentDataPath + "/SavedNetwork");
            for (int i = 0; i < neurons.Length; i++)
            {
                sw.Write(neurons[i].Length + " ");
            }
            sw.WriteLine();

            for (int i = 1; i < neurons.Length; i++)
            {
                for (int j = 0; j < neurons[i].Length; j++)
                {
                    for (int k = 0; k < neurons[i - 1].Length; k++)
                    {
                        sw.Write(weights[i - 1][j][k] + " ");
                    }

                    sw.WriteLine(biases[i][j]);
                }
            }
        }
        catch (IOException e)
        {
            Debug.Log("Neuronnätet kunde inte sparas till sökvägen: " + Application.persistentDataPath + "/SavedNetwork");
            Debug.LogException(e);
            return false;
        }

        return true;
    }

    public bool LoadNetwork()
    {
        try
        {
            using StreamReader sr = new StreamReader(Application.persistentDataPath + "/SavedNetwork");
            string[] line = sr.ReadLine().Trim().Split();
            layers = new int[line.Length];
            for (int i = 0; i < line.Length; i++)
            {
                if (!int.TryParse(line[i], NumberStyles.Integer, new CultureInfo("en-US"), out layers[i]))
                {
                    Debug.Log("Kunde ej \"parsea\" " + line[i] + " till integer");
                    return false;
                }
            }

            CreateNewNetwork();

            for (int i = 1; i < layers.Length; i++)
            {
                for (int j = 0; j < layers[i]; j++)
                {
                    int count = 0;
                    line = sr.ReadLine().Trim().Split();
                    for (int k = 0; k < layers[i - 1]; k++)
                    {
                        if (!double.TryParse(line[count++], NumberStyles.Float, new CultureInfo("en-US"), out weights[i - 1][j][k]))
                        {
                            Debug.Log(double.Parse(line[count - 1], new CultureInfo("en-US")));
                            Debug.Log("Kunde ej \"parsea\" " + line[count - 1] + " till double");
                            return false;
                        }
                    }

                    if (!double.TryParse(line[count++], NumberStyles.Float, new CultureInfo("en-US"), out biases[i][j]))
                    {
                        Debug.Log("Kunde ej \"parsea\" " + line[count - 1] + " till double");
                        return false;
                    }
                }
            }
        }
        catch (IOException e)
        {
            Debug.Log("Neuronnätet kunde ej laddas in ifrån sökvägen: " + Application.persistentDataPath + "/SavedNetwork");
            Debug.LogException(e);
            return false;
        }

        return true;
    }

    public double Cost()
    {
        double cost = 0d;
        int outputLayer = layers.Length - 1;
        for (int i = 0; i < neurons[outputLayer].Length; i++)
        {
            if (i == answer)
            {
                cost += System.Math.Pow(neurons[outputLayer][i] - 1d, 2);
            }
            else
            {
                cost += System.Math.Pow(neurons[outputLayer][i], 2);
            }
        }

        return cost;
    }

    public string GetOutput()
    {
        int outputLayer = layers.Length - 1;
        double value = 0d;
        for (int i = 0; i < neurons[outputLayer].Length; i++)
        {
            if (neurons[outputLayer][i] > value)
            {
                value = neurons[outputLayer][i];
                output = i + " : " + (value * 100d).ToString("#.##") + "%";
            }
        }

        return output;
    }

    public float Accuracy()
    {
        return total == 0 ? 0 : (float)(correct / (float)total);
    }

    public void Train(double[] data)
    {
        InputData(data, (int) data[0]);
        Backpropagation();
    }

    public void Feed(double[] data)
    {
        InputData(data, (int) data[0]);
    }

    private double[] InputData(double[] input, int ans)
    {
        if (input.Length - 1 != layers[0])
        {
            Debug.Log("Input-tabellen är ej korrekt längd");
            return null;
        }

        if (ans >= layers[layers.Length - 1] || ans < 0)
        {
            Debug.Log("Input-svaret är ej inom korrekt omfång");
            return null;
        }

        answer = ans;
        for (int i = 0; i < layers[0]; i++)
        {
            neurons[0][i] = input[i + 1];
        }

        return FeedForward();
    }

    private double[] FeedForward()
    {
        for (int i = 1; i < layers.Length; i++)
        {
            for (int j = 0; j < neurons[i].Length; j++)
            {
                double x = 0d;
                for (int k = 0; k < neurons[i - 1].Length; k++)
                {
                    x += weights[i - 1][j][k] * neurons[i - 1][k];
                }

                double z = x + biases[i][j];
                activation_z_values[i][j] = z;
                neurons[i][j] = Activation(z);
            }
        }

        return neurons[layers.Length - 1];
    }

    private void Backpropagation()
    {
        if (samples == 0)
        {
            correct = total = 0;
        }

        for (int i = neurons.Length - 1; i > 0; i--)
        {
            for (int j = 0; j < neurons[i].Length; j++)
            {
                double delta = 0d;
                if (i == neurons.Length - 1)
                {
                    if (j == answer)
                    {
                        delta = 2 * (neurons[i][j] - 1d);
                    }
                    else
                    {
                        delta  = 2 * neurons[i][j];
                    }
                }
                else
                {
                    for (int l = 0; l < neurons[i + 1].Length; l++)
                    {
                        delta += weights[i][l][j] * activation_z_values[i + 1][l];
                    }
                }

                double derivative = ActivationDerivative(activation_z_values[i][j]) * delta;
                activation_z_values[i][j] = derivative;
                for (int k = 0; k < neurons[i - 1].Length; k++)
                {
                    weightDerivative[i - 1][j][k] += neurons[i - 1][k] * derivative;
                }

                biasDerivative[i][j] += derivative;
            }
        }

        int outputLayer = layers.Length - 1;
        double value = neurons[outputLayer][answer];
        bool flag = true;
        for (int i = 0; i <= outputLayer; i++)
        {
            if (i == answer) continue;
            if (neurons[outputLayer][i] > value)
            {
                flag = false;
                break;
            }
        }

        if (flag) correct++;
        total++;

        if (++samples >= batchSize)
        {
            samples = 0;
            UpdateNetwork();
        }
    }

    private void UpdateNetwork()
    {
        for (int i = 1; i < neurons.Length; i++)
        {
            for (int j = 0; j < neurons[i].Length; j++)
            {
                for (int k = 0; k < neurons[i - 1].Length; k++)
                {
                    weights[i - 1][j][k] -= learnRate * weightDerivative[i - 1][j][k] / batchSize;
                    weightDerivative[i - 1][j][k] = 0d;
                }

                biases[i][j] -= learnRate * biasDerivative[i][j] / batchSize;
                biasDerivative[i][j] = 0d;
            }
        }
    }

    private double Activation(double x)
    {
        return 1d / (1d + System.Math.Exp(-x));
    }

    private double ActivationDerivative(double x)
    {
        double exp = System.Math.Exp(x);
        return exp / System.Math.Pow(1d + exp, 2);
    }

    #endregion
}