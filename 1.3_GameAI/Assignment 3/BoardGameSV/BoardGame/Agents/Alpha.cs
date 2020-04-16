using System.Collections.Generic;
using System;
using GXPEngine;	// Use this for Time

namespace AgentMain {

	class Alpha : Agent {
		static Random myrandom=new Random();
		private int sampleSize = 0;

		public Alpha(string name, int pSampleSize = 10) : base(name) {
			sampleSize = pSampleSize;
		}

		public override int ChooseMove (GameBoard current, int timeLeftMS)
		{
			int ID = current.GetActivePlayer ();
			Console.WriteLine (name+": I'm playing as player {0}", ID);

			List<int> moves = current.GetMoves ();
			//Check for instant win.
			for (int i = 0; i < moves.Count; i++)
			{
				GameBoard clone = current.Clone();
				clone.MakeMove(moves[i]);
				if (clone.CheckWinner() == ID)
				{
					return moves[i];
				}
			}
			//Check for instant loss.
			for (int i = 0; i < moves.Count; i++)
			{
				GameBoard clone = current.Clone();
				clone.SetActivePlayer(clone.GetActivePlayer() * -1);
				clone.MakeMove(moves[i]);
				if (clone.CheckWinner() == clone.GetActivePlayer() * -1)
				{
					return moves[i];
				}
			}

			int[,] gameOutcomes = new int[current.GetMoves().Count, sampleSize];
			float[] gameOutcomeAverages = new float[current.GetMoves().Count];
			int bestMove = 0;

			for (int i = 0; i < current.GetMoves().Count; i++)
			{
				GameBoard tempBoard = current.Clone();
				tempBoard.MakeMove(current.GetMoves()[i]);
				gameOutcomeAverages[i] = 0;
				for (int j = 0; j < sampleSize; j++)
				{
					gameOutcomes[i, j] = playRandomGame(tempBoard.Clone());
					gameOutcomeAverages[i] += gameOutcomes[i, j];
				}
				gameOutcomeAverages[i] /= sampleSize;
				if (Math.Abs(ID - gameOutcomeAverages[i]) <= Math.Abs(ID - gameOutcomeAverages[bestMove]))
				{
					bestMove = i;
				}
			}

			string outcomesString = "Games: ";
			for (int i = 0; i < gameOutcomes.GetLength(0); i++)
			{
				outcomesString += "\n Move " + i + ": ";
				for (int j = 0; j < gameOutcomes.GetLength(1); j++)
				{
					outcomesString += gameOutcomes[i, j] + ", ";
				}

				outcomesString += "\t Average: " + gameOutcomeAverages[i];
			}
			return moves[bestMove];
		}

		public int playRandomGame(GameBoard board)
		{
			while (board.GetMoves().Count > 0)
			{
				board.MakeMove(board.GetMoves()[myrandom.Next(0, board.GetMoves().Count)]);
				if (board.CheckWinner() != 0)
				{
					return board.CheckWinner();
				}
			}
			if (board.CheckWinner() != 0)
			{
				return board.CheckWinner();
			}
			return 0;
		}
	}
}