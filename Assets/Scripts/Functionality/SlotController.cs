using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using System;
using TMPro;

public class SlotController : MonoBehaviour
{


    [Header("Sprites")]
    [SerializeField] private Sprite[] iconImages;
    [SerializeField] private List<Sprite> wildAnimSprite;
    [SerializeField] private List<Sprite> timeMachine1Sprite;
    [SerializeField] private List<Sprite> timeMachine2Sprite;
    [SerializeField] private List<Sprite> circleAnimSprite;
    [SerializeField] private List<Sprite> squareAnimSprite;

    [Header("Slot Images")]
    [SerializeField] private List<SlotImage> slotMatrix;
    [SerializeField] internal GameObject disableIconsPanel;


    [Header("Slots Transforms")]
    [SerializeField] private RectTransform[] Slot_Transform;
    [SerializeField] private RectTransform mask_transform;
    [SerializeField] private RectTransform bg_mask_transform;
    [SerializeField] private RectTransform[] bg_slot_transform;
    [SerializeField] private RectTransform[] sideBars;
    [SerializeField] private ImageAnimation[] sideBarsAnim;

    [SerializeField] private RectTransform[] horizontalBars;
    [SerializeField] internal ImageAnimation watchAnimation;
    [SerializeField] internal int level;

    [SerializeField] private TMP_Text noOfWays;

    [Header("tween properties")]
    [SerializeField] private float tweenHeight = 0;
    [SerializeField] private float initialPos;



    private List<Tweener> alltweens = new List<Tweener>();

    private Tweener WinTween = null;

    [SerializeField] private List<Image> levelIndicator;
    [SerializeField] internal List<SlotIconView> animatingIcons = new List<SlotIconView>();

    internal IEnumerator StartSpin()
    {

        for (int i = 0; i < Slot_Transform.Length; i++)
        {
            InitializeTweening(Slot_Transform[i]);
            yield return new WaitForSeconds(0.15f);

        }

        yield return new WaitForSeconds(0.2f);
    }

