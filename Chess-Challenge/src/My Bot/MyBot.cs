using ChessChallenge.API;
using System;

public class MyBot : IChessBot
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
		double result = 0, whiteScore = 0, blackScore = 0;
		for (int row = 0; row < 8; row++)
		{
			for (int col = 0; col < 8; col++)
			{
				// piece value
				Piece piece = globalBoard.GetPiece(new Square(col, row));
				result = 0;
				if (piece.IsPawn)
				{
					if (piece.IsWhite)
					{
						switch (row)
						{
							case 6: { result = 4.5; break; }
							case 5: { result = 1.5; break; }
							case 4: { result = 1.1; break; }
							default: { result = 1; break; }
						}
					}
					else
					{
						switch (row)
						{
							case 1: { result = 4.5; break; }
							case 2: { result = 1.5; break; }
							case 3: { result = 1.1; break; }
							default: { result = 1; break; }
						}
					}
				}
				if (piece.IsKnight)
				{
					result = 3.5 + KnightRelativePositionValue2[row, col] / 56;
				}
				if (piece.IsBishop)
				{
					result = 3.5 + BishopRelativePositionValue2[row, col] / 121;
				}
				if (piece.IsRook)
				{
					result = 5;
				}
				if (piece.IsQueen)
				{
					result = 10 + QueenRelativePositionValue2[row, col] / 317;
				}
				if (piece.IsKing)
				{
					result = KingRelativePositionValue2[row, col] / 512 / 2;
				}
				// player value
				if (piece.IsWhite)
				{
					whiteScore += result;
				}
				else
				{
					blackScore += result;
				}
			}
		}
		return whiteScore - blackScore;
	}

	#region value tables
	//max=56
	private double[,] KnightRelativePositionValue2 ={
	{ 12,18,23,26,26,23,18,12},
	{ 18,24,32,37,37,32,24,18},
	{ 23,32,42,48,48,42,32,23},
	{ 26,37,48,56,56,48,37,26},
	{ 26,37,48,56,56,48,37,26},
	{ 23,32,42,48,48,42,32,23},
	{ 18,24,32,37,37,32,24,18},
	{ 12,18,23,26,26,23,18,12}
	};
	//max=121
	private double[,] BishopRelativePositionValue2 ={
	{ 73,67,63,61,61,63,67,73},
	{ 67,85,81,79,79,81,85,67},
	{ 63,81,101,99,99,101,81,63},
	{ 61,79,99,121,121,99,79,61},
	{ 61,79,99,121,121,99,79,61},
	{ 63,81,101,99,99,101,81,63},
	{ 67,85,81,79,79,81,85,67},
	{ 73,67,63,61,61,63,67,73}
	};
	//max=317
	private double[,] QueenRelativePositionValue2 ={
	{ 269,263,259,257,257,259,263,269},
	{ 263,281,277,275,275,277,281,263},
	{ 259,277,297,295,295,297,277,259},
	{ 257,275,295,317,317,295,275,257},
	{ 257,275,295,317,317,295,275,257},
	{ 259,277,297,295,295,297,277,259},
	{ 263,281,277,275,275,277,281,263},
	{ 269,263,259,257,257,259,263,269}
	};
	//max=512
	private double[,] KingRelativePositionValue2 ={
	{ 105,183,220,233,233,220,183,105},
	{ 183,318,382,404,404,382,318,183},
	{ 220,382,459,485,485,459,382,220},
	{ 233,404,485,512,512,485,404,233},
	{ 233,404,485,512,512,485,404,233},
	{ 220,382,459,485,485,459,382,220},
	{ 183,318,382,404,404,382,318,183},
	{ 105,183,220,233,233,220,183,105}
	};
	#endregion
}