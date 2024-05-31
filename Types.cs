
using System.ComponentModel.DataAnnotations;

public enum Colour : int { White, Black, NColours = 2 }

public enum PieceType : int { Man, King, NPieceTypes = 2 }

public enum Direction : int
{
    NW, NE, SW, SE,
    NDirections = 4,
}

public enum Rank : int
{
    R1 = 0,
    R2 = 1,
    R3 = 2,
    R4 = 3,
    R5 = 4,
    R6 = 5,
    R7 = 6,
    R8 = 7,
    NRanks = 8,
}

public enum File : int
{
    FA = 0,
    FB = 1,
    FC = 2,
    FD = 3,
    FE = 4,
    FF = 5,
    FG = 6,
    FH = 7,
    NFiles = 8,
}

public enum Square : int
{
    A1, C1, E1, G1,
    B2, D2, F2, H2,
    A3, C3, E3, G3,
    B4, D4, F4, H4,
    A5, C5, E5, G5,
    B6, D6, F6, H6,
    A7, C7, E7, G7,
    B8, D8, F8, H8,
    NSquares = 32,
}

public class PVLine
{
    public int length;
    public Move[] moves = new Move[Game.MAX_DEPTH + 1];

    public void UpdatePV(PVLine childPV, Move move)
    {
        length = 1 + childPV.length;
        moves[0] = move;
        for (int i = 0; i < childPV.length; i++)
            moves[i + 1] = childPV.moves[i];
    }

    public void Print()
    {
        for (int i = 0; i < length - 1; i++)
            Console.Write($"{moves[i]} ");
        Console.WriteLine();
    }
}


