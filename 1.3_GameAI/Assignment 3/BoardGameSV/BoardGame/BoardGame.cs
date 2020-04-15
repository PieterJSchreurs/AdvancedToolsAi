using System;
using GXPEngine;
using System.Threading;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel; 

public class BoardGame : Game
{
    string outputPath = "C:\\Users\\Piet\\Documents\\GitHub\\Game AI\\1.3_GameAI\\Results.xlsx";
   
    GameBoard mainboard;
    BoardView mainview;
    OptionButton gameChoiceButton;
    OptionButton[] playerSelect;
    Button message;
    OptionButton startButton;
    OptionButton gameLength;
    Button[] score;
    OptionButton matchSize;
    OptionButton autoPlay;
    StopWatch[] clock;

    AnimationSprite[] playerIndicator;

    Agent[] Player = new Agent[2];

    Agent activeplayer;

    Thread moveChooseThread = null;

    int[] wins = new int[2];
    int startingplayer;
    int _move;
    int activeplayerindex;

    bool boardchanged = true;

    enum GameState
    {
        Pause,          // end of game - pause to show outcome
        Menu,           // menu - activate "choose game" buttons, etc
        WaitForMove,    // wait for human or AI move
        ProcessMove,    // process the given move: update the main board
        CheckWin        // check whether someone has won and if so display (one frame after ProcessMove, to allow view to first update itself)
    }

    GameState state;

    public BoardGame() : base(800, 600, false, true)
    {

        AddChild(new Sprite("../../assets/wood.jpg"));

        Player[0] = new RandomPlayer("Random");
        Player[1] = new RandomPlayer("Random+", true);
        //Player [7] = new AgentMain.Alpha ("Alpha");

        startingplayer = -1;

        state = GameState.Menu;

        //gameChoiceButton = new OptionButton (190, 30, 600, 15, new string[]{ "Tic Tac Toe", "Connect Four", "Pentago" , "Gomoku 9x9","Domineering 7x7","Domineering 9x9","Othello","Hex 7x7","Hex 10x10"}); // , "Gomoku 15x15"
        gameChoiceButton = new OptionButton(190, 30, 600, 15, new string[] { "Gomoku 9x9" });
        AddChild(gameChoiceButton);
        gameChoiceButton.OnButtonClick += RegisterBoardChoice;

        ///matchSize=new OptionButton(190,30,600,55,new string[]{"Best of 1","Best of 2","Best of 3","Best of 4","Best of 5","Best of 6"});
        matchSize = new OptionButton(190, 30, 600, 55, new string[] { "Best of 100" });
        AddChild(matchSize);
        //gameLength = new OptionButton (190, 30, 600, 95, new string[]{ "9 minute", "5 minutes", "2 minutes", "1 minutes" });
        gameLength = new OptionButton(190, 30, 600, 95, new string[] { "1 minutes" });
        AddChild(gameLength);
        autoPlay = new OptionButton(190, 30, 600, 560, new string[] {  "Autoplay on" });
        AddChild(autoPlay);

        playerSelect = new OptionButton[2];
        playerIndicator = new AnimationSprite[2];
        score = new Button[2];
        clock = new StopWatch[2];

        string[] playerNames = new string[Player.Length];
        for (int i = 0; i < Player.Length; i++)
            playerNames[i] = Player[i].name;

        for (int p = 0; p < 2; p++)
        {
            playerSelect[p] = new OptionButton(100, 30, 130 + 240 * p, 15, playerNames);
            AddChild(playerSelect[p]);

            playerIndicator[p] = new AnimationSprite("../../assets/tileset.png", 3, 1);
            playerIndicator[p].SetFrame(2 - p);
            playerIndicator[p].SetScaleXY(0.3f, 0.3f);
            playerIndicator[p].SetXY(245 + 75 * p, 10); // assuming width=35?
            AddChild(playerIndicator[p]);

            score[p] = new Button(30, 30, 90 + 390 * p, 15, "0");
            AddChild(score[p]);

            clock[p] = new StopWatch(60, 30, 20 + 500 * p, 15, 300000);
            clock[p].OnTimeOut += TimeOut;
            AddChild(clock[p]);
        }
        message = new Button(400, 30, 100, 560, "Choose game and press Start Match");
        AddChild(message);

        startButton = new OptionButton(190, 30, 600, 200, new string[] { "Start New Match", "End Match" });
        AddChild(startButton);
        startButton.OnButtonClick += StartButtonPress;
    }

