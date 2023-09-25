﻿using ChessChallenge.API;
using MyChess = ChessChallenge.Chess;
using System;
using System.Linq;
using System.Collections.Generic;

public class MyBot : IChessBot
{
	// undo accidental remote branch deletion
	// global variables
	private Board globalBoard;
	private readonly Random random = new Random();
	private int maxDepth = 5, currentDepth = 0, maxBreadth = 150000, currentBreadth = 1;
	private Move dummyMove;
	private Dictionary<Move, double> killerMoves = new Dictionary<Move, double>();

	private double GetKillerMove(Move move)
	{
		if (killerMoves.ContainsKey(move)) return killerMoves[move];
		return 0;
	}

	public Move Think(Board board, Timer timer)
	{
		globalBoard = board;

		// shallow search
		//int fullMaxDepth = maxDepth;
		//maxDepth = 4;
		//dummyMove = DeepThink(timer, -2000, 2000, board.IsWhiteToMove ? 1 : -1).Item1;
		// full depth search
		//maxDepth = fullMaxDepth;
		dummyMove = DeepThink(timer, -2000, 2000, board.IsWhiteToMove ? 1 : -1).Item1;
		//if (dummyMove.IsCapture || dummyMove.MovePieceType == PieceType.Pawn)
		//{
		//	killerMoves.Clear();
		//}
		return dummyMove;
	}

	private (Move, double) DeepThink(Timer timer, double alfa, double beta, int player)
	{
		// if a leaf is reached return the static evaluation
		if ((currentDepth >= maxDepth && currentBreadth >= maxBreadth) || globalBoard.IsInCheckmate() || globalBoard.IsDraw())
		{
			int color = globalBoard.IsWhiteToMove ? 1 : -1;
			if (globalBoard.IsInsufficientMaterial())
				return (dummyMove, 0);
			if (globalBoard.IsInCheckmate())
				return (dummyMove, -color * (1000 - currentDepth));
			if (globalBoard.IsFiftyMoveDraw() || globalBoard.IsInStalemate() || globalBoard.IsRepeatedPosition())
				return (dummyMove, color * 500 * Math.Sign(StaticEvaluation()));
			return (dummyMove, StaticEvaluation());
		}

		// initializations
		int bestMoveIndex = -1;
		double bestMoveValue = -2000 * player;
		//Move[] moves = new Move[]();
		Span<Move> moves = new Span<Move>();
		globalBoard.GetLegalMovesNonAlloc(ref moves);
		//Move[] moves = globalBoard.GetLegalMoves();
		// if(currentDepth > maxDepth) moves = globalBoard.get
		double[] moveValues = new double[moves.Length];

		// sort moves
		//moves = moves.OrderByDescending(move => killerMoves.ContainsKey(move) ? killerMoves[move] : 0)
		//	.ThenByDescending(move => (int)move.CapturePieceType)
		//	.ThenBy(move => (int)globalBoard.GetPiece(move.StartSquare).PieceType)
		//	.ToArray();
		moves = moves.ToArray().OrderByDescending(move => (int)move.CapturePieceType)
			.ThenBy(move => (int)globalBoard.GetPiece(move.StartSquare).PieceType)
			//.ThenByDescending(move => killerMoves.ContainsKey(move) ? killerMoves[move] : 0)
			.ToArray();

		currentDepth++;
		currentBreadth = currentBreadth * (moves.Length + 1);
		for (int k = 0; k < moves.Length; k++)
		{
			globalBoard.MakeMove(moves[k]);
			moveValues[k] = DeepThink(timer, alfa, beta, -player).Item2;
			globalBoard.UndoMove(moves[k]);

			if (player == 1)
			{
				if (moveValues[k] > bestMoveValue)// || (moveValues[k] == bestMoveValue && random.Next(100) < 25))
				{
					bestMoveIndex = k;
					bestMoveValue = moveValues[k];
				}
				alfa = Math.Max(alfa, bestMoveValue);
			}
			else
			{
				if (moveValues[k] < bestMoveValue)// || (moveValues[k] == bestMoveValue && random.Next(100) < 25))
				{
					bestMoveIndex = k;
					bestMoveValue = moveValues[k];
				}
				beta = Math.Min(beta, bestMoveValue);
			}
			if (alfa > beta) break;
		}
		currentBreadth = currentBreadth / (moves.Length + 1);
		currentDepth--;

		//if (killerMoves.ContainsKey(moves[bestMoveIndex]))
		//{
		//	killerMoves[moves[bestMoveIndex]] = Math.Max(killerMoves[moves[bestMoveIndex]], Math.Abs(bestMoveValue));
		//}
		//else if (!moves[bestMoveIndex].IsPromotion && !moves[bestMoveIndex].IsCapture)
		//	killerMoves.Add(moves[bestMoveIndex], Math.Abs(bestMoveValue));

		return (moves[bestMoveIndex], bestMoveValue);
	}

