using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using UnityEngine.UI;
using TMPro;

public class TwitchManager : SingletonMonobehaviour<TwitchManager>
{
    private TcpClient twitchClient;
    private StreamReader reader;
    private StreamWriter writter;

    [Header("Reference UI Connexion")]
    public TMP_InputField PasswordInput;
    public TMP_InputField ChannelNameInput;
    public GameObject PanelConnexion;


    [Header("Commande of the game")]
    public string AttackCommande;
    public string JoinCommande;
    public string QuitCommande;
    public string LeftCommande;
    public string RightCommande;
    public string UpCommande;
    public string DownCommande;
    public string ChoiceCommande = "!card";

    [Header("Parameters")]
    public string channelName;
    public int maxCharacterInNames;
    public int numberMaxOfPlayer;

    [SerializeField]
    private UI_MainMenu _UIMainMenu;
    
    bool bConnexionIsDone = false;
    [System.NonSerialized] public bool canJoinedGame = false;
    [System.NonSerialized] public bool playersCanMakeActions = false;
    [System.NonSerialized] public bool playersCanMakeChoices = false;
    [System.NonSerialized] public bool canReadCommands = false;

    public override bool DestroyOnLoad => false;

    private void Start() // Use datas store in PlayerPrefs
    {
        //PanelConnexion.SetActive(true);
        if (PlayerPrefs.HasKey("PasswordInput")) { PasswordInput.text = PlayerPrefs.GetString("PasswordInput"); }
        if (PlayerPrefs.HasKey("ChannelNameInput")) { ChannelNameInput.text = PlayerPrefs.GetString("ChannelNameInput"); }
    }

    public void Connect() //A lancer avec le boutton connect.
    {
        //Store datas of connexion in PlayerPrefs
        PlayerPrefs.SetString("PasswordInput", PasswordInput.text);
        PlayerPrefs.SetString("ChannelNameInput", ChannelNameInput.text.ToLower());

        twitchClient = new TcpClient("irc.chat.twitch.tv", 6667);
        reader = new StreamReader(twitchClient.GetStream());
        writter = new StreamWriter(twitchClient.GetStream());

        writter.WriteLine("PASS " + PasswordInput.text);
        writter.WriteLine("NICK " + ChannelNameInput.text.ToLower());
        writter.WriteLine("USER " + ChannelNameInput.text.ToLower() + " 8 * :" + ChannelNameInput.text.ToLower());
        writter.WriteLine("JOIN #" + ChannelNameInput.text.ToLower());
        writter.Flush();

        if (twitchClient.Connected)
        {
            bConnexionIsDone = true;
            _UIMainMenu.DisplayPlayButton();
            channelName = CutPlayerName(ChannelNameInput.text);
        }
    }

    public bool GetConnexionIsDone()
    {
        return bConnexionIsDone;
    }

    private void ReadChat()
    {
        if (twitchClient.Available > 0)
        {
            var message = reader.ReadLine();

            if (message.Contains("Your host is"))
            {
                PanelConnexion.SetActive(false);
            }
            
            if (message.Contains("PRIVMSG") && canReadCommands)
            {
                //Get the USER of the message.
                int splitPointU = message.IndexOf("!", 1);
                string NameOfUser = message.Substring(0, splitPointU);
                NameOfUser = NameOfUser.Substring(1);

                //Get the MESSAGE.
                int splitPointM = message.IndexOf(":", 1);
                string messageOfUser = message.Substring(splitPointM + 1);

                AnalyseChatCommand(NameOfUser, messageOfUser);
            }
        }
    }

    private void Update()
    {
        if (bConnexionIsDone && (!twitchClient.Connected))
        {
            Connect();
        }

        if (bConnexionIsDone)
        {
            ReadChat();
        }
        
    }

