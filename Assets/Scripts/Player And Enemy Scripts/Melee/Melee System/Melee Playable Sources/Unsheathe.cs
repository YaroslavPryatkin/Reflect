using UnityEngine;
using System.Collections.Generic;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "Unsheathe", menuName = "Melee/Specific Part Holders/Unsheathe")]
    public class Unsheathe : MeleePlayableSource
    {
        [SerializeField] private Transition transition;
        [SerializeField] private MeleeAnimation meleeAnimation;

        [SerializeField, HideInInspector] private List<MeleePlayablePart> parts = new();

        private void OnValidate()
        {
            parts.Clear();
            if (transition != null && meleeAnimation != null)
            {
                parts.Add(transition);
                parts.Add(meleeAnimation);
            }
        }

        public override List<MeleePlayablePart> Parts => parts;
    }
}