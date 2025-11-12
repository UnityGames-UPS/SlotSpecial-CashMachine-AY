using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
  #region References
  [Header("Main Buttons")]
  [SerializeField]
  private Button Spin_Button;
  [SerializeField]
  private Button StopAutoSpin_Button;
  [SerializeField]
  private Button AutoSpinPanel_Button;
  [SerializeField]
  private Button GameExit_Button;
  [SerializeField]
  private Button Turbo_Button;
  [SerializeField]
  internal Button StopSpin_Button;
  private Sprite turboOriginalSprite;

  [Header("Main Texts")]
  [SerializeField]
  private TMP_Text BalanceMain_Text;
  [SerializeField]
  private TMP_Text BetMain_Text;
  [SerializeField]
  private TMP_Text WinMain_Text;
  [SerializeField]
  private TMP_Text Message_Text;

  [Header("AutoSpin Panel")]
  [SerializeField]
  private GameObject AT_GameObject;
  [SerializeField]
  private RectTransform AT_Transform;
  [SerializeField]
  private Transform AT_ImageTransform;
  [SerializeField]
  private Button[] AutoCount_Buttons;
  [SerializeField]
  private GameObject SpinImage_Object;
  [SerializeField]
  private GameObject AutoSpinImage_Object;
  [SerializeField]
  private TMP_Text AutoCounter_Text;

  [Header("Screen Raycast")]
  [SerializeField]
  private Button RayCast_Button;
  [SerializeField]
  private GameObject RayCast_Object;
  [SerializeField]

  [Header("Bet Popup")]
  private GameObject BetPanel_Object;
  [SerializeField]
  private Button Bet_Button;
  [SerializeField]
  private Slider Bet_Slider;
  [SerializeField]
  private Slider Denom_Slider;
  [SerializeField]
  private TMP_Text Bet_Text;
  [SerializeField]
  private TMP_Text Denom_Text;
  [SerializeField]
  private Button Betplus_Button;
  [SerializeField]
  private Button Betminus_Button;
  [SerializeField]
  private Button Denomplus_Button;
  [SerializeField]
  private Button Denomminus_Button;

  [Header("Slots BackGround")]
  [SerializeField]
  private Image[] Slots_image;
  [SerializeField]
  private Color Disabled_Color;
  [SerializeField]
  private Color Win_Color;

  [Header("Menu Setup")]
  [SerializeField]
  private Button Menu_Button;
  [SerializeField]
  private Button Sound_Button;
  [SerializeField]
  private Button Mute_Button;
  [SerializeField]
  private Button Rules_Button;
  [SerializeField]
  private Button BetSettings_Button;
  [SerializeField]
  private Button Settings_Button;
  [SerializeField]
  private RectTransform MenuButtons_Rect;
  [SerializeField]
  private GameObject MenuButton_Object;

  [Header("TitleSettings")]
  [SerializeField]
  private GameObject CMHeading_object;
  [SerializeField]
  private GameObject RespinHeading_object;

  [Header("Red UI Setup")]
  [SerializeField]
  private GameObject RedSpinSetup;

  [Header("RulesPopup")]
  [SerializeField]
  private GameObject RulesPopup;
  [SerializeField]
  private Button CloseRules_Button;
  [SerializeField]
  private TMP_Text ZeroSpin_Text;
  [SerializeField]
  private TMP_Text RedSpin_Text;
  [SerializeField]
  private TMP_Text ReelActivation_Text;

  [Header("AutSpinPopup")]
  [SerializeField]
  private GameObject AutoSpinPopup;
  [SerializeField]
  private Button CloseAutoPopup_Button;
  [SerializeField]
  private Slider AutoSpin_Slider;
  [SerializeField]
  private TMP_Text AutoSpinSetup_Text;
  [SerializeField]
  private Button AutoSpinPlus_Button;
  [SerializeField]
  private Button AutoSpinMinus_Button;

  [Header("Win Popup")]
  [SerializeField]
  private GameObject NiceWinPopup;
  [SerializeField]
  private Button NiceWinPopupCloseBtn;

  [Header("Exit Popup")]
  [SerializeField]
  private GameObject ExitPopup_Object;
  [SerializeField]
  private Button YesExit_Button;
  [SerializeField]
  private Button NoExit_Button;
  [SerializeField]
  private Button CloseExitButton;

  [Header("Low Balance Popup")]
  [SerializeField]
  private GameObject LBPopup_Object;
  [SerializeField]
  private Button CloseLB_Button;

  [Header("Reconnection Popup")]
  [SerializeField] private GameObject ReconnectPopup_Object;

  [Header("Disconnection Popup")]
  [SerializeField]
  private GameObject DisconnectionPopup_Object;
  [SerializeField]
  private Button CloseDisconnect_Button;

  [Header("Audio Setup")]
  [SerializeField]
  private GameObject Mute_Object;
  [SerializeField]
  private GameObject Sound_Object;

  [Header("Main Popup BG")]
  [SerializeField]
  private GameObject PopupMain_Object;

  [Header("Controllers")]
  [SerializeField]
  private SlotController slotManager;
  [SerializeField]
  private AudioController audioController;
  #endregion

  [SerializeField] private SocketIOManager socketManager;
  [SerializeField] private List<int> availableBets = new List<int>();   // features
  [SerializeField] private List<int> availableDenoms = new List<int>(); // bets

  private int SpinCount = 0;
  private int currentBet = 10;
  private bool isAtOpen = false;
  [SerializeField] private bool isBetOpen = false;
  private bool isMenuOpen = false;
  private bool SkipWin;
  private bool isExit = false;

  private void Start()
  {
    isAtOpen = false;
    isBetOpen = false;
    isMenuOpen = false;
    Initialisation();
  }

  #region Initial Setup
  private void Initialisation()
  {
    if (Spin_Button) Spin_Button.onClick.RemoveAllListeners();
    if (Spin_Button) Spin_Button.onClick.AddListener(delegate { audioController.PlaySpinButtonAudio(); StartSpinning(); });

    if (AutoSpinPanel_Button) AutoSpinPanel_Button.onClick.RemoveAllListeners();
    if (AutoSpinPanel_Button) AutoSpinPanel_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); OnATClick(); });

    if (RayCast_Button) RayCast_Button.onClick.RemoveAllListeners();
    if (RayCast_Button) RayCast_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); OnRayCastClick(); });

    if (StopAutoSpin_Button) StopAutoSpin_Button.onClick.RemoveAllListeners();
    if (StopAutoSpin_Button) StopAutoSpin_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); StoppingAutoSpin(); });

    if (Bet_Button) Bet_Button.onClick.RemoveAllListeners();
    if (Bet_Button) Bet_Button.onClick.AddListener(delegate { OnBetClick(); audioController.PlayButtonAudio(); });

    if (Bet_Slider) Bet_Slider.onValueChanged.RemoveAllListeners();
    if (Bet_Slider) Bet_Slider.onValueChanged.AddListener(OnBetChange);

    if (Denom_Slider) Denom_Slider.onValueChanged.RemoveAllListeners();
    if (Denom_Slider) Denom_Slider.onValueChanged.AddListener(OnDenomChange);

    if (Betplus_Button) Betplus_Button.onClick.RemoveAllListeners();
    if (Betplus_Button) Betplus_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); OnBetButton(true); });

    if (Betminus_Button) Betminus_Button.onClick.RemoveAllListeners();
    if (Betminus_Button) Betminus_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); OnBetButton(false); });

    if (Denomplus_Button) Denomplus_Button.onClick.RemoveAllListeners();
    if (Denomplus_Button) Denomplus_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); OnDenomButton(true); });

    if (Denomminus_Button) Denomminus_Button.onClick.RemoveAllListeners();
    if (Denomminus_Button) Denomminus_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); OnDenomButton(false); });

    if (Menu_Button) Menu_Button.onClick.RemoveAllListeners();
    if (Menu_Button) Menu_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); OnMenuClick(); });

    if (Mute_Button) Mute_Button.onClick.RemoveAllListeners();
    if (Mute_Button) Mute_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); ToggleSound(false); });

    if (Sound_Button) Sound_Button.onClick.RemoveAllListeners();
    if (Sound_Button) Sound_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); ToggleSound(true); });

    if (BetSettings_Button) BetSettings_Button.onClick.RemoveAllListeners();
    if (BetSettings_Button) BetSettings_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); OnBetClick(); });

    if (Rules_Button) Rules_Button.onClick.RemoveAllListeners();
    if (Rules_Button) Rules_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); OpenRulesPopup(); });

    if (Settings_Button) Settings_Button.onClick.RemoveAllListeners();
    if (Settings_Button) Settings_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); OpenSettingsPopup(); AutoSpin_Slider.value = 0; });

    if (CloseRules_Button) CloseRules_Button.onClick.RemoveAllListeners();
    if (CloseRules_Button) CloseRules_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); CloseRulesPopup(); });

    if (CloseAutoPopup_Button) CloseAutoPopup_Button.onClick.RemoveAllListeners();
    if (CloseAutoPopup_Button) CloseAutoPopup_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); CloseSettingsPopup(); });

    if (AutoSpin_Slider) AutoSpin_Slider.onValueChanged.RemoveAllListeners();
    if (AutoSpin_Slider) AutoSpin_Slider.onValueChanged.AddListener(OnATSlide);

    if (AutoSpinPlus_Button) AutoSpinPlus_Button.onClick.RemoveAllListeners();
    if (AutoSpinPlus_Button) AutoSpinPlus_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); ToggleAutoSpin(true); });

    if (AutoSpinMinus_Button) AutoSpinMinus_Button.onClick.RemoveAllListeners();
    if (AutoSpinMinus_Button) AutoSpinMinus_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); ToggleAutoSpin(false); });

    if (GameExit_Button) GameExit_Button.onClick.RemoveAllListeners();
    if (GameExit_Button) GameExit_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); TogglePopup(ExitPopup_Object, true); });

    if (CloseExitButton) CloseExitButton.onClick.RemoveAllListeners();
    if (CloseExitButton) CloseExitButton.onClick.AddListener(delegate { audioController.PlayButtonAudio(); if (!isExit) TogglePopup(ExitPopup_Object); });

    if (NoExit_Button) NoExit_Button.onClick.RemoveAllListeners();
    if (NoExit_Button) NoExit_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); if (!isExit) TogglePopup(ExitPopup_Object); });

    if (YesExit_Button) YesExit_Button.onClick.RemoveAllListeners();
    if (YesExit_Button) YesExit_Button.onClick.AddListener(delegate { audioController.PlayButtonAudio(); CallOnExitFunction(); });

    if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.RemoveAllListeners();
    if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.AddListener(delegate { CallOnExitFunction(); });

    if (CloseLB_Button) CloseLB_Button.onClick.RemoveAllListeners();
    if (CloseLB_Button) CloseLB_Button.onClick.AddListener(delegate { TogglePopup(LBPopup_Object); });

    if (StopSpin_Button) StopSpin_Button.onClick.RemoveAllListeners();
    if (StopSpin_Button) StopSpin_Button.onClick.AddListener(() => { slotManager.StopSpinToggle = true; StopSpin_Button.gameObject.SetActive(false); if (audioController) audioController.PlayButtonAudio(); });

    if (Turbo_Button) Turbo_Button.onClick.RemoveAllListeners();
    if (Turbo_Button) Turbo_Button.onClick.AddListener(TurboToggle);

    if (NiceWinPopupCloseBtn) NiceWinPopupCloseBtn.onClick.RemoveAllListeners();
    if (NiceWinPopupCloseBtn) NiceWinPopupCloseBtn.onClick.AddListener(delegate { SkipWin = true; ToggleWinPopup(false); });

    for (int i = 0; i < AutoCount_Buttons.Length; i++)
    {
      switch (i)
      {
        case 0:
          if (AutoCount_Buttons[i]) AutoCount_Buttons[i].onClick.RemoveAllListeners();
          if (AutoCount_Buttons[i]) AutoCount_Buttons[i].onClick.AddListener(delegate { audioController.PlayButtonAudio(); OnATClick(); OnATSlide(0); });
          break;
        case 1:
          if (AutoCount_Buttons[i]) AutoCount_Buttons[i].onClick.RemoveAllListeners();
          if (AutoCount_Buttons[i]) AutoCount_Buttons[i].onClick.AddListener(delegate { audioController.PlayButtonAudio(); ChangeAutoView(10); OnATClick(); });
          break;
        case 2:
          if (AutoCount_Buttons[i]) AutoCount_Buttons[i].onClick.RemoveAllListeners();
          if (AutoCount_Buttons[i]) AutoCount_Buttons[i].onClick.AddListener(delegate { audioController.PlayButtonAudio(); ChangeAutoView(25); OnATClick(); });
          break;
        case 3:
          if (AutoCount_Buttons[i]) AutoCount_Buttons[i].onClick.RemoveAllListeners();
          if (AutoCount_Buttons[i]) AutoCount_Buttons[i].onClick.AddListener(delegate { audioController.PlayButtonAudio(); ChangeAutoView(50); OnATClick(); });
          break;
        case 4:
          if (AutoCount_Buttons[i]) AutoCount_Buttons[i].onClick.RemoveAllListeners();
          if (AutoCount_Buttons[i]) AutoCount_Buttons[i].onClick.AddListener(delegate { audioController.PlayButtonAudio(); ChangeAutoView(100); OnATClick(); });
          break;
      }
    }

    if (BetMain_Text) BetMain_Text.text = "10.00";
    turboOriginalSprite = Turbo_Button.GetComponent<Image>().sprite;
  }

  internal void PopulateSymbolsPayout(Paylines paylines)
  {
    for (int i = 0; i < paylines.symbols.Count; i++)
    {
      if (paylines.symbols[i].name.ToUpper() == "REDRESPIN")
      {
        if (RedSpin_Text) RedSpin_Text.text = paylines.symbols[i].description.ToString();
      }
      if (paylines.symbols[i].name.ToUpper() == "ZERORESPIN")
      {
        if (ZeroSpin_Text) ZeroSpin_Text.text = paylines.symbols[i].description.ToString();
      }
      if (paylines.symbols[i].name.ToUpper() == "REELACTIVATION")
      {
        if (ReelActivation_Text) ReelActivation_Text.text = paylines.symbols[i].description.ToString();
      }
    }
  }
  #endregion

  private void StartSpinning()
  {
    if (isAtOpen)
    {
      OnATClick();
    }
    if (isBetOpen)
    {
      OnBetClick();
    }
    if (isMenuOpen)
    {
      OnMenuClick();
    }
    ToggleButtonGrp(false);
    if (AutoSpinPanel_Button) AutoSpinPanel_Button.interactable = false;
    if (slotManager) slotManager.StartSpin();
  }

  private void OnRayCastClick()
  {
    if (isAtOpen)
    {
      OnATClick();
    }
    if (isBetOpen)
    {
      OnBetClick();
    }
    if (isMenuOpen)
    {
      OnMenuClick();
    }
  }

  void TurboToggle()
  {
    if (audioController) audioController.PlayButtonAudio();
    if (slotManager.IsTurboOn)
    {
      slotManager.IsTurboOn = false;
      Turbo_Button.GetComponent<ImageAnimation>().StopAnimation();
      Turbo_Button.image.sprite = turboOriginalSprite;
    }
    else
    {
      slotManager.IsTurboOn = true;
      Turbo_Button.GetComponent<ImageAnimation>().StartAnimation();
    }
  }
  #region AutoSpin
  private void OnATClick()
  {
    if (AutoSpinPanel_Button) AutoSpinPanel_Button.interactable = false;
    if (!isAtOpen)
    {
      if (AT_ImageTransform) AT_ImageTransform.DORotate(new Vector3(0, 180, 0), 0.5f);
      if (AT_GameObject) AT_GameObject.SetActive(true);
      if (isBetOpen)
      {
        OnBetClick();
      }
      if (isMenuOpen)
      {
        OnMenuClick();
      }
      if (RayCast_Object) RayCast_Object.SetActive(true);
      if (AT_Transform) AT_Transform.DOLocalMoveX(0, 0.5f).OnComplete(delegate
      {
        if (!slotManager.IsSpinning)
        {
          if (AutoSpinPanel_Button) AutoSpinPanel_Button.interactable = true;
        }

      });
      isAtOpen = true;
    }
    else
    {
      if (RayCast_Object) RayCast_Object.SetActive(false);
      if (AT_ImageTransform) AT_ImageTransform.DORotate(new Vector3(0, 0, 0), 0.5f);
      if (AT_Transform) AT_Transform.DOLocalMoveX(940, 0.5f).OnComplete(delegate
      {
        if (AT_GameObject) AT_GameObject.SetActive(false);
        if (!slotManager.IsSpinning)
        {
          if (AutoSpinPanel_Button) AutoSpinPanel_Button.interactable = true;
        }
      });
      isAtOpen = false;
    }
  }

  private void ChangeAutoView(int count)
  {
    SpinCount = count;
    if (StopAutoSpin_Button) StopAutoSpin_Button.interactable = true;
    if (AutoSpinImage_Object) AutoSpinImage_Object.SetActive(true);
    if (SpinImage_Object) SpinImage_Object.SetActive(false);
    updateAutoCount(SpinCount);
  }

  private void StartAutoSpin()
  {
    ToggleButtonGrp(false);
    if (slotManager) slotManager.AutoSpin(SpinCount);
  }
  #endregion

  #region BetSettings
  private void OnBetClick()
  {
    if (!isBetOpen)
    {
      if (BetPanel_Object) BetPanel_Object.SetActive(true);
      if (isAtOpen)
      {
        OnATClick();
      }
      if (isMenuOpen)
      {
        OnMenuClick();
      }
      isBetOpen = true;
      if (RayCast_Object) RayCast_Object.SetActive(true);
    }
    else
    {
      if (RayCast_Object) RayCast_Object.SetActive(false);
      if (BetPanel_Object) BetPanel_Object.SetActive(false);
      isBetOpen = false;
    }
  }

  private void OnBetChange(float value)
  {
    int index = Mathf.RoundToInt(value);
    if (index < 0 || index >= availableBets.Count) return;

    // Reset: enable all
    foreach (Transform t in slotManager.Slot_Transform) t.gameObject.SetActive(true);
    foreach (Transform t in slotManager.RedSlot_Transform) t.gameObject.SetActive(true);

    // Apply your old disable/color logic
    if (index == 0)
    {
      slotManager.Slot_Transform[2].gameObject.SetActive(false);
      slotManager.Slot_Transform[1].gameObject.SetActive(false);
      slotManager.RedSlot_Transform[2].gameObject.SetActive(false);
      slotManager.RedSlot_Transform[1].gameObject.SetActive(false);

      if (Slots_image[1]) Slots_image[1].color = Disabled_Color;
      if (Slots_image[2]) Slots_image[2].color = Disabled_Color;
    }
    else if (index == 1)
    {
      slotManager.Slot_Transform[2].gameObject.SetActive(false);
      slotManager.RedSlot_Transform[2].gameObject.SetActive(false);

      if (Slots_image[1]) Slots_image[1].color = Color.white;
      if (Slots_image[2]) Slots_image[2].color = Disabled_Color;
    }
    else
    {
      if (Slots_image[1]) Slots_image[1].color = Color.white;
      if (Slots_image[2]) Slots_image[2].color = Color.white;
    }

    slotManager.SlotNumber = index;
    currentBet = availableBets[index];
    int TotalBet = availableDenoms[slotManager.DenomCounter] * currentBet;
    slotManager.CurrentBet = TotalBet;

    if (Bet_Text) Bet_Text.text = currentBet.ToString();
    if (BetMain_Text) BetMain_Text.text = TotalBet.ToString();
  }

  internal void SetupBetWindow(List<int> denoms, List<int> betLevel)
  {
    availableBets = betLevel;
    if (Bet_Slider)
    {
      Bet_Slider.minValue = 0;
      Bet_Slider.maxValue = betLevel.Count - 1;
      Bet_Slider.wholeNumbers = true;
      Bet_Slider.value = betLevel.Count - 1; // default first bet level
    }
    availableDenoms = denoms;
    if (Denom_Slider)
    {
      Denom_Slider.minValue = 0;
      Denom_Slider.maxValue = denoms.Count - 1;
      Denom_Slider.wholeNumbers = true;
      Denom_Slider.value = 0; // default first denom
    }

    OnBetChange(betLevel.Count - 1);
    OnDenomChange(0);
  }

  private void OnDenomChange(float value)
  {
    int index = Mathf.RoundToInt(value);
    if (index < 0 || index >= availableDenoms.Count) return;

    int DenomVal = availableDenoms[index];
    if (Denom_Text) Denom_Text.text = DenomVal.ToString();

    slotManager.DenomCounter = index; // keep track for server call
    slotManager.CurrentBet = DenomVal * availableBets[slotManager.SlotNumber];
    BetMain_Text.text = slotManager.CurrentBet.ToString();
    // Debug.Log(myvalue);
  }

  private void OnBetButton(bool isIncrement)
  {
    if (isIncrement)
    {
      if (Bet_Slider.value < Bet_Slider.maxValue)
        Bet_Slider.value++;
    }
    else
    {
      if (Bet_Slider.value > Bet_Slider.minValue)
        Bet_Slider.value--;
    }
  }

  private void OnDenomButton(bool isIncrement)
  {
    if (isIncrement)
    {
      if (Denom_Slider.value < Denom_Slider.maxValue)
        Denom_Slider.value++;
    }
    else
    {
      if (Denom_Slider.value > Denom_Slider.minValue)
        Denom_Slider.value--;
    }
  }
  #endregion

  #region MenuSettup
  private void OnMenuClick()
  {
    if (!isMenuOpen)
    {
      if (MenuButton_Object) MenuButton_Object.SetActive(true);
      if (isBetOpen)
      {
        OnBetClick();
      }
      if (isAtOpen)
      {
        OnATClick();
      }
      if (RayCast_Object) RayCast_Object.SetActive(true);
      if (MenuButtons_Rect) MenuButtons_Rect.DOLocalMoveY(-36, 0.5f);
      isMenuOpen = true;
    }
    else
    {
      if (RayCast_Object) RayCast_Object.SetActive(false);
      if (MenuButtons_Rect) MenuButtons_Rect.DOLocalMoveY(-445, 0.5f).OnComplete(delegate
      {
        if (MenuButton_Object) MenuButton_Object.SetActive(false);
      });
      isMenuOpen = false;
    }
  }

  private void ToggleSound(bool isActive)
  {
    if (isActive)
    {
      if (Mute_Object) Mute_Object.SetActive(true);
      if (Sound_Object) Sound_Object.SetActive(false);
    }
    else
    {
      if (Mute_Object) Mute_Object.SetActive(false);
      if (Sound_Object) Sound_Object.SetActive(true);
    }
    if (audioController) audioController.ToggleMute(isActive);
  }

  private void OpenRulesPopup()
  {
    OnMenuClick();
    if (PopupMain_Object) PopupMain_Object.SetActive(true);
    if (RulesPopup) RulesPopup.SetActive(true);
  }

  private void CloseRulesPopup()
  {
    if (PopupMain_Object) PopupMain_Object.SetActive(false);
    if (RulesPopup) RulesPopup.SetActive(false);
  }

  private void OpenSettingsPopup()
  {
    OnMenuClick();
    if (PopupMain_Object) PopupMain_Object.SetActive(true);
    if (AutoSpinPopup) AutoSpinPopup.SetActive(true);
  }

  private void CloseSettingsPopup()
  {
    if (PopupMain_Object) PopupMain_Object.SetActive(false);
    if (AutoSpinPopup) AutoSpinPopup.SetActive(false);
  }

  private void OnATSlide(float value)
  {
    if (AutoSpinSetup_Text) AutoSpinSetup_Text.text = value.ToString();
    if (value > 0)
    {
      SpinCount = (int)value;
      if (StopAutoSpin_Button) StopAutoSpin_Button.interactable = true;
      if (AutoSpinImage_Object) AutoSpinImage_Object.SetActive(true);
      if (SpinImage_Object) SpinImage_Object.SetActive(false);
      updateAutoCount(SpinCount);
    }
    else
    {
      SpinCount = (int)value;
      if (StopAutoSpin_Button) StopAutoSpin_Button.interactable = false;
      if (AutoSpinImage_Object) AutoSpinImage_Object.SetActive(false);
      if (SpinImage_Object) SpinImage_Object.SetActive(true);
      updateAutoCount(SpinCount);
    }
  }

  private void ToggleAutoSpin(bool isIncrement)
  {
    if (isIncrement)
    {
      if (AutoSpin_Slider) AutoSpin_Slider.value++;
    }
    else
    {
      if (AutoSpin_Slider) AutoSpin_Slider.value--;
    }
  }
  #endregion

  #region WinPopup
  private void ToggleWinPopup(bool isActive)
  {
    // CloseAllPopups();
    // if (PopupMain_Object) PopupMain_Object.SetActive(isActive);
    if (NiceWinPopup) NiceWinPopup.SetActive(isActive);
  }

  private void CloseAllPopups()
  {
    if (RulesPopup.activeSelf) RulesPopup.SetActive(false);
    if (isAtOpen)
    {
      OnATClick();
    }
    if (isMenuOpen)
    {
      OnMenuClick();
    }
  }
  #endregion

  #region Miscellanious Popups
  private void TogglePopup(GameObject popup, bool isActive = false)
  {
    if (popup == LBPopup_Object)
    {
      if (RulesPopup.activeSelf) RulesPopup.SetActive(false);
      if (AutoSpinPopup.activeSelf) AutoSpinPopup.SetActive(false);
    }
    if (popup == DisconnectionPopup_Object)
    {
      if (RulesPopup.activeSelf) RulesPopup.SetActive(false);
      if (AutoSpinPopup.activeSelf) AutoSpinPopup.SetActive(false);
      if (LBPopup_Object.activeSelf) LBPopup_Object.SetActive(false);
    }
    if (popup == ReconnectPopup_Object)
    {
      if (RulesPopup.activeSelf) RulesPopup.SetActive(false);
      if (AutoSpinPopup.activeSelf) AutoSpinPopup.SetActive(false);
      if (LBPopup_Object.activeSelf) LBPopup_Object.SetActive(false);
    }
    if (PopupMain_Object) PopupMain_Object.SetActive(isActive);
    if (popup) popup.SetActive(isActive);
  }

  private void CallOnExitFunction()
  {
    isExit = true;
    //audioController.PlayButtonAudio();
    slotManager.CallCloseSocket();
  }

  #endregion

  #region InternalMethods
  internal void StoppingAutoSpin()
  {
    if (isBetOpen)
    {
      OnBetClick();
    }
    if (slotManager.IsAutoSpin)
    {
      if (StopAutoSpin_Button) StopAutoSpin_Button.interactable = false;
      if (slotManager) slotManager.StopAutoSpin();
    }
    else
    {
      StartAutoSpin();
    }
  }

  internal void updateAutoCount(int count)
  {
    if (AutoCounter_Text) AutoCounter_Text.text = count.ToString();
  }

  internal void AddWinColor(int value)
  {
    Slots_image[value].color = Win_Color;
  }

  internal void resetWinColor()
  {
    for (int i = 0; i < slotManager.SlotNumber + 1; i++)
    {
      Slots_image[i].color = Color.white;
    }
  }

  internal void StopAutoSpin()
  {
    if (AutoSpinImage_Object) AutoSpinImage_Object.SetActive(false);
    if (SpinImage_Object) SpinImage_Object.SetActive(true);
  }

  internal void GreenRespin(bool isActice)
  {
    if (isActice)
    {
      UpdateMessageText("Respin");
      if (CMHeading_object) CMHeading_object.SetActive(false);
      if (RespinHeading_object) RespinHeading_object.SetActive(true);
    }
    else
    {
      if (CMHeading_object) CMHeading_object.SetActive(true);
      if (RespinHeading_object) RespinHeading_object.SetActive(false);
    }
  }

  internal void RedRespin(bool isActive)
  {
    if (isActive)
    {
      StartCoroutine(RedRespinRoutine());
    }
    else
    {
      if (RedSpinSetup) RedSpinSetup.SetActive(false);
    }
  }

  private IEnumerator RedRespinRoutine()
  {
    UpdateMessageText("Respin");
    if (RedSpinSetup) RedSpinSetup.SetActive(true);
    yield return new WaitForSeconds(0.5f);
    if (RedSpinSetup) RedSpinSetup.SetActive(false);
    yield return new WaitForSeconds(0.5f);
    if (RedSpinSetup) RedSpinSetup.SetActive(true);
    yield return new WaitForSeconds(0.5f);
    if (RedSpinSetup) RedSpinSetup.SetActive(false);
    yield return new WaitForSeconds(0.5f);
    if (RedSpinSetup) RedSpinSetup.SetActive(true);
  }

  internal void ToggleButtonGrp(bool isActive)
  {
    if (Spin_Button) Spin_Button.interactable = isActive;
    if (slotManager.IsAutoSpin && isActive)
    {
      if (AutoSpinPanel_Button) AutoSpinPanel_Button.interactable = false;
    }
    else
    {
      if (AutoSpinPanel_Button) AutoSpinPanel_Button.interactable = isActive;
    }
    if (Spin_Button) Spin_Button.interactable = isActive;
    if (Bet_Button) Bet_Button.interactable = isActive;
    if (Settings_Button) Settings_Button.interactable = isActive;
    if (BetSettings_Button) BetSettings_Button.interactable = isActive;
  }

  internal void UpdateBalance(double balance)
  {
    if (BalanceMain_Text) BalanceMain_Text.text = balance.ToString("f2");
  }

  internal IEnumerator UpdateWinnings(double balance, double winning)
  {
    bool isComplete = false;
    double prevBalance = double.Parse(BalanceMain_Text.text);
    double prevWinning = 0f;
    UpdateMessageText("Pays " + winning);
    DOTween.To(() => prevBalance, (val) => prevBalance = val, balance, 0.2f).OnUpdate(() =>
    {
      if (BalanceMain_Text) BalanceMain_Text.text = prevBalance.ToString("f2");
    });

    DOTween.To(() => prevWinning, (val) => prevWinning = val, winning, 0.2f).OnUpdate(() =>
    {
      if (WinMain_Text) WinMain_Text.text = prevWinning.ToString("f2");
    }).OnComplete(delegate { isComplete = true; });

    if (winning >= (slotManager.CurrentBet * 5))
    {
      ToggleWinPopup(true);
      if (audioController) audioController.PlayWLAudio("bigwin");
      SkipWin = false;
      for (int i = 0; i < 20; i++)
      {
        if (SkipWin)
        {
          break;
        }
        yield return new WaitForSecondsRealtime(0.1f);
      }
      ToggleWinPopup(false);
    }

    yield return new WaitUntil(() => isComplete);
    yield return new WaitForSeconds(0.5f);
  }

  internal void UpdateTweenBalance(double bet)
  {
    double prevBalance = double.Parse(BalanceMain_Text.text);
    double currentBalance = prevBalance - bet;
    DOTween.To(() => prevBalance, (val) => prevBalance = val, currentBalance, 1f).OnUpdate(() =>
    {
      if (BalanceMain_Text) BalanceMain_Text.text = prevBalance.ToString("f2");
    });
  }

  internal void ResetWinText()
  {
    if (WinMain_Text) WinMain_Text.text = "0.00";
    UpdateMessageText("");
  }

  internal void EnableLowBalance()
  {
    TogglePopup(LBPopup_Object, true);
  }

  internal void CheckAndClosePopups()
  {
    if (ReconnectPopup_Object.activeInHierarchy)
    {
      TogglePopup(ReconnectPopup_Object);
    }
    if (DisconnectionPopup_Object.activeInHierarchy)
    {
      TogglePopup(DisconnectionPopup_Object);
    }
  }

  internal void ReconnectionPopup()
  {
    TogglePopup(ReconnectPopup_Object, true);
  }
  internal void EnableDisconect()
  {
    if (!isExit)
    {
      TogglePopup(DisconnectionPopup_Object, true);
    }
  }

  internal bool CheckBalance(double bet)
  {
    double prevBalance = double.Parse(BalanceMain_Text.text);
    double currentBalance = prevBalance - bet;
    if (currentBalance < 0)
    {
      return false;
    }
    else
    {
      return true;
    }
  }

  internal void UpdateMessageText(string messsage)
  {
    if (Message_Text) Message_Text.text = messsage;
  }
  #endregion
}
