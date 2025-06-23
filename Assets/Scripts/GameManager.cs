using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
public class GameManager : MonoBehaviour
{
  [Header("scripts")]
  [SerializeField] internal JSFunctCalls JSManager;
  [SerializeField] private SlotController slotManager;
  [SerializeField] private UIManager uIManager;
  [SerializeField] private SocketController socketController;
  [SerializeField] private AudioController audioController;

  [Header("For spins")]
  [SerializeField] private Button SlotStart_Button;
  [SerializeField] private Button StopSpin_Button;
  [SerializeField] private Button ToatlBetMinus_Button;
  [SerializeField] private Button TotalBetPlus_Button;
  [SerializeField] private TMP_Text totalBet_text;
  [SerializeField] private bool isSpinning;
  [SerializeField] private Button Turbo_Button;
  [SerializeField] private GameObject turboAnim;

  [Header("For auto spins")]
  [SerializeField] private Button AutoSpin_Button;

  [SerializeField] private Button[] AutoSpinOptions_Button;
  [SerializeField] private TMP_Text[] AutoSpinOptions_Text;
  [SerializeField] private Button AutoSpinStop_Button;
  [SerializeField] private Button AutoSpinPopup_Button;
  [SerializeField] private Button autoSpinUp;
  [SerializeField] private Button autoSpinDown;
  [SerializeField] private bool isAutoSpin;
  [SerializeField] private int autoSpinCounter;
  [SerializeField] private TMP_Text autoSpinText;
  [SerializeField] private TMP_Text autoSpinShowText;
  List<int> autoOptions = new List<int>() { 15, 20, 25, 30, 40, 100 };
  [SerializeField] private int maxAutoSpinValue = 1000;


  [Header("For FreeSpins")]
  [SerializeField] private bool specialSpin;

  [SerializeField] private double currentBalance;
  [SerializeField] private double currentTotalBet;
  [SerializeField] private int betCounter = 0;


  private Coroutine autoSpinRoutine;
  private Coroutine freeSpinRoutine;
  [SerializeField] private int freeSpinCount;
  [SerializeField] private bool isFreeSpin;


  private bool initiated;
  [SerializeField] private bool turboMode;
  static internal bool immediateStop;
  static internal bool winAnimComplete = false;
  private Coroutine winPopUpRoutine;

  private int autoSpinLeft;
  void Start()
  {
    SetButton(SlotStart_Button, ExecuteSpin, true);
    SetButton(AutoSpin_Button, () =>
    {
      ExecuteAutoSpin();
      // uIManager.ClosePopup();
    }, true);
    InitiateAutoSpin();
    SetButton(AutoSpinStop_Button, () => StartCoroutine(StopAutoSpinCoroutine()));
    SetButton(ToatlBetMinus_Button, () => OnBetChange(false));
    SetButton(TotalBetPlus_Button, () => OnBetChange(true));
    SetButton(autoSpinUp, () => OnAutoSpinChange(true));
    SetButton(autoSpinDown, () => OnAutoSpinChange(false));
    SetButton(Turbo_Button, () => ToggleTurboMode());
    SetButton(StopSpin_Button, () => StartCoroutine(StopSpin()));
    autoSpinCounter = 0;
    // SetButton(freeSpinStartButton, () => );

    // autoSpinShowText.text = autoOptions[autoSpinCounter].ToString();


    slotManager.shuffleInitialMatrix();
    socketController.OnInit = InitGame;
    uIManager.ToggleAudio = audioController.ToggleMute;
    uIManager.playButtonAudio = audioController.PlayButtonAudio;
    uIManager.OnExit = () => socketController.CloseSocket();
    socketController.ShowDisconnectionPopup = uIManager.DisconnectionPopup;

    socketController.OpenSocket();

    // StopSpin_Button.onClick.AddListener(() => StartCoroutine(StopSpin()));
    // Turbo_Button.onClick.AddListener(ToggleTurboMode);
  }


