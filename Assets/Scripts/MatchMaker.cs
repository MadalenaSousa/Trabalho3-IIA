using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


public class MatchMaker : MonoBehaviour {

	// instances
	public static MatchMaker instance = null;
	public Text infoText;
	public bool simulating = false;
	public string PathBluePlayer;
    public string PathRedPlayer;
    public GameObject simulationPrefab;
	private SimulationInfo bestSimulation;
	private NeuralNetwork BlueController;
    private NeuralNetwork RedController;
    public int TheTimeScale = 1;
    protected string folder = "Assets/Logs/";
    public bool randomRedPlayerPosition = false;
    public bool randomBluePlayerPosition = false;
    public bool randomBallPosition = false;
    public bool defenseTask = false;

    public float MatchTime;

	void Awake(){
		// deal with the singleton part
		if (instance == null) {
			instance = this;
		}
		else if (instance != this) {
			DestroyImmediate (gameObject);    
		}
		DontDestroyOnLoad(gameObject);
		loadBest ();
		simulating = false;

	}

	void loadBest() {
		if(File.Exists(folder + PathBluePlayer))
		{
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(folder + PathBluePlayer, FileMode.Open);
			this.BlueController = (NeuralNetwork) bf.Deserialize(file);
			file.Close();
		}

        if (File.Exists(folder + PathRedPlayer))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(folder + PathRedPlayer, FileMode.Open);
            this.RedController = (NeuralNetwork)bf.Deserialize(file);
            file.Close();
        }

    }

	private SimulationInfo createSimulation(int sim_i, Rect location)
	{
        D31NeuralControler bluePlayerScript = null;
        D31NeuralControler redPlayerScript = null;
        GameObject sim = Instantiate(simulationPrefab, transform.position + new Vector3(0, (sim_i * 250), 0), Quaternion.identity);
        sim.GetComponentInChildren<Camera>().rect = location;
        if (sim.transform.Find("D31-red") != null)
            redPlayerScript = sim.transform.Find("D31-red").gameObject.transform.Find("Body").gameObject.GetComponent<D31NeuralControler>();
        if (sim.transform.Find("D31-blue") != null)
            bluePlayerScript = sim.transform.Find("D31-blue").gameObject.transform.Find("Body").gameObject.GetComponent<D31NeuralControler>();
        sim.GetComponentInChildren<Camera> ().rect = location;
		sim.transform.Find("Scoring System").gameObject.GetComponent<ScoreKeeper>().setIds(PathBluePlayer, PathRedPlayer);

		if (bluePlayerScript != null &&  bluePlayerScript.enabled)
        {// BluePlayer Controller
            bluePlayerScript.neuralController = BlueController;
            bluePlayerScript.maxSimulTime = this.MatchTime;
            bluePlayerScript.running = true;
        }
        if (redPlayerScript != null && redPlayerScript.enabled || PathRedPlayer.Length != 0)
        {// RedController Controller
            redPlayerScript.enabled = true;
            redPlayerScript.neuralController = RedController;
            redPlayerScript.maxSimulTime = this.MatchTime;
            redPlayerScript.running = true;
		}

        return new SimulationInfo (sim, redPlayerScript,bluePlayerScript, 0,0);
	}

	void Update () {
        infoText.text = "Playing a match for " + this.MatchTime +" secs";
		// show best.. in loop
		if (!simulating) {
            // x values: -20, 20
            // z values: -25, 25
            Vector3 redPlayerStartPosition = new Vector3(Random.Range(-20, 20), 0, Random.Range(0, 20));
            Vector3 ballStartPosition = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 0));



            bestSimulation = createSimulation (0, new Rect (0.0f, 0.0f, 1f, 1f));

            if (randomRedPlayerPosition)
            {
                GameObject p = bestSimulation.sim.transform.Find("D31-red").gameObject;
                redPlayerStartPosition.y = p.transform.localPosition.y;
                p.transform.localPosition = redPlayerStartPosition;
            }

            if (randomBallPosition)
            {
                GameObject p = bestSimulation.sim.transform.Find("Ball").gameObject;
                ballStartPosition.y = p.transform.localPosition.y;
                p.transform.localPosition = ballStartPosition;
            }

            if (defenseTask)
            {
                Goal goal = bestSimulation.sim.transform.Find("Field").transform.Find("RedGoal").GetComponent<Goal>();
                GameObject p = bestSimulation.sim.transform.Find("Ball").gameObject;
                if (randomBallPosition)
                {
                    goal.initalBallPosition = ballStartPosition;
                }
                else
                {
                    goal.initalBallPosition = p.transform.position;
                }
                goal.ShootTheBallInMyDirection();

            }

            Time.timeScale = TheTimeScale;
			simulating = true;

		} else if (simulating) {
			if (!bestSimulation.playerRed.running && bestSimulation.playerRed.gameOver) {
                Debug.Log("Red " + bestSimulation.playerRed.GetScoreRed());
                if(bestSimulation.playerBlue != null)
                    Debug.Log("Blue " + bestSimulation.playerBlue.GetScoreBlue());
                simulating = false;
				DestroyImmediate (bestSimulation.sim);
			}
		}
	}
	}




