using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
  private List<FrozenIndex> frozenIndices;

  private Coroutine AutoSpinRoutine = null;
  private Coroutine tweenroutine = null;
  private List<FrozenIndex> FrozenList = new();

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
      if (AutoSpinRoutine != null) StopCoroutine(AutoSpinRoutine);
      if (tweenroutine != null) StopCoroutine(tweenroutine);
      tweenroutine = null;
      AutoSpinRoutine = null;
      // removed StopCoroutine(StopAutoSpinCoroutine()) which attempted to stop itself incorrectly
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
    FrozenList = new();
    uiController.ResetWinText();
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
        InitializeTweening(Slot_Transform[i], false, true);
        yield return new WaitForSeconds(0.1f);
      }
      else
      {
        InitializeTweening(Slot_Transform[i], false);
        yield return new WaitForSeconds(0.1f);
      }
    }

    socketManager.AccumulateResult(DenomCounter, SlotNumber);
    yield return new WaitUntil(() => socketManager.isResultdone);

    PopulateNormalSpin();

    if (socketManager.resultData.payload.frozenIndices.Count > 0)
    {
      foreach (FrozenIndex frozen in socketManager.resultData.payload.frozenIndices)
      {
        FrozenList.Add(frozen);
      }
    }

    for (int i = 0; i < SlotNumber + 1; i++)
    {
      if (i == 1)
      {
        yield return StopTweening(5, Slot_Transform[i], i, socketManager.resultData.payload.currentWinning, false, true);
      }
      else
      {
        yield return StopTweening(5, Slot_Transform[i], i, socketManager.resultData.payload.currentWinning, false);
      }
    }
    StopSpinToggle = false;
    StartNormalAnimation();
    // wait for last tween to finish safely
    Debug.Log("called");
    if (alltweens.Count > 0)
      yield return alltweens[alltweens.Count - 1].WaitForCompletion();
    KillAllTweens();
    Debug.Log("called");
    if (socketManager.resultData.payload.isZeroRespin)
    {
      yield return GreenRespinLogic();
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

  private IEnumerator GreenRespinLogic()
  {
    if (uiController) uiController.GreenRespin(true);
    if (audioController) audioController.PlayWLAudio("respin");
    if (IsTurboOn) yield return null;
    else yield return new WaitForSecondsRealtime(2.5f);
    socketManager.AccumulateResult(DenomCounter, SlotNumber);
    yield return new WaitUntil(() => socketManager.isResultdone);
    // for (int len = 0; len < 1; len++)
    // {
    //   int cols = socketManager.resultData.matrix[0].Count;
    //   for (int col = 0; col < cols && col < Slot_Transform.Length; col++)
    //   {
    //     if (!IsFrozen(0, col))
    //       yield return InitiateGreenRespin(col, col == 1);
    //   }
    // }
    for (int i = 0; i < SlotNumber + 1; i++)
    {
      if (!IsFrozen(0, i))
      {
        if (i == 1)
          yield return InitiateGreenRespin(i, true);
        else
          yield return InitiateGreenRespin(i, false);
      }
    }
    if (audioController) audioController.PlayWLAudio("spin");
    if (IsTurboOn) yield return new WaitForSeconds(0.5f);
    else yield return new WaitForSeconds(2f);
    PopulateNormalSpin();
    int k = 0;
    for (int i = 0; i < SlotNumber + 1; i++)
    {
      if (!IsFrozen(0, i))
      {
        // stop logic
        if (i != 1)
          yield return StopGreenRespin(i, k, socketManager.resultData.payload.currentWinning, false);
        else
          yield return StopGreenRespin(i, k, socketManager.resultData.payload.currentWinning, true);
        k++;
      }
    }
    StartNormalAnimation();
    KillAllTweens();
    yield return new WaitForSeconds(1f);
    if (uiController) uiController.GreenRespin(false);
  }

  private IEnumerator InitiateGreenRespin(int value, bool isMid)
  {
    InitializeTweening(Slot_Transform[value], true, isMid);
    yield return new WaitForSeconds(0.1f);
  }

  private IEnumerator StopGreenRespin(int value, int tweenvalue, double isMoney, bool isMid)
  {
    yield return StopTweening(5, Slot_Transform[value], tweenvalue, isMoney, true, isMid);
  }
  #endregion

  #region RedSpinLogic

  // private IEnumerator RedSpinLogic()
  // {
  //     PopulateRedSpin(0, true);
  //     if (audioController) audioController.PlayWLAudio("respin");
  //     if (uiController) uiController.GreenRespin(true);
  //     yield return new WaitForSeconds(1);
  //     if (uiController) uiController.RedRespin(true);
  //     if (IsTurboOn) yield return new WaitForSeconds(2f);
  //     else yield return new WaitForSeconds(3f);

  //     // Start red respin tweens only for columns that are not frozen for row 0
  //     for (int col = 0; col < SlotNumber + 1; col++)
  //     {
  //         if (!IsFrozen(0, col))
  //         {
  //             yield return InitiateRedRespin(col, col == 1);
  //         }
  //     }
  //     if (audioController) audioController.PlayWLAudio("spin");
  //     if (IsTurboOn) yield return new WaitForSeconds(1f);
  //     else yield return new WaitForSeconds(2f);
  //     int k = 0;
  //     PopulateRedSpin(1, false);
  //     PopulateNormalSpin(1, true);
  //     StartNormalAnimation(1);
  //     for (int i = 0; i < socketManager.resultData.matrix.Count; i++)
  //     {
  //         if (int.Parse(socketManager.resultData.matrix[0][i]) == 0 && !IsFrozen(0, i))
  //         {
  //             if (i != 1)
  //             {
  //                 yield return StopRedRespin(i, k, int.Parse(socketManager.resultData.matrix[1][i]), false);
  //                 k++;
  //             }
  //             else
  //             {
  //                 yield return StopRedRespin(i, k, int.Parse(socketManager.resultData.matrix[1][i]), true);
  //                 k++;
  //             }
  //         }
  //     }
  //     if (IsTurboOn) yield return new WaitForSeconds(1f);
  //     else yield return new WaitForSeconds(2f);
  //     // KillAllRedTweens();
  //     if (uiController) uiController.GreenRespin(false);
  //     if (uiController) uiController.RedRespin(false);
  //     KillAllRedTweens();
  // }
  private IEnumerator RedSpinLogic()
  {
    if (audioController) audioController.PlayWLAudio("respin");
    if (uiController) uiController.GreenRespin(true);
    yield return new WaitForSeconds(1);
    if (uiController) uiController.RedRespin(true);

    yield return new WaitForSeconds(2f);

    socketManager.AccumulateResult(DenomCounter, SlotNumber);
    yield return new WaitUntil(() => socketManager.isResultdone);

    // Start red respin tweens only for columns that are not frozen for row 0
    for (int col = 0; col < SlotNumber + 1; col++)
    {
      if (!IsFrozen(0, col))
      {
        yield return InitiateRedRespin(col, col == 1);
      }
    }
    if (audioController) audioController.PlayWLAudio("spin");
    if (!IsTurboOn) yield return new WaitForSeconds(2f);

    PopulateRedSpin();
    if (socketManager.resultData.payload.frozenIndices.Count > FrozenList.Count)
    {
      foreach (FrozenIndex frozen in socketManager.resultData.payload.frozenIndices)
      {
        FrozenList.Add(frozen);
      }
    }

    // int columnsInRow0 = socketManager.resultData.matrix[0].Count;
    // int columnsInRow1 = socketManager.resultData.matrix.Count > 1 ? socketManager.resultData.matrix[1].Count : 0;

    // int k = 0;
    // for (int i = 0; i < columnsInRow0; i++)
    // {
    //   if (int.Parse(socketManager.resultData.matrix[0][i]) == 0 && !IsFrozen(0, i))
    //   {
    //     if (i < columnsInRow1)
    //     {
    //       if (i != 1)
    //       {
    //         yield return StopRedRespin(i, k, socketManager.resultData.payload.currentWinning, false);
    //         k++;
    //       }
    //       else
    //       {
    //         yield return StopRedRespin(i, k, socketManager.resultData.payload.currentWinning, true);
    //         k++;
    //       }
    //     }
    //   }
    // }

    for (int i = 0; i < SlotNumber + 1; i++)
    {
      if (i == 1)
      {
        yield return StopTweening(5, Slot_Transform[i], i, socketManager.resultData.payload.currentWinning, true, true);
      }
      else
      {
        yield return StopTweening(5, Slot_Transform[i], i, socketManager.resultData.payload.currentWinning, true);
      }
    }

    if (IsTurboOn) yield return null;
    else yield return new WaitForSeconds(2f);

    KillAllRedTweens();
    StartRedAnimation();

    yield return new WaitForSeconds(1f);

    if (uiController) uiController.GreenRespin(false);
    if (uiController) uiController.RedRespin(false);
  }

  private IEnumerator InitiateRedRespin(int value, bool isMid)
  {
    InitializeTweening(RedSlot_Transform[value], false, isMid);
    yield return new WaitForSeconds(0.1f);
  }

  #endregion

  #region PopulateLogic

  private void PopulateRedSpin()
  {
    for (int i = 0; i < SlotNumber + 1; i++)
    {
      if (!IsFrozen(0, i))
      {
        int symbolIndex = int.Parse(socketManager.resultData.matrix[0][i]);
        if (symbolIndex == 0)
        {
          int randomIndex = Random.Range(7, 10);
          symbolIndex = randomIndex;
        }
        PopulateRedAnimationSprites(RedStop_Anims[i], RedStop_Images[i], symbolIndex);
      }
    }
  }

  void StartRedAnimation()
  {
    uiController?.resetWinColor();
    for (int i = 0; i < RedStop_Anims.Length; i++)
    {
      if (i > SlotNumber)
      {
        break;
      }
      if (RedStop_Anims[i].textureArray.Count > 0) RedStop_Anims[i].StartAnimation();
      if (socketManager.resultData.payload.currentWinning > 0)
      {
        if (int.Parse(socketManager.resultData.matrix[0][i]) != 0)
        {
          uiController?.AddWinColor(i);
        }
      }
    }
  }

  private void PopulateNormalSpin()
  {
    for (int i = 0; i < SlotNumber + 1; i++)
    {
      if (!IsFrozen(0, i))
      {
        int symbolIndex = int.Parse(socketManager.resultData.matrix[0][i]);
        if (symbolIndex == 0)
        {
          int randomIndex = Random.Range(7, 10);
          symbolIndex = randomIndex;
        }
        PopulateAnimationSprites(Stop_Anims[i], Stop_Images[i], symbolIndex);
      }
    }
  }

  private void StartNormalAnimation()
  {
    uiController?.resetWinColor();
    for (int i = 0; i < Stop_Anims.Length; i++)
    {
      if (i > SlotNumber)
      {
        break;
      }
      if (Stop_Anims[i].textureArray.Count > 0) Stop_Anims[i].StartAnimation();
      if (socketManager.resultData.payload.currentWinning > 0)
      {
        if (int.Parse(socketManager.resultData.matrix[0][i]) != 0)
        {
          uiController?.AddWinColor(i);
        }
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
    // Debug.Log("Setting sprite val: " + val);
    StopImage.sprite = Slot_Sprites[val];
  }

  private void PopulateRedAnimationSprites(ImageAnimation animScript, Image StopImage, int val)
  {
    animScript.textureArray.Clear();
    animScript.textureArray.TrimExcess();
    animScript.AnimationSpeed = 8;
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
        animScript.AnimationSpeed = 6;
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


  private void InitializeTweening(Transform slotTransform, bool isGreen, bool IsMid = false)
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
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, 0f, slotTransform.localPosition.z);
      tweener = slotTransform.DOLocalMoveY(-myTweenHeight, 1f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
      redalltweens.Add(tweener);
    }
    else
    {
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, -myTweenHeight, slotTransform.localPosition.z);
      tweener = slotTransform.DOLocalMoveY(0, 1f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
      alltweens.Add(tweener);
    }
    tweener.Play();
  }

  private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, double winning, bool isGreen, bool isMid = false)
  {
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
    bool IsRegister = false;
    if (!IsTurboOn)
    {
      // register callback and wait until it triggers
      IsRegister = false;
      if (index >= 0 && index < alltweens.Count && alltweens[index] != null)
      {
        alltweens[index].OnStepComplete(() => { IsRegister = true; });
      }
      yield return new WaitUntil(() => IsRegister);
    }

    if (uiController) uiController.StopSpin_Button.gameObject.SetActive(false);
    alltweens[index].Kill();
    int tweenpos = (reqpos * (mySizeFactor + mySpaceFactor)) - (mySizeFactor + (2 * mySpaceFactor));

    if (!isGreen)
    {
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, 0f, slotTransform.localPosition.z);
      if (index >= 0)
      {
        Tweener t = slotTransform.DOLocalMoveY(-tweenpos + 100 + (mySpaceFactor > 0 ? mySpaceFactor / 4 : 0), 0.7f).SetEase(Ease.OutBounce);
        redalltweens.Add(t);
      }
    }
    else
    {
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, -myTweenHeight, slotTransform.localPosition.z);
      if (index >= 0)
      {
        Tweener t = slotTransform.DOLocalMoveY(-tweenpos + 100, 1f);
        alltweens.Add(t);
      }
    }

    if (!StopSpinToggle)
    {
      alltweens[index].WaitForCompletion();
    }

    if (winning > 0)
    {
      if (audioController) audioController.PlayWLAudio("win");
    }
    else
    {
      if (audioController) audioController.PlayWLAudio("dot");
    }
  }

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

  private bool IsFrozen(int row, int column)
  {
    // return FrozenList != Vector2.zero && new Vector2(row, column) == FrozenList;
    if (FrozenList.Count > 0)
    {
      foreach (FrozenIndex frozen in FrozenList)
      {
        if (frozen.position[1] == column)
          return true;
      }
    }
    return false;
  }

}
