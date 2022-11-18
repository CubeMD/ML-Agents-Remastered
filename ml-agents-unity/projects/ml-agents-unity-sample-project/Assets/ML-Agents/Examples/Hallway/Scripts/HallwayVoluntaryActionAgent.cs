using System;
using System.Collections;
using System.Collections.Generic;
using AgentDebugTool.Scripts.Agent;
using Tools;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ML_Agents.Examples.Hallway.Scripts
{
    public class HallwayVoluntaryActionAgent : DebuggableAgent
    {
        public class HallwayAgentAction
        {
            public int discreteAction;
            public float delay;

            public HallwayAgentAction(int discreteAction, float delay)
            {
                this.discreteAction = discreteAction;
                this.delay = delay;
            }
        }
        
        public class HallwayAgentObservation
        {
            public float[] vectorObservations;
            public List<float[]> objectObservations;
            
            public HallwayAgentObservation(float[] vectorObservations, List<float[]> objectObservations)
            {
                this.vectorObservations = vectorObservations;
                this.objectObservations = objectObservations;
            }
        }

        public class AgentStep
        {
            public HallwayAgentAction hallwayAgentAction;
            public HallwayAgentObservation hallwayAgentObservation;
            public float reward;

            public AgentStep(HallwayAgentObservation hallwayAgentObservation, HallwayAgentAction hallwayAgentAction, float reward)
            {
                this.hallwayAgentObservation = hallwayAgentObservation;
                this.hallwayAgentAction = hallwayAgentAction;
                this.reward = reward;
            }
        }

        [SerializeField] private AreaManager areaManager;

        [SerializeField] private Rigidbody agentRigidbody;
        [SerializeField] private BufferSensorComponent questionSensor;
    
        [SerializeField] private Renderer groundRenderer;

        [SerializeField] private HallwaySettings hallwaySettings;
        [SerializeField] private bool isHuman;
        [SerializeField] private bool drawBufferSensorMonitor;
        [SerializeField] private Vector3 minMeanMaxActionDelay;
        [SerializeField] private float maxEpisodeLengthInSeconds;
        
        private bool rightAnswerIsX;
        private float timeSinceLastDecision;
        private float timeSinceBeginningOfEpisode;
        private HallwayAgentAction decidedAgentAction = new HallwayAgentAction(0, 0);
        private readonly List<AgentStep> episodeTrajectory = new List<AgentStep>();


        public override void Initialize()
        {
            Academy.Instance.AutomaticSteppingEnabled = false;
            
        }

        public void Start()
        {
            Academy.Instance.EnvironmentStep();
        }

        public override void OnEpisodeBegin()
        {
            rightAnswerIsX = areaManager.Randomize();
            transform.localPosition = Vector3.up + Vector3.right * (5 * Random.value) + Vector3.forward * (5 * Random.value);
            transform.rotation = Quaternion.Euler(0f, Random.value * 360f, 0f);
            agentRigidbody.velocity = Vector3.zero;
            timeSinceLastDecision = 0;
            timeSinceBeginningOfEpisode = 0;
            decidedAgentAction = new HallwayAgentAction(0, 0);
            
            if (isHuman)
            {
                episodeTrajectory.Clear();
            }
            // else
            // {
            //     RequestDecision();
            //     Academy.Instance.EnvironmentStep();
            // }
        }
        
        public void FixedUpdate()
        {
            timeSinceBeginningOfEpisode += Time.fixedDeltaTime;
            timeSinceLastDecision += Time.fixedDeltaTime;
            
            if (timeSinceBeginningOfEpisode >= maxEpisodeLengthInSeconds)
            {
                AddReward(-1f);
                if (isHuman)
                {
                    DumpTrajectoryIntoHeuristicAndEndEpisode();
                }
                else
                {
                    EndEpisode();
                }
                
            }
            else if (isHuman)
            {
                if (timeSinceLastDecision >= minMeanMaxActionDelay.z || decidedAgentAction.delay != 0)
                {
                    AddReward(-0.1f);
                    decidedAgentAction.delay = timeSinceLastDecision;
                    episodeTrajectory.Add(new AgentStep(GetAgentEnvironmentObservation(), decidedAgentAction, Reward));
                    timeSinceLastDecision = 0;
                    decidedAgentAction = new HallwayAgentAction(decidedAgentAction.discreteAction, 0);
                }
            }
            else if (!isHuman)
            {
                if (timeSinceLastDecision >= decidedAgentAction.delay)
                {
                    timeSinceLastDecision = 0;
                    AddReward(-0.1f);
                    RequestDecision();
                    Academy.Instance.EnvironmentStep();
                }
            }
            
            InteractWithEnvironment(decidedAgentAction);
        }

        public void Update()
        {
            if (isHuman)
            {
                int humanAgentAction = 0;
            
                if (Input.GetKey(KeyCode.W))
                {
                    humanAgentAction = 1;
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    humanAgentAction = 2;
                }
                else if (Input.GetKey(KeyCode.A))
                {
                    humanAgentAction = 3;
                }

                if (decidedAgentAction.discreteAction != humanAgentAction)
                {
                    decidedAgentAction = new HallwayAgentAction(humanAgentAction, timeSinceLastDecision);
                }
            }
        }
        
        public HallwayAgentObservation GetAgentEnvironmentObservation()
        {
            Monitor.RemoveAllValuesFromAllTransforms();
            
            float[] vectorObservations =
            {
                timeSinceBeginningOfEpisode / maxEpisodeLengthInSeconds,
                agentRigidbody.velocity.x / hallwaySettings.agentRunSpeed / 15,
                agentRigidbody.velocity.z / hallwaySettings.agentRunSpeed / 15,
                agentRigidbody.angularVelocity.y,
                transform.localPosition.x / 10,
                transform.localPosition.z / 25,
                transform.rotation.eulerAngles.y / 180 - 1
            };

            observationsDebugSet.Add("Time: ", $"{vectorObservations[0]}");
            observationsDebugSet.Add("Vel X: ", $"{vectorObservations[1]}");
            observationsDebugSet.Add("Vel z: ", $"{vectorObservations[2]}");
            observationsDebugSet.Add("Ang Vel: ", $"{vectorObservations[3]}");
            observationsDebugSet.Add("Pos X: ", $"{vectorObservations[4]}");
            observationsDebugSet.Add("Pos Z: ", $"{vectorObservations[5]}");
            observationsDebugSet.Add("Rot: ", $"{vectorObservations[6]}");
            
            List<float[]> objectObservations = new List<float[]>();

            foreach (Collider sensedCollider in Physics.OverlapSphere(transform.position, hallwaySettings.agentSensorRadius))     
            {
                if (sensedCollider.transform.parent.TryGetComponent(out Symbol symbol))
                {
                    Vector3 relativeNormalizedObjectPosition = transform.InverseTransformPoint(symbol.transform.position) / hallwaySettings.agentSensorRadius;
                    
                    float[] observation =
                    {
                        1, 
                        symbol.IsSymbolX ? 1 : -1, 
                        relativeNormalizedObjectPosition.x,
                        relativeNormalizedObjectPosition.z
                    };
                    
                    objectObservations.Add(observation);
                    
                    if (drawBufferSensorMonitor)
                    {
                        Monitor.Log("Data: ", string.Join(" ", observation), symbol.transform);
                        Monitor.Log("Type: ", "Symbol", symbol.transform);
                    }
                }   
                else if (sensedCollider.transform.parent.TryGetComponent(out PressurePad pressurePad))
                {
                    Vector3 relativeNormalizedObjectPosition = transform.InverseTransformPoint(pressurePad.transform.position) / hallwaySettings.agentSensorRadius;
                    float[] observation = 
                    { 
                        0, 
                        pressurePad.AssociatedSymbol.IsSymbolX ? 1 : -1, 
                        relativeNormalizedObjectPosition.x, 
                        relativeNormalizedObjectPosition.z
                    };
                    
                    objectObservations.Add(observation);
                    
                    if (drawBufferSensorMonitor)
                    {
                        Monitor.Log("Data: ", string.Join(" ", observation), pressurePad.transform);
                        Monitor.Log("Type: ", "Pressure pad", pressurePad.transform);
                    }
                }
            }
            BroadcastObservationsCollected();
            return new HallwayAgentObservation(vectorObservations, objectObservations);
        }
        
        public override void CollectObservations(VectorSensor sensor)
        {
            HallwayAgentObservation hallwayAgentObservation = isHuman ? episodeTrajectory[0].hallwayAgentObservation : GetAgentEnvironmentObservation();
            
            foreach (float observation in hallwayAgentObservation.vectorObservations)
            {
                sensor.AddObservation(observation);
            }

            foreach (float[] objectObservation in hallwayAgentObservation.objectObservations)     
            {
                questionSensor.AppendObservation(objectObservation);
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            if (isHuman)
            {
                ActionSegment<int> discreteActionsOut = actionsOut.DiscreteActions;
                ActionSegment<float> continuousActionsOut = actionsOut.ContinuousActions;

                discreteActionsOut[0] = episodeTrajectory[0].hallwayAgentAction.discreteAction;
                continuousActionsOut[0] = episodeTrajectory[0].hallwayAgentAction.delay < minMeanMaxActionDelay.y ?
                        Mathf.InverseLerp(minMeanMaxActionDelay.x, minMeanMaxActionDelay.y, episodeTrajectory[0].hallwayAgentAction.delay) - 1 :
                        Mathf.InverseLerp(minMeanMaxActionDelay.y, minMeanMaxActionDelay.z, episodeTrajectory[0].hallwayAgentAction.delay);
                
                SetReward(episodeTrajectory[0].reward);
            }
        }
    
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            base.OnActionReceived(actionBuffers);
            timeSinceLastDecision = 0;

            if (!isHuman)
            {
                float desiredDelay = actionBuffers.ContinuousActions[0] < 0 ? 
                    Mathf.Lerp(minMeanMaxActionDelay.x, minMeanMaxActionDelay.y, actionBuffers.ContinuousActions[0] + 1) : 
                    Mathf.Lerp(minMeanMaxActionDelay.y, minMeanMaxActionDelay.z, actionBuffers.ContinuousActions[0]);
                
                decidedAgentAction = new HallwayAgentAction(actionBuffers.DiscreteActions[0], desiredDelay);
            }
        }

        public void InteractWithEnvironment(HallwayAgentAction hallwayAgentAction)
        {
            Vector3 rotateDir = Vector3.zero;
            Vector3 dirToGo = Vector3.zero;
            
            int action = hallwayAgentAction.discreteAction;
            AddReward(-0.01f);

            switch (action)
            {
                case 1:
                    dirToGo = transform.forward;
                    break;
                case 2:
                    rotateDir = transform.up * 1f;
                    break;
                case 3:
                    rotateDir = transform.up * -1f;
                    break;
            }
            
            transform.Rotate(rotateDir, Time.fixedDeltaTime * hallwaySettings.agentRotationSpeed);
            agentRigidbody.AddForce(dirToGo * hallwaySettings.agentRunSpeed, ForceMode.VelocityChange);
        }

        public void DumpTrajectoryIntoHeuristicAndEndEpisode()
        {
            int count = episodeTrajectory.Count - 1;
            for (int index = 0; index < count; index++)
            {
                RequestDecision();
                Academy.Instance.EnvironmentStep();
                episodeTrajectory.RemoveAt(0);
            }
            
            EndEpisode();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("wall"))
            {
                AddReward(-0.05f * Time.fixedDeltaTime);
            }
        }
        
        private void OnCollisionStay(Collision collision)
        {
            if (collision.collider.CompareTag("wall"))
            {
                AddReward(-0.05f * Time.fixedDeltaTime);
            }
        }

        private void OnTriggerEnter(Collider target)
        {
            if (target.transform.parent.TryGetComponent(out PressurePad pressurePad))
            {
                if ((!rightAnswerIsX && !pressurePad.AssociatedSymbol.IsSymbolX) ||
                    (rightAnswerIsX && pressurePad.AssociatedSymbol.IsSymbolX))
                {
                    //AddReward(2f);
                    StartCoroutine(SwapMaterialForRenderer(groundRenderer, 0.5f, hallwaySettings.goalScoredMaterial));
                }
                else
                {
                    //AddReward(-0.1f);
                    StartCoroutine(SwapMaterialForRenderer(groundRenderer, 0.5f, hallwaySettings.failMaterial));
                }

                if (isHuman)
                {
                    DumpTrajectoryIntoHeuristicAndEndEpisode();
                }
                else
                {
                    EndEpisode();
                }
            }
        }

        IEnumerator SwapMaterialForRenderer(Renderer ren, float time, Material material)
        {
            Material defaultMaterial = ren.material;
        
            ren.material = material;
            yield return new WaitForSeconds(time);
            ren.material = defaultMaterial;
        }
    }
}
