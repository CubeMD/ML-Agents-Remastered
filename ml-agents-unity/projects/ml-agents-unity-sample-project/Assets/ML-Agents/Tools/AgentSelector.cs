using System;
using System.Collections.Generic;
using AgentDebugTool.Scripts.Agent;
using UnityEngine;

namespace AgentDebugTool.Scripts
{
    /// <summary>
    /// Agent selector class that iterates between all existing and active agents using keyboard shortcuts
    /// Invokes static event OnNewAgentSelected upon new agent selected
    /// </summary>
    public class AgentSelector : MonoBehaviour
    {
        public static event Action<AgentSelector, DebuggableAgent> OnNewAgentSelected;
        
        [SerializeField]
        private KeyCode nextAgentKey = KeyCode.RightArrow;
        [SerializeField]
        private KeyCode previousAgentKey = KeyCode.LeftArrow;
        
        private int currentIndex = -1;
        private readonly List<DebuggableAgent> activeAgents = new List<DebuggableAgent>();

        private void Awake()
        { 
            DebuggableAgent.OnAnyAgentEnabled += HandleAnyAgentEnabled;
            DebuggableAgent.OnAnyAgentDisabled += HandleAnyAgentDisabled;
            DebuggableAgent.OnAnyAgentDestroyed += HandleAnyAgentDestroyed;
        }

        private void Start()
        {
            DebuggableAgent[] agents = FindObjectsOfType<DebuggableAgent>();

            foreach (DebuggableAgent agent in agents)
            {
                HandleAnyAgentEnabled(agent);
            }
        }

        private void OnDisable()
        {
            DebuggableAgent.OnAnyAgentEnabled -= HandleAnyAgentEnabled;
            DebuggableAgent.OnAnyAgentDisabled -= HandleAnyAgentDisabled;
            DebuggableAgent.OnAnyAgentDestroyed -= HandleAnyAgentDestroyed;
        }

        private void Update()
        {
            if (Input.GetKeyDown(previousAgentKey))
            {
                int newIndex = currentIndex - 1;
                SetNewIndex(newIndex < 0 ? activeAgents.Count - 1 : newIndex);
            }
            else if (Input.GetKeyDown(nextAgentKey))
            {
                int newIndex = currentIndex + 1;
                SetNewIndex(newIndex >= activeAgents.Count ? 0 : newIndex);
            }
        }

        /// <summary>
        /// Sets new agent index
        /// </summary>
        /// <param name="index">Index to set</param>
        private void SetNewIndex(int index)
        {
            if (index >= activeAgents.Count || activeAgents.Count == 0 || index < 0)
            {
                return;
            }

            currentIndex = index;
            OnNewAgentSelected?.Invoke(this, activeAgents[currentIndex]);
        }

        #region Handlers

        private void HandleAnyAgentEnabled(DebuggableAgent debuggableAgent)
        {
            if (!activeAgents.Contains(debuggableAgent))
            {
                activeAgents.Add(debuggableAgent);
                if (currentIndex < 0)
                {
                    SetNewIndex(0);
                }
            }
        }

        private void HandleAnyAgentDestroyed(DebuggableAgent debuggableAgent)
        {
            if (activeAgents.Contains(debuggableAgent))
            {
                activeAgents.Remove(debuggableAgent);
            }

            int newIndex = activeAgents.Count - 1 > currentIndex ? currentIndex : currentIndex - 1;
            SetNewIndex(newIndex);
        }
        
        private void HandleAnyAgentDisabled(DebuggableAgent debuggableAgent)
        {
            if (activeAgents.Contains(debuggableAgent))
            {
                activeAgents.Remove(debuggableAgent);
            }
            
            int newIndex = activeAgents.Count - 1 > currentIndex ? currentIndex : currentIndex - 1;
            SetNewIndex(newIndex);
        }

        #endregion
    }
}