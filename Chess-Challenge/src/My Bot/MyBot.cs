using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
	private readonly Random random = new Random();
	public Move Think(Board board, Timer timer)
	{
		Move[] moves = board.GetLegalMoves();
		return moves[random.Next(moves.Length)];
	}
}