  private void SetButton(Button button, Action action, bool slotButton = false)
  {
    if (button == null) return;

    button.onClick.RemoveAllListeners();
    button.onClick.AddListener(() =>
    {
      audioController.PlayButtonAudio();
      action?.Invoke();

    });
  }
  void InitGame()
  {
    if (!initiated)
    {
      initiated = true;
      betCounter = 0;
      // TODO: change total bet
      currentTotalBet = socketController.socketModel.initGameData.Bets[betCounter];
      currentBalance = socketController.socketModel.playerData.balance;
      if (totalBet_text) totalBet_text.text = currentTotalBet.ToString();
      if (currentBalance < currentTotalBet)
        uIManager.LowBalPopup();
      // if (betPerLine_text) betPerLine_text.text = socketController.socketModel.initGameData.Bets[betCounter].ToString();
      // PayLineCOntroller.paylines = socketController.socketModel.initGameData.lineData;
      uIManager.UpdatePlayerInfo(socketController.socketModel.playerData);
      uIManager.PopulateSymbolsPayout(socketController.socketModel.uIData);
#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager.SendCustomMessage("OnEnter");
#endif
    }
    else
    {
      uIManager.PopulateSymbolsPayout(socketController.socketModel.uIData);
    }


  }

  void InitiateAutoSpin()
  {


    for (int i = 0; i < autoOptions.Count; i++)
    {
      int capturedIndex = i;
      SetButton(AutoSpinOptions_Button[capturedIndex], () => ExecuteAutoSpin(autoOptions[capturedIndex]));
      AutoSpinOptions_Text[capturedIndex].text = autoOptions[capturedIndex].ToString();
      autoSpinCounter = autoOptions[capturedIndex];
      // uIManager.ClosePopup();

    }


  }

  void ExecuteSpin() => StartCoroutine(SpinRoutine());


  void ExecuteAutoSpin(int nofAutoSpin = 0)
  {
    if (nofAutoSpin <= 0)
      nofAutoSpin = autoSpinCounter;

    if (!isSpinning && nofAutoSpin > 0)
    {

      isAutoSpin = true;
      autoSpinText.text = nofAutoSpin.ToString();
      autoSpinText.transform.gameObject.SetActive(true);
      // AutoSpin_Button.gameObject.SetActive(false);

      AutoSpinStop_Button.gameObject.SetActive(true);
      autoSpinRoutine = StartCoroutine(AutoSpinRoutine(nofAutoSpin));
    }

  }

  void ToggleTurboMode()
  {
    turboMode = !turboMode;
    if (turboMode)
      turboAnim.SetActive(true);
    else
      turboAnim.SetActive(false);

  }

  IEnumerator FreeSpinRoutine()
  {
    uIManager.ToggleFreeSpinPanel(true);
    uIManager.EnablePurplebar(true);
    uIManager.CloseFreeSpinPopup();

    if (StopSpin_Button.gameObject.activeSelf)
    {
      StopSpin_Button.gameObject.SetActive(false);
      immediateStop = false;
    }
    while (freeSpinCount > 0)
    {
      freeSpinCount--;
      uIManager.UpdateFreeSpinInfo(freeSpinCount);

      yield return SpinRoutine();
      yield return new WaitForSeconds(1);
    }
    slotManager.ResizeSlotMatrix(0);
    audioController.PlaySizeUpSound(true);
    uIManager.EnablePurplebar(false);
    yield return new WaitForSeconds(1f);
    audioController.playBgAudio("FP");
    audioController.PlaySizeUpSound(false);

    uIManager.ToggleFreeSpinPanel(false);

    isAutoSpin = false;
    isSpinning = false;
    isFreeSpin = false;

    if (autoSpinLeft > 0)
    {
      ExecuteAutoSpin(autoSpinLeft);
      uIManager.ClosePopup();

    }
    else
    {
      ToggleButtonGrp(true);

    }

    yield return null;
  }
  IEnumerator AutoSpinRoutine(int noOfSPin)
  {
    autoSpinLeft = noOfSPin;
    while (autoSpinLeft > 0 && isAutoSpin)
    {
      autoSpinLeft--;
      autoSpinText.text = autoSpinLeft.ToString();

      yield return SpinRoutine();
      yield return new WaitForSeconds(0.5f);

    }
    autoSpinText.transform.gameObject.SetActive(false);
    autoSpinText.text = "0";
    isSpinning = false;
    autoSpinLeft = 0;
    StartCoroutine(StopAutoSpinCoroutine());
    yield return null;
  }

