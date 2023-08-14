using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    public class EvilBot : IChessBot
    {

		// global variables
		private Board globalBoard;
		private readonly Random random = new Random();
		private int maxDepth = 4, currentDepth = 0;

		public Move Think(Board board, Timer timer)
		{
			globalBoard = board;
			int alpha = -2000;
			int beta = 2000;
			int currentPlayer = board.IsWhiteToMove ? 1 : -1;
			return DeepThink(timer, alpha, beta, currentPlayer).Item1;
		}

		private (Move, double) DeepThink(Timer timer, double alfa, double beta, int player)
		{
			int bestMoveIndex = -1;
			double bestMoveValue = -2000 * player;

			// determine moves and ending
			Move[] moves = globalBoard.GetLegalMoves();
			double[] moveValues = new double[moves.Length];
			// (Move, double)[] moveHelper = new (Move, double)[moves.Length];

			// if a leaf is reached return the static evaluation
			if (currentDepth == maxDepth || globalBoard.IsInCheckmate() || globalBoard.IsDraw())
			{
				return (new Move(), EndingEvaluation());
			}

			currentDepth++;
			for (int k = 0; k < moves.Length; k++)
			{
				globalBoard.MakeMove(moves[k]);
				moveValues[k] = DeepThink(timer, alfa, beta, -player).Item2;
				if (player == 1)
				{
					if (moveValues[k] > bestMoveValue || (moveValues[k] == bestMoveValue && random.Next(100) < 25))
					{
						bestMoveIndex = k;
						bestMoveValue = moveValues[k];
					}
					alfa = Math.Max(alfa, bestMoveValue);
				}
				else
				{
					if (moveValues[k] < bestMoveValue || (moveValues[k] == bestMoveValue && random.Next(100) < 25))
					{
						bestMoveIndex = k;
						bestMoveValue = moveValues[k];
					}
					beta = Math.Min(beta, bestMoveValue);
				}
				globalBoard.UndoMove(moves[k]);
				if (alfa > beta) break;
			}
			currentDepth--;
			//for (int i = 0; i < moves.Length; i++)
			//{
			//	moveHelper[i] = (moves[i], moveValues[i]);
			//}
			return (moves[bestMoveIndex], bestMoveValue);
		}

		private double EndingEvaluation()
		{
			int color = globalBoard.IsWhiteToMove ? 1 : -1;

			if (globalBoard.IsInCheckmate())
			{
				return -color * (1000 - currentDepth);
			}
			if (globalBoard.IsInsufficientMaterial())
			{
				return 0;
			}
			if (globalBoard.IsFiftyMoveDraw() || globalBoard.IsInStalemate() || globalBoard.IsRepeatedPosition())
			{
				return color * 500 * Math.Sign(StaticEvaluation());
			}
			return StaticEvaluation();
		}

		private double StaticEvaluation()
		{
			double result = 0;
			for (int row = 0; row < 8; row++)
			{
				for (int col = 0; col < 8; col++)
				{
					Piece p = globalBoard.GetPiece(new Square(col, row));
					int pieceColor = p.IsWhite ? 1 : -1;
					if (p.IsPawn)
					{
						result += pieceColor * 1;
					}
					if (p.IsKnight)
					{
						result += pieceColor * 3.5;
					}
					if (p.IsBishop)
					{
						result += pieceColor * 3.5;
					}
					if (p.IsRook)
					{
						result += pieceColor * 5;
					}
					if (p.IsQueen)
					{
						result += pieceColor * 10;
					}
					//if (p.IsKing)
					//{
					//	result += ColorEvaluation(p) * 10000;
					//}
				}
			}
			return result;
		}
	}
}