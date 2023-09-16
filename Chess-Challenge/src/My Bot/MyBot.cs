using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
	#region Transposition Table

	public MyBot()
	{
		for (ulong i = 0; i <= TranspositionMask; i++)
		{
			transpositionTable[i] = new Transposition();
		}
	}

	// transposition table
	private class Transposition
	{
		public double Value = 0;
		public int AnalyzedDepth = 0;
		public int Flag = 0; // invalid
		public int MovesSinceLastHit = 0;

		public Transposition()
		{
			Value = 0;
			AnalyzedDepth = 0;
			MovesSinceLastHit = 0;
			Flag = 0; // invalid
		}

		public Transposition(double value, int analyzedDepth)
		{
			Value = value;
			AnalyzedDepth = analyzedDepth;
			MovesSinceLastHit = 0;
			Flag = 1; // valid
		}
	}

	private const ulong TranspositionMask = 0x7FFFFF;
	private Transposition[] transpositionTable = new Transposition[TranspositionMask + 1];
	private Transposition currentTransposition
	{
		get { return transpositionTable[globalBoard.ZobristKey & TranspositionMask]; }
		set { transpositionTable[globalBoard.ZobristKey & TranspositionMask] = value; }
	}

	private void AddTransposition(double value, int analyzedDepth)
	{
		if (currentTransposition.Flag == 0)
		{
			currentTransposition = new Transposition(value, analyzedDepth);
		}
		else if (((Math.Abs(currentTransposition.Value + value) < 1800) && currentTransposition.AnalyzedDepth < analyzedDepth) ||
				(Math.Abs(currentTransposition.Value + value) > 1800 && currentTransposition.AnalyzedDepth > analyzedDepth))
		{
			currentTransposition = new Transposition(value, analyzedDepth);
		}
	}
	#endregion

	// global variables
	private Board globalBoard;
	private readonly Random random = new Random();
	private int maxDepth = 5, currentDepth = 0, maxBreadth = 150000, currentBreadth = 1;

	public Move Think(Board board, Timer timer)
	{
		globalBoard = board;
		Move move = DeepThink(timer, -2000, 2000, board.IsWhiteToMove ? 1 : -1).Item1;

		// clean transposition table
		if (move.IsCapture || move.MovePieceType == PieceType.Pawn)
		{
			foreach (Transposition transposition in transpositionTable)
			{
				transposition.Flag = 0; // invalid
			}
		}
		else
		{
			foreach (Transposition transposition in transpositionTable)
			{
				transposition.MovesSinceLastHit++;
				if (transposition.MovesSinceLastHit > transposition.AnalyzedDepth + 2)
				{
					transposition.Flag = 0; // invalid
				}
			}
		}
		return move;
	}

	private (Move, double, int) DeepThink(Timer timer, double alfa, double beta, int player)
	{
		// initializations
		int bestMoveIndex = -1;
		double bestMoveValue = -2000 * player;
		(Move, double, int)[] moves = globalBoard.GetLegalMoves().Select(move => (move, 0.0, 0)).ToArray();

		// if a leaf is reached return the static evaluation
		if ((currentDepth >= maxDepth && currentBreadth >= maxBreadth) || globalBoard.IsInCheckmate() || globalBoard.IsDraw())
		{
			double value = EndingEvaluation();
			AddTransposition(value, 0);
			return (new Move(), value, 0);
		}

		// sort moves V2
		for (int k = 0; k < moves.Length; k++)
		{
			// move deepening part
			globalBoard.MakeMove(moves[k].Item1);
			// check transposition table - only here
			if (currentTransposition.Flag == 1) // if valid
			{
				if (currentTransposition.AnalyzedDepth > maxDepth - currentDepth)
				{
					moves[k].Item2 = currentTransposition.Value;
					moves[k].Item3 = currentTransposition.AnalyzedDepth;
					currentTransposition.MovesSinceLastHit = 0;
				}
			}
			globalBoard.UndoMove(moves[k].Item1);
		}

		if (player == 1)
		{
			moves = moves.OrderByDescending(move => move.Item2).ToArray();
		}
		else
		{
			moves = moves.OrderBy(move => move.Item2).ToArray();
		}

		// sort moves V1
		//moves = moves.OrderByDescending(move => ((int)move.CapturePieceType))
		//   .ThenBy(move => (int)globalBoard.GetPiece(move.StartSquare).PieceType)
		//   .ToArray();

		currentDepth++;
		currentBreadth = currentBreadth * (moves.Length + 1);
		for (int k = 0; k < moves.Length; k++)
		{
			// move deepening part
			if (moves[k].Item3 == 0 && currentDepth != maxDepth)
			{
				globalBoard.MakeMove(moves[k].Item1);
				(Move dummyMove, moves[k].Item2, moves[k].Item3) = DeepThink(timer, alfa, beta, -player);
				AddTransposition(moves[k].Item2, moves[k].Item3);
				globalBoard.UndoMove(moves[k].Item1);
			}

			// alpha-beta part
			if (player == 1)
			{
				if (moves[k].Item2 > bestMoveValue)// || (moves[k].Value == bestMoveValue && random.Next(100) < 25))
				{
					bestMoveIndex = k;
					bestMoveValue = moves[k].Item2;
				}
				alfa = Math.Max(alfa, bestMoveValue);
			}
			else
			{
				if (moves[k].Item2 < bestMoveValue)// || (moves[k].Value == bestMoveValue && random.Next(100) < 25))
				{
					bestMoveIndex = k;
					bestMoveValue = moves[k].Item2;
				}
				beta = Math.Min(beta, bestMoveValue);
			}
			if (alfa > beta) break;
		}
		currentBreadth = currentBreadth / (moves.Length + 1);
		currentDepth--;

		moves[bestMoveIndex].Item3++;
		AddTransposition(bestMoveValue, moves[bestMoveIndex].Item3);
		return moves[bestMoveIndex];
	}

	#region Evaluation

	// not efficient to replace this in the method call
	private double EndingEvaluation()
	{
		int color = globalBoard.IsWhiteToMove ? 1 : -1;

		if (globalBoard.IsInsufficientMaterial())
		{
			return 0;
		}
		if (globalBoard.IsFiftyMoveDraw() || globalBoard.IsInStalemate() || globalBoard.IsRepeatedPosition())
		{
			return color * 500 * Math.Sign(StaticEvaluation());
		}
		if (globalBoard.IsInCheckmate())
		{
			return -color * (1000 - currentDepth);
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
					result = 3.25 + Square(row, col) / 2;
				}
				if (piece.IsBishop)
				{
					result = 3.25 + DiagonalPositionValue[f(row), f(col)] / 121 / 2;
				}
				if (piece.IsRook)
				{
					result = 5;
				}
				if (piece.IsQueen)
				{
					result = 9.75 + (DiagonalPositionValue[f(row), f(col)] + 196) / 317 / 2;
				}
				if (piece.IsKing)
				{
					result = Square(row, col) / 5;
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

	private int f(int x) => 3.5 > x ? x : 7 - x;
	private double g(int x) => Math.Sin(Math.PI * x / 7);

	private double Square(int row, int col) => (g(row) + g(col)) / 2;

	// max=121
	private double[,] DiagonalPositionValue ={
	{ 73,67,63,61},
	{ 67,85,81,79},
	{ 63,81,101,99},
	{ 61,79,99,121},
	};
	// rook max 196

	#endregion
}