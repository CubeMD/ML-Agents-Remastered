using System;
using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ML_Agents.Examples.Hallway.Scripts
{
    public class HallwayAgent : Agent
    {
        [SerializeField] private AreaManager areaManager;

        [SerializeField] private Rigidbody agentRigidbody;
        [SerializeField] private BufferSensorComponent questionSensor;
    
        [SerializeField] private Renderer groundRenderer;

        [SerializeField] private HallwaySettings hallwaySettings;
        
        private bool rightAnswerIsX;

        public override void OnEpisodeBegin()
        {
            rightAnswerIsX = areaManager.Randomize();
            transform.localPosition = Vector3.up;
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            agentRigidbody.velocity = Vector3.zero;
        }
    
        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(StepCount / (float)MaxStep);
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
                        Vector3 relativeNormalizedObjectPosition = transform.InverseTransformPoint(symbol.transform.position) / hallwaySettings.agentSensorRadius;
                        float[] observation = { 1, symbol.IsSymbolX ? 1 : -1, relativeNormalizedObjectPosition.x, relativeNormalizedObjectPosition.z};
                        questionSensor.AppendObservation(observation);
                    }   
                    else if (sensedCollider.transform.parent.TryGetComponent(out PressurePad pressurePad))
                    {
                        Vector3 relativeNormalizedObjectPosition = transform.InverseTransformPoint(pressurePad.transform.position) / hallwaySettings.agentSensorRadius;
                        float[] observation = { 0, pressurePad.AssociatedSymbol.IsSymbolX ? 1 : -1, relativeNormalizedObjectPosition.x, relativeNormalizedObjectPosition.z};
                        questionSensor.AppendObservation(observation);
                    }
                }
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<int> discreteActionsOut = actionsOut.DiscreteActions;
            
            if (Input.GetKey(KeyCode.W))
            {
                discreteActionsOut[0] = 0;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                discreteActionsOut[0] = 1;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                discreteActionsOut[0] = 2;
            }
        }
    
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            AddReward(-1f / MaxStep);
            
            Vector3 rotateDir = Vector3.zero;
            Vector3 dirToGo = Vector3.zero;
            
            int action = actionBuffers.DiscreteActions[0];
            
            switch (action)
            {
                case 0:
                    dirToGo = transform.forward;
                    break;
                case 1:
                    rotateDir = transform.up * 1f;
                    AddReward(-0.01f);
                    break;
                case 2:
                    rotateDir = transform.up * -1f;
                    AddReward(-0.01f);
                    break;
            }
            
            transform.Rotate(rotateDir, Time.fixedDeltaTime * hallwaySettings.agentRotationSpeed);
            agentRigidbody.AddForce(dirToGo * hallwaySettings.agentRunSpeed, ForceMode.VelocityChange);
        }
        
        private void OnCollisionStay(Collision collision)
        {
            if (collision.collider.CompareTag("wall"))
            {
                AddReward(-0.01f);
            }
        }

        private void OnTriggerEnter(Collider target)
        {
            if (target.transform.parent.TryGetComponent(out PressurePad pressurePad))
            {
                if ((!rightAnswerIsX && !pressurePad.AssociatedSymbol.IsSymbolX) ||
                    (rightAnswerIsX && pressurePad.AssociatedSymbol.IsSymbolX))
                {
                    AddReward(1f);
                    StartCoroutine(SwapGroundMaterial(0.5f, hallwaySettings.goalScoredMaterial));
                }
                else
                {
                    StartCoroutine(SwapGroundMaterial(0.5f, hallwaySettings.failMaterial));
                }

                EndEpisode();
            }
        }

        IEnumerator SwapGroundMaterial(float time, Material material)
        {
            Material defaultMaterial = groundRenderer.material;
        
            groundRenderer.material = material;
            yield return new WaitForSeconds(time);
            groundRenderer.material = defaultMaterial;
        }
    }
}
