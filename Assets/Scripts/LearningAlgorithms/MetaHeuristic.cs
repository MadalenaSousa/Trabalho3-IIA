using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

public abstract class MetaHeuristic : MonoBehaviour
{

    public enum MutationType { Gaussian, Random };
    public MutationType mutation = MutationType.Gaussian;
    public enum SelectionType { Tournament};
    public SelectionType selectionMethod;
    public int populationSize;
	public int[] topology;
	public int numGenerations;
    public int maxNumberOfEvaluations;
    public bool seedPopulationFromFile;
    public string pathToFileRed;
    public string pathToFileBlue;
    protected NeuralNetwork neuralNetworkFromFile;
    [HideInInspector] public int generation;
    [HideInInspector] public string logFilename;

	protected List<Individual> populationRed;
    protected List<Individual> populationBlue;

    protected int evaluatedIndividuals;
	protected string report = "Generation,PopBestRed,PopBestBlue,PopAvgRed,PopAvgBlue,BestOverallRed,BestOverallBlue\n";
	protected string bestRed = "";
    protected string bestBlue = "";
    public SelectionMethod selection;
    protected string folder = "Assets/Logs/";
    public string sufix = "_teste";
    private string curr_folder = null;
	public Individual overallBestRed{ get; set;}
    public Individual overallBestBlue { get; set; }

    public List<Individual> PopulationRed
	{
		get
		{
			return populationRed;
		}
	}

    public List<Individual> PopulationBlue
    {
        get
        {
            return populationBlue;
        }
    }

    public Individual GenerationBestRed
	{
		get
		{
			float max = float.MinValue;
			Individual max_ind = null;
			foreach (Individual indiv in populationRed) {
				if (indiv.Fitness > max) {
					max = indiv.Fitness;
					max_ind = indiv;
				}
			}
			return max_ind;
		}
	}

    public Individual GenerationBestBlue
    {
        get
        {
            float max = float.MinValue;
            Individual max_ind = null;
            foreach (Individual indiv in populationBlue)
            {
                if (indiv.Fitness > max)
                {
                    max = indiv.Fitness;
                    max_ind = indiv;
                }
            }
            return max_ind;
        }
    }


    public float PopAvgRed
	{
		get
		{
			float sum = 0.0f;
			foreach (Individual indiv in populationRed) {
				sum += indiv.Fitness;
			}
			return (sum / populationSize);
		}
	}

    public float PopAvgBlue
    {
        get
        {
            float sum = 0.0f;
            foreach (Individual indiv in populationBlue)
            {
                sum += indiv.Fitness;
            }
            return (sum / populationSize);
        }
    }

    //Population Initilization
    public abstract void InitPopulation ();
	//The Step function assumes that the fitness values of all the individuals in the population have been calculated.
	public abstract void Step();


	public void updateReport() {
		if (overallBestRed == null || overallBestRed.Fitness < GenerationBestRed.Fitness) {
			overallBestRed = GenerationBestRed.Clone();
            //Debug.Log("Iteration " + generation + " Fitness " + overallBestRed.Fitness + " Best Red\n" + overallBestRed);
		}

        if (overallBestBlue == null || overallBestBlue.Fitness < GenerationBestBlue.Fitness)
        {
            overallBestBlue = GenerationBestBlue.Clone();
            //Debug.Log("Iteration " + generation + " Fitness " + overallBestBlue.Fitness + " Best Blue\n" + overallBestBlue);
        }
        float populationBestRed = GenerationBestRed.Fitness;
        float populationBestBlue = GenerationBestBlue.Fitness;
        bestRed = overallBestRed.ToString();
        bestBlue = overallBestBlue.ToString();
        report +=  string.Format("{0},{1},{2},{3},{4},{5},{6}\n", generation,populationBestRed,populationBestBlue, PopAvgRed, PopAvgBlue, overallBestRed.Fitness, overallBestBlue.Fitness);
		Debug.Log (report);
    }

    public void ResetBestOverall()
    {
        dumpOverallBest(string.Format("Best_Red_gen_{0}.dat", generation), overallBestRed);
        dumpOverallBest(string.Format("Best_Blue_gen_{0}.dat", generation), overallBestBlue);
        overallBestRed = null;
        overallBestRed = null;
    }

    public void dumpStats() {
        string cur_time = System.DateTime.Now.ToString("MM-dd-HH-mm-ss");
        if (this.curr_folder == null)
        {
            this.curr_folder = cur_time+sufix+"/";
            if (!Directory.Exists(folder + curr_folder))
            {
                Directory.CreateDirectory(folder + curr_folder);
            }
        }
		writeToFile (curr_folder + string.Format ("EvolutionaryStatistics_{0}_gen_{1}.csv", cur_time, generation), report);
		writeToFile(curr_folder + string.Format ("EvolutionaryRunBest_Red_{0}_gen_{1}.txt", cur_time, generation), bestRed);
        writeToFile(curr_folder + string.Format("EvolutionaryRunBest_Blue_{0}_gen_{1}.txt", cur_time, generation), bestBlue);
        dumpOverallBest(curr_folder + string.Format ("Best_Red_{0}_gen_{1}.dat", cur_time, generation), overallBestRed);
        dumpOverallBest(curr_folder + string.Format("Best_Blue_{0}_gen_{1}.dat", cur_time, generation), overallBestBlue);
	}

    private void writeToFile(string path, string data)
    {
        StreamWriter writer = new StreamWriter(folder + path, true);
        writer.WriteLine(data);
        writer.Close();
    }

    public void dumpOverallBest(string path, Individual ind) {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(folder + path);
        bf.Serialize(file, ind.getIndividualController());
        file.Close();
    }


    public NeuralNetwork getIndividualFromFile(string path)
    {
        neuralNetworkFromFile = null;
        if (neuralNetworkFromFile == null && File.Exists(path))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(path, FileMode.Open);
            neuralNetworkFromFile = (NeuralNetwork)bf.Deserialize(file);
            file.Close();
        }
        else
        {
            throw new FileNotFoundException("The file you provided could not be loaded");
        }

        return neuralNetworkFromFile;
    }

    public NeuralNetwork getRedIndividualFromFile()
    {
        return getIndividualFromFile(pathToFileRed);
    }

    public NeuralNetwork getBlueIndividualFromFile()
    {
        return getIndividualFromFile(pathToFileBlue);
    }


}

