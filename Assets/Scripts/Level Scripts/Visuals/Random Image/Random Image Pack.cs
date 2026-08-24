using UnityEngine;
using System.Collections.Generic;
using CustomAttributes;


[CreateAssetMenu(fileName = "RandomSpritePack", menuName = "Scriptable Objects/RandomSpritePack")]
public class RandomSpritePack : ScriptableObject
{
    [SerializeField] public Sprite[] sprites;
}
