using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MetaHeuristic;

public class GeneticIndividual : Individual {


	public GeneticIndividual(int[] topology, int numberOfEvaluations, MutationType mutation) : base(topology, numberOfEvaluations, mutation) {
	}

	public override void Initialize () 
	{
		for (int i = 0; i < totalSize; i++)
		{
			genotype[i] = Random.Range(-1.0f, 1.0f);
		}
	}

   public override void Initialize(NeuralNetwork nn)
    {
        int size = nn.weights.Length * nn.weights[0].Length * nn.weights[0][0].Length;
        if (size != totalSize)
        {
            throw new System.Exception("The Networks do not have the same size!");
        }

        float[] weights = new float[size];
        int weightPos = 0;
        for (int i = 0; i < nn.weights.Length; i++)
        {
            for (int j = 0; j < nn.weights[i].Length; j++)
            {
                for (int k = 0; k < nn.weights[i][j].Length; k++)
                {
                    weights[weightPos++] = nn.weights[i][j][k];
                }
            }
        }
        weights.CopyTo(genotype, 0);
    }

    public override Individual Clone()
    {
        GeneticIndividual new_ind = new GeneticIndividual(this.topology, this.maxNumberOfEvaluations, this.mutation);

        genotype.CopyTo(new_ind.genotype, 0);
        new_ind.fitness = this.Fitness;
        new_ind.evaluated = false;

        return new_ind;
    }


    public override void Mutate(float probability)
    {
        switch (mutation)
        {
            case MetaHeuristic.MutationType.Gaussian:
                MutateGaussian(probability);
                break;
            case MetaHeuristic.MutationType.Random:
                MutateRandom(probability);
                break;
        }
    }
    public void MutateRandom(float probability)
    {
        for (int i = 0; i < totalSize; i++)
        {
            if (Random.Range(0.0f, 1.0f) < probability)
            {
                genotype[i] = Random.Range(-1.0f, 1.0f);
            }
        }
    }

    
    public void MutateGaussian(float probability)
    {
        /* YOUR CODE HERE! */
        float mean = 0; // média/meio
        float stdev = 0.5f; //desvio padrão

        for (int i = 0; i < totalSize; i++)
        {
            if (Random.Range(0.0f, 1.0f) < probability) // de vez em quando, probabilisticamente, entro no if
            {
                genotype[i] = genotype[i] + NextGaussian(mean, stdev); // e aplico uma mutação gaussiana
            }
        }
    }

    public override void Crossover(Individual partner, float probability)
    {
        /* YOUR CODE HERE! */
        /* Nota: O crossover deverá alterar ambos os indivíduos */

        if (Random.Range(0.0f, 1.0f) < probability) // de vez em quando, probabilisticamente, entro no if
        {
            int randPoint = Random.Range(0, totalSize - 1);

            for (int i = 0; i < totalSize; i++) //a partir do ponto selecionado
            {
                if (i >= randPoint)
                {
                    float temp = this.genotype[i]; // troca todos os genes para a frente do ponto
                    this.genotype[i] = partner.getGenotype(i);
                    partner.setGenotype(i, temp);
                }
            }

        }
    }


}
