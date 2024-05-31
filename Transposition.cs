
using System.Runtime.InteropServices;
public enum TTFlag : byte
{
    Exact, FailLow, FailHigh
}

[StructLayout(LayoutKind.Sequential)]
public class TTEntry
{
    public ulong hash = 0;
    public Move bestMove = new(0);
    public int searchEval = Game.INVALID_VAL;
    public int boardEval = Game.INVALID_VAL;
    public int depth = 0;
    public TTFlag flag = TTFlag.Exact;
    public bool Read(Board board, int alpha, int beta, ref Move bestMove, ref int value, ref int boardEval, int depth, int ply)
    {

        if (hash == board.hash)
        {
            if (this.depth >= depth)
            {
                int tempVal = searchEval;

                if (searchEval > Game.MATE)
                    tempVal -= ply;
                else if (searchEval < -Game.MATE)
                    tempVal += ply;

                switch (flag)
                {
                    case TTFlag.Exact:
                        value = tempVal;
                        break;
                    case TTFlag.FailLow:
                        if (tempVal <= alpha) value = tempVal;
                        break;
                    case TTFlag.FailHigh:
                        if (tempVal >= beta) value = tempVal;
                        break;
                }
            }

            bestMove = this.bestMove;
            boardEval = this.boardEval;

            return true;
        }

        return false;
    }

    public void Write(Board board, int alpha, int beta, ref Move bestMove, int searchEval, int boardEval, int depth, int ply)
    {
        hash = board.hash;
        this.searchEval = searchEval;
        this.boardEval = boardEval;
        this.depth = depth;

        if (bestMove != new Move(0))
            this.bestMove = bestMove;

        if (this.searchEval > Game.MATE) this.searchEval += ply;
        if (this.searchEval < -Game.MATE) this.searchEval -= ply;

        if (this.searchEval <= alpha) flag = TTFlag.FailLow;
        else if (this.searchEval >= beta) flag = TTFlag.FailHigh;
        else flag = TTFlag.Exact;
    }
}

public class TranspositionTable
{
    public TTEntry[] table = new TTEntry[1];
    public ulong entries;
    public TranspositionTable(int sizeMB)
    {
        entries = (ulong)(sizeMB * (1 << 20) / Marshal.SizeOf(typeof(TTEntry)));
        table = new TTEntry[entries];

        for (ulong i = 0; i < entries; i++)
        {
            table[i] = new TTEntry();
        }
    }

    public ref TTEntry GetEntry(Board board)
    {
        return ref table[board.hash % entries];
    }
}