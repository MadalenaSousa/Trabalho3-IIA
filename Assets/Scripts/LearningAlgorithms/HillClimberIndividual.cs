using System.Linq;
using UnityEngine;

public class HillClimberIndividual : Individual {

	public HillClimberIndividual(int[] topology, int numberOfEvaluations, MetaHeuristic.MutationType mutation) : base(topology, numberOfEvaluations, mutation) {
		
	}

	public override void Initialize () {
		for (int i = 0; i < totalSize; i++) {
			genotype [i] = Random.Range (-1.0f, 1.0f);
		}
	}

    public override void Initialize(NeuralNetwork nn)
    {
        int size = nn.weights.Length * nn.weights[0].Length * nn.weights[0][0].Length;
        if (size != totalSize)
        {
            throw new System.Exception("The Networks do not have the same size!");
        }
        Debug.Log(nn.weights.SelectMany(listsLevel0 => listsLevel0.SelectMany(a => a).ToArray()).ToArray());
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
        throw new System.NotImplementedException();
    }
    public override void Crossover (Individual partner, float probability)
	{
		throw new System.NotImplementedException ();
	}

	public override Individual Clone ()
	{
		HillClimberIndividual new_ind = new HillClimberIndividual(this.topology, this.maxNumberOfEvaluations, this.mutation);
		genotype.CopyTo (new_ind.genotype, 0);
		new_ind.fitness = this.Fitness;
		new_ind.evaluated = false;
        new_ind.completedEvaluations = 0;
		return new_ind;
	}
}
