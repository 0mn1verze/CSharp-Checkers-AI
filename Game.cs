using System.Diagnostics;

public class SearchStats
{
    public Move[,] killerMoves = new Move[Game.MAX_DEPTH, 2];
    public int[,] historyMoves = new int[(int)Square.NSquares, (int)Square.NSquares];

    public void updateHistory(Move move, int depth)
    {
        historyMoves[(int)move.From(), (int)move.GetFinalDestination()] += depth * depth;
    }

    public void updateKiller(Move move, int ply)
    {
        if (killerMoves[ply, 0] == null || killerMoves[ply, 0] == move)
            return;
        killerMoves[ply, 1] = killerMoves[ply, 0];
        killerMoves[ply, 0] = move;
    }
}

public static class Zobrist
{
    public static readonly ulong[,,] pieceKeys = new ulong[(int)Colour.NColours, (int)PieceType.NPieceTypes, (int)Square.NSquares];
    public static readonly ulong sideKey;

    public static readonly Random random = new(9999);

    static Zobrist()
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];

        for (int i = 0; i < (int)Colour.NColours; i++)
        {
            for (int j = 0; j < (int)PieceType.NPieceTypes; j++)
            {
                for (int k = 0; k < (int)Square.NSquares; k++)
                {
                    random.NextBytes(buffer);
                    pieceKeys[i, j, k] = BitConverter.ToUInt64(buffer);
                }
            }
        }

        random.NextBytes(buffer);
        sideKey = BitConverter.ToUInt64(buffer);
    }

    public static ulong pieceKey(Colour colour, bool isKing, Square square) => pieceKeys[(int)colour, isKing ? 1 : 0, (int)square];
}

public class Game
{
    public const int MAX_HISTORY = 512;
    public const int MAX_DEPTH = 64;
    public const int INF = 50000;
    public const int MATE = INF - MAX_DEPTH;
    public const int INVALID_VAL = 50001;
    public Board board = new Board();
    public int moveCount = 0;
    public int lastReversible = 0;
    public Board[] history = new Board[MAX_HISTORY];
    public ulong[] hashHistory = new ulong[MAX_HISTORY];
    public int ply;
    public long nodes;
    public Colour computerSide = Colour.Black;
    public SearchStats searchStats = new();

    public TranspositionTable TTable;

    public Game()
    {
        board.hash = CalculateZobristKey();
        TTable = new(16);
    }

    public ulong CalculateZobristKey()
    {
        ulong hash = 0;

        for (Rank rank = Rank.R8; rank >= Rank.R1; rank--)
        {
            for (File file = File.FA; file <= File.FH; file++)
            {
                if (Utils.IsGameSquare(rank, file))
                {
                    if (Bitboard.IsSet(board.wOcc, rank, file))
                        hash ^= Zobrist.pieceKey(Colour.White, Bitboard.IsSet(board.kings, rank, file), Utils.Square(rank, file));
                    else if (Bitboard.IsSet(board.bOcc, rank, file))
                        hash ^= Zobrist.pieceKey(Colour.Black, Bitboard.IsSet(board.kings, rank, file), Utils.Square(rank, file));
                }
            }
        }

        if (board.sideToMove == Colour.Black)
            hash ^= Zobrist.sideKey;

        return hash;
    }

    public void DoSingleJump(Square from, Square to)
    {
        Square jumpSquare = Utils.MidSquare(from, to);

        board.MovePiece(from, to);

        board.hash ^= Zobrist.pieceKey(board.sideToMove, Bitboard.IsSet(board.kings, to), from);
        board.hash ^= Zobrist.pieceKey(board.sideToMove, Bitboard.IsSet(board.kings, to), to);
        board.hash ^= Zobrist.pieceKey(board.sideToMove ^ Colour.Black, Bitboard.IsSet(board.kings, jumpSquare), jumpSquare);

        board.RemovePiece(jumpSquare);
    }

