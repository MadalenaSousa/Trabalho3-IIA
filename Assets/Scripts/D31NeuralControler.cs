using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


public class D31NeuralControler : MonoBehaviour
{
    public RobotUnit agent; // the agent controller we want to use
    public int player;
    public GameObject ball;
    public GameObject MyGoal;
    public GameObject AdversaryGoal;
    public GameObject Adversary;
    public GameObject ScoreSystem;


    public int numberOfInputSensores { get; private set; }
    public float[] sensorsInput;
    public float simulationTime = 0;

    // Available Information 
    [Header("Environment  Information")]
    public List<float> distanceToBall;
    public List<float> distanceToMyGoal;
    public List<float> distanceToAdversaryGoal;
    public List<float> distanceToAdversary;
    public List<float> distancefromBallToAdversaryGoal;
    public List<float> distancefromBallToMyGoal;
    public List<float> distanceToClosestWall;
    public float distanceTravelled = 0.0f;
    public float avgSpeed = 0.0f;
    public float maxSpeed = 0.0f;
    public int hitTheBall;
    public int hitTheWall;
    



    public float maxSimulTime = 1;
    public bool GameFieldDebugMode = false;
    public bool gameOver = false;
    public bool running = false;
    public float currentSpeed = 0.0f;
    public int fixedUpdateCalls = 0;


    private Vector3 startPos;
    private Vector3 previousPos;
    private int SampleRate = 1;
    private int countFrames = 0;
    public int GoalsOnAdversaryGoal;
    public int GoalsOnMyGoal;
    public float[] result;



    public NeuralNetwork neuralController;

    private void Awake()
    {
        // get the robot controller
        agent = GetComponent<RobotUnit>();
        numberOfInputSensores = 12;
        sensorsInput = new float[numberOfInputSensores];

        startPos = agent.transform.localPosition;
        previousPos = startPos;
        
        if (GameFieldDebugMode && this.neuralController.weights == null)
        {
            Debug.Log("creating nn..!! ONLY IN GameFieldDebug SCENE THIS SHOULD BE USED!");
            int[] top = { 12, 4, 2 };
            this.neuralController = new NeuralNetwork(top, 0);

        }
        distanceToBall = new List<float>();
        distanceToMyGoal = new List<float>();
        distanceToAdversaryGoal = new List<float>();
        distanceToAdversary = new List<float>();
        distancefromBallToAdversaryGoal = new List<float>();
        distancefromBallToMyGoal = new List<float>();
        distanceToClosestWall = new List<float>();

    }


    private void FixedUpdate()
    {
        simulationTime += Time.deltaTime;
        if (running && fixedUpdateCalls % 10 == 0)
        {
            // updating sensors
            SensorHandling();
            // move
            result = this.neuralController.process(sensorsInput);
            float angle = result[0] * 180;
            float strength = result[1];


            // debug raycast for the force and angle being applied on the agent
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up;
            dir.z = dir.y;
            dir.y = 0;
            Vector3 rayDirection = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up;
            rayDirection.z = rayDirection.y;
            rayDirection.y = 0;
            if (strength > 0)
            {
                Debug.DrawRay(this.transform.position, -rayDirection.normalized * 5, Color.black);
            }
            else
            {
                Debug.DrawRay(this.transform.position, rayDirection.normalized * 5, Color.black);
            }

            agent.rb.AddForce(dir * strength * agent.speed);


            // updating race status
            updateGameStatus();

            // check method
            if (endSimulationConditions())
            {
                wrapUp();
            }
            countFrames++;
        }
        fixedUpdateCalls++;
    }

