using System.Collections;
using System.Collections.Generic;
using System.Text;
using AgentDebugTool.Scripts.Agent;
using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEngine.UI;

namespace AgentDebugTool.Scripts.Trackers
{
    public class AgentTracker : BaseTracker
    {
        private const string LINE_AGENT_NAME = "Agent: {0}";
        private const string LINE_ACTIONS = "Actions:";
        private const string SUBMENU_SPACE = "  ";
        private const string LINE_ACTIONS_CONT = "Continuous: ";
        private const string LINE_ACTIONS_DESC = "Discrete: ";
        private const string ACTION = "{0} ";
        private const string NOT_APPLICABLE = "N/A";
        private const string LINE_CUMULATIVE_REWARD = "Cumulative Reward: {0}";
        private const string LINE_OBSERVATIONS = "Observations:";
        private const string OBSERVATION = "{0}: {1}";
        
        private const float FADE_OUT_DURATION = 0.2f;
        
        private DebuggableAgent currentDebuggableAgent;
        private readonly StringBuilder actionsString;
        private readonly StringBuilder observationsString;
        private readonly CanvasGroup decisionText;
        private readonly Toggle pauseOnDecisionToggle;
        private Coroutine decisionFadeRoutine;
        private bool pauseOnDecision;
        private float lastCumulativeReward = 0;
        
        public AgentTracker(AgentDebugTool reference, Text counterText, CanvasGroup decisionText, Toggle pauseToggle, DebuggableAgent debuggableAgent = null)
            : base(reference, counterText)
        {
            currentDebuggableAgent = debuggableAgent;
            this.decisionText = decisionText;
            pauseOnDecisionToggle = pauseToggle;
            actionsString = new StringBuilder(100);
            observationsString = new StringBuilder(100);
        }

        protected override void ActivationActions()
        {
            base.ActivationActions();
            SubscribeToEvents();

            if (pauseOnDecisionToggle != null)
            {
                pauseOnDecisionToggle.onValueChanged.AddListener(HandlePauseToggleValueChanged);
            }
            
            AgentSelector.OnNewAgentSelected += HandleNewAgentSelected;
            decisionText.alpha = 0;
        }

        protected override void DeactivationActions()
        {
            base.DeactivationActions();
            UnsubscribeFromEvents();
            
            if (pauseOnDecisionToggle != null)
            {
                pauseOnDecisionToggle.onValueChanged.RemoveListener(HandlePauseToggleValueChanged);
            }
            
            AgentSelector.OnNewAgentSelected -= HandleNewAgentSelected;
            StopFadeRoutine();
        }

        private void StopFadeRoutine()
        {
            if (decisionFadeRoutine != null && agentDebugTool != null)
            {
                agentDebugTool.StopCoroutine(decisionFadeRoutine);
            }
        }

        private void HandleNewAgentSelected(AgentSelector agentSelector, DebuggableAgent debuggableAgent)
        {
            UnsubscribeFromEvents();
            currentDebuggableAgent = debuggableAgent;
            SubscribeToEvents();
            actionsString.Length = 0;
            observationsString.Length = 0;
            StopFadeRoutine();
            decisionText.alpha = 0;
            UpdateValueAndDisplay(true);
        }

        private void SubscribeToEvents()
        {
            if (currentDebuggableAgent != null)
            {
                currentDebuggableAgent.OnAgentDecisionRequested += HandleDebuggableAgentDecisionRequested;
                currentDebuggableAgent.OnAgentObservationsCollected += HandleDebuggableAgentObservationsCollected;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (currentDebuggableAgent != null)
            {
                currentDebuggableAgent.OnAgentDecisionRequested -= HandleDebuggableAgentDecisionRequested;
                currentDebuggableAgent.OnAgentObservationsCollected -= HandleDebuggableAgentObservationsCollected;
            }
        }

        private void HandleDebuggableAgentDecisionRequested(DebuggableAgent debuggableAgent, ActionBuffers actionBuffers)
        {
            actionsString.Length = 0;
            actionsString
                .Append(LINE_ACTIONS)
                .Append(NEW_LINE)
                .Append(SUBMENU_SPACE)
                .Append(LINE_ACTIONS_CONT);

            if (actionBuffers.ContinuousActions.Length < 1)
            {
                actionsString.Append(NOT_APPLICABLE);
            }
            else
            {
                foreach (float action in actionBuffers.ContinuousActions)
                {
                    actionsString.AppendFormat(ACTION, action.ToString("0.0000"));
                }
            }

            actionsString
                .Append(NEW_LINE)
                .Append(SUBMENU_SPACE)
                .Append(LINE_ACTIONS_DESC);
            
            if (actionBuffers.DiscreteActions.Length < 1)
            {
                actionsString.Append(NOT_APPLICABLE);
            }
            else
            {
                foreach (int action in actionBuffers.DiscreteActions)
                {
                    actionsString.AppendFormat(ACTION, action.ToString("0.0000"));
                }
            }
            
            lastCumulativeReward = currentDebuggableAgent.GetCumulativeReward();

            StopFadeRoutine();
            if (agentDebugTool != null)
            {
                decisionText.alpha = 1;
                decisionFadeRoutine = agentDebugTool.StartCoroutine(DecisionTextFade());
            }

            dirty = true;

            if (pauseOnDecision)
            {
                TimeScaleUtility.Instance.SetTimeScale(0);
            }
            
            UpdateValueAndDisplay(false);
        }

        private void HandleDebuggableAgentObservationsCollected(DebuggableAgent debuggableAgent, Dictionary<string, string> observations)
        {
            observationsString.Length = 0;
            observationsString.Append(LINE_OBSERVATIONS);

            foreach (KeyValuePair<string,string> observationPair in observations)
            {
                observationsString
                    .Append(NEW_LINE)
                    .Append(SUBMENU_SPACE)
                    .AppendFormat(OBSERVATION, observationPair.Key, observationPair.Value);
            }

            dirty = true;
            UpdateValueAndDisplay(false);
        }
        
        protected override void UpdateValue(bool force)
        {
            if (dirty || force)
            {
                //Resets the text builder length
                text.Length = 0;

                // Add agent name
                text.Append(string.Format(LINE_AGENT_NAME, currentDebuggableAgent.AgentID));

                // Adds action information
                text.Append(NEW_LINE)
                    .Append(actionsString);
                
                // Adds observation information
                text.Append(NEW_LINE)
                    .Append(observationsString);
                
                text.Append(NEW_LINE)
                    .AppendFormat(LINE_CUMULATIVE_REWARD, lastCumulativeReward.ToString("0.0"));
            }
        }
        
        private void HandlePauseToggleValueChanged(bool pause)
        {
            pauseOnDecision = pause;
        }

        private IEnumerator DecisionTextFade()
        {
            while (decisionText.alpha > 0)
            {
                decisionText.alpha = Mathf.Clamp01(decisionText.alpha - Time.deltaTime / FADE_OUT_DURATION);
                yield return null;
            }

            decisionFadeRoutine = null;
        }
    }
}