    public void MakeMove(Move move)
    {
        hashHistory[moveCount] = board.hash;
        history[moveCount++] = (Board)board.Clone();


        Square from = move.From();
        Square to = move.To();

        int jumpLen = move.JumpLen();

        bool isKing = Bitboard.IsSet(board.kings, from);

        if (jumpLen == 0)
        {

            board.MovePiece(from, to);
            board.movesSinceCapture++;

            board.hash ^= Zobrist.pieceKey(board.sideToMove, isKing, from);
            board.hash ^= Zobrist.pieceKey(board.sideToMove, isKing, to);

            if (!isKing)
                lastReversible = moveCount;
        }
        else
        {
            DoSingleJump(from, to);

            if (jumpLen > 1)
            {
                Square next = to;

                for (int i = 0; i < jumpLen - 1; i++)
                {
                    next += Move.JumpAddDir[(int)move.Dir(i)];
                    DoSingleJump(to, next);
                    to = next;
                }
            }

            board.movesSinceCapture = 0;
            lastReversible = moveCount;
        }

        if (!isKing && ((Utils.Rank(to) == Rank.R8 && board.sideToMove == Colour.White) || (Utils.Rank(to) == Rank.R1 && board.sideToMove == Colour.Black)))
        {
            board.hash ^= Zobrist.pieceKey(board.sideToMove, false, to);
            board.hash ^= Zobrist.pieceKey(board.sideToMove, true, to);
            Bitboard.SetBit(ref board.kings, to);
            lastReversible = moveCount;
        }

        board.sideToMove ^= Colour.Black;
        board.hash ^= Zobrist.sideKey;
    }

    public void UnmakeMove() => board = history[--moveCount];

    public int QuiescenceSearch(PVLine parentPV, int alpha, int beta)
    {
        PVLine childPV = new();

        if (ply >= MAX_DEPTH - 1)
            return Eval();

        if (board.movesSinceCapture >= 99)
            return 0;

        nodes++;

        MoveGen moveGen = new(board);

        moveGen.GenerateJumps();

        if (moveGen.moveList.count == 0)
            return Eval();

        moveGen.OrderMoves(searchStats, ply);

        int value = Eval();

        for (int i = 0; i < moveGen.moveList.count; i++)
        {
            Move move = moveGen.moveList.moves[i];

            MakeMove(move);

            ply++;

            value = Math.Max(value, -QuiescenceSearch(childPV, -beta, -alpha));

            ply--;

            UnmakeMove();

            if (value > alpha)
            {
                alpha = value;

                parentPV.UpdatePV(childPV, move);
                if (value > beta)
                {
                    searchStats.updateKiller(move, ply);
                    searchStats.updateHistory(move, 0);
                    return beta;
                }
            }
        }

        return value;

    }

    public int NegaMaxSearch(PVLine parentPV, int alpha, int beta, int depth)
    {
        if (depth <= 0)
            return QuiescenceSearch(parentPV, alpha, beta);

        if (ply >= MAX_DEPTH - 1)
            return Eval();

        if (IsRepetition())
            return 0;

        PVLine childPV = new();
        bool pvNode = alpha + 1 < beta;
        bool rootNode = depth == 0;
        Move bestMove = new(0);
        int value = -INF;
        int ttValue = INVALID_VAL;
        int boardEval = INVALID_VAL;

        nodes++;

        ref TTEntry entry = ref TTable.GetEntry(board);

        if (!rootNode && entry != null)
        {
            entry.Read(board, alpha, beta, ref bestMove, ref ttValue, ref boardEval, depth, ply);
            if (ttValue != INVALID_VAL && ttValue >= alpha)
                return ttValue;

            if (!pvNode && beta > -1500 && ttValue == INVALID_VAL && depth > 2 && ply >= 3)
            {
                if (boardEval == INVALID_VAL)
                {
                    boardEval = Eval();
                    if (entry != null) entry.boardEval = boardEval;
                }

                if (boardEval >= beta + 30)
                {
                    int verifyDepth = Math.Max(depth - 4, 1);
                    int verifyValue = -NegaMaxSearch(childPV, -(beta + 30 + 1), -(beta + 30), verifyDepth);
                    if (verifyValue > beta + 30) value = verifyValue;
                }
            }
        }

        MoveGen moveGen = new(board);

        moveGen.GenerateMoves();

        if (moveGen.moveList.count == 0)
            return -INF + ply;

        if (board.movesSinceCapture >= 99)
            return 0;

        moveGen.OrderMoves(searchStats, ply);

        int moveCount = 0;
        bool skipQuiet = false;

        for (int i = 0; i < moveGen.moveList.count; i++)
        {
            Move move = moveGen.moveList.moves[i];

            if (depth >= 8 && moveCount > (3 + depth * depth) / 2)
                skipQuiet = true;

            if (skipQuiet && move.JumpLen() == 0)
                continue;

            moveCount++;

            MakeMove(move);

            ply++;

            bool doFullSearch;

            if (depth >= 2 && moveCount > 1 && move.JumpLen() == 0)
            {
                int pvsDepth = (moveCount > 4) ? depth - 1 - ply / 2 : depth - 2;

                value = -NegaMaxSearch(childPV, -alpha - 1, -alpha, pvsDepth);

                doFullSearch = value > alpha;
            }
            else
                doFullSearch = !pvNode || moveCount > 1;

            if (doFullSearch)
                value = -NegaMaxSearch(childPV, -alpha - 1, -alpha, depth - 1);

            if (pvNode && (moveCount == 1 || value > alpha))
                value = -NegaMaxSearch(childPV, -beta, -alpha, depth - 1);

            ply--;

            UnmakeMove();

            if (value > alpha)
            {
                alpha = value;
                bestMove = move;

                parentPV.UpdatePV(childPV, move);

                if (value >= beta)
                {
                    entry?.Write(board, alpha, beta, ref bestMove, value, boardEval, depth, ply);
                    searchStats.updateKiller(move, ply);
                    searchStats.updateHistory(move, depth);
                    return beta;
                }
            }
        }

        entry?.Write(board, alpha, beta, ref bestMove, value, boardEval, depth, ply);

        return value;
    }