    int PlayerToIndex(int player)
    {
        return (player + 1) / 2;
    }

    public void RegisterBoardChoice(int choice)
    {
        //gameChoice = choice;
        boardchanged = true;
    }

    void CreateBoard()
    {
        if (boardchanged)
        {
            if (mainboard != null)
            {
                mainview.Destroy();
            }
            string gamechoice = gameChoiceButton.GetSelectionString();
            switch (gamechoice)
            {
                case "Gomoku 9x9":
                    mainboard = new GomokuBoard(9, 9, 5, 5);
                    mainview = new GomokuView((GomokuBoard)mainboard);
                    break;

                default:
                    Console.WriteLine("Unknown game name");
                    return;
            }
            AddChild(mainview);
            boardchanged = false;
        }
        else
        {
            mainboard.Reset();
        }
        mainboard.SetActivePlayer(1);
    }

    // Registers human move


    void WaitForAIMove()
    {
        //Console.WriteLine ("Start of the parallel thread");
        _move = activeplayer.ChooseMove(mainboard.Clone(), clock[PlayerToIndex(mainboard.GetActivePlayer())].GetTime()); // blocking call, hence threading
        if (state == GameState.WaitForMove)
        {// Maybe game state has changed in other thread!
            state = GameState.ProcessMove;
            //Console.WriteLine ("End of parallel thread: processing AI move {0}", _move);
        }
    }

    public void StartButtonPress(int selection)
    {
        if (state == GameState.Menu)
        { // Start a new match
            message.ShowMessage("");
            gameChoiceButton.SetActive(false);
            matchSize.SetActive(false);
            gameLength.SetActive(false);
            startingplayer = -1;
            wins[0] = 0;
            score[0].ShowMessage("0");
            wins[1] = 0;
            score[1].ShowMessage("0");
            startButton.SetSelection(1); // ("End Match")
            StartGame();
        }
        else
        {
            AbortAIThread();
            message.ShowMessage("Choose game and press Start Match");
            GotoMenu();
        }
    }

    void StartGame()
    {
        CreateBoard();
        mainboard.SetActivePlayer(startingplayer);
        int gamelength = int.Parse(gameLength.GetSelectionString().Substring(0, 1)) * 60000;
        clock[0].SetTime(gamelength);
        clock[1].SetTime(gamelength);
        state = GameState.CheckWin;
    }

    public void TimeOut()
    {
        AbortAIThread();
        int winnerindex;
        if (clock[0].GetTime() == 0)
            winnerindex = 1;
        else
            winnerindex = 0;
        wins[winnerindex]++;
        score[winnerindex].ShowMessage(wins[winnerindex].ToString());
        message.ShowMessage("Time out! " + playerSelect[winnerindex].GetSelectionString() + " wins");
        state = GameState.Pause;
    }

