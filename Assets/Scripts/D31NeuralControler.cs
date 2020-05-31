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
        this.maxSimulTime = 15; // Descomentem e alterem aqui valor do maxSimultime se necessário.
        return simulationTime > this.maxSimulTime;
    }

    public float GetScoreBlue(EvolvingControl.FitnessTypeBlue behaviour, float goalsW, float hitBallW, float hitTheWallW, float ballDistToAdversaryGoalW, float myDistToBallW, float myDistToAdversaryGoal, float ballDistToMyGoalW, float myDistToMyGoalW, float myDistToWallW)
    {
        float fitness = 0.0f;

        if (behaviour == EvolvingControl.FitnessTypeBlue.kick1)
        {
            fitness = kickFitness1(goalsW, hitBallW, ballDistToAdversaryGoalW, myDistToBallW, ballDistToMyGoalW, myDistToWallW);
        }
        else if(behaviour == EvolvingControl.FitnessTypeBlue.kick2)
        {
            fitness = kickFitness2(goalsW, hitBallW, ballDistToAdversaryGoalW, myDistToBallW);
        }
        else if (behaviour == EvolvingControl.FitnessTypeBlue.Control)
        {
            fitness = controlFitness(goalsW, hitBallW, ballDistToAdversaryGoalW, myDistToBallW, myDistToAdversaryGoal, ballDistToMyGoalW, myDistToMyGoalW);
        }
        else if (behaviour == EvolvingControl.FitnessTypeBlue.Defend)
        {
            fitness = defendFitness(goalsW, hitBallW, myDistToBallW, myDistToMyGoalW);
        } 
        else if(behaviour == EvolvingControl.FitnessTypeBlue.kickAdversary)
        {
            fitness = kickFitnessAdversary();
        } 
        else if(behaviour == EvolvingControl.FitnessTypeBlue.ControlAdversary)
        {
            fitness = controlFitnessAdversary();
        }
        else if (behaviour == EvolvingControl.FitnessTypeBlue.DefendAdversary)
        {
            fitness = defendFitnessAdversary();
        }

        return fitness;
    }

    public float GetScoreRed(EvolvingControl.FitnessTypeRed behaviour, float goalsW, float hitBallW, float hitTheWallW, float ballDistToAdversaryGoalW, float myDistToBallW, float myDistToAdversaryGoal, float ballDistToMyGoalW, float myDistToMyGoalW, float myDistToWallW)
    {
        float fitness = 0.0f;
        
        if(behaviour == EvolvingControl.FitnessTypeRed.kick1)
        {
            fitness = kickFitness1(goalsW, hitBallW, ballDistToAdversaryGoalW, myDistToBallW, ballDistToMyGoalW, myDistToWallW);
        }
        else if (behaviour == EvolvingControl.FitnessTypeRed.kick2)
        {
            fitness = kickFitness2(goalsW, hitBallW, ballDistToAdversaryGoalW, myDistToBallW);
        }
        else if(behaviour == EvolvingControl.FitnessTypeRed.Control)
        {
            fitness = controlFitness(goalsW, hitBallW, ballDistToAdversaryGoalW, myDistToBallW, myDistToAdversaryGoal, ballDistToMyGoalW, myDistToMyGoalW);
        } 
        else if(behaviour == EvolvingControl.FitnessTypeRed.Defend)
        {
            fitness = defendFitness(goalsW, hitBallW, myDistToBallW, myDistToMyGoalW);
        }
        else if (behaviour == EvolvingControl.FitnessTypeRed.kickAdversary)
        {
            fitness = kickFitnessAdversary();
        }
        else if (behaviour == EvolvingControl.FitnessTypeRed.ControlAdversary)
        {
            fitness = controlFitnessAdversary();
        }
        else if (behaviour == EvolvingControl.FitnessTypeRed.DefendAdversary)
        {
            fitness = defendFitnessAdversary();
        }

        return fitness;
    }

    public float defendFitness(float goalsW, float hitBallW, float myDistToBallW, float myDistToMyGoalW)
    {
      
        //-----My Dist To Ball
        float distToBallCount = 0; //quero que a distância à bola seja menor que 0.1 mais vezes
        
        for(int i = 0; i < distanceToBall.Count; i++)
        {
            if(distanceToBall[i] < 0.1)
            {
                distToBallCount++; //*50
            }
        }

        //-----My Dist To Goal
        float insideGoalCount = 0;
        
        for (int i = 0; i < distanceToMyGoal.Count; i++)
        {
            if(distanceToMyGoal[i] == 0)
            {
                insideGoalCount++;
            }
        }

        float distToMyGoalValue;

        if(insideGoalCount > 2) //penalizo se ele tiver mais que 2 vezes dentro da baliza
        {
            distToMyGoalValue = myDistToMyGoalW * -10; //*10
        } else
        {
            distToMyGoalValue = myDistToMyGoalW * 10;
        }

        //-----Goals
        float goalsValue; //quero recompensar quando defendem e penalizar quando sofrem golo
        
        if (GoalsOnMyGoal == 0) 
        {
            goalsValue = goalsW * 200; //*1000
        }
        else
        {
            goalsValue = -goalsW * GoalsOnMyGoal;
        }

        //-----Hit Ball
        float hitBallValue; //quero penalizar quando não tocam na bola e recompensar quando tocam
        
        if (hitTheBall == 0)
        {
            hitBallValue = -hitBallW * 200; //*50
        } else
        {
            hitBallValue = hitBallW * hitTheBall;
        }

        //-----FINAL FITNESS SUM
        float fitness = myDistToBallW * distToBallCount + distToMyGoalValue + goalsValue + hitBallValue;

        return fitness;
    }

    public float kickFitness1(float goalsW, float hitBallW, float ballDistToAdversaryGoalW, float myDistToBallW, float ballDistToMyGoalW, float myDistToWallW)
    {
        //-----Dist To Ball
        float distToBallCount = 0;
        float distToBallValue;

        for (int i = 0; i < distanceToBall.Count; i++)
        {

            if (distanceToBall[i] < 0.05) //quero estar a menos de 0.05 da bola o maior número de vezes
            {
                distToBallCount++;
            }

        }
        distToBallValue = distToBallCount * myDistToBallW;


        //-----Dist From Ball To Adversary Goal
        float distBallToAdversaryGoalCount = 0;
        float distBallToAdversaryGoalValue;


        for (int t = 0; t < distancefromBallToAdversaryGoal.Count; t++)
        {

            if (distancefromBallToAdversaryGoal[t] < distancefromBallToAdversaryGoal[0]) //recompenso quando a distância da bola à baliza adversária é menor que a inicial o maior número de vezes 
            {
                distBallToAdversaryGoalCount++;
            }
            else
            {
                distBallToAdversaryGoalCount--;
            }
        }

        distBallToAdversaryGoalValue = distBallToAdversaryGoalCount * ballDistToAdversaryGoalW;


        //-----Dist From Ball To My Goal
        float distBallToMyGoalCount = 0;
        float distBallToMyGoalValue;


        for (int t = 0; t < distancefromBallToMyGoal.Count; t++)
        {

            if (distancefromBallToMyGoal[t] < distancefromBallToMyGoal[0]) //penalizo quando a distância da bola à minha baliza é menor que a inicial o maior número de vezes
            {
                distBallToMyGoalCount--;
            }
            else
            {
                distBallToMyGoalCount++;
            }
        }

        distBallToMyGoalValue = distBallToMyGoalCount * ballDistToMyGoalW;


        //-----Dist To Wall
        float distToWallCount = 0;
        float distToWallValue;

        for (int i = 0; i < distanceToClosestWall.Count; i++)
        {

            if (distanceToClosestWall[i] < 0.08) //penalizo quando a minha distância à parede é menor que 0.08
            {
                distToWallCount--;
            }

        }

        distToWallValue = distToWallCount * myDistToWallW;


        //-----Hit The Ball
        float hitBallValue;

        if (hitTheBall == 0) //penalizo quando não toca na bola e recompenso caso contrário
        {
            hitBallValue = -hitBallW * 200;
        }
        else
        {
            hitBallValue = hitBallW * hitTheBall;
        }


        //-----Hit The Wall
        float hitWallValue = -20 * hitTheWall; //penalizo por bater na parede


        //-----Goals
        float goalsValue;

        if (GoalsOnAdversaryGoal == 0) //penalizo por não marcar golos e recompenso caso contrário
        {
            goalsValue = -goalsW * 200;
        }
        else
        {
            goalsValue = goalsW * GoalsOnAdversaryGoal;
        }

        float GoalsOnMyGoalValue;

        if (GoalsOnMyGoal > 0) //penalizo por golos na minha baliza e recompenso caso contrário
        {
            GoalsOnMyGoalValue = -GoalsOnMyGoal * 1000;
        }
        else
        {
            GoalsOnMyGoalValue = 800;
        }


        //-----FINAL SUM
        float kickfitness = goalsValue + GoalsOnMyGoalValue + hitBallValue + distToBallValue + distBallToAdversaryGoalValue + hitWallValue + distBallToMyGoalValue + distToWallValue;

        return kickfitness;
    }

    public float kickFitness2(float goalsW, float hitBallW, float ballDistToAdversaryGoalW, float myDistToBallW)
    {
        //-----My Dist To Ball
         float distToBallCount = 0;
         float distToBallValue;

         for (int i = 0; i < distanceToBall.Count; i++)
         {

             if (distanceToBall[i] < 0.05)
             {
                 distToBallCount = distToBallCount + (2 - distanceToBall[i]); //*50
             }

         }

         distToBallValue = distToBallCount * myDistToBallW; //quero que a minha distância à bola seja menor que 0.05 o maior número de vezes

         //-----Ball Dist To Goals
         float ballDistValue;
         float ballDistCount = 0;

         for(int i = 0; i < distancefromBallToMyGoal.Count; i++)
         {
             if(distancefromBallToMyGoal[i] > distancefromBallToAdversaryGoal[i]) //quero que a distância da bola à minha baliza seja menor que a distância da bla à baliza adversária o maior número de vezes
            {
                 ballDistCount++;
             }
             else
             {
                 ballDistCount--;
             }

             if(i > 0 && distancefromBallToAdversaryGoal[i] < distancefromBallToAdversaryGoal[i-1]) //quero que a distância i da bola à baliza adversária seja menor que a distância anterior o maior número de vezes
             {
                 ballDistCount++; //*50
             }
             else
             {
                 ballDistCount--;
             }
         }

         ballDistValue = ballDistCount * ballDistToAdversaryGoalW; 

         //-----Inside Goal
         float insideGoalCount = 0;

         for (int i = 0; i < distanceToMyGoal.Count; i++)
         {
             if (distanceToMyGoal[i] == 0 || distanceToAdversaryGoal[i] == 0)
             {
                 insideGoalCount++;
             }
         }

         float distToMyGoalValue;

         if (insideGoalCount > 4) //penalizo se ele tiver mais que 4 vezes dentro da baliza
         {
             distToMyGoalValue = -100;
         }
         else
         {
             distToMyGoalValue = 50;
         }

         //-----Hit The Ball
         float hitBallValue; //pensalizo por não tocar e recompenso por tocar

         if (hitTheBall == 0)
         {
             hitBallValue = -hitBallW * 200;
         }
         else
         {
             hitBallValue = hitBallW * hitTheBall; //*100
         }

         //-----Goals
         float goalsValue; //penalizo por 0 golos marcados e recompenso caso contrário

         if (GoalsOnAdversaryGoal == 0)
         {
             goalsValue = -goalsW * 200;
         }
         else
         {
             goalsValue = goalsW * GoalsOnAdversaryGoal; //*1000
         }

         float GoalsOnMyGoalValue; //penalizo pelos golos sofridos e recompenso caso contrário

         if (GoalsOnMyGoal > 0)
         {
             GoalsOnMyGoalValue = -GoalsOnMyGoal * 800;
         }
         else
         {
             GoalsOnMyGoalValue = 800;
         }

         //-----FINAL SUM
         float kickfitness = goalsValue + GoalsOnMyGoalValue + hitBallValue + distToBallValue + ballDistValue + (hitTheWall * -10) + distToMyGoalValue;
         return kickfitness;
    }


    public float controlFitness(float goalsW, float hitBallW, float ballDistToAdversaryGoalW, float myDistToBallW, float myDistToAdversaryGoal, float ballDistToMyGoalW, float myDistToMyGoalW)
    {

        //---Distância do jogador à bola
        float distToBallCount = 0;

        for (int i = 0; i < distanceToBall.Count; i++)
        {
            if (distanceToBall[i] < 0.1)
            {
                distToBallCount++;
            }
        }

        //---Hit Ball - recompensar quando tocam na bola, penalizar quando não tocam
        float HitBallValue;

        if (hitTheBall == 0)
        {
            HitBallValue = -hitBallW * 200;
        }
        else
        {
            HitBallValue = hitBallW * hitTheBall;
        }


        //---Distância do jogador à baliza
        float insideGoalCount = 0;

        for (int i = 0; i < distanceToMyGoal.Count; i++)
        {
            if (distanceToMyGoal[i] == 0)
            {
                insideGoalCount++;
            }
        }


        float distToMyGoalValue;

        if (insideGoalCount > 2) //--penaliza quando está mais de 2 vezes dentro da baliza
        {
            distToMyGoalValue = myDistToMyGoalW * -10;
        }
        else
        {
            distToMyGoalValue = myDistToMyGoalW * 10;
        }


        //--- fitness final
        float controlfitness = myDistToBallW * distToBallCount + distToMyGoalValue + HitBallValue;

        return controlfitness;
    }

    float defendFitnessAdversary()
    {
        float fitness = 0;
        return fitness;
    }

    float kickFitnessAdversary()
    {
        float fitness = 0;
        return fitness;
    }

    float controlFitnessAdversary()
    {
        float fitness = 0;
        return fitness;
    }

    public float StandartDev(List<float> values)
    {
        float mean = values.Sum() / values.Count;
        float sumSquares = 0;

        for(int i = 0; i < values.Count; i++)
        {
            sumSquares = sumSquares + ((values[i] - mean) * (values[i] - mean));
        }

        return (float)Math.Sqrt(sumSquares / (values.Count - 1));
    }

}