    public bool IsRepetition()
    {
        for (int i = lastReversible; i < moveCount; i++)
        {
            if (hashHistory[i] == board.hash)
                return true;
        }
        return false;
    }

    public Move SearchPosition()
    {
        nodes = 0;
        Print();

        Move bestMove = new(0);


        var watch = Stopwatch.StartNew();

        PVLine pv = new();

        for (int i = 1; i < 11; i++)
        {
            int score = NegaMaxSearch(pv, -INF, INF, i);

            bestMove = pv.moves[0];

            Console.Write($"Depth: {i} Score: {score} Nodes: {nodes} Time: {watch.ElapsedMilliseconds}ms nodes/s: {nodes * 1000 / (watch.ElapsedMilliseconds + 1)} ");
            pv.Print();
        }

        watch.Stop();

        return bestMove;
    }

    public bool CheckCornerTrap(Square corner, Square trapSq, Square kingTrapSq, bool isWhite)
    {
        uint Us = isWhite ? board.wOcc : board.bOcc;
        uint Them = isWhite ? board.bOcc : board.wOcc;

        if (Bitboard.IsSet(Us, corner))
        {
            if (Bitboard.IsSet(Them, trapSq) || (Bitboard.IsSet(Them, kingTrapSq) && Bitboard.IsSet(board.kings, kingTrapSq)))
                return true;
        }
        return false;
    }

    public int Eval()
    {
        int whitePieces = Bitboard.PopCount(board.wOcc);
        int blackPieces = Bitboard.PopCount(board.bOcc);

        int whiteKings = Bitboard.PopCount(board.wOcc & board.kings);
        int blackKings = Bitboard.PopCount(board.bOcc & board.kings);

        int score = 100 * (whitePieces - blackPieces) + 50 * (whiteKings - blackKings);

        // Approximate the distance to promotion
        int whiteDistToKing = Rank.R8 - Utils.Rank(Bitboard.MSB(board.wOcc));
        int blackDistToKing = Utils.Rank(Bitboard.LSB(board.bOcc)) - Rank.R1;

        score += 10 * (whiteDistToKing - blackDistToKing);

        // Estimate center control
        uint whiteCenter = board.wOcc & Board.MiddleRow;
        uint blackCenter = board.bOcc & Board.MiddleRow;
        uint whiteKingCenter = whiteCenter & board.kings;
        uint blackKingCenter = blackCenter & board.kings;
        uint whiteCenterBox = whiteCenter & Board.MiddleBox;
        uint blackCenterBox = blackCenter & Board.MiddleBox;
        uint whiteKingCenterBox = whiteKingCenter & Board.MiddleBox;
        uint blackKingCenterBox = blackKingCenter & Board.MiddleBox;

        score += Bitboard.PopCount(whiteCenter) - Bitboard.PopCount(blackCenter);
        score += Bitboard.PopCount(whiteKingCenter) - Bitboard.PopCount(blackKingCenter);
        score += Bitboard.PopCount(whiteCenterBox) - Bitboard.PopCount(blackCenterBox);
        score += Bitboard.PopCount(whiteKingCenterBox) - Bitboard.PopCount(blackKingCenterBox);

        // Check for passers
        for (int i = (int)File.FA; i < (int)File.FH; i++)
        {
            uint potentialWhitePassers = board.wOcc & Board.RankMask[i];
            Square passer = Bitboard.MSB(potentialWhitePassers);
            uint passedMask = Board.PassedMask[(int)Colour.White, (int)passer];
            if (passer == Square.NSquares)
                continue;
            if ((passedMask & board.bOcc) == 0)
                score += 5;

            uint potentialBlackPassers = board.bOcc & Board.RankMask[i];
            passer = Bitboard.LSB(potentialBlackPassers);
            if (passer == Square.NSquares)
                continue;
            passedMask = Board.PassedMask[(int)Colour.Black, (int)passer];
            if ((passedMask & board.wOcc) == 0)
                score -= 5;
        }

        // Check if piece is trapped in corner
        if (CheckCornerTrap(Square.A1, Square.A3, Square.C1, true))
            score -= 10;
        if (CheckCornerTrap(Square.H8, Square.H6, Square.F8, false))
            score += 10;

        uint backward = board.wOcc >> 4 & ((board.wOcc & Board.Mask3) >> 3 | (board.wOcc & Board.Mask5) >> 5);
        int whiteThreatened = Bitboard.PopCount(backward & board.wOcc);
        int whiteProtected = Bitboard.PopCount(backward & ~board.wOcc);
        score += 5 * (whiteProtected - whiteThreatened);

        backward = board.bOcc << 4 & ((board.bOcc & Board.Mask5) << 3 | (board.bOcc & Board.Mask3) << 5);
        int blackThreatened = Bitboard.PopCount(backward & board.bOcc);
        int blackProtected = Bitboard.PopCount(backward & ~board.bOcc);
        score -= 5 * (blackProtected - blackThreatened);

        return (board.sideToMove == Colour.White) ? score : -score;
    }