    void FinishGame()
    {
        // Check match progress
        //Console.WriteLine("Finishing game");
        for (int winnerindex = 0; winnerindex <= 1; winnerindex++)
        {
            if (wins[winnerindex] > (matchSize.GetSelection() + 100) / 2)
            { // Match over
                message.ShowMessage(playerSelect[winnerindex].GetSelectionString() + " wins the match!");
                GotoMenu();

                //Excel.Application excel = new Excel.Application();
                //Excel.Workbook workbook = excel.Workbooks.Add(Type.Missing);
                //Excel.Worksheet sheet = (Excel.Worksheet)workbook.ActiveSheet;
                //((Excel.Range)sheet.Cells[1, 1]).Value = "Hello";

                //workbook.SaveAs(outputPath);
                //workbook.Close();
                //excel.Quit();
                return;
            }
        }
        if (wins[0] + wins[1] == matchSize.GetSelection() + 100)
        {
            message.ShowMessage("The match is a draw!");
            GotoMenu();
        }
        else
        { // Next game of the match:
            Console.WriteLine("Next game of match");
            startingplayer = -startingplayer;
            StartGame();
        }
    }

    // Called whenever the match is over; activates "menu state"
    void GotoMenu()
    {
        state = GameState.Menu;
        gameChoiceButton.SetActive(true);
        matchSize.SetActive(true);
        gameLength.SetActive(true);
        playerIndicator[0].color = 0xffffffff;
        playerIndicator[1].color = 0xffffffff;
        clock[0].SetActive(false);
        clock[1].SetActive(false);
        startButton.SetSelection(0); // ("Start Match")
    }

    void AbortAIThread()
    {
        if (moveChooseThread != null)
        {
            Console.WriteLine("Aborting the AI thread");
            moveChooseThread.Abort();
            moveChooseThread.Join();
            moveChooseThread = null;
            Console.WriteLine("Aborting successful");
        }
    }

    // the main loop:
    public void Update()
    {

        switch (state)
        {
            case GameState.Pause:
                if (Input.GetMouseButtonDown(0) || autoPlay.GetSelection() == 0)
                {
                    FinishGame();
                }
                break;
            case GameState.CheckWin:
                int winner = mainboard.CheckWinner();
                bool gameover = false;
                if (winner != 0)
                {   // It there a winner?
                    winner = PlayerToIndex(winner);
                    wins[winner]++;
                    score[winner].ShowMessage(wins[winner].ToString());
                    message.ShowMessage(playerSelect[winner].GetSelectionString() + " wins (click to continue)");
                    gameover = true;
                }
                else if (mainboard.GetMoves().Count == 0)
                {   // Otherwise, maybe it's a draw?
                    message.ShowMessage("It's a draw (click to continue)");
                    gameover = true;
                }

                if (gameover)
                {
                    state = GameState.Pause;
                }
                else
                { // Switch turn, activate player
                    activeplayerindex = PlayerToIndex(mainboard.GetActivePlayer());
                    playerIndicator[activeplayerindex].color = 0xffa0ffa0;  // light green
                    clock[activeplayerindex].SetActive(true);
                    activeplayer = Player[playerSelect[activeplayerindex].GetSelection()];
                    //Console.WriteLine ("Possible moves for player "+ activeplayer + ":");
                    //foreach (int x in mainboard.GetMoves())
                    //	Console.Write(mainboard.MoveToString(x));
                    state = GameState.WaitForMove;

                    message.ShowMessage("Thinking...");
                    moveChooseThread = new Thread(WaitForAIMove);
                    moveChooseThread.Priority = ThreadPriority.Highest;
                    moveChooseThread.Start();
                    //Console.WriteLine ("Continuing main thread");

                }
                break;
            case GameState.ProcessMove:
                playerIndicator[activeplayerindex].color = 0xffffffff;
                clock[activeplayerindex].SetActive(false);

                Console.WriteLine("Making move {0} for player {1}", _move, mainboard.Symbol(mainboard.GetActivePlayer()));
                mainboard.MakeMove(_move);
                Console.WriteLine(mainboard.ToString());
                Console.WriteLine("Maximum number of moves left: " + mainboard.MaxMovesLeft());
                state = GameState.CheckWin;
                break;
                //case GameState.Menu:
                //	break;
        }
    }

    static void Main()
    {
        new BoardGame().Start();
    }
}