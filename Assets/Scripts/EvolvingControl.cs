using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Random = UnityEngine.Random;


public class SimulationInfo
{
    public GameObject sim;
    public D31NeuralControler playerRed;
    public D31NeuralControler playerBlue;
    public int individualIndexRed;
    public int individualIndexBlue;


    public SimulationInfo(GameObject sim, D31NeuralControler playerRed, D31NeuralControler playerBlue, int individualIndexRed, int individualIndexBlue)
    {
        this.sim = sim;
        this.playerRed = playerRed;
        this.playerBlue = playerBlue;
        this.individualIndexRed = individualIndexRed;
        this.individualIndexBlue = individualIndexBlue;

    }
}

public class EvolvingControl : MonoBehaviour {




    // instances
    public static EvolvingControl instance = null;
    [HideInInspector]
    public Text infoText;
    [HideInInspector]
    public bool simulating = false;
	public GameObject simulationPrefab;
	public int SimultaneousSimulations;
	public int RandomSeed;

	private MetaHeuristic metaengine;
	private List<SimulationInfo> simsInfo;
	private bool goNextGen = false;
	private int sims_done = 0;
	private int indiv_index_red = 0;
    private int indiv_index_blue = 0;
    private bool allFinished = false;
    int simulations_index = 0;
    int totalSimulations = 0;
    [Header("Scenario Conditions")]
    public bool randomRedPlayerPosition = true;
    public bool randomBluePlayerPosition = true;
    public bool randomBallPosition = true;
    public bool movingBall = true;
    public int ChangePositions;
    protected Vector3 redPlayerStartPosition;
    protected Vector3 ballStartPosition;
    protected Vector3 bluePlayerStartPosition;
    protected List<int> indexesRed;
    protected List<int> indexesBlue;
    protected bool singlePlayer;
    protected string textoUpdate;

    public void Shuffle(List<int> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }

    private void Start()
	{
		
	}

	void Awake(){
		// deal with the singleton part
		if (instance == null) {
			instance = this;
		}
		else if (instance != this) {
			Destroy (gameObject);    
		}
		Random.InitState(RandomSeed);

		DontDestroyOnLoad(gameObject);
		initMetaHeuristic ();
        
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
        init ();

	}
		
	void init(){
		createSimulationGrid (); 
		startSimulations ();
	}

	void initMetaHeuristic(){
		MetaHeuristic[] metaengines = this.GetComponentsInParent<MetaHeuristic> ();
		// check which one is active
		foreach(MetaHeuristic tmp in metaengines){
			if(tmp.enabled){
				metaengine = tmp;
				metaengine.InitPopulation();
				break;
			}
		}
	}


	private void createSimulationGrid ()
	{
        indexesRed = Enumerable.Range(0, metaengine.populationSize).ToList();
        indexesBlue = Enumerable.Range(0, metaengine.populationSize).ToList();

        singlePlayer = simulationPrefab.name.Contains("Solo");
        if (singlePlayer) {
            totalSimulations = metaengine.populationSize; //just the one agent
        }
        else
        {
            totalSimulations = metaengine.populationSize; // All vs All
            this.Shuffle(indexesRed);
            this.Shuffle(indexesBlue);

        }
        
		simsInfo = new List<SimulationInfo> ();
		int ncols = totalSimulations == 1 ? 1 : Mathf.Min(SimultaneousSimulations,5);
        float spacing = 1.0f / (float) ncols;
		float sim_height = 1f / (float) ncols;
		float start_x = 0.0f, start_y = 0.0f;
        for (int i = 0; i < SimultaneousSimulations && i < totalSimulations; i++)
        {

            if (i > 0 && i % ncols == 0)
            {
                start_x = 0.0f;
                start_y += sim_height;
            }
            simsInfo.Add(createSimulation(simulations_index, new Rect(start_x, start_y, spacing, sim_height), indexesRed[simulations_index], indexesBlue[simulations_index]));
            start_x += spacing;
			
			simulations_index++;


		}
			
		Time.timeScale = 1.0f;
	}

	

