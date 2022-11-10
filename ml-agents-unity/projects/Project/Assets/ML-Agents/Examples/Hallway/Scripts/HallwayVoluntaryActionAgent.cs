using System;
using System.Collections;
using System.ComponentModel.Design;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ML_Agents.Examples.Hallway.Scripts
{
    public class HallwayVoluntaryActionAgent : Agent
    {
        public class HallwayAgentAction
        {
            public int discreteAction;
            public float delay;

            public HallwayAgentAction()
            {
                
            }
            
            public HallwayAgentAction(int discreteAction, float delay)
            {
                this.discreteAction = discreteAction;
                this.delay = delay;
            }
        }
        
        [SerializeField] private AreaManager areaManager;

        [SerializeField] private Rigidbody agentRigidbody;
        [SerializeField] private BufferSensorComponent questionSensor;
    
        [SerializeField] private Renderer groundRenderer;

        [SerializeField] private HallwaySettings hallwaySettings;
        [SerializeField] private bool isHuman;
        [SerializeField] private Vector3 minMeanMaxActionDelay;
        [SerializeField] private float maxEpisodeLengthInSeconds;
        
        private bool rightAnswerIsX;
        private float timeSinceLastDecision;
        private float timeSinceBeginningOfEpisode;
        private HallwayAgentAction CompletedAgentAction;

        public override void OnEpisodeBegin()
        {
            rightAnswerIsX = areaManager.Randomize();
            transform.localPosition = Vector3.up + Vector3.right * (5 * Random.value) + Vector3.forward * (5 * Random.value);
            transform.rotation = Quaternion.Euler(0f, Random.value * 360f, 0f);
            agentRigidbody.velocity = Vector3.zero;
            timeSinceLastDecision = 0;
            timeSinceBeginningOfEpisode = 0;
            CompletedAgentAction = new HallwayAgentAction();
        }

        public override void Initialize()
        {
            Academy.Instance.AgentPreStep += MakeRequests;
        }
        
        protected void OnDestroy()
        {
            if (Academy.IsInitialized)
            {
                Academy.Instance.AgentPreStep -= MakeRequests;
            }
        }
    
        private void MakeRequests(int academyStepCount)
        {
            timeSinceBeginningOfEpisode += Time.fixedDeltaTime;
            
            if (timeSinceBeginningOfEpisode >= maxEpisodeLengthInSeconds)
            {
                AddReward(-1f);
                EndEpisode();
            }
            
            if (isHuman && CompletedAgentAction != null)
            {
                RequestDecision();
            }
            else if (!isHuman)
            {
                timeSinceLastDecision += Time.fixedDeltaTime;

                if (CompletedAgentAction == null)
                {
                    CompletedAgentAction = new HallwayAgentAction();
                    timeSinceLastDecision = 0;
                    RequestDecision();
                }
                else if (timeSinceLastDecision >= CompletedAgentAction.delay)
                {
                    timeSinceLastDecision = 0;
                    RequestDecision();
                }
                else
                {
                    InteractWithEnvironment(CompletedAgentAction);
                }
            }
        }

        public void Update()
        {
            if (isHuman && CompletedAgentAction == null)
            {
                timeSinceLastDecision += Time.deltaTime;
                
                int humanAgentAction;
            
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
                else
                {
                    return;
                }

                CompletedAgentAction = new HallwayAgentAction(humanAgentAction, timeSinceLastDecision);
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(timeSinceBeginningOfEpisode / maxEpisodeLengthInSeconds);
            sensor.AddObservation(agentRigidbody.velocity.x / hallwaySettings.agentRunSpeed / 15);
            sensor.AddObservation(agentRigidbody.velocity.z / hallwaySettings.agentRunSpeed / 15);
            sensor.AddObservation(agentRigidbody.angularVelocity.y);
            sensor.AddObservation(transform.localPosition.x / 10);
            sensor.AddObservation(transform.localPosition.z / 25);
            sensor.AddObservation(transform.rotation.eulerAngles.y / 180 - 1);

            Collider[] sensedObjects = Physics.OverlapSphere(transform.position, hallwaySettings.agentSensorRadius);
            
            if (sensedObjects.Length > 0)
            {
                foreach (Collider sensedCollider in sensedObjects)     
                {
                    if (sensedCollider.transform.parent.TryGetComponent(out Symbol symbol))
                    {
                        // symbol.SetSensedMaterialRoutine(SwapMaterialForRenderer(symbol.ren, 0.5f, hallwaySettings.failMaterial));;
                        Vector3 relativeNormalizedObjectPosition = transform.InverseTransformPoint(symbol.transform.position) / hallwaySettings.agentSensorRadius;
                        float[] observation = { 1, symbol.IsSymbolX ? 1 : -1, relativeNormalizedObjectPosition.x, relativeNormalizedObjectPosition.z};
                        questionSensor.AppendObservation(observation);
                    }   
                    else if (sensedCollider.transform.parent.TryGetComponent(out PressurePad pressurePad))
                    {
                        // StartCoroutine(SwapMaterialForRenderer(pressurePad.ren, 0.5f, hallwaySettings.failMaterial));
                        Vector3 relativeNormalizedObjectPosition = transform.InverseTransformPoint(pressurePad.transform.position) / hallwaySettings.agentSensorRadius;
                        float[] observation = { 0, pressurePad.AssociatedSymbol.IsSymbolX ? 1 : -1, relativeNormalizedObjectPosition.x, relativeNormalizedObjectPosition.z};
                        questionSensor.AppendObservation(observation);
                    }
                }
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            if (isHuman)
            {
                ActionSegment<int> discreteActionsOut = actionsOut.DiscreteActions;
            
                if (Input.GetKey(KeyCode.W))
                {
                    discreteActionsOut[0] = 1;
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    discreteActionsOut[0] = 2;
                }
                else if (Input.GetKey(KeyCode.A))
                {
                    discreteActionsOut[0] = 3;
                }
            }
        }
    
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            AddReward(Time.fixedDeltaTime / maxEpisodeLengthInSeconds);
            timeSinceLastDecision = 0;

            if (!isHuman)
            {
                float desiredDelay = actionBuffers.ContinuousActions[0] < 0 ? 
                    Mathf.Lerp(minMeanMaxActionDelay.x, minMeanMaxActionDelay.y, actionBuffers.ContinuousActions[0] + 1) : 
                    Mathf.Lerp(minMeanMaxActionDelay.y, minMeanMaxActionDelay.z, actionBuffers.ContinuousActions[0]);
                
                CompletedAgentAction = new HallwayAgentAction(actionBuffers.DiscreteActions[0], desiredDelay);
            }
        }

        public void InteractWithEnvironment(HallwayAgentAction hallwayAgentAction)
        {
            Vector3 rotateDir = Vector3.zero;
            Vector3 dirToGo = Vector3.zero;
            
            int action = hallwayAgentAction.discreteAction;
            AddReward(action == 1 ? 0 : -0.25f * Time.fixedDeltaTime / maxEpisodeLengthInSeconds);
            
            
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
                    AddReward(2f);
                    StartCoroutine(SwapMaterialForRenderer(groundRenderer, 0.5f, hallwaySettings.goalScoredMaterial));
                }
                else
                {
                    AddReward(-0.1f);
                    StartCoroutine(SwapMaterialForRenderer(groundRenderer, 0.5f, hallwaySettings.failMaterial));
                }

                EndEpisode();
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