    internal void PopulateSLotMatrix(List<List<int>> resultData)
    {
        int matrixRowCount = 0;

        for (int i = resultData.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < resultData[i].Count; j++)
            {
                slotMatrix[j].slotImages[6 - matrixRowCount].iconImage.sprite = iconImages[resultData[i][j]];
                slotMatrix[j].slotImages[6 - matrixRowCount].id = resultData[i][j];
            }
            matrixRowCount++;
        }
    }
    internal IEnumerator StopSpin()
    {

        for (int i = 0; i < Slot_Transform.Length; i++)
        {
            StopTweening(Slot_Transform[i], i);

            yield return new WaitForSeconds(0.2f);
        }
        // yield return new WaitForSeconds(0.2f);

        KillAllTweens();

    }

    internal void shuffleInitialMatrix()
    {
        for (int i = 0; i < slotMatrix.Count; i++)
        {
            for (int j = 0; j < slotMatrix[i].slotImages.Count; j++)
            {
                int randomIndex = UnityEngine.Random.Range(0, iconImages.Length - 1);
                slotMatrix[i].slotImages[j].iconImage.sprite = iconImages[randomIndex];
                slotMatrix[i].slotImages[j].id = randomIndex;
                slotMatrix[i].slotImages[j].pos = (i * 10 + j);
            }
        }
    }


    internal void ResizeSlotMatrix(int levelCount)
    {

        if (levelCount > 0 && levelCount < 4)
        {
            for (int i = 0; i < 5; i++)
            {
                if (i == 0)
                    slotMatrix[i].slotImages[4 - levelCount].iconImage.sprite = iconImages[UnityEngine.Random.Range(0, 5)];
                else
                    slotMatrix[i].slotImages[4 - levelCount].iconImage.sprite = iconImages[UnityEngine.Random.Range(5, 9)];
            }
        }

        watchAnimation.StopAnimation();

        level = levelCount;
        if (level == 1)
        {
            levelIndicator[0].gameObject.SetActive(true);
            levelIndicator[0].DOColor(Color.white, 1f);
            noOfWays.text = $"1024\nways";
        }
        else if (level == 2)
        {
            levelIndicator[1].gameObject.SetActive(true);
            levelIndicator[1].DOColor(Color.white, 1f);
            noOfWays.text = $"3125\nways";


        }
        else if (level == 3)
        {
            levelIndicator[2].gameObject.SetActive(true);
            levelIndicator[2].DOColor(Color.white, 1f);
            noOfWays.text = $"7776\nways";


        }
        else if (level == 4)
        {
            levelIndicator[3].gameObject.SetActive(true);
            levelIndicator[3].DOColor(Color.white, 1f);
            noOfWays.text = $"16807\nways";


        }
        else if (level == 0)
        {
            noOfWays.text = $"243\nways";

            foreach (var item in levelIndicator)
            {
                item.color = new Color(1, 1, 1, 0);
                item.gameObject.SetActive(false);
            }
        }
        Vector2 sizeDelta = mask_transform.sizeDelta;

        float iconHeight = sizeDelta.y / (3 + level);
        float iconWidth = iconHeight * 1.25f;

        sizeDelta.x = 5 * iconWidth;
        float reelHeight = 15 * iconHeight;
        initialPos = -(iconHeight * (3 + (level - 1) * 0.5f));
        tweenHeight = reelHeight + initialPos;
        mask_transform.DOSizeDelta(sizeDelta, 1f);
        bg_mask_transform.DOSizeDelta(sizeDelta, 1f);
        float offset = iconWidth * 2 + 35;
        bool animateSideBars = true;

        if (level == 4)
        {
            offset = 210;
            foreach (var item in horizontalBars)
            {
                item.sizeDelta = new Vector2(820, 40);
            }
            watchAnimation.StartAnimation();

        }
        else if (level > 0)
        {
            offset = iconWidth * 2 - (level - 1) * 20;
            watchAnimation.StartAnimation();

        }
        else
        {
            animateSideBars = false;
            foreach (var item in horizontalBars)
            {
                item.sizeDelta = new Vector2(890, 40);
            }
        }

        sideBars[0].DOLocalMoveX(offset, 1f);
        sideBars[1].DOLocalMoveX(-offset, 1f);

        if (animateSideBars)
        {
            foreach (var anim in sideBarsAnim)
            {
                anim.StopAnimation();
                anim.StartAnimation();
            }
        }

        for (int i = 0; i < Slot_Transform.Length; i++)
        {
            int index = i;
            Slot_Transform[index].DOSizeDelta(new Vector2(iconWidth, reelHeight), 1f).OnUpdate(() =>
            {

                LayoutRebuilder.ForceRebuildLayoutImmediate(Slot_Transform[index]);

            });
            bg_slot_transform[index].DOSizeDelta(new Vector2(iconWidth, iconHeight * 7), 1f).OnUpdate(() =>
            {

                LayoutRebuilder.ForceRebuildLayoutImmediate(bg_slot_transform[index]);

            });
        }
        for (int i = 0; i < Slot_Transform.Length; i++)
        {
            Vector2 finalPos = new Vector2((i - Slot_Transform.Length / 2) * iconWidth, initialPos);
            Slot_Transform[i].DOLocalMove(finalPos, 1);
            bg_slot_transform[i].DOLocalMove(new Vector2(finalPos.x, -(iconHeight * (2 + (level - 1) * 0.5f))), 1);
        }

    }

    internal void StartIconAnimation(List<string> iconPos, int matrixlength)
    {
        for (int j = 0; j < iconPos.Count; j++)
        {
            ;
            int[] pos = iconPos[j].Split(',').Select(int.Parse).ToArray();
            Debug.Log("row,col" + ((7 - matrixlength) + pos[1]) + "," + pos[0]);
            SlotIconView tempIcon = slotMatrix[pos[0]].slotImages[(4 - level) + pos[1]];
            if (tempIcon.id == 10)
                tempIcon.StartAnim(wildAnimSprite);
            else if (tempIcon.id == 9)
                tempIcon.StartAnim(timeMachine2Sprite);
            else if (tempIcon.id == 8)
                tempIcon.StartAnim(timeMachine1Sprite);
            else if (tempIcon.id == 6 || tempIcon.id == 7)
                tempIcon.StartAnim(squareAnimSprite);
            else if (tempIcon.id == 11)
                tempIcon.StartAnim(circleAnimSprite);
            else if (tempIcon.id < 6)
                tempIcon.StartAnim(circleAnimSprite);

            animatingIcons.Add(tempIcon);
        }

    }

    internal void StopIconAnimation()
    {

        foreach (var item in animatingIcons)
        {
            item.StopAnim();
            // yield return new WaitUntil(() => item.activeanimation.currentAnimationState == ImageAnimation.ImageState.NONE);
            // for (int i = item.activeanimation.textureArray.Count - 1; i > 0; i--)
            // {
            //     item.activeanimation.textureArray.RemoveAt(i);
            // }
        }

        animatingIcons.Clear();
    }



    #region TweeningCode
    private void InitializeTweening(Transform slotTransform)
    {
        // slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        Tweener tweener = slotTransform.DOLocalMoveY(-tweenHeight, 0.15f).SetLoops(-1, LoopType.Restart).SetDelay(0);
        alltweens.Add(tweener);
        // tweener.Play();
    }

    private void StopTweening(Transform slotTransform, int index)
    {
        alltweens[index].Pause();
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, initialPos + 265);
        alltweens[index] = slotTransform.DOLocalMoveY(initialPos, 0.2f).SetEase(Ease.OutElastic); // slot initial pos - iconsizefactor - spacing

    }


    private void KillAllTweens()
    {
        for (int i = 0; i < alltweens.Count; i++)
        {
            alltweens[i].Kill();
        }
        alltweens.Clear();

    }
    #endregion

}

[Serializable]
public class SlotImage
{
    public List<SlotIconView> slotImages = new List<SlotIconView>(10);
}


