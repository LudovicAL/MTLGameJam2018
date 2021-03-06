﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    public GameObject workerButtonPrefab;
    public int numberOfWorkers = 0;
    public Sprite spriteBusyWorker;
    public Sprite spriteFreeWorker;
	public List<GameObject> workerButtons;
	public GameObject panelWorker;
    private EndGame eg;
    private GameStatesManager gameStatesManager;    //Refers to the GameStateManager
    private StaticData.AvailableGameStates gameState;   //Mimics the GameStateManager's gameState variable at all time
    private Text textCasualties;
    private Text textSurvivors;
    private Text textRatio;
	private Slider SliderDiff;
    private float m_SuccessRatioNeeded = 0.3f;
	private CameraController cameraController;
	private CiviliansSpawner civiliansSpawner;
	private DifficultyParameters difficultyParameters;

    void Awake() {
		gameStatesManager = GameObject.Find("Scriptsbucket").GetComponent<GameStatesManager>();
        panelWorker = GameObject.Find("Panel Worker");
		difficultyParameters = GameObject.Find("Scriptsbucket").GetComponent<DifficultyParameters>();
		civiliansSpawner = GameObject.Find ("Scriptsbucket").GetComponent<CiviliansSpawner>();
		cameraController = GameObject.Find("Scriptsbucket").GetComponent<CameraController>();
		eg = GameObject.Find("Scriptsbucket").GetComponent<EndGame>();
		textCasualties = GameObject.Find("Text Casualties").GetComponent<Text>();
		textSurvivors = GameObject.Find("Text Survivors").GetComponent<Text>();
		textRatio = GameObject.Find("Text Ratio").GetComponent<Text>();
		SliderDiff = GameObject.Find("Slider Difficulty").GetComponent<Slider>();
        workerButtons = new List<GameObject>();
        for (int i = 0; i < numberOfWorkers; i++) {
            int index = i;
            GameObject newButton = Instantiate(workerButtonPrefab, panelWorker.transform);
            newButton.GetComponent<Button>().onClick.AddListener(delegate { WorkerButtonPress(index); });
            workerButtons.Add(newButton);
        }
    }

    // Use this for initialization
    void Start() {
        GameObject.Find("Button Start").GetComponent<Button>().onClick.AddListener(StartButtonPress);
        GameObject.Find("Button Quit").GetComponent<Button>().onClick.AddListener(QuitButtonPress);
        GameObject.Find("Button Abandon").GetComponent<Button>().onClick.AddListener(QuitButtonPress);
        GameObject.Find("Button Continue").GetComponent<Button>().onClick.AddListener(ContinueButtonPress);
		SliderDiff.onValueChanged.AddListener(delegate { OnSliderValueChanged(); });
        gameStatesManager.MenuGameState.AddListener(OnMenu);
        gameStatesManager.StartingGameState.AddListener(OnStarting);
        gameStatesManager.PlayingGameState.AddListener(OnPlaying);
        gameStatesManager.PausedGameState.AddListener(OnPausing);
        gameStatesManager.EndingGameState.AddListener(OnEnding);
        SetState(gameStatesManager.gameState);
        showPanel();
        UpdateWorkerButtons(true);
    }

	public void OnSliderValueChanged() {
		difficultyParameters.AdjustParameters ((int)SliderDiff.value);
	}

	private void showPanel() {
		switch (gameState) {
			case StaticData.AvailableGameStates.Ending:
				showPanel ("Panel End");
				break;
			case StaticData.AvailableGameStates.Menu:
				showPanel ("Panel Menu");
				break;
			case StaticData.AvailableGameStates.Paused:
				showPanel ("Panel Menu");
				break;
			case StaticData.AvailableGameStates.Playing:
				showPanel ("Panel Game");
				break;
			case StaticData.AvailableGameStates.Starting:
				showPanel ("Panel Game");
				break;
			default:
				showPanel ("Panel Menu");
				break;
		}
	}

    private void showPanel(string panelName)
    {
        for (int i = 0, max = this.transform.childCount; i < max; i++)
        {
            if (this.transform.GetChild(i).gameObject.name == panelName || this.transform.GetChild(i).gameObject.name == "EventSystem")
            {
                this.transform.GetChild(i).gameObject.SetActive(true);
            }
            else
            {
                this.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

    public void WorkerButtonPress(int buttonNo) {
		cameraController.ToggleCameraMode();
		bool newIsInCameraMode = cameraController.GetIsInCameraMode();

        // TODO: if we ever have more than one button, change only the one that gets changed
        UpdateWorkerButtons(newIsInCameraMode);
    }

    public void UpdateWorkerButtons(bool _isInCameraMode)
    {
        foreach (GameObject go in workerButtons)
        {
            Transform backgroundImage = go.transform.Find("BackgroundImage");
            if (backgroundImage != null)
            {
                Color updatedColor = _isInCameraMode ? Color.green : Color.yellow;
                updatedColor.a = 0.4f; // alpha
                backgroundImage.GetComponent<Image>().color = updatedColor;
            }
            Transform workerImage = go.transform.Find("Image Worker");
            if (workerImage != null)
            {
                workerImage.GetComponent<Image>().sprite = _isInCameraMode ? spriteFreeWorker : spriteBusyWorker;
            }
        }
    }

    public void StartButtonPress() {
		civiliansSpawner.SpawnCivilians ();
        RequestGameStateChange(StaticData.AvailableGameStates.Playing);
    }

    public void QuitButtonPress()
    {
        Application.Quit();
    }

    public void ContinueButtonPress() {
        if (GameObject.Find("WhatsNext").tag != "Respawn") {
			//TODO: MAKE IT SO THE LEVEL RELOAD ITSELF WITHOUT RELOADING THE SCENE (WHICH IS TROUBLESOME)
        }
    }

    //Listener functions a defined for every GameState
    protected void OnMenu() {
        SetState(StaticData.AvailableGameStates.Menu);
    }

    protected void OnStarting() {
        SetState(StaticData.AvailableGameStates.Starting);
    }

    protected void OnPlaying() {
        SetState(StaticData.AvailableGameStates.Playing);
    }

    protected void OnPausing() {
        SetState(StaticData.AvailableGameStates.Paused);
    }

    protected void OnEnding() {
        SetState(StaticData.AvailableGameStates.Ending);
        ShowEndScreen();
    }

    private void SetState(StaticData.AvailableGameStates state) {
        gameState = state;
		showPanel();
    }

    //Use this function to request a game state change from the GameStateManager
    private void RequestGameStateChange(StaticData.AvailableGameStates state) {
        gameStatesManager.ChangeGameState(state);
    }

    public void ShowEndScreen() {
        Camera camera = Camera.main;
        camera.transform.position = new Vector3(0, 0);
        camera.orthographicSize = 3f;

        textCasualties.text = eg.m_NumberOfCasualties.ToString();
        textSurvivors.text = eg.m_NumberOfCivilians.ToString();
        float ratio = (float)eg.m_NumberOfCivilians / ((float)eg.m_NumberOfCasualties + (float)eg.m_NumberOfCivilians);
        textRatio.text = ratio.ToString("0.0%");

        if (ratio >= m_SuccessRatioNeeded) {
            GameObject.Find("Text Ratio").GetComponent<Text>().color = new Color(0.3529f, .5098f, .047f);
            GameObject.Find("WhatsNext").GetComponent<Text>().text = "Continue";
            GameObject.Find("Text WinLose").GetComponent<Text>().text = "You win";
        } else {
            GameObject.Find("Text Ratio").GetComponent<Text>().color = new Color(0.196f, 0.0078f, 0.0078f);
            GameObject.Find("WhatsNext").GetComponent<Text>().text = "Restart";
            GameObject.Find("Text WinLose").GetComponent<Text>().text = "You lose";
            GameObject.Find("WhatsNext").tag = "Respawn";
        }
    }
}
