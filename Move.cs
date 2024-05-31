
// Following the implementation in GUINN checkers.
// Format of a move: from(5), to(5), jumpLen(4), jumpPathDirs(18) 9 * 2
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public class Move : ICloneable
{
    public uint data;
    public int score;
    public static int[] JumpAddDir = [7, 9, -9, -7];
    public Move() => data = 0;
    public Move(uint data) => this.data = data;
    public Move(Square from, Square to) => data = (uint)from | (uint)to << 5;
    public object Clone() => MemberwiseClone();
    public Square From() => (Square)(data & 31);
    public Square To() => (Square)((data >> 5) & 31);
    public int JumpLen() => (int)((data >> 10) & 15);
    public Direction Dir(int i) => (Direction)((data >> (14 + i * 2)) & 3);
    public void SetJumpDir(int i, Direction direction)
    {
        int shift = 14 + i * 2;
        data &= ~((uint)0xFFFFF << shift);
        data |= (uint)direction << shift;
    }
    public void SetJumpLen(int jumpLen)
    {
        data &= ~((uint)15 << 10);
        data |= (uint)jumpLen << 10;
    }
    public bool Equals(Move move) => data == move.data;
    public Square GetFinalDestination()
    {
        Square sq = To();
        for (int i = 0; i < JumpLen() - 1; i++)
            sq += JumpAddDir[(int)Dir(i)];
        return sq;
    }
    // public override string ToString() => $"{From()} -> {GetFinalDestination()}";
    public override string ToString()
    {
        if (JumpLen() == 0)
            return $"{From()} -> {To()}";
        return $"{From()} x {GetFinalDestination()}";
    }
}