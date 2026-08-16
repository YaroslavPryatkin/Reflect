using UnityEngine;
using System.Collections.Generic;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "PartHolder", menuName = "Melee/PartHolder")]
    public class PartHolder : MeleePlayableSource
    {
        [SerializeField] private List<MeleePlayablePart> parts = new();
        public override List<MeleePlayablePart> Parts => parts;
    }
}