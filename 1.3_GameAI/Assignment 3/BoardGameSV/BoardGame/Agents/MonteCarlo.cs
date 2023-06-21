using GXPEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace AgentMain
{
    class MonteCarlo : Agent
    {
        static Random myrandom = new Random();
        int depth;
        int sampleSize;
        bool recognizeWin;
        bool recognizeLoss;
        float timer;
        public MonteCarlo(string pName, int pDepth, int pSampleSize) : base(pName)
        {
            depth = pDepth;
            sampleSize = pSampleSize;

        }
        public override int ChooseMove(GameBoard current, int timeLeftMS)
        {
            timer = Time.now;
            int ID = current.GetActivePlayer();
            List<int> moves = current.GetMoves();

            int bestMove = 0;
            float bestScore = -10;
            //Check for instant win.
            for (int i = 0; i < moves.Count; i++)
            {
                GameBoard gameboardClone = current.Clone();
                gameboardClone.MakeMove(moves[i]);
                if (gameboardClone.CheckWinner() == ID)
                {
                    return moves[i];
                }
            }
            //Check for instant loss.
            for (int i = 0; i < moves.Count; i++)
            {
                GameBoard gameBoardClone = current.Clone();
                gameBoardClone.SetActivePlayer(gameBoardClone.GetActivePlayer() * -1);
                gameBoardClone.MakeMove(moves[i]);
                if (gameBoardClone.CheckWinner() == gameBoardClone.GetActivePlayer() * -1)
                {
                    return moves[i];
                }
            }
            for (int i = 0; i < moves.Count; i++)
            {
                int wins = 0;
                int losses = 0;
                GameBoard cloneBoard = current.Clone();
                cloneBoard.MakeMove(moves[i]);

                for (int j = 0; j < sampleSize; j++)
                {
                    GameBoard clone = cloneBoard.Clone();
                    int winner = PlayRandomGame(clone, depth);
                    if (winner == ID) wins++;
                    else if (winner == -ID) losses++;
                }
                float score = (wins - losses) / (float)sampleSize;

                if (bestScore < score)
                {
                    bestScore = score;
                    bestMove = moves[i];
                }
            }
            timer = Time.now - timer;
            BoardGame.AddTimeToPlayer(timer, current.GetActivePlayer());
            return bestMove;
        }

        private int PlayRandomGame(GameBoard pGameBoardCopy, int pDepth)
        {
            int playedDepth = 0;
            int ID = pGameBoardCopy.GetActivePlayer() * -1;
            while (playedDepth < pDepth)
            {
                if (pGameBoardCopy.GetMoves().Count == 0) //All moves are done.
                {
                    return pGameBoardCopy.CheckWinner();
                }
                pGameBoardCopy.MakeMove(pGameBoardCopy.GetMoves()[myrandom.Next(0, pGameBoardCopy.GetMoves().Count)]);
                if (pGameBoardCopy.CheckWinner() != 0)
                {
                    return pGameBoardCopy.CheckWinner();
                }
                playedDepth++;
            }

            //}
            return pGameBoardCopy.CheckWinner();
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
}