  private IEnumerator StopAutoSpinCoroutine(bool hard = false)
  {
    isAutoSpin = false;


    AutoSpin_Button.gameObject.SetActive(true);
    AutoSpinStop_Button.gameObject.SetActive(false);
    autoSpinText.transform.gameObject.SetActive(false);
    yield return new WaitUntil(() => !isSpinning);


    if (autoSpinRoutine != null)
    {
      StopCoroutine(autoSpinRoutine);
      autoSpinRoutine = null;
      autoSpinText.text = "0";
    }

    AutoSpinPopup_Button.gameObject.SetActive(true);
    ToggleButtonGrp(true);
    autoSpinLeft = 0;

    autoSpinText.text = "0";
    yield return null;

  }
  IEnumerator SpinRoutine()
  {
    bool start = OnSpinStart();
    if (!start)
    {

      isSpinning = false;
      if (isAutoSpin)
      {
        StartCoroutine(StopAutoSpinCoroutine());
      }

      ToggleButtonGrp(true);
      yield break;
    }

    yield return OnSpin();
    yield return OnSpinEnd();
    if (socketController.socketModel.resultGameData.features.freeSpin.isFreeSpin)
    {
      int prevFreeSpin = freeSpinCount;
      freeSpinCount = socketController.socketModel.resultGameData.features.freeSpin.count;
      uIManager.UpdateFreeSpinInfo(freeSpinCount);
      isFreeSpin = true;
      specialSpin = false;
      if (autoSpinRoutine != null)
      {
        isAutoSpin = false;
        if (autoSpinRoutine != null)
        {
          StopCoroutine(autoSpinRoutine);
          autoSpinRoutine = null;
          autoSpinText.text = "0";
        }
        // yield return StopAutoSpinCoroutine(true);
      }

      if (freeSpinRoutine != null)
      {
        StopCoroutine(freeSpinRoutine);
        uIManager.FreeSpinPopup(freeSpinCount - prevFreeSpin, false);
        yield return new WaitForSeconds(2f);
        uIManager.CloseFreeSpinPopup();
        freeSpinRoutine = StartCoroutine(FreeSpinRoutine());
      }
      else
      {
        uIManager.FreeSpinPopup(freeSpinCount, true);
        audioController.playBgAudio("FP");
        yield return new WaitForSeconds(2f);
        uIManager.CloseFreeSpinPopup();
        freeSpinRoutine = StartCoroutine(FreeSpinRoutine());
      }

      yield break;
    }
    if (specialSpin)
    {
      uIManager.FreeSpinTextAnim();
      yield return SpinRoutine();
    }

    if (!isAutoSpin && !isFreeSpin)
    {
      isSpinning = false;
      ToggleButtonGrp(true);
    }
  }

  IEnumerator StopSpin()
  {
    if (isAutoSpin || isFreeSpin || immediateStop || specialSpin)
      yield break;
    immediateStop = true;
    StopSpin_Button.interactable = false;
    yield return new WaitUntil(() => !isSpinning);
    immediateStop = false;
    StopSpin_Button.interactable = true;


  }
  bool OnSpinStart()
  {
    slotManager.watchAnimation.StopAnimation();
    isSpinning = true;
    slotManager.disableIconsPanel.SetActive(false);
    if (currentBalance < currentTotalBet && !isFreeSpin)
    {
      uIManager.LowBalPopup();
      return false;
    }
    ToggleButtonGrp(false);
    uIManager.ClosePopup();
    return true;


  }

