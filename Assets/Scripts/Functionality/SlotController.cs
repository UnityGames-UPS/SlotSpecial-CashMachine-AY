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
  private Dictionary<int, Tweener> alltweens = new(3);
  private Dictionary<int, Tweener> redalltweens = new(3);

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
  private int[] RandomEmptyIndex = new int[3];
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
      if (uiController) uiController.ToggleButtonGrp(true);
      yield break;
    }
    
    if (!IsTurboOn)
    {
      uiController.StopSpin_Button.gameObject.SetActive(true);
    }

    IsSpinning = true;
    ResetAllAnims();
    uiController.resetWinColor();
    if (uiController) uiController.UpdateTweenBalance(socketManager.Bets[BetCounter]);

    for (int i = 0; i < SlotNumber + 1; i++)
    {
      if (i == 1)
      {
        InitializeTweening(Slot_Transform[i], i, true, true);
        yield return new WaitForSeconds(0.1f);
      }
      else
      {
        InitializeTweening(Slot_Transform[i], i, true);
        yield return new WaitForSeconds(0.1f);
      }
    }
    if (audioController) audioController.PlayWLAudio("spin");

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

    if (IsTurboOn)
    {
      yield return null;
    }
    else
    {
      for (int i = 0; i < 10; i++)
      {
        if (StopSpinToggle)
        {
          break;
        }
        yield return null;
      }
    }

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
    StopSpinToggle = false;
    StartNormalAnimation();
    // wait for last tween to finish safely
    if (alltweens.Count > 0)
      yield return alltweens[SlotNumber].WaitForCompletion();
    KillAllTweens();
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
    IsSpinning = false;
    if (!IsAutoSpin)
    {
      if (uiController) uiController.ToggleButtonGrp(true);
    }
  }


  void ResetAllAnims()
  {
    ResetNormalAnims();
    ResetRedAnims();
  }

  void ResetRedAnims()
  {
    foreach (ImageAnimation image in RedStop_Anims)
    {
      image.StopAnimation();
    }
  }

  private void ResetNormalAnims()
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

    socketManager.AccumulateResult(DenomCounter, SlotNumber);
    yield return new WaitUntil(() => socketManager.isResultdone);
    PopulateNormalSpin();

    if (IsTurboOn)
    {
      yield return null;
    }
    else
    {
      yield return new WaitForSeconds(2f);
    } 
    
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
    InitializeTweening(Slot_Transform[value], value, true, isMid);
    yield return new WaitForSeconds(0.1f);
  }

  private IEnumerator StopGreenRespin(int value, int tweenvalue, double isMoney, bool isMid)
  {
    yield return StopTweening(5, Slot_Transform[value], tweenvalue, isMoney, true, isMid);
  }
  #endregion

  #region RedSpinLogic
  private IEnumerator RedSpinLogic()
  {
    PopulateRedSpin(false);
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

    ResetNormalAnims();
    PopulateNormalSpin();
    PopulateRedSpin(true);
    if (socketManager.resultData.payload.frozenIndices.Count > FrozenList.Count)
    {
      foreach (FrozenIndex frozen in socketManager.resultData.payload.frozenIndices)
      {
        FrozenList.Add(frozen);
      }
    }

    if (IsTurboOn)
      yield return null;
    else
      yield return new WaitForSeconds(1f);

    int k = -1;
    for (int i = 0; i < SlotNumber + 1; i++)
    {
      if (!IsFrozen(0, i))
      {
        k = i;
        if (i == 1)
        {
          yield return StopTweening(5, RedSlot_Transform[i], i, socketManager.resultData.payload.currentWinning, false, true);
        }
        else
        {
          yield return StopTweening(5, RedSlot_Transform[i], i, socketManager.resultData.payload.currentWinning, false);
        }
      }
    }

    if (k != -1)
      yield return redalltweens[k].WaitForCompletion();

    KillAllRedTweens();
    StartRedAnimation();
    yield return new WaitForSeconds(2f);

    if (uiController) uiController.GreenRespin(false);
    if (uiController) uiController.RedRespin(false);
    StartNormalAnimation();
  }

  private IEnumerator InitiateRedRespin(int value, bool isMid)
  {
    InitializeTweening(RedSlot_Transform[value], value, false, isMid);
    yield return new WaitForSeconds(0.1f);
  }

  #endregion

  #region PopulateLogic

  private void PopulateRedSpin(bool checkIsFrozen)
  {
    for (int i = 0; i < SlotNumber + 1; i++)
    {
      if (checkIsFrozen)
      {
        if (IsFrozen(0, i))
        {
          continue;
        }
      }
      int symbolIndex = int.Parse(socketManager.resultData.matrix[0][i]);
      if (symbolIndex == 0)
      {
        symbolIndex = RandomEmptyIndex[i];
      }
      PopulateRedAnimationSprites(RedStop_Anims[i], RedStop_Images[i], symbolIndex);
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
          RandomEmptyIndex[i] = Random.Range(7, 10);
          symbolIndex = RandomEmptyIndex[i];
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


  private void InitializeTweening(Transform slotTransform, int index, bool isGreen, bool IsMid = false)
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
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, -myTweenHeight, slotTransform.localPosition.z);
      tweener = slotTransform.DOLocalMoveY(0, 1f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
      redalltweens[index] = tweener;
    }
    else
    {
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, 0f, slotTransform.localPosition.z);
      tweener = slotTransform.DOLocalMoveY(-myTweenHeight, 1f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
      alltweens[index] = tweener;
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
      if (isGreen)
      {
        alltweens[index]?.OnStepComplete(() => { IsRegister = true; });
        yield return new WaitUntil(() => IsRegister);
      }
      else
      {
        redalltweens[index]?.OnStepComplete(() => { IsRegister = true; });
        yield return new WaitUntil(() => IsRegister);
      }
    }

    if (uiController) uiController.StopSpin_Button.gameObject.SetActive(false);
    if (isGreen)
    {
      alltweens[index].Kill();
    }
    else
    {
      redalltweens[index].Kill();
    }

    int tweenpos = (reqpos * (mySizeFactor + mySpaceFactor)) - (mySizeFactor + (2 * mySpaceFactor));
    if (!isGreen)
    {
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, -4783f, slotTransform.localPosition.z);
      Tweener t = slotTransform.DOLocalMoveY(-tweenpos + 100 + (mySpaceFactor > 0 ? mySpaceFactor / 4 : 0), 0.7f).SetEase(Ease.OutBounce);
      redalltweens[index] = t;
    }
    else
    {
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, 0f, slotTransform.localPosition.z);
      Tweener t = slotTransform.DOLocalMoveY(-tweenpos + 100 + (mySpaceFactor > 0 ? mySpaceFactor / 4 : 0), 0.7f).SetEase(Ease.OutBounce);
      alltweens[index] = t;
    }

    if (isGreen)
    {
      if (!StopSpinToggle)
      {
        yield return alltweens[index].WaitForCompletion();
      }
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
    foreach (KeyValuePair<int, Tweener> pair in alltweens)
    {
      pair.Value.Kill();
    }
    alltweens.Clear();
    alltweens.TrimExcess();
  }

  private void KillAllRedTweens()
  {
    foreach (KeyValuePair<int, Tweener> pair in redalltweens)
    {
      pair.Value.Kill();
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
