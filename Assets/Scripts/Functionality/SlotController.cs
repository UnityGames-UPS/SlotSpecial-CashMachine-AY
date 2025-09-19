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
    private List<FrozenIndex> frozenIndices;

    private Coroutine AutoSpinRoutine = null;
    private Coroutine tweenroutine = null;
    private string[,] frozenMatrix;

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
                InitializeTweening(Slot_Transform[i], 1, false, true);
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                InitializeTweening(Slot_Transform[i], 1, false);
                yield return new WaitForSeconds(0.1f);
            }
        }

        socketManager.AccumulateResult(DenomCounter, SlotNumber);

        yield return new WaitUntil(() => socketManager.isResultdone);

        if (socketManager.resultData.payload.frozenIndices != null && socketManager.resultData.matrix != null && socketManager.resultData.matrix.Count > 0)
        {
            int rows = socketManager.resultData.matrix.Count;
            // use max columns among rows so frozenMatrix covers any ragged rows
            int cols = 0;
            for (int r = 0; r < rows; r++)
                cols = Mathf.Max(cols, socketManager.resultData.matrix[r].Count);

            frozenMatrix = new string[rows, cols];

            foreach (var f in socketManager.resultData.payload.frozenIndices)
            {
                int row = f.position[0];
                int col = f.position[1];
                if (row >= 0 && row < rows && col >= 0 && col < cols)
                    frozenMatrix[row, col] = f.symbol;
            }
        }
        else
        {
            frozenMatrix = null;
        }

        PopulateNormalSpin(0, false);
        for (int i = 0; i < SlotNumber + 1; i++)
        {
            if (i == 1)
            {
                yield return StopTweening(5, Slot_Transform[i], i, 1, int.Parse(socketManager.resultData.matrix[0][i]), false, true);
            }
            else
            {
                yield return StopTweening(5, Slot_Transform[i], i, 1, int.Parse(socketManager.resultData.matrix[0][i]), false);
            }
        }
        StopSpinToggle = false;
        StartNormalAnimation(0);
        // wait for last tween to finish safely
        if (alltweens.Count > 0)
            yield return alltweens[alltweens.Count - 1].WaitForCompletion();
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

        for (int len = 0; len < length; len++)
        {
            // Start regular spin for this respin iteration
            yield return StartRespinIteration(len);

            yield return new WaitForSeconds(1f);
        }
        if (uiController) uiController.GreenRespin(false);
    }


    private IEnumerator InitiateGreenRespin(int value, bool isMid)
    {
        InitializeTweening(Slot_Transform[value], 1, true, isMid);
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
        if (IsTurboOn) yield return new WaitForSeconds(2f);
        else yield return new WaitForSeconds(3f);

        // Start red respin iteration
        yield return StartRespinIteration(0);

        if (IsTurboOn) yield return new WaitForSeconds(1f);
        else yield return new WaitForSeconds(2f);

        if (uiController) uiController.GreenRespin(false);
        if (uiController) uiController.RedRespin(false);
    }

    private IEnumerator InitiateRedRespin(int value, bool isMid)
    {
        InitializeTweening(RedSlot_Transform[value], 2, false, isMid);
        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator StopRedRespin(int value, int tweenvalue, int isMoney, bool isMid)
    {
        // Wait for the red tween to complete naturally instead of killing it abruptly
        yield return StopTweening(5, RedSlot_Transform[value], tweenvalue, 2, isMoney, false, isMid);

        // Start animations after the tween completes
        if (socketManager.resultData.matrix.Count > 1) // Ensure row 1 exists
        {
            int columnsInRow1 = socketManager.resultData.matrix[1].Count;
            for (int i = 0; i < SlotNumber + 1 && i < columnsInRow1; i++)
            {
                if (int.Parse(socketManager.resultData.matrix[1][i]) != 0)
                {
                    if (i < RedStop_Anims.Length && RedStop_Anims[i] != null)
                    {
                        RedStop_Anims[i].StartAnimation();
                    }
                }
            }
        }
    }
    #endregion

    #region Respin Helper Methods

    private IEnumerator StartRespinIteration(int respinLevel)
    {
        // Store current frozen state
        var currentFrozenMatrix = frozenMatrix;

        if (audioController) audioController.PlayWLAudio("spin");

        // Start spinning (reuse existing spin logic)
        IsSpinning = true;
        ResetSpin();

        // Start tweening for all slots (frozen ones will be updated later)
        for (int i = 0; i < SlotNumber + 1; i++)
        {
            if (i == 1)
            {
                InitializeTweening(Slot_Transform[i], 1, false, true);
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                InitializeTweening(Slot_Transform[i], 1, false);
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Send respin request to backend
        socketManager.AccumulateResult(DenomCounter, SlotNumber);
        yield return new WaitUntil(() => socketManager.isResultdone);

        // Update frozen matrix with new backend response
        UpdateFrozenMatrix();

        // Populate results from backend
        PopulateNormalSpin(respinLevel, false);

        // Stop tweening and show results - only update unfrozen slots
        for (int i = 0; i < SlotNumber + 1; i++)
        {
            int symbolId = 0;
            if (socketManager.resultData.matrix != null &&
                respinLevel < socketManager.resultData.matrix.Count &&
                i < socketManager.resultData.matrix[respinLevel].Count)
            {
                symbolId = int.Parse(socketManager.resultData.matrix[respinLevel][i]);
            }

            if (i == 1)
            {
                yield return StopTweening(5, Slot_Transform[i], i, 1, symbolId, false, true);
            }
            else
            {
                yield return StopTweening(5, Slot_Transform[i], i, 1, symbolId, false);
            }
        }

        StartNormalAnimation(respinLevel);

        // Wait for last tween to finish safely
        if (alltweens.Count > 0)
            yield return alltweens[alltweens.Count - 1].WaitForCompletion();

        KillAllTweens();

        // Show winnings if any for this respin
        if (socketManager.resultData.payload?.currentWinning > 0)
        {
            yield return uiController.UpdateWinnings(socketManager.playerdata.balance, socketManager.resultData.payload.currentWinning);
        }

        IsSpinning = false;
    }

    private void UpdateFrozenMatrix()
    {
        // Update frozen matrix based on backend respin response
        if (socketManager.resultData?.payload?.frozenIndices != null &&
            socketManager.resultData.matrix != null &&
            socketManager.resultData.matrix.Count > 0)
        {
            int rows = socketManager.resultData.matrix.Count;
            int cols = 0;
            for (int r = 0; r < rows; r++)
                cols = Mathf.Max(cols, socketManager.resultData.matrix[r].Count);

            frozenMatrix = new string[rows, cols];

            foreach (var f in socketManager.resultData.payload.frozenIndices)
            {
                int row = f.position[0];
                int col = f.position[1];
                if (row >= 0 && row < rows && col >= 0 && col < cols)
                    frozenMatrix[row, col] = f.symbol;
            }
        }
    }
    #endregion

    #region PopulateLogic
    private void PopulateRedSpin(int rowIndex, bool isFirst)
    {
        var matrix = socketManager.resultData?.matrix;
        if (matrix == null || rowIndex < 0 || rowIndex >= matrix.Count) return;

        var row = matrix[rowIndex];
        int cols = row?.Count ?? 0;
        int uiCols = Mathf.Min(cols, RedStop_Images.Length);

        for (int col = 0; col < uiCols; col++)
        {
            string frozenSymbol = (frozenMatrix != null && rowIndex < frozenMatrix.GetLength(0) && col < frozenMatrix.GetLength(1))
                ? frozenMatrix[rowIndex, col]
                : null;

            if (!string.IsNullOrEmpty(frozenSymbol))
            {
                if (int.TryParse(frozenSymbol, out int symbolId) && symbolId != 0)
                    PopulateRedAnimationSprites(RedStop_Anims[col], RedStop_Images[col], symbolId);
                else if (!isFirst)
                {
                    int m_index = UnityEngine.Random.Range(7, 10);
                    RedStop_Images[col].sprite = RedSlot_Sprites[m_index];
                    if (col < Stop_Images.Length) Stop_Images[col].sprite = Slot_Sprites[m_index];
                }
            }
            else
            {
                if (int.TryParse(row[col], out int symbolId) && symbolId != 0)
                {
                    PopulateRedAnimationSprites(RedStop_Anims[col], RedStop_Images[col], symbolId);
                }
                else if (!isFirst)
                {
                    int m_index = UnityEngine.Random.Range(7, 10);
                    RedStop_Images[col].sprite = RedSlot_Sprites[m_index];
                    if (col < Stop_Images.Length) Stop_Images[col].sprite = Slot_Sprites[m_index];
                }
            }
        }
    }

    private void PopulateNormalSpin(int rowIndex, bool isFirst)
    {
        var matrix = socketManager.resultData?.matrix;
        if (matrix == null || rowIndex < 0 || rowIndex >= matrix.Count) return;

        var row = matrix[rowIndex];
        int cols = row?.Count ?? 0;
        int uiCols = Mathf.Min(cols, Stop_Images.Length);

        for (int col = 0; col < uiCols; col++)
        {
            string frozenSymbol = (frozenMatrix != null && rowIndex < frozenMatrix.GetLength(0) && col < frozenMatrix.GetLength(1))
                ? frozenMatrix[rowIndex, col]
                : null;

            if (!string.IsNullOrEmpty(frozenSymbol))
            {
                if (int.TryParse(frozenSymbol, out int symbolId) && symbolId != 0)
                    PopulateAnimationSprites(Stop_Anims[col], Stop_Images[col], symbolId);
                else if (!isFirst)
                {
                    int m_index = UnityEngine.Random.Range(7, 10);
                    Stop_Images[col].sprite = Slot_Sprites[m_index];
                    if (col < RedStop_Images.Length) RedStop_Images[col].sprite = RedSlot_Sprites[m_index];
                }
            }
            else
            {
                if (int.TryParse(row[col], out int symbolId) && symbolId != 0)
                {
                    PopulateAnimationSprites(Stop_Anims[col], Stop_Images[col], symbolId);
                }
                else if (!isFirst)
                {
                    int m_index = UnityEngine.Random.Range(7, 10);
                    Stop_Images[col].sprite = Slot_Sprites[m_index];
                    if (col < RedStop_Images.Length) RedStop_Images[col].sprite = RedSlot_Sprites[m_index];
                }
            }
        }
    }

    private void StartNormalAnimation(int rowIndex)
    {
        var matrix = socketManager.resultData?.matrix;
        if (matrix == null || rowIndex < 0 || rowIndex >= matrix.Count) return;

        var row = matrix[rowIndex];
        int cols = row?.Count ?? 0;
        int uiCols = Mathf.Min(cols, Stop_Anims.Length);

        for (int col = 0; col < uiCols; col++)
        {
            if (int.TryParse(row[col], out int symbolId) && symbolId != 0)
            {
                if (Stop_Anims[col] != null) Stop_Anims[col].StartAnimation();
                uiController?.AddWinColor(col);
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
            slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, 0f, slotTransform.localPosition.z);
            tweener = slotTransform.DOLocalMoveY(-myTweenHeight, 1f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
        }
        else
        {
            slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, -myTweenHeight, slotTransform.localPosition.z);
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

    private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, int type, int isMoney, bool isGreen, bool isMid = false)
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
        if (type == 1)
        {

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
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
            if (uiController) uiController.StopSpin_Button.gameObject.SetActive(false);
            if (index >= 0 && index < alltweens.Count && alltweens[index] != null)
                alltweens[index].Kill();
            int tweenpos = (reqpos * (mySizeFactor + mySpaceFactor)) - (mySizeFactor + (2 * mySpaceFactor));
            if (!isGreen)
            {
                slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, 0f, slotTransform.localPosition.z);
                if (index >= 0)
                {
                    Tweener t = slotTransform.DOLocalMoveY(-tweenpos + 100 + (mySpaceFactor > 0 ? mySpaceFactor / 4 : 0), 0.7f).SetEase(Ease.OutBounce);
                    if (index < alltweens.Count) alltweens[index] = t;
                    else alltweens.Add(t);
                }
            }
            else
            {
                slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, -myTweenHeight, slotTransform.localPosition.z);
                if (index >= 0)
                {
                    Tweener t = slotTransform.DOLocalMoveY(-tweenpos + 100, 1f);
                    if (index < alltweens.Count) alltweens[index] = t;
                    else alltweens.Add(t);
                }
            }

            if (!StopSpinToggle)
            {
                if (index >= 0 && index < alltweens.Count && alltweens[index] != null)
                    yield return alltweens[index].WaitForCompletion();
            }
            else
            {

                yield return null;

            }
        }
        else
        {
            bool IsRegister = false;
            // register callback and wait
            if (index >= 0 && index < redalltweens.Count && redalltweens[index] != null)
            {
                redalltweens[index].OnStepComplete(() => { IsRegister = true; });
            }
            yield return new WaitUntil(() => IsRegister);
            if (index >= 0 && index < redalltweens.Count && redalltweens[index] != null)
                redalltweens[index].Kill();
            int tweenpos = (reqpos * (mySizeFactor + mySpaceFactor)) - (mySizeFactor + (2 * mySpaceFactor));
            Tweener newT = slotTransform.DOLocalMoveY(-tweenpos + 100 + (mySpaceFactor > 0 ? mySpaceFactor / 4 : 0), 0.7f).SetEase(Ease.OutBounce);
            if (index >= 0)
            {
                if (index < redalltweens.Count) redalltweens[index] = newT;
                else redalltweens.Add(newT);
            }
            yield return newT.WaitForCompletion();
            if (index >= 0 && index < redalltweens.Count && redalltweens[index] != null)
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
        return frozenMatrix != null && !string.IsNullOrEmpty(frozenMatrix[row, column]);
    }

}