  IEnumerator OnSpin()
  {
    if (!isFreeSpin && !specialSpin)
      uIManager.DeductBalanceAnim(socketController.socketModel.playerData.balance - currentTotalBet, socketController.socketModel.playerData.balance);

    slotManager.watchAnimation.StartAnimation();

    if (!isFreeSpin && !isAutoSpin && !specialSpin)
      StopSpin_Button.gameObject.SetActive(true);

    if (specialSpin)
      immediateStop = false;

    // Debug.Log("immediate stop" + immediateStop);

    var spinData = new { currentBet = betCounter };
    socketController.SendData("spin:request", spinData);
    yield return slotManager.StartSpin(turboMode: turboMode);
    yield return new WaitUntil(() => socketController.isResultdone);

    if (!immediateStop && !turboMode)
      yield return new WaitForSeconds(0.45f);
    else
    {
      yield return new WaitForSeconds(0.15f);
    }
    // slotManager.StopIconAnimation();
    slotManager.PopulateSLotMatrix(socketController.socketModel.resultGameData.matrix);
    currentBalance = socketController.socketModel.playerData.balance;
    yield return slotManager.StopSpin(turboMode: turboMode, audioController.PlaySpinStopAudio);

    if (StopSpin_Button.gameObject.activeSelf)
    {
      StopSpin_Button.gameObject.SetActive(false);
    }

  }
  IEnumerator OnSpinEnd()
  {
    audioController.StopSpinAudio();
    if (socketController.socketModel.resultGameData.symbolsToEmit.Count > 0)
    {
      audioController.PlayWLAudio("electric");
      slotManager.StartIconAnimation(socketController.socketModel.resultGameData.symbolsToEmit, socketController.socketModel.resultGameData.matrix.Count);
      yield return new WaitForSeconds(1.5f);
      audioController.StopWLAaudio();
    }

    uIManager.UpdatePlayerInfo(socketController.socketModel.playerData, socketController.socketModel.resultGameData.payload);

    if (!isFreeSpin)
    {
      specialSpin = socketController.socketModel.resultGameData.features.freeSpin.isLevelUp;

      if (specialSpin)
      {
        audioController.PlaySizeUpSound(true);
        slotManager.ResizeSlotMatrix(socketController.socketModel.resultGameData.features.freeSpin.level);
        yield return new WaitForSeconds(1.5f);
        audioController.PlaySizeUpSound(false);
      }
      else
      {
        if (socketController.socketModel.resultGameData.features.freeSpin.level == 0 && slotManager.level > 0)
        {
          audioController.PlaySizeUpSound(true);
          slotManager.ResizeSlotMatrix(0);
          yield return new WaitForSeconds(1.5f);
          audioController.PlaySizeUpSound(false);
        }
      }
    }
    if (socketController.socketModel.resultGameData.payload.winAmount > 0)
    {
      winAnimComplete = false;
      CheckWinPopups(socketController.socketModel.resultGameData.payload.winAmount);
      yield return new WaitWhile(() => !winAnimComplete);
      winAnimComplete = false;
      if (winPopUpRoutine != null)
      {
        StopCoroutine(winPopUpRoutine);
        winPopUpRoutine = null;
      }
      audioController.StopWLAaudio();
    }
    if (isFreeSpin)
      uIManager.UpdateFreeSpinInfo(winnings: socketController.socketModel.resultGameData.payload.winAmount);

    slotManager.StopIconAnimation();
    slotManager.watchAnimation.StopAnimation();

    yield return null;
  }

  void ToggleButtonGrp(bool toggle)
  {
    if (SlotStart_Button) SlotStart_Button.interactable = toggle;
    if (AutoSpin_Button) AutoSpin_Button.interactable = toggle;
    if (AutoSpinPopup_Button) AutoSpinPopup_Button.interactable = toggle;
    if (ToatlBetMinus_Button) ToatlBetMinus_Button.interactable = toggle;
    if (TotalBetPlus_Button) TotalBetPlus_Button.interactable = toggle;
    uIManager.Settings_Button.interactable = toggle;
  }

  private void OnBetChange(bool inc)
  {
    if (audioController) audioController.PlayButtonAudio();

    if (inc)
    {
      betCounter++;
    }
    else
    {
      betCounter--;

    }
    if (betCounter > socketController.socketModel.initGameData.Bets.Count - 1)
    {
      betCounter = 0;
    }
    if (betCounter < 0)
    {
      betCounter = socketController.socketModel.initGameData.Bets.Count - 1;
    }

    currentTotalBet = socketController.socketModel.initGameData.Bets[betCounter];
    if (totalBet_text) totalBet_text.text = currentTotalBet.ToString();
    // if (currentBalance < currentTotalBet)
    //     uIManager.LowBalPopup();
  }

  private void OnAutoSpinChange(bool inc)
  {

    if (audioController) audioController.PlayButtonAudio();

    if (inc)
    {
      autoSpinCounter++;
      if (autoSpinCounter > maxAutoSpinValue)
      {
        autoSpinCounter = 1;
      }
    }
    else
    {
      autoSpinCounter--;
      if (autoSpinCounter < 1)
      {
        autoSpinCounter = maxAutoSpinValue;

      }
    }

    autoSpinShowText.text = autoSpinCounter.ToString();
  }


  void CheckWinPopups(double amount)
  {
    if (amount > 0 && amount < currentTotalBet * 5)
    {
      uIManager.EnableWinPopUp(0, amount);
    }
    else if (amount >= currentTotalBet * 5 && amount <= currentTotalBet * 7.5)
    {
      uIManager.EnableWinPopUp(1, amount);
      audioController.PlayWLAudio("big");

    }
    else if (amount >= currentTotalBet * 7.5 && amount < currentTotalBet * 10)
    {
      uIManager.EnableWinPopUp(2, amount);
      audioController.PlayWLAudio("big");

    }
    else if (amount >= currentTotalBet * 10)
    {
      uIManager.EnableWinPopUp(3, amount);
      audioController.PlayWLAudio("big");
    }
  }
}
