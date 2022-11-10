using UnityEngine;

namespace ML_Agents.Examples.Hallway.Scripts
{
    public class AreaManager : MonoBehaviour
    {
        [SerializeField] private Symbol[] questionSymbols;
        [SerializeField] private PressurePad[] pressurePads;


        /// <summary>
        /// Randomizes question and pressure pad sensors by performing random rotations
        /// </summary>
        /// <returns>If there are more Xs</returns>
        public bool Randomize()
        {
            // Randomize question symbols
            int amountXsAreMoreThanOs = 0;
            
            foreach (Symbol questionSymbol in questionSymbols)
            {
                bool isX = Random.Range(0, 2) == 1;
                questionSymbol.SetSymbol(isX);
                amountXsAreMoreThanOs += isX ? 1 : -1;
            }

            // Should flip symbols associated with pressure pads
            if (Random.Range(0, 2) == 1) 
            {
                foreach (PressurePad pressurePad in pressurePads)
                {
                    pressurePad.AssociatedSymbol.SetSymbol(!pressurePad.AssociatedSymbol.IsSymbolX);
                }
            }

            // Should rotate the area itself
            if (Random.Range(0, 2) == 1)
            {
                transform.Rotate(0, 180, 0);
            }

            return amountXsAreMoreThanOs > 0;
        }
    }
}