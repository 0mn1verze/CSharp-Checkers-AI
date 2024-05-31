
public static class Utils
{

    public static Square Square(Rank rank, File file) => (Square)((int)rank * 4 + (int)file / 2);

    public static Rank Rank(Square square) => (Rank)((int)square / 4);

    public static File File(Square square) => (File)((int)square % 4 * 2 + (int)Rank(square) % 2);

    public static Square MidSquare(Square sq1, Square sq2) => (Square)(((int)sq1 + (int)sq2 + ((int)Rank(sq1) % 2 == 0 ? -1 : 1)) / 2);

    public static bool IsGameSquare(Rank rank, File file) => ((int)rank + (int)file) % 2 == 0;

    public static bool isNorth(Direction direction) => ((int)direction & 2) == 0;

    public static bool isEast(Direction direction) => ((int)direction & 1) == 1;
}