    /// <summary>
    /// Analyse player message to see if the message contains commands for the game
    /// </summary>
    /// <param name="nameOfPlayer"> Player to analyse command</param>
    /// <param name="messageOfPlayer"> Message to analyse </param>
    public void AnalyseChatCommand(string nameOfPlayer, string messageOfPlayer)
    {
        
        nameOfPlayer = CutPlayerName(nameOfPlayer);

        // Tcheck if game didn't start. 
        if (messageOfPlayer == JoinCommande && canJoinedGame) //Connection du joueur twitch dans le jeu 
        {
            if (!PlayerManager.Instance._listPlayersNames.Contains(nameOfPlayer) && PlayerManager.Instance._listPlayersNames.Count < numberMaxOfPlayer)
            {
                PlayerManager.Instance._listPlayersNames.Add(nameOfPlayer);
                PlayerManager.Instance.SpawnPlayerOnLobby(nameOfPlayer);
            }
        }


        if (PlayerManager.Instance._listPlayersNames.Contains(nameOfPlayer)) // S'assurer que le joueur est dans la liste des joueurs pour faire ces commandes. 
        {
            if (messageOfPlayer == QuitCommande) //Deconnection du joueur twitch du jeu. 
            {
                PlayerManager.Instance._listPlayersNames.Remove(nameOfPlayer);
            }

            if (playersCanMakeActions)
            {
                //Need to be in ActionState in the GlobalManager.

                Player currentplayer = PlayerManager.Instance.ReturnPlayerWithName(nameOfPlayer);

                if (messageOfPlayer == AttackCommande) //Attack
                {
                    InputManager.Instance.AttackCommand(currentplayer);
                }
                if (messageOfPlayer == LeftCommande) //MoveLeft
                {
                    InputManager.Instance.MoveCommand(currentplayer, EnumClass.Direction.Left);
                }
                if (messageOfPlayer == RightCommande) //MoveRight
                {
                    InputManager.Instance.MoveCommand(currentplayer, EnumClass.Direction.Right);
                }
                if (messageOfPlayer == UpCommande) //MoveTop
                {
                    InputManager.Instance.MoveCommand(currentplayer, EnumClass.Direction.Up);
                }
                if (messageOfPlayer == DownCommande) //MoveDown
                {
                    InputManager.Instance.MoveCommand(currentplayer, EnumClass.Direction.Down);
                }
            }
            
            if (playersCanMakeChoices)
            {
                //Need to be in ActionState in the GlobalManager.

                Player currentplayer = PlayerManager.Instance.ReturnPlayerWithName(nameOfPlayer);

                if (messageOfPlayer.Contains(ChoiceCommande))
                {
                    //Enlever le text de la commande
                    string numberOfCommandTxt = messageOfPlayer.Substring(ChoiceCommande.Length);

                    string officialsNumber = string.Empty;

                    //Chercher si nous possedons un/des chiffre dans notre nouveau string
                    foreach (char cara in numberOfCommandTxt)
                    {
                        if (char.IsDigit(cara))
                        {
                            officialsNumber += cara;
                        }
                    }

                    //Si le string n'est pas null on le transform en int
                    if (officialsNumber.Length > 0)
                    {
                        int numberOfCommand = int.Parse(officialsNumber);

                        if (numberOfCommand > 0)
                        {
                            numberOfCommand--;

                            InputManager.Instance.ChoiceCommand(currentplayer, numberOfCommand);
                        }
                    }
                    else 
                    {
                        return; 
                    }
                }
            }
        }        
    }

    public string CutPlayerName(string namePlayer)
    {
        
        if (namePlayer.Length > maxCharacterInNames)
        {
            string nameCut = "";
            for (int i = 0; i < maxCharacterInNames; i++)
            {
                nameCut += namePlayer[i];
            }
            return nameCut;
        }
        else
        {
            return namePlayer;
        }
    }

    /// <summary>
    /// Check if the twitch client is connected to a channel
    /// </summary>
    public bool IsConnected()
    {
        if (bConnexionIsDone)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Set if players can join game with chat commands
    /// </summary>
    /// <param name="state"></param>
    public void SetPlayersCanJoin(bool state)
    {
        canJoinedGame = state;
    }
    
    public void SetCanReadCommand(bool state)
    {
        canReadCommands = state;
    }

}