	private SimulationInfo createSimulation(int sim_i, Rect location, int indexIndRed, int indexIndBlue)
	{
        D31NeuralControler bluePlayerScript = null;
        D31NeuralControler redPlayerScript = null;
        GameObject sim = Instantiate (simulationPrefab, transform.position + new Vector3 (0, (sim_i * 250), 0), Quaternion.identity);
        sim.GetComponentInChildren<Camera>().rect = location;
        if(sim.transform.Find("D31-red") != null)
        {

            redPlayerScript = sim.transform.Find("D31-red").gameObject.transform.Find("Body").gameObject.GetComponent<D31NeuralControler>();

        }

        if (sim.transform.Find("D31-blue") != null)
        {
            bluePlayerScript = sim.transform.Find("D31-blue").gameObject.transform.Find("Body").gameObject.GetComponent<D31NeuralControler>();
           
        }

        sim.transform.Find("Scoring System").gameObject.GetComponent<ScoreKeeper>().setIds(""+indexIndBlue, ""+indexIndRed);

        if (redPlayerScript != null && redPlayerScript.enabled) {
            redPlayerScript.neuralController = metaengine.PopulationRed[indexIndRed].getIndividualController();
        }
        if (bluePlayerScript != null && bluePlayerScript.enabled)
        {
            bluePlayerScript.neuralController = metaengine.PopulationBlue[indexIndBlue].getIndividualController();
        }

        return new SimulationInfo (sim, redPlayerScript, bluePlayerScript , indexIndRed, indexIndBlue);
	}


    private void SetTasks(SimulationInfo info)
    {
        if (randomRedPlayerPosition)
        {
            GameObject p = info.sim.transform.Find("D31-red").gameObject;
            redPlayerStartPosition.y = p.transform.localPosition.y;
            p.transform.localPosition = redPlayerStartPosition;
        }

        if (randomBluePlayerPosition)
        {
            GameObject p = info.sim.transform.Find("D31-blue").gameObject;
            bluePlayerStartPosition.y = p.transform.localPosition.y;
            p.transform.localPosition = bluePlayerStartPosition;
        }

        if (randomBallPosition)
        {
            GameObject p = info.sim.transform.Find("Ball").gameObject;
            ballStartPosition.y = p.transform.localPosition.y;
            p.transform.localPosition = ballStartPosition;
        }
        if (movingBall)
        {
            GameObject p = info.sim.transform.Find("Ball").gameObject;
            Goal goal = info.sim.transform.Find("Field").transform.Find("RedGoal").GetComponent<Goal>();
            goal.initalBallPosition = ballStartPosition;
            goal.ShootTheBallInMyDirection();

        }
    }


