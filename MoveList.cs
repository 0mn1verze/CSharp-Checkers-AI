using System.Globalization;

public class MoveList
{
    public Move[] moves;
    public int count;
    public int MAX_MOVES = 256;
    public MoveList()
    {
        moves = new Move[MAX_MOVES];
        count = 0;
    }
    public void AddMove(Move move)
    {
        moves[count++] = move;
    }
    public void AddJump(Move root, int pathNum)
    {
        root.SetJumpLen(pathNum + 1);
        moves[count++] = (Move)root.Clone();
    }
    public void Clear()
    {
        count = 0;
    }
    public void Print()
    {
        for (int i = 0; i < count; i++)
        {
            Console.WriteLine(moves[i]);
        }
    }
    public void Sort()
    {
        for (int i = 0; i < count - 1; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                if (moves[i].score < moves[j].score)
                {
                    (moves[j], moves[i]) = (moves[i], moves[j]);
                }
            }
        }
    }
}