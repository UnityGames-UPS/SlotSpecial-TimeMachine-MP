using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
public class GameManager : MonoBehaviour
{
    [Header("scripts")]
    [SerializeField] private SlotController slotManager;
    [SerializeField] private UIManager uIManager;
    [SerializeField] private SocketController socketController;
    [SerializeField] private AudioController audioController;

    [Header("For spins")]
    [SerializeField] private Button SlotStart_Button;
    [SerializeField] private Button ToatlBetMinus_Button;
    [SerializeField] private Button TotalBetPlus_Button;
    [SerializeField] private TMP_Text totalBet_text;
    [SerializeField] private bool isSpinning;
    [SerializeField] private TMP_Text gameStateText;

    [Header("For auto spins")]
    [SerializeField] private Button AutoSpin_Button;
    [SerializeField] private Button AutoSpinStop_Button;
    [SerializeField] private Button AutoSpinPopup_Button;
    [SerializeField] private Button autoSpinUp;
    [SerializeField] private Button autoSpinDown;
    [SerializeField] private bool isAutoSpin;
    [SerializeField] private int autoSpinCounter;
    [SerializeField] private TMP_Text autoSpinText;
    [SerializeField] private TMP_Text autoSpinShowText;
    List<int> autoOptions = new List<int>() { 15, 20, 25, 30, 40, 100 };



    [Header("For FreeSpins")]
    [SerializeField] private bool specialSpin;

    [SerializeField] private double currentBalance;
    [SerializeField] private double currentTotalBet;
    [SerializeField] private int betCounter = 0;

    [SerializeField] private Button freeSpinStartButton;

    private Coroutine autoSpinRoutine;
    private Coroutine freeSpinRoutine;
    [SerializeField] private int winIterationCount;

    [SerializeField] private int freeSpinCount;

    [SerializeField] private bool isFreeSpin;


    private bool initiated;

