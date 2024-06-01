public class Board : ICloneable
{
    public uint wOcc;
    public uint bOcc;
    public uint kings;
    public Colour sideToMove;
    public int movesSinceCapture;
    public const uint MiddleRow = 0x000FF000;
    public const uint MiddleBox = 0x00066000;
    public const uint Mask3 = 0x70707070;
    public const uint Mask5 = 0x0E0E0E0E;
    public static uint[] FileMask = [0x01010101, 0x10101010, 0x02020202, 0x20202020, 0x04040404, 0x40404040, 0x08080808, 0x80808080];
    public static uint[] RankMask = [0xF, 0xF0, 0xF00, 0xF000, 0xF0000, 0xF00000, 0xF000000, 0xF0000000];
    public static uint[,] PassedMask = new uint[2, 32];
    public ulong hash;

    static Board()
    {
        for (Square i = Square.A1; i < Square.H8; i++)
        {
            File file = Utils.File(i);
            Rank rank = Utils.Rank(i);
            PassedMask[(int)Colour.White, (int)i] = FileMask[(int)file];
            if (file != File.FA)
                PassedMask[(int)Colour.White, (int)i] |= FileMask[(int)file - 1];
            if (file != File.FH)
                PassedMask[(int)Colour.White, (int)i] |= FileMask[(int)file + 1];

            for (Rank r = rank - 1; r >= Rank.R1; r--)
                PassedMask[(int)Colour.White, (int)i] &= ~RankMask[(int)r];

            PassedMask[(int)Colour.Black, (int)i] = FileMask[(int)file];
            if (file != File.FA)
                PassedMask[(int)Colour.Black, (int)i] |= FileMask[(int)file - 1];
            if (file != File.FH)
                PassedMask[(int)Colour.Black, (int)i] |= FileMask[(int)file + 1];

            for (Rank r = rank + 1; r <= Rank.R8; r++)
                PassedMask[(int)Colour.Black, (int)i] &= ~RankMask[(int)r];
        }
    }

    public Board()
    {
        wOcc = 0x00000FFF;
        bOcc = 0xFFF00000;
        kings = 0x00000000;
    }

    public object Clone() => MemberwiseClone();

    public ref uint Occ(Colour colour) => ref (colour == Colour.White) ? ref wOcc : ref bOcc;

    public void AddPiece(Square square, Colour colour, PieceType pieceType)
    {
        uint bb = Bitboard.SquareBB(square);
        if (colour == Colour.White)
        {
            wOcc |= bb;
            if (pieceType == PieceType.King)
                kings |= bb;
        }
        else
        {
            bOcc |= bb;
            if (pieceType == PieceType.King)
                kings |= bb;
        }
    }

    public void MovePiece(Square from, Square to)
    {
        uint bitMove = Bitboard.SquareBB(from) | Bitboard.SquareBB(to);
        ref uint occ = ref Occ(sideToMove);
        occ ^= bitMove;

        if (Bitboard.IsSet(kings, from))
            kings ^= bitMove;
    }

    public void RemovePiece(Square square)
    {
        uint bb = Bitboard.SquareBB(square);
        wOcc &= ~bb;
        bOcc &= ~bb;
        kings &= ~bb;
    }

    public void Print()
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
                    if (Bitboard.IsSet(wOcc, rank, file))
                        if (Bitboard.IsSet(kings, rank, file))
                            Console.Write("| W ");
                        else
                            Console.Write("| w ");
                    else if (Bitboard.IsSet(bOcc, rank, file))
                        if (Bitboard.IsSet(kings, rank, file))
                            Console.Write("| B ");
                        else
                            Console.Write("| b ");
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