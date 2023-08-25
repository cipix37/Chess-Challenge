using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
	// transposition table
	private class Transposition
	{
		public Move Move;
		public double Value;
		public int AnalyzedDepth;
		public int MovesSinceLastHit;

		public Transposition(double value, int analyzedDepth)
		{
			Value = value;
			AnalyzedDepth = analyzedDepth;
			MovesSinceLastHit = 0;
		}

		public Transposition(Move move)
		{
			Move = move;
		}

		public void UpdateTransposition(Transposition transposition)
		{
			Value = transposition.Value;
			AnalyzedDepth = transposition.AnalyzedDepth;
		}
	}

	private Dictionary<ulong, Transposition> transpositionTable = new Dictionary<ulong, Transposition>();

	private void AddTransposition(ulong key, Transposition transposition)
	{
		if (!transpositionTable.ContainsKey(key))
		{
			transpositionTable.Add(key, transposition);
		}
		else
		{
			if (((Math.Abs(transpositionTable[key].Value + transposition.Value) < 1800) && transpositionTable[key].AnalyzedDepth < transposition.AnalyzedDepth) ||
				(Math.Abs(transpositionTable[key].Value + transposition.Value) > 1800 && transpositionTable[key].AnalyzedDepth > transposition.AnalyzedDepth))
			{
				transpositionTable[key].Value = transposition.Value;
				transpositionTable[key].AnalyzedDepth = transposition.AnalyzedDepth;
				transpositionTable[key].MovesSinceLastHit = 0;
			}
		}
	}

	// global variables
	private Board globalBoard;
	private readonly Random random = new Random();
	private int maxDepth = 4, currentDepth = 0, maxBreadth = 250000, currentBreadth = 1;

	public Move Think(Board board, Timer timer)
	{
		globalBoard = board;
		Move move = DeepThink(timer, -2000, 2000, board.IsWhiteToMove ? 1 : -1).Move;

		// clean transposition table
		if (move.IsCapture || move.MovePieceType == PieceType.Pawn)
		{
			transpositionTable.Clear();
		}
		else
		{
			foreach ((ulong key, Transposition transposition) in transpositionTable)
			{
				transposition.MovesSinceLastHit++;
				if (transposition.MovesSinceLastHit > transposition.AnalyzedDepth + 2)
				{
					transpositionTable.Remove(key);
				}
			}
		}
		return move;
	}

	private Transposition DeepThink(Timer timer, double alfa, double beta, int player)
	{
		// if a leaf is reached return the static evaluation
		if ((currentDepth >= maxDepth && currentBreadth >= maxBreadth) || globalBoard.IsInCheckmate() || globalBoard.IsDraw())
		{
			double value = EndingEvaluation();
			AddTransposition(globalBoard.ZobristKey, new Transposition(value, 0));
			return new Transposition(value, 0);
		}

		// initializations
		int bestMoveIndex = -1;
		double bestMoveValue = -2000 * player;
		Transposition[] moves = globalBoard.GetLegalMoves().Select(move => new Transposition(move)).ToArray();

		// sort moves
		for (int k = 0; k < moves.Length; k++)
		{
			// move deepening part
			globalBoard.MakeMove(moves[k].Move);
			// check transposition table - only here
			if (transpositionTable.ContainsKey(globalBoard.ZobristKey))
			{
				if (transpositionTable[globalBoard.ZobristKey].AnalyzedDepth > maxDepth - currentDepth)
				{
					moves[k].UpdateTransposition(transpositionTable[globalBoard.ZobristKey]);
					transpositionTable[globalBoard.ZobristKey].MovesSinceLastHit = 0;
				}
			}
			globalBoard.UndoMove(moves[k].Move);
		}
		moves.OrderBy(move => move.Value);

		currentDepth++;
		currentBreadth = currentBreadth * (moves.Length + 1);
		for (int k = 0; k < moves.Length; k++)
		{
			// move deepening part
			globalBoard.MakeMove(moves[k].Move);
			moves[k].UpdateTransposition(DeepThink(timer, alfa, beta, -player));
			globalBoard.UndoMove(moves[k].Move);

			// alpha-beta part
			if (player == 1)
			{
				if (moves[k].Value > bestMoveValue)// || (moves[k].Value == bestMoveValue && random.Next(100) < 25))
				{
					bestMoveIndex = k;
					bestMoveValue = moves[k].Value;
				}
				alfa = Math.Max(alfa, bestMoveValue);
			}
			else
			{
				if (moves[k].Value < bestMoveValue)// || (moves[k].Value == bestMoveValue && random.Next(100) < 25))
				{
					bestMoveIndex = k;
					bestMoveValue = moves[k].Value;
				}
				beta = Math.Min(beta, bestMoveValue);
			}
			if (alfa > beta) break;
		}
		currentBreadth = currentBreadth / (moves.Length + 1);
		currentDepth--;

		AddTransposition(globalBoard.ZobristKey, new Transposition(bestMoveValue, moves[bestMoveIndex].AnalyzedDepth + 1));
		moves[bestMoveIndex].AnalyzedDepth++;
		return moves[bestMoveIndex];
	}

	#region Evaluation

	// not efficient to replace this in the method call
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
					result = 3.5 + SquarePositionValue[f(row), f(col)] / 56;
				}
				if (piece.IsBishop)
				{
					result = 3.5 + DiagonalPositionValue[f(row), f(col)] / 121;
				}
				if (piece.IsRook)
				{
					result = 5;
				}
				if (piece.IsQueen)
				{
					result = 10 + (DiagonalPositionValue[f(row), f(col)] + 196) / 317;
				}
				if (piece.IsKing)
				{
					result = SquarePositionValue[f(row), f(col)] / 112;
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
		//if (whiteScore > blackScore)
		//{
		//	return whiteScore / blackScore;
		//}
		//return -blackScore / whiteScore;
	}

	#region Value Tables

	private int f(int x) => 3.5 > x ? x : 7 - x;

	// max=56
	private double[,] SquarePositionValue ={
	{ 12,18,23,26},
	{ 18,24,32,37},
	{ 23,32,42,48},
	{ 26,37,48,56},
	};
	// max=121
	private double[,] DiagonalPositionValue ={
	{ 73,67,63,61},
	{ 67,85,81,79},
	{ 63,81,101,99},
	{ 61,79,99,121},
	};
	// rook max 196
	#endregion

	#endregion
}