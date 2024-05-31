using System.Net.NetworkInformation;

public class MoveGen(Board board)
{
    public Board board = board;
    public Colour sideToMove = board.sideToMove;
    public MoveList moveList = new();
    public Move rootJump = new(0);
    public const uint Mask3 = 0x70707070;
    public const uint Mask5 = 0x0E0E0E0E;
    public static int Offset(Direction direction) => Move.JumpAddDir[(int)direction];
    public static bool ValidDirection(Square from, Direction direction)
    {
        if (from >= Square.A7 && Utils.isNorth(direction)) return false;
        if (from <= Square.H2 && !Utils.isNorth(direction)) return false;
        if ((int)from % 4 == 0 && !Utils.isEast(direction)) return false;
        if ((int)from % 4 == 3 && Utils.isEast(direction)) return false;
        return true;
    }
    public int Forward() => (sideToMove == Colour.White) ? 0 : 2;
    public int Backward() => (sideToMove == Colour.White) ? 2 : 0;
    public void GenerateWhiteQuiet()
    {
        uint nOcc = ~(board.wOcc | board.bOcc);
        uint wKing = board.wOcc & board.kings;

        AddMoves(nOcc >> 4 & board.wOcc, 4);
        AddMoves((nOcc & Mask3) >> 3 & board.wOcc, 3);
        AddMoves((nOcc & Mask5) >> 5 & board.wOcc, 5);

        if (wKing != 0)
        {
            AddMoves(nOcc << 4 & wKing, -4);
            AddMoves((nOcc & Mask5) << 3 & wKing, -3);
            AddMoves((nOcc & Mask3) << 5 & wKing, -5);
        }
    }

    public void GenerateBlackQuiet()
    {
        uint nOcc = ~(board.wOcc | board.bOcc);
        uint bKing = board.bOcc & board.kings;

        AddMoves(nOcc << 4 & board.bOcc, -4);
        AddMoves((nOcc & Mask5) << 3 & board.bOcc, -3);
        AddMoves((nOcc & Mask3) << 5 & board.bOcc, -5);

        if (bKing != 0)
        {
            AddMoves(nOcc >> 4 & bKing, 4);
            AddMoves((nOcc & Mask3) >> 3 & bKing, 3);
            AddMoves((nOcc & Mask5) >> 5 & bKing, 5);
        }
    }

    public void GenerateWhiteJumps()
    {
        uint nOcc = ~(board.wOcc | board.bOcc);
        uint wKing = board.wOcc & board.kings;

        uint temp = nOcc >> 4 & board.bOcc;
        if (temp != 0)
        {
            AddJumps((temp & Mask3) >> 3 & board.wOcc, 7);
            AddJumps((temp & Mask5) >> 5 & board.wOcc, 9);
        }
        temp = (nOcc & Mask3) >> 3 & board.bOcc;
        if (temp != 0) AddJumps(temp >> 4 & board.wOcc, 7);
        temp = (nOcc & Mask5) >> 5 & board.bOcc;
        if (temp != 0) AddJumps(temp >> 4 & board.wOcc, 9);

        if (wKing != 0)
        {
            temp = nOcc << 4 & board.bOcc;
            if (temp != 0)
            {
                AddJumps((temp & Mask5) << 3 & wKing, -7);
                AddJumps((temp & Mask3) << 5 & wKing, -9);
            }
            temp = (nOcc & Mask5) << 3 & board.bOcc;
            if (temp != 0) AddJumps(temp << 4 & wKing, -7);
            temp = (nOcc & Mask3) << 5 & board.bOcc;
            if (temp != 0) AddJumps(temp << 4 & wKing, -9);
        }
    }

