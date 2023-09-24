using ChessChallenge.API;
using MyChess = ChessChallenge.Chess;
using System;
using System.Linq;
using System.Collections.Generic;

public class MyBot : IChessBot
{
	// global variables
	private Board globalBoard;
	private readonly Random random = new Random();
	private int currentMaxDepth, currentDepth = 0, maxBreadth, currentBreadth = 1;
	private Move dummyMove;
	private static int[] DepthAverageTimes = { 385, 385, 385, 385, 697, 2965, 19294, 168007 };
	private IEnumerable<int> DepthRange = Enumerable.Range(0, DepthAverageTimes.Length);

	public Move Think(Board board, Timer timer)
	{
		//ChessChallenge.Application.ConsoleHelper.Log("### Start turn ###");
		//ChessChallenge.Application.ConsoleHelper.Log($"Time remaining: {timer.MillisecondsRemaining}-{timer.OpponentMillisecondsRemaining}");

		globalBoard = board;

		// set defaut search depth
		int defaultDepth = DepthRange.LastOrDefault(k => DepthAverageTimes[k] < timer.GameStartTimeMilliseconds / 20);
		currentMaxDepth = defaultDepth;
		//ChessChallenge.Application.ConsoleHelper.Log($"Defaut depth = {currentMaxDepth}");
		//ChessChallenge.Application.ConsoleHelper.Log($"Ply count = {globalBoard.PlyCount}");

		// adjust search depth
		if (globalBoard.PlyCount > 16) // no need to overthink first couple of moves
		{
			// if I have enough time to search more depth, then do it
			// returns 0 if opponent has more time
			currentMaxDepth = Math.Max(currentMaxDepth,
				DepthRange.LastOrDefault(k => timer.MillisecondsRemaining - DepthAverageTimes[k] > timer.OpponentMillisecondsRemaining));
			//ChessChallenge.Application.ConsoleHelper.Log($"current Max Depth 1 = {currentMaxDepth}");

			// if there is a lot of remaining time in the match, then search more depth
			// returns 0 if no depth satisfies equation
			currentMaxDepth = Math.Max(currentMaxDepth,
				DepthRange.LastOrDefault(k => timer.MillisecondsRemaining - DepthAverageTimes[k] > timer.GameStartTimeMilliseconds / 6));
			//ChessChallenge.Application.ConsoleHelper.Log($"current Max Depth 2 = {currentMaxDepth}");

			// time crisis -> need to work on this to account for the time diff
			if (currentMaxDepth == defaultDepth)
			{
				if (timer.MillisecondsRemaining < timer.OpponentMillisecondsRemaining)
				{
					currentMaxDepth = 4;
				}
			}
		}

		// set max breadth depending on depth
		maxBreadth = (int)Math.Pow(10, currentMaxDepth);

		//ChessChallenge.Application.ConsoleHelper.Log($"max depth = {currentMaxDepth}");
		//ChessChallenge.Application.ConsoleHelper.Log($"max Breadth = {maxBreadth}");
		//ChessChallenge.Application.ConsoleHelper.Log(m.ToString());
		//ChessChallenge.Application.ConsoleHelper.Log($"Time used: {timer.MillisecondsElapsedThisTurn}");
		//ChessChallenge.Application.ConsoleHelper.Log($"Time remaining: {timer.MillisecondsRemaining}-{timer.OpponentMillisecondsRemaining}");
		//ChessChallenge.Application.ConsoleHelper.Log("### End turn ###\n");

		return DeepThink(timer, -2000, 2000, board.IsWhiteToMove ? 1 : -1).Item1;
	}

	private (Move, double) DeepThink(Timer timer, double alfa, double beta, int player)
	{
		// if a leaf is reached return the static evaluation
		//if (currentDepth >= currentMaxDepth || globalBoard.IsInCheckmate() || globalBoard.IsDraw())
		if ((currentDepth >= currentMaxDepth && currentBreadth >= maxBreadth) || globalBoard.IsInCheckmate() || globalBoard.IsDraw())
		{
			//ChessChallenge.Application.ConsoleHelper.Log($"current depth = {currentDepth} current breadth = {currentBreadth}");
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
		currentBreadth *= (2 + moves.Length / 3);
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
		currentBreadth /= (2 + moves.Length / 3);
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
					//if (PassedPawn(square))
					//{
					//	if (piece.IsWhite)
					//	{
					//		switch (row)
					//		{
					//			case 6: result = 4.5; break;
					//			case 5: result = 2.5; break;
					//			case 4: result = 1.5; break;
					//			case 3: result = 1.3; break;
					//			default: result = 1.1; break;
					//		}
					//	}
					//	else
					//	{
					//		switch (row)
					//		{
					//			case 1: result = 4.5; break;
					//			case 2: result = 2.5; break;
					//			case 3: result = 1.5; break;
					//			case 4: result = 1.3; break;
					//			default: result = 1.1; break;
					//		}
					//	}
					//}
					//else
					//{
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
					//}
					//if (IsolatedPawn(square)) result -= 0.15;
					////if (BackwardPawn(square)) result -= 0.1;
					//if (MultiplePawn(square)) result -= 0.1;
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