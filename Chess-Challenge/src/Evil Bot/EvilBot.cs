﻿using ChessChallenge.API;
using System;
using System.Linq;
using MyChess = ChessChallenge.Chess;

namespace ChessChallenge.Example
{
	public class EvilBot : IChessBot
	{

		// global variables
		private Board globalBoard;
		private readonly Random random = new Random();
		private int maxDepth = 5, currentDepth = 0, maxBreadth = 150000, currentBreadth = 1;
		private Move dummyMove;

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
			Move[] moves = globalBoard.GetLegalMoves();
			double[] moveValues = new double[moves.Length];

			// sort moves
			moves = moves.OrderByDescending(move => ((int)move.CapturePieceType))
				.ThenBy(move => (int)globalBoard.GetPiece(move.StartSquare).PieceType)
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
					if (piece.IsBishop) result = 3.25 + DiagonalPositionValue[f(row), f(col)] / 46 / 2;
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
	{ 25,18,10,1},
	{ 18,40,36,31},
	{ 10,36,45,43},
	{ 1,31,43,46},
	};
		// rook max 196

		#endregion
	}
}