    private void startSimulations()
	{
        // x values: -20, 20
        // z values: -25, 25

        if (metaengine.generation % ChangePositions == 0)
        {   
            if(randomBallPosition)
            {
                ballStartPosition = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 0));
            }
            if (randomRedPlayerPosition)
            {
                redPlayerStartPosition = new Vector3(Random.Range(-20, 20), 0, Random.Range(0, 20));
            }
            if (randomBluePlayerPosition)
            {
                bluePlayerStartPosition = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 0));
            }

        }


        if (metaengine.generation != 0 && (!singlePlayer || randomBallPosition || randomRedPlayerPosition || randomBluePlayerPosition))
        {
            metaengine.ResetBestOverall();
        }



        foreach (SimulationInfo info in simsInfo) 
        {
            SetTasks(info);

            info.playerRed.running = true;
            if (info.playerBlue != null)
            {
                info.playerBlue.running = true;
            }
        }
		simulating = true;
		sims_done = 0;
    }

    private void FixedUpdate () {
        if (!allFinished) {
			// evolve solution.. Simulate
			if (simulating) {
			
				for (int i = 0; i < simsInfo.Count; i++) {
                    if (simsInfo [i] != null 
						&& !simsInfo [i].playerRed.running && simsInfo [i].playerRed.gameOver 
						&& (simsInfo[i].playerBlue == null || !simsInfo[i].playerBlue.running && simsInfo[i].playerBlue.gameOver)) { 

						// FITNESS ASSIGNMENT 
						if (simsInfo[i].playerRed != null && !metaengine.PopulationRed [simsInfo [i].individualIndexRed].Evaluated) {
							metaengine.PopulationRed [simsInfo [i].individualIndexRed].SetEvaluations(simsInfo [i].playerRed.GetScoreRed());
                        }
                        if (simsInfo[i].playerBlue != null && !metaengine.PopulationBlue[simsInfo[i].individualIndexBlue].Evaluated)
                        {
                            metaengine.PopulationBlue[simsInfo[i].individualIndexBlue].SetEvaluations(simsInfo[i].playerBlue.GetScoreBlue());
                        }
						//
						
                        Rect rect = new Rect(simsInfo[i].sim.GetComponentInChildren<Camera>().rect);
                        DestroyImmediate(simsInfo[i].sim);
                        // deploy another in its place
                        if (simulations_index < totalSimulations) {
							simsInfo [i] = createSimulation (i, rect, indexesRed[simulations_index], indexesBlue[simulations_index]);
                            SetTasks(simsInfo[i]);
                            if(simsInfo[i].playerRed != null)
							    simsInfo [i].playerRed.running = true;
                            if(simsInfo[i].playerBlue != null)
                                simsInfo [i].playerBlue.running = true;
                            if ((indiv_index_blue + 1) % metaengine.populationSize == 0)
                            {
                                indiv_index_blue = 0;
                                indiv_index_red++;
                            }
                            else
                            {
                                indiv_index_blue++;
                            }
                            simulations_index++;
						} else {
							simsInfo [i] = null;
						}
						sims_done++;
					}
					
                }
                if(metaengine.generation == 1)
                {
                    infoText.text = "Generation: " + metaengine.generation + "/" + metaengine.numberOfGenerations + "    Simulation: " + sims_done + "/" + totalSimulations + "\nCurrent Pop Avg Fitness Red: " + metaengine.PopAvgRed + " Current Best Red: " + metaengine.GenerationBestRed.Fitness + "\nCurrent Pop Avg Fitness Blue: " + metaengine.PopAvgBlue + " Current Best Blue: " + metaengine.GenerationBestBlue.Fitness;
                }
                infoText.text = textoUpdate;

                if (sims_done == totalSimulations) {
                    textoUpdate = "Generation: " + metaengine.generation + "/" + metaengine.numberOfGenerations + "    Simulation: " + sims_done + "/" + totalSimulations + "\nCurrent Pop Avg Fitness Red: " + metaengine.PopAvgRed + " Current Best Red: " + metaengine.GenerationBestRed.Fitness + "\nCurrent Pop Avg Fitness Blue: " + metaengine.PopAvgBlue + " Current Best Blue: " + metaengine.GenerationBestBlue.Fitness;
                    // clear simsInfo array..
                    simsInfo.Clear ();
                    goNextGen = true;
					simulating = false;
				}
			}
		}
	}

	public void Update(){
		if (goNextGen) {
			goNextGen = false;
			if (metaengine.generation < metaengine.numberOfGenerations) {

				// Perform an evolutionary algorithm step
				metaengine.Step ();
				// reset simulation grid variables
				sims_done = 0;
				indiv_index_red = 0;
                indiv_index_blue = 0;
                simulations_index = 0;
                // Init grids and start simulations again
                init ();
			} else {
				if (!allFinished) {
					allFinished = true;
					simulating = false;
					metaengine.updateReport ();
					metaengine.dumpStats ();
                    infoText.text = "All Done!";
				}
			}
		}

	}

}
