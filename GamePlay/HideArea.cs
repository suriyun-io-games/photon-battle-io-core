using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideArea : MonoBehaviour
{
    [System.Serializable]
    public struct RendererAndMaterials
    {
        public Renderer renderer;
        [Tooltip("Materials when your character is outside the area")]
        public Material[] outsideMaterials;
        [Tooltip("Materials when your character is inside the area")]
        public Material[] insideMaterials;
    }

    public RendererAndMaterials[] rendererAndMaterials;
    
    private CharacterEntity tempCharacter;
    private bool isMineCharacterInside;
    private readonly HashSet<CharacterEntity> insideCharacters = new HashSet<CharacterEntity>();

    private void Awake()
    {
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;

        foreach (var entry in rendererAndMaterials)
        {
            entry.renderer.materials = entry.outsideMaterials;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        tempCharacter = other.GetComponent<CharacterEntity>();
        if (tempCharacter == null)
            return;
        insideCharacters.Add(tempCharacter);
        if (tempCharacter == BaseNetworkGameCharacter.Local)
        {
            isMineCharacterInside = true;
            foreach (var entry in rendererAndMaterials)
            {
                entry.renderer.materials = entry.insideMaterials;
            }
        }
        foreach (var insideCharacter in insideCharacters)
        {
            if (insideCharacter == BaseNetworkGameCharacter.Local) continue;
            insideCharacter.IsHidding = !isMineCharacterInside;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        tempCharacter = other.GetComponent<CharacterEntity>();
        if (tempCharacter == null)
            return;
        insideCharacters.Remove(tempCharacter);
        if (tempCharacter == BaseNetworkGameCharacter.Local)
        {
            isMineCharacterInside = false;
            foreach (var entry in rendererAndMaterials)
            {
                entry.renderer.materials = entry.outsideMaterials;
            }
        }
        foreach (var insideCharacter in insideCharacters)
        {
            if (insideCharacter == BaseNetworkGameCharacter.Local) continue;
            insideCharacter.IsHidding = !isMineCharacterInside;
        }
    }
}
