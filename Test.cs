public class Test
{
    public Game game = new Game();

    public void Perft(int depth)
    {
        game.Print();

        var watch = System.Diagnostics.Stopwatch.StartNew();

        MoveGen moveGen = new(game.board);

        moveGen.GenerateMoves();

        int count = 0;
        for (int i = 0; i < moveGen.moveList.count; i++)
        {
            game.MakeMove(moveGen.moveList.moves[i]);
            int nodes = PerftTest(depth - 1);
            game.UnmakeMove();

            Console.WriteLine(moveGen.moveList.moves[i].ToString() + " " + nodes);

            count += nodes;
        }

        watch.Stop();

        var elapsedMs = watch.ElapsedMilliseconds;


        Console.WriteLine(count);

        Console.WriteLine(elapsedMs);
    }

    public int PerftTest(int depth)
    {

        if (depth == 0)
        {
            return 1;
        }

        MoveGen moveGen = new(game.board);

        moveGen.GenerateMoves();

        int count = 0;
        for (int i = 0; i < moveGen.moveList.count; i++)
        {
            game.MakeMove(moveGen.moveList.moves[i]);
            count += PerftTest(depth - 1);
            game.UnmakeMove();
        }

        return count;
    }
}