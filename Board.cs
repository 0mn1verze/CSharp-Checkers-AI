public class Board : ICloneable
{
    public uint wOcc;
    public uint bOcc;
    public uint kings;
    public Colour sideToMove;
    public int movesSinceCapture;

    public ulong hash;

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