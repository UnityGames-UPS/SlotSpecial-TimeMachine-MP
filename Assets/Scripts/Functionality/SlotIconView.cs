using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotIconView : MonoBehaviour
{
    [Header("required fields")]
    [SerializeField] internal int pos;
    [SerializeField] internal int id = -1;
    [SerializeField] internal Image iconImage;

    [SerializeField] private Image circleImage;
    [SerializeField] private Image borderImage;

    [SerializeField] internal ImageAnimation activeanimation;
    internal void StartAnim(List<Sprite> animSprite)
    {
        if(animSprite.Count==0 )
        {
            Debug.Log("no anim sprite");
            return;
        }
        activeanimation.textureArray.Clear();
        activeanimation.textureArray.AddRange(animSprite);
        activeanimation.AnimationSpeed = animSprite.Count;
        if(activeanimation.textureArray.Count==0)
                {
            Debug.Log("no anim sprite");
            return;
        }
        if (id < 6 || id == 11)
        {
            activeanimation.rendererDelegate = circleImage;

        }
        else if (id >= 6 && id < 8)
        {
            activeanimation.rendererDelegate = borderImage;

        }
        else if (id >= 8 & id < 11)
        {
            activeanimation.rendererDelegate = iconImage;

        }
        activeanimation.StartAnimation();

    }

    internal void StopAnim()
    {
        activeanimation.StopAnimation();

        // Sprite firstSprite = activeanimation.textureArray[0];
        activeanimation.textureArray.Clear();
        // activeanimation.textureArray.Add(firstSprite);


    }

}
