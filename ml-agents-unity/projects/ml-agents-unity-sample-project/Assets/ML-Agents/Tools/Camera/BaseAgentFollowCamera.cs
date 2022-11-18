using AgentDebugTool.Scripts.Agent;
using AgentDebugTool.Scripts.Utilities;
using UnityEngine;

namespace AgentDebugTool.Scripts.Camera
{
    /// <summary>
    /// Base class for camera that follows the selected agent
    /// Subscribes to AgentSelector.OnNewAgentSelected event to update the position & rotation
    /// </summary>
    public abstract class BaseAgentFollowCamera : MonoBehaviour
    {
        private Unity.MLAgents.Agent agentToFollow;

        protected Vector3 AgentPosition => agentToFollow != null ? agentToFollow.transform.position : Vector3.zero;
        protected Quaternion AgentRotation => agentToFollow != null ? agentToFollow.transform.rotation : Quaternion.identity;
        
        /// <summary>
        /// When override must invoke base.Awake
        /// </summary>
        protected virtual void Awake()
        {
            AgentSelector.OnNewAgentSelected += HandleNewAgentSelected;
        }

        /// <summary>
        /// When override must invoke base.OnDestroy
        /// </summary>
        protected virtual void OnDestroy()
        {
            AgentSelector.OnNewAgentSelected -= HandleNewAgentSelected;
        }

        protected virtual void HandleNewAgentSelected(AgentSelector agentSelector, DebuggableAgent debuggableAgent)
        {
            agentToFollow = debuggableAgent;
            UpdatePosition();
        }

        protected abstract void UpdatePosition();
        
        protected Quaternion GetReferenceOrientation(Vector3 worldUp)
        {
            if (agentToFollow == null)
            {
                return Quaternion.LookRotation(Vector3.forward, worldUp);
            }
         
            Vector3 forward = (AgentRotation * Vector3.forward).ProjectOntoPlane(worldUp);
            return Quaternion.LookRotation(forward, worldUp);
        }
    }
}