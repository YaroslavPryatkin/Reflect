using UnityEngine;
using System.Collections.Generic;


[RequireComponent(typeof(SpriteRenderer))]
public class RandomSpriteSetter : MonoBehaviour
{
    [SerializeField] private RandomSpritePack spritePack;
    private static readonly Dictionary<RandomSpritePack, int> NonRepeating = new();

    private void Awake()
    {
        if (NonRepeating.TryGetValue(spritePack, out var index))
        {
            GetComponent<SpriteRenderer>().sprite = spritePack.sprites.GetRandomElement(ref index);
            NonRepeating[spritePack] = index;
        }
        else
        {
            index = 0;
            GetComponent<SpriteRenderer>().sprite = spritePack.sprites.GetRandomElement(ref index);
            NonRepeating[spritePack] = index;
        }
    }

}
