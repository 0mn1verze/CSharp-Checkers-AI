using System.Numerics;

public static class Bitboard
{

    public static uint SquareBB(Square square) => (uint)1 << (int)square;

    public static uint SquareBB(Rank rank, File file) => (uint)1 << (int)Utils.Square(rank, file);

    public static Square MSB(uint bitboard) => (Square)(32 - BitOperations.LeadingZeroCount(bitboard));

    public static Square LSB(uint bitboard) => (Square)BitOperations.TrailingZeroCount(bitboard);

    public static int PopCount(uint bitboard) => BitOperations.PopCount(bitboard);

    public static Square PopBit(ref uint bitboard)
    {
        Square square = LSB(bitboard);
        bitboard &= bitboard - 1;
        return square;
    }

    public static void SetBit(ref uint bitboard, Square square) => bitboard |= SquareBB(square);

    public static void ClearBit(ref uint bitboard, Square square) => bitboard &= ~SquareBB(square);

    public static bool IsSet(uint bitboard, Square square) => (bitboard & SquareBB(square)) != 0;

    public static bool IsSet(uint bitboard, Rank rank, File file) => (bitboard & SquareBB(rank, file)) != 0;

    public static void PrintBitboard(uint bitboard)
    {
        string seperator = "  +---+---+---+---+---+---+---+---+";

        for (Rank rank = Rank.R8; rank >= Rank.R1; rank--)
        {
            Console.WriteLine(seperator);
            Console.Write($"{(int)rank + 1} ");
            for (File file = File.FA; file <= File.FH; file++)
            {
                if (!Utils.IsGameSquare(rank, file))
                    Console.Write("|   ");
                else
                {
                    if (IsSet(bitboard, rank, file))
                        Console.Write("| + ");
                    else
                        Console.Write("|   ");
                }
            }
            Console.WriteLine("|");
        }
        Console.WriteLine(seperator);
        Console.WriteLine("    a   b   c   d   e   f   g   h");
    }
}