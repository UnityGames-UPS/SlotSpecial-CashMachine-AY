using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SlotController : MonoBehaviour
{
    #region References
    [Header("Arrays & Lists")]
    [SerializeField]
    private Sprite[] Slot_Sprites;
    [SerializeField]
    internal Transform[] Slot_Transform;
    [SerializeField]
    private Image[] Stop_Images;
    [SerializeField]
    private ImageAnimation[] Stop_Anims;
    [SerializeField]
    private Sprite[] RedSlot_Sprites;
    [SerializeField]
    internal Transform[] RedSlot_Transform;
    [SerializeField]
    private Image[] RedStop_Images;
    [SerializeField]
    private ImageAnimation[] RedStop_Anims;
    private List<Tweener> alltweens = new List<Tweener>(3);
    private List<Tweener> redalltweens = new List<Tweener>(3);

    [Header("Animated Sprites")]
    [SerializeField]
    private Sprite[] Symbol1;
    [SerializeField]
    private Sprite[] Symbol2;
    [SerializeField]
    private Sprite[] Symbol3;
    [SerializeField]
    private Sprite[] Symbol4;
    [SerializeField]
    private Sprite[] Symbol5;
    [SerializeField]
    private Sprite[] Symbol6;

    [Header("Red Animated Sprites")]
    [SerializeField]
    private Sprite[] RedSymbol1;
    [SerializeField]
    private Sprite[] RedSymbol2;
    [SerializeField]
    private Sprite[] RedSymbol3;
    [SerializeField]
    private Sprite[] RedSymbol4;
    [SerializeField]
    private Sprite[] RedSymbol5;
    [SerializeField]
    private Sprite[] RedSymbol6;

    internal bool IsSpinning = false;
    internal bool IsAutoSpin = false;

    [Header("Integers")]
    [SerializeField]
    private int IconSizeFactor = 0;
    [SerializeField]
    private int SpaceFactor = 0;
    [SerializeField]
    private int tweenHeight = 0;
    [SerializeField]
    private int MidIconSizeFactor = 0;
    [SerializeField]
    private int MidSpaceFactor = 0;
    [SerializeField]
    private int MidtweenHeight = 0;
    internal int SlotNumber;
    internal int BetCounter;
    internal int DenomCounter;

    [Header("Controllers")]
    [SerializeField]
    private UIManager uiController;
    [SerializeField]
    private SocketIOManager socketManager;
    [SerializeField]
    private AudioController audioController;
    #endregion

    [Header("turbo")]
    [SerializeField] internal bool StopSpinToggle;
    [SerializeField] internal bool IsTurboOn;
    private bool WasAutoSpinOn;
    private float SpinDelay = 0.2f;



    private Coroutine AutoSpinRoutine = null;
    private Coroutine tweenroutine = null;

    private void Start()
    {
        tweenHeight = (15 * IconSizeFactor) - 280;
        MidtweenHeight = (15 * MidIconSizeFactor) - 280;
    }

    internal void UpdateUI(double balance)
    {
        if (uiController) uiController.UpdateBalance(balance);
        if (!uiController.CheckBalance(balance))
        {
            if (uiController) uiController.EnableLowBalance();
        }

    }

    #region AutoSpin
    internal void AutoSpin(int count)
    {
        if (!IsAutoSpin)
        {
            IsAutoSpin = true;

            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                AutoSpinRoutine = null;
            }
            AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine(count));
        }
    }
    internal void StopAutoSpin()
    {
        if (IsAutoSpin)
        {
            IsAutoSpin = false;
            StartCoroutine(StopAutoSpinCoroutine());
        }
    }

    // private IEnumerator AutoSpinCoroutine(int count)
    // {
    //     while (IsAutoSpin && count > 0)
    //     {
    //         StartSpin();
    //         count--;
    //         if (uiController) uiController.updateAutoCount(count);
    //         yield return tweenroutine;
    //     }
    //     StopAutoSpin();
    // }
    private IEnumerator AutoSpinCoroutine(int count)
    {
        while (IsAutoSpin && count > 0)
        {
            StartSpin();
            count--;
            if (uiController) uiController.updateAutoCount(count);
            yield return new WaitUntil(() => !IsSpinning);
        }
        StopAutoSpin();
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !IsSpinning);
        if (AutoSpinRoutine != null || tweenroutine != null)
        {
            StopCoroutine(AutoSpinRoutine);
            StopCoroutine(tweenroutine);
            tweenroutine = null;
            AutoSpinRoutine = null;
            StopCoroutine(StopAutoSpinCoroutine());
            if (uiController) uiController.ToggleButtonGrp(true);
            if (uiController) uiController.StopAutoSpin();
        }
    }


    #endregion

    #region SpinLogic
    internal void StartSpin()
    {
        tweenroutine = StartCoroutine(TweenRoutine());
    }

    private IEnumerator TweenRoutine()
    {
        if (!uiController.CheckBalance(socketManager.Bets[BetCounter]))
        {
            if (uiController) uiController.EnableLowBalance();
            StopAutoSpin();

            if (IsTurboOn) yield return new WaitForSeconds(0.2f);
            else yield return new WaitForSeconds(2);
            if (uiController) uiController.ToggleButtonGrp(true);
            yield break;
        }
        if (!IsTurboOn)
        {
            uiController.StopSpin_Button.gameObject.SetActive(true);
        }
        if (audioController) audioController.PlayWLAudio("spin");
        IsSpinning = true;
        ResetSpin();
        uiController.resetWinColor();
        if (uiController) uiController.UpdateTweenBalance(socketManager.Bets[BetCounter]);
        for (int i = 0; i < SlotNumber + 1; i++)
        {
            if (i == 1)
            {
                InitializeTweening(Slot_Transform[i], 1, false, true);
                // InitializeTweening(Slot_Transform[i], 1, false, true, i);
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                InitializeTweening(Slot_Transform[i], 1, false);
                // InitializeTweening(Slot_Transform[i], 1, false, false, i);
                yield return new WaitForSeconds(0.1f);
            }
        }

        // socketManager.AccumulateResult(BetCounter,SlotNumber);
        socketManager.AccumulateResult(DenomCounter, SlotNumber);

        yield return new WaitUntil(() => socketManager.isResultdone);

        PopulateNormalSpin(0, false);


        // for (int i = 0; i < SlotNumber + 1; i++)
        // {
        //     if (i == 1)
        //     {
        //         yield return StopTweening(5, Slot_Transform[i], i, 1, int.Parse(socketManager.resultData.matrix[0][i]), false, true);
        //     }
        //     else
        //     {
        //         yield return StopTweening(5, Slot_Transform[i], i, 1, int.Parse(socketManager.resultData.matrix[0][i]), false);
        //     }
        // }
        for (int i = 0; i < SlotNumber + 1; i++)
        {
            string symbolStr = socketManager.resultData.matrix[0][i];

            if (IsFrozen(i))
            {
                symbolStr = GetFrozenSymbol(i); // ✅ overwrite with frozen symbol
            }

            int symbolVal = int.Parse(symbolStr);

            if (i == 1)
            {
                yield return StopTweening(5, Slot_Transform[i], i, 1, symbolVal, false, true);
            }
            else
            {
                yield return StopTweening(5, Slot_Transform[i], i, 1, symbolVal, false);
            }
        }

        StopSpinToggle = false;
        StartNormalAnimation(0);
        //yield return new WaitForSeconds(0.3f);
        yield return alltweens[^1].WaitForCompletion();
        KillAllTweens();
        if (socketManager.resultData.payload.isZeroRespin)
        {
            yield return GreenRespinLogic(socketManager.resultData.matrix.Count);
        }
        if (socketManager.resultData.payload.isRedRespin)
        {
            yield return RedSpinLogic();
        }
        if (socketManager.resultData.payload?.currentWinning > 0)
        {
            yield return uiController.UpdateWinnings(socketManager.playerdata.balance, socketManager.resultData.payload.currentWinning);
        }
        else
        {
            uiController.ResetWinText();
            yield return new WaitForSeconds(.9f);
        }
        KillAllTweens();
        IsSpinning = false;
        if (!IsAutoSpin)
        {
            if (uiController) uiController.ToggleButtonGrp(true);
        }
    }

    private void ResetSpin()
    {
        for (int i = 0; i < Stop_Anims.Length; i++)
        {
            Stop_Anims[i].StopAnimation();
        }
    }
    #endregion

    #region GreenSpinLogic

    private IEnumerator GreenRespinLogic(int length)
    {
        if (uiController) uiController.GreenRespin(true);
        if (audioController) audioController.PlayWLAudio("respin");
        if (IsTurboOn) yield return new WaitForSecondsRealtime(0.5f);
        else yield return new WaitForSecondsRealtime(2.5f);
        for (int len = 0; len < length - 1; len++)
        {
            for (int i = 0; i < SlotNumber + 1; i++)
            {
                if (int.Parse(socketManager.resultData.matrix[len][i]) == 0)
                {
                    if (i != 1)
                    {
                        yield return InitiateGreenRespin(i, false);
                    }
                    else
                    {
                        yield return InitiateGreenRespin(i, true);
                    }
                }
            }
            if (audioController) audioController.PlayWLAudio("spin");
            if (IsTurboOn) yield return new WaitForSeconds(0.5f);
            else yield return new WaitForSeconds(2f);
            PopulateNormalSpin(len + 1, false);
            int k = 0;
            for (int i = 0; i < SlotNumber + 1; i++)
            {
                if (int.Parse(socketManager.resultData.matrix[len][i]) == 0)
                {
                    if (i != 1)
                    {
                        yield return StopGreenRespin(i, k, int.Parse(socketManager.resultData.matrix[len][i]), false);
                        k++;
                    }
                    else
                    {
                        yield return StopGreenRespin(i, k, int.Parse(socketManager.resultData.matrix[len][i]), true);
                        k++;
                    }
                }
            }
            StartNormalAnimation(len + 1);
            KillAllTweens();
            yield return new WaitForSeconds(1f);
        }
        if (uiController) uiController.GreenRespin(false);
    }

    private IEnumerator InitiateGreenRespin(int value, bool isMid)
    {
        InitializeTweening(Slot_Transform[value], 1, true, isMid);
        // InitializeTweening(Slot_Transform[value], 1, true, isMid, value);
        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator StopGreenRespin(int value, int tweenvalue, int isMoney, bool isMid)
    {
        yield return StopTweening(5, Slot_Transform[value], tweenvalue, 1, isMoney, true, isMid);
    }
    #endregion

    #region RedSpinLogic

    private IEnumerator RedSpinLogic()
    {
        PopulateRedSpin(0, true);
        if (audioController) audioController.PlayWLAudio("respin");
        if (uiController) uiController.GreenRespin(true);
        yield return new WaitForSeconds(1);
        if (uiController) uiController.RedRespin(true);
        // yield return new WaitForSeconds(3);
        if (IsTurboOn) yield return new WaitForSeconds(2f);
        else yield return new WaitForSeconds(3f);
        for (int i = 0; i < SlotNumber + 1; i++)
        {
            if (int.Parse(socketManager.resultData.matrix[0][i]) == 0)
            {
                if (i != 1)
                {
                    yield return InitiateRedRespin(i, false);
                }
                else
                {
                    yield return InitiateRedRespin(i, true);
                }
            }
        }
        if (audioController) audioController.PlayWLAudio("spin");
        // yield return new WaitForSeconds(2f);
        if (IsTurboOn) yield return new WaitForSeconds(1f);
        else yield return new WaitForSeconds(2f);
        int k = 0;
        PopulateRedSpin(1, false);
        PopulateNormalSpin(1, true);
        StartNormalAnimation(1);
        for (int i = 0; i < SlotNumber + 1; i++)
        {
            if (int.Parse(socketManager.resultData.matrix[0][i]) == 0)
            {
                if (i != 1)
                {
                    yield return StopRedRespin(i, k, int.Parse(socketManager.resultData.matrix[1][i]), false);
                    k++;
                }
                else
                {
                    yield return StopRedRespin(i, k, int.Parse(socketManager.resultData.matrix[1][i]), true);
                    k++;
                }
            }
        }
        //yield return new WaitForSeconds(2);
        if (IsTurboOn) yield return new WaitForSeconds(1f);
        else yield return new WaitForSeconds(2f);
        KillAllRedTweens();
        if (uiController) uiController.GreenRespin(false);
        if (uiController) uiController.RedRespin(false);
    }

    private IEnumerator InitiateRedRespin(int value, bool isMid)
    {
        InitializeTweening(RedSlot_Transform[value], 2, false, isMid);
        // InitializeTweening(RedSlot_Transform[value], 2, false, isMid, value);
        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator StopRedRespin(int value, int tweenvalue, int isMoney, bool isMid)
    {
        yield return StopTweening(5, RedSlot_Transform[value], tweenvalue, 1, isMoney, false, isMid);
        for (int i = 0; i < SlotNumber + 1; i++)
        {
            if (int.Parse(socketManager.resultData.matrix[1][i]) != 0)
            {
                RedStop_Anims[i].StartAnimation();
            }
        }
    }
    #endregion

    #region PopulateLogic

    private void PopulateRedSpin(int index, bool isFirst)
    {
        for (int i = 0; i < SlotNumber + 1; i++)
        {
            if (int.Parse(socketManager.resultData.matrix[index][i]) != 0)
            {
                PopulateRedAnimationSprites(RedStop_Anims[i], RedStop_Images[i], int.Parse(socketManager.resultData.matrix[index][i]));
            }
            else if (!isFirst)
            {
                int m_index = UnityEngine.Random.Range(7, 10);
                RedStop_Images[i].sprite = RedSlot_Sprites[m_index];
                Stop_Images[i].sprite = Slot_Sprites[m_index];
            }
        }
    }

    private void PopulateNormalSpin(int index, bool isFirst)
    {
        for (int i = 0; i < SlotNumber + 1; i++)
        {
            if (int.Parse(socketManager.resultData.matrix[index][i]) != 0)
            {
                PopulateAnimationSprites(Stop_Anims[i], Stop_Images[i], int.Parse(socketManager.resultData.matrix[index][i]));
            }
            else if (!isFirst)
            {
                int m_index = UnityEngine.Random.Range(7, 10);
                Stop_Images[i].sprite = Slot_Sprites[m_index];
                RedStop_Images[i].sprite = RedSlot_Sprites[m_index];
            }
        }
    }

    private void StartNormalAnimation(int index)
    {
        for (int i = 0; i < SlotNumber + 1; i++)
        {
            if (int.Parse(socketManager.resultData.matrix[index][i]) != 0)
            {
                Stop_Anims[i].StartAnimation();
                uiController.AddWinColor(i);
            }
        }
    }
    #endregion

    #region TweenLogic
    private void PopulateAnimationSprites(ImageAnimation animScript, Image StopImage, int val)
    {
        animScript.textureArray.Clear();
        animScript.textureArray.TrimExcess();
        animScript.AnimationSpeed = 8;
        switch (val)
        {
            case 1:
                for (int i = 0; i < Symbol3.Length; i++)
                {
                    animScript.textureArray.Add(Symbol3[i]);
                }
                break;
            case 2:
                for (int i = 0; i < Symbol4.Length; i++)
                {
                    animScript.textureArray.Add(Symbol4[i]);
                }
                break;
            case 3:
                for (int i = 0; i < Symbol5.Length; i++)
                {
                    animScript.textureArray.Add(Symbol5[i]);
                }
                break;
            case 4:
                animScript.AnimationSpeed = 6;
                for (int i = 0; i < Symbol6.Length; i++)
                {
                    animScript.textureArray.Add(Symbol6[i]);
                }
                break;
            case 5:
                for (int i = 0; i < Symbol2.Length; i++)
                {
                    animScript.textureArray.Add(Symbol2[i]);
                }
                break;
            case 6:
                for (int i = 0; i < Symbol1.Length; i++)
                {
                    animScript.textureArray.Add(Symbol1[i]);
                }
                break;
        }
        StopImage.sprite = Slot_Sprites[val];
    }

    private void PopulateRedAnimationSprites(ImageAnimation animScript, Image StopImage, int val)
    {
        animScript.textureArray.Clear();
        animScript.textureArray.TrimExcess();
        switch (val)
        {
            case 1:
                for (int i = 0; i < RedSymbol3.Length; i++)
                {
                    animScript.textureArray.Add(RedSymbol3[i]);
                }
                break;
            case 2:
                for (int i = 0; i < RedSymbol4.Length; i++)
                {
                    animScript.textureArray.Add(RedSymbol4[i]);
                }
                break;
            case 3:
                for (int i = 0; i < RedSymbol5.Length; i++)
                {
                    animScript.textureArray.Add(RedSymbol5[i]);
                }
                break;
            case 4:
                for (int i = 0; i < RedSymbol6.Length; i++)
                {
                    animScript.textureArray.Add(RedSymbol6[i]);
                }
                break;
            case 5:
                for (int i = 0; i < RedSymbol2.Length; i++)
                {
                    animScript.textureArray.Add(RedSymbol2[i]);
                }
                break;
            case 6:
                for (int i = 0; i < RedSymbol1.Length; i++)
                {
                    animScript.textureArray.Add(RedSymbol1[i]);
                }
                break;
        }
        StopImage.sprite = RedSlot_Sprites[val];
    }

    private void InitializeTweening(Transform slotTransform, int type, bool isGreen, bool IsMid = false)
    {
        int myTweenHeight = 0;
        if (IsMid)
        {
            myTweenHeight = MidtweenHeight;
        }
        else
        {
            myTweenHeight = tweenHeight;
        }
        Tweener tweener = null;
        if (!isGreen)
        {
            slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
            tweener = slotTransform.DOLocalMoveY(-myTweenHeight, 1f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
        }
        else
        {
            slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, -myTweenHeight);
            tweener = slotTransform.DOLocalMoveY(0, 1f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
        }
        tweener.Play();

        if (type == 1)
        {
            alltweens.Add(tweener);
        }
        else
        {
            redalltweens.Add(tweener);
        }
    }
    // private void InitializeTweening(Transform slotTransform, int type, bool isGreen, bool IsMid = false)
    // {
    //     int myTweenHeight = IsMid ? MidtweenHeight : tweenHeight;

    //     // start pos
    //     if (!isGreen)
    //         slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
    //     else
    //         slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, -myTweenHeight);

    //     Tweener tweener = (!isGreen)
    //         ? slotTransform.DOLocalMoveY(-myTweenHeight, 1f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0)
    //         : slotTransform.DOLocalMoveY(0, 1f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);

    //     tweener.Play();

    //     // Find column index (so we store tween at correct slot index)
    //     int columnIndex = -1;
    //     if (type == 1)
    //     {
    //         for (int i = 0; i < Slot_Transform.Length; i++)
    //         {
    //             if (Slot_Transform[i] == slotTransform) { columnIndex = i; break; }
    //         }
    //     }
    //     else
    //     {
    //         for (int i = 0; i < RedSlot_Transform.Length; i++)
    //         {
    //             if (RedSlot_Transform[i] == slotTransform) { columnIndex = i; break; }
    //         }
    //     }

    //     if (type == 1)
    //     {
    //         if (columnIndex >= 0)
    //         {
    //             while (alltweens.Count <= columnIndex) alltweens.Add(null);
    //             if (alltweens[columnIndex] != null) { alltweens[columnIndex].Kill(); alltweens[columnIndex] = null; }
    //             alltweens[columnIndex] = tweener;
    //         }
    //         else
    //         {
    //             // fallback: append
    //             alltweens.Add(tweener);
    //         }
    //     }
    //     else
    //     {
    //         if (columnIndex >= 0)
    //         {
    //             while (redalltweens.Count <= columnIndex) redalltweens.Add(null);
    //             if (redalltweens[columnIndex] != null) { redalltweens[columnIndex].Kill(); redalltweens[columnIndex] = null; }
    //             redalltweens[columnIndex] = tweener;
    //         }
    //         else
    //         {
    //             redalltweens.Add(tweener);
    //         }
    //     }
    // }

    private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, int type, int isMoney, bool isGreen, bool isMid = false)
    {
        if (IsFrozen(index))
        {
            // Kill tween if running
            if (type == 1 && index < alltweens.Count && alltweens[index] != null)
                alltweens[index].Kill();
            if (type != 1 && index < redalltweens.Count && redalltweens[index] != null)
                redalltweens[index].Kill();

            string frozenSymbol = GetFrozenSymbol(index);
            int frozenVal = int.Parse(frozenSymbol);

            // Force sprite/animation
            PopulateAnimationSprites(Stop_Anims[index], Stop_Images[index], frozenVal);

            // Snap transform so it won’t move
            slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);

            if (frozenVal > 0)
            {
                uiController.AddWinColor(index);
                if (audioController) audioController.PlayWLAudio("win");
            }
            else
            {
                if (audioController) audioController.PlayWLAudio("dot");
            }

            yield break; // don’t run normal tween
        }

        int myTweenHeight = 0;
        int mySizeFactor = 0;
        int mySpaceFactor = 0;
        if (isMid)
        {
            mySizeFactor = MidIconSizeFactor;
            mySpaceFactor = MidSpaceFactor;
            myTweenHeight = MidtweenHeight;
        }
        else
        {
            mySizeFactor = IconSizeFactor;
            mySpaceFactor = SpaceFactor;
            myTweenHeight = tweenHeight;
        }
        if (type == 1)
        {

            bool IsRegister = false;
            if (!IsTurboOn)
            {
                yield return alltweens[index].OnStepComplete(delegate { IsRegister = true; });
                yield return new WaitUntil(() => IsRegister);
            }
            else                                                             // changes
            {
                yield return new WaitForSeconds(0.1f);
            }
            uiController.StopSpin_Button.gameObject.SetActive(false);
            alltweens[index].Kill();
            int tweenpos = (reqpos * (mySizeFactor + mySpaceFactor)) - (mySizeFactor + (2 * mySpaceFactor));
            if (!isGreen)
            {
                slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);                                  //change
                alltweens[index] = slotTransform.DOLocalMoveY(-tweenpos + 100 + (mySpaceFactor > 0 ? mySpaceFactor / 4 : 0), 0.7f).SetEase(Ease.OutBounce);
            }
            else
            {
                slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, -myTweenHeight);
                alltweens[index] = slotTransform.DOLocalMoveY(-tweenpos + 100, 1f);
            }

            if (!StopSpinToggle)
            {
                yield return alltweens[index].WaitForCompletion();
            }
            else
            {

                yield return null;

            }
            //  alltweens[index].Kill();
        }
        else
        {
            bool IsRegister = false;
            yield return redalltweens[index].OnStepComplete(delegate { IsRegister = true; });
            yield return new WaitUntil(() => IsRegister);
            redalltweens[index].Kill();
            int tweenpos = (reqpos * (mySizeFactor + mySpaceFactor)) - (mySizeFactor + (2 * mySpaceFactor));
            redalltweens[index] = slotTransform.DOLocalMoveY(-tweenpos + 100 + (mySpaceFactor > 0 ? mySpaceFactor / 4 : 0), 0.7f).SetEase(Ease.OutBounce);
            yield return redalltweens[index].WaitForCompletion();
            redalltweens[index].Kill();
        }
        if (isMoney == 0)
        {
            if (audioController) audioController.PlayWLAudio("dot");
        }
        else
        {
            if (audioController) audioController.PlayWLAudio("win");
            uiController.AddWinColor(index);
            //  yield return new WaitForSecondsRealtime(2);
        }
    }
    // private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, int type, int isMoney, bool isGreen, bool isMid = false)
    // {
    //     Debug.Log($"[StopTweening] index={index} type={type} isGreen={isGreen} isMid={isMid} symbol={isMoney}");

    //     // --- Frozen handling: stop and snap immediately ---
    //     if (IsFrozen(index))
    //     {
    //         // kill any running tween for this column
    //         if (type == 1)
    //         {
    //             if (index < alltweens.Count && alltweens[index] != null) { alltweens[index].Kill(); alltweens[index] = null; }
    //         }
    //         else
    //         {
    //             if (index < redalltweens.Count && redalltweens[index] != null) { redalltweens[index].Kill(); redalltweens[index] = null; }
    //         }

    //         string frozenSymbol = GetFrozenSymbol(index);
    //         int frozenVal = int.Parse(frozenSymbol);

    //         // Force sprite/animation for frozen value
    //         if (type == 1) PopulateAnimationSprites(Stop_Anims[index], Stop_Images[index], frozenVal);
    //         else PopulateRedAnimationSprites(RedStop_Anims[index], RedStop_Images[index], frozenVal);

    //         // Snap transform so it won’t keep moving
    //         if (type == 1)
    //             slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
    //         else
    //             slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);

    //         // UI + audio
    //         if (frozenVal > 0)
    //         {
    //             uiController.AddWinColor(index);
    //             if (audioController) audioController.PlayWLAudio("win");
    //         }
    //         else
    //         {
    //             if (audioController) audioController.PlayWLAudio("dot");
    //         }

    //         yield break;
    //     }

    //     // --- compute sizes/positions ---
    //     int myTweenHeight = isMid ? MidtweenHeight : tweenHeight;
    //     int mySizeFactor = isMid ? MidIconSizeFactor : IconSizeFactor;
    //     int mySpaceFactor = isMid ? MidSpaceFactor : SpaceFactor;

    //     // same math you had for tweenpos
    //     int tweenpos = (reqpos * (mySizeFactor + mySpaceFactor)) - (mySizeFactor + (2 * mySpaceFactor));
    //     float landingY = -tweenpos + 100 + (mySpaceFactor > 0 ? mySpaceFactor / 4f : 0f);

    //     // --- Normal branch (type == 1) ---
    //     if (type == 1)
    //     {
    //         bool IsRegister = false;

    //         if (!IsTurboOn)
    //         {
    //             if (index < alltweens.Count && alltweens[index] != null)
    //             {
    //                 alltweens[index].OnStepComplete(() => { IsRegister = true; });
    //                 yield return new WaitUntil(() => IsRegister);
    //             }
    //             else
    //             {
    //                 // no running looped tween found - small wait so UI doesn't jump
    //                 yield return new WaitForSeconds(0.05f);
    //             }
    //         }
    //         else
    //         {
    //             yield return new WaitForSeconds(0.1f);
    //         }

    //         // hide stop button
    //         if (uiController) uiController.StopSpin_Button.gameObject.SetActive(false);

    //         // Kill loop tween
    //         if (index < alltweens.Count && alltweens[index] != null)
    //         {
    //             alltweens[index].Kill();
    //             alltweens[index] = null;
    //         }

    //         // Create final landing tween
    //         if (!isGreen)
    //         {
    //             slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
    //             alltweens[index] = slotTransform.DOLocalMoveY(landingY, 0.7f).SetEase(Ease.OutBounce);
    //         }
    //         else
    //         {
    //             slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, -myTweenHeight);
    //             alltweens[index] = slotTransform.DOLocalMoveY(-tweenpos + 100, 1f);
    //         }

    //         if (!StopSpinToggle)
    //         {
    //             yield return alltweens[index].WaitForCompletion();
    //         }
    //         else
    //         {
    //             yield return null;
    //         }
    //     }
    //     // --- Red branch (type != 1) ---
    //     else
    //     {
    //         bool IsRegister = false;

    //         if (!IsTurboOn)
    //         {
    //             if (index < redalltweens.Count && redalltweens[index] != null)
    //             {
    //                 redalltweens[index].OnStepComplete(() => { IsRegister = true; });
    //                 yield return new WaitUntil(() => IsRegister);
    //             }
    //             else
    //             {
    //                 yield return new WaitForSeconds(0.05f);
    //             }
    //         }
    //         else
    //         {
    //             yield return new WaitForSeconds(0.1f);
    //         }

    //         if (index < redalltweens.Count && redalltweens[index] != null)
    //         {
    //             redalltweens[index].Kill();
    //             redalltweens[index] = null;
    //         }

    //         // landing tween for red
    //         redalltweens[index] = slotTransform.DOLocalMoveY(landingY, 0.7f).SetEase(Ease.OutBounce);
    //         yield return redalltweens[index].WaitForCompletion();

    //         if (index < redalltweens.Count && redalltweens[index] != null)
    //         {
    //             redalltweens[index].Kill();
    //             redalltweens[index] = null;
    //         }
    //     }

    //     // --- play dot/win based on symbol given in isMoney param ---
    //     if (isMoney == 0)
    //     {
    //         if (audioController) audioController.PlayWLAudio("dot");
    //     }
    //     else
    //     {
    //         if (audioController) audioController.PlayWLAudio("win");
    //         uiController.AddWinColor(index);
    //     }
    // }


    private void KillAllTweens()
    {
        for (int i = 0; i < alltweens.Count; i++)
        {
            if (alltweens[i] != null)
            {
                alltweens[i].Kill();
            }
        }
        alltweens.Clear();
        alltweens.TrimExcess();
    }

    private void KillAllRedTweens()
    {
        for (int i = 0; i < redalltweens.Count; i++)
        {
            if (redalltweens[i] != null)
            {
                redalltweens[i].Kill();
            }
        }
        redalltweens.Clear();
        redalltweens.TrimExcess();
    }
    #endregion

    #region Internal Methods

    internal void CallCloseSocket()
    {
        StartCoroutine(socketManager.CloseSocket());
    }

    internal void DisconnectionPopup()
    {
        if (uiController) uiController.EnableDisconect();
    }

    internal void PopulateSymbols(Paylines paylines)
    {
        uiController.PopulateSymbolsPayout(paylines);
    }

    #endregion

    private bool IsFrozen(int column)
    {
        var frozen = socketManager.resultData.payload?.frozenIndices;
        if (frozen == null) return false;
        return frozen.Exists(f => f.position[1] == column);
    }

    private string GetFrozenSymbol(int column)
    {
        var frozen = socketManager.resultData.payload?.frozenIndices;
        if (frozen == null) return null;
        var match = frozen.Find(f => f.position[1] == column);
        return match?.symbol;
    }

}
