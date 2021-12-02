using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GlobalManager : MonoBehaviour
{
    public static GlobalManager Instance;

    public List<CommandInGame> ListCommandsInGame = new List<CommandInGame>();

    private EnumClass.GameState currentGameState;

    [SerializeField] private int actionsTimerDuration;
    [SerializeField] private int buffTimerDuration;
    private int currentTimer;

    public SO_PanelChoice[] panelChoiceArray;

    private int turnCount;

    #region Unity Basic Events
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        turnCount = 1;
        StartState(EnumClass.GameState.ChoseBuffTurn);
        UIManager.Instance.UpdateTurnCount(turnCount);
        UIManager.Instance.DisplayGameScreen(true);
        UIManager.Instance.DisplayEndScreen(false);
    }

    #endregion
    
    #region Coroutines
    
    IEnumerator ActionsChoiceCoroutine()
    {
        UIManager.Instance.DisplayPhaseTitle("Phase du choix des actions");
        UIManager.Instance.DisplayPhaseDescription("Choisissez votre prochain déplacement avec les flèches directionnelles, et A pour attaquer.");
        //UIManager.Instance.DisplayAllPlayersUI(PlayerManager.Instance.PlayerList, true);

        TwitchManager.Instance.playersCanMakeActions = true;

        currentTimer = actionsTimerDuration;

        InputManager.Instance.EnableActionInputs(true);
        UIManager.Instance.ActivateTimerBar(true);

        while (currentTimer > 0)
        {
            yield return new WaitForSeconds(1);
            currentTimer--;
            UIManager.Instance.UpdateTimerBar((float)currentTimer/actionsTimerDuration);
        }
        
        
        //UIManager.Instance.DisplayAllPlayersUI(PlayerManager.Instance.PlayerList, false);
        UIManager.Instance.ActivateTimerBar(false);
        InputManager.Instance.EnableActionInputs(false);

        TwitchManager.Instance.playersCanMakeActions = false;

        StartState(EnumClass.GameState.ActionTurn);
        
    }
    
    IEnumerator BuffChoiceCoroutine()
    {
        UIManager.Instance.DisplayPhaseTitle("Phase d'amélioration");
        UIManager.Instance.DisplayPhaseDescription("Votre décision affectera grandement votre manière de jouer");
        UIManager.Instance.ActivateTimerBar(true);

        UIManager.Instance.UpdateChoiceCardsImage();
        UIManager.Instance.DisplayChoiceScreen(true);
        
        InputManager.Instance.EnableChoiceInputs(true);
        TwitchManager.Instance.playersCanMakeChoices = true;

        currentTimer = buffTimerDuration;

        while (currentTimer > 0)
        {
            yield return new WaitForSeconds(1);
            currentTimer--;
            UIManager.Instance.UpdateTimerBar((float)currentTimer/buffTimerDuration);
        }
        
        UIManager.Instance.ActivateTimerBar(false);
        UIManager.Instance.DisplayChoiceScreen(false);
        
        TwitchManager.Instance.playersCanMakeChoices = false;
        InputManager.Instance.EnableChoiceInputs(false);

        CheckPlayersChoicesInputs();
        
        StartAllActionsInGame();
        
        StartState(EnumClass.GameState.WaitingTurn);
    }
    
    
    #endregion

    #region ActionsInGame Handling
    
    public void AddActionInGameToList(CommandInGame ActionToAdd)
    {
        //Ici on devra trier si le propri�taire de l'action que l'on ajoute a la liste n'avait pas deja une action dans la liste avant de remettre son action. 
        
        Debug.Log(ActionToAdd + "have been added to the list of actions");
        ListCommandsInGame.Add(ActionToAdd);
    }

    public void StartAllActionsInGame()
    {
        if (ListCommandsInGame.Count > 0)
        {
            UIManager.Instance.DisplayPhaseTitle("Phase d'action");
            UIManager.Instance.DisplayPhaseDescription("Déroulement des actions choisies précédemment");
            ListCommandsInGame[0].LaunchActionInGame();
        }
        else
        {
            Debug.Log("Personne n'a choisi d'action pendant ce tour !");
            EndActionTurn();
        }
        
    }
    
    #endregion
    
    #region GameState Handling
    
    void StartState(EnumClass.GameState nextState)
    {
        currentGameState = nextState;
        
        switch (currentGameState)
        {
            case(EnumClass.GameState.WaitingTurn):
                StartCoroutine(ActionsChoiceCoroutine());
                break;
            
            case(EnumClass.GameState.ActionTurn):
                PlayerManager.Instance.ManagePlayersDebuffs();
                StartAllActionsInGame();
                break;
            
            case(EnumClass.GameState.ChoseBuffTurn):
                StartCoroutine(BuffChoiceCoroutine());
                break;
            
            case(EnumClass.GameState.GameEnd):
                UIManager.Instance.DisplayEndScreen(false);
                UIManager.Instance.DisplayEndScreen(true);
                break;
        }
    }
    
    public void EndActionTurn()
    {
        turnCount++;
        UIManager.Instance.UpdateTurnCount(turnCount);
        CheckNextTurn(turnCount);
    }

    void CheckNextTurn(int nextTurn)
    {
        foreach (SO_PanelChoice choice in panelChoiceArray)
        {
            if (choice.turnToTakeEffect == turnCount)
            {
                StartState(EnumClass.GameState.ChoseBuffTurn);
                return;
            }
        }

        StartState(EnumClass.GameState.WaitingTurn);

    }


    public EnumClass.GameState GetCurrentGameState()
    {
        return currentGameState;
    }

    public int GetCurrentTurn()
    {
        return turnCount;
    }

    #endregion
    

    public List<CommandInGame> FindPlayerCommands(Player ownerOfCommands)
    {
        List<CommandInGame> listToReturn = new List<CommandInGame>();

        foreach (CommandInGame command in ListCommandsInGame)
        {
            if (command.OwnerPlayer == ownerOfCommands)
            {
                listToReturn.Add(command);
            }
        }

        return listToReturn;
    }

    public void DestroyAllCommandsOfPlayer(Player ownerOfCommands)
    {
        List<CommandInGame> listToDestroy = FindPlayerCommands(ownerOfCommands);

        for (int i = 0; i < listToDestroy.Count; i++)
        {
            listToDestroy[i].DestroyCommand();

            if (ListCommandsInGame.Contains(listToDestroy[i]))
            {
                ListCommandsInGame.Remove(listToDestroy[i]);
            }
        }
    }

    public void RemoveMoveCommandOfPlayer(Player ownerOfCommands)
    {
        List<CommandInGame> AllCommandsOfPlayer = FindPlayerCommands(ownerOfCommands);

        for (int i = 0; i < AllCommandsOfPlayer.Count; i++)
        {
            if (ListCommandsInGame.Contains(AllCommandsOfPlayer[i]) && AllCommandsOfPlayer[i] is CommandMoving)
            {
                AllCommandsOfPlayer[i].DestroyCommand();
                ListCommandsInGame.Remove(AllCommandsOfPlayer[i]);
            }
        }
    }

    public void RemoveAttackCommandOfPlayer(Player ownerOfCommands)
    {
        List<CommandInGame> AllCommandsOfPlayer = FindPlayerCommands(ownerOfCommands);

        for (int i = 0; i < AllCommandsOfPlayer.Count; i++)
        {
            if (ListCommandsInGame.Contains(AllCommandsOfPlayer[i]) && AllCommandsOfPlayer[i] is CommandAttack)
            {
                AllCommandsOfPlayer[i].DestroyCommand();
                ListCommandsInGame.Remove(AllCommandsOfPlayer[i]);
            }
        }
    }

    public int GetRandomChoiceIndex()
    {
        int index = (int) Random.Range(0, 3);
        return index;
    }

    public SO_PanelChoice GetPanelChoiceOfThisTurn()
    {
        foreach (var choicePanel in panelChoiceArray)
        {
            if (turnCount == choicePanel.turnToTakeEffect)
            {
                return choicePanel;
            }
        }

        return null;
    }
    
    
    private void CheckPlayersChoicesInputs()
    {
        foreach (var player in PlayerManager.Instance.PlayerList)
        {
            List<CommandInGame> playerPreviousChoiceInput = FindPlayerCommands(player);

            if (playerPreviousChoiceInput.Count == 0)
            {
                InputManager.Instance.ChoiceCommand(player, GetRandomChoiceIndex());
            }
        }
    }
    
    
    
    
}