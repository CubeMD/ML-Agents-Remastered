using System;
using AgentDebugTool.Scripts.Utilities;
using UnityEngine;

namespace AgentDebugTool.Scripts
{
    public class TimeScaleUtility : Tools.Singleton<TimeScaleUtility>
    {
        private const float TIMESCALE_UPPER_LIMIT = 100f;
        public static event Action<TimeScaleUtility, float> OnTimescaleChanged;
        
        [SerializeField]
        private float timeScaleStep = 1f;

        [SerializeField]
        private KeyCode timescaleIncreaseKey = KeyCode.KeypadPlus;
        [SerializeField]
        private KeyCode timescaleDecreaseKey = KeyCode.KeypadMinus;
        
        private void Update()
        {
            if (Input.GetKeyDown(timescaleIncreaseKey))
            {
                SetTimeScale(Time.timeScale + timeScaleStep);
            }
            else if (Input.GetKeyDown(timescaleDecreaseKey))
            {
                SetTimeScale(Time.timeScale - timeScaleStep);
            }
        }
        
        /// <summary>
        /// Sets the timescale to the given value according to the limits
        /// Invokes OnTimescaleChanged event
        /// </summary>
        /// <param name="timeScaleToSet">Timescale to set</param>
        public void SetTimeScale(float timeScaleToSet)
        {
            if (Time.timeScale != timeScaleToSet)
            {
                Time.timeScale = Mathf.Clamp(timeScaleToSet, 0, TIMESCALE_UPPER_LIMIT);
                OnTimescaleChanged?.Invoke(this, Time.timeScale);
            }
        }
    }
}