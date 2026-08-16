using UnityEngine;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "Transition", menuName = "Melee/Part/Transition")]
    public class Transition : MeleePlayablePart
    {
        public override bool ShouldAnimationTransition => true;
    }
}