    public void GenerateBlackJumps()
    {
        uint nOcc = ~(board.wOcc | board.bOcc);
        uint bKing = board.bOcc & board.kings;

        uint temp = nOcc << 4 & board.wOcc;
        if (temp != 0)
        {
            AddJumps((temp & Mask5) << 3 & board.bOcc, -7);
            AddJumps((temp & Mask3) << 5 & board.bOcc, -9);
        }
        temp = (nOcc & Mask5) << 3 & board.wOcc;
        if (temp != 0) AddJumps(temp << 4 & board.bOcc, -7);
        temp = (nOcc & Mask3) << 5 & board.wOcc;
        if (temp != 0) AddJumps(temp << 4 & board.bOcc, -9);

        if (bKing != 0)
        {
            temp = nOcc >> 4 & board.wOcc;
            if (temp != 0)
            {
                AddJumps((temp & Mask3) >> 3 & bKing, 7);
                AddJumps((temp & Mask5) >> 5 & bKing, 9);
            }
            temp = (nOcc & Mask3) >> 3 & board.wOcc;
            if (temp != 0) AddJumps(temp >> 4 & bKing, 7);
            temp = (nOcc & Mask5) >> 5 & board.wOcc;
            if (temp != 0) AddJumps(temp >> 4 & bKing, 9);
        }
    }

    public void AddJumps(uint movers, int offset)
    {
        while (movers != 0)
        {
            Square from = Bitboard.PopBit(ref movers);
            Square to = from + offset;

            board.MovePiece(from, to);
            rootJump = new Move(from, to);
            FindSqJumps(to, Utils.MidSquare(from, to), 0, Bitboard.IsSet(board.kings, to));
            board.MovePiece(to, from);
        }
    }
    public void AddSqDir(Square from, int pathIdx, Direction direction, bool isKing)
    {
        if (!ValidDirection(from, direction)) return;

        uint Them = board.Occ(sideToMove ^ Colour.Black);
        uint Occ = board.wOcc | board.bOcc;

        Square to = from + Offset(direction);
        Square jumpSquare = Utils.MidSquare(from, to);
        if (Bitboard.IsSet(Them, jumpSquare) && !Bitboard.IsSet(Occ, to))
        {
            rootJump.SetJumpDir(pathIdx, direction);
            FindSqJumps(to, jumpSquare, pathIdx + 1, isKing);
        }
    }
    public void FindSqJumps(Square from, Square jumpSquare, int pathIdx, bool isKing)
    {
        Colour enemy = sideToMove ^ Colour.Black;
        uint oldPieces = board.Occ(enemy);
        board.Occ(enemy) ^= Bitboard.SquareBB(jumpSquare);

        AddSqDir(from, pathIdx, Forward() + Direction.NE, isKing);
        AddSqDir(from, pathIdx, Forward() + Direction.NW, isKing);

        if (isKing)
        {
            AddSqDir(from, pathIdx, Backward() + Direction.NE, isKing);
            AddSqDir(from, pathIdx, Backward() + Direction.NW, isKing);
        }

        moveList.AddJump(rootJump, pathIdx);

        board.Occ(enemy) = oldPieces;
    }
    public void AddMoves(uint movers, int offset)
    {
        while (movers != 0)
        {
            Square from = Bitboard.PopBit(ref movers);
            Square to = from + offset;
            moveList.AddMove(new Move(from, to));
        }
    }

    public void GenerateMoves()
    {
        if (sideToMove == Colour.White)
        {
            GenerateWhiteJumps();
            GenerateWhiteQuiet();
        }
        else
        {
            GenerateBlackJumps();
            GenerateBlackQuiet();
        }
    }

    public void GenerateJumps()
    {
        if (sideToMove == Colour.White)
            GenerateWhiteJumps();
        else
            GenerateBlackJumps();
    }

    public void OrderMoves(SearchStats stats, int ply)
    {
        for (int i = 0; i < moveList.count; i++)
        {
            Move move = moveList.moves[i];
            Square from = move.From();
            Square to = move.To();

            move.score = move.JumpLen() > 0 ? 10000 : 0;

            if (stats.killerMoves[ply, 0] == move) move.score += 9000;
            if (stats.killerMoves[ply, 1] == move) move.score += 8000;
            move.score += stats.historyMoves[(int)from, (int)to];
        }

        moveList.Sort();
    }
}