	#region evaluation

	private double StaticEvaluation()
	{
		double result = 0, whiteScore = 0, blackScore = 0;
		for (int row = 0; row < 8; row++)
		{
			for (int col = 0; col < 8; col++)
			{
				// piece value
				Square square = new Square(col, row);
				Piece piece = globalBoard.GetPiece(square);
				result = 0;
				if (piece.IsPawn)
				{
					if (PassedPawn(square))
					{
						if (piece.IsWhite)
						{
							switch (row)
							{
								case 6: result = 4.5; break;
								case 5: result = 2.5; break;
								case 4: result = 1.5; break;
								case 3: result = 1.3; break;
								default: result = 1.1; break;
							}
						}
						else
						{
							switch (row)
							{
								case 1: result = 4.5; break;
								case 2: result = 2.5; break;
								case 3: result = 1.5; break;
								case 4: result = 1.3; break;
								default: result = 1.1; break;
							}
						}
					}
					else
					{
						if (piece.IsWhite)
						{
							switch (row)
							{
								case 6: result = 4.5; break;
								case 5: result = 1.5; break;
								case 4: result = 1.1; break;
								default: result = 1; break;
							}
						}
						else
						{
							switch (row)
							{
								case 1: result = 4.5; break;
								case 2: result = 1.5; break;
								case 3: result = 1.1; break;
								default: result = 1; break;
							}
						}
					}
					if (IsolatedPawn(square)) result -= 0.15;
					//if (BackwardPawn(square)) result -= 0.1;
					if (MultiplePawn(square)) result -= 0.1;
				}
				if (piece.IsKnight) result = 3.25 + Square(row, col) / 2;
				if (piece.IsBishop) result = 3.25 + DiagonalPositionValue[f(row), f(col)] / 121 / 2;
				if (piece.IsRook) result = 5;
				if (piece.IsQueen) result = 9.75 + (DiagonalPositionValue[f(row), f(col)] + 196) / 317 / 2;
				if (piece.IsKing) result = Square(row, col) / 5;
				// player value
				if (piece.IsWhite) whiteScore += result;
				else blackScore += result;
			}
		}
		return whiteScore - blackScore;
		//if (whiteScore > blackScore)
		//{
		//	return whiteScore / blackScore;
		//}
		//return -blackScore / whiteScore;
	}

	private bool PassedPawn(Square square)
	{
		if (globalBoard.GetPiece(square).IsWhite)
			return (MyChess.Bits.WhitePassedPawnMask[square.Index] & globalBoard.GetPieceBitboard(PieceType.Pawn, false)) == 0;
		return (MyChess.Bits.BlackPassedPawnMask[square.Index] & globalBoard.GetPieceBitboard(PieceType.Pawn, true)) == 0;
	}

	//private bool BackwardPawn(Square square)
	//{
	//	int rank = MyChess.BoardHelper.RankIndex(square.Index);
	//	ulong WhiteBackwardMask = ((1ul << 8 * (rank + 1)) - 1);
	//	ulong BlackBackwardMask = ~(ulong.MaxValue >> (64 - 8 * rank));
	//	if (globalBoard.GetPiece(square).IsWhite)
	//	{
	//		return (WhiteBackwardMask & MyChess.Bits.AdjacentFileMasks[square.File] & globalBoard.GetPieceBitboard(PieceType.Pawn, true)) == 0;
	//	}
	//	return (BlackBackwardMask & MyChess.Bits.AdjacentFileMasks[square.File] & globalBoard.GetPieceBitboard(PieceType.Pawn, false)) == 0;
	//}

	private bool MultiplePawn(Square square)
	{
		return BitboardHelper.GetNumberOfSetBits(
			MyChess.Bits.FileMask[square.File] & globalBoard.GetPieceBitboard(PieceType.Pawn, globalBoard.GetPiece(square).IsWhite)
			) > 1;
	}

	private bool IsolatedPawn(Square square)
	{
		return (MyChess.Bits.AdjacentFileMasks[square.File] & globalBoard.GetPieceBitboard(PieceType.Pawn, globalBoard.GetPiece(square).IsWhite)) == 0;
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