    // The ambient variables are created here!
    public void SensorHandling()
    {

        Dictionary<string, ObjectInfo> objects = agent.objectsDetector.GetVisibleObjects();

        sensorsInput[0] = objects["DistanceToBall"].distance / 95.0f;
        sensorsInput[1] = objects["DistanceToBall"].angle / 360.0f;
        sensorsInput[2] = objects["MyGoal"].distance / 95.0f;
        sensorsInput[3] = objects["MyGoal"].angle / 360.0f;
        sensorsInput[4] = objects["AdversaryGoal"].distance / 95.0f;
        sensorsInput[5] = objects["AdversaryGoal"].angle / 360;
        if (objects.ContainsKey("Adversary"))
        {
            sensorsInput[6] = objects["Adversary"].distance / 95.0f;
            sensorsInput[7] = objects["Adversary"].angle / 360.0f;
        }
        else
        {
            sensorsInput[6] = -1;// -1 == não existe
            sensorsInput[7] = -1;// -1 == não existe
        }

        sensorsInput[8] = Mathf.CeilToInt(Vector3.Distance(ball.transform.localPosition, MyGoal.transform.localPosition)) / 95.0f; // Normalization: 95 is the max value of distance 


        sensorsInput[9] = Mathf.CeilToInt(Vector3.Distance(ball.transform.localPosition, AdversaryGoal.transform.localPosition)) / 95.0f; // Normalization: 95 is the max value of distance


        sensorsInput[10] = objects["Wall"].distance / 95.0f;
        sensorsInput[11] = objects["Wall"].angle / 360.0f;

        if (countFrames % SampleRate == 0)
        {
            distanceToBall.Add(sensorsInput[0]);
            distanceToMyGoal.Add(sensorsInput[2]);
            distanceToAdversaryGoal.Add(sensorsInput[4]);
            distanceToAdversary.Add(sensorsInput[6]);
            distancefromBallToMyGoal.Add(sensorsInput[8]);
            distancefromBallToAdversaryGoal.Add(sensorsInput[9]);
            distanceToClosestWall.Add(sensorsInput[10]);
        }
    }


    public void updateGameStatus()
    {
        // This is the information you can use to build the fitness function. 
        Vector2 pp = new Vector2(previousPos.x, previousPos.z);
        Vector2 aPos = new Vector2(agent.transform.localPosition.x, agent.transform.localPosition.z);
        float currentDistance = (pp - aPos).magnitude;
        distanceTravelled += currentDistance;
        previousPos = agent.transform.localPosition;
        hitTheBall = agent.hitTheBall;
        hitTheWall = agent.hitTheWall;
        
        currentSpeed = currentDistance / Time.deltaTime;
        maxSpeed = (currentSpeed > maxSpeed ? currentSpeed : maxSpeed);

        // get my score
        GoalsOnMyGoal = ScoreSystem.GetComponent<ScoreKeeper>().score[player == 0 ? 1 : 0];
        // get adversary score
        GoalsOnAdversaryGoal = ScoreSystem.GetComponent<ScoreKeeper>().score[player];


    }

    public void wrapUp()
    {
        avgSpeed = distanceTravelled / simulationTime;
        gameOver = true;
        running = false;
        countFrames = 0;
        fixedUpdateCalls = 0;
    }

    public static float StdDev(IEnumerable<float> values)
    {
        float ret = 0;
        int count = values.Count();
        if (count > 1)
        {
            //Compute the Average
            float avg = values.Average();

            //Perform the Sum of (value-avg)^2
            float sum = values.Sum(d => (d - avg) * (d - avg));

            //Put it all together
            ret = Mathf.Sqrt(sum / count);
        }
        return ret;
    }

    //* FITNESS AND END SIMULATION CONDITIONS *// 

    private bool endSimulationConditions()
    {
        // You can modify this to change the length of the simulation of an individual before evaluating it.
        // (a variavel maxSimulTime está por defeito a 30 segundos)
        //this.maxSimulTime = 30; // Descomentem e alterem aqui valor do maxSimultime se necessário.
        return simulationTime > this.maxSimulTime;
    }

    public float GetScoreBlue()
    {
        // Fitness function for the Blue player. The code to attribute fitness to individuals should be written here.  
        //* YOUR CODE HERE*//
        float fitness = distanceTravelled;
        return fitness;
    }

    public float GetScoreRed()
    {
        // Fitness function for the Red player. The code to attribute fitness to individuals should be written here. 
        //* YOUR CODE HERE*//
        float fitness = distanceTravelled;
        return fitness;
    }

}