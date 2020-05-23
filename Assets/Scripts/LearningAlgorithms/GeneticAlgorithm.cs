using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticAlgorithm : MetaHeuristic
{

    public int tournamentSize;

    [Header("Red Population Parameters")]
    public float mutationProbabilityRedPopulation;
    public float crossoverProbabilityRedPopulation;
    public bool elitistRed = true;

    [Header("Blue Population Parameters")]
    public float mutationProbabilityBluePopulation;
    public float crossoverProbabilityBluePopulation;
    public bool elitistBlue = true;

    public override void InitPopulation()
    {
        maxNumberOfEvaluations = Mathf.Min(maxNumberOfEvaluations, populationSize);
        populationRed = new List<Individual>();
        populationBlue = new List<Individual>();

        while (populationRed.Count < populationSize)
        {
            GeneticIndividual new_ind_red = new GeneticIndividual(NNTopology, maxNumberOfEvaluations, mutationMethod);
            GeneticIndividual new_ind_blue = new GeneticIndividual(NNTopology, maxNumberOfEvaluations, mutationMethod);

            if (seedPopulationFromFile)
            {
                NeuralNetwork nnRed = getRedIndividualFromFile();
                NeuralNetwork nnBlue = getBlueIndividualFromFile();
                new_ind_red.Initialize(nnRed);
                new_ind_blue.Initialize(nnBlue);
                //only the first individual is an exact copy. the other are going to suffer mutations
                if (populationRed.Count != 0 && populationBlue.Count != 0)
                {
                    new_ind_red.Mutate(mutationProbabilityRedPopulation);
                    new_ind_blue.Mutate(mutationProbabilityBluePopulation);
                }

            }
            else
            {
                new_ind_red.Initialize();
                new_ind_blue.Initialize();
            }

            populationRed.Add(new_ind_red);
            populationBlue.Add(new_ind_blue);
        }

        switch (selectionMethod)
        {
            case SelectionType.Tournament:
                selection = new TournamentSelection(tournamentSize);
                break;
        }

    }

    //The Step function assumes that the fitness values of all the individuals in the population have been calculated.
    public override void Step()
    {
        updateReport(); //called to get some stats

        // update the generation before creating the new pop
        generation++;
        if (generation == numberOfGenerations) // no need to execute the step if we reached max generation
            return;
        
        List<Individual> newPopRed;
        List<Individual> newPopBlue;

        //Select parents
        newPopRed = selection.selectIndividuals(populationRed, populationSize);
        newPopBlue = selection.selectIndividuals(populationBlue, populationSize);

        //Crossover
        for (int i = 0; i < populationSize - 1; i += 2)
        {
            Individual parent1Red = newPopRed[i];
            Individual parent2Red = newPopRed[i + 1];

            Individual parent1Blue = newPopBlue[i];
            Individual parent2Blue = newPopBlue[i + 1];

            parent1Red.Crossover(parent2Red, crossoverProbabilityRedPopulation);
            parent1Blue.Crossover(parent2Blue, crossoverProbabilityBluePopulation);
        }

        //Mutation 
        for (int i = 0; i < populationSize; i++)
        {
            newPopRed[i].Mutate(mutationProbabilityRedPopulation);
            newPopBlue[i].Mutate(mutationProbabilityBluePopulation);

        }

        if (elitistRed)
        {
            Individual tmpRed = overallBestRed.Clone();
            newPopRed[0] = tmpRed;
        }
        if (elitistBlue)
        {

            Individual tmpBlue = overallBestBlue.Clone();
            newPopBlue[0] = tmpBlue;
        }

        populationRed = newPopRed;
        populationBlue = newPopBlue;
    }

}