using GXPEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;

class Greedy : Agent
{
    static Random myrandom = new Random();

    bool recognizeWin;
    bool recognizeLoss;
    int counter = 0;
    float timer;

    public Greedy(string name, bool pRecognizeWin = false, bool pRecognizeLoss = false) : base(name)
    {
        recognizeWin = pRecognizeWin;
        recognizeLoss = pRecognizeLoss;
    }

    public override int ChooseMove(GameBoard current, int timeLeftMS)
    {
        timer = Time.now;
        int ID = current.GetActivePlayer();
        Console.WriteLine(name + ": I'm playing as player {0}", ID);
        winnerInTurns(current.Clone(), 1);

        List<int> moves = current.GetMoves();

        if (recognizeWin)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                GameBoard clone = current.Clone();
                clone.MakeMove(moves[i]);
                if (clone.CheckWinner() == ID)
                {
                    Console.WriteLine(name + ": I can win!");
                    Console.WriteLine("Losses prevented: {0}", counter);
                    return moves[i];
                }
            }
        }
        if (recognizeLoss)
        {
            //See if other one can win, prevent this.
            for (int i = 0; i < moves.Count; i++)
            {
                GameBoard clone = current.Clone();
                clone.SetActivePlayer(clone.GetActivePlayer() * -1);
                clone.MakeMove(moves[i]);
                if (clone.CheckWinner() == clone.GetActivePlayer() * -1)
                {
                    Console.WriteLine(name + ": I can prevent a win!");
                    counter++;
                    return moves[i];
                }
            }
        }
        timer =  Time.now - timer;
        BoardGame.AddTimeToPlayer(timer, current.GetActivePlayer());
        // ...otherwise, choose a random move:
        return moves[myrandom.Next(0, moves.Count)];
    }

    private bool winnerInTurns(GameBoard board, int maxTurns, int turns = 0)
    {
        if (turns >= maxTurns)
        {
            return false;
        }
        int possibleTurns = board.GetMoves().Count;                 
        GameBoard tempBoard;                                        //Empty
        for (int i = 0; i < possibleTurns; i++)
        {
            tempBoard = board.Clone();                              //Cloned board
            tempBoard.MakeMove(board.GetMoves()[i]);                //Make move on cloned board
            if (tempBoard.CheckWinner() != 0)                       //If player won with that move
            {
                Console.WriteLine("Player " + tempBoard.CheckWinner() + " can win in " + (turns + 1) + " turns.");
                return true;                                        //Return true
            }
            if (winnerInTurns(tempBoard, maxTurns, turns + 1))         //If the step below returns true
            {
                return true;                                        //Also return true
            }
        }
        return false;                                               //If no way of winning was found, return false
    }
}
