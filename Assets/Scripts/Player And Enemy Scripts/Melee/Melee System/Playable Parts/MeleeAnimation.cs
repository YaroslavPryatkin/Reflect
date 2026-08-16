using System.Collections.Generic;
using UnityEngine;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "Animation", menuName = "Melee/Part/Animation")]
    public class MeleeAnimation : MeleePlayablePart
    {
        [SerializeField] private AnimationClip clip;
        
        [SerializeField, HideInInspector] private bool hasClip;
        [SerializeField, HideInInspector] private float clipSpeed;
        public override AnimationClip Clip => clip;
        public override float ClipSpeed => clipSpeed;
        public override bool HasClip => hasClip;

        private void OnValidate()
        {
            hasClip = clip != null;

            if (hasClip)
                clipSpeed = UtilityFunctions.GetAnimationSpeed(clip, Duration);
        }
    }
}
