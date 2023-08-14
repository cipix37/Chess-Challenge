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
					result += pieceColor * (3.5 + KnightRelativePositionValue[row, col] / 8);
				}
				if (p.IsBishop)
				{
					result += pieceColor * (3.5 + BishopRelativePositionValue[row, col] / 13);
				}
				if (p.IsRook)
				{
					result += pieceColor * 5;
				}
				if (p.IsQueen)
				{
					result += pieceColor * (10 + QueenRelativePositionValue[row, col] / 27);
				}
				if (p.IsKing)
				{
					result += pieceColor * KingRelativePositionValue[row, col] / 8 / 2;
				}
			}
		}
		return result;
	}

	//max=8
	private double[,] KnightRelativePositionValue = {
	{ 2, 3, 4, 4, 4, 4, 3, 2},
	{ 3, 4, 6, 6, 6, 6, 4, 3},
	{ 4, 6, 8, 8, 8, 8, 6, 4},
	{ 4, 6, 8, 8, 8, 8, 6, 4},
	{ 4, 6, 8, 8, 8, 8, 6, 4},
	{ 4, 6, 8, 8, 8, 8, 6, 4},
	{ 3, 4, 6, 6, 6, 6, 4, 3},
	{ 2, 3, 4, 4, 4, 4, 3, 2}
	};
	//max=13
	private double[,] BishopRelativePositionValue ={
	{ 7, 7, 7, 7, 7, 7, 7, 7},
	{ 7, 23, 23, 23, 23, 23, 23, 7},
	{ 7, 23, 11, 11, 11, 11, 23, 7},
	{ 7, 23, 11, 13, 13, 11, 23, 7},
	{ 7, 23, 11, 13, 13, 11, 23, 7},
	{ 7, 23, 11, 11, 11, 11, 23, 7},
	{ 7, 23, 23, 23, 23, 23, 23, 7},
	{ 7, 7, 7, 7, 7, 7, 7, 7}
	};
	//max=27
	private double[,] QueenRelativePositionValue ={
	{ 21, 21, 21, 21, 21, 21, 21, 21},
	{ 21, 23, 23, 23, 23, 23, 23, 21},
	{ 21, 23, 25, 25, 25, 25, 23, 21},
	{ 21, 23, 25, 27, 27, 25, 23, 21},
	{ 21, 23, 25, 27, 27, 25, 23, 21},
	{ 21, 23, 25, 25, 25, 25, 23, 21},
	{ 21, 23, 23, 23, 23, 23, 23, 21},
	{ 21, 21, 21, 21, 21, 21, 21, 21}
	};
	//max=8
	private double[,] KingRelativePositionValue ={
	{ 3, 5, 5, 5, 5, 5, 5, 3},
	{ 5, 8, 8, 8, 8, 8, 8, 5},
	{ 5, 8, 8, 8, 8, 8, 8, 5},
	{ 5, 8, 8, 8, 8, 8, 8, 5},
	{ 5, 8, 8, 8, 8, 8, 8, 5},
	{ 5, 8, 8, 8, 8, 8, 8, 5},
	{ 5, 8, 8, 8, 8, 8, 8, 5},
	{ 3, 5, 5, 5, 5, 5, 5, 3}
	};
}