using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;

namespace AgentDebugTool.Scripts.Agent
{
    /// <summary>
    /// Custom Agent class that allows 
    /// </summary>
    public abstract class DebuggableAgent : Unity.MLAgents.Agent
    {
        public static event Action<DebuggableAgent> OnAnyAgentEnabled;
        public static event Action<DebuggableAgent> OnAnyAgentEpisodeBegin;
        public static event Action<DebuggableAgent, float> OnAnyAgentDecisionRequested;
        public static event Action<DebuggableAgent> OnAnyAgentDisabled;
        public static event Action<DebuggableAgent> OnAnyAgentDestroyed;
        
        public event Action<DebuggableAgent, ActionBuffers> OnAgentDecisionRequested;
        public event Action<DebuggableAgent, Dictionary<string, string>> OnAgentObservationsCollected;
        
        public int AgentID => transform.GetHashCode();

        protected readonly Dictionary<string, string> observationsDebugSet = new Dictionary<string, string>();
        private readonly Dictionary<string, float> episodeStatisticSet = new Dictionary<string, float>();

        protected float timeOfEpisodeStart;
        protected float timeOfLastDecision;
        
        private static float totalGameplaySeconds = 0;
        private static int totalDecisions = 0;
        private static int totalEpisodes = 0;
        
        /// <summary>
        /// When override must invoke base.OnEnable
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            OnAnyAgentEnabled?.Invoke(this);
        }
        
        /// <summary>
        /// When override must invoke base.OnDisable
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            OnAnyAgentDisabled?.Invoke(this);
        }

        /// <summary>
        /// When override must invoke base.OnDestroy
        /// </summary>
        protected virtual void OnDestroy()
        {
            OnAnyAgentDestroyed?.Invoke(this);
        }

        /// <summary>
        /// When override must invoke base.OnEpisodeBegin
        /// Sends end episode stats and updates a time of the episode start
        /// </summary>
        public override void OnEpisodeBegin()
        {
            totalEpisodes++;
            Academy.Instance.StatsRecorder.Add("Totals/Episodes", totalEpisodes, StatAggregationMethod.MostRecent);
            
            SendEndEpisodeStatistic();
            OnAnyAgentEpisodeBegin?.Invoke(this);
            timeOfEpisodeStart = timeOfLastDecision = Time.time;
        }

        /// <summary>
        /// Sends all recorded statistic to Academy Stats Recorder with Histogram aggregation method
        /// Cleans the episode statistic set
        /// </summary>
        /// <param name="clear">If false, the episode statistic set won't be cleared after sending</param>
        protected void SendEndEpisodeStatistic(bool clear = true)
        {
            foreach (string key in episodeStatisticSet.Keys)
            {
                Academy.Instance.StatsRecorder.Add(key, episodeStatisticSet[key], StatAggregationMethod.Histogram);
            }

            if (clear)
            {
                HashSet<string> keys = new HashSet<string>(episodeStatisticSet.Keys);
                foreach (string key in keys)
                {
                    episodeStatisticSet[key] = 0;
                }
            }
        }
        
        /// <summary>
        /// Records episode statistic value to episode statistic set
        /// If such ID exists in the set, the value will be added to sum
        /// Otherwise, the KeyPair with given ID and value will be created
        /// </summary>
        /// <param name="id">Statistic ID</param>
        /// <param name="value">Statistic value to add</param>
        protected void AddEpisodeStatisticRecord(string id, float value = 1f)
        {
            if (episodeStatisticSet.ContainsKey(id))
            {
                episodeStatisticSet[id] += value;
            }
            else
            {
                episodeStatisticSet.Add(id, value);
            }
        }
        
        /// <summary>
        /// When override must invoke base.OnActionReceived or manually invoke BroadcastDecisionRequested()
        /// </summary>
        public override void OnActionReceived(ActionBuffers actions)
        {
            BroadcastDecisionRequested(actions);
        }

        /// <summary>
        /// Invokes events related to decision requested
        /// Adds episode statistic record for decisions
        /// </summary>
        /// <param name="actions">Action buffer</param>
        protected void BroadcastDecisionRequested(ActionBuffers actions)
        {
            OnAnyAgentDecisionRequested?.Invoke(this, Time.time - timeOfLastDecision);
            OnAgentDecisionRequested?.Invoke(this, actions);
            CollectStatisticOnDecisionRequest();
  
            timeOfLastDecision = Time.time;
        }

        private void CollectStatisticOnDecisionRequest()
        {
            totalDecisions++;
            totalGameplaySeconds += timeOfLastDecision;
            Academy.Instance.StatsRecorder.Add("Totals/Decisions", totalDecisions, StatAggregationMethod.MostRecent);
            Academy.Instance.StatsRecorder.Add("Totals/GameplaySeconds", totalGameplaySeconds, StatAggregationMethod.MostRecent);
            
            AddEpisodeStatisticRecord("PerEpisode/Decisions");
        }

        /// <summary>
        /// Invokes OnAgentObservationsCollected event with observationsDebugSet
        /// </summary>
        protected void BroadcastObservationsCollected()
        {
            OnAgentObservationsCollected?.Invoke(this, observationsDebugSet);
            observationsDebugSet.Clear();
        }
    }
}