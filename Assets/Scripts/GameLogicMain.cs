using UnityEngine;
using System.Collections;

public class GameLogicMain : MonoBehaviour {

    public enum GameStates { Waiting, MoveSequence, Freestyle };

    private GameStates gameState = GameStates.Waiting;
    [HideInInspector]
    public float timeLeft
    {
        get;
        private set;
    }
    public float maxTime
    {
        get;
        private set;
    }

    // A sequence of successful gestures triggers freestyle mode
    private uint sequenceNumber = 0;
    private float difficultyFactor = 1.0f; // The difficulty increases for each new sequence

    public uint maxSequenceNumber = 10; // number of moves between freestyle sessions
    public float freestyleTime = 10.0f; // seconds of freestyle mode time between sequences
    public  float beginnerSequenceTime = 30.0f; // When "Start game" is pressed this gets set as the remaining time
    public float difficultySteepness = 1.1f; // this number gets multiplied by the difficulty factor for each successful sequence

    private void resetTime()
    {
        // need to give less and less time to subsequent sequences so that difficulty increases
        float sequenceStartTime = beginnerSequenceTime / difficultyFactor;
        timeLeft = sequenceStartTime;
        maxTime = timeLeft;
        sequenceNumber = 0;
    }

    void Start () {
		Messenger.AddListener<Gestures, float> (Events.Gesture, HandleGesture);
		Messenger.AddListener (Events.CorrectGesture, HandleCorrectGesture);
        Messenger.AddListener (Events.StartFreestyleMode, HandleFreestyleTriggered);
        Messenger.AddListener (Events.FreestyleModeOver, HandleFreestyleModeOver);
        Messenger.AddListener (Events.StartSequenceMode, HandleStartSequenceMode);
        Messenger.AddListener (Events.StartGame, HandleStartGame);
		resetTime ();
	}

    public void StartGameButton()
    {
        Messenger.Broadcast(Events.StartGame);
        // deactivate button
        // reset game
        // change game mode to sequence etc...
    }

    void HandleStartGame()
    {
        difficultyFactor = 1.0f; // reset the difficulty 
        Messenger.Broadcast(Events.StartSequenceMode);
    }

    void HandleStartSequenceMode()
    {
        resetTime();
        gameState = GameStates.MoveSequence;
        NextGesture();
    }

    void HandleFreestyleModeOver()
    {
        // sequence is successful --> increase difficulty!
        difficultyFactor = difficultySteepness * difficultyFactor;
    }

    void Update () {
        if (gameState != GameStates.Waiting)
        {
            // "cheat" key space for debuging purpose
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Messenger.Broadcast(Events.CorrectGesture);
            }

            // increment the timer down
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0)
            {
                if (gameState == GameStates.MoveSequence)
                {
                    Messenger.Broadcast(Events.GameOver);
					gameState = GameStates.Waiting;
                }
                else if (gameState == GameStates.Freestyle) // elif in case more gameModes are added
                {
                    Messenger.Broadcast(Events.FreestyleModeOver);
                    Messenger.Broadcast(Events.StartSequenceMode);
                }
            }
        } // if (gameState != GameState.Waiting)
        else // the we are on the menu
        {
            bool joy_start_isdown = Mathf.Abs(Input.GetAxis("JoyStart")) > 0.4;
            if (joy_start_isdown)
            {
                StartGameButton();
            }
        }
    }

	private Gestures? currentGesture;

	void HandleGesture(Gestures g, float score) {
		Debug.Log ("Gesture: " + g.ToString () + " (" + score + ")");
        if (gameState == GameStates.MoveSequence)
        {
			if (currentGesture.HasValue && currentGesture.Value == g) {
				Messenger.Broadcast (Events.CorrectGesture);
			} else {
				Messenger.Broadcast (Events.IncorrectGesture);
			}
        }
        else if (gameState == GameStates.Freestyle)
        {
            // send some message to give some possitive feedback...
			Messenger.Broadcast (Events.CorrectGesture);
        }
	}

    
	void HandleCorrectGesture() {
        // This function only makes sense to call in sequence mode
        if (sequenceNumber >= maxSequenceNumber-1) // if sequence complete should go to freestyle mode
        {
            if (gameState != GameStates.Freestyle)
            {
                gameState = GameStates.Freestyle;
                Messenger.Broadcast(Events.StartFreestyleMode);
            }
        }
        else // else should reduce time a bit and increment sequencenumber
        {
            maxTime = maxTime * (0.9f);
            timeLeft = maxTime;
            sequenceNumber++;
            NextGesture();
        }
	}

    void HandleFreestyleTriggered()
    {
        timeLeft = freestyleTime;
        maxTime = timeLeft;
        sequenceNumber = 0;
    }

    Gestures RandomGesture() {
		var values = Gestures.GetValues(typeof(Gestures));
		int index = (int)(Random.value * values.Length);
		Gestures gesture = (Gestures)values.GetValue(index);
		return gesture;
	}

	private void NextGesture() {
		//select new gesture
		var previousGesture = currentGesture;

		while(currentGesture == previousGesture) {
			currentGesture = RandomGesture ();
		}

		Messenger.Broadcast<Gestures> (Events.NewGesture, currentGesture.Value);
	}
}
