using UnityEngine;
using System.Collections.Generic;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "Sheathe", menuName = "Melee/Specific Part Holders/Sheathe")]
    public class Sheathe : MeleePlayableSource
    {
        [SerializeField] private Transition transition1;
        [SerializeField] private MeleeAnimation meleeAnimation;
        [SerializeField] private Transition transition2;

        [SerializeField, HideInInspector] private List<MeleePlayablePart> parts = new();

        private void OnValidate()
        {
            parts.Clear();
            if (transition1 != null && meleeAnimation != null && transition2 != null)
            {
                parts.Add(transition1);
                parts.Add(meleeAnimation);
                parts.Add(transition2);
            }
        }

        public override List<MeleePlayablePart> Parts => parts;
    }
}