using UnityEngine;
using UnityEngine.UI;

namespace AgentDebugTool.Scripts.Trackers
{
    /// <summary>
    /// Engine information tracker
    /// Any statistic related to engine or game play can be updated here
    /// </summary>
    public class EngineTracker : UpdatableTracker
    {
        private const string LINE_START_TIMESCALE = "Timescale: x{0}";

        private float lastTimeScaleValue = 1;
        protected override float UpdateInterval => 1f;

        public EngineTracker(AgentDebugTool reference, Text counterText) : base(reference, counterText) { }

        protected override void ActivationActions()
        {
            base.ActivationActions();
            
            // Unsubscribing to prevent double subscribing
            TimeScaleUtility.OnTimescaleChanged -= HandleTimeScaleChanged;
            TimeScaleUtility.OnTimescaleChanged += HandleTimeScaleChanged;

            // Initial display of timescale value
            if (TimeScaleUtility.Instance != null)
            {
                HandleTimeScaleChanged(TimeScaleUtility.Instance, Time.timeScale);
            }
        }

        protected override void DeactivationActions()
        {
            base.DeactivationActions();
            TimeScaleUtility.OnTimescaleChanged -= HandleTimeScaleChanged;
        }

        private void HandleTimeScaleChanged(TimeScaleUtility timeScale, float scale)
        {
            UpdateValueAndDisplay(true);
        }

        protected override void UpdateValue(bool force)
        {
            if (lastTimeScaleValue != Time.timeScale || force)
            {
                lastTimeScaleValue = Time.timeScale;
                dirty = true;
            }
            
            if (dirty || force)
            {
                //Resets the text builder length
                text.Length = 0;
                
                // Adds timescale information
                text.AppendFormat(LINE_START_TIMESCALE, lastTimeScaleValue);
            }
        }
    }
}