    public void Turn()
    {
        if (board.sideToMove == computerSide)
            ComputerTurn();
        else
            PlayerTurn();

        Print();

        MoveGen moveGen = new(board);
        moveGen.GenerateMoves();

        if (moveGen.moveList.count == 0)
        {
            Console.WriteLine((board.sideToMove ^ Colour.Black) + " won");
            Environment.Exit(0);
        }

        if (board.movesSinceCapture >= 99)
        {
            Console.WriteLine("Draw");
            Environment.Exit(0);
        }
    }

    public void SelfPlay()
    {
        ComputerTurn();

        Print();

        MoveGen moveGen = new(board);
        moveGen.GenerateMoves();

        if (moveGen.moveList.count == 0)
        {
            Console.WriteLine((board.sideToMove ^ Colour.Black) + " won");
            Environment.Exit(0);
        }

        if (board.movesSinceCapture >= 99)
        {
            Console.WriteLine("Draw");
            Environment.Exit(0);
        }

        Console.WriteLine("Press any key to continue");
        Console.ReadKey();
    }

    public void ComputerTurn()
    {
        Move move = SearchPosition();
        MakeMove(move);
    }

    public void PlayerTurn()
    {
        Console.Write("Enter move: ");
        string? move = Console.ReadLine();

        if (string.Equals(move, "quit", StringComparison.OrdinalIgnoreCase) || string.Equals(move, "exit", StringComparison.OrdinalIgnoreCase) || string.Equals(move, "stop", StringComparison.OrdinalIgnoreCase))
            Environment.Exit(0);

        Move parsed = ParseMove(move);
        if (parsed.Equals(new Move(0)))
        {
            Console.WriteLine("Invalid move");
            PlayerTurn();

        }
        else
        {
            Console.WriteLine(parsed);
            MakeMove(parsed);

        }
    }

    public Move ParseMove(string? move)
    {
        MoveGen moveGen = new(board);
        moveGen.GenerateMoves();

        for (int i = 0; i < moveGen.moveList.count; i++)
        {
            Move m = moveGen.moveList.moves[i];
            if (string.Equals(m.From().ToString() + m.GetFinalDestination().ToString(), move, StringComparison.OrdinalIgnoreCase))
                return m;
        }
        return new Move(0);
    }

    public void Play()
    {
        Console.Write("Self Play? (y/n): ");
        string? selfPlay = Console.ReadLine();
        if (string.Equals(selfPlay, "y", StringComparison.OrdinalIgnoreCase))
        {
            while (true)
            {
                SelfPlay();
            }
        }

        Console.Write("Enter side to play as (w/b): ");
        string? side = Console.ReadLine();
        if (string.Equals(side, "w", StringComparison.OrdinalIgnoreCase))
            computerSide = Colour.Black;
        else
            computerSide = Colour.White;

        Print();
        while (true)
        {
            Turn();
        }

    }

    public void Print()
    {
        board.Print();
        Console.WriteLine("Side to move: " + board.sideToMove);
        Console.WriteLine($"Hash key: {board.hash:x}");
    }
}