    void Start()
    {
        SetButton(SlotStart_Button, ExecuteSpin, true);
        SetButton(AutoSpin_Button, () =>
        {
            ExecuteAutoSpin();
            uIManager.ClosePopup();
        }, true);
        SetButton(AutoSpinStop_Button, () => StartCoroutine(StopAutoSpinCoroutine()));
        SetButton(ToatlBetMinus_Button, () => OnBetChange(false));
        SetButton(TotalBetPlus_Button, () => OnBetChange(true));
        SetButton(autoSpinUp, () => OnAutoSpinChange(true));
        SetButton(autoSpinDown, () => OnAutoSpinChange(false));
        SetButton(freeSpinStartButton, () => freeSpinRoutine = StartCoroutine(FreeSpinRoutine()));

        autoSpinShowText.text = autoOptions[autoSpinCounter].ToString();


        slotManager.shuffleInitialMatrix();
        socketController.OnInit = InitGame;
        uIManager.ToggleAudio = audioController.ToggleMute;
        uIManager.playButtonAudio = audioController.PlayButtonAudio;
        uIManager.OnExit = () => socketController.CloseSocket();
        socketController.ShowDisconnectionPopup = uIManager.DisconnectionPopup;

        socketController.OpenSocket();
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
            currentBalance = socketController.socketModel.playerData.Balance;
            if (totalBet_text) totalBet_text.text = currentTotalBet.ToString();
            // if (betPerLine_text) betPerLine_text.text = socketController.socketModel.initGameData.Bets[betCounter].ToString();
            // PayLineCOntroller.paylines = socketController.socketModel.initGameData.lineData;
            uIManager.UpdatePlayerInfo(socketController.socketModel.playerData);
            uIManager.PopulateSymbolsPayout(socketController.socketModel.uIData);
            Application.ExternalCall("window.parent.postMessage", "OnEnter", "*");
        }
        else
        {
            uIManager.PopulateSymbolsPayout(socketController.socketModel.uIData);
        }


    }


    void ExecuteSpin() => StartCoroutine(SpinRoutine());


    void ExecuteAutoSpin()
    {
        if (!isSpinning && autoOptions[autoSpinCounter] > 0)
        {

            isAutoSpin = true;
            autoSpinText.text = autoOptions[autoSpinCounter].ToString();
            autoSpinText.transform.parent.gameObject.SetActive(true);
            // AutoSpin_Button.gameObject.SetActive(false);

            AutoSpinStop_Button.gameObject.SetActive(true);
            autoSpinRoutine = StartCoroutine(AutoSpinRoutine());
        }

    }

    IEnumerator FreeSpinRoutine()
    {
        uIManager.ToggleFreeSpinPanel(true);
        uIManager.EnablePurplebar(true);
        uIManager.CloseFreeSpinPopup();
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

        ToggleButtonGrp(true);
        isAutoSpin = false;
        isSpinning = false;
        isFreeSpin = false;
        yield return null;
    }
    IEnumerator AutoSpinRoutine()
    {
        int noOfSPin = autoOptions[autoSpinCounter];
        while (noOfSPin > 0 && isAutoSpin)
        {
            noOfSPin--;
            autoSpinText.text = noOfSPin.ToString();

            yield return SpinRoutine();
            yield return new WaitForSeconds(0.5f);

        }
        autoSpinText.transform.parent.gameObject.SetActive(false);
        autoSpinText.text = "0";
        isSpinning = false;
        StartCoroutine(StopAutoSpinCoroutine());
        yield return null;
    }

    private IEnumerator StopAutoSpinCoroutine(bool hard = false)
    {
        Debug.Log("stop autospin called");
        isAutoSpin = false;
        AutoSpin_Button.gameObject.SetActive(true);
        AutoSpinStop_Button.gameObject.SetActive(false);
        if (!hard)
            yield return new WaitUntil(() => !isSpinning);

        if (autoSpinRoutine != null)
        {
            StopCoroutine(autoSpinRoutine);
            autoSpinRoutine = null;
            autoSpinText.transform.parent.gameObject.SetActive(false);
            autoSpinText.text = "0";
        }
        AutoSpinPopup_Button.gameObject.SetActive(true);
        if (!hard)
            ToggleButtonGrp(true);
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

        if (!isFreeSpin && !specialSpin)
            uIManager.DeductBalanceAnim(socketController.socketModel.playerData.Balance - currentTotalBet, socketController.socketModel.playerData.Balance);

        yield return OnSpin();
        yield return OnSpinEnd();
        if (socketController.socketModel.resultGameData.isFreeSpin)
        {
            int prevFreeSpin = freeSpinCount;
            freeSpinCount = socketController.socketModel.resultGameData.freeSpinCount;
            uIManager.UpdateFreeSpinInfo(freeSpinCount);
            isFreeSpin = true;
            specialSpin = false;
            if (autoSpinRoutine != null)
            {
                yield return StopAutoSpinCoroutine(true);
            }

            if (freeSpinRoutine != null)
            {
                StopCoroutine(freeSpinRoutine);
                freeSpinStartButton.gameObject.SetActive(false);
                uIManager.FreeSpinPopup(freeSpinCount - prevFreeSpin,false);
                yield return new WaitForSeconds(2f);
                uIManager.CloseFreeSpinPopup();
                freeSpinStartButton.gameObject.SetActive(true);
                freeSpinRoutine = StartCoroutine(FreeSpinRoutine());
            }
            else
            {

                uIManager.FreeSpinPopup(freeSpinCount,true);
                audioController.playBgAudio("FP");
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
    bool OnSpinStart()
    {

        isSpinning = true;
        winIterationCount = 0;
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

        var spinData = new { data = new { currentBet = betCounter, currentLines = 1, spins = 1 }, id = "SPIN" };
        socketController.SendData("message", spinData);
        yield return slotManager.StartSpin();
        yield return new WaitUntil(() => socketController.isResultdone);
        yield return new WaitForSeconds(0.35f);
        // slotManager.StopIconAnimation();
        slotManager.PopulateSLotMatrix(socketController.socketModel.resultGameData.resultSymbols);
        currentBalance = socketController.socketModel.playerData.Balance;
        yield return slotManager.StopSpin();
        audioController.PlaySpinStopAudio();
        yield return new WaitForSeconds(0.2f);
        // if (audioController) 


    }
    IEnumerator OnSpinEnd()
    {
        audioController.StopSpinAudio();
        if (socketController.socketModel.resultGameData.symbolsToEmit.Count > 0)
        {
            audioController.PlayWLAudio("electric");
            slotManager.StartIconAnimation(Helper.RemoveDuplicates(socketController.socketModel.resultGameData.symbolsToEmit), socketController.socketModel.resultGameData.resultSymbols.Count);
            yield return new WaitForSeconds(1.5f);
            audioController.StopWLAaudio();
        }

        uIManager.UpdatePlayerInfo(socketController.socketModel.playerData);

        if (!isFreeSpin)
        {
            specialSpin = socketController.socketModel.resultGameData.isLevelUp;

            if (specialSpin)
            {
                audioController.PlaySizeUpSound(true);
                slotManager.ResizeSlotMatrix(socketController.socketModel.resultGameData.level);
                yield return new WaitForSeconds(1.5f);
                audioController.PlaySizeUpSound(false);
            }
            else
            {
                if (socketController.socketModel.resultGameData.level == 0)
                {
                    audioController.PlaySizeUpSound(true);
                    slotManager.ResizeSlotMatrix(0);
                    yield return new WaitForSeconds(1.5f);
                    audioController.PlaySizeUpSound(false);

                }
            }
        }
        if (socketController.socketModel.playerData.currentWining > 0)
        {

            CheckWinPopups(socketController.socketModel.playerData.currentWining);
            yield return uIManager.WinTextAnim(socketController.socketModel.playerData.currentWining);
            audioController.StopWLAaudio();

        }
        if (isFreeSpin)
            uIManager.UpdateFreeSpinInfo(winnings: socketController.socketModel.playerData.currentWining);
        slotManager.StopIconAnimation();
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
            if (betCounter < socketController.socketModel.initGameData.Bets.Count - 1)
            {
                betCounter++;
            }
        }
        else
        {
            if (betCounter > 0)
            {
                betCounter--;
            }
        }

        currentTotalBet = socketController.socketModel.initGameData.Bets[betCounter];
        if (totalBet_text) totalBet_text.text = currentTotalBet.ToString();
        if (currentBalance < currentTotalBet)
            uIManager.LowBalPopup();
    }

    private void OnAutoSpinChange(bool inc)
    {

        if (audioController) audioController.PlayButtonAudio();

        if (inc)
        {
            if (autoSpinCounter < autoOptions.Count - 1)
            {
                autoSpinCounter++;
            }
        }
        else
        {
            if (autoSpinCounter > 0)
            {
                autoSpinCounter--;
            }
        }

        autoSpinShowText.text = autoOptions[autoSpinCounter].ToString();


    }


    void CheckWinPopups(double amount)
    {
        if (amount >= currentTotalBet * 5 && amount < currentTotalBet * 7.5)
        {
            uIManager.EnableWinPopUp(1);
            audioController.PlayWLAudio("big");

        }
        else if (amount >= currentTotalBet * 7.5 && amount < currentTotalBet * 10)
        {
            uIManager.EnableWinPopUp(2);
            audioController.PlayWLAudio("big");

        }
        else if (amount >= currentTotalBet * 10)
        {
            uIManager.EnableWinPopUp(3);
            audioController.PlayWLAudio("big");

        }
        else
        {
            uIManager.EnableWinPopUp(0);
            audioController.PlayWLAudio();

        }
    }


}
