using System.Collections.Generic;
using AgentDebugTool.Scripts.Trackers;
using UnityEngine;
using UnityEngine.UI;

namespace AgentDebugTool.Scripts
{
    /// <summary>
    /// Main agent debug class
    /// Should be present in the scene to work
    /// Initializes trackers and maintains their status
    /// </summary>
    public class AgentDebugTool : MonoBehaviour
    {
        [SerializeField]
        private Text agentDebug;
        [SerializeField]
        private CanvasGroup agentDecisionText;
        [SerializeField]
        private Toggle pauseOnDecisionToggle;
        [SerializeField]
        private Text gameplayTextField;

        protected readonly List<BaseTracker> trackers = new List<BaseTracker>();
        
        private void Awake()
        {
            // Initializes all counter types
            InitializeTrackers();
            ActivateTrackers();
        }

        /// <summary>
        /// Creates trackers and adds it to the trackers list
        /// When override must invoke base.InitializeTrackers
        /// </summary>
        protected virtual void InitializeTrackers()
        {
            // Initialize counters
            EngineTracker engineTracker = new EngineTracker(this, gameplayTextField);
            trackers.Add(engineTracker);
            
            AgentTracker agentTracker = new AgentTracker(this, agentDebug, agentDecisionText, pauseOnDecisionToggle);
            trackers.Add(agentTracker);
        }

        private void ActivateTrackers()
        {
            foreach (BaseTracker tracker in trackers)
            {
                tracker.Activate();
            }
        }

        private void OnDisable()
        {
            foreach (BaseTracker tracker in trackers)
            {
                tracker.Deactivate();
            }
        }

        private void OnDestroy()
        {
            foreach (BaseTracker tracker in trackers)
            {
                tracker.Terminate();
            }
            
            trackers.Clear();
        }
    }
}