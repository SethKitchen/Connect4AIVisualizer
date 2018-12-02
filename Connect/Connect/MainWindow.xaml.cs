using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Connect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static BoardState TheBoard;
        public static Random random = new Random();
        public static double SPLIT = .99;
        public static double LEFTOVER;
        public static bool MoveHasBeenMade = false;
        public int MovePlayer1 = 0;
        public static Dictionary<StateAction, double> Q=new Dictionary<StateAction, double>();
        public static Dictionary<StateAction, int> Q_count=new Dictionary<StateAction, int>();
        public Thread currentGameThread;

        public void LoadQsAndQCount()
        {
            if (File.Exists("bestQ"))
            {
                using (StreamReader sr = new StreamReader("bestQ"))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        string[] split = line.Split(',');
                        int rows = int.Parse(split[1]);
                        int cols = int.Parse(split[2]);
                        BoardState bs = new BoardState(rows, cols);
                        int splitCounter = 3;
                        for (int a = 0; a < rows; a++)
                        {
                            for (int b = 0; b < cols; b++)
                            {
                                bs.board[a, b] = int.Parse(split[splitCounter]);
                                splitCounter++;
                            }
                        }
                        StateAction sa = new StateAction(bs, int.Parse(split[0]));
                        Q.Add(sa, double.Parse(split[splitCounter]));
                    }
                }
            }
            if (File.Exists("bestQCounter"))
            {
                using (StreamReader sr = new StreamReader("bestQCounter"))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        string[] split = line.Split(',');
                        int rows = int.Parse(split[1]);
                        int cols = int.Parse(split[2]);
                        BoardState bs = new BoardState(rows, cols);
                        int splitCounter = 3;
                        for (int a = 0; a < rows; a++)
                        {
                            for (int b = 0; b < cols; b++)
                            {
                                bs.board[a, b] = int.Parse(split[splitCounter]);
                                splitCounter++;
                            }
                        }
                        StateAction sa = new StateAction(bs, int.Parse(split[0]));
                        Q_count.Add(sa, int.Parse(split[splitCounter]));
                    }
                }
            }
        }

        public void DrawBoard()
        {
            for (int i = 0; i < TheBoard.rows; i++)
            {
                for (int j = 0; j < TheBoard.cols; j++)
                {
                    Ellipse r = new Ellipse();
                    r.Height = 50;
                    r.Width = 50;
                    if (TheBoard.board[i, j] == 0)
                    {
                        r.Fill = new SolidColorBrush(Colors.Yellow);
                    }
                    else if(TheBoard.board[i,j]==1)
                    {
                        r.Fill = new SolidColorBrush(Colors.Black);
                    }
                    else
                    {
                        r.Fill = new SolidColorBrush(Colors.Red);
                    }
                    Canvas.SetLeft(r, j * 50);
                    Canvas.SetTop(r, i*50);
                    BoardGrid.Children.Add(r);
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            TheBoard = new BoardState(6, 7);
            DrawBoard();
            LoadQsAndQCount();
            Thread t = new Thread(StartGame);
            currentGameThread = t;
            t.Start();
        }

        public double random_start()
        {
            return random.NextDouble() * (0.1);
        }

        public int Pi(BoardState b)
        {
            int bestCol = -1;
            double bestWeight = -1;
            LEFTOVER= ((1.0 - SPLIT) / (b.cols - 1));
            for (int i = 0; i < b.cols; ++i)
            {
                StateAction sa = new StateAction(b, i);
                double value = 0;
                double weight = (Q.TryGetValue(sa, out value)) ? Q[sa] : random_start();
                if (weight > bestWeight)
                {
                    bestCol = i;
                    bestWeight = weight;
                }
            }

            List<Tuple<int, double>> probs = new List<Tuple<int, double>>();
            for (int i = 0; i < b.cols; ++i)
            {
                probs.Add(new Tuple<int, double>(0, 0));
                if (i == bestCol)
                    probs[i] = new Tuple<int, double>(i, SPLIT);
                else
                    probs[i] = new Tuple<int, double>(i, LEFTOVER);
            }

            return pick_random(probs);
        }

        public int pick_random(List<Tuple<int, double>> probs)
        {

            // get universal probability 
            double u = probs.Sum(p => p.Item2);

            // pick a random number between 0 and u
            double r = random.NextDouble() * u;

            double sum = 0;
            foreach (var t in probs)
            {
                // loop until the random number is less than our cumulative probability
                if (r <= (sum = sum + t.Item2))
                {
                    return t.Item1;
                }
            }
            return -1; //Should never get here.
        }


        public int random_action(BoardState b)
        {
            return random.Next(0, b.cols);

        }

        public class Episode
        {
            public List<StateAction> sas = new List<StateAction>();
            public Result result;
        }

        public void StartGame()
        {
            Result r = Result.Invalid;
            while (!(r == Result.Win || r == Result.Tie || r == Result.Loss))
            {
                r=ComPlay();
                BoardGrid.Dispatcher.Invoke(() => {
                    DrawBoard();
                });
                if(r==Result.Win)
                {
                    BoardGrid.Dispatcher.Invoke(() =>
                    {
                        winner.Text = "Computer";
                    });
                }
                if (!(r == Result.Win || r == Result.Tie || r == Result.Loss))
                {
                    r=UserPlay();
                    BoardGrid.Dispatcher.Invoke(() => {
                        DrawBoard();
                    });
                    if (r == Result.Win)
                    {
                        BoardGrid.Dispatcher.Invoke(() =>
                        {
                            winner.Text = "Player";
                        });
                    }
                }
            }
        }

        public Result ComPlay()
        {
            Result r = Result.Invalid;
            int p1Action;
            BoardState bn = TheBoard;
            while (r == Result.Invalid)
            {
                p1Action = Pi(TheBoard);
                bn = TheBoard.move(p1Action, Player.Player1, ref r);
            }
            TheBoard = bn;
            return r;
        }

        public Result UserPlay()
        {
            Result r = Result.Invalid;
            BoardState bn = TheBoard;
            while (r == Result.Invalid)
            {
                while (!MoveHasBeenMade) ;
                MoveHasBeenMade = false;
                bn = TheBoard.move(MovePlayer1, Player.Player2, ref r);
            }
            TheBoard = bn;
            return r;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MoveHasBeenMade = true;
            MovePlayer1 = 0;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MoveHasBeenMade = true;
            MovePlayer1 = 1;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MoveHasBeenMade = true;
            MovePlayer1 = 2;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            MoveHasBeenMade = true;
            MovePlayer1 = 3;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            MoveHasBeenMade = true;
            MovePlayer1 = 4;
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            MoveHasBeenMade = true;
            MovePlayer1 = 5;
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            MoveHasBeenMade = true;
            MovePlayer1 = 6;
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            TheBoard = new BoardState(6, 7);
            DrawBoard();
            Thread t = new Thread(StartGame);
            currentGameThread.Abort();
            currentGameThread = t;
            t.Start();
            winner.Text = "